using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Mp4Browser.Mp4;
using Mp4Browser.Mp4.Boxes;

namespace Mp4Browser
{
    public partial class Form1 : Form
    {
        private string _fileName;
        private const string Title = "C# MP4 browser";

        public Form1()
        {
            InitializeComponent();
            buttonSaveMDAT.Enabled = false;
            labelStatus.Text = string.Empty;
            buttonSaveSubtitle.Visible = false;
            treeView1.AllowDrop = true;
        }

        private void buttonOpenFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                OpenFile(openFileDialog1.FileName);
            }
        }

        private void OpenFile(string fileName)
        {
            textBoxInfo.Text = string.Empty;
            textBoxFileName.Text = fileName;
            treeView1.Nodes.Clear();
            treeView1.BeginUpdate();
            _fileName = fileName;
            labelStatus.Text = $"Opening {_fileName}...";
            Refresh();
            Application.DoEvents();
            new Mp4Parser(fileName, treeView1);
            buttonSaveMDAT.Enabled = true;
            treeView1.EndUpdate();
            labelStatus.Text = string.Empty;
            Text = $"{Title} - {_fileName}";
            buttonSaveSubtitle.Visible = false;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            textBoxInfo.Text = e.Node?.Tag + (e.Node.Nodes.Count > 0 ? (Environment.NewLine + "Child nodes: " + e.Node.Nodes.Count) : string.Empty);
            if (e.Node.Tag is Stbl)
            {
                buttonSaveSubtitle.Visible = true;
            }
            else
            {
                buttonSaveSubtitle.Visible = false;
            }
        }

        private void buttonSaveMDAT_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog(this) != DialogResult.OK)
                return;

            treeView1.Nodes.Clear();
            treeView1.BeginUpdate();
            var parser = new Mp4Parser(openFileDialog1.FileName, treeView1);
            treeView1.EndUpdate();
            parser.SaveMdats(saveFileDialog1.FileName);
        }

        private void buttonSaveSubtitle_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "SubRip files|*.srt";
            saveFileDialog1.FileName = "subtitle.srt";
            saveFileDialog1.DefaultExt = ".srt";
            if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                var stbl = (Stbl)treeView1.SelectedNode.Tag;
                double totalTime = 0;
                List<double> allTimes = new List<double>();
                if (stbl != null)
                {
                    var sb = new StringBuilder();
                    foreach (var timeInfo in stbl.Ssts)
                    {
                        for (int i = 0; i < timeInfo.SampleCount; i++)
                        {
                            totalTime += timeInfo.SampleDelta / (double)stbl.TimeScale;
                            allTimes.Add(totalTime);
                        }
                    }

                    int count = 1;
                    var index = 0;
                    var textIndex = 0;
                    while (index < allTimes.Count - 1)
                    {
                        if (index > 0 && stbl.SampleSizes[index + 1] == 2)
                        {
                            index++;
                        }


                        var time = allTimes[index];
                        var timeEnd = time + 2;
                        if (index + 1 < allTimes.Count)
                            timeEnd = allTimes[index + 1];
                        sb.AppendLine(count.ToString());
                        sb.AppendLine(MillisecondsToSrt(time) + " --> " + MillisecondsToSrt(timeEnd));

                        var text = string.Empty;
                        if (stbl.Texts.Count > textIndex)
                            text = stbl.Texts[textIndex].Text;
                        sb.AppendLine(text);

                        sb.AppendLine();
                        count++;
                        index++;
                        textIndex++;
                    }

                    System.IO.File.WriteAllText(saveFileDialog1.FileName, sb.ToString());
                }
            }
        }

        private static string MillisecondsToSrt(double time)
        {
            var ts = TimeSpan.FromSeconds(time);
            const string decimalSeparator = ",";
            var s = $"{ts.Hours + ts.Days * 24:00}:{ts.Minutes:00}:{ts.Seconds:00}{decimalSeparator}{ts.Milliseconds:000}";

            if (time >= 0)
            {
                return s;
            }

            return "-" + s.Replace("-", string.Empty);
        }

        private void treeView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (fileNames.Length > 0)
            {
                OpenFile(fileNames[0]);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.O)
            {
                e.SuppressKeyPress = true;
                buttonOpenFile_Click(null, null);
            }
        }
    }
}
