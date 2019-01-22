using System;
using System.IO;
using System.Linq;

namespace RabbitChallenge
{
    public class Options
    {
        // ReSharper disable once TooManyDependencies
        private Options(
            int maximumNumberOfWords,
            int numberOfThreads,
            string wordDictionaryPath,
            CharacterDistribution anagramFilter,
            Tuple<string, byte[]>[] hashes)
        {
            MaximumNumberOfWords = maximumNumberOfWords;
            NumberOfThreads = numberOfThreads;
            WordDictionaryPath = wordDictionaryPath;
            AnagramFilter = anagramFilter;
            Hashes = hashes;
        }

        public CharacterDistribution AnagramFilter { get; }
        public Tuple<string, byte[]>[] Hashes { get; }
        public int MaximumNumberOfWords { get; }
        public int NumberOfThreads { get; }
        public string WordDictionaryPath { get; }

        public static Options FromArguments(string[] arguments)
        {
            if (arguments.Length < 1 || !int.TryParse(arguments[0], out var maxNumberOfWords) || maxNumberOfWords <= 0)
            {
                throw new ArgumentException("Invalid maximum number of words.");
            }

            if (arguments.Length < 2 || !int.TryParse(arguments[1], out var numberOfThreads) || numberOfThreads <= 0)
            {
                throw new ArgumentException("Invalid number of threads.");
            }

            if (arguments.Length < 3 || string.IsNullOrWhiteSpace(arguments[2]) || !File.Exists(arguments[2].Trim()))
            {
                throw new ArgumentException("Missing dictionary file.");
            }

            var wordDictionaryPath = Path.GetFullPath(arguments[2].Trim());

            if (arguments.Length < 4 ||
                string.IsNullOrWhiteSpace(arguments[3]) ||
                !arguments[3].Trim().All(c => c >= 'a' || c <= 'z' || c == ' '))
            {
                throw new ArgumentException("Invalid anagram string.");
            }

            var anagramFilter = CharacterDistribution.FromString(new string(
                arguments[3]
                    .Trim()
                    .Where(c => c >= 'a' && c <= 'z')
                    .ToArray()
            ));

            var hashStrings = arguments
                .Skip(4)
                .Select(s => s.ToLower().Trim())
                .ToArray();

            if (arguments.Length < 5 || hashStrings.Any(s => s.Length != 32 || s.Any(c => c < '0' || c > 'f')))
            {
                throw new ArgumentException("Invalid or missing MD5 hash.");
            }

            return new Options(
                maxNumberOfWords,
                numberOfThreads,
                wordDictionaryPath,
                anagramFilter,
                hashStrings.Select(
                        hash => Tuple.Create(
                            hash,
                            Enumerable.Range(0, hash.Length)
                                .Where(x => x % 2 == 0)
                                .Select(x => Convert.ToByte(hash.Substring(x, 2), 16))
                                .ToArray())
                    )
                    .ToArray()
            );
        }
    }
}