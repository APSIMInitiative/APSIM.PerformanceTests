using System;
using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace APSIM.POStats.Portal.Controllers
{
    [Route("api")]
    [ApiController]
    public class Api : ControllerBase
    {
        /// <summary>The database context.</summary>
        private readonly StatsDbContext statsDb;

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
        public IActionResult Open(int pullRequestNumber, string author)
        {
            if (pullRequestNumber == 0)
                return BadRequest("You must supply a pull request number");
            if (string.IsNullOrEmpty(author))
                return BadRequest("You must supply an author");
            try
            {
                statsDb.OpenPullRequest(pullRequestNumber, author);
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
        public IActionResult Close(int pullRequestNumber)
        {
            if (pullRequestNumber == 0)
                return BadRequest("You must supply a pull request number");

            try
            {
                var pullRequest = statsDb.ClosePullRequest(pullRequestNumber);

                VariableComparison.Status status = PullRequestFunctions.GetStatus(pullRequest);
                GitHub.SetStatus(pullRequestNumber, status);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
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
            if (pullRequest == null)
                return BadRequest("You must supply a pull request");

            try
            {
                statsDb.AddDataToPullRequest(pullRequest);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            // Send pass/fail to gitHub
            return Ok();
        }
    }
}