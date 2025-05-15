using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Models;
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
        static int Main(string[] args)
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

            try
            {
                // Convert command line arguments to variables.
                //get the PR id
                int pullId = Convert.ToInt32(args[0]);

                //get the run date
                DateTime runDate = DateTime.ParseExact(args[1], "yyyy.M.d-HH:mm", CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);

                //get the author
                string author = args[2];

                //get the directories
                List<string> searchDirectories = new List<string>();
                for (int i = 3; i < args.Length; i++)
                {
                    if (Directory.Exists(args[i]))
                        searchDirectories.Add(args[i]);
                    else
                        throw new Exception($"Directory \"{args[i]}\" does not exist.");
                }

                //get the Pull Request details
                PullRequest pullRequest = Shared.Collector.RetrieveData(pullId, runDate, author, searchDirectories);

                // Send POStats data to web api.
                Stopwatch stopwatch = Stopwatch.StartNew();
                UploadStats(pullRequest, "POSTATS_UPLOAD_URL");
                Console.WriteLine($"Elapsed time to send data to new web api: {stopwatch.Elapsed.TotalSeconds} seconds");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Collector ERROR: " + ex.ToString());
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Upload method for new POSTATS endpoints.
        /// </summary>
        /// <param name="pullRequest"></param>
        /// <param name="urlEnvironmentVariable"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static void UploadStats(PullRequest pullRequest, string urlEnvironmentVariable)
        {
            Console.WriteLine("Running POStats Uploader.");

            string url = Environment.GetEnvironmentVariable(urlEnvironmentVariable);
            if (string.IsNullOrEmpty(url))
                throw new Exception($"Cannot find environment variable {urlEnvironmentVariable}");

            // Tell endpoint we're about to upload data.
            Task<string> response = WebUtilities.GetAsync($"{url}/open?pullRequestNumber={pullRequest.Number}&author={pullRequest.Author}");
            response.Wait();

            bool ok = true;
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
                    response = WebUtilities.PostAsync($"{url}/adddata", pullRequest, null);
                    response.Wait();
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Error when collecting file {file.Name}");
                    Console.WriteLine(exception.Message);
                    ok = false;
                }
            }

            // Tell endpoint we're about to upload data.
            if (ok)
            {
                response = WebUtilities.GetAsync($"{url}/close?pullRequestNumber={pullRequest.Number}");
                response.Wait();
            }
            else
            {
                throw new Exception("Errors Uploading data to POStats");
            }
        }
    }
}