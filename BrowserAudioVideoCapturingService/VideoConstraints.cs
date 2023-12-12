namespace BrowserAudioVideoCapturingService;

public class VideoConstraints
{
    public MandatoryVideoConstraints Mandatory { get; }

    public VideoConstraints(int width, int height, int frameRate)
    {
        Mandatory = new MandatoryVideoConstraints(width, height, frameRate);
    }
}
