using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlTypes;
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
        static string destinationFolder;

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

            destinationFolder = ConfigurationManager.AppSettings["destinationFolder"];

            string fileName = Path.GetFileNameWithoutExtension($@"{inputFileName}");

            // get the current files in destination folder
            List<string> existingFiles = Directory.GetFiles(destinationFolder)
                .Where(file => Path.GetFileName(file).StartsWith(fileName))
                .ToList();

            Console.WriteLine($"Existing Files in the directory : {existingFiles.Count}");

            List<string> existingFileLanguages = new List<string>();

            foreach (string file in existingFiles)
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                int index = filename.IndexOf(fileName);

                //consider the '-' in the filename and increment the index eg -Hindi, -Arabic etc
                string language = filename.Remove(index, fileName.Length+1);
                existingFileLanguages.Add(language);
            }

            Dictionary<string, string> languageCodeMap = GetLanguageCodeMapping();

            // Get the list of language codes
            var filesToTranslate = languageCodeMap.Values.ToList();

            var missingSubtitles = filesToTranslate.Except(existingFileLanguages).ToList();

            Console.WriteLine();
            Console.WriteLine($"Missing subtitles count: {missingSubtitles.Count}");


            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            Console.OutputEncoding = Encoding.Unicode;

            foreach (string subtitleLanguage in missingSubtitles)
            {
                string subtitleLanguageCode = languageCodeMap.First(x => x.Value == subtitleLanguage).Key;
                await TranslateSubtitle(fileName, requestBody, subtitleLanguageCode, subtitleLanguage)
                    .ConfigureAwait(false);
            }

        }

        private static Dictionary<string, string> GetLanguageCodeMapping()
        {
            var section = (Hashtable)ConfigurationManager.GetSection("CodeLanguageMapping");

            Dictionary<string, string> codeLanguageMap = section.Cast<DictionaryEntry>().ToDictionary(d => (string)d.Key, d => (string)d.Value);

            return codeLanguageMap;

        }

        private static async Task TranslateSubtitle(string fileName, string textToTranslate, string languageCode, string languageValue)
        {
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
                    Console.WriteLine($"Writing {languageValue} converted output");

                    dynamic jsonResult = JsonConvert.DeserializeObject(result);

                    if (jsonResult != null && jsonResult.First != null && jsonResult.First.translations != null && jsonResult.First.translations.First != null)
                    {
                        string convertedOutput = jsonResult.First.translations.First.text;

                        string webVttPrefix = string.Concat("WEBVTT", Environment.NewLine, Environment.NewLine);
                        string finalText = string.Concat(webVttPrefix, convertedOutput);

                        string destinationFolder = ConfigurationManager.AppSettings["destinationFolder"];
                        Console.WriteLine($"Destination folder: {destinationFolder}");

                        string outputFile = Path.Combine(destinationFolder, $"{fileName}-{languageValue}.vtt");
                        File.WriteAllText(outputFile, finalText);

                        Console.WriteLine($"Translated subtitles for {languageValue} saved successfully");
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine("Unable to retrieve the translated text from the JSON response.");
                        Console.WriteLine("JSON Response:");
                        Console.WriteLine(result);
                    }
                }

        private static void BuildRequest(string textToTranslate, string route, HttpRequestMessage request)
        {
            string apiKey = Environment.GetEnvironmentVariable("TranslatorAPIKey", EnvironmentVariableTarget.Machine);

            ///// Use this If environment variable has errors, 
            //string apiKey = "";

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
