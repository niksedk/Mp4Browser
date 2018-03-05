using System;
using System.Windows.Forms;
using Mp4Browser.Mp4;

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
        }

        private void buttonOpenFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                textBoxFileName.Text = openFileDialog1.FileName;
                treeView1.Nodes.Clear();
                treeView1.BeginUpdate();
                _fileName = openFileDialog1.FileName;
                labelStatus.Text = $"Opening {_fileName}...";
                Refresh();
                Application.DoEvents();
                var parser = new Mp4Parser(openFileDialog1.FileName, treeView1);
                buttonSaveMDAT.Enabled = true;
                treeView1.EndUpdate();
                labelStatus.Text = string.Empty;
                Text = $"{Title} - {_fileName}";
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            textBoxInfo.Text = e.Node?.Tag?.ToString();
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
    }
}
