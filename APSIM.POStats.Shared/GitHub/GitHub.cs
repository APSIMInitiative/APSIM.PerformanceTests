using Octokit;
using System;
using System.Threading.Tasks;

namespace APSIM.POStats.Shared.GitHub
{
    public class GitHub
    {
        /// <summary>Get details about a given pull request.</summary>
        /// <param name="pullRequestID">The pull request id.</param>
        public static GitHubPullRequestDetails GetPullRequest(int pullRequestID)
        {
            //get our user-agent from the web utilties
            System.Net.Http.Headers.ProductHeaderValue agent = WebUtilities.GetUserAgent();

            //convert to octokit value
            ProductHeaderValue octoAgent = new ProductHeaderValue(agent.Name, agent.Version);

            //create github client for request
            GitHubClient githubClient = new GitHubClient(octoAgent);

            try
            {
                //do the request
                Task<PullRequest> pullRequestTask = githubClient.PullRequest.Get("APSIMInitiative", "ApsimX", pullRequestID);
                pullRequestTask.Wait();

                //return the result as a GitHubPullRequestDetails because we don't need all the data the base class has
                return new GitHubPullRequestDetails(pullRequestTask.Result);
            }
            catch (Exception exception)
            {
                throw new Exception($"GitHub cannot return details for pull request: {pullRequestID}" + Environment.NewLine + exception.Message);
            }
        }

        /// <summary>
        /// Set the GitHub status.
        /// </summary>
        /// <param name="pullRequestNumber">The pull request number.</param>
        /// <param name="pass">Set the status to pass?</param>
        public static void SetStatus(int pullRequestNumber, VariableComparison.Status status)
        {
            //check we have our login token
            string token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrEmpty(token))
                throw new Exception("Cannot find environment variable GITHUB_TOKEN");

            //check the Pull request is open
            GitHubPullRequestDetails pullRequestTask = GetPullRequest(pullRequestNumber);
            if (pullRequestTask.State.ToLower() == "closed")
                throw new Exception($"Cannot set the status of a closed Pull Request (id: {pullRequestNumber})");

            //check the status of POStats for this PR
            string state = "failure";
            string stateFormatted = status.ToString();
            if (status == VariableComparison.Status.Same)
                state = "success";

            //build our check link that refers back to POStats from github
            string urlStr = $"https://postats.apsim.info/{pullRequestNumber}";

            //Status POST body details
            GitHubStatusDetails body = new GitHubStatusDetails(state, urlStr, stateFormatted, "APSIM.POStats");

            //Send POST request
            Task<string> response = WebUtilities.PostAsync(pullRequestTask.StatusURL, body, token);
            response.Wait();
        }
    }
}
