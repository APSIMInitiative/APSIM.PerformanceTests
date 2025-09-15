using System;
using System.Timers;
using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Models;
using APSIM.POStats.Shared.GitHub;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using APSIM.POStats.Portal.Models;

namespace APSIM.POStats.Portal.Controllers
{
    [Route("api")]
    [ApiController]
    public class Api : ControllerBase
    {
        /// <summary>The database context.</summary>
        private readonly StatsDbContext statsDb;

        /// <summary>Lock for adding to the db</summary>
        private static object _lock = new();

        /// <summary>Final timeout in minutes. Controls how long to wait before finally closing a pull request.</summary>
        private const double FINAL_TIMEOUT = 30.0;

        /// <summary>Event handler for finish timer.</summary>
        private static void OnCheckIfFinished(Object source, ElapsedEventArgs e)
        {
            string url = Environment.GetEnvironmentVariable("POSTATS_UPLOAD_URL");
            if (string.IsNullOrEmpty(url))
                throw new Exception($"Cannot find environment variable POSTATS_UPLOAD_URL");

            Console.WriteLine($"Tick");
            PullRequestTimer timer = source as PullRequestTimer;

            Task<string> response = WebUtilities.GetAsync($"{url}api/count?pullrequestnumber={timer.PullRequest}&commitid={timer.Commit}");
            response.Wait();

            string result = response.Result.ToString();
            if (!string.IsNullOrEmpty(result))
            {
                int count = int.Parse(result);

                double minutes = (DateTime.Now - timer.StartTime).TotalMinutes;

                Console.WriteLine($"{timer.PullRequest} ({timer.Commit}): count={count} and minutes={Math.Round(minutes, 1)}");
                if (minutes >= FINAL_TIMEOUT)
                {
                    timer.Stop();
                    response = WebUtilities.GetAsync($"{url}api/close?pullRequestNumber={timer.PullRequest}&commitId={timer.Commit}");
                    response.Wait();
                }
            }
        }

        /// <summary>Constructor.</summary>
        /// <param name="stats">The database context.</param>
        public Api(StatsDbContext stats)
        {
            statsDb = stats;
        }

        /// <summary>Invoked by collector to open a pull request.</summary>
        /// <param name="pullrequestnumber">The number of the pull request to open.</param>
        /// <param name="author">The author of the pull request.</param>
        /// <returns></returns>
        [HttpGet("open")]
        public IActionResult Open(int pullrequestnumber, string commitid, int count, string author)
        {
            Console.WriteLine($"\"{author}\" opening PR \"{pullrequestnumber}\" with commit \"{commitid}\" and count \"{count}\"");
            if (pullrequestnumber == 0)
                return BadRequest("You must supply a pullrequestnumber");
            if (string.IsNullOrEmpty(commitid))
                return BadRequest("You must supply a commitid");
            if (string.IsNullOrEmpty(author))
                return BadRequest("You must supply an author");
            try
            {
                GitHub.SetStatus(pullrequestnumber, commitid, VariableComparison.Status.Running, $"Running {count} Validation Tasks");

                statsDb.OpenPullRequest(pullrequestnumber, commitid, author, count);

                // Create a timer to check how many files have been returned
                PullRequestTimer finishTimer = new PullRequestTimer { Interval = 10000, PullRequest = pullrequestnumber, Commit = commitid };
                finishTimer.Elapsed += OnCheckIfFinished;
                finishTimer.AutoReset = true;
                finishTimer.StartTime = DateTime.Now;
                finishTimer.Start();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok();
        }

        /// <summary>Invoked by collector to close a pull request.</summary>
        /// <param name="pullrequestnumber">The number of the pull request to close.</param>
        /// <returns></returns>
        [HttpGet("close")]
        public async Task<IActionResult> Close(int pullrequestnumber, string commitid)
        {
            Console.WriteLine($"api/close called");
            
            if (pullrequestnumber == 0)
                return BadRequest("You must supply a pull request number");

            if (commitid.Length == 0)
                return BadRequest("You must supply a commit number");

            if (statsDb.PullRequestWithCommitExists(pullrequestnumber, commitid))
            {
                Console.WriteLine($"Closing PR \"{pullrequestnumber} with commit {commitid}\"");

                try
                {
                    // Send pass/fail to gitHub
                    var pullRequest = statsDb.ClosePullRequest(pullrequestnumber);

                    VariableComparison.Status status = PullRequestFunctions.GetStatus(pullRequest);
                    GitHub.SetStatus(pullrequestnumber, commitid, status);
                    if (string.IsNullOrEmpty(pullRequest.Pool))
                        throw new Exception("No pool associated with this pull request. Pool is required to close the Azure Batch pool.");
                    await AzureBatchManager.CloseBatchPoolAsync(pullRequest.Pool);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.ToString());
                }
            }
            else
            {
                PullRequestDetails pr = statsDb.GetPullRequest(pullrequestnumber);
                if (pr == null)
                    return BadRequest($"A PR with {pullrequestnumber} does not exist in the database");
                else
                    return BadRequest($"A PR with {pullrequestnumber} does exist, but has commit number {pr.Commit}, and you submitted a commit of {commitid}");
            }

            return Ok();
        }

        /// <summary>Returns the number of files remaining for a pull request</summary>
        /// <param name="pullrequestnumber">The number of the pull request.</param>
        /// <param name="pullrequestnumber">The commit id of the pull request.</param>
        /// <returns></returns>
        [HttpGet("count")]
        public IActionResult Count(int pullrequestnumber, string commitid)
        {
            Console.WriteLine($"api/count");
            
            if (pullrequestnumber == 0)
                return BadRequest("You must supply a pull request number");

            if (commitid.Length == 0)
                return BadRequest("You must supply a commit number");

            if (statsDb.PullRequestWithCommitExists(pullrequestnumber, commitid))
            {
                try
                {
                    var count = statsDb.GetNumberOfCompletesInPullRequest(pullrequestnumber, commitid);
                    Console.WriteLine($"{count}");
                    return Ok(count);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.ToString());
                }
            }
            else
            {
                PullRequestDetails pr = statsDb.GetPullRequest(pullrequestnumber);
                if (pr == null)
                    return BadRequest($"A PR with {pullrequestnumber} does not exist in the database");
                else
                    return BadRequest($"A PR with {pullrequestnumber} does exist, but has commit number {pr.Commit}, and you submitted a commit of {commitid}");
            }
        }

        /// <summary>Invoked by collector to upload a pull request.</summary>
        /// <param name="pullRequest"></param>
        /// <returns></returns>
        [HttpPost("adddata")]
        [RequestSizeLimit(100_000_000)]
        public IActionResult PostAsync([FromBody]PullRequestDetails pullrequest)
        {
            Console.WriteLine($"api/adddata called");

            if (pullrequest == null)
                return BadRequest("You must supply a pull request");

            lock (_lock)
            {
                if (statsDb.PullRequestWithCommitExists(pullrequest.PullRequest, pullrequest.Commit))
                {
                    try
                    {
                        PullRequestDetails pr = statsDb.AddDataToPullRequest(pullrequest);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex.ToString());
                    }
                }
                else
                {
                    PullRequestDetails pr = statsDb.GetPullRequest(pullrequest.PullRequest);
                    if (pr == null)
                        return BadRequest($"A PR with {pullrequest.PullRequest} does not exist in the database");
                    else
                        return BadRequest($"A PR with {pullrequest.PullRequest} does exist, but has commit number {pr.Commit}, and you submitted a commit of {pullrequest.Commit}");
                }
            }
            return Ok();
        }
    }
}