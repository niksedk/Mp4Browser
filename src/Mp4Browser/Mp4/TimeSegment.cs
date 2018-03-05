namespace Mp4Browser.Mp4
{
    public class TimeSegment
    {
        public uint? Duration { get; set; }
        public uint? TimeOffset { get; set; }
        public ulong BaseMediaDecodeTime { get; set; }
    }
}