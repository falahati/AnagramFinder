using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace RabbitChallenge
{
    internal class Program
    {
        private static readonly byte[] WordSeparatorBytes = Encoding.ASCII.GetBytes(" ");

        // ReSharper disable once MethodTooLong
        // ReSharper disable once TooManyArguments
        // ReSharper disable once ExcessiveIndentation
        private static IEnumerable<byte[]> GetMatchedPhrases(
            IEnumerable<string> dictionaryWords,
            CharacterDistribution anagramFilter,
            int maximumNumberOfWords,
            int numberOfThreads
        )
        {
            // Loading dictionary to memory
            var sanitizedAnagramWordPairs = dictionaryWords
                .AsParallel()
                .WithDegreeOfParallelism(numberOfThreads)
                .SanitizeWordDictionary(anagramFilter);

            // Initialize characters combinations query
            var anagramCombinations = sanitizedAnagramWordPairs.Keys
                .ToArray()
                .GetDistributionCombinations(
                    anagramFilter,
                    maximumNumberOfWords,
                    numberOfThreads
                );

            // Go over each characters combination
            foreach (var anagramCombination in anagramCombinations)
            {
                // Finding words that meet each characters combination
                var wordCombinations = anagramCombination
                    .Where(dis => dis.Rank > 0)
                    .Select(distribution => sanitizedAnagramWordPairs[distribution])
                    .GetCartesianProduct().ToArray();
                
                // Create an array of bytes for each word combinations and yield
                foreach (var wordCombination in wordCombinations)
                {
                    IEnumerable<byte> bytes = new byte[0];
                    var isEmpty = true;

                    foreach (var word in wordCombination)
                    {
                        if (!isEmpty)
                        {
                            bytes = bytes.Concat(WordSeparatorBytes);
                        }
                        else
                        {
                            isEmpty = false;
                        }

                        bytes = bytes.Concat(word);
                    }

                    yield return bytes.ToArray();
                }
            }
        }

        private static void Main(string[] args)
        {
            // Loading options
            Options options;

            try
            {
                options = Options.FromArguments(args);
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(e.Message);
                Console.WriteLine();
                Console.WriteLine(
                    "dotnet \"" +
                    Path.GetFileName(Assembly.GetExecutingAssembly().Location) + "\" " +
                    "{NumberOfWords} {NumberOfThreads} {\"DictionaryFilePath\"} {\"Anagram}\"} {\"MD5Hash\"} [\"MD5Hash\"] [\"MD5Hash\"] [...]"
                );
                Console.WriteLine();
                return;
            }

            Console.WriteLine("Anagram: {0}", options.AnagramFilter);
            Console.WriteLine("Word Dictionary: {0}", options.WordDictionaryPath);
            Console.WriteLine("Max Number of Words: {0}", options.MaximumNumberOfWords);
            Console.WriteLine("MD5 Hashes: {0}", string.Join(", ", options.Hashes.Select(t => t.Item1)));
            Console.WriteLine("Number of Threads: {0}", options.NumberOfThreads);

            // Separator
            Console.WriteLine(new string('-', Console.BufferWidth - 1));

            // Initializing
            var totalTestedPhrases = 0d;
            var testedPhrasesSinceLastUpdate = 0ul;
            var dictionaryWords = File.ReadAllLines(options.WordDictionaryPath);
            var hashBytes = options.Hashes.Select(t => t.Item2).ToArray();
            var md5 = MD5.Create();

            // Start processing
            var updateStopWatch = Stopwatch.StartNew();
            var mainStopWatch = Stopwatch.StartNew();

            var matchedPhrases = GetMatchedPhrases(
                dictionaryWords,
                options.AnagramFilter,
                options.MaximumNumberOfWords,
                options.NumberOfThreads
            );

            foreach (var matchedPhrase in matchedPhrases)
            {
                var phraseHash = md5.ComputeHash(matchedPhrase);
                var matchedHash = hashBytes.FirstOrDefault(hash => hash.SequenceEqual(phraseHash));

                if (matchedHash != null)
                {
                    var matchedHashIndex = Array.IndexOf(hashBytes, matchedHash);
                    var hashFoundMessage = string.Format(
                        "-- Elapsed: {0:F2}s - Phrase: '{1}' - Matched Hash: [#{2:D}] {{{3}}} ",
                        mainStopWatch.Elapsed.TotalSeconds,
                        Encoding.ASCII.GetString(matchedPhrase),
                        matchedHashIndex,
                        options.Hashes[matchedHashIndex].Item1
                    );
                    Console.CursorLeft = 0;
                    Console.WriteLine(
                        hashFoundMessage +
                        new string(' ', Math.Max(1, Console.BufferWidth - hashFoundMessage.Length) - 1)
                    );
                }

                if (updateStopWatch.ElapsedMilliseconds >= 1000)
                {
                    totalTestedPhrases += testedPhrasesSinceLastUpdate;
                    var reportMessage = string.Format(
                        "-- Elapsed: {0:F2}s - Phrases: {1:N0} - Average: {2:N0} H/s - Current: {3:N0} H/s",
                        mainStopWatch.Elapsed.TotalSeconds,
                        totalTestedPhrases,
                        totalTestedPhrases / mainStopWatch.Elapsed.TotalSeconds,
                        testedPhrasesSinceLastUpdate / updateStopWatch.Elapsed.TotalSeconds
                    );
                    updateStopWatch.Restart();
                    testedPhrasesSinceLastUpdate = 0;

                    Console.CursorLeft = 0;
                    Console.Write(
                        reportMessage + new string(' ', Math.Max(1, Console.BufferWidth - reportMessage.Length) - 1)
                    );
                }

                testedPhrasesSinceLastUpdate++;
            }

            mainStopWatch.Stop();
            md5.Dispose();

            var summeryMessage = string.Format(
                "-- Elapsed: {0:F2}s - Phrases: {1:N0} - Average: {2:N0} H/s",
                mainStopWatch.Elapsed.TotalSeconds,
                totalTestedPhrases,
                totalTestedPhrases / mainStopWatch.Elapsed.TotalSeconds
            );

            Console.CursorLeft = 0;
            Console.WriteLine(
                summeryMessage + new string(' ', Math.Max(1, Console.BufferWidth - summeryMessage.Length) - 1)
            );
            Console.WriteLine("-- End of execution. Press 'Enter' to exit.");
            Console.ReadLine();
        }
    }
}