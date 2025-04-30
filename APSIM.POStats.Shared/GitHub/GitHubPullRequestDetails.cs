using Octokit;
using System;

namespace APSIM.POStats.Shared.GitHub
{
    /// <summary>Class encapsulating pull request details.</summary>
    public class GitHubPullRequestDetails
    {
        /// <summary>The id / number of the pull request.</summary>
        public readonly long Number;

        /// <summary>The author / creator of the pull request.</summary>
        public readonly string Author;

        /// <summary>Date the pull request was created.</summary>
        public readonly DateTime DateCreated;

        /// <summary>State of the pull request (open/closed)</summary>
        public readonly string State;

        /// <summary>Url to return status POST requests to</summary>
        public readonly string StatusURL;

        /// <summary>Constructor.</summary>
        /// <param name="result">A pull request instance.</param>
        public GitHubPullRequestDetails(PullRequest result) 
        {
            Number = result.Number;
            Author = result.User.Login;
            DateCreated = result.CreatedAt.DateTime;
            State = result.State.ToString();
            StatusURL = result.StatusesUrl;
        }
    }
}
