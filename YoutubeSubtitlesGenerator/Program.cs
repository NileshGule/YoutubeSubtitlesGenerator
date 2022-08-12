using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace YoutubeSubtitlesGenerator
{
    class Program
    {
        private static readonly string region = "southeastasia";
        private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com";

        static async Task Main(string[] args)
        {
            //string textToTranslate = "I would really like to drive your car around the block a few times!";

            Dictionary<string, string> languageCodeMap = GetLanguageCodeMapping();

            //string route = "/translate?api-version=3.0&from=en&to=fr";
            string textToTranslate = File.ReadAllText(@"C:\Users\niles\Downloads\captions.vtt").Replace("&nbsp;", "");

            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            Console.OutputEncoding = Encoding.Unicode;

            foreach (KeyValuePair<string, string> languageSetting in languageCodeMap)
            {
                await TranslateSubtitle(requestBody, languageSetting).ConfigureAwait(false);
            }

        }

        private static Dictionary<string, string> GetLanguageCodeMapping()
        {
            return new Dictionary<string, string>() {
                {"af", "Afrikaans" },
                {"sq", "Albanian" },
                {"ar", "Arabic" },
                {"hy", "Armenian" },
                { "bn", "Bangla" },
                { "bg", "Bulgarian" },
                { "zh-Hans", "ChineseSimplified" },
                { "hr", "Croatian" },
                { "cs", "Czek" },
                { "da", "Danish" },
                { "nl", "Dutch" },
                { "fil", "Filipino" },
                { "fi", "Finnish" },
                { "fr", "French" },
                { "de", "German" },
                { "el", "Greek" },
                { "he", "Hebrew" },
                { "hi", "Hindi" },
                { "hu", "Hungarian" },
                { "id", "Indonesian" },
                { "ga", "Irish" },
                { "it", "Italian" },
                { "ja", "Japanese" },
                { "ko", "Korean" },
                { "ms", "Malay" },
                { "my", "Myanmar" },
                { "ne", "Nepali" },
                { "nb", "Norwegian" },
                { "fa", "Persian" },
                { "pl", "Polish" },
                { "pt-pt", "Portuguese" },
                { "ro", "Romanian" },
                { "ru", "Russian" },
                { "es", "Spanish" },
                { "sv", "Swedish" },
                { "th", "Thai" },
                { "tr", "Turkish" },
                { "uk", "Ukrainian" },
                { "vi", "Vietnamese" }
            };
        }

        private static async Task TranslateSubtitle(string textToTranslate, KeyValuePair<string, string> languageSetting)
        {
            string languageCode = languageSetting.Key.ToString();
            string languageValue = languageSetting.Value.ToString();

            const string videoLanguage = "en";

            string route = $"/translate?api-version=3.0&from={videoLanguage}&to={languageCode}";

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                BuildRequest(textToTranslate, route, request);

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                // Read response as a string.
                string result = await response.Content.ReadAsStringAsync();
                WriteTranslatedText(languageValue, result);
                // Console.WriteLine(jsonResult.translations.text);
            }
        }

        private static void WriteTranslatedText(string languageValue, string result)
        {
            //Console.WriteLine(result);
            dynamic jsonResult = JsonConvert.DeserializeObject(result);
            Console.WriteLine($"Writing {languageValue} converted output");

            string convertedOutput = jsonResult.First.translations.First.text;
            Console.WriteLine(convertedOutput);

            File.WriteAllText($@"C:\Users\niles\Downloads\TranslatedOutput\Caption-{languageValue}.vtt", convertedOutput);
            Console.WriteLine();
        }

        private static void BuildRequest(string textToTranslate, string route, HttpRequestMessage request)
        {
            string apiKey = Environment.GetEnvironmentVariable("TranslatorAPIKey", EnvironmentVariableTarget.Machine);
            Console.WriteLine($"API Key exists : {string.IsNullOrEmpty(apiKey)}");

            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(endpoint + route);
            request.Content = new StringContent(textToTranslate, Encoding.UTF8, "application/json");
            request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", region);
        }
    }
}
