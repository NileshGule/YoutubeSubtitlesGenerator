using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Collections;
using System.Configuration;
using Google.Apis.Upload;

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

        YouTubeService youtubeService = await CreateYouTubeService();

        var searchRequest = youtubeService.Videos.List("snippet");
        searchRequest.Id = videoId;
        var searchResponse = await searchRequest.ExecuteAsync();

        Console.WriteLine($"Total videos found: {searchResponse.Items.Count}");

        var youTubeVideo = searchResponse.Items.FirstOrDefault();

        if (youTubeVideo != null)
        {
            var captions = youtubeService.Captions.List("id,snippet", videoId).Execute();
            
            var currentSubtitles =
                captions.Items.Select(c => c.Snippet.Language.ToLower())
                                         .ToList();

            currentSubtitles.ForEach(subtitle => Console.WriteLine($"Subtitle language: {subtitle}"));

            Console.WriteLine($"Current subtitles count: {currentSubtitles.Count}");

            // Get the list of language codes
            var translatedSubtitles = languageCodeMap.Keys.ToList();

            var missingSubtitles = translatedSubtitles.Except(currentSubtitles).ToList();

            Console.WriteLine();
            Console.WriteLine($"Missing subtitles count: {missingSubtitles.Count}");

            // Google Data API is able to upload max of 26 Subtitle languages ata time
            // limit the max number to 25 subtitles in one go

            const int MAX_SUBTITLES_BATCH_SIZE = 25;

            // Add missing subtitles
            foreach (var subtitle in missingSubtitles.Take(MAX_SUBTITLES_BATCH_SIZE))
            {
                languageCode = subtitle;
                languageName = languageCodeMap[subtitle];

                string translatedFileName = $@"{translationFolder}/{fileName}-{languageName}.vtt";

                Console.WriteLine($"Uploading {translatedFileName} for language {languageName}");

                await AddVideoCaption(videoId, languageCode, languageName, translatedFileName);
            }
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
        YouTubeService youtubeService = await CreateYouTubeService();

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



        //here we read our .srt which contains our subtitles/captions...
        using (var fileStream = new FileStream($@"{subtitleFileName}", FileMode.Open))
        {
            //create the request now and insert our params...
            var captionRequest = youtubeService.Captions.Insert(caption, "snippet", fileStream, "application/atom+xml");

            captionRequest.ProgressChanged += CaptionRequest_ProgressChanged;
            captionRequest.ResponseReceived += CaptionRequest_ResponseReceived;

            //finally upload the request... and wait.
            await captionRequest.UploadAsync();

            Console.WriteLine();
            Console.WriteLine($"Uploaded {subtitleFileName}, in {languageName}");
        }


    }

    private static async Task<YouTubeService> CreateYouTubeService()
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

        return youtubeService;
    }

    private static void CaptionRequest_ResponseReceived(Caption caption)
    {
        Console.WriteLine();
        Console.WriteLine($"{caption.Snippet.Language} caption status = {caption.Snippet.Status}");
    }

    private static void CaptionRequest_ProgressChanged(IUploadProgress progress)
    {
        switch (progress.Status)
        {
            case UploadStatus.Failed:
                Console.WriteLine();
                Console.WriteLine($"An error occured while uploading the subtitles. {Environment.NewLine}{progress.Exception}");
                break;
        }
    }
}

       