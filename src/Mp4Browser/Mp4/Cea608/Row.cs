namespace Mp4Browser.Mp4.Cea608
{
    public class Row
    {
        public int Position { get; set; }

        public PenState CurrentPenState = new PenState();

        public StyledUnicodeChar[] chars = new[]
        {
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
            new StyledUnicodeChar(),
        };

        public bool Equals(Row other)
        {
            for (var i = 0; i < Constants.SCREEN_COL_COUNT; i++)
            {
                if (!chars[i].Equals(other.chars[i]))
                {
                    if (!chars[i].Equals(other.chars[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void Copy(Row other)
        {
            for (var i = 0; i < Constants.SCREEN_COL_COUNT; i++)
            {
                chars[i].Copy(other.chars[i]);
            }
        }

        public int FirstNonEmpty()
        {
            for (var i = 0; i < Constants.SCREEN_COL_COUNT; i++)
            {
                if (!chars[i].IsEmpty())
                {
                    return i;
                }
            }

            return -1;
        }

        public bool IsEmpty()
        {
            for (var i = 0; i < Constants.SCREEN_COL_COUNT; i++)
            {
                if (!chars[i].IsEmpty())
                {
                    return false;
                }
            }

            return true;
        }

        public void MoveCursor(int relPos)
        {
            var newPos = Position + relPos;
            if (relPos > 1)
            {
                for (var i = Position + 1; i < newPos + 1; i++)
                {
                    chars[i].SetPenState(CurrentPenState);
                }
            }

            Position = newPos;
        }

        public void BackSpace()
        {
            MoveCursor(-1);
            chars[Position].SetChar(Constants.EMPTY_CHAR, CurrentPenState);
        }

        public void InsertChar(int b)
        {
            if (b >= 0x90)
            { // Extended char
                BackSpace();
            }

            var ch = GetCharForByte(b);
            chars[Position].SetChar(ch, CurrentPenState);
            MoveCursor(1);
        }

        /// <summary>
        /// Get Unicode Character from CEA-608 byte code.
        /// </summary>
        public static string GetCharForByte(int byteValue)
        {
            if (Constants.EXTENDED_CHAR_CODES.TryGetValue(byteValue, out var v))
            {
                return char.ConvertFromUtf32(v);
            }

            return char.ConvertFromUtf32(byteValue);
        }

        public void ClearFromPos(int startPos)
        {
            for (var i = startPos; i < Constants.SCREEN_COL_COUNT; i++)
            {
                chars[i].Reset();
            }
        }

        public void Clear()
        {
            ClearFromPos(0);
            Position = 0;
            CurrentPenState.Reset();
        }

        public void ClearToEndOfRow()
        {
            ClearFromPos(Position);
        }

        public void SetPenStyles(SerializedPenState styles)
        {
            CurrentPenState.SetStyles(styles);
            var currChar = chars[Position];
            currChar.SetPenState(CurrentPenState);
        }
    }
}
