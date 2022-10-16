using System;
using System.IO;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Mdia : Box
    {
        public Mdhd Mdhd;
        public Minf Minf;
        public readonly string HandlerType;
        public readonly string HandlerName = string.Empty;

        public bool IsTextSubtitle => HandlerType == "sbtl" || HandlerType == "text";

        public bool IsVobSubSubtitle => HandlerType == "subp";

        public bool IsClosedCaption => HandlerType == "clcp";

        public bool IsVideo => HandlerType == "vide";

        public bool IsAudio => HandlerType == "soun";

        public Mdia(Stream fs, ulong maximumLength, TreeNode root)
        {
            Position = (ulong)fs.Position;
            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                    return;

                if (Name == "minf" && IsTextSubtitle || IsVobSubSubtitle || IsClosedCaption || IsVideo)
                {
                    var minfNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };
                    ulong timeScale = 90000;
                    if (Mdhd != null)
                        timeScale = Mdhd.TimeScale;
                    Minf = new Minf(fs, Position, timeScale, HandlerType, this, minfNode);
                    root.Nodes.Add(minfNode);
                }
                else if (Name == "hdlr")
                {
                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    HandlerType = GetString(8, 4);

                    //Buffer[0]    = 1-byte specification of the version of this handler information.
                    //Buffer[1..3] = 3-byte space for handler information flags. Set this field to 0.
                    string componentType = GetString(4, 4).Replace("\0", string.Empty);
                    string componentSubtype = GetString(8, 4).Replace("\0", string.Empty);

                    HandlerName = componentSubtype;
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              (string.IsNullOrEmpty(componentType) ? "" : "componentType: " + componentType + Environment.NewLine) +
                              "HandlerName: " + HandlerName
                    });
                }
                else if (Name == "mdhd")
                {
                    Mdhd = new Mdhd(fs, Size);
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "LanguageString: " + Mdhd.LanguageString + Environment.NewLine +
                              "Timescale: " + Mdhd.TimeScale
                    });
                }
                fs.Seek((long)Position, SeekOrigin.Begin);
            }
        }

    }
}
