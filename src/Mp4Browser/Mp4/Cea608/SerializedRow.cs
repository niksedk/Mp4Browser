namespace Mp4Browser.Mp4.Cea608
{
    public class SerializedRow
    {
        public int Row { get; set; }

        /// <summary>
        /// Column indentation.
        /// </summary>
        public int Position { get; set; } 

        public CcStyle Style { get; set; }
        public SerializedStyledUnicodeChar[] Columns { get; set; }
    }
}
