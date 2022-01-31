using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using DevExpress.Snap;
using Nuance.SpeechAnywhere;

namespace SnapTestDocuments
{
    public partial class SnapControlForm : Form
    {
        private MemoryStream memoryStream;
        private SnapContextImpl context;
        private DocumentEntityBase selectedItem;
        private Nuance.SpeechAnywhere.Custom.VuiController vuiController;
        private SnapControl[] myControls;
        string ActiveSnapControl;

        //private Nuance.SoD.TextControl.TextControlManager manager;
        //private Nuance.SoD.TextControl.SodGateway connector = Nuance.SoD.TextControl.SodGateway.Instance;
        public SnapControlForm()
        {
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;
            ActiveSnapControl = appSettings["ActiveSnapControl"] ?? "SnapControl";
            if (ActiveSnapControl == "DictSnapControl")
            {
                InitializeComponentDict();
            }
            else if (ActiveSnapControl == "SnapControl")
            {
                InitializeComponent();
            }
            else if (ActiveSnapControl == "ExtSnapControl")
            {
                InitializeComponentExt();
            }
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
            if (ActiveSnapControl == "DictSnapControl")
            {
                myControls = new SnapControl[] { snapControl2 };
                InitializeVuiController();
            }


        }


        private void InitializeComponentExt()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SnapControlForm));
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.bntOpen = new System.Windows.Forms.Button();
            this.textBox1 = new SnapTestDocuments.ExtTextControl();
            this.label1 = new System.Windows.Forms.Label();
            this.snapDockManager1 = new DevExpress.Snap.Extensions.SnapDockManager(this.components);
            this.btnSaveDoc = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.rulerBar1 = new TXTextControl.RulerBar();
            this.textControl1 = new SnapTestDocuments.ExtTxTextControl();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.snapDocumentManager1 = new DevExpress.Snap.Extensions.SnapDocumentManager(this.components);
            this.noDocumentsView1 = new DevExpress.XtraBars.Docking2010.Views.NoDocuments.NoDocumentsView(this.components);
            this.tabbedView1 = new DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView(this.components);
            this.behaviorManager1 = new DevExpress.Utils.Behaviors.BehaviorManager(this.components);
            this.btnDocSetup = new System.Windows.Forms.Button();
            this.btnPasteText = new System.Windows.Forms.Button();
            this.btnSection = new System.Windows.Forms.Button();
            this.lbSections = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.snapControl2 = new ExtSnapControl();
            ((System.ComponentModel.ISupportInitialize)(this.snapDockManager1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.snapDocumentManager1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.noDocumentsView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tabbedView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.behaviorManager1)).BeginInit();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // bntOpen
            // 
            this.bntOpen.Location = new System.Drawing.Point(936, 26);
            this.bntOpen.Name = "bntOpen";
            this.bntOpen.Size = new System.Drawing.Size(75, 23);
            this.bntOpen.TabIndex = 0;
            this.bntOpen.Text = "...";
            this.bntOpen.UseVisualStyleBackColor = true;
            this.bntOpen.Click += new System.EventHandler(this.BntOpen_Click);
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("Arial", 10F);
            this.textBox1.Location = new System.Drawing.Point(27, 26);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(807, 83);
            this.textBox1.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(121, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Path to Snap Document";
            // 
            // snapDockManager1
            // 
            this.snapDockManager1.Form = this;
            this.snapDockManager1.SnapControl = this.snapControl2;
            this.snapDockManager1.TopZIndexControls.AddRange(new string[] {
            "DevExpress.XtraBars.BarDockControl",
            "DevExpress.XtraBars.StandaloneBarDockControl",
            "System.Windows.Forms.StatusBar",
            "System.Windows.Forms.MenuStrip",
            "System.Windows.Forms.StatusStrip",
            "DevExpress.XtraBars.Ribbon.RibbonStatusBar",
            "DevExpress.XtraBars.Ribbon.RibbonControl",
            "DevExpress.XtraBars.Navigation.OfficeNavigationBar",
            "DevExpress.XtraBars.Navigation.TileNavPane",
            "DevExpress.XtraBars.TabFormControl",
            "DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormControl",
            "DevExpress.XtraBars.ToolbarForm.ToolbarFormControl"});
            // 
            // btnSaveDoc
            // 
            this.btnSaveDoc.Location = new System.Drawing.Point(1043, 26);
            this.btnSaveDoc.Name = "btnSaveDoc";
            this.btnSaveDoc.Size = new System.Drawing.Size(75, 23);
            this.btnSaveDoc.TabIndex = 4;
            this.btnSaveDoc.Text = "Save";
            this.btnSaveDoc.UseVisualStyleBackColor = true;
            this.btnSaveDoc.Click += new System.EventHandler(this.BtnSaveDoc_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(6, 124);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.textBox2);
            this.splitContainer1.Size = new System.Drawing.Size(1206, 578);
            this.splitContainer1.SplitterDistance = 909;
            this.splitContainer1.TabIndex = 5;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.snapControl2);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.rulerBar1);
            this.splitContainer2.Panel2.Controls.Add(this.textControl1);
            this.splitContainer2.Size = new System.Drawing.Size(909, 578);
            this.splitContainer2.SplitterDistance = 291;
            this.splitContainer2.TabIndex = 1;
            // 
            // rulerBar1
            // 
            this.rulerBar1.Dock = System.Windows.Forms.DockStyle.Top;
            this.rulerBar1.Location = new System.Drawing.Point(0, 0);
            this.rulerBar1.Name = "rulerBar1";
            this.rulerBar1.Size = new System.Drawing.Size(909, 25);
            this.rulerBar1.TabIndex = 1;
            this.rulerBar1.Text = "rulerBar1";
            // 
            // textControl1
            // 
            this.textControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textControl1.Font = new System.Drawing.Font("Arial", 10F);
            this.textControl1.Location = new System.Drawing.Point(0, 0);
            this.textControl1.Name = "textControl1";
            this.textControl1.RulerBar = this.rulerBar1;
            this.textControl1.Size = new System.Drawing.Size(909, 283);
            this.textControl1.TabIndex = 0;
            this.textControl1.Text = "textControl1";
            // 
            // textBox2
            // 
            this.textBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox2.Location = new System.Drawing.Point(0, 0);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox2.Size = new System.Drawing.Size(293, 578);
            this.textBox2.TabIndex = 0;
            // 
            // snapDocumentManager1
            // 
            this.snapDocumentManager1.ClientControl = this.snapControl2;
            this.snapDocumentManager1.View = this.noDocumentsView1;
            this.snapDocumentManager1.ViewCollection.AddRange(new DevExpress.XtraBars.Docking2010.Views.BaseView[] {
            this.noDocumentsView1,
            this.tabbedView1});
            // 
            // btnDocSetup
            // 
            this.btnDocSetup.Location = new System.Drawing.Point(1124, 26);
            this.btnDocSetup.Name = "btnDocSetup";
            this.btnDocSetup.Size = new System.Drawing.Size(75, 23);
            this.btnDocSetup.TabIndex = 6;
            this.btnDocSetup.Text = "docsetup";
            this.btnDocSetup.UseVisualStyleBackColor = true;
            this.btnDocSetup.Click += new System.EventHandler(this.BtnDocSetup_Click);
            // 
            // btnPasteText
            // 
            this.btnPasteText.Location = new System.Drawing.Point(840, 26);
            this.btnPasteText.Name = "btnPasteText";
            this.btnPasteText.Size = new System.Drawing.Size(75, 23);
            this.btnPasteText.TabIndex = 7;
            this.btnPasteText.Text = "ClipBoard";
            this.btnPasteText.UseVisualStyleBackColor = true;
            this.btnPasteText.Click += new System.EventHandler(this.btnPasteText_Click);
            // 
            // btnSection
            // 
            this.btnSection.Location = new System.Drawing.Point(936, 75);
            this.btnSection.Name = "btnSection";
            this.btnSection.Size = new System.Drawing.Size(75, 23);
            this.btnSection.TabIndex = 6;
            this.btnSection.Text = "gosection";
            this.btnSection.UseVisualStyleBackColor = true;
            this.btnSection.Click += new System.EventHandler(this.btnSection_Click);
            // 
            // lbSections
            // 
            this.lbSections.FormattingEnabled = true;
            this.lbSections.Location = new System.Drawing.Point(1017, 75);
            this.lbSections.Name = "lbSections";
            this.lbSections.ScrollAlwaysVisible = true;
            this.lbSections.Size = new System.Drawing.Size(182, 30);
            this.lbSections.TabIndex = 8;
            this.lbSections.SelectedIndexChanged += new System.EventHandler(this.lbSections_SelectedIndexChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(840, 75);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "Record";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.btnSection_Click);
            // 
            // snapControl2
            // 
            this.snapControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.snapControl2.Location = new System.Drawing.Point(0, 0);
            this.snapControl2.Name = "snapControl2";
            this.snapControl2.Options.DocumentCapabilities.TrackChanges = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            this.snapControl2.Options.SnapMailMergeVisualOptions.DataSourceName = null;
            this.snapControl2.Size = new System.Drawing.Size(909, 291);
            this.snapControl2.TabIndex = 0;
            this.snapControl2.Text = "snapControl2";
            // 
            // SnapControlForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1211, 697);
            this.Controls.Add(this.lbSections);
            this.Controls.Add(this.btnPasteText);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnSection);
            this.Controls.Add(this.btnDocSetup);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.btnSaveDoc);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.bntOpen);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SnapControlForm";
            this.Text = "SnapTester";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SnapControlForm_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.snapDockManager1)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.snapDocumentManager1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.noDocumentsView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tabbedView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.behaviorManager1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        private void InitializeComponentDict()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SnapControlForm));
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.bntOpen = new System.Windows.Forms.Button();
            this.textBox1 = new SnapTestDocuments.ExtTextControl();
            this.label1 = new System.Windows.Forms.Label();
            this.snapDockManager1 = new DevExpress.Snap.Extensions.SnapDockManager(this.components);
            this.btnSaveDoc = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.rulerBar1 = new TXTextControl.RulerBar();
            this.textControl1 = new SnapTestDocuments.ExtTxTextControl();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.snapDocumentManager1 = new DevExpress.Snap.Extensions.SnapDocumentManager(this.components);
            this.noDocumentsView1 = new DevExpress.XtraBars.Docking2010.Views.NoDocuments.NoDocumentsView(this.components);
            this.tabbedView1 = new DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView(this.components);
            this.behaviorManager1 = new DevExpress.Utils.Behaviors.BehaviorManager(this.components);
            this.btnDocSetup = new System.Windows.Forms.Button();
            this.btnPasteText = new System.Windows.Forms.Button();
            this.btnSection = new System.Windows.Forms.Button();
            this.lbSections = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.snapControl2 = new DictSnapControl();
            ((System.ComponentModel.ISupportInitialize)(this.snapDockManager1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.snapDocumentManager1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.noDocumentsView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tabbedView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.behaviorManager1)).BeginInit();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // bntOpen
            // 
            this.bntOpen.Location = new System.Drawing.Point(936, 26);
            this.bntOpen.Name = "bntOpen";
            this.bntOpen.Size = new System.Drawing.Size(75, 23);
            this.bntOpen.TabIndex = 0;
            this.bntOpen.Text = "...";
            this.bntOpen.UseVisualStyleBackColor = true;
            this.bntOpen.Click += new System.EventHandler(this.BntOpen_Click);
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("Arial", 10F);
            this.textBox1.Location = new System.Drawing.Point(27, 26);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(807, 83);
            this.textBox1.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(121, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Path to Snap Document";
            // 
            // snapDockManager1
            // 
            this.snapDockManager1.Form = this;
            this.snapDockManager1.SnapControl = this.snapControl2;
            this.snapDockManager1.TopZIndexControls.AddRange(new string[] {
            "DevExpress.XtraBars.BarDockControl",
            "DevExpress.XtraBars.StandaloneBarDockControl",
            "System.Windows.Forms.StatusBar",
            "System.Windows.Forms.MenuStrip",
            "System.Windows.Forms.StatusStrip",
            "DevExpress.XtraBars.Ribbon.RibbonStatusBar",
            "DevExpress.XtraBars.Ribbon.RibbonControl",
            "DevExpress.XtraBars.Navigation.OfficeNavigationBar",
            "DevExpress.XtraBars.Navigation.TileNavPane",
            "DevExpress.XtraBars.TabFormControl",
            "DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormControl",
            "DevExpress.XtraBars.ToolbarForm.ToolbarFormControl"});
            // 
            // btnSaveDoc
            // 
            this.btnSaveDoc.Location = new System.Drawing.Point(1043, 26);
            this.btnSaveDoc.Name = "btnSaveDoc";
            this.btnSaveDoc.Size = new System.Drawing.Size(75, 23);
            this.btnSaveDoc.TabIndex = 4;
            this.btnSaveDoc.Text = "Save";
            this.btnSaveDoc.UseVisualStyleBackColor = true;
            this.btnSaveDoc.Click += new System.EventHandler(this.BtnSaveDoc_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(6, 124);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.textBox2);
            this.splitContainer1.Size = new System.Drawing.Size(1206, 578);
            this.splitContainer1.SplitterDistance = 909;
            this.splitContainer1.TabIndex = 5;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.snapControl2);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.rulerBar1);
            this.splitContainer2.Panel2.Controls.Add(this.textControl1);
            this.splitContainer2.Size = new System.Drawing.Size(909, 578);
            this.splitContainer2.SplitterDistance = 291;
            this.splitContainer2.TabIndex = 1;
            // 
            // rulerBar1
            // 
            this.rulerBar1.Dock = System.Windows.Forms.DockStyle.Top;
            this.rulerBar1.Location = new System.Drawing.Point(0, 0);
            this.rulerBar1.Name = "rulerBar1";
            this.rulerBar1.Size = new System.Drawing.Size(909, 25);
            this.rulerBar1.TabIndex = 1;
            this.rulerBar1.Text = "rulerBar1";
            // 
            // textControl1
            // 
            this.textControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textControl1.Font = new System.Drawing.Font("Arial", 10F);
            this.textControl1.Location = new System.Drawing.Point(0, 0);
            this.textControl1.Name = "textControl1";
            this.textControl1.RulerBar = this.rulerBar1;
            this.textControl1.Size = new System.Drawing.Size(909, 283);
            this.textControl1.TabIndex = 0;
            this.textControl1.Text = "textControl1";
            // 
            // textBox2
            // 
            this.textBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox2.Location = new System.Drawing.Point(0, 0);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox2.Size = new System.Drawing.Size(293, 578);
            this.textBox2.TabIndex = 0;
            // 
            // snapDocumentManager1
            // 
            this.snapDocumentManager1.ClientControl = this.snapControl2;
            this.snapDocumentManager1.View = this.noDocumentsView1;
            this.snapDocumentManager1.ViewCollection.AddRange(new DevExpress.XtraBars.Docking2010.Views.BaseView[] {
            this.noDocumentsView1,
            this.tabbedView1});
            // 
            // btnDocSetup
            // 
            this.btnDocSetup.Location = new System.Drawing.Point(1124, 26);
            this.btnDocSetup.Name = "btnDocSetup";
            this.btnDocSetup.Size = new System.Drawing.Size(75, 23);
            this.btnDocSetup.TabIndex = 6;
            this.btnDocSetup.Text = "docsetup";
            this.btnDocSetup.UseVisualStyleBackColor = true;
            this.btnDocSetup.Click += new System.EventHandler(this.BtnDocSetup_Click);
            // 
            // btnPasteText
            // 
            this.btnPasteText.Location = new System.Drawing.Point(840, 26);
            this.btnPasteText.Name = "btnPasteText";
            this.btnPasteText.Size = new System.Drawing.Size(75, 23);
            this.btnPasteText.TabIndex = 7;
            this.btnPasteText.Text = "ClipBoard";
            this.btnPasteText.UseVisualStyleBackColor = true;
            this.btnPasteText.Click += new System.EventHandler(this.btnPasteText_Click);
            // 
            // btnSection
            // 
            this.btnSection.Location = new System.Drawing.Point(936, 75);
            this.btnSection.Name = "btnSection";
            this.btnSection.Size = new System.Drawing.Size(75, 23);
            this.btnSection.TabIndex = 6;
            this.btnSection.Text = "gosection";
            this.btnSection.UseVisualStyleBackColor = true;
            this.btnSection.Click += new System.EventHandler(this.btnSection_Click);
            // 
            // lbSections
            // 
            this.lbSections.FormattingEnabled = true;
            this.lbSections.Location = new System.Drawing.Point(1017, 75);
            this.lbSections.Name = "lbSections";
            this.lbSections.ScrollAlwaysVisible = true;
            this.lbSections.Size = new System.Drawing.Size(182, 30);
            this.lbSections.TabIndex = 8;
            this.lbSections.SelectedIndexChanged += new System.EventHandler(this.lbSections_SelectedIndexChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(840, 75);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "Record";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.btnSection_Click);
            // 
            // snapControl2
            // 
            this.snapControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.snapControl2.Location = new System.Drawing.Point(0, 0);
            this.snapControl2.Name = "snapControl2";
            this.snapControl2.Options.DocumentCapabilities.TrackChanges = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            this.snapControl2.Options.SnapMailMergeVisualOptions.DataSourceName = null;
            this.snapControl2.Size = new System.Drawing.Size(909, 291);
            this.snapControl2.TabIndex = 0;
            this.snapControl2.Text = "snapControl2";
            // 
            // SnapControlForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1211, 697);
            this.Controls.Add(this.lbSections);
            this.Controls.Add(this.btnPasteText);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnSection);
            this.Controls.Add(this.btnDocSetup);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.btnSaveDoc);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.bntOpen);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SnapControlForm";
            this.Text = "SnapTester";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SnapControlForm_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.snapDockManager1)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.snapDocumentManager1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.noDocumentsView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tabbedView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.behaviorManager1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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
                if (context == null && ActiveSnapControl != "SnapControl")
                {
                    context = new SnapContextImpl();
                    if (ActiveSnapControl == "ExtSnapControl")
                    {
                        ((ExtSnapControl)snapControl2).SetContext = context;
                    }
                    else if (ActiveSnapControl == "DictSnapControl")
                    {
                        ((DictSnapControl)snapControl2).SetContext = context;
                    }
                    context.WorkControl = snapControl2;
                    //connector.Start(context);
                    context.GetManager<IDragonAccessManager>()?.UpdateSelectedItem(new DocumentEntityBase{ name = "", Type = DocumentEntityTypes.EmptySection});
                }
                else
                {
                    //context.GetManager<IDragonAccessManager>()?.UpdateSelectedItem(new DocumentEntityBase { name = "", Type = DocumentEntityTypes.EmptySection });
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
