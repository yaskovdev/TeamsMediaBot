namespace TeamsMediaBot;

using Microsoft.Skype.Bots.Media;
using AppLogLevel = LogLevel;

public class MediaPlatformLogger : IMediaPlatformLogger
{
    private readonly ILogger<MediaPlatformLogger> _logger;

    public MediaPlatformLogger(ILogger<MediaPlatformLogger> logger)
    {
        _logger = logger;
    }

    public void WriteLog(LogLevel level, string logStatement)
    {
        var logLevel = Map(level);
        if (logLevel is AppLogLevel.Warning or AppLogLevel.Error)
        {
            _logger.Log(logLevel, "{Message}", logStatement);
        }
    }

    private static AppLogLevel Map(LogLevel source) =>
        source switch
        {
            LogLevel.Error => AppLogLevel.Error,
            LogLevel.Warning => AppLogLevel.Warning,
            LogLevel.Information => AppLogLevel.Information,
            LogLevel.Verbose => AppLogLevel.Debug,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
}
