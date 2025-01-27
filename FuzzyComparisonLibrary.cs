using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuzzyComparisonLibrary
{
    public static class FuzzyComparer
    {
        public static double GetUnifiedSimilarity(string source, string target)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                return 0;

            var scores = new ConcurrentBag<double>();

            Parallel.Invoke(
                () => scores.Add(LevenshteinSimilarity(source, target)),
                () => scores.Add(JaroWinklerSimilarity(source, target)),
                () => scores.Add(CosineSimilarity(source, target)),
                () => scores.Add(TrigramOverlapSimilarity(source, target)),
                () => scores.Add(SimHashSimilarity(source, target)),
                () => scores.Add(MinHashSimilarity(source, target))
            );

            return scores.Any() ? scores.Average() : 0;
        }

        public static double GetSimilarityByMethod(string source, string target, Func<string, string, double> similarityMethod)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || similarityMethod == null)
                return 0;

            return similarityMethod(source, target);
        }

        private static double LevenshteinSimilarity(string source, string target)
        {
            int distance = LevenshteinDistance(source, target);
            int maxLength = Math.Max(source.Length, target.Length);
            return maxLength == 0 ? 1 : 1 - (double)distance / maxLength;
        }

        private static int LevenshteinDistance(string source, string target)
        {
            var dp = new int[source.Length + 1, target.Length + 1];

            Parallel.For(0, source.Length + 1, i => dp[i, 0] = i);
            Parallel.For(0, target.Length + 1, j => dp[0, j] = j);

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                    dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
                }
            }

            return dp[source.Length, target.Length];
        }

        private static double JaroWinklerSimilarity(string source, string target)
        {
            int m = MatchingCharacters(source, target);
            if (m == 0) return 0;

            double jaro = (1.0 / 3) * (
                (double)m / source.Length +
                (double)m / target.Length +
                (double)(m - Transpositions(source, target)) / m);

            int prefixLength = CommonPrefixLength(source, target);
            return jaro + 0.1 * prefixLength * (1 - jaro);
        }

        private static int MatchingCharacters(string source, string target)
        {
            int matchingWindow = Math.Max(source.Length, target.Length) / 2 - 1;
            var sourceMatched = new bool[source.Length];
            var targetMatched = new bool[target.Length];
            int matches = 0;

            Parallel.For(0, source.Length, i =>
            {
                for (int j = Math.Max(0, i - matchingWindow); j < Math.Min(target.Length, i + matchingWindow + 1); j++)
                {
                    if (!targetMatched[j] && source[i] == target[j])
                    {
                        sourceMatched[i] = true;
                        targetMatched[j] = true;
                        matches++;
                        break;
                    }
                }
            });

            return matches;
        }

        private static int Transpositions(string source, string target)
        {
            int matches = MatchingCharacters(source, target);
            if (matches == 0) return 0;

            int k = 0, transpositions = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == target[k]) k++;
                else transpositions++;
            }

            return transpositions / 2;
        }

        private static int CommonPrefixLength(string source, string target)
        {
            int maxPrefixLength = Math.Min(4, Math.Min(source.Length, target.Length));
            int prefixLength = 0;

            for (int i = 0; i < maxPrefixLength; i++)
            {
                if (source[i] == target[i]) prefixLength++;
                else break;
            }

            return prefixLength;
        }

        private static double CosineSimilarity(string source, string target)
        {
            var sourceBigrams = GetBigrams(source);
            var targetBigrams = GetBigrams(target);

            var allBigrams = sourceBigrams.Keys.Union(targetBigrams.Keys);
            double dotProduct = allBigrams.AsParallel().Sum(bigram => sourceBigrams.GetValueOrDefault(bigram, 0) * targetBigrams.GetValueOrDefault(bigram, 0));
            double magnitudeSource = Math.Sqrt(sourceBigrams.Values.AsParallel().Sum(v => v * v));
            double magnitudeTarget = Math.Sqrt(targetBigrams.Values.AsParallel().Sum(v => v * v));

            return magnitudeSource == 0 || magnitudeTarget == 0 ? 0 : dotProduct / (magnitudeSource * magnitudeTarget);
        }

        private static Dictionary<string, int> GetBigrams(string text)
        {
            var bigrams = new ConcurrentDictionary<string, int>();

            Parallel.For(0, text.Length - 1, i =>
            {
                string bigram = text.Substring(i, 2);
                bigrams.AddOrUpdate(bigram, 1, (_, count) => count + 1);
            });

            return bigrams.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private static double TrigramOverlapSimilarity(string source, string target)
        {
            var sourceTrigrams = GetTrigrams(source);
            var targetTrigrams = GetTrigrams(target);

            var intersection = sourceTrigrams.AsParallel().Intersect(targetTrigrams).Count();
            var union = sourceTrigrams.AsParallel().Union(targetTrigrams).Count();

            return union == 0 ? 0 : (double)intersection / union;
        }

        private static HashSet<string> GetTrigrams(string text)
        {
            var trigrams = new ConcurrentBag<string>();

            Parallel.For(0, text.Length - 2, i =>
            {
                trigrams.Add(text.Substring(i, 3));
            });

            return new HashSet<string>(trigrams);
        }

        private static double SimHashSimilarity(string source, string target)
        {
            long sourceHash = SimHash(source);
            long targetHash = SimHash(target);

            int differingBits = HammingWeight(sourceHash ^ targetHash);
            return 1 - (double)differingBits / 64;
        }

        private static long SimHash(string text)
        {
            int[] bitVector = new int[64];
            Parallel.ForEach(GetBigrams(text), bigram =>
            {
                long hash = Hash(bigram.Key);
                for (int i = 0; i < 64; i++)
                {
                    Interlocked.Add(ref bitVector[i], (hash & (1L << i)) != 0 ? 1 : -1);
                }
            });

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
            return text.Aggregate(0L, (hash, c) => hash * 31 + c);
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

        private static double MinHashSimilarity(string source, string target)
        {
            var sourceShingles = GetShingles(source, 3);
            var targetShingles = GetShingles(target, 3);

            var sourceMinHash = MinHash(sourceShingles);
            var targetMinHash = MinHash(targetShingles);

            return sourceMinHash == targetMinHash ? 1 : 0;
        }

        private static HashSet<string> GetShingles(string text, int length)
        {
            var shingles = new ConcurrentBag<string>();

            Parallel.For(0, text.Length - length + 1, i =>
            {
                shingles.Add(text.Substring(i, length));
            });

            return new HashSet<string>(shingles);
        }

        private static int MinHash(HashSet<string> shingles)
        {
            return shingles.AsParallel().Min(shingle => shingle.GetHashCode());
        }
    }
}
