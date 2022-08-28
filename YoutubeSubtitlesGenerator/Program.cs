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
        static async Task Main(string[] args)
        {
            // string textToTranslate = "I would really like to drive your car around the block a few times!";
            string textToTranslate;
            string inputFileName;

            if (args.Length == 0)
            {
                Console.WriteLine($"Missing input file name. {Environment.NewLine} Cannot continue without the input file.");
                return;
            }
            else
            {
                inputFileName = args[0];
                Console.WriteLine($"Input file name : {inputFileName}");

                // remove WEBVTT, kind: captions Language: en lines from original file by skipping first 4 lines
                File.WriteAllLines(inputFileName, File.ReadLines(inputFileName).Skip(4).ToList());

                textToTranslate = File.ReadAllText($@"{inputFileName}").Replace("&nbsp;", "");

                //Console.WriteLine($"Text to translate : {Environment.NewLine}{textToTranslate}");
            }

            string fileName = Path.GetFileNameWithoutExtension($@"{inputFileName}");

            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            Console.OutputEncoding = Encoding.Unicode;

            Dictionary<string, string> languageCodeMap = GetLanguageCodeMapping();

            foreach (KeyValuePair<string, string> languageSetting in languageCodeMap)
            {
                // await TranslateSubtitle(fileName, requestBody, languageSetting)
                //     .ConfigureAwait(false);
            }

        }

        private static Dictionary<string, string> GetLanguageCodeMapping()
        {
            var section = (Hashtable)ConfigurationManager.GetSection("CodeLanguageMapping");

            Dictionary<string, string> codeLanguageMap = section.Cast<DictionaryEntry>().ToDictionary(d => (string)d.Key, d => (string)d.Value);

            return codeLanguageMap;

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

            string webVttPrefix = string.Concat("WEBVTT", Environment.NewLine, Environment.NewLine);

            string finalText = string.Concat(webVttPrefix, convertedOutput);

            File.WriteAllText($@"{destinationFolder}\{fileName}-{languageValue}.vtt", finalText);
            Console.WriteLine();
        }

        private static void BuildRequest(string textToTranslate, string route, HttpRequestMessage request)
        {
            string apiKey = Environment.GetEnvironmentVariable("TranslatorAPIKey", EnvironmentVariableTarget.Machine);

            string translatorRegion = ConfigurationManager.AppSettings["translatorRegion"];
            string translatorEndpoint = ConfigurationManager.AppSettings["translatorEndpoint"];
            

            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(translatorEndpoint + route);
            
            request.Content = new StringContent(textToTranslate, Encoding.UTF8, "application/json");
            
            request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", translatorRegion);
        }
    }
}
