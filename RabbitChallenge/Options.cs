using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RabbitChallenge
{
    internal class Options
    {
        // ReSharper disable once TooManyDependencies
        private Options(
            int maximumNumberOfWords,
            int numberOfTasks,
            string wordDictionaryPath,
            CharacterDistribution anagramFilter,
            Dictionary<uint[], string> hashes,
            bool silence)
        {
            MaximumNumberOfWords = maximumNumberOfWords;
            NumberOfTasks = numberOfTasks;
            WordDictionaryPath = wordDictionaryPath;
            AnagramFilter = anagramFilter;
            Hashes = hashes;
            Silence = silence;
        }

        public CharacterDistribution AnagramFilter { get; }
        public Dictionary<uint[], string> Hashes { get; }
        public int MaximumNumberOfWords { get; }
        public int NumberOfTasks { get; }
        public bool Silence { get; }
        public string WordDictionaryPath { get; }

        public static Options FromArguments(string[] arguments)
        {
            if (arguments.Length < 1 || !int.TryParse(arguments[0], out var maxNumberOfWords) || maxNumberOfWords <= 0)
            {
                throw new ArgumentException("Invalid maximum number of words.");
            }

            if (arguments.Length < 2 || !int.TryParse(arguments[1], out var numberOfTasks) || numberOfTasks <= 0)
            {
                throw new ArgumentException("Invalid number of tasks.");
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

            var silence =
                arguments.LastOrDefault()?.Equals("Silence", StringComparison.InvariantCultureIgnoreCase) == true;

            var hashStrings =
                (silence ? arguments.Skip(4).Take(arguments.Length - 5) : arguments.Skip(4))
                .Select(s => s.ToLower().Trim())
                .ToArray();

            if (arguments.Length < 5 || hashStrings.Any(s => s.Length != 32 || s.Any(c => c < '0' || c > 'f')))
            {
                throw new ArgumentException("Invalid or missing MD5 hash.");
            }

            return new Options(
                maxNumberOfWords,
                numberOfTasks,
                wordDictionaryPath,
                anagramFilter,
                hashStrings.ToDictionary(GetMD5Bytes, hash => hash),
                silence || Console.IsOutputRedirected
            );
        }

        // ReSharper disable once TooManyDeclarations
        // ReSharper disable once InconsistentNaming
        private static uint[] GetMD5Bytes(string hash)
        {
            return hash
                .Select((c, i) => Tuple.Create(i - i % 2, c))
                .GroupBy(t => t.Item1)
                .Select(
                    ts => Convert.ToByte(new string(ts.Select(t => t.Item2).ToArray()), 16)
                )
                .Select((b, i) => Tuple.Create(i - i % 4, b))
                .GroupBy(t => t.Item1)
                .Select(
                    ts => BitConverter.ToUInt32(ts.Select(t => t.Item2).ToArray())
                )
                .ToArray();
        }
    }
}