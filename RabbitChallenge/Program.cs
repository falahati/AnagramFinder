using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace RabbitChallenge
{
    internal class Program
    {
        private static readonly object ConsoleLock = new object();

        private static void DoProcess(Options options)
        {
            // Initializing and defining required variables
            var totalTestedPhrases = 0ul;
            var lastUpdatedPhrases = 0ul;
            var mainStopWatch = Stopwatch.StartNew();
            var updateStopWatch = new Stopwatch();
            var hashBytes = options.Hashes.Select(pair => pair.Key).ToArray();

            // If we are not silence, start the reporting timer
            var reportingTimer = options.Silence
                ? null
                : new Timer(state =>
                {
                    var reportMessage = string.Format(
                        "-- Elapsed: {0:F2}s - Phrases: {1:N0} - Average: {2:N0} H/s - Current: {3:N0} H/s",
                        mainStopWatch.Elapsed.TotalSeconds,
                        totalTestedPhrases,
                        totalTestedPhrases / mainStopWatch.Elapsed.TotalSeconds,
                        (totalTestedPhrases - lastUpdatedPhrases) / updateStopWatch.Elapsed.TotalSeconds
                    );

                    updateStopWatch.Restart();
                    lastUpdatedPhrases = totalTestedPhrases;

                    WriteConsole(reportMessage, true, true);
                }, null, TimeSpan.FromSeconds(0.3), TimeSpan.FromSeconds(1));

            // Sensitizing and load dictionary to memory with PLINQ
            var dictionaryWords = File.ReadAllLines(options.WordDictionaryPath)
                .AsParallel()
                .WithDegreeOfParallelism(options.NumberOfTasks)
                .SanitizeWordDictionary();

            // We are kinda abusing the main timer to acquire the time required to load the dictionary into memory
            WriteConsole(
                string.Format("Dictionary loaded in {0:F3} seconds.", mainStopWatch.Elapsed.TotalSeconds)
            );

            // Starting stopwatches
            mainStopWatch.Restart();
            updateStopWatch.Start();

            // Initializing the query for searching for matched phrases
            var matchedPhrases =
                dictionaryWords.GetMatchedPhrases(
                    options.AnagramFilter,
                    options.MaximumNumberOfWords,
                    options.NumberOfTasks
                );

            // Executing the query using PLINQ
            matchedPhrases.ForAll(matchedPhrase =>
            {
                //// Found a phrase, check MD5
                var phraseHash = MD5Managed.Calculate(matchedPhrase);
                var matchedHash = hashBytes.FirstOrDefault(
                    hash =>
                        hash[0] == phraseHash[0] &&
                        hash[1] == phraseHash[1] &&
                        hash[2] == phraseHash[2] &&
                        hash[3] == phraseHash[3]
                );

                if (matchedHash != null)
                {
                    // Print the newly found and validated phrase
                    var hashFoundMessage = string.Format(
                        "-- Elapsed: {0:F2}s - Phrase: '{1}' - Matched Hash: {{{2}}} ",
                        mainStopWatch.Elapsed.TotalSeconds,
                        Encoding.ASCII.GetString(matchedPhrase),
                        options.Hashes[matchedHash]
                    );
                    WriteConsole(hashFoundMessage, !options.Silence);
                }

                totalTestedPhrases++;
            });

            // Stop the reporting timer and stop watches
            reportingTimer?.Dispose();
            mainStopWatch.Stop();
            updateStopWatch.Stop();


            // Print summery
            var summeryMessage = string.Format(
                "-- Elapsed: {0:F2}s - Phrases: {1:N0} - Average: {2:N0} H/s",
                mainStopWatch.Elapsed.TotalSeconds,
                totalTestedPhrases,
                totalTestedPhrases / mainStopWatch.Elapsed.TotalSeconds
            );
            WriteConsole(summeryMessage, !options.Silence);
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
                WriteConsole();
                WriteConsole(e.Message);
                WriteConsole();
                WriteConsole(
                    $"dotnet \"{assemblyPath}\" " +
                    "{NumberOfWords} {NumberOfTasks} {\"DictionaryFilePath\"} {\"Anagram}\"} " +
                    "{\"MD5Hash\"} [\"MD5Hash\"] [\"MD5Hash\"] [...] [Silence]"
                );
                WriteConsole();

                return null;
            }

            if (!options.Silence)
            {
                // Printing current options
                WriteConsole($"Anagram: {options.AnagramFilter}");
                WriteConsole($"Word Dictionary: {options.WordDictionaryPath}");
                WriteConsole($"Max Number of Words: {options.MaximumNumberOfWords}");
                WriteConsole($"MD5 Hashes: {string.Join(", ", options.Hashes.Values)}");
                WriteConsole($"Number of Tasks: {options.NumberOfTasks}");
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

            if (!options.Silence)
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

            if (!options.Silence)
            {
                Console.WriteLine("-- End of program execution.");
                Console.ReadLine();
            }
        }

        [SuppressMessage("ReSharper", "FlagArgument")]
        private static void WriteConsole(string message = null, bool overwrite = false, bool isInline = false)
        {
            message = message ?? "";

            lock (ConsoleLock)
            {
                if (overwrite)
                {
                    Console.CursorLeft = 0;
                    var padding = new string(' ', Math.Max(1, Console.BufferWidth - message.Length) - 1);

                    if (isInline)
                    {
                        Console.Write(message + padding);
                    }
                    else
                    {
                        Console.WriteLine(message + padding);
                    }
                }
                else
                {
                    if (isInline)
                    {
                        Console.Write(message);
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }
            }
        }
    }
}