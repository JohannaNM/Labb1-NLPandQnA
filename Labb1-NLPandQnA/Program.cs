using Azure.AI.Language.QuestionAnswering;
using Azure;
using System;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Labb1_NLPandQnA
{
    // QnA är exemplet om hundar. 
    internal class Program
    {
        private static string translatorEndpoint = "https://api.cognitive.microsofttranslator.com";
        private static string cogSvcKey;
        private static string cogSvcRegion;
        private static string qnaMakerEndpoint = "https://mylanguageservice24.cognitiveservices.azure.com/";
        private static string qnaMakerKey;
        private static string projectName = "qnaLabb1";
        private static string deploymentName = "production";


        static async Task Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                cogSvcKey = configuration["CognitiveServiceKey"];
                cogSvcRegion = configuration["CognitiveServiceRegion"];
                qnaMakerKey = configuration["QnAMakerKey"];

                // Set console encoding to unicode
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;

                // QnA Maker client setup
                Uri endpoint = new Uri(qnaMakerEndpoint);
                AzureKeyCredential credential = new AzureKeyCredential(qnaMakerKey);
                QuestionAnsweringClient client = new QuestionAnsweringClient(endpoint, credential);
                QuestionAnsweringProject project = new QuestionAnsweringProject(projectName, deploymentName);

                Console.WriteLine("Ask a question about dogs, type exit to quit.");

                while (true)
                {
                    // Get user input
                    string userQuestion = Console.ReadLine();
                    if (userQuestion.ToLower() == "exit")
                    {
                        break;
                    }

                    // Detect the language
                    string language = await GetLanguage(userQuestion);
                    Console.WriteLine("Language: " + language);

                    // Translate question to English if necessary
                    string translatedQuestion = language != "en" ? await Translate(userQuestion, language) : userQuestion;
                    if (language != "en")
                    {
                        Console.WriteLine("Translated Question: " + translatedQuestion);
                    }

                    // Get QnA response
                    string qnaAnswer = await GetQnaResponse(client, project, translatedQuestion);
                    if (language == "en")
                    {
                        Console.WriteLine("QnA Answer: " + qnaAnswer);
                    }
                   
                    // Translate answer back to original language if not english
                    string translatedAnswer = language != "en" ? await Translate(qnaAnswer, "en", language) : qnaAnswer;
                    if (language != "en")
                    {
                        Console.WriteLine("Translated Answer: " + translatedAnswer);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task<string> GetLanguage(string text)
        {
            // Default language is English
            string language = "en";

            // Use the Translator detect function
            object[] body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage())
                {
                    // Build the request
                    string path = "/detect?api-version=3.0";
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(translatorEndpoint + path);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", cogSvcKey);
                    request.Headers.Add("Ocp-Apim-Subscription-Region", cogSvcRegion);

                    // Send the request and get response
                    HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                    // Read response as a string
                    string responseContent = await response.Content.ReadAsStringAsync();

                    // Parse JSON array and get language
                    JArray jsonResponse = JArray.Parse(responseContent);
                    language = (string)jsonResponse[0]["language"];
                }
            }

            // return the language
            return language;
        }

        static async Task<string> Translate(string text, string sourceLanguage, string targetLanguage = "en")
        {
            string translation = "";

            // Use the Translator translate function
            object[] body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage())
                {
                    // Build the request
                    string path = $"/translate?api-version=3.0&from={sourceLanguage}&to={targetLanguage}";
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(translatorEndpoint + path);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", cogSvcKey);
                    request.Headers.Add("Ocp-Apim-Subscription-Region", cogSvcRegion);

                    // Send the request and get response
                    HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                    // Read response as a string
                    string responseContent = await response.Content.ReadAsStringAsync();

                    // Parse JSON array and get translation
                    JArray jsonResponse = JArray.Parse(responseContent);
                    translation = (string)jsonResponse[0]["translations"][0]["text"];
                }
            }

            // Return the translation
            return translation;
        }

        static async Task<string> GetQnaResponse(QuestionAnsweringClient client, QuestionAnsweringProject project, string question)
        {
            string answer = "";

            try
            {
                Response<AnswersResult> response = client.GetAnswers(question, project);
                foreach (KnowledgeBaseAnswer qnaAnswer in response.Value.Answers)
                {
                    answer = qnaAnswer.Answer;
                    break; 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
            }

            return answer;
        }

       
    }
}
