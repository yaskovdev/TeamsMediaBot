namespace BrowserAudioVideoCapturingService;

public class StartRecordingSettings
{
    public bool Video => true;

    public bool Audio => true;

    public int TimeSliceMs => 10;

    public string MimeType => $"video/webm;codecs=\"{Constants.VideoEncoder},{Constants.AudioEncoder}\"";

    public VideoConstraints VideoConstraints { get; }

    public StartRecordingSettings(int width, int height, int frameRate)
    {
        VideoConstraints = new VideoConstraints(width, height, frameRate);
    }
}
