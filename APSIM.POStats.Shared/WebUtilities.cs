using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http.Headers;

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

                if (!response.IsSuccessStatusCode)
                {
                    string output = "";
                    output += "User Agent: " + productHeaderValue.ToString();
                    output += "Has Authorization: " + hasAuthorization.ToString();
                    output += "Contents: " + Environment.NewLine + jsonString;
                    output += "Request: " + Environment.NewLine + response.ToString();
                    output += "Message: " + Environment.NewLine + response.Content.ToString();
                    throw new Exception($"Error sending {type} Request" + Environment.NewLine + output);
                }

                return response.Content.ToString();
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
    }
}