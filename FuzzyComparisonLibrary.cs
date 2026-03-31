namespace FuzzyComparisonLibrary;

public static class FuzzyComparer
{
    /// <summary>
    /// Returns a unified similarity score (0–1) by averaging all six algorithms.
    /// The six algorithms run in parallel for maximum throughput.
    /// </summary>
    public static double GetUnifiedSimilarity(string source, string target)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            return 0;

        double lev = 0, jaro = 0, cosine = 0, trigram = 0, simhash = 0, minhash = 0;

        Parallel.Invoke(
            () => lev     = LevenshteinSimilarity(source, target),
            () => jaro    = JaroWinklerSimilarity(source, target),
            () => cosine  = CosineSimilarity(source, target),
            () => trigram = TrigramOverlapSimilarity(source, target),
            () => simhash = SimHashSimilarity(source, target),
            () => minhash = MinHashSimilarity(source, target)
        );

        return (lev + jaro + cosine + trigram + simhash + minhash) / 6.0;
    }

    /// <summary>
    /// Returns a similarity score (0–1) computed by the supplied algorithm delegate.
    /// </summary>
    public static double GetSimilarityByMethod(string source, string target, Func<string, string, double> similarityMethod)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || similarityMethod is null)
            return 0;

        return similarityMethod(source, target);
    }

    // -------------------------------------------------------------------------
    // Public algorithm methods (usable individually via GetSimilarityByMethod)
    // -------------------------------------------------------------------------

    /// <summary>Levenshtein edit-distance similarity.</summary>
    public static double LevenshteinSimilarity(string source, string target)
    {
        int distance = LevenshteinDistance(source, target);
        int maxLength = Math.Max(source.Length, target.Length);
        return maxLength == 0 ? 1.0 : 1.0 - (double)distance / maxLength;
    }

    /// <summary>Jaro-Winkler similarity.</summary>
    public static double JaroWinklerSimilarity(string source, string target)
    {
        var (m, t) = GetMatchesAndTranspositions(source, target);
        if (m == 0) return 0;

        double jaro = (1.0 / 3.0) * (
            (double)m / source.Length +
            (double)m / target.Length +
            (double)(m - t) / m);

        int prefixLength = CommonPrefixLength(source, target);
        return jaro + 0.1 * prefixLength * (1.0 - jaro);
    }

    /// <summary>Cosine similarity over character bigrams.</summary>
    public static double CosineSimilarity(string source, string target)
    {
        var sourceBigrams = GetBigrams(source);
        var targetBigrams = GetBigrams(target);

        double dotProduct = 0;
        foreach (var (bigram, count) in sourceBigrams)
        {
            if (targetBigrams.TryGetValue(bigram, out int targetCount))
                dotProduct += count * targetCount;
        }

        double magSource = Math.Sqrt(sourceBigrams.Values.Sum(v => (double)v * v));
        double magTarget = Math.Sqrt(targetBigrams.Values.Sum(v => (double)v * v));

        return magSource == 0 || magTarget == 0 ? 0 : dotProduct / (magSource * magTarget);
    }

    /// <summary>Jaccard (trigram overlap) similarity.</summary>
    public static double TrigramOverlapSimilarity(string source, string target)
    {
        var sourceTrigrams = GetNGrams(source, 3);
        var targetTrigrams = GetNGrams(target, 3);

        int intersection = 0;
        foreach (var t in sourceTrigrams)
        {
            if (targetTrigrams.Contains(t)) intersection++;
        }

        int union = sourceTrigrams.Count + targetTrigrams.Count - intersection;
        return union == 0 ? 0 : (double)intersection / union;
    }

    /// <summary>SimHash similarity based on Hamming distance of fingerprints.</summary>
    public static double SimHashSimilarity(string source, string target)
    {
        long sourceHash = SimHash(source);
        long targetHash = SimHash(target);

        int differingBits = HammingWeight(sourceHash ^ targetHash);
        return 1.0 - (double)differingBits / 64;
    }

    /// <summary>
    /// MinHash Jaccard similarity estimate using 64 independent hash functions.
    /// </summary>
    public static double MinHashSimilarity(string source, string target)
    {
        var sourceShingles = GetNGrams(source, 3);
        var targetShingles = GetNGrams(target, 3);

        if (sourceShingles.Count == 0 && targetShingles.Count == 0) return 1.0;
        if (sourceShingles.Count == 0 || targetShingles.Count == 0) return 0.0;

        const int numHashes = 64;
        int matches = 0;

        for (int seed = 0; seed < numHashes; seed++)
        {
            int sourceMin = int.MaxValue, targetMin = int.MaxValue;
            foreach (var shingle in sourceShingles)
            {
                int h = HashWithSeed(shingle, seed);
                if (h < sourceMin) sourceMin = h;
            }
            foreach (var shingle in targetShingles)
            {
                int h = HashWithSeed(shingle, seed);
                if (h < targetMin) targetMin = h;
            }
            if (sourceMin == targetMin) matches++;
        }

        return (double)matches / numHashes;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Rolling two-row Levenshtein — O(min(m,n)) space instead of O(m·n).
    /// </summary>
    private static int LevenshteinDistance(string source, string target)
    {
        if (source.Length == 0) return target.Length;
        if (target.Length == 0) return source.Length;

        int[] previous = new int[target.Length + 1];
        int[] current  = new int[target.Length + 1];

        for (int j = 0; j <= target.Length; j++)
            previous[j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            current[0] = i;
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(previous[j] + 1, current[j - 1] + 1),
                    previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }

        return previous[target.Length];
    }

    /// <summary>
    /// Single-pass matching + transposition count for Jaro-Winkler.
    /// Replaces the old two-pass approach that called MatchingCharacters twice
    /// and contained a race-prone Parallel.For over shared mutable arrays.
    /// </summary>
    private static (int matches, int transpositions) GetMatchesAndTranspositions(
        string source, string target)
    {
        if (source.Length == 0 || target.Length == 0) return (0, 0);

        int matchWindow = Math.Max(0, Math.Max(source.Length, target.Length) / 2 - 1);
        bool[] sourceMatched = new bool[source.Length];
        bool[] targetMatched = new bool[target.Length];
        int matches = 0;

        for (int i = 0; i < source.Length; i++)
        {
            int start = Math.Max(0, i - matchWindow);
            int end   = Math.Min(target.Length, i + matchWindow + 1);
            for (int j = start; j < end; j++)
            {
                if (!targetMatched[j] && source[i] == target[j])
                {
                    sourceMatched[i] = true;
                    targetMatched[j] = true;
                    matches++;
                    break;
                }
            }
        }

        if (matches == 0) return (0, 0);

        int k = 0, transpositions = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (!sourceMatched[i]) continue;
            while (!targetMatched[k]) k++;
            if (source[i] != target[k]) transpositions++;
            k++;
        }

        return (matches, transpositions / 2);
    }

    private static int CommonPrefixLength(string source, string target)
    {
        int maxLen = Math.Min(4, Math.Min(source.Length, target.Length));
        int prefix = 0;
        for (int i = 0; i < maxLen; i++)
        {
            if (source[i] != target[i]) break;
            prefix++;
        }
        return prefix;
    }

    /// <summary>
    /// Sequential bigram frequency map — avoids ConcurrentDictionary/Parallel overhead
    /// for the short strings typical in fuzzy matching.
    /// </summary>
    private static Dictionary<string, int> GetBigrams(string text)
    {
        var bigrams = new Dictionary<string, int>(text.Length);
        for (int i = 0; i < text.Length - 1; i++)
        {
            string bigram = text.Substring(i, 2);
            bigrams[bigram] = bigrams.GetValueOrDefault(bigram) + 1;
        }
        return bigrams;
    }

    /// <summary>
    /// Builds a set of n-grams (bigrams, trigrams, shingles …) sequentially.
    /// Replaces the separate GetTrigrams / GetShingles helpers that used
    /// ConcurrentBag + Parallel.For.
    /// </summary>
    private static HashSet<string> GetNGrams(string text, int n)
    {
        int count = text.Length - n + 1;
        if (count <= 0) return [];
        var ngrams = new HashSet<string>(count);
        for (int i = 0; i < count; i++)
            ngrams.Add(text.Substring(i, n));
        return ngrams;
    }

    /// <summary>
    /// SimHash fingerprint — sequential inner loop eliminates Interlocked overhead.
    /// </summary>
    private static long SimHash(string text)
    {
        int[] bitVector = new int[64];
        foreach (var (bigram, _) in GetBigrams(text))
        {
            long hash = Hash(bigram);
            for (int i = 0; i < 64; i++)
                bitVector[i] += (hash & (1L << i)) != 0 ? 1 : -1;
        }

        long result = 0;
        for (int i = 0; i < 64; i++)
        {
            if (bitVector[i] > 0)
                result |= 1L << i;
        }
        return result;
    }

    private static long Hash(string text)
    {
        long hash = 0;
        foreach (char c in text)
            hash = hash * 31 + c;
        return hash;
    }

    private static int HashWithSeed(string text, int seed)
    {
        unchecked
        {
            // FNV-1a with seed mixing — good avalanche effect for MinHash
            const uint FnvPrime  = 16_777_619;
            const uint FnvOffset = 2_166_136_261;
            uint hash = FnvOffset ^ (uint)seed;
            hash *= FnvPrime;
            foreach (char c in text)
            {
                hash ^= (byte)(c & 0xFF);
                hash *= FnvPrime;
                hash ^= (byte)(c >> 8);
                hash *= FnvPrime;
            }
            return (int)hash;
        }
    }

    private static int HammingWeight(long value)
    {
        int weight = 0;
        while (value != 0)
        {
            weight++;
            value &= value - 1;
        }
        return weight;
    }
}
