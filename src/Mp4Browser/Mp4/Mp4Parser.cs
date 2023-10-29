using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Mp4Browser.Mp4.Boxes;
using System.Text;
using Mp4Browser.Mp4.Cea608;

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
        public List<CcData> TrunAllCcData = new List<CcData>();

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
                        Tag = "Element: " + Name + " - Movie Box" + Environment.NewLine +
                              "Size: " + Size
                    };
                    treeView?.Nodes.Add(node);
                    Moov = new Moov(fs, Position, node);
                }
                else if (Name == "mdat")
                {
                    var node = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Media Data Box" + Environment.NewLine +
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
                        Tag = "Element: " + Name + " - Movie Fragment Box" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };
                    treeView?.Nodes.Add(node);
                    Moof = new Moof(fs, Position, node, StartPosition);
                    if (Moof.Traf?.Trun?.Samples.Count > 0)
                    {
                        samples.AddRange(Moof.Traf.Trun.Samples);
                    }

                    if (Moof.Traf?.Trun?.DataOffset != null)
                    {
                        var dts = Moof.Traf.Tfdt.BaseMediaDecodeTime;
                        var startPosition = (uint)(Moof.StartPosition + Moof.Traf.Trun.DataOffset.Value);
                        for (var index = 0; index < Moof.Traf.Trun.Samples.Count; index++)
                        {
                            var sample = Moof.Traf.Trun.Samples[index];
                            if (sample.Size.HasValue)
                            {
                                var ccData = GetCcData(fs, startPosition, sample.Size.Value);
                                if (ccData.Count > 0)
                                {
                                    if (sample.TimeOffset.HasValue)
                                    {
                                        ccData[0].Time = (ulong)((long)dts + sample.TimeOffset.Value);
                                    }

                                    TrunAllCcData.Add(ccData[0]); //TODO: can there be more than one?
                                }

                                startPosition += sample.Size.Value;
                            }

                            if (sample.Duration.HasValue)
                            {
                                dts += sample.Duration.Value;
                            }
                        }
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
                if (count > 10_000)
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

            var sortedData = TrunAllCcData.OrderBy(p => p.Time).ToList();
            var parser = new CcDataC608Parser();
            parser.DisplayScreen += DisplayScreen;
            foreach (var cc in sortedData)
            {
                parser.AddData((int)cc.Time, new[] { cc.Data1, cc.Data2 });
            }
        }

        private void DisplayScreen(DataOutput data)
        {
            Console.WriteLine($"Start: {FormatTime(data.Start)}, {FormatTime(data.End)}, {GetText(data.Screen)}");
        }

        private static string GetText(SerializedRow[] dataScreen)
        {
            var sb = new StringBuilder();

            foreach (var row in dataScreen)
            {
                foreach (var column in row.Columns)
                {
                    sb.Append(column.Character);
                }
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }

        private string FormatTime(int time)
        {
            return TimeSpan.FromMilliseconds(time).ToString();
        }

        public class CcData
        {
            public CcData(int type, int data1, int data2)
            {
                Type = type;
                Data1 = data1;
                Data2 = data2;
            }

            public int Type { get; set; }
            public int Data1 { get; set; }
            public int Data2 { get; set; }
            public ulong Time { get; set; }
        }

        private static List<CcData> GetCcData(Stream fs, uint startPos, ulong size)
        {
            var fieldData = new List<CcData>();
            for (var i = startPos; i < startPos + size - 5; i++)
            {
                var buffer = new byte[4];
                fs.Seek(i, SeekOrigin.Begin);
                fs.Read(buffer, 0, buffer.Length);
                var nalSize = GetUInt32(buffer, 0);
                var flag = fs.ReadByte();
                if (IsRbspNalUnitType(flag & 0x1F))
                {
                    var seiData = GetSeiData(fs, i + 5, i + nalSize + 3);
                    ParseCcDataFromSei(seiData, fieldData);
                }

                i += nalSize + 3;
            }
            return fieldData;
        }

        private static bool IsRbspNalUnitType(int unitType)
        {
            return unitType == 0x06;
        }

        private static byte[] GetSeiData(Stream fs, uint startPos, uint endPos)
        {
            var data = new List<byte>();
            var buffer = new byte[endPos - startPos];
            fs.Seek(startPos, SeekOrigin.Begin);
            fs.Read(buffer, 0, buffer.Length);

            for (var x = startPos; x < endPos; x++)
            {
                var idx = x - startPos;

                if (x + 2 < endPos && buffer[idx] == 0x00 && buffer[idx + 1] == 0x00 && buffer[idx + 2] == 0x03)
                {
                    data.Add(0x00);
                    data.Add(0x00);
                    x += 2;
                }
                else
                {
                    data.Add(buffer[idx]);
                }
            }

            return data.ToArray();
        }

        private static void ParseCcDataFromSei(byte[] buffer, List<CcData> fieldData)
        {
            var x = 0;
            while (x < buffer.Length)
            {
                var payloadType = 0;
                var payloadSize = 0;
                int now;

                do
                {
                    now = buffer[x++];
                    payloadType += now;
                } while (now == 0xFF);

                do
                {
                    now = buffer[x++];
                    payloadSize += now;
                } while (now == 0xFF);

                if (IsStartOfCcDataHeader(payloadType, buffer, x))
                {
                    var pos = x + 10;
                    var ccCount = pos + (buffer[pos - 2] & 0x1F) * 3;
                    for (var i = pos; i < ccCount; i += 3)
                    {
                        var b = buffer[i];
                        if ((b & 0x4) > 0)
                        {
                            var ccType = b & 0x3;
                            if (IsCcType(ccType))
                            {
                                var ccData1 = buffer[i + 1];
                                var ccData2 = buffer[i + 2];
                                if (IsNonEmptyCcData(ccData1, ccData2))
                                {
                                    fieldData.Add(new CcData(ccType, ccData1, ccData2));
                                    //fieldData[ccType].push(ccData1, ccData2);
                                }
                            }
                        }
                    }
                }

                x += payloadSize;
            }
        }

        private static bool IsCcType(int type)
        {
            return type == 0 || type == 1;
        }

        private static bool IsNonEmptyCcData(int ccData1, int ccData2)
        {
            return (ccData1 & 0x7f) > 0 || (ccData2 & 0x7f) > 0;
        }

        private static bool IsStartOfCcDataHeader(int payloadType, byte[] buffer, int pos)
        {
            return payloadType == 4 &&
                   GetUInt32(buffer, pos) == 3036688711 &&
                   GetUInt32(buffer, pos + 4) == 1094267907;
        }

        private static uint GetUInt32(byte[] buffer, int pos)
        {
            return (uint)((buffer[pos + 0] << 24) + (buffer[pos + 1] << 16) + (buffer[pos + 2] << 8) + buffer[pos + 3]);
        }

        private static string GetFriendlyBoxName(string name)
        {
            switch (name)
            {
                case "ftyp": return "File Type Box";
                case "styp": return "Segment Type Box";
                case "sidx": return "Segment Index Box";
                case "free": return "Free Space Box";
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
