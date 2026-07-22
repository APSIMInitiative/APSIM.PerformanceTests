using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APSIM.POStats.Shared
{
    public class WebUtilities
    {
        private enum RequestType 
        {
            GET,
            POST
        }

        private static async Task<string> RequestAsync(RequestType type, string requestUrl, string jsonString, string authorizationToken = null)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.Timeout = new TimeSpan(0, 5, 0);  // 5 minutes

                bool hasAuthorization = false;
                if (authorizationToken != null)
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorizationToken);
                    hasAuthorization = true;
                }

                ProductHeaderValue productHeaderValue = new ProductHeaderValue("ApsimPOStatsWebApp");
                ProductInfoHeaderValue productInfoHeaderValue = new ProductInfoHeaderValue(productHeaderValue);
                httpClient.DefaultRequestHeaders.UserAgent.Clear();
                httpClient.DefaultRequestHeaders.UserAgent.Add(productInfoHeaderValue);

                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                Console.WriteLine($"{type} Request: {requestUrl}");

                HttpResponseMessage response = null;
                if (type == RequestType.GET)
                {
                    response = await httpClient.GetAsync(requestUrl);
                }
                else if (type == RequestType.POST)
                {
                    response = await httpClient.PostAsync(requestUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                }
                
                string fullResponse = await response.Content.ReadAsStringAsync();
                string body = GetHtmlBodyContent(fullResponse);

                Console.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
                if (response.StatusCode >= HttpStatusCode.BadRequest)
                {
                    string output = "";
                    output += $"User Agent: {productHeaderValue}\n";
                    output += $"Has Authorization: {hasAuthorization}\n";
                    output += $"Contents:\n{jsonString}\n";
                    output += $"Request:\n{response}\n";
                    output += $"Response:\n{body}\n";

                    Console.WriteLine($"Error sending {type} for URL {requestUrl} Request\n{output}");
                    throw new Exception($"Error sending {type} for URL {requestUrl} Request\n{output}");
                }
                return body;
            }
        }

        public static ProductHeaderValue GetUserAgent()
        {
            return new ProductHeaderValue("ApsimWebApp", "1.0");
        }

        public static async Task<string> GetAsync(string requestUrl, string authorizationToken = null)
        {
            return await RequestAsync(RequestType.GET, requestUrl, "", authorizationToken);
        }

        public static async Task<string> PostAsync<T>(string requestUrl, T jsonObject, string authorizationToken = null)
        {
            return await RequestAsync(RequestType.POST, requestUrl, JsonSerializer.Serialize(jsonObject), authorizationToken);
        }

        /// <summary>
        /// Extract the content of the body tag from an HTML response. If no body tag is found, return the original content.
        /// </summary> 
        /// <param name="content">The HTML content to extract the body from.</param>
        /// <returns>The content of the body tag, or the original content if no body tag is found.</returns>
        private static string GetHtmlBodyContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Avoid regex work for non-HTML responses (e.g. JSON).
            if (content.IndexOf("<body", StringComparison.OrdinalIgnoreCase) < 0)
                return content;

            Match bodyMatch = Regex.Match(content, @"<body\b[^>]*>([\s\S]*?)</body>", RegexOptions.IgnoreCase);
            string bodyHtml = bodyMatch.Success ? bodyMatch.Groups[1].Value : content;

            // Convert to plain text for cleaner error output.
            bodyHtml = Regex.Replace(bodyHtml, @"<script\b[^>]*>[\s\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);
            bodyHtml = Regex.Replace(bodyHtml, @"<style\b[^>]*>[\s\S]*?</style>", string.Empty, RegexOptions.IgnoreCase);
            string text = Regex.Replace(bodyHtml, @"<[^>]+>", " ");
            text = WebUtility.HtmlDecode(text);
            return Regex.Replace(text, @"\s+", " ").Trim();
        }
    }
}