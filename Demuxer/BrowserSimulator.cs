namespace Demuxer;

public class BrowserSimulator
{
    private readonly IDemuxer _demuxer;

    public BrowserSimulator(IDemuxer demuxer)
    {
        _demuxer = demuxer;
    }

    public void StartProducingMedia()
    {
        for (var i = 0; i < 43; i++)
        {
            var bytes = File.ReadAllBytes(@$"c:\dev\experiment3\chunks\chunk_{i:D7}");
            _demuxer.WritePacket(bytes);
            Thread.Sleep(1000);
        }
    }
}
