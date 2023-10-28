namespace Mp4Browser.Mp4.Cea608
{
    public class SerializedRow
    {
        public int Row { get; set; }
        public int Position { get; set; } // col indent
        public CcStyle Style { get; set; }
        public SerializedStyledUnicodeChar[] Columns { get; set; }
    }
}
