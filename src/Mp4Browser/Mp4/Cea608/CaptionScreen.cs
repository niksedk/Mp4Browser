﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Mp4Browser.Mp4.Cea608
{
    public class CaptionScreen
    {
        public Row[] Rows = new Row[]
        {
            new Row(),
            new Row(),
            new Row(),
            new Row(),
            new Row(),
            new Row(),
            new Row(),
            new Row(),
            new Row(),
            new Row(),
            new Row(),
            new Row(),
            new Row(),
            new Row(),
            new Row(),
        };

        public int CurrentRow = 14;
        public int? nrRollUpRows;
        public bool roll;

        public CaptionScreen()
        {
            Reset();
        }

        public SerializedRow[] Serialize()
        {
            var results = new List<SerializedRow>();
            for (var i = 0; i < Constants.SCREEN_ROW_COUNT; i++)
            {
                var row = Rows[i];
                if (row.IsEmpty())
                {
                    continue;
                }

                results.Add(new SerializedRow
                {
                    Row = i,
                    Position = row.FirstNonEmpty(),
                    Style = row.CurrentPenState.Serialize(),
                    Columns = row.chars.Select(SerializeChar).ToArray()
                });
            }
            return results.ToArray();
        }

        private SerializedStyledUnicodeChar SerializeChar(StyledUnicodeChar character)
        {
            return new SerializedStyledUnicodeChar
            {
                Character = character.Uchar,
                Style = character.PenState.Serialize(),
            };
        }

        public void Reset()
        {
            for (var i = 0; i < Constants.SCREEN_ROW_COUNT; i++)
            {
                Rows[i].Clear();
            }
            CurrentRow = Constants.SCREEN_ROW_COUNT - 1;
        }

        public bool Equals(CaptionScreen other)
        {
            var equal = true;
            for (var i = 0; i < Constants.SCREEN_ROW_COUNT; i++)
            {
                if (!Rows[i].Equals(other.Rows[i]))
                {
                    equal = false;
                    break;
                }
            }

            return equal;
        }

        public void Copy(CaptionScreen other)
        {
            for (var i = 0; i < Constants.SCREEN_ROW_COUNT; i++)
            {
                Rows[i].Copy(other.Rows[i]);
            }
        }

        public bool IsEmpty()
        {
            var empty = true;
            for (var i = 0; i < Constants.SCREEN_ROW_COUNT; i++)
            {
                if (!Rows[i].IsEmpty())
                {
                    empty = false;
                    break;
                }
            }
            return empty;
        }

        public void BackSpace()
        {
            Rows[CurrentRow].BackSpace();
        }

        public void ClearToEndOfRow()
        {
            Rows[CurrentRow].ClearToEndOfRow();
        }

        public void InsertChar(int character)
        {
            Rows[CurrentRow].InsertChar(character);
        }

        public void SetPen(SerializedPenState styles)
        {
            Rows[CurrentRow].SetPenStyles(styles);
        }

        public void MoveCursor(int relPos)
        {
            Rows[CurrentRow].MoveCursor(relPos);
        }

        public void SetPac(PacData pacData)
        {
            var newRow = pacData.Row - 1;
            CurrentRow = newRow;
            var row = Rows[CurrentRow];
            if (pacData.Indent != null)
            {
                var indent = pacData.Indent;
                var prevPos = Math.Max(indent.Value - 1, 0);
                row.Position = pacData.Indent.Value;
                pacData.Color = row.chars[prevPos].PenState.Foreground;
            }

            SetPen(new SerializedPenState
            {
                Foreground = pacData.Color ?? Constants.COLOR_WHITE,
                Underline = pacData.Underline,
                Italics = pacData.Italics ?? false,
                Background = Constants.COLOR_BLACK,
                Flash = false,
            });
        }

        public void SetBkgData(SerializedPenState bkgData)
        {
            BackSpace();
            SetPen(bkgData);
            InsertChar(0x20); // Space
        }

        public void SetRollUpRows(int nrRows)
        {
            nrRollUpRows = nrRows;
        }

        public void RollUp()
        {
            // if the row is empty we have nothing to roll-up
            if (nrRollUpRows == null || Rows[CurrentRow].IsEmpty())
            {
                return;
            }

            var rows = Rows.ToList();
            rows.RemoveAt(CurrentRow - nrRollUpRows.Value + 1);
            rows.Add(new Row());
            Rows = rows.ToArray();
        }
    }
}
