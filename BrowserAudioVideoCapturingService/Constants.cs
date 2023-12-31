﻿namespace BrowserAudioVideoCapturingService;

// TODO: can we increase Width, Height and / or FrameRate? Will if affect CPU consumption much? Or it is mostly VideoEncoder that is affecting it?
public static class Constants
{
    /// <summary>
    /// See https://developer.mozilla.org/en-US/docs/Web/Media/Formats/codecs_parameter#avc_profiles.
    /// </summary>
    public const string VideoEncoder = "avc1.424028";

    // TODO: what audio codecs does InputComponent expect (previously it was absent; InputComponent wanted aac)?
    public const string AudioEncoder = "opus";
}
