using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace APSIM.POStats.Shared
{
    public class WebUtilities
    {
        public static async Task<string> PostAsync<T>(string requestUrl, T content)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.Timeout = new TimeSpan(0, 10, 0);  // 10 minutes
                var json = JsonSerializer.Serialize(content);
                Console.WriteLine($"Length of json {json.Length} characters");
                using var response = await httpClient.PostAsync(requestUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                using var i = response.EnsureSuccessStatusCode();
                var data = await response.Content.ReadAsStringAsync();
                return data;
            }
        }
    }
}