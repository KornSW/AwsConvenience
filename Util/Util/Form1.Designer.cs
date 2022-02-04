
namespace Util {
  partial class Form1 {
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
      if (disposing && (components != null)) {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
      this.tabMain = new System.Windows.Forms.TabControl();
      this.tabSNS = new System.Windows.Forms.TabPage();
      this.splitContainer1 = new System.Windows.Forms.SplitContainer();
      this.textBox1 = new System.Windows.Forms.TextBox();
      this.textBox2 = new System.Windows.Forms.TextBox();
      this.txtSnsTopicArn = new System.Windows.Forms.ComboBox();
      this.txtSnsTopicParameterName = new System.Windows.Forms.ComboBox();
      this.btnResolveSnsTopicFromParameterStore = new System.Windows.Forms.Button();
      this.label4 = new System.Windows.Forms.Label();
      this.label3 = new System.Windows.Forms.Label();
      this.groupBox1 = new System.Windows.Forms.GroupBox();
      this.label2 = new System.Windows.Forms.Label();
      this.label1 = new System.Windows.Forms.Label();
      this.txtSecretKey = new System.Windows.Forms.TextBox();
      this.txtAccessKey = new System.Windows.Forms.TextBox();
      this.tabMain.SuspendLayout();
      this.tabSNS.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
      this.splitContainer1.Panel1.SuspendLayout();
      this.splitContainer1.Panel2.SuspendLayout();
      this.splitContainer1.SuspendLayout();
      this.groupBox1.SuspendLayout();
      this.SuspendLayout();
      // 
      // tabMain
      // 
      this.tabMain.Controls.Add(this.tabSNS);
      this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tabMain.Location = new System.Drawing.Point(0, 94);
      this.tabMain.Name = "tabMain";
      this.tabMain.SelectedIndex = 0;
      this.tabMain.Size = new System.Drawing.Size(997, 534);
      this.tabMain.TabIndex = 0;
      // 
      // tabSNS
      // 
      this.tabSNS.Controls.Add(this.splitContainer1);
      this.tabSNS.Controls.Add(this.txtSnsTopicArn);
      this.tabSNS.Controls.Add(this.txtSnsTopicParameterName);
      this.tabSNS.Controls.Add(this.btnResolveSnsTopicFromParameterStore);
      this.tabSNS.Controls.Add(this.label4);
      this.tabSNS.Controls.Add(this.label3);
      this.tabSNS.Location = new System.Drawing.Point(4, 24);
      this.tabSNS.Name = "tabSNS";
      this.tabSNS.Padding = new System.Windows.Forms.Padding(3);
      this.tabSNS.Size = new System.Drawing.Size(989, 506);
      this.tabSNS.TabIndex = 0;
      this.tabSNS.Text = "SNS";
      this.tabSNS.UseVisualStyleBackColor = true;
      // 
      // splitContainer1
      // 
      this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.splitContainer1.Location = new System.Drawing.Point(0, 72);
      this.splitContainer1.Name = "splitContainer1";
      // 
      // splitContainer1.Panel1
      // 
      this.splitContainer1.Panel1.Controls.Add(this.textBox1);
      // 
      // splitContainer1.Panel2
      // 
      this.splitContainer1.Panel2.Controls.Add(this.textBox2);
      this.splitContainer1.Size = new System.Drawing.Size(989, 434);
      this.splitContainer1.SplitterDistance = 329;
      this.splitContainer1.SplitterWidth = 6;
      this.splitContainer1.TabIndex = 4;
      // 
      // textBox1
      // 
      this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.textBox1.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
      this.textBox1.Location = new System.Drawing.Point(0, 0);
      this.textBox1.Multiline = true;
      this.textBox1.Name = "textBox1";
      this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.textBox1.Size = new System.Drawing.Size(329, 434);
      this.textBox1.TabIndex = 0;
      this.textBox1.Text = "outbound topic";
      // 
      // textBox2
      // 
      this.textBox2.Dock = System.Windows.Forms.DockStyle.Fill;
      this.textBox2.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
      this.textBox2.Location = new System.Drawing.Point(0, 0);
      this.textBox2.Multiline = true;
      this.textBox2.Name = "textBox2";
      this.textBox2.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.textBox2.Size = new System.Drawing.Size(654, 434);
      this.textBox2.TabIndex = 0;
      this.textBox2.Text = "inbound topic";
      // 
      // txtSnsTopicArn
      // 
      this.txtSnsTopicArn.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtSnsTopicArn.FormattingEnabled = true;
      this.txtSnsTopicArn.Location = new System.Drawing.Point(107, 43);
      this.txtSnsTopicArn.Name = "txtSnsTopicArn";
      this.txtSnsTopicArn.Size = new System.Drawing.Size(874, 23);
      this.txtSnsTopicArn.TabIndex = 3;
      // 
      // txtSnsTopicParameterName
      // 
      this.txtSnsTopicParameterName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtSnsTopicParameterName.FormattingEnabled = true;
      this.txtSnsTopicParameterName.Location = new System.Drawing.Point(107, 14);
      this.txtSnsTopicParameterName.Name = "txtSnsTopicParameterName";
      this.txtSnsTopicParameterName.Size = new System.Drawing.Size(874, 23);
      this.txtSnsTopicParameterName.TabIndex = 3;
      // 
      // btnResolveSnsTopicFromParameterStore
      // 
      this.btnResolveSnsTopicFromParameterStore.Location = new System.Drawing.Point(68, 43);
      this.btnResolveSnsTopicFromParameterStore.Name = "btnResolveSnsTopicFromParameterStore";
      this.btnResolveSnsTopicFromParameterStore.Size = new System.Drawing.Size(33, 23);
      this.btnResolveSnsTopicFromParameterStore.TabIndex = 2;
      this.btnResolveSnsTopicFromParameterStore.Text = "Res";
      this.btnResolveSnsTopicFromParameterStore.UseVisualStyleBackColor = true;
      // 
      // label4
      // 
      this.label4.AutoSize = true;
      this.label4.Location = new System.Drawing.Point(8, 17);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(93, 15);
      this.label4.TabIndex = 1;
      this.label4.Text = "ParameterName";
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(8, 46);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(54, 15);
      this.label3.TabIndex = 1;
      this.label3.Text = "TopicArn";
      // 
      // groupBox1
      // 
      this.groupBox1.Controls.Add(this.label2);
      this.groupBox1.Controls.Add(this.label1);
      this.groupBox1.Controls.Add(this.txtSecretKey);
      this.groupBox1.Controls.Add(this.txtAccessKey);
      this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
      this.groupBox1.Location = new System.Drawing.Point(0, 0);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(997, 94);
      this.groupBox1.TabIndex = 1;
      this.groupBox1.TabStop = false;
      this.groupBox1.Text = "Connection";
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(12, 54);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(58, 15);
      this.label2.TabIndex = 1;
      this.label2.Text = "SecretKey";
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(12, 25);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(62, 15);
      this.label1.TabIndex = 1;
      this.label1.Text = "AccessKey";
      // 
      // txtSecretKey
      // 
      this.txtSecretKey.Location = new System.Drawing.Point(80, 51);
      this.txtSecretKey.Name = "txtSecretKey";
      this.txtSecretKey.Size = new System.Drawing.Size(345, 23);
      this.txtSecretKey.TabIndex = 0;
      // 
      // txtAccessKey
      // 
      this.txtAccessKey.Location = new System.Drawing.Point(80, 22);
      this.txtAccessKey.Name = "txtAccessKey";
      this.txtAccessKey.Size = new System.Drawing.Size(345, 23);
      this.txtAccessKey.TabIndex = 0;
      // 
      // Form1
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(997, 628);
      this.Controls.Add(this.tabMain);
      this.Controls.Add(this.groupBox1);
      this.Name = "Form1";
      this.Text = "AWS Test Util";
      this.tabMain.ResumeLayout(false);
      this.tabSNS.ResumeLayout(false);
      this.tabSNS.PerformLayout();
      this.splitContainer1.Panel1.ResumeLayout(false);
      this.splitContainer1.Panel1.PerformLayout();
      this.splitContainer1.Panel2.ResumeLayout(false);
      this.splitContainer1.Panel2.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
      this.splitContainer1.ResumeLayout(false);
      this.groupBox1.ResumeLayout(false);
      this.groupBox1.PerformLayout();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.TabControl tabMain;
    private System.Windows.Forms.TabPage tabSNS;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox txtSecretKey;
    private System.Windows.Forms.TextBox txtAccessKey;
    private System.Windows.Forms.ComboBox txtSnsTopicArn;
    private System.Windows.Forms.ComboBox txtSnsTopicParameterName;
    private System.Windows.Forms.Button btnResolveSnsTopicFromParameterStore;
    private System.Windows.Forms.SplitContainer splitContainer1;
    private System.Windows.Forms.TextBox textBox1;
    private System.Windows.Forms.TextBox textBox2;
  }
}

