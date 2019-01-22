using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RabbitChallenge
{
    // ReSharper disable once HollowTypeName
    internal static class Helper
    {
        /// <summary>
        ///     Based on this:
        ///     https://ericlippert.com/2010/06/28/computing-a-cartesian-product-with-linq/
        /// </summary>
        /// <typeparam name="T">The type of inner array.</typeparam>
        /// <param name="sequences">The array of arrays.</param>
        /// <returns>An array of all possible combination of passed array items.</returns>
        // ReSharper disable once TooManyDeclarations
        public static IEnumerable<IEnumerable<T>> GetCartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            return sequences?.Aggregate(
                new[] {Enumerable.Empty<T>()} as IEnumerable<IEnumerable<T>>,
                (accumulatedSequences, sequence) =>
                    accumulatedSequences.SelectMany(
                        accumulatedSequence => sequence,
                        (accumulatedSequence, item) => accumulatedSequence.Concat(new[] {item})
                    )
            );
        }

        // ReSharper disable once TooManyArguments
        // ReSharper disable once TooManyDeclarations
        public static IEnumerable<CharacterDistribution[]> GetDistributionCombinations(
            this IEnumerable<CharacterDistribution> dictionaryWords,
            CharacterDistribution filter,
            int maximumNumberOfWords,
            int numberOfThreads
        )
        {
            if (maximumNumberOfWords == 1)
            {
                // Initializing the query required to find the only 
                // character combination that meets the anagram filter
                return dictionaryWords.Select(distribution => new[] {distribution});
            }

            var dictionaryWordsArray = dictionaryWords.ToArray();

            // Initializing the query required to find the combinations 
            // of characters that meets the anagram filter
            return dictionaryWordsArray
                .AsParallel()
                .WithDegreeOfParallelism(numberOfThreads)
                .SelectMany(distribution =>
                {
                    var characterDistributions = new CharacterDistribution[maximumNumberOfWords];
                    characterDistributions[0] = distribution;

                    var newFilter = filter - distribution;

                    return GetDistributionCombinations(
                        dictionaryWordsArray.Where(newFilter.CanContain).ToArray(),
                        characterDistributions,
                        1,
                        newFilter
                    );
                });
        }

        // ReSharper disable once TooManyDeclarations
        public static Dictionary<CharacterDistribution, byte[][]> SanitizeWordDictionary(
            this IEnumerable<string> enumerable,
            CharacterDistribution filter
        )
        {
            return enumerable
                .Select(str => str.ToLower().Trim())
                .Distinct()
                .Where(str => (str.Length > 1 || str == "a" || str == "i" || str == "o") && str.Length <= filter.Rank)
                .GroupBy(CharacterDistribution.FromString)
                .Where(grouping => grouping.Key.Rank > 0 && filter.CanContain(grouping.Key))
                .OrderByDescending(grouping => grouping.Key.Rank)
                .ToDictionary(
                    grouping => grouping.Key,
                    grouping => grouping.Select(s => Encoding.ASCII.GetBytes(s)).ToArray()
                );
        }

        // ReSharper disable once TooManyArguments
        private static IEnumerable<CharacterDistribution[]> GetDistributionCombinations(
            CharacterDistribution[] validCharacterDistributions,
            CharacterDistribution[] currentDistribution,
            int currentLength,
            CharacterDistribution filter
        )
        {
            foreach (var word in validCharacterDistributions)
            {
                var newWords = new CharacterDistribution[currentDistribution.Length];
                Array.Copy(currentDistribution, newWords, currentLength);
                newWords[currentLength] = word;

                if (word.Rank == filter.Rank)
                {
                    yield return newWords;
                }

                if (currentLength == currentDistribution.Length - 1)
                {
                    yield break;
                }

                var newFilter = filter - word;

                var subCombinations = GetDistributionCombinations(
                    validCharacterDistributions.Where(newFilter.CanContain).ToArray(),
                    newWords,
                    currentLength + 1,
                    newFilter
                );

                foreach (var combination in subCombinations)
                {
                    yield return combination;
                }
            }
        }
    }
}