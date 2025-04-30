using Octokit;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace APSIM.POStats.Shared
{
    public class GitHub
    {
        /// <summary>Get details about a given pull request.</summary>
        /// <param name="pullRequestID">The pull request id.</param>
        public static GitHubPullRequestDetails GetPullRequest(int pullRequestID)
        {
            try
            {
                var github = new GitHubClient(new ProductHeaderValue("ApsimX"));
                var pullRequestTask = github.PullRequest.Get("APSIMInitiative", "ApsimX", pullRequestID);
                pullRequestTask.Wait();
                return new GitHubPullRequestDetails(pullRequestTask.Result);
            }
            catch (Exception)
            {
                throw new Exception($"GitHub cannot return details for pull request: {pullRequestID}");
            }
        }

        /// <summary>
        /// Set the GitHub status.
        /// </summary>
        /// <param name="pullRequestNumber">The pull request number.</param>
        /// <param name="pass">Set the status to pass?</param>
        public static async void SetStatus(int pullRequestNumber, VariableComparison.Status status)
        {
            Console.WriteLine($"\"{pullRequestNumber}\" has a status of \"{status.ToString()}\"");

            GitHubClient github = new GitHubClient(new ProductHeaderValue("ApsimX"));
            string token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrEmpty(token))
                throw new Exception("Cannot find environment variable GITHUB_TOKEN");
            github.Credentials = new Credentials(token);
            Task<Octokit.PullRequest> pullRequestTask = github.PullRequest.Get("APSIMInitiative", "ApsimX", pullRequestNumber);
            pullRequestTask.Wait();
            Octokit.PullRequest pullRequest = pullRequestTask.Result;
            Uri statusURL = new System.Uri(pullRequest.StatusesUrl);

            Console.WriteLine($"Uri: {statusURL.ToString()}");

            string state = "failure";
            string stateFormatted = status.ToString();
            if (status == VariableComparison.Status.Same)
                state = "success";

            string urlStr = string.Format("https://postats.apsim.info/{0}", pullRequestNumber);

            string body = "{" + Environment.NewLine +
                          "  \"state\": \"" + state + "\"," + Environment.NewLine +
                          "  \"target_url\": \"" + urlStr + "\"," + Environment.NewLine +
                          "  \"description\": \"" + stateFormatted + "\"," + Environment.NewLine +
                          "  \"context\": \"APSIM.POStats\"" + Environment.NewLine +
                          "}";

            Console.WriteLine(body);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var productHeaderValue = new System.Net.Http.Headers.ProductHeaderValue(name: "ApsimPOStatsWebApp", version: "1.0");
                var productInfoHeaderValue = new System.Net.Http.Headers.ProductInfoHeaderValue(productHeaderValue);
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.Add(productInfoHeaderValue);

                var response = await client.PostAsync(statusURL, new StringContent(body, Encoding.UTF8, @"application/json"));
                var data = await response.Content.ReadAsStringAsync();

                Console.WriteLine("Response:");
                Console.WriteLine(response.RequestMessage);
                Console.WriteLine(response.StatusCode);
                Console.WriteLine(data);
            }
        }
    }

    /// <summary>Class encapsulating pull request details.</summary>
    public class GitHubPullRequestDetails
    {
        /// <summary>The OctoKit pullrequest instance;</summary>
        private readonly Octokit.PullRequest pullRequest;

        /// <summary>Constructor.</summary>
        /// <param name="result">A pull request instance.</param>
        public GitHubPullRequestDetails(Octokit.PullRequest result) => pullRequest = result;

        /// <summary>The id / number of the pull request.</summary>
        public long Number => pullRequest.Number;

        /// <summary>The author / creator of the pull request.</summary>
        public string Author => pullRequest.User.Login;

        /// <summary>Date the pull request was created.</summary>
        public DateTime DateCreated => pullRequest.CreatedAt.DateTime;
    }
}
