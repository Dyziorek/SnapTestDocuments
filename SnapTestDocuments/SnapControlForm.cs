using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using Nuance.SpeechAnywhere;

namespace SnapTestDocuments
{
    public partial class SnapControlForm : Form
    {
        private MemoryStream memoryStream;
        private SnapContextImpl context;
        private DocumentEntityBase selectedItem;
        private Nuance.SpeechAnywhere.Custom.VuiController vuiController;
        private DictSnapControl[] myControls;

        //private Nuance.SoD.TextControl.TextControlManager manager;
        //private Nuance.SoD.TextControl.SodGateway connector = Nuance.SoD.TextControl.SodGateway.Instance;
        public SnapControlForm()
        {
            InitializeComponent();
            StringBuilder resultWholeText = new StringBuilder();
            var sree = resultWholeText.ToString();
            log4net.LogManager.GetLogger("DictSnapControl").ErrorFormat("Empry SB '{0}'", sree);
            log4net.Config.XmlConfigurator.Configure(new FileInfo(string.Format("{0}.config", @"C:\Users\ddus\source\repos\SnapTestDocuments\SnapTestDocuments\bin\Deploy\log4net")));
            //string fx = "The quick brown fox jumps over lazy";
            //string fxc = "The brown fox jumps over lazy";

            //DiffMatchPatchText matchPatchText = new DiffMatchPatchText();
            //var differ = matchPatchText.diff_main(fx, fxc);
            //var deltat = matchPatchText.diff_toDelta(differ);
            //System.Diagnostics.Debug.WriteLine(deltat);
            myControls = new DictSnapControl[] { snapControl2 };
            InitializeVuiController();



        }
        private void InitializeVuiController()
        {
            // Add event handlers for RecordingStarted and RecordingStopped events
            Session.SharedSession.RecordingStarted += new RecordingStarted(SharedSession_RecordingStarted);
            Session.SharedSession.RecordingStopped += new RecordingStopped(SharedSession_RecordingStopped);

            vuiController = new Nuance.SpeechAnywhere.Custom.VuiController
            {
                // Set the recognition language. This overrides the default setting which is the current UI culture;
                Language = "en-us"
            };
            // Enable name field navigation.
            // For example, the first text control is associated with the "medical history" concept. 
            // Users can say "go to medical history" to reach this text control.
            // SetConceptName() must be called before Initialize() to be effective.
            vuiController.SetConceptName(myControls[0], "medical history");

            // Initialize the VuiController by passing Nuance.SpeechAnywhere.Custom.ITextControl[] - which is in this case "myControls"
            vuiController.Open(myControls);
            vuiController.Focused = true;
        }

        void SharedSession_RecordingStarted()
        {
            // This event occurs in case recording was started.
            // We react by changing the toggle record button title. 
        }

        void SharedSession_RecordingStopped()
        {
            // This event occurs in case recording was stopped.
            // We react by changing the toggle record button title. 
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

                snapControl2.LoadDocument(textBox1.Text);
                snapControl2.Options.Bookmarks.Visibility = DevExpress.XtraRichEdit.RichEditBookmarkVisibility.Visible;
                if (context == null)
                {
                    context = new SnapContextImpl();
                    snapControl2.SetContext = context;
                    context.WorkControl = snapControl2;
                    //connector.Start(context);
                    context.GetManager<IDragonAccessManager>()?.UpdateSelectedItem(new DocumentEntityBase{ name = "", Type = DocumentEntityTypes.EmptySection});
                }
                else
                {
                    context.GetManager<IDragonAccessManager>()?.UpdateSelectedItem(new DocumentEntityBase { name = "", Type = DocumentEntityTypes.EmptySection });
                }
                lbSections.Items.Clear();
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
            snapControl2.SaveDocumentAs();

        }

        private void BtnDocSetup_Click(object sender, EventArgs e)
        {
            //ShowPageSetupFormCommand cmd = new ShowPageSetupFormCommand(snapControl2);
            //cmd.Execute();

            //var doc = snapControl2.Document;

            //var table = snapControl2.Document.Tables.Create(doc.Range.End, 1, 1);

            //var fieldInfo = doc.Fields.Create(table.Cell(0, 0).ContentRange.Start, "SN EF|MACRO|SlideResultsMacro|SNF");
            //fieldInfo.ShowCodes = false;
            //doc.InsertText(fieldInfo.ResultRange.Start, "Slide Results Macro");

            //DevExpress.XtraRichEdit.Commands.ToggleShowWhitespaceCommand too = new ToggleShowWhitespaceCommand(snapControl2);
            //too.Execute();
            var ranger = snapControl2.Document.CreateRange(0, snapControl2.Document.Length);
            var subDoc = ranger.BeginUpdateDocument();
            subDoc.BeginUpdate();
            subDoc.Replace(ranger, "");
            subDoc.EndUpdate();
            ranger.EndUpdateDocument(subDoc);
            
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
                    snapControl2.LoadDocument(memStr);
                }


            }
        }

        private void btnSection_Click(object sender, EventArgs e)
        {

            DevExpress.XtraRichEdit.API.Native.DocumentPosition documentPosition = snapControl2.Document.Selection.Start;
            selectedItem = null;
            lbSections.Items.Clear();
            foreach (var fieldInfo in snapControl2.Document.Fields)
            {
                var fieldCodeText = snapControl2.Document.GetText(fieldInfo.CodeRange);
                var fieldCodePar = new string(fieldCodeText.Take(fieldCodeText.IndexOf("_FUNCTION")).Skip(11).ToArray());
                if (fieldCodeText.Contains("_FUNCTION") &&
                SnapDocumentRangeTools.IsTargetDocumentPositionInBaseDocumentRange(fieldInfo.Range, documentPosition))
                {
                    selectedItem = new DocumentEntityBase
                    {
                        name = fieldCodePar,
                        Type = DocumentEntityTypes.InterpretationSection
                    };
                }
                if (fieldCodeText.Contains("_FUNCTION"))
                {
                    lbSections.Items.Add(fieldCodePar);
                }
            }

            context.GetManager<IDragonAccessManager>().UpdateSelectedItem(this.selectedItem);
                
            IInterpSectionsManager sectionMgr = context.GetManager<IInterpSectionsManager>();
            var sectionField = sectionMgr.GetSectionField(selectedItem);
            if (sectionField != null)
                System.Diagnostics.Debug.WriteLine(snapControl2.Document.GetText(sectionField.CodeRange));

            context.GetManager<IDragonAccessManager>().UpdateSelectedItem(selectedItem);
        }

        private void lbSections_SelectedIndexChanged(object sender, EventArgs e)
        {
            var results = lbSections.SelectedItem as string;
            if (results != null)
            {
                foreach (var fieldInfo in snapControl2.Document.Fields)
                {
                    var fieldCodeText = snapControl2.Document.GetText(fieldInfo.CodeRange);
                    if (fieldCodeText.Contains(results))
                    {
                        context.SnapDocument.CaretPosition = fieldInfo.ResultRange.End;
                        context.SnapControl.Focus();
                        selectedItem = new DocumentEntityBase
                        {
                            name = results,
                            Type = DocumentEntityTypes.InterpretationSection
                        };
                        context.GetManager<IDragonAccessManager>().UpdateSelectedItem(this.selectedItem);
                        break;
                    }
                }
            }

        }

        private void SnapControlForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            var appenders = log4net.LogManager.GetRepository().GetAppenders();
            foreach (var app in appenders)
            {
                if (app is log4net.Appender.BufferingForwardingAppender)
                {
                    log4net.Appender.BufferingForwardingAppender check = app as log4net.Appender.BufferingForwardingAppender;
                    check.Flush();
                }
            }
        }
    }
}
