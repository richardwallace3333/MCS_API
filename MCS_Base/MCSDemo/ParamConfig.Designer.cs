﻿
namespace PalletCheck
{
    partial class ParamConfig
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
            this.tabCategories = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btnLoadSettings = new System.Windows.Forms.Button();
            this.btnSaveSettings = new System.Windows.Forms.Button();
            this.btnCalcBaseline = new System.Windows.Forms.Button();
            this.btnAddCamera = new System.Windows.Forms.Button();
            this.btnRemoveCamera = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tabCategories.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabCategories
            // 
            this.tabCategories.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabCategories.Controls.Add(this.tabPage1);
            this.tabCategories.Controls.Add(this.tabPage2);
            this.tabCategories.Location = new System.Drawing.Point(0, 73);
            this.tabCategories.Name = "tabCategories";
            this.tabCategories.SelectedIndex = 0;
            this.tabCategories.Size = new System.Drawing.Size(760, 557);
            this.tabCategories.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(752, 531);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(752, 389);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btnLoadSettings
            // 
            this.btnLoadSettings.BackColor = System.Drawing.SystemColors.Control;
            this.btnLoadSettings.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLoadSettings.Location = new System.Drawing.Point(97, 9);
            this.btnLoadSettings.Name = "btnLoadSettings";
            this.btnLoadSettings.Size = new System.Drawing.Size(141, 35);
            this.btnLoadSettings.TabIndex = 1;
            this.btnLoadSettings.Text = "LOAD SETTINGS";
            this.btnLoadSettings.UseVisualStyleBackColor = false;
            this.btnLoadSettings.Click += new System.EventHandler(this.btnLoadSettings_Click);
            // 
            // btnSaveSettings
            // 
            this.btnSaveSettings.BackColor = System.Drawing.SystemColors.Control;
            this.btnSaveSettings.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSaveSettings.Location = new System.Drawing.Point(261, 9);
            this.btnSaveSettings.Name = "btnSaveSettings";
            this.btnSaveSettings.Size = new System.Drawing.Size(141, 35);
            this.btnSaveSettings.TabIndex = 2;
            this.btnSaveSettings.Text = "SAVE SETTINGS";
            this.btnSaveSettings.UseVisualStyleBackColor = false;
            this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
            // 
            // btnCalcBaseline
            // 
            this.btnCalcBaseline.BackColor = System.Drawing.SystemColors.Control;
            this.btnCalcBaseline.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCalcBaseline.Location = new System.Drawing.Point(425, 9);
            this.btnCalcBaseline.Name = "btnCalcBaseline";
            this.btnCalcBaseline.Size = new System.Drawing.Size(141, 35);
            this.btnCalcBaseline.TabIndex = 3;
            this.btnCalcBaseline.Text = "CALC BASELINE";
            this.btnCalcBaseline.UseVisualStyleBackColor = false;
            this.btnCalcBaseline.Click += new System.EventHandler(this.btnCalcBaseline_Click);
            // 
            // btnAddCamera
            // 
            this.btnAddCamera.BackColor = System.Drawing.SystemColors.Control;
            this.btnAddCamera.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddCamera.Location = new System.Drawing.Point(589, 9);
            this.btnAddCamera.Name = "btnAddCamera";
            this.btnAddCamera.Size = new System.Drawing.Size(141, 35);
            this.btnAddCamera.TabIndex = 4;
            this.btnAddCamera.Text = "ADD CAMERA";
            this.btnAddCamera.UseVisualStyleBackColor = false;
            this.btnAddCamera.Click += new System.EventHandler(this.btnAddCamera_Click);
            // 
            // btnRemoveCamera
            // 
            this.btnRemoveCamera.BackColor = System.Drawing.SystemColors.Control;
            this.btnRemoveCamera.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRemoveCamera.Location = new System.Drawing.Point(753, 9);
            this.btnRemoveCamera.Name = "btnRemoveCamera";
            this.btnRemoveCamera.Size = new System.Drawing.Size(141, 35);
            this.btnRemoveCamera.TabIndex = 5;
            this.btnRemoveCamera.Text = "REMOVE CAMERA";
            this.btnRemoveCamera.UseVisualStyleBackColor = false;
            this.btnRemoveCamera.Click += new System.EventHandler(this.btnRemoveCamera_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btnCalcBaseline);
            this.panel1.Controls.Add(this.btnLoadSettings);
            this.panel1.Controls.Add(this.btnSaveSettings);
            this.panel1.Controls.Add(this.btnAddCamera);
            this.panel1.Controls.Add(this.btnRemoveCamera);
            this.panel1.Location = new System.Drawing.Point(4, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(743, 55);
            this.panel1.TabIndex = 4;
            // 
            // ParamConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(759, 630);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.tabCategories);
            this.Name = "ParamConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Settings Editor";
            this.tabCategories.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabCategories;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button btnLoadSettings;
        private System.Windows.Forms.Button btnSaveSettings;
        private System.Windows.Forms.Button btnCalcBaseline;
        private System.Windows.Forms.Button btnAddCamera;
        private System.Windows.Forms.Button btnRemoveCamera;
        private System.Windows.Forms.Panel panel1;
    }
}