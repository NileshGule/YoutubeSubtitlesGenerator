using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace YoutubeSubtitlesGenerator
{
    class Program
    {
        private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com";
        

        static async Task Main(string[] args)
        {
            //string textToTranslate = "I would really like to drive your car around the block a few times!";

            Dictionary<string, string> languageCodeMap = GetLanguageCodeMapping();

            //string route = "/translate?api-version=3.0&from=en&to=fr";
            string textToTranslate = File.ReadAllText(@"C:\Users\niles\Downloads\captions.vtt").Replace("&nbsp;", "");

            string fileName = Path.GetFileNameWithoutExtension(@"C:\Users\niles\Downloads\captions.vtt");

            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            Console.OutputEncoding = Encoding.Unicode;

            foreach (KeyValuePair<string, string> languageSetting in languageCodeMap)
            {
                await TranslateSubtitle(fileName, requestBody, languageSetting)
                    .ConfigureAwait(false);
            }

        }

        private static Dictionary<string, string> GetLanguageCodeMapping()
        {
            var section = (Hashtable)ConfigurationManager.GetSection("CodeLanguageMapping");

            Dictionary<string, string> codeLanguageMap = section.Cast<DictionaryEntry>().ToDictionary(d => (string)d.Key, d => (string)d.Value);

            return codeLanguageMap;

            //return new Dictionary<string, string>() {
            //    {"af", "Afrikaans" },
            //    {"sq", "Albanian" },
            //    {"ar", "Arabic" },
            //    {"hy", "Armenian" },
            //    //{ "", "" },
                //{ "", "" },
                
            //};
        }

        private static async Task TranslateSubtitle(string fileName, string textToTranslate, KeyValuePair<string, string> languageSetting)
        {
            string languageCode = languageSetting.Key.ToString();
            string languageValue = languageSetting.Value.ToString();

            string defaultVideoLanguage = ConfigurationManager.AppSettings["defaultVideoLanguage"];

            string route = $"/translate?api-version=3.0&from={defaultVideoLanguage}&to={languageCode}";

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                BuildRequest(textToTranslate, route, request);

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                // Read response as a string.
                string result = await response.Content.ReadAsStringAsync();
                WriteTranslatedText(fileName, languageValue, result);
                // Console.WriteLine(jsonResult.translations.text);
            }
        }

        private static void WriteTranslatedText(string fileName, string languageValue, string result)
        {
            //Console.WriteLine(result);
            dynamic jsonResult = JsonConvert.DeserializeObject(result);
            Console.WriteLine($"Writing {languageValue} converted output");

            string convertedOutput = jsonResult.First.translations.First.text;
            Console.WriteLine(convertedOutput);

            //string outputDirectory = @"C:\Users\niles\Downloads\TranslatedOutput";

            string destinationFolder = ConfigurationManager.AppSettings["destinationFolder"];
            Console.WriteLine($"Destination folder : {destinationFolder}");

            File.WriteAllText($@"{destinationFolder}\{fileName}-{languageValue}.vtt", convertedOutput);
            Console.WriteLine();
        }

        private static void BuildRequest(string textToTranslate, string route, HttpRequestMessage request)
        {
            string apiKey = Environment.GetEnvironmentVariable("TranslatorAPIKey", EnvironmentVariableTarget.Machine);
            Console.WriteLine($"API Key exists : {!string.IsNullOrEmpty(apiKey)}");

            string translatorRegion = ConfigurationManager.AppSettings["translatorRegion"];
            Console.WriteLine($"Translator region : {translatorRegion}");

            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(endpoint + route);
            request.Content = new StringContent(textToTranslate, Encoding.UTF8, "application/json");
            request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", translatorRegion);
        }
    }
}
