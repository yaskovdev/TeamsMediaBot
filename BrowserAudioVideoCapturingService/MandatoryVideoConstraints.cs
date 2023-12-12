namespace BrowserAudioVideoCapturingService;

public record MandatoryVideoConstraints(int MinWidth, int MaxWidth, int MinHeight, int MaxHeight, int MinFrameRate, int MaxFrameRate)
{
    public MandatoryVideoConstraints(int Width, int Height, int FrameRate) : this(Width, Width, Height, Height, FrameRate, FrameRate)
    {
    }
}
