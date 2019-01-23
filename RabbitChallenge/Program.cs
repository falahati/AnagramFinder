using System;
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
        private static void DoProcess(Options options)
        {
            // Initializing variables
            var totalTestedPhrases = 0d;
            var testedPhrasesSinceLastUpdate = 0d;
            var dictionaryWords = File.ReadAllLines(options.WordDictionaryPath);
            var hashBytes = options.Hashes.Select(t => t.Item2).ToArray();
            var md5 = MD5.Create();

            // Starting the real job of finding matching phrases
            var updateStopWatch = Stopwatch.StartNew();
            var mainStopWatch = Stopwatch.StartNew();

            var matchedPhrases =
                dictionaryWords.GetMatchedPhrases(
                    options.AnagramFilter,
                    options.MaximumNumberOfWords,
                    options.NumberOfTasks
                );

            foreach (var matchedPhrase in matchedPhrases)
            {
                // Found a phrase, check MD5
                var phraseHash = md5.ComputeHash(matchedPhrase);
                var matchedHash = hashBytes.FirstOrDefault(hash => hash.SequenceEqual(phraseHash));

                if (matchedHash != null)
                {
                    // Print the newly found and validated phrase
                    var matchedHashIndex = Array.IndexOf(hashBytes, matchedHash);
                    var hashFoundMessage = string.Format(
                        "-- Elapsed: {0:F2}s - Phrase: '{1}' - Matched Hash: [#{2:D}] {{{3}}} ",
                        mainStopWatch.Elapsed.TotalSeconds,
                        Encoding.ASCII.GetString(matchedPhrase),
                        matchedHashIndex,
                        options.Hashes[matchedHashIndex].Item1
                    );

                    if (!options.NoReport)
                    {
                        Console.CursorLeft = 0;
                    }

                    Console.WriteLine(
                        hashFoundMessage +
                        new string(' ', Math.Max(1, Console.BufferWidth - hashFoundMessage.Length) - 1)
                    );
                }

                // Update after each minute - more or less
                if (!options.NoReport && updateStopWatch.ElapsedMilliseconds > 1000)
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

            totalTestedPhrases += testedPhrasesSinceLastUpdate;

            // Print summery
            var summeryMessage = string.Format(
                "-- Elapsed: {0:F2}s - Phrases: {1:N0} - Average: {2:N0} H/s",
                mainStopWatch.Elapsed.TotalSeconds,
                totalTestedPhrases,
                totalTestedPhrases / mainStopWatch.Elapsed.TotalSeconds
            );

            if (!options.NoReport)
            {
                Console.CursorLeft = 0;
            }

            Console.WriteLine(
                summeryMessage + new string(' ', Math.Max(1, Console.BufferWidth - summeryMessage.Length) - 1)
            );

            if (!options.NoReport)
            {
                Console.WriteLine("-- End of program execution.");
                Console.ReadLine();
            }
        }

        private static Options GetOptions(string[] arguments)
        {
            // Loading options
            Options options;

            try
            {
                options = Options.FromArguments(arguments);
            }
            catch (Exception e)
            {
                // Print Help
                var assemblyPath = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
                Console.WriteLine();
                Console.WriteLine(e.Message);
                Console.WriteLine();
                Console.WriteLine(
                    $"dotnet \"{assemblyPath}\" {{NumberOfWords}} {{NumberOfTasks}} {{\"DictionaryFilePath\"}} {{\"Anagram}}\"}} {{\"MD5Hash\"}} [\"MD5Hash\"] [\"MD5Hash\"] [...] [NoReport]"
                );
                Console.WriteLine();

                return null;
            }

            if (!options.NoReport)
            {
                // Printing current options
                Console.WriteLine("Anagram: {0}", options.AnagramFilter);
                Console.WriteLine("Word Dictionary: {0}", options.WordDictionaryPath);
                Console.WriteLine("Max Number of Words: {0}", options.MaximumNumberOfWords);
                Console.WriteLine("MD5 Hashes: {0}", string.Join(", ", options.Hashes.Select(t => t.Item1)));
                Console.WriteLine("Number of Tasks: {0}", options.NumberOfTasks);
            }

            return options;
        }

        private static void Main(string[] args)
        {
            var options = GetOptions(args);

            if (options == null)
            {
                return;
            }

            if (!options.NoReport)
            {
                // Print a separator
                Console.WriteLine(new string('-', Console.BufferWidth - 1));
            }

            try
            {
                DoProcess(options);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}