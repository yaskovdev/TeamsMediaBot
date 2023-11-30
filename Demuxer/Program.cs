namespace Demuxer;

internal static class Program
{
    public static async Task Main()
    {
        using var demuxer = new Demuxer();
        var producer = new Thread(() =>
        {
            var simulator = new BrowserSimulator(demuxer);
            simulator.StartProducingMedia();
        });
        producer.Start();

        await using var outputVideoStream = File.Create(@"c:\dev\experiment3\capture.video");
        await using var outputAudioStream = File.Create(@"c:\dev\experiment3\capture.audio");
        while (true)
        {
            var frame = demuxer.ReadFrame();
            if (frame.Data.Count == 0)
            {
                break;
            }
            Console.WriteLine($"Extracted frame of type {frame.Type} with size {frame.Data.Count} and timestamp {frame.Timestamp}");
            await PickStreamFor(frame, outputVideoStream, outputAudioStream).WriteAsync(frame.Data);
        }
        producer.Join();
    }

    private static Stream PickStreamFor(Frame frame, Stream videoStream, Stream audioStream) =>
        frame.Type switch
        {
            FrameType.Video => videoStream,
            FrameType.Audio => audioStream,
            _ => Stream.Null
        };
}
