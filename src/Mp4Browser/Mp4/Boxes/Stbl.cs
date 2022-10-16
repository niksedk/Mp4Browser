using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Stbl : Box
    {
        public class TimeInfo
        {
            public uint SampleCount { get; set; }
            public uint SampleDelta { get; set; }
        }

        public List<string> Texts = new List<string>();
        public List<double> StartTimeCodes = new List<double>();
        public List<double> EndTimeCodes = new List<double>();
        public List<uint> SampleSizes = new List<uint>();
        public ulong StszSampleCount = 0;
        private readonly Mdia _mdia;
        public ulong TimeScale { get; set; }
        public string HandlerType { get; set; }
        public List<TimeInfo> Ssts { get; set; }
        public List<Byte[]> TextBuffers { get; set; }

        public string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }

        public Stbl(Stream fs, ulong maximumLength, ulong timeScale, string handlerType, Mdia mdia, TreeNode root)
        {
            TimeScale = timeScale;
            HandlerType = handlerType;
            Ssts = new List<TimeInfo>();
            TextBuffers = new List<byte[]>();
            _mdia = mdia;
            Position = (ulong)fs.Position;
            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                    return;

                if (Name == "stco") // 32-bit - chunk offset
                {
                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    int version = Buffer[0];
                    uint totalEntries = GetUInt(4);
                    var sbTexts = new StringBuilder();

                    uint lastOffset = 0;
                    for (int i = 0; i < totalEntries; i++)
                    {
                        uint offset = GetUInt(8 + i * 4);
                        if (lastOffset + 5 < offset)
                        {
                            var s = ReadText(fs, offset, handlerType, i);
                            sbTexts.AppendLine($" {i} - {s}");
                        }
                        lastOffset = offset;
                    }
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Chunk Offset" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Total entries: " + totalEntries + Environment.NewLine +
                              "Texts: " + Environment.NewLine + sbTexts
                    });
                }
                else if (Name == "co64") // 64-bit
                {
                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    int version = Buffer[0];
                    uint totalEntries = GetUInt(4);
                    var sbTexts = new StringBuilder();

                    ulong lastOffset = 0;
                    for (int i = 0; i < totalEntries; i++)
                    {
                        ulong offset = GetUInt64(8 + i * 8);
                        if (lastOffset + 8 < offset)
                        {
                            var s = ReadText(fs, offset, handlerType, i);
                            sbTexts.AppendLine($" {i} - {s}");
                            if (s == "Amen. Dig in.")
                                s = s;
                        }

                        lastOffset = offset;
                    }
                    var co64Node = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Total entries: " + totalEntries + Environment.NewLine +
                              "Texts: " + Environment.NewLine + sbTexts
                    };
                    root.Nodes.Add(co64Node);
                }
                else if (Name == "stsz") // sample sizes
                {
                    var sbSampleSizes = new StringBuilder();
                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    int version = Buffer[0];
                    uint uniformSizeOfEachSample = GetUInt(4);
                    uint numberOfSampleSizes = GetUInt(8);
                    StszSampleCount = numberOfSampleSizes;
                    for (int i = 0; i < numberOfSampleSizes; i++)
                    {
                        if (12 + i * 4 + 4 < Buffer.Length)
                        {
                            uint sampleSize = GetUInt(12 + i * 4);
                            SampleSizes.Add(sampleSize);
                            sbSampleSizes.AppendLine($" {i} - Sample size: {sampleSize}");
                        }
                    }
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Sample Size Box" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Sample sizes:" + Environment.NewLine +
                              sbSampleSizes
                    });
                }
                else if (Name == "stts") // sample table time to sample map
                {
                    //https://developer.apple.com/library/mac/#documentation/QuickTime/QTFF/QTFFChap2/qtff2.html#//apple_ref/doc/uid/TP40000939-CH204-SW1

                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    int version = Buffer[0];
                    uint numberOfSampleTimes = GetUInt(4);
                    double totalTime = 0;
                    var sbTimeToSample = new StringBuilder();
                    if (_mdia.IsClosedCaption)
                    {
                        for (int i = 0; i < numberOfSampleTimes; i++)
                        {
                            uint sampleCount = GetUInt(8 + i * 8);
                            uint sampleDelta = GetUInt(12 + i * 8);
                            Ssts.Add(new TimeInfo { SampleCount  = sampleCount, SampleDelta = sampleDelta} );
                            sbTimeToSample.AppendLine($" {i} - {sampleCount} - {sampleDelta}");
                            for (int j = 0; j < sampleCount; j++)
                            {
                                totalTime += sampleDelta / (double)timeScale;
                                if (StartTimeCodes.Count > 0)
                                    EndTimeCodes[EndTimeCodes.Count - 1] = totalTime - 0.001;
                                StartTimeCodes.Add(totalTime);
                                EndTimeCodes.Add(totalTime + 2.5);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < numberOfSampleTimes; i++)
                        {
                            uint sampleCount = GetUInt(8 + i * 8);
                            uint sampleDelta = GetUInt(12 + i * 8);
                            Ssts.Add(new TimeInfo { SampleCount = sampleCount, SampleDelta = sampleDelta });
                            sbTimeToSample.AppendLine($" {i} - {sampleCount} - {sampleDelta}");
                            totalTime += sampleDelta / (double)timeScale;
                            if (StartTimeCodes.Count <= EndTimeCodes.Count)
                                StartTimeCodes.Add(totalTime);
                            else
                                EndTimeCodes.Add(totalTime);
                        }
                    }
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Decoding, Time to Sample" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Number of samples: " + numberOfSampleTimes + Environment.NewLine +
                              "Samples: " + Environment.NewLine +
                                 sbTimeToSample
                    });
                }
                else if (Name == "stsc") // sample table sample to chunk map
                {
                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    int version = Buffer[0];
                    uint numberOfSampleTimes = GetUInt(4);
                    for (int i = 0; i < numberOfSampleTimes; i++)
                    {
                        if (16 + i * 12 + 4 < Buffer.Length)
                        {
                            uint firstChunk = GetUInt(8 + i * 12);
                            uint samplesPerChunk = GetUInt(12 + i * 12);
                            uint sampleDescriptionIndex = GetUInt(16 + i * 12);
                        }
                    }
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Sample‐to‐Chunk" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Number of samples: " + numberOfSampleTimes
                    });
                }
                else if (Name == "stsd")
                {
                    var stsdNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Sample Description Box" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };
                    var stsd = new Stsd(fs, Position, stsdNode);
                    root.Nodes.Add(stsdNode);
                }
                else 
                {
                    var unknown = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - unkown "+ Environment.NewLine + 
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };
                    root.Nodes.Add(unknown);
                }

                fs.Seek((long)Position, SeekOrigin.Begin);
            }
        }

        private string ReadText(Stream fs, ulong offset, string handlerType, int index)
        {
            if (handlerType == "vide")
                return string.Empty;

            var sb2 = new StringBuilder();
            fs.Seek((long)offset, SeekOrigin.Begin);
            var data = new byte[4];
            fs.Read(data, 0, 2);
            uint textSize = (uint)GetWord(data, 0);

            if (handlerType == "subp") // VobSub created with Mp4Box
            {
                if (textSize > 100)
                {
                    fs.Seek((long)offset, SeekOrigin.Begin);
                    data = new byte[textSize + 2];
                    fs.Read(data, 0, data.Length);
                    sb2.AppendLine("Binay image");
                }
            }
            else
            {
                if (index + 1 < SampleSizes.Count && SampleSizes[index + 1] <= 2)
                    return string.Empty;

                if (textSize == 0)
                {
                    fs.Read(data, 2, 2);
                    textSize = GetUInt(data, 0); // don't get it exactly - seems like mp4box sometimes uses 2 bytes length field (first text record only)... handbrake uses 4 bytes
                }
                if (textSize > 0 && textSize < 500)
                {
                    data = new byte[textSize];
                    fs.Read(data, 0, data.Length);
                    string text = GetString(data, 0, (int)textSize).TrimEnd();
                    sb2.Append(text);

                    if (_mdia.IsClosedCaption)
                    {
                        var sb = new StringBuilder();
                        for (int j = 8; j < data.Length - 3; j++)
                        {
                            string h = data[j].ToString("X2").ToLower();
                            if (h.Length < 2)
                                h = "0" + h;
                            sb.Append(h);
                            if (j % 2 == 1)
                                sb.Append(' ');
                        }
                        text = string.Empty; // ScenaristClosedCaptions.GetSccText(hex, ref errorCount);
                        if (text.StartsWith("n", StringComparison.Ordinal) && text.Length > 1)
                            text = "<i>" + text.Substring(1) + "</i>";
                        if (text.StartsWith("-n", StringComparison.Ordinal))
                            text = text.Remove(0, 2);
                        if (text.StartsWith("-N", StringComparison.Ordinal))
                            text = text.Remove(0, 2);
                        if (text.StartsWith("-", StringComparison.Ordinal) && !text.Contains(Environment.NewLine + "-"))
                            text = text.Remove(0, 1);
                    }
                    Texts.Add(text.Replace(Environment.NewLine, "\n").Replace("\n", Environment.NewLine));
                }
                else
                {
    //                if (infoSampleSize == 2)
                        return string.Empty;
                    Texts.Add(string.Empty);
                    sb2.Append(string.Empty);
                }
            }
            var resultSb = new StringBuilder();
            foreach (char ch in sb2.ToString())
            {
                if (!char.IsControl(ch))
                    resultSb.Append(ch);
                else
                {
                    resultSb.Append(" ");
                }
            }
            return resultSb.ToString();
        }

    }
}
