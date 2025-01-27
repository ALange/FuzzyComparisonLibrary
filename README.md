# FuzzyComparisonLibrary

## Overview

`FuzzyComparisonLibrary` is a C# library for calculating the similarity between two strings using multiple fuzzy comparison algorithms. The library supports multithreaded processing to enhance performance, making it efficient for applications requiring quick and reliable string similarity calculations.

### Supported Algorithms

- Levenshtein Similarity
- Jaro-Winkler Similarity
- Cosine Similarity
- Trigram Overlap Similarity
- SimHash Similarity
- MinHash Similarity

## Author

Adam Lange [code@adamlange.pl](mailto\:code@adamlange.pl)

## Source Code

[FuzzyComparisonLibrary on GitHub](https://github.com/ALange/FuzzyComparisonLibrary)

## Installation

Include the `FuzzyComparisonLibrary` in your project by adding the source code or compiling it into a DLL.

## Usage

### Example 1: Unified Similarity Calculation

```csharp
using System;
using FuzzyComparisonLibrary;

class Program
{
    static void Main(string[] args)
    {
        string source = "hello world";
        string target = "hola world";

        double similarity = FuzzyComparer.GetUnifiedSimilarity(source, target);
        Console.WriteLine($"Unified Similarity: {similarity:P2}");
    }
}
```

### Example 2: Specific Algorithm Calculation

```csharp
using System;
using FuzzyComparisonLibrary;

class Program
{
    static void Main(string[] args)
    {
        string source = "hello";
        string target = "hallo";

        double levenshteinSimilarity = FuzzyComparer.GetSimilarityByMethod(source, target, FuzzyComparer.LevenshteinSimilarity);
        Console.WriteLine($"Levenshtein Similarity: {levenshteinSimilarity:P2}");

        double jaroWinklerSimilarity = FuzzyComparer.GetSimilarityByMethod(source, target, FuzzyComparer.JaroWinklerSimilarity);
        Console.WriteLine($"Jaro-Winkler Similarity: {jaroWinklerSimilarity:P2}");

        double cosineSimilarity = FuzzyComparer.GetSimilarityByMethod(source, target, FuzzyComparer.CosineSimilarity);
        Console.WriteLine($"Cosine Similarity: {cosineSimilarity:P2}");

        double trigramOverlapSimilarity = FuzzyComparer.GetSimilarityByMethod(source, target, FuzzyComparer.TrigramOverlapSimilarity);
        Console.WriteLine($"Trigram Overlap Similarity: {trigramOverlapSimilarity:P2}");

        double simHashSimilarity = FuzzyComparer.GetSimilarityByMethod(source, target, FuzzyComparer.SimHashSimilarity);
        Console.WriteLine($"SimHash Similarity: {simHashSimilarity:P2}");

        double minHashSimilarity = FuzzyComparer.GetSimilarityByMethod(source, target, FuzzyComparer.MinHashSimilarity);
        Console.WriteLine($"MinHash Similarity: {minHashSimilarity:P2}");
    }
}
```

### Available Methods

#### `GetUnifiedSimilarity`

Calculates a unified similarity score by averaging the results of all supported algorithms.

```csharp
double similarity = FuzzyComparer.GetUnifiedSimilarity(source, target);
```

#### `GetSimilarityByMethod`

Allows computation of similarity using a specific algorithm by passing the desired method.

```csharp
double similarity = FuzzyComparer.GetSimilarityByMethod(source, target, FuzzyComparer.LevenshteinSimilarity);
```

#### `LevenshteinSimilarity`

Calculates similarity based on Levenshtein (edit distance).

```csharp
double similarity = FuzzyComparer.LevenshteinSimilarity(source, target);
```

#### `JaroWinklerSimilarity`

Calculates similarity using the Jaro-Winkler algorithm.

```csharp
double similarity = FuzzyComparer.JaroWinklerSimilarity(source, target);
```

#### `CosineSimilarity`

Calculates similarity based on cosine similarity of character bigrams.

```csharp
double similarity = FuzzyComparer.CosineSimilarity(source, target);
```

#### `TrigramOverlapSimilarity`

Calculates similarity based on the overlap of character trigrams.

```csharp
double similarity = FuzzyComparer.TrigramOverlapSimilarity(source, target);
```

#### `SimHashSimilarity`

Calculates similarity using the SimHash algorithm.

```csharp
double similarity = FuzzyComparer.SimHashSimilarity(source, target);
```

#### `MinHashSimilarity`

Calculates similarity using the MinHash algorithm for shingles.

```csharp
double similarity = FuzzyComparer.MinHashSimilarity(source, target);
```

## Features

- Multithreaded implementation for improved performance.
- Flexible API to compute unified or individual similarity scores.
- Support for various string similarity algorithms.

## License

MIT License

Copyright (c) 2025 Adam Lange [code@adamlange.pl](mailto\:code@adamlange.pl)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

For more information, contact Adam Lange at [code@adamlange.pl](mailto\:code@adamlange.pl).

