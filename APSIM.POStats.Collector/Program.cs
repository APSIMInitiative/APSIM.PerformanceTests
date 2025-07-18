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
                Console.WriteLine("  1. (string) Command [open, upload, close, jenkins]");
                Console.WriteLine("  2. (int) Pull Request Id");
                Console.WriteLine("  3. (int) Commit Id");
                Console.WriteLine("  4. (string) UserID");
                Console.WriteLine("  5. (datetime) Date");
                Console.WriteLine("  6. (string) Directories (space separated)");
                Console.WriteLine(@"  Example: APSIM.POStats.Collector Upload 1111 abcdef12345 2016.12.01-06:33 hol353 c:\Apsimx\Tests c:\Apsimx\UnderReview");
                return 1;
            }

            try
            {
                // Convert command line arguments to variables.

                //get the command
                string command = args[0];
                command = command.ToLower();

                //get the PR id
                int pullId = Convert.ToInt32(args[1]);

                //get the commit id
                string commitId = args[2];

                //get the author
                string author = args[3];

                //get the run date
                DateTime runDate = DateTime.ParseExact(args[4], "yyyy.M.d-HH:mm", CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);

                //get the directories
                List<string> searchDirectories = new List<string>();
                for (int i = 5; i < args.Length; i++)
                {
                    if (Directory.Exists(args[i]))
                        searchDirectories.Add(args[i]);
                    else
                        throw new Exception($"Directory \"{args[i]}\" does not exist.");
                }

                //get the Pull Request details
                PullRequestDetails pullRequest = Shared.Collector.RetrieveData(pullId, commitId, author, runDate, searchDirectories);

                string url = Environment.GetEnvironmentVariable("POSTATS_UPLOAD_URL");
                Console.WriteLine($"{url}");
                if (string.IsNullOrEmpty(url))
                    throw new Exception($"Cannot find environment variable POSTATS_UPLOAD_URL");

                url = url + "api";

                if (command == "open")
                {
                    // Tell endpoint we're about to upload data.
                    Task<string> response = WebUtilities.GetAsync($"{url}/open?pullrequestnumber={pullRequest.PullRequest}&commitnumber={pullRequest.Commit}&author={pullRequest.Author}");
                    response.Wait();
                }
                else if (command == "upload")
                {
                    // Send POStats data to web api.
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    UploadStats(pullRequest, url);
                    Console.WriteLine($"Elapsed time to send data to new web api: {stopwatch.Elapsed.TotalSeconds} seconds");
                }
                else if (command == "close")
                {
                    Task<string> response = WebUtilities.GetAsync($"{url}/close?pullrequestnumber={pullRequest.PullRequest}&commitid={pullRequest.Commit}");
                    response.Wait();
                }
                //this is provided for Jenkins so it can continue to run with the original uploading code
                else if (command == "jenkins")
                {
                    //get the Pull Request details
                    PullRequestJenkins pullRequestJenkins = new PullRequestJenkins();
                    pullRequestJenkins.Id = pullRequest.Id;
                    pullRequestJenkins.Number = pullRequest.PullRequest;
                    pullRequestJenkins.Author = pullRequest.Author;
                    pullRequestJenkins.DateRun = pullRequest.DateRun;
                    pullRequestJenkins.DateStatsAccepted = pullRequest.DateStatsAccepted;
                    pullRequestJenkins.Files = pullRequest.Files;
                    pullRequestJenkins.AcceptedPullRequestId = pullRequest.AcceptedPullRequestId;
                    pullRequestJenkins.AcceptedPullRequest = null;
                    UploadStatsJenkins(pullRequestJenkins);
                }
                else
                {
                    throw new Exception($"Command \"{command}\" is not valid.");
                }
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
        private static void UploadStats(PullRequestDetails pullRequest, string url)
        {
            List<ApsimFile> files = new();
            files.AddRange(pullRequest.Files);

            //In the case we have no files produced, just send it back empty.
            if (files.Count == 0)
            {
                Task<string> response = WebUtilities.PostAsync($"{url}/adddata", pullRequest, null);
                response.Wait();
            }
            else
            {
                foreach (var file in files)
                {
                    // Upload data for one file only.
                    pullRequest.Files = new List<ApsimFile>();
                    pullRequest.Files.Add(file);

                    Task<string> response = WebUtilities.PostAsync($"{url}/adddata", pullRequest, null);
                    response.Wait();
                }
            }
        }
        
        /// <summary>
        /// Upload method for Jenkins
        /// </summary>
        /// <param name="pullRequest"></param>
        /// <param name="urlEnvironmentVariable"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static void UploadStatsJenkins(PullRequestJenkins pullRequest)
        {
            Console.WriteLine("Running POStats Uploader.");

            string url = Environment.GetEnvironmentVariable("POSTATS_UPLOAD_URL");
            Console.WriteLine($"{url}");
            if (string.IsNullOrEmpty(url))
                throw new Exception($"Cannot find environment variable POSTATS_UPLOAD_URL");

            url = url + "api";

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