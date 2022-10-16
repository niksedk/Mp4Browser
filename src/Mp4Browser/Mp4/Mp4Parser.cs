using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Mp4Browser.Mp4.Boxes;
using System.Text;

namespace Mp4Browser.Mp4
{
    /// <summary>
    /// http://wiki.multimedia.cx/index.php?title=QuickTime_container
    /// http://standards.iso.org/ittf/PubliclyAvailableStandards/c061988_ISO_IEC_14496-12_2012.zip
    /// </summary>
    public class Mp4Parser : Box
    {
        public string FileName { get; }
        internal Moov Moov { get; private set; }
        internal Moof Moof { get; private set; }

        public List<Trak> GetSubtitleTracks()
        {
            var list = new List<Trak>();
            if (Moov?.Tracks != null)
            {
                foreach (var trak in Moov.Tracks)
                {
                    if (trak.Mdia != null && (trak.Mdia.IsTextSubtitle || trak.Mdia.IsVobSubSubtitle || trak.Mdia.IsClosedCaption) && trak.Mdia.Minf?.Stbl != null)
                    {
                        list.Add(trak);
                    }
                }
            }
            return list;
        }

        public List<Trak> GetAudioTracks()
        {
            var list = new List<Trak>();
            if (Moov?.Tracks != null)
            {
                foreach (var trak in Moov.Tracks)
                {
                    if (trak.Mdia != null && trak.Mdia.IsAudio)
                    {
                        list.Add(trak);
                    }
                }
            }
            return list;
        }

        public List<Trak> GetVideoTracks()
        {
            var list = new List<Trak>();
            if (Moov?.Tracks != null)
            {
                foreach (var trak in Moov.Tracks)
                {
                    if (trak.Mdia != null && trak.Mdia.IsVideo)
                    {
                        list.Add(trak);
                    }
                }
            }
            return list;
        }

        public TimeSpan Duration
        {
            get
            {
                if (Moov?.Mvhd != null && Moov.Mvhd.TimeScale > 0)
                    return TimeSpan.FromSeconds((double)Moov.Mvhd.Duration / Moov.Mvhd.TimeScale);
                return new TimeSpan();
            }
        }

        public DateTime CreationDate
        {
            get
            {
                if (Moov?.Mvhd != null && Moov.Mvhd.TimeScale > 0)
                    return new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc).Add(TimeSpan.FromSeconds(Moov.Mvhd.CreationTime));
                return DateTime.Now;
            }
        }

        /// <summary>
        /// Resolution of first video track. If not present returns 0.0
        /// </summary>
        public System.Drawing.Point VideoResolution
        {
            get
            {
                if (Moov?.Tracks != null)
                {
                    foreach (var trak in Moov.Tracks)
                    {
                        if (trak?.Mdia != null && trak.Tkhd != null)
                        {
                            if (trak.Mdia.IsVideo)
                                return new System.Drawing.Point((int)trak.Tkhd.Width, (int)trak.Tkhd.Height);
                        }
                    }
                }
                return new System.Drawing.Point(0, 0);
            }
        }

        public Mp4Parser(string fileName, TreeView treeView)
        {
            FileName = fileName;
            using (var fs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                ParseMp4(fs, treeView);
                fs.Close();
            }
        }

        public int SaveMdats(string firstFileName)
        {
            int mdatCount = 0;
            using (var fs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int count = 0;
                Position = 0;
                fs.Seek(0, SeekOrigin.Begin);
                bool moreBytes = true;
                while (moreBytes)
                {
                    moreBytes = InitializeSizeAndName(fs);
                    if (Size < 8)
                    {
                        return mdatCount;
                    }

                    if (Name == "moov")
                    {

                    }
                    else if (Name == "mdat")
                    {
                        var before = fs.Position;
                        var readLength = (long)Position - before;
                        var buffer = new byte[readLength];
                        fs.Read(buffer, 0, (int)readLength);
                        var s = Encoding.UTF8.GetString(buffer);
                        var fName = Path.GetFileNameWithoutExtension(firstFileName);
                        mdatCount++;
                        fName = Path.Combine(Path.GetDirectoryName(firstFileName), $"{fName}-{mdatCount:00}.xml");
                        File.WriteAllText(fName, s, Encoding.UTF8);
                    }
                    else if (Name == "moof")
                    {
                    }
                    count++;
                    if (count > 1000)
                    {
                        break;
                    }

                    if (Position > (ulong)fs.Length)
                    {
                        break;
                    }

                    fs.Seek((long)Position, SeekOrigin.Begin);
                }

                fs.Close();
            }
            return mdatCount;
        }

        public Mp4Parser(FileStream fs, TreeView treeView)
        {
            FileName = null;
            ParseMp4(fs, treeView);
        }

        private void ParseMp4(FileStream fs, TreeView treeView)
        {
            var samples = new List<TimeSegment>();
            var payloads = new List<string>();
            var count = 0;
            Position = 0;
            fs.Seek(0, SeekOrigin.Begin);
            var moreBytes = true;
            while (moreBytes)
            {
                moreBytes = InitializeSizeAndName(fs);
                if (Size < 8)
                {
                    return;
                }

                if (Name == "moov")
                {
                    var node = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Movie" + Environment.NewLine +
                              "Size: " + Size
                    };
                    treeView?.Nodes.Add(node);
                    Moov = new Moov(fs, Position, node);
                }
                else if (Name == "mdat")
                {
                    var node = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Media Data" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };
                    treeView?.Nodes.Add(node);
                    var mdat = new Mdat(fs, Position, node);
                    if (mdat.Payloads.Count > 0)
                    {
                        payloads.AddRange(mdat.Payloads);
                    }

                    node.Text += $" (Count={node.Nodes.Count})";
                }
                else if (Name == "moof")
                {
                    var node = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Movie Fragment" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };
                    treeView?.Nodes.Add(node);
                    Moof = new Moof(fs, Position, node);
                    if (Moof.Traf?.Trun?.Samples.Count > 0)
                    {
                        samples.AddRange(Moof.Traf.Trun.Samples);
                    }
                }
                else if (Name == "sidx")
                {
                    var sidx = new Sidx(fs);
                    var referenceItems = new StringBuilder();
                    for (var index = 0; index < sidx.ReferenceItems.Count; index++)
                    {
                        var item = sidx.ReferenceItems[index];
                        referenceItems.AppendLine($"- ReferenceType[{index}]: {item.ReferenceType}");
                        referenceItems.AppendLine($"- ReferencedSize[{index}]: {item.ReferencedSize}");
                        referenceItems.AppendLine($"- SubSegmentDuration[{index}]: {item.SubSegmentDuration}");
                        referenceItems.AppendLine($"- StartsWithSap[{index}]: {item.StartsWithSap}");
                        referenceItems.AppendLine($"- SapType[{index}]: {item.SapType}");
                        referenceItems.AppendLine($"- SapDeltaTime[{index}]: {item.SapDeltaTime}");
                    }

                    var node = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + GetFriendlyBoxName(Name) + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "RefernceId: " + sidx.ReferenceId + Environment.NewLine +
                              "TimeScale: " + sidx.TimeScale + Environment.NewLine +
                              "EarliestPresentationTime :" + sidx.EarliestPresentationTime + Environment.NewLine +
                              "FirstOffset :" + sidx.FirstOffset + Environment.NewLine +
                              "ReferenceCount:" + sidx.ReferenceCount + Environment.NewLine +
                              referenceItems.ToString()
                    };
                    treeView?.Nodes.Add(node);
                }
                else
                {
                    treeView?.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + GetFriendlyBoxName(Name) + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    });
                }

                count++;
                if (count > 1000)
                    break;

                if (Position > (ulong)fs.Length)
                    break;
                fs.Seek((long)Position, SeekOrigin.Begin);
            }
            fs.Close();
        }

        private static string GetFriendlyBoxName(string name)
        {
            switch (name)
            {
                case "ftyp": return "File Type";
                case "styp": return "Segment Type";
                case "sidx": return "Segment Index";
                default: return string.Empty;
            }
        }

        internal double FrameRate
        {
            get
            {
                // Formula: moov.mdia.stbl.stsz.samplecount / (moov.trak.tkhd.duration / moov.mvhd.timescale) - http://www.w3.org/2008/WebVideo/Annotations/drafts/ontology10/CR/test.php?table=containerMPEG4
                if (Moov?.Mvhd != null && Moov.Mvhd.TimeScale > 0)
                {
                    var videoTracks = GetVideoTracks();
                    if (videoTracks.Count > 0 && videoTracks[0].Tkhd != null && videoTracks[0].Mdia?.Minf?.Stbl != null)
                    {
                        double duration = videoTracks[0].Tkhd.Duration;
                        double sampleCount = videoTracks[0].Mdia.Minf.Stbl.StszSampleCount;
                        return sampleCount / (duration / Moov.Mvhd.TimeScale);
                    }
                }
                return 0;
            }
        }

    }
}
