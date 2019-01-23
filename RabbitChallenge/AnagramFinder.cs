using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RabbitChallenge
{
    internal static class AnagramFinder
    {
        private static readonly byte[] WordSeparatorBytes = Encoding.ASCII.GetBytes(" ");

        /// <summary>
        ///     Tries to find matching phrases to meet a provided <see cref="anagramFilter" />.
        /// </summary>
        /// <param name="sanitizedAnagramWordPairs">A list of all possible words.</param>
        /// <param name="anagramFilter">A <see cref="CharacterDistribution" /> containing characters to filter with.</param>
        /// <param name="maximumNumberOfWords">Maximum number of words in a phrase.</param>
        /// <param name="numberOfTasks">Number of tasks to spawn and search with.</param>
        /// <returns>An array of <see cref="T:byte[]" /> representing a phrase.</returns>
        // ReSharper disable once TooManyArguments
        // ReSharper disable once ExcessiveIndentation
        // ReSharper disable once TooManyDeclarations
        public static ParallelQuery<byte[]> GetMatchedPhrases(
            this Dictionary<CharacterDistribution, byte[][]> sanitizedAnagramWordPairs,
            CharacterDistribution anagramFilter,
            int maximumNumberOfWords,
            int numberOfTasks
        )
        {
            // Create a second array containing only character distributions
            var filteredDistributions = sanitizedAnagramWordPairs.Keys.Where(anagramFilter.CanContain).ToArray();
            var distributionsHashTable = new GenericHashTable<CharacterDistribution>(filteredDistributions, null);

            // Initialize characters combinations query with PLINQ
            var anagramCombinations = filteredDistributions
                .AsParallel()
                .WithDegreeOfParallelism(numberOfTasks)
                .SelectMany(distribution =>
                    GetDistributionCombinations(
                        filteredDistributions,
                        distributionsHashTable,
                        distribution,
                        maximumNumberOfWords,
                        anagramFilter - distribution
                    )
                );

            // Continue the characters combination query by extracting every 
            // possible word combination associated with a character distribution
            var wordCombinations = anagramCombinations.SelectMany(anagramCombination =>
                anagramCombination
                    .Where(distribution => !distribution.IsEmpty())
                    .Select(distribution => sanitizedAnagramWordPairs[distribution])
                    .GetCartesianProduct()
            );

            // Create binary representations of phrases from the word combinations
            return wordCombinations.Select(wordCombination =>
                wordCombination
                    .Aggregate(
                        new byte[0],
                        (before, item) => before.Concat(
                            before.Length == 0 ? item : WordSeparatorBytes.Concat(item)
                        ).ToArray()
                    )
            );
        }


        /// <summary>
        ///     Sanitizes, sort and group similar words, then loads them all into memory
        /// </summary>
        /// <param name="enumerable">The string sequence to sanitize</param>
        /// <returns>
        ///     A sanitized dictionary of <see cref="CharacterDistribution" />s along with their corresponding words byte
        ///     arrays
        /// </returns>
        // ReSharper disable once TooManyDeclarations
        public static Dictionary<CharacterDistribution, byte[][]> SanitizeWordDictionary(
            this IEnumerable<string> enumerable
        )
        {
            return enumerable
                .Select(str => str.ToLower().Trim())
                .Distinct()
                .GroupBy(CharacterDistribution.FromString)
                .Where(grouping => grouping.Key.IsValid())
                .OrderByDescending(grouping => grouping.Key.Rank)
                .ToDictionary(
                    grouping => grouping.Key,
                    grouping => grouping.Select(s => Encoding.ASCII.GetBytes(s)).ToArray()
                );
        }

        /// <summary>
        ///     Performs a cross join; code is based on this:
        ///     https://ericlippert.com/2010/06/28/computing-a-cartesian-product-with-linq/
        /// </summary>
        /// <typeparam name="T">The type of inner array.</typeparam>
        /// <param name="sequences">The array of arrays.</param>
        /// <returns>An array of all possible combination of passed array items.</returns>
        // ReSharper disable once TooManyDeclarations
        private static IEnumerable<IEnumerable<T>> GetCartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
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

        /// <summary>
        ///     Returns valid combinations of <see cref="CharacterDistribution" />s that fits the filter provided.
        /// </summary>
        /// <param name="filteredDistributions">
        ///     A list of all possible <see cref="CharacterDistribution" />s; should be already
        ///     sanitized and filtered.
        /// </param>
        /// <param name="distributionsHashTable">Unfiltered hash table of all possible <see cref="CharacterDistribution" />s.</param>
        /// <param name="firstWordDistribution">The initial word to continue the search with.</param>
        /// <param name="maxPhraseLength">The maximum possible length of phrases.</param>
        /// <param name="filter">The filter to be applied to child searches.</param>
        /// <returns>An array of <see cref="CharacterDistribution" />s representing a phrase.</returns>
        // ReSharper disable once TooManyArguments
        private static IEnumerable<CharacterDistribution[]> GetDistributionCombinations(
            CharacterDistribution[] filteredDistributions,
            GenericHashTable<CharacterDistribution> distributionsHashTable,
            CharacterDistribution firstWordDistribution,
            int maxPhraseLength,
            CharacterDistribution filter
        )
        {
            // Create an array with the maximum possible size, so we don't need to resize it later
            var words = new CharacterDistribution[maxPhraseLength];
            words[0] = firstWordDistribution;

            // If filter is already empty, then this word is enough
            if (filter.IsEmpty())
            {
                yield return words;

                yield break;
            }

            // If maximum possible length of phrases is one, then this is the only word we had to check for
            if (maxPhraseLength == 1 || !filter.IsValid())
            {
                yield break;
            }

            // If only one word is missing, try finding it based on the filter directly
            if (maxPhraseLength == 2)
            {
                if (distributionsHashTable.Contains(filter))
                {
                    words[1] = filter;

                    yield return words;
                }

                yield break;
            }

            // Otherwise, go through all sub combinations
            var subCombinations = GetDistributionSubCombinations(
                filteredDistributions.Where(filter.CanContain).ToArray(),
                distributionsHashTable,
                words,
                1,
                maxPhraseLength,
                filter
            );

            // And yield if anything matched
            foreach (var combination in subCombinations)
            {
                yield return combination;
            }
        }

        /// <summary>
        ///     Returns valid sub combinations of <see cref="CharacterDistribution" />s that fits the filter provided.
        ///     The difference between this method and
        ///     <see
        ///         cref="GetDistributionCombinations" />
        ///     is that this  method wont check to see if the currently passed <see cref="CharacterDistribution" />
        ///     meets the passed filter.
        /// </summary>
        /// <param name="filteredDistributions">
        ///     A list of all possible <see cref="CharacterDistribution" />s. Should be already
        ///     filtered.
        /// </param>
        /// <param name="distributionsHashTable">Unfiltered hash table of all possible <see cref="CharacterDistribution" />s.</param>
        /// <param name="phraseDistributions">An array of previous <see cref="CharacterDistribution" />s.</param>
        /// <param name="currentPhraseLength">
        ///     The number of <see cref="CharacterDistribution" />s in the
        ///     <see cref="phraseDistributions" /> argument.
        /// </param>
        /// <param name="maxPhraseLength">The maximum possible length of phrases.</param>
        /// <param name="filter">The filter to be applied to child searches.</param>
        /// <returns>An array of <see cref="CharacterDistribution" />s representing a phrase.</returns>
        // ReSharper disable once TooManyArguments
        private static IEnumerable<CharacterDistribution[]> GetDistributionSubCombinations(
            CharacterDistribution[] filteredDistributions,
            GenericHashTable<CharacterDistribution> distributionsHashTable,
            CharacterDistribution[] phraseDistributions,
            int currentPhraseLength,
            int maxPhraseLength,
            CharacterDistribution filter
        )
        {
            // Trying to find the next word
            foreach (var word in filteredDistributions)
            {
                var newWords = new CharacterDistribution[maxPhraseLength];
                Array.Copy(phraseDistributions, newWords, currentPhraseLength);
                newWords[currentPhraseLength] = word;

                // Try to proactively decide if this word is going to be enough
                if (word.Rank == filter.Rank)
                {
                    yield return newWords;

                    continue;
                }

                // This was the last word, no need to go deeper
                if (currentPhraseLength == maxPhraseLength - 1)
                {
                    continue;
                }

                // Calculate the required characters after this one
                var newFilter = filter - word;

                // If we still missing some characters; but so little that
                // it doesn't make sense to continue; ignore this word
                if (!newFilter.IsValid())
                {
                    continue;
                }

                // If only one word is missing, try finding it based on the filter directly
                if (currentPhraseLength == maxPhraseLength - 2)
                {
                    if (distributionsHashTable.Contains(newFilter))
                    {
                        newWords[currentPhraseLength + 1] = newFilter;

                        yield return newWords;
                    }

                    continue;
                }

                var subCombinations = GetDistributionSubCombinations(
                    filteredDistributions.Where(newFilter.CanContain).ToArray(),
                    distributionsHashTable,
                    newWords,
                    currentPhraseLength + 1,
                    maxPhraseLength,
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