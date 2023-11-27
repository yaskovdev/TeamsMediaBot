namespace TeamsMediaBot.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Skype.Bots.Media;
using Services;

[ApiController]
[Route("/api")]
public class JoinCallController : ControllerBase
{
    private readonly ITeamsMediaBotService _service;
    private readonly ILogger<JoinCallController> _logger;

    // TODO: implement streaming properly
    private static bool _ready;

    public JoinCallController(ITeamsMediaBotService service, ILogger<JoinCallController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("/join-call")]
    public async Task Get()
    {
        var videoSocket = await _service.JoinCall(new Uri("https://teams.microsoft.com/l/meetup-join/19%3ameeting_MDA1NDJjZDgtNDRhYy00MGY4LWE2YzQtMjI1YzFlNTAzYzMw%40thread.v2/0?context=%7b%22Tid%22%3a%2272f988bf-86f1-41af-91ab-2d7cd011db47%22%2c%22Oid%22%3a%22b1b11b68-1839-4792-a462-1854254ddfe8%22%2c%22MessageId%22%3a%220%22%7d"));
        videoSocket.VideoSendStatusChanged += OnVideoSendStatusChanged;
        long timestamp = 0;
        while (true)
        {
            if (_ready)
            {
                for (var i = 0;; i++)
                {
                    try
                    {
                        var packet = await System.IO.File.ReadAllBytesAsync($@"c:\dev\experiment\{i + 1:D7}.raw");
                        videoSocket.Send(new VideoBuffer(packet, timestamp));
                        timestamp += 1000;
                    }
                    catch (FileNotFoundException)
                    {
                        var isFirstFrameMissing = i == 0;
                        if (isFirstFrameMissing)
                        {
                            throw;
                        }
                        break;
                    }
                }
            }
        }
    }

    private void OnVideoSendStatusChanged(object? sender, VideoSendStatusChangedEventArgs args)
    {
        _logger.LogInformation("New socket status: {Status}", args.MediaSendStatus);
        _ready = args.MediaSendStatus == MediaSendStatus.Active;
    }
}
