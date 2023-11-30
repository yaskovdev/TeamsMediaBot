namespace BrowserAudioVideoCapturingService;

public class MandatoryVideoConstraints
{
    public int MinWidth => Constants.Width;

    public int MaxWidth => Constants.Width;

    public int MinHeight => Constants.Height;

    public int MaxHeight => Constants.Height;

    public int MinFrameRate => Constants.FrameRate;

    public int MaxFrameRate => Constants.FrameRate;
}
