using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("YouTube Data API: My Uploads");
        Console.WriteLine("============================");

        try
        {
             Run().Wait();
        }
        catch (AggregateException ex)
        {
            foreach (var e in ex.InnerExceptions)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static async Task Run()
    {
        UserCredential credential;
        using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                // This OAuth 2.0 access scope allows for read-only access to the authenticated 
                // user's account, but not other types of account access.
                new[] { YouTubeService.Scope.YoutubeReadonly },
                "user",
                CancellationToken.None,
                new FileDataStore(this.GetType().ToString())
            );
        }

        var youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = this.GetType().ToString()
        });

        var channelsListRequest = youtubeService.Channels.List("contentDetails");
        channelsListRequest.Mine = true;

        // Retrieve the contentDetails part of the channel resource for the authenticated user's channel.
        var channelsListResponse = await channelsListRequest.ExecuteAsync();

        foreach (var channel in channelsListResponse.Items)
        {
            // From the API response, extract the playlist ID that identifies the list
            // of videos uploaded to the authenticated user's channel.
            var uploadsListId = channel.ContentDetails.RelatedPlaylists.Uploads;

            Console.WriteLine("Videos in list {0}", uploadsListId);

            var nextPageToken = "";
            while (nextPageToken != null)
            {
                var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
                playlistItemsListRequest.PlaylistId = uploadsListId;
                playlistItemsListRequest.MaxResults = 50;
                playlistItemsListRequest.PageToken = nextPageToken;

                // Retrieve the list of videos uploaded to the authenticated user's channel.
                var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

                foreach (var playlistItem in playlistItemsListResponse.Items)
                {
                    // Print information about each video.
                    Console.WriteLine("{0} ({1})", playlistItem.Snippet.Title, playlistItem.Snippet.ResourceId.VideoId);
                }

                nextPageToken = playlistItemsListResponse.NextPageToken;
            }

        }
    }

    async Task addVideoCaption(string videoID) //pass your video id here..
        {
            UserCredential credential;
            //you should go out and get a json file that keeps your information... You can get that from the developers console...
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { YouTubeService.Scope.YoutubeForceSsl, YouTubeService.Scope.Youtube, YouTubeService.Scope.Youtubepartner },
                    "ACCOUNT NAME HERE",
                    CancellationToken.None,
                    new FileDataStore(this.GetType().ToString())
                );
            }
            //creates the service...
            var youtubeService = new Google.Apis.YouTube.v3.YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = this.GetType().ToString(),
            });

            //create a CaptionSnippet object...
            CaptionSnippet capSnippet = new CaptionSnippet();
            capSnippet.Language = "en";
            capSnippet.Name = videoID + "_Caption";
            capSnippet.VideoId = videoID;
            capSnippet.IsDraft = false;

            //create new caption object
            Caption caption = new Caption();

            //set the completed snippet to the object now...
            caption.Snippet = capSnippet;

            //here we read our .srt which contains our subtitles/captions...
            using (var fileStream = new FileStream("filepathhere", FileMode.Open))
            {
                //create the request now and insert our params...
                var captionRequest = youtubeService.Captions.Insert(caption, "snippet", fileStream, "application/atom+xml");

                //finally upload the request... and wait.
                await captionRequest.UploadAsync();
            }

        }
    
}

       