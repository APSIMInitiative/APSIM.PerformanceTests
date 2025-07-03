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
        /// <param name="pullrequestnumber">The number of the pull request to open.</param>
        /// <param name="author">The author of the pull request.</param>
        /// <returns></returns>
        [HttpGet("open")]
        public IActionResult Open(int pullrequestnumber, string commitid, int count, string author)
        {
            Console.WriteLine($"\"{author}\" opening PR \"{pullrequestnumber}\"");
            if (pullrequestnumber == 0)
                return BadRequest("You must supply a pull request number");
            if (string.IsNullOrEmpty(author))
                return BadRequest("You must supply an author");
            try
            {
                GitHub.SetStatus(pullrequestnumber, commitid, VariableComparison.Status.Running);

                statsDb.OpenPullRequest(pullrequestnumber, commitid, author, count);

                // Create a timer to close the PR after 30 minutes
                TimeoutTimer timeoutTimer = new TimeoutTimer {Interval=1800000, PullRequestNumber=pullrequestnumber, CommitId=commitid};
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
        /// <param name="pullrequestnumber">The number of the pull request to close.</param>
        /// <returns></returns>
        [HttpGet("close")]
        public IActionResult Close(int pullrequestnumber, string commitid)
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
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.ToString());
                }
            }
            else
            {
                PullRequest pr = statsDb.GetPullRequest(pullrequestnumber);
                if (pr == null)
                    return BadRequest($"A PR with {pullrequestnumber} does not exist in the database");
                else
                    return BadRequest($"A PR with {pullrequestnumber} does exist, but has commit number {pr.LastCommit}, and you submitted a commit of {commitid}");
            }

            return Ok();
        }

        /// <summary>Invoked by collector to upload a pull request.</summary>
        /// <param name="pullRequest"></param>
        /// <returns></returns>
        [HttpPost("adddata")]
        [RequestSizeLimit(100_000_000)]
        public IActionResult Post([FromBody]PullRequest pullrequest)
        {
            Console.WriteLine($"api/adddata called");

            if (pullrequest == null)
                return BadRequest("You must supply a pull request");

            if (statsDb.PullRequestWithCommitExists(pullrequest.Number, pullrequest.LastCommit))
            {
                Console.WriteLine($"Adding Data to PR \"{pullrequest.Number}\"");
                try
                {
                    statsDb.AddDataToPullRequest(pullrequest);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.ToString());
                }
            }
            else
            {
                PullRequest pr = statsDb.GetPullRequest(pullrequest.Number);
                if (pr == null)
                    return BadRequest($"A PR with {pullrequest.Number} does not exist in the database");
                else
                    return BadRequest($"A PR with {pullrequest.Number} does exist, but has commit number {pr.LastCommit}, and you submitted a commit of {pullrequest.LastCommit}");
            }

            return Ok();
        }
    }
}