using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections;
using System.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        string videoId;
        string fileName;
        string languageCode;
        string languageName;

        if (args.Length != 2)
        {
            Console.WriteLine($"Missing input parameters. {Environment.NewLine} Cannot continue without the Youtube video ID and input file.");
            return;
        }
        else
        {
            videoId = args[0];

            fileName = args[1];
            Console.WriteLine($"Input file name : {fileName}");
        }

        Dictionary<string, string> languageCodeMap = GetLanguageCodeMapping();

        string translationFolder = ConfigurationManager.AppSettings["translationFolder"];
        Console.WriteLine($"Directory with translated files: {translationFolder}");

        foreach (KeyValuePair<string, string> languageSetting in languageCodeMap)
        {
            languageCode = languageSetting.Key;
            languageName = languageSetting.Value;

            string translatedFileName = $@"{translationFolder}\{fileName}-{languageName}.vtt";

            await AddVideoCaption(videoId, languageCode, languageName, translatedFileName);
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static Dictionary<string, string> GetLanguageCodeMapping()
    {
        var section = (Hashtable)ConfigurationManager.GetSection("CodeLanguageMapping");

        Dictionary<string, string> codeLanguageMap = section.Cast<DictionaryEntry>().ToDictionary(d => (string)d.Key, d => (string)d.Value);

        return codeLanguageMap;

    }
    static async Task AddVideoCaption(string videoID, string languageCode, string languageName, string subtitleFileName) //pass your video id here..
    {
        UserCredential credential;
        
        //you should go out and get a json file that keeps your information... You can get that from the developers console...
        using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { YouTubeService.Scope.YoutubeForceSsl, YouTubeService.Scope.Youtube, YouTubeService.Scope.Youtubepartner },
                "user",
                CancellationToken.None
            );
        }
        //creates the service...
        var youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "YoutubeSubtitleUploader"
        });

        // updated mismatched language codes between Microsoft Translator and Youtube API
        if (languageCode == "zh-Hans")
            languageCode = "zh-CN";
        if (languageCode == "pt-pt")
            languageCode = "pt-PT";

        //create a CaptionSnippet object...
        CaptionSnippet capSnippet = new()
        {
            Language = languageCode,
            Name = languageName,
            VideoId = videoID,
            IsDraft = false
        };

        //create new caption object and set the completed snippet
        Caption caption = new()
        {
            Snippet = capSnippet
        };
              

        try
        {
            //here we read our .srt which contains our subtitles/captions...
            using (var fileStream = new FileStream($@"{subtitleFileName}", FileMode.Open))
            {
                //create the request now and insert our params...
                var captionRequest = youtubeService.Captions.Insert(caption, "snippet", fileStream, "application/atom+xml");

                //finally upload the request... and wait.
                await captionRequest.UploadAsync();

                Console.WriteLine();
                Console.WriteLine($"Uploaded {subtitleFileName}, in {languageName}");
            }
        }
        catch
        {
            Console.WriteLine();
            Console.WriteLine($"There was some problem Uploading {subtitleFileName}, in {languageName}");
        }

    }
    
}

       