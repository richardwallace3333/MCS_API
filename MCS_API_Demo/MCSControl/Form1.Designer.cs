using System;

namespace MCSControl
{
    partial class Form1
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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.button5 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.button6 = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.button7 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.button8 = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.button9 = new System.Windows.Forms.Button();
            this.button10 = new System.Windows.Forms.Button();
            this.button11 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.button12 = new System.Windows.Forms.Button();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.button13 = new System.Windows.Forms.Button();
            this.button14 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(16, 100);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(147, 65);
            this.button1.TabIndex = 0;
            this.button1.Text = "Connect";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // button2
            // 
            this.button2.Enabled = false;
            this.button2.Location = new System.Drawing.Point(174, 100);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(147, 65);
            this.button2.TabIndex = 1;
            this.button2.Text = "Disconnect";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // button3
            // 
            this.button3.Enabled = false;
            this.button3.Location = new System.Drawing.Point(16, 178);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(147, 65);
            this.button3.TabIndex = 2;
            this.button3.Text = "Start";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // button4
            // 
            this.button4.Enabled = false;
            this.button4.Location = new System.Drawing.Point(174, 178);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(147, 65);
            this.button4.TabIndex = 3;
            this.button4.Text = "Stop";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 44);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(216, 25);
            this.label1.TabIndex = 4;
            this.label1.Text = "Status: Disconnected";
            // 
            // button5
            // 
            this.button5.Enabled = false;
            this.button5.Location = new System.Drawing.Point(16, 414);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(305, 65);
            this.button5.TabIndex = 5;
            this.button5.Text = "Get Latest Catpure Id";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.btnGetId_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(334, 434);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(0, 25);
            this.label2.TabIndex = 6;
            // 
            // button6
            // 
            this.button6.Enabled = false;
            this.button6.Location = new System.Drawing.Point(16, 491);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(305, 65);
            this.button6.TabIndex = 7;
            this.button6.Text = "Get Capture Images";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.btnGetImages_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(16, 575);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(161, 145);
            this.pictureBox1.TabIndex = 8;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Location = new System.Drawing.Point(183, 575);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(163, 145);
            this.pictureBox2.TabIndex = 9;
            this.pictureBox2.TabStop = false;
            // 
            // button7
            // 
            this.button7.Enabled = false;
            this.button7.Location = new System.Drawing.Point(16, 258);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(305, 65);
            this.button7.TabIndex = 10;
            this.button7.Text = "Get Status";
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.btnGetStatus_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(334, 278);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(0, 25);
            this.label3.TabIndex = 11;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(334, 355);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(0, 25);
            this.label4.TabIndex = 13;
            // 
            // button8
            // 
            this.button8.Enabled = false;
            this.button8.Location = new System.Drawing.Point(16, 335);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(305, 65);
            this.button8.TabIndex = 12;
            this.button8.Text = "Get Camera Count";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.btnGetCameraCount_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(428, 100);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(693, 854);
            this.tabControl1.TabIndex = 14;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(8, 39);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(677, 807);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(8, 39);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(677, 807);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(427, 24);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(147, 65);
            this.button9.TabIndex = 15;
            this.button9.Text = "Add Camera";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.btnAddCamera_Click);
            // 
            // button10
            // 
            this.button10.Location = new System.Drawing.Point(592, 24);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(195, 65);
            this.button10.TabIndex = 16;
            this.button10.Text = "Remove Camera";
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.btnRemoveCamera_Click);
            // 
            // button11
            // 
            this.button11.Enabled = false;
            this.button11.Location = new System.Drawing.Point(17, 739);
            this.button11.Name = "button11";
            this.button11.Size = new System.Drawing.Size(305, 65);
            this.button11.TabIndex = 17;
            this.button11.Text = "Get Recording Root DIR";
            this.button11.UseVisualStyleBackColor = true;
            this.button11.Click += new System.EventHandler(this.btnGetRecordingRootDIR_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(25, 818);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(0, 25);
            this.label5.TabIndex = 18;
            // 
            // button12
            // 
            this.button12.Enabled = false;
            this.button12.Location = new System.Drawing.Point(17, 862);
            this.button12.Name = "button12";
            this.button12.Size = new System.Drawing.Size(305, 65);
            this.button12.TabIndex = 19;
            this.button12.Text = "Set Recording Root DIR";
            this.button12.UseVisualStyleBackColor = true;
            this.button12.Click += new System.EventHandler(this.btnSetRecordingRootDIR_Click);
            // 
            // tabControl2
            // 
            this.tabControl2.Controls.Add(this.tabPage3);
            this.tabControl2.Controls.Add(this.tabPage4);
            this.tabControl2.Location = new System.Drawing.Point(1127, 100);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(779, 846);
            this.tabControl2.TabIndex = 20;
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(8, 39);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(763, 799);
            this.tabPage3.TabIndex = 0;
            this.tabPage3.Text = "tabPage3";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Location = new System.Drawing.Point(8, 39);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(763, 799);
            this.tabPage4.TabIndex = 1;
            this.tabPage4.Text = "tabPage4";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // button13
            // 
            this.button13.Location = new System.Drawing.Point(918, 24);
            this.button13.Name = "button13";
            this.button13.Size = new System.Drawing.Size(195, 65);
            this.button13.TabIndex = 21;
            this.button13.Text = "Refresh";
            this.button13.UseVisualStyleBackColor = true;
            this.button13.Click += new System.EventHandler(this.btnUpdateParamSettings_Click);
            // 
            // button14
            // 
            this.button14.Location = new System.Drawing.Point(1703, 44);
            this.button14.Name = "button14";
            this.button14.Size = new System.Drawing.Size(195, 65);
            this.button14.TabIndex = 22;
            this.button14.Text = "Refresh";
            this.button14.UseVisualStyleBackColor = true;
            this.button14.Click += new System.EventHandler(this.btnUpdateStatusSettings_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1919, 1208);
            this.Controls.Add(this.button14);
            this.Controls.Add(this.button13);
            this.Controls.Add(this.tabControl2);
            this.Controls.Add(this.button12);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button11);
            this.Controls.Add(this.button10);
            this.Controls.Add(this.button9);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.button8);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.button7);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabControl2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Button button10;
        private System.Windows.Forms.Button button11;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button button12;
        private System.Windows.Forms.TabControl tabControl2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.Button button13;
        private System.Windows.Forms.Button button14;
    }
}

