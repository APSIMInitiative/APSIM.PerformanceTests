using APSIM.POStats.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace APSIM.POStats.Collector
{
    class Program
    {
        /// <summary>
        /// Main entry points.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>0 on success. 1 on error.</returns>
        static async Task<int> Main(string[] args)
        {
            try
            {
                //Test that something has been passed
                if (args.Length < 4)
                {
                    Console.WriteLine("Arguments required are: ");
                    Console.WriteLine("  1. (int) Pull Request Id");
                    Console.WriteLine("  2. (datetime) Date");
                    Console.WriteLine("  3. (string) UserID");
                    Console.WriteLine("  4. (string) Directories (space separated)");
                    Console.WriteLine(@"  Example: APSIM.POStats.Collector 1111 2016.12.01-06:33 hol353 c:\Apsimx\Tests c:\Apsimx\UnderReview");
                    return 1;
                }

                // Convert command line arguments to variables.
                int pullId = Convert.ToInt32(args[0]);
                DateTime runDate = DateTime.ParseExact(args[1], "yyyy.M.d-HH:mm", CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
                string author = args[2];
                var searchDirectories = new List<string>();
                for (int i = 3; i < args.Length; i++)
                {
                    if (Directory.Exists(args[i]))
                        searchDirectories.Add(args[i]);
                }

                var pullRequest = Shared.Collector.RetrieveData(pullId, runDate, author, searchDirectories);

                // Send POStats data to web api. Sometimes it fails so try a number of times.
                var stopwatch = Stopwatch.StartNew();
                int maxNumAttempts = 3;
                int numAttempts = 0;
                bool fail = true;
                string errorMessage = null;
                while (fail && numAttempts < maxNumAttempts)
                {
                    try
                    {
                        Console.WriteLine("Sending POStats data to web api...");
                        if (numAttempts > 0)
                            Console.WriteLine("Retrying....");
                        numAttempts++;

                        string url = Environment.GetEnvironmentVariable("COLLECTOR_URL");
                        if (string.IsNullOrEmpty(url))
                            throw new Exception("Cannot find environment variable COLLECTOR_URL");

                        errorMessage = await WebUtilities.PostAsync(url, pullRequest);
                    }
                    catch (Exception err)
                    {
                        errorMessage = err.ToString();
                    }
                    fail = errorMessage != string.Empty;
                }
                Console.WriteLine($"Elapsed time to send data to web api: {stopwatch.Elapsed.TotalSeconds} seconds");

                if (fail && errorMessage != string.Empty)
                    throw new Exception(errorMessage);
                else
                    Console.WriteLine("Collector completed successfully");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Collector ERROR: " + ex.ToString());
                return 1;
            }
            return 0;
        }
    }
}