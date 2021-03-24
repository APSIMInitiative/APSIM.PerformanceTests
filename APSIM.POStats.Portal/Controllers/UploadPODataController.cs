using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APSIM.POStats.Portal.Data;
using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace APSIM.POStats.Portal.Controllers
{
    [Route("api/uploadpodata")]
    [ApiController]
    public class UploadPODataController : ControllerBase
    {
        /// <summary>The database context.</summary>
        private readonly StatsDbContext statsDb;

        /// <summary>Constructor.</summary>
        /// <param name="stats">The database context.</param>
        public UploadPODataController(StatsDbContext stats)
        {
            statsDb = stats;
        }

        /// <summary>Invoked by collector to upload a pull request.</summary>
        /// <param name="pullRequest"></param>
        /// <returns></returns>
        [HttpPost]
        [RequestSizeLimit(100_000_000)]
        public string Post([FromBody]PullRequest pullRequest)
        {
            try
            {
                UploadPullRequest(pullRequest);

                // Send pass/fail to gitHub
                bool isPass = PullRequestFunctions.IsPass(pullRequest);
                GitHub.SetStatus(pullRequest.Number, isPass);
            }
            catch (Exception err)
            {
                return $"Error from POStats web api: {err}";
            }
            return null;
        }

        public void UploadPullRequest(PullRequest pullRequest)
        {
            // Remove the old PR.
            var oldPRs = statsDb.PullRequests.Where(pr => pr.Number == pullRequest.Number);
            statsDb.PullRequests.RemoveRange(oldPRs);

            // Set the accepted PR to the latest one.
            pullRequest.AcceptedPullRequest = statsDb.GetMostRecentAcceptedPullRequest();

            // Send PR to database.
            statsDb.PullRequests.Add(pullRequest);
            statsDb.SaveChanges();
        }
    }
}