using System;
using System.Timers;
using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Models;
using APSIM.POStats.Shared.GitHub;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace APSIM.POStats.Portal.Controllers
{
    [Route("api")]
    [ApiController]
    public class Api : ControllerBase
    {
        /// <summary>The database context.</summary>
        private readonly StatsDbContext statsDb;

        /// <summary>Event handler for timeout timer.</summary>
        private static void OnTimeout(Object source, ElapsedEventArgs e)
        {
            string url = Environment.GetEnvironmentVariable("POSTATS_UPLOAD_URL");
            if (string.IsNullOrEmpty(url))
                throw new Exception($"Cannot find environment variable POSTATS_UPLOAD_URL");

            TimeoutTimer timer = source as TimeoutTimer;
            Task<string> response = WebUtilities.GetAsync($"{url}api/close?pullRequestNumber={timer.PullRequestNumber}&commitId={timer.CommitId}");
            response.Wait();
        }

        /// <summary>Constructor.</summary>
        /// <param name="stats">The database context.</param>
        public Api(StatsDbContext stats)
        {
            statsDb = stats;
        }

        /// <summary>Invoked by collector to open a pull request.</summary>
        /// <param name="pullRequestNumber">The number of the pull request to open.</param>
        /// <param name="author">The author of the pull request.</param>
        /// <returns></returns>
        [HttpGet("open")]
        public IActionResult Open(int pullRequestNumber, string commitId, int count, string author)
        {
            Console.WriteLine($"\"{author}\" opening PR \"{pullRequestNumber}\"");
            if (pullRequestNumber == 0)
                return BadRequest("You must supply a pull request number");
            if (string.IsNullOrEmpty(author))
                return BadRequest("You must supply an author");
            try
            {
                GitHub.SetStatus(pullRequestNumber, commitId, VariableComparison.Status.Running);

                statsDb.OpenPullRequest(pullRequestNumber, commitId, author, count);

                // Create a timer to close the PR after 30 minutes
                TimeoutTimer timeoutTimer = new TimeoutTimer {Interval=1800000, PullRequestNumber=pullRequestNumber, CommitId=commitId};
                timeoutTimer.Elapsed += OnTimeout;
                timeoutTimer.AutoReset = false;
                timeoutTimer.Start();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok();
        }

        /// <summary>Invoked by collector to close a pull request.</summary>
        /// <param name="pullRequestNumber">The number of the pull request to close.</param>
        /// <returns></returns>
        [HttpGet("close")]
        public IActionResult Close(int pullRequestNumber, string commitId)
        {
            Console.WriteLine($"api/close called");
            
            if (pullRequestNumber == 0)
                return BadRequest("You must supply a pull request number");

            if (commitId.Length == 0)
                return BadRequest("You must supply a commit number");

            if (statsDb.PullRequestWithCommitExists(pullRequestNumber, commitId))
            {
                Console.WriteLine($"Closing PR \"{pullRequestNumber} with commit {commitId}\"");

                try
                {
                    // Send pass/fail to gitHub
                    var pullRequest = statsDb.ClosePullRequest(pullRequestNumber);

                    VariableComparison.Status status = PullRequestFunctions.GetStatus(pullRequest);
                    GitHub.SetStatus(pullRequestNumber, commitId, status);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.ToString());
                }
            }
            else
            {
                PullRequest pr = statsDb.GetPullRequest(pullRequestNumber);
                if (pr == null)
                    return BadRequest($"A PR with {pullRequestNumber} does not exist in the database");
                else
                    return BadRequest($"A PR with {pullRequestNumber} does exist, but has commit number {pr.LastCommit}, and you submitted a commit of {commitId}");
            }

            return Ok();
        }

        /// <summary>Invoked by collector to upload a pull request.</summary>
        /// <param name="pullRequest"></param>
        /// <returns></returns>
        [HttpPost("adddata")]
        [RequestSizeLimit(100_000_000)]
        public IActionResult Post([FromBody]PullRequest pullRequest)
        {
            Console.WriteLine($"api/adddata called");

            if (pullRequest == null)
                return BadRequest("You must supply a pull request");

            if (statsDb.PullRequestWithCommitExists(pullRequest.Number, pullRequest.LastCommit))
            {
                Console.WriteLine($"Adding Data to PR \"{pullRequest.Number}\"");
                try
                {
                    statsDb.AddDataToPullRequest(pullRequest);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.ToString());
                }
            }
            else
            {
                PullRequest pr = statsDb.GetPullRequest(pullRequest.Number);
                if (pr == null)
                    return BadRequest($"A PR with {pullRequest.Number} does not exist in the database");
                else
                    return BadRequest($"A PR with {pullRequest.Number} does exist, but has commit number {pr.LastCommit}, and you submitted a commit of {pullRequest.LastCommit}");
            }

            return Ok();
        }
    }
}