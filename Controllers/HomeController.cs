using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> IndexAsync()
        {

            string apiKey = "your_api_key";
            string articlePath = "article.txt"; // Replace with the path to your article file

            // Read the article content from the file
            string articleContent = System.IO.File.ReadAllText(articlePath);

            // Extract companies using OpenAI GPT-3
            var extractedResult = await ExtractCompanies(apiKey, articleContent, "Reuters, Fortune");
            Console.WriteLine("Extracted Companies:");
            Console.WriteLine(JsonConvert.SerializeObject(extractedResult, Formatting.Indented));

            // Filter out irrelevant companies using ML methods (or simple rules)
            var filteredResult = FilterIrrelevantCompanies(extractedResult.RelatedCompanies);
            Console.WriteLine("\nFiltered Companies:");
            Console.WriteLine(JsonConvert.SerializeObject(filteredResult, Formatting.Indented));

            return View();
        }



        static async Task<ExtractionResult> ExtractCompanies(string apiKey, string articleContent, string example)
        {
            using (HttpClient client = new HttpClient())
            {
                string apiUrl = "https://api.openai.com/v1/engines/text-davinci-003/completions";

                // Create a prompt with the article text
                string prompt = $"Given the following article, extract relevant companies:\n{articleContent}\nExample: {example}";

                var requestData = new
                {
                    prompt = prompt,
                    temperature = 0.7,
                    max_tokens = 150,
                    n = 1
                };

                string requestDataJson = JsonConvert.SerializeObject(requestData);

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(requestDataJson));

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);

                    try
                    {
                        var companies = new List<string> { jsonResponse.choices[0].text.ToString() };
                        return new ExtractionResult { RelatedCompanies = companies };
                    }
                    catch (JsonReaderException)
                    {
                        return new ExtractionResult { Error = "Unable to parse JSON" };
                    }
                }
                else
                {
                    return new ExtractionResult { Error = $"Request failed with status code {response.StatusCode}" };
                }
            }
        }


        static FilteringResult FilterIrrelevantCompanies(List<string> companies)
        {

            var filteredCompanies = companies.FindAll(company => !company.ToLower().Contains("techcrunch"));

            return new FilteringResult { RelatedCompanies = filteredCompanies };
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

class ExtractionResult
{
    public List<string> RelatedCompanies { get; set; }
    public string Error { get; set; }
}

class FilteringResult
{
    public List<string> RelatedCompanies { get; set; }
}