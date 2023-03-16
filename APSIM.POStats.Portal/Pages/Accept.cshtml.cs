using APSIM.POStats.Portal.Data;
using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;

namespace APSIM.POStats.Portal.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class AcceptModel : PageModel
    {        
        /// <summary>The database context.</summary>
        private readonly StatsDbContext statsDb;

        /// <summary>The pull request to accept stats on.</summary>
        private PullRequest pullRequest;

        /// <summary>Constructor</summary>
        public AcceptModel(StatsDbContext stats)
        {
            statsDb = stats;
        }

        /// <summary>The pull request id.</summary>
        public int PullRequestId => pullRequest.Id;

        /// <summary>The pull request .</summary>
        public int PullRequestNumber => pullRequest.Number;

        /// <summary>Invoked when page is first loaded.</summary>
        /// <param name="id">The id of the pull request to work with.</param>
        public void OnGet(int id)
        {
            pullRequest = statsDb.PullRequests.Find(id);
            if (pullRequest == null)
                throw new Exception("Cannot find pull request to accept");
        }

        /// <summary>Invoked when user clicks submit.</summary>
        public void OnPost()
        {
            var pullRequestNumber = Convert.ToInt32(Request.Form["PullRequestNumber"]);
            pullRequest = statsDb.PullRequests.FirstOrDefault(pr => pr.Number == pullRequestNumber);
            if (pullRequest == null)
                throw new Exception($"Cannot find pull request {PullRequestNumber}");

            var password = Request.Form["Password"].ToString();
            if (password == Vault.Read("AcceptPassword"))
            {
                pullRequest.DateStatsAccepted = DateTime.Now;
                statsDb.SaveChanges();

                // Send pass/fail to gitHub
                GitHub.SetStatus(pullRequest.Number, VariableComparison.Status.Same);
                Response.Redirect($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase.Value}/{pullRequestNumber}");
            }
        }
    }
}
