namespace Mp4Browser.Mp4.Cea608
{
    public class PenState
    {
        public string Foreground { get; set; } = Constants.COLOR_WHITE;
        public bool Underline;
        public bool Italics { get; set; }
        public string Background { get; set; } = Constants.COLOR_BLACK;
        public bool Flash { get; set; }

        public PenState()
        {
        }

        public PenState(string foreground, bool underline, bool italics, string background, bool flash1)
        {
            Foreground = foreground;
            Underline = underline;
            Italics = italics;
            Background = background;
            Flash = flash1;
        }

        public void Reset()
        {
            Foreground = Constants.COLOR_WHITE;
            Underline = false;
            Italics = false;
            Background = Constants.COLOR_BLACK;
            Flash = false;
        }

        public CcStyle Serialize()
        {
            return new CcStyle
            {

                Foreground = Foreground,
                Underline = Underline,
                Italics = Italics,
                Flash = Flash,
                Background = Background
            };
        }

        public void Copy(PenState newPenState)
        {
            Foreground = newPenState.Foreground;
            Underline = newPenState.Underline;
            Italics = newPenState.Italics;
            Background = newPenState.Background;
            Flash = newPenState.Flash;
        }

        public bool Equals(PenState other)
        {
            return Foreground == other.Foreground &&
                   Underline == other.Underline &&
                   Italics == other.Italics &&
                   Background == other.Background &&
                   Flash == other.Flash;
        }

        public bool IsDefault()
        {
            return Foreground == Constants.COLOR_WHITE && !Underline && !Italics && Background == Constants.COLOR_BLACK && !Flash;
        }

        public void SetStyles(SerializedPenState styles)
        {
            if (styles.Foreground != null)
            {
                Foreground = styles.Foreground;
            }
            if (styles.Underline != null)
            {
                Underline = styles.Underline.Value;
            }
            if (styles.Background != null)
            {
                Background = styles.Background;
            }
            if (styles.Flash != null)
            {
                Flash = styles.Flash.Value;
            }
            if (styles.Italics != null)
            {
                Italics = styles.Italics.Value;
            }
        }
    }
}
