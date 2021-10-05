using SnapTestDocuemnts;

namespace SnapTestDocuments
{
    partial class SnapControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SnapControl));
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.bntOpen = new System.Windows.Forms.Button();
            this.textBox1 = new SnapTestDocuemnts.ExtTextControl();
            this.label1 = new System.Windows.Forms.Label();
            this.snapDockManager1 = new DevExpress.Snap.Extensions.SnapDockManager(this.components);
            this.snapControl1 = new SnapTestDocuemnts.ExtSnapControl();
            this.btnSaveDoc = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.rulerBar1 = new TXTextControl.RulerBar();
            this.textControl1 = new TXTextControl.TextControl();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.snapDocumentManager1 = new DevExpress.Snap.Extensions.SnapDocumentManager(this.components);
            this.noDocumentsView1 = new DevExpress.XtraBars.Docking2010.Views.NoDocuments.NoDocumentsView(this.components);
            this.tabbedView1 = new DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView(this.components);
            this.behaviorManager1 = new DevExpress.Utils.Behaviors.BehaviorManager(this.components);
            this.btnDocSetup = new System.Windows.Forms.Button();
            this.btnPasteText = new System.Windows.Forms.Button();
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
            this.snapDockManager1.SnapControl = this.snapControl1;
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
            // snapControl1
            // 
            this.snapControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.snapControl1.Location = new System.Drawing.Point(0, 0);
            this.snapControl1.Name = "snapControl1";
            this.snapControl1.Options.DocumentCapabilities.TrackChanges = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            this.snapControl1.Options.SnapMailMergeVisualOptions.DataSourceName = null;
            this.snapControl1.Size = new System.Drawing.Size(909, 291);
            this.snapControl1.TabIndex = 0;
            this.snapControl1.Text = "snapControl1";
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
            this.splitContainer2.Panel1.Controls.Add(this.snapControl1);
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
            this.snapDocumentManager1.ClientControl = this.snapControl1;
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
            // SnapControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1211, 697);
            this.Controls.Add(this.btnPasteText);
            this.Controls.Add(this.btnDocSetup);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.btnSaveDoc);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.bntOpen);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SnapControl";
            this.Text = "SnapTester";
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

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button bntOpen;
        private ExtTextControl textBox1;
        private System.Windows.Forms.Label label1;
        private DevExpress.Snap.Extensions.SnapDockManager snapDockManager1;
        private System.Windows.Forms.Button btnSaveDoc;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private ExtSnapControl snapControl1;
        private DevExpress.Snap.Extensions.SnapDocumentManager snapDocumentManager1;
        private DevExpress.XtraBars.Docking2010.Views.NoDocuments.NoDocumentsView noDocumentsView1;
        private DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView tabbedView1;
        private System.Windows.Forms.TextBox textBox2;
        private DevExpress.Utils.Behaviors.BehaviorManager behaviorManager1;
        private System.Windows.Forms.Button btnDocSetup;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private TXTextControl.TextControl textControl1;
        private System.Windows.Forms.Button btnPasteText;
        private TXTextControl.RulerBar rulerBar1;
    }
}

