using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Collections;
using System.Configuration;
using Google.Apis.Services;

class Program
{
    // The YouTube service object
    private static YouTubeService youtubeService;

    // The list of videos to display
    private static List<Video> videos;

    // The selected video
    private static Video selectedVideo;

    static async Task Main(string[] args)
    {
        // Create the YouTube service
        youtubeService = await CreateYouTubeService();

        // List the most recent 5 videos
        await ListVideos();

        // Allow user to select one of the videos
        Console.WriteLine();
        await SelectVideo();

        // Download the subtitle in English language for the selected video
        Console.WriteLine();
        await DownloadSubtitle();
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
            ApplicationName = "YoutubeSubtitleDownloader"
        });

        return youtubeService;
    }

    // List the most recent 5 videos with the video name and id
        private static async Task ListVideos()
        {
            // Create a search request with the following parameters:
            // - Order by date (most recent first)
            // - Type: video
            // - Max results: 5
            var searchRequest = youtubeService.Search.List("snippet");
            searchRequest.ChannelId = Environment.GetEnvironmentVariable("YOUTUBE_CHANNEL_ID");
            searchRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
            searchRequest.Type = "video";
            searchRequest.MaxResults = 5;

            var searchResponse = await searchRequest.ExecuteAsync();

            videos = searchResponse.Items.Select(item => new Video()
            {
                Id = item.Id.VideoId,
                Title = item.Snippet.Title
            }).ToList();

            Console.WriteLine("The most recent 5 videos are:");
            for (int i = 0; i < videos.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {videos[i].Id} - {videos[i].Title}");
            }
        }

        // Allow user to select one of the videos
        private static async Task SelectVideo()
        {
            // Prompt the user to enter the number of the video they want to select
            Console.WriteLine("Enter the number of the video you want to select (1-5):");
            int choice = int.Parse(Console.ReadLine());

            // Validate the user input
            if (choice < 1 || choice > 5)
            {
                Console.WriteLine("Invalid input. Please try again.");
                await SelectVideo();
                return;
            }

            // Get the selected video from the list
            selectedVideo = videos[choice - 1];

            // Display the selected video information
            Console.WriteLine($"You selected: {selectedVideo.Title} ({selectedVideo.Id})");
        }

        // Download the subtitle in English language for the selected video
        private static async Task DownloadSubtitle()
        {
            // Create a caption request with the following parameters:
            // - Video id: the selected video id
            // - Filter: only include captions that are publicly available
            var captionRequest = youtubeService.Captions.List("snippet", selectedVideo.Id);
            // captionRequest.VideoId = selectedVideo.Id;
            // captionRequest.OnBehalfOfContentOwner = "";

            // Execute the caption request and get the response
            var captionResponse = await captionRequest.ExecuteAsync();
            
            // Find the caption track that has the language code "en" (English)
            // and is a standard track (not auto-generated or auto-synced)
            // and is not a draft
            // and is serving
            var captionTrack = captionResponse.Items.FirstOrDefault(
                item => item.Snippet.Language == "en" 
                && item.Snippet.TrackKind == "standard" 
                && item.Snippet.IsDraft == false
                && item.Snippet.Status == "serving");
            
            // Check if the caption track exists
            if (captionTrack == null)
            {
                Console.WriteLine("No English subtitle available for this video.");
                return;
            }

            // Create a download request with the following parameters:
            // - Caption id: the caption track id
            // - Tfmt: the format of the subtitle file (vtt or srt)
            var captionsResource = new CaptionsResource(youtubeService);
            var downloadRequest = youtubeService.Captions.Download(captionTrack.Id);
            // downloadRequest.Id = captionTrack.Id;
            downloadRequest.Tfmt = "vtt";

            string downloadsFolder = ConfigurationManager.AppSettings["downloadsFolder"];

            string fileName = Path.Combine(downloadsFolder, $"{selectedVideo.Id}.vtt");

            // Execute the download request and get the response stream
            using (var responseStream = await downloadRequest.ExecuteAsStreamAsync())
            {
                // Create a file stream to write the subtitle to a local file
                using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    // Copy the response stream to the file stream
                    await responseStream.CopyToAsync(fileStream);
                }
            }

            // Display the download confirmation
            Console.WriteLine($"Subtitle downloaded to {fileName}");
        }

}
 // A class to represent a video with its id and title
    class Video
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }

       