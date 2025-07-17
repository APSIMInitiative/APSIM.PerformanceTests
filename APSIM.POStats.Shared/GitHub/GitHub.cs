using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace APSIM.POStats.Shared.GitHub
{
    public class GitHub
    {
        private static readonly string GITHUB_API = "https://api.github.com/";
        private static readonly string REPO = "repos/APSIMInitiative/ApsimX/";

        /// <summary>Get details about a given pull request.</summary>
        /// <param name="pullRequestID">The pull request id.</param>
        public static GitHubPullRequestDetails GetPullRequest(int pullRequestID)
        {
            //check we have our login token
            string token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrEmpty(token))
                throw new Exception("Cannot find environment variable GITHUB_TOKEN");

            //build url we need
            string url = GITHUB_API + REPO + "pulls/" + pullRequestID.ToString();

            //do the request
            Task<string> response = WebUtilities.GetAsync(url, token);
            response.Wait();

            //deseralize json response
            Dictionary<string, object> dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(response.Result);
            Dictionary<string, object> user = JsonSerializer.Deserialize<Dictionary<string, object>>(dictionary["user"].ToString());

            //get variables we need
            int number = Convert.ToInt32(dictionary["number"].ToString());
            string author = user["login"].ToString();
            DateTime dateTime = DateTime.Parse(dictionary["created_at"].ToString());
            string state = dictionary["state"].ToString();
            string statusURL = dictionary["statuses_url"].ToString();

            Console.WriteLine($"Github Status Update: {number} {author} {dateTime} {state} {statusURL}");

            //return the result as a GitHubPullRequestDetails because we don't need all the data the base class has
            return new GitHubPullRequestDetails(number, author, dateTime, state, statusURL);
        }

        /// <summary>
        /// Set the GitHub status.
        /// </summary>
        /// <param name="pullRequestNumber">The pull request number.</param>
        /// <param name="pass">Set the status to pass?</param>
        public static void SetStatus(int pullRequestNumber, string commitId, VariableComparison.Status status, string message = "")
        {
            //check we have our login token
            string token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrEmpty(token))
                throw new Exception("Cannot find environment variable GITHUB_TOKEN");

            //check the Pull request is open
            GitHubPullRequestDetails pullRequestTask = GetPullRequest(pullRequestNumber);
            if (pullRequestTask.State.ToLower() != "open")
                throw new Exception($"Cannot set the status of a Pull Request that is not open (id: {pullRequestNumber})");

            //if (!pullRequestTask.StatusURL.Contains(commitId))
            //    throw new Exception($"Cannot set the status of a Pull Request as the commit id is not the latest (PR: {pullRequestNumber}, Commit:{commitId})");

            //check the status of POStats for this PR
            string state = "failure";
           

            if (status == VariableComparison.Status.Same)
                state = "success";

            if (status == VariableComparison.Status.Running)
                state = "pending";

            string stateFormatted = status.ToString();
            if (String.IsNullOrEmpty(message))
                stateFormatted = message;

                //build our check link that refers back to POStats from github
                string serverURL = Environment.GetEnvironmentVariable("POSTATS_UPLOAD_URL");
            string urlStr = $"{serverURL}{pullRequestNumber}";

            //Status POST body details
            GitHubStatusDetails body = new GitHubStatusDetails(state, urlStr, stateFormatted, "APSIM.POStats2");

            //Send POST request
            Task<string> response = WebUtilities.PostAsync(pullRequestTask.StatusURL, body, token);
            response.Wait();
        }
    }
}
