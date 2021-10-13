using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace SnapTestDocuments
{
    public partial class SnapControlForm : Form
    {
        private MemoryStream memoryStream;
        private SnapContextImpl context;

        public SnapControlForm()
        {
            InitializeComponent();
        }

        private void BntOpen_Click(object sender, EventArgs e)
        {
            openFileDialog1.DefaultExt = "snx";
            openFileDialog1.Title = "Open Snaps";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                using (FileStream fstrm = new FileStream(openFileDialog1.FileName, FileMode.Open))
                {
                    memoryStream = new MemoryStream((int)fstrm.Length);
                    fstrm.CopyTo(memoryStream);
                }

                String basePath = System.IO.Path.GetDirectoryName(openFileDialog1.FileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(openFileDialog1.FileName);
                if (!String.IsNullOrEmpty(basePath))
                {
                    if (System.IO.File.Exists(basePath + ".rtf"))
                    {
                        textControl1.Load(System.IO.File.ReadAllText(basePath + ".rtf"), TXTextControl.StringStreamType.RichTextFormat);
                    }
                }

                textBox1.Text = openFileDialog1.FileName;

                snapControl1.LoadDocument(textBox1.Text);
                snapControl1.Options.Bookmarks.Visibility = DevExpress.XtraRichEdit.RichEditBookmarkVisibility.Visible;
                if (context == null)
                {
                    context = new SnapContextImpl();
                    snapControl1.SetContext = context;
                    context.WorkControl = snapControl1;
                    IDragonAccessManager accMgr = context.GetManager<IDragonAccessManager>();
                }
            }

            XDocument snDocument = GetDocumentFromPackage(memoryStream);
            FilTreeView(snDocument);
        }

        private void FilTreeView(XDocument snDocument)
        {
            textBox2.Text = snDocument.ToString();
        }

        private XDocument GetDocumentFromPackage(MemoryStream memoryStr)
        {
            if (memoryStr != null)
            {
                using (Package zipPack = Package.Open(memoryStr))
                {
                    return XDocument.Load(zipPack.GetParts().Where(partCheck => partCheck.Uri.OriginalString.Contains("document")).First().GetStream());
                }
            }

            return new XDocument();
        }

        private void BtnSaveDoc_Click(object sender, EventArgs e)
        {
            snapControl1.SaveDocumentAs();

        }

        private void BtnDocSetup_Click(object sender, EventArgs e)
        {
            //ShowPageSetupFormCommand cmd = new ShowPageSetupFormCommand(snapControl1);
            //cmd.Execute();

            var doc = snapControl1.Document;

            var table = snapControl1.Document.Tables.Create(doc.Range.End, 1, 1);

            var fieldInfo = doc.Fields.Create(table.Cell(0, 0).ContentRange.Start, "SN EF|MACRO|SlideResultsMacro|SNF");
            fieldInfo.ShowCodes = false;
            doc.InsertText(fieldInfo.ResultRange.Start, "Slide Results Macro");

            //DevExpress.XtraRichEdit.Commands.ToggleShowWhitespaceCommand too = new ToggleShowWhitespaceCommand(snapControl1);
            //too.Execute();
        }

        private void btnPasteText_Click(object sender, EventArgs e)
        {
            string clipText = (string)Clipboard.GetData(DataFormats.Text);
            if (clipText?.Length > 0 && clipText.First().Equals('[') && clipText.Last().Equals(']'))
            {
                clipText = clipText.Trim('[', ']');
                var textNumbers = clipText.Split(' ', ',');
                List<byte> bytesArr = new List<byte>();

                foreach (var textPart in textNumbers)
                {
                    if (textPart.Length > 0)
                    {
                        int intVal = int.Parse(textPart);
                        byte[] bv = BitConverter.GetBytes(intVal);
                        bytesArr.Add(bv[0]);
                    }
                }

                using (MemoryStream memStr = new MemoryStream(bytesArr.ToArray()))
                {
                    XDocument snDocument = GetDocumentFromPackage(memStr);
                    FilTreeView(snDocument);
                    memStr.Seek(0, SeekOrigin.Begin);
                    snapControl1.LoadDocument(memStr);
                }


            }
        }
    }
}
