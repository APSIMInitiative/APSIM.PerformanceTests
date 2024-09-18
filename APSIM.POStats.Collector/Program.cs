using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
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

                // Send POStats data to web api.
                var stopwatch = Stopwatch.StartNew();
                await UploadStats(pullRequest, "CollectorURL");
                Console.WriteLine($"Elapsed time to send data to web api: {stopwatch.Elapsed.TotalSeconds} seconds");

                // Send POStats data to new POStats web api.
                await UploadStatsNew(pullRequest, "POSTATS_UPLOAD_URL");
                Console.WriteLine($"Elapsed time to send data to new web api: {stopwatch.Elapsed.TotalSeconds} seconds");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Collector ERROR: " + ex.ToString());
                return 1;
            }
            return 0;
        }

        private static async Task UploadStats(PullRequest pullRequest, string urlEnvironmentVariable)
        {
            int maxNumAttempts = 3;
            int numAttempts = 0;
            string errorMessage = string.Empty;
            bool fail = true;
            while (fail && numAttempts < maxNumAttempts)
            {
                try
                {
                    Console.WriteLine($"Sending POStats data to {urlEnvironmentVariable} web api...");
                    if (numAttempts > 0)
                        Console.WriteLine("Retrying....");
                    numAttempts++;

                    string url = Environment.GetEnvironmentVariable(urlEnvironmentVariable);
                    if (string.IsNullOrEmpty(url))
                        throw new Exception($"Cannot find environment variable {urlEnvironmentVariable}");
                    errorMessage = await WebUtilities.PostAsync(url, pullRequest);
                }
                catch (Exception err)
                {
                    errorMessage = err.ToString();
                }
                fail = errorMessage != string.Empty;
            }

            if (fail && errorMessage != string.Empty)
                throw new Exception(errorMessage);
            else
                Console.WriteLine($"{urlEnvironmentVariable} collector completed successfully");
        }


        /// <summary>
        /// Upload method for new POSTATS endpoints.
        /// </summary>
        /// <param name="pullRequest"></param>
        /// <param name="urlEnvironmentVariable"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static async Task UploadStatsNew(PullRequest pullRequest, string urlEnvironmentVariable)
        {
            Console.WriteLine("Running new uploader.");

            string url = Environment.GetEnvironmentVariable(urlEnvironmentVariable);
            if (string.IsNullOrEmpty(url))
                throw new Exception($"Cannot find environment variable {urlEnvironmentVariable}");

            // Tell endpoint we're about to upload data.
            using var client = new HttpClient();
            await client.GetAsync($"{url}/open?pullRequestNumber={pullRequest.Number}&author={pullRequest.Author}");

            bool ok = false;
            List<ApsimFile> files = new();
            files.AddRange(pullRequest.Files);
            foreach (var file in files)
            {
                // Upload data for one file only.
                pullRequest.Files.Clear();
                pullRequest.Files.Add(file);

                Console.WriteLine($"Sending POStats data to web api for file {file.Name}");

                try
                {
                    var response = await WebUtilities.PostAsync($"{url}/adddata", pullRequest);
                    ok = string.IsNullOrEmpty(response);
                    Console.WriteLine(response);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            // Tell endpoint we're about to upload data.
            if (ok)
                await client.GetAsync($"{url}/api/close?pullRequestNumber={pullRequest.Number}");
        }

    }
}