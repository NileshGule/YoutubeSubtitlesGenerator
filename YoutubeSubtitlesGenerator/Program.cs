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

        static List<String> vttFiles;

        static String selectedFile;

        static async Task Main(string[] args)
        {
            
            string textToTranslate;
            string inputFileName;

            await ListFiles();

            Console.WriteLine();
            await SelectFileToTranslate();
            
            inputFileName = selectedFile;
            Console.WriteLine($"Input file name : {inputFileName}");

            // remove WEBVTT, kind: captions Language: en lines from original file by skipping first 4 lines
            File.WriteAllLines(inputFileName, File.ReadLines(inputFileName).Skip(4).ToList());

            textToTranslate = File.ReadAllText($@"{inputFileName}").Replace("&nbsp;", "");

            
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

        private static async Task ListFiles()
        {
            string downloadsFolder = ConfigurationManager.AppSettings["downloadsFolder"];
            Console.WriteLine($"Downloads folder : {downloadsFolder}");

            // Check if the folder exists
            if (Directory.Exists(downloadsFolder))
            {

                string[] files = Directory.GetFiles(downloadsFolder, "*.vtt");

                Console.WriteLine($"Number of VTT files in the directory : {files.Length}");

                // Sort the files by creation date in descending order
                vttFiles = files.OrderByDescending(file => File.GetCreationTime(file)).Take(5).ToList();

                
                // Display the files with their name and creation date
                Console.WriteLine();
                Console.WriteLine("The top 5 files sorted by creation date are:");
                int index = 1;
                foreach (var file in vttFiles)
                {
                    Console.WriteLine($"{index}. {Path.GetFileName(file)} ({File.GetCreationTime(file).ToString("F")})");
                    index++;
                }
            }
            else
            {
                Console.WriteLine($"Folder {downloadsFolder} does not exist. Please check the configuration");
            }
        }

        private static async Task SelectFileToTranslate()
        {
            Console.WriteLine();
            Console.WriteLine("Please select the file to translate (1-5):");
            int choice = int.Parse(Console.ReadLine());

            // Validate the user input
            if (choice < 1 || choice > 5)
            {
                Console.WriteLine("Invalid input. Please try again.");
                await SelectFileToTranslate();
                return;
            }

            // Get the selected video from the list
            selectedFile = vttFiles[choice - 1];

            // Display the selected video information
            Console.WriteLine($"You selected: {selectedFile}");
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
            //Console.WriteLine(result);
            dynamic jsonResult = JsonConvert.DeserializeObject(result);
            Console.WriteLine($"Writing {languageValue} converted output");

            string convertedOutput = jsonResult.First.translations.First.text;
            // Console.WriteLine(convertedOutput);

            //string outputDirectory = @"C:\Users\niles\Downloads\TranslatedOutput";

            //string destinationFolder = ConfigurationManager.AppSettings["destinationFolder"];
            Console.WriteLine($"Destination folder : {destinationFolder}");

            string webVttPrefix = string.Concat("WEBVTT", Environment.NewLine, Environment.NewLine);

            string finalText = string.Concat(webVttPrefix, convertedOutput);

            string fileNameWithExtension = string.Concat($@"{fileName}-{languageValue}", ".vtt");
            
            string completeFileName = Path.Combine(destinationFolder, fileNameWithExtension);

            File.WriteAllText(completeFileName, finalText);
            
            Console.WriteLine($"Translated subtitles for {languageValue} saved successfully");
            Console.WriteLine();
        }

        private static void BuildRequest(string textToTranslate, string route, HttpRequestMessage request)
        {
            // EnvironemntVariableTarget.Machine is to be used on Windows only. 
            // Replace the below line with the Mac equivalent when running on MacOS

            // string apiKey = Environment.GetEnvironmentVariable("TranslatorAPIKey", EnvironmentVariableTarget.Machine);
            string apiKey = Environment.GetEnvironmentVariable("TranslatorAPIKey", EnvironmentVariableTarget.Process);

            // Console.WriteLine($"API Key : {apiKey}");
            
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
