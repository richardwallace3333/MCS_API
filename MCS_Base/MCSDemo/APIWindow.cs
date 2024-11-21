﻿using PalletCheck;
using System;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using Sick.GenIStream;

namespace PalletCheck
{
    public partial class APIWindow : Form
    {
        System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
        System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();

        Dictionary<string, DataGridViewRow> rowDict = new Dictionary<string, DataGridViewRow>();
        System.Windows.Forms.Timer ScreenshotTimer = new System.Windows.Forms.Timer();
        string SnapshotDir;

        public APIWindow()
        {
            InitializeComponent();

            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;

            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;


            BuildPagesFromStatusStorage();

            StatusStorage.OnStatusChanged_Callback += StatusChangedCB;

            ScreenshotTimer.Interval = 1000;
            ScreenshotTimer.Tick += ScreenshotTimerCB;
        }

        private void StatusChangedCB(string category, string field, string value)
        {
            string full_key = category + field;
            if (rowDict.ContainsKey(full_key))
            {
                DataGridViewRow row = rowDict[full_key];
                row.Cells[1].Value = value; 
            }
        }


        private void BuildPagesFromStatusStorage()
        {
            try
            {
                tabCategories.TabPages.Clear();
                rowDict.Clear();

                // Build controls
                TabPage TP1 = new TabPage("SDK settings");
                TP1.Tag = "Tab1";
                tabCategories.TabPages.Add(TP1);
                TabPage TP2 = new TabPage("HTTP server settings");
                TP2.Tag = "Tab2";
                tabCategories.TabPages.Add(TP2);
                TabPage TP3 = new TabPage("FTP settings");
                TP3.Tag = "Tab3";
                tabCategories.TabPages.Add(TP3);

                string[] sdkSettingKeys = { "Camera Resolution", "Frame Rate", "Image Format", "Focus Mode", "Lighting Conditions" };
                string[] sdkSettingValues = { "1920x1080", "30", "Image Format", "JPEG", "manual", "enable flash" };
                string[] httpSettingKeys = { "Base URL", "Request Timeout", "Authentication Method", "Content Type", "Retry Limit" };
                string[] httpSettingValues = { "", "30", "Basic Auth", "application/json", "" };
                string[] ftpSettingKeys = { "Server Address", "Port Number", "Username", "Password", "Transfer Mode" };
                string[] ftpSettingValues = { "", "21", "", "", "Binary" };

                for (int tabIndex = 0; tabIndex < 3; tabIndex ++)
                {
                    DataGridView DGV = new DataGridView();
                    DGV.Columns.Clear();
                    DGV.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    DGV.Columns.Add("Description", "Description");
                    DGV.Columns[0].ReadOnly = true;
                    DGV.Columns.Add("Value", "Value");
                    DGV.AllowUserToAddRows = false;
                    DGV.AllowUserToOrderColumns = false;
                    DGV.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
                    DGV.DefaultCellStyle = dataGridViewCellStyle2;
                    DGV.RowHeadersVisible = false;

                    string[] Keys;
                    string[] Values;
                    if (tabIndex == 0)
                    {
                        TP1.Controls.Add(DGV);
                        Keys = sdkSettingKeys;
                        Values = sdkSettingValues;
                    } else if (tabIndex == 1)
                    {
                        TP2.Controls.Add(DGV);
                        Keys = httpSettingKeys;
                        Values = httpSettingValues;
                    } else
                    {
                        TP3.Controls.Add(DGV);
                        Keys = ftpSettingKeys;
                        Values = ftpSettingValues;
                    }
                    DGV.Dock = DockStyle.Fill;
                    DGV.Rows.Clear();

             
                    for (int i = 0; i < 5; i++)
                    {
                        string category_key = Keys[i];
                        string params_key = Keys[i];
                        string params_value = Values[i];
                        string full_key = category_key + params_key;
                        int iRow = DGV.Rows.Add();
                        DataGridViewRow row = DGV.Rows[iRow];
                        row.SetValues(new string[] { params_key, params_value });
                        row.Tag = params_value;
                        rowDict.Add(full_key, row);
                        DGV.CellValueChanged += DGV_CellValueChanged;
                    }
                }
               
                /*foreach (KeyValuePair<string, Dictionary<string, string>> kvp in StatusStorage.Categories)
                {
                    TabPage TP = new TabPage(kvp.Key);
                    TP.Tag = kvp.Value;
                    tabCategories.TabPages.Add(TP);

                    DataGridView DGV = new DataGridView();
                    DGV.Columns.Clear();
                    DGV.ReadOnly = true;
                    DGV.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    DGV.Columns.Add("Description", "Description");
                    DGV.Columns[0].ReadOnly = true;
                    DGV.Columns.Add("Value", "Value");
                    DGV.AllowUserToAddRows = false;
                    DGV.AllowUserToOrderColumns = false;
                    DGV.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
                    DGV.DefaultCellStyle = dataGridViewCellStyle2;
                    DGV.RowHeadersVisible = false;
                    TP.Controls.Add(DGV);
                    DGV.Dock = DockStyle.Fill;
                    DGV.Rows.Clear();
                    foreach (KeyValuePair<string, string> Params in kvp.Value)
                    {
                        if (string.IsNullOrEmpty(Params.Key))
                            continue;

                        string category_key = kvp.Key;
                        string params_key = Params.Key;
                        string params_value = Params.Value;
                        string full_key = category_key + params_key;

                        int iRow = DGV.Rows.Add();
                        DataGridViewRow row = DGV.Rows[iRow];
                        row.SetValues(new string[] { Params.Key, Params.Value });
                        row.Tag = kvp.Value;

                        rowDict.Add(full_key, row);
                    }

                    DGV.CellValueChanged += DGV_CellValueChanged;
                }*/
            }
            catch(Exception ex)
            {
                Logger.WriteException(ex);
            }
        }

        private void DGV_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            //DataGridView DGV = (DataGridView)sender;
            //DataGridViewRow DVR = DGV.Rows[e.RowIndex];

            //Dictionary<string, string> Param = DVR.Tag as Dictionary<string, string>;

            //Param[DVR.Cells[0].Value as string] = DVR.Cells[1].Value as string;

            //ParamStorage.Save(ParamStorage.LastFilename);
        }

        private void CopyLastNFiles(string pattern, int n, string srcdir, string dstdir)
        {
            try
            {
                System.IO.Directory.CreateDirectory(dstdir);

                string[] filePaths = Directory.GetFiles(srcdir, pattern, SearchOption.TopDirectoryOnly);
                if(filePaths.Length > 0)
                {
                    Array.Sort(filePaths);
                    Array.Reverse(filePaths);
                    n = Math.Min(n, filePaths.Length);
                    for (int i = 0; i < n; i++)
                    {
                        string srcfilename = filePaths[i];
                        string dstfilename = srcfilename.Replace(srcdir, dstdir);
                        Logger.WriteLine(string.Format("Snapshot Copy: {0} to {1}", srcfilename, dstfilename));
                        File.Copy(srcfilename, dstfilename, true);
                    }
                }
            }
            catch(Exception e)
            {
                Logger.WriteException(e);
            }
        }


        private Sick.GenIStream.ConfigurationStatus TryParameters(ICamera Camera, string Filename)
        {
            if (Camera != null && Camera.IsConnected)
            {
                ConfigurationResult Result = Camera.ImportParameters(Filename);
                return Result.Status;
            }
            return Sick.GenIStream.ConfigurationStatus.ERROR_GENERIC;
        }


        private string[] ReplaceConfigLine(string[] Lines, string Name, string Value)
        {
            string[] newLines = (string[])Lines.Clone();
            for(int i=0; i<newLines.Length; i++)
            {
                string[] parts = newLines[i].Split(',');
                if (parts[0]==Name)
                {
                    newLines[i] = Name + "," + Value;
                    return newLines;
                }
            }
            Logger.WriteLine("ReplaceConfigLine:: COULD NOT FIND VALUE IN CONFIG: " + Name);
            return null;
        }

        private int FindHighestHz(ICamera Camera, string[] SourceConfigLines)
        {
            string ConfigDir = "F:\\LucidCloud\\Projects\\SICK_PalletCheck\\Ranger3Benchmarking";
            int[] StepSizes = { 1000, 200, 100, 50 };
            int BestHz = 100;
            int HzH = 10000;
            Sick.GenIStream.ConfigurationStatus Status = ConfigurationStatus.OK;

            foreach (int StepSize in StepSizes)
            {
                Logger.WriteLine("COnfiguration search stepsize = " + StepSize.ToString());
                int Hz = BestHz;
                while (Hz <= HzH)
                {
                    string[] ConfigLines = ReplaceConfigLine(SourceConfigLines, "AcquisitionLineRate", Hz.ToString());
                    File.WriteAllLines(ConfigDir + "\\TweakedConfiguration.csv", ConfigLines);
                    Status = TryParameters(Camera, ConfigDir + "\\TweakedConfiguration.csv");

                    if (Status == ConfigurationStatus.OK)
                    {
                        if (Hz > BestHz) BestHz = Hz;
                        Logger.WriteLine("Configuration was ok with Hz = " + Hz.ToString());
                    }
                    else
                    {
                        Logger.WriteLine("Configuration FAILED with Hz = " + Hz.ToString());
                        break;
                    }

                    Hz += StepSize;
                }
            }

            Logger.WriteLine("Best Hz = " + BestHz.ToString());
            return BestHz;
        }

        /*
        private void DoCameraParameterScan()
        {
            R3Cam R3Camera = MainWindow.Singleton.Camera1;
            ICamera Camera = R3Camera.Camera;
            string ConfigDir = "F:\\LucidCloud\\Projects\\SICK_PalletCheck\\Ranger3Benchmarking";

            Sick.GenIStream.ConfigurationStatus Status = ConfigurationStatus.OK;

            string[] BaseConfigLines = File.ReadAllLines(ConfigDir + "\\BaseConfiguration.csv");


            for(int ExposureTime=25; ExposureTime<=1250; ExposureTime+=25)
            //for (int ExposureTime = 100; ExposureTime <= 1000; ExposureTime += 100)
            {
                Logger.WriteBorder("ExposureTime = " + ExposureTime.ToString());
                string OutputCSVPath = string.Format("F:\\LucidCloud\\Projects\\SICK_PalletCheck\\Ranger3Benchmarking\\Ranger3Rates_{0:0000}.csv",ExposureTime);
                if(File.Exists(OutputCSVPath))
                {
                    Logger.WriteLine("File already exists...");
                    continue;
                }

                File.WriteAllText(OutputCSVPath, "ExposureTime,WindowHeight,MaxHz\n");

                string[] ExpConfigLines = ReplaceConfigLine(BaseConfigLines, "ExposureTime_RegionSelector_Region0", ExposureTime.ToString());
                         ExpConfigLines = ReplaceConfigLine(ExpConfigLines, "ExposureTime_RegionSelector_Region1", ExposureTime.ToString());

                
                for (int WindowHeight = 16; WindowHeight <= 832; WindowHeight += 16)
                {
                    Logger.WriteBorder("WindowHeight = " + WindowHeight.ToString());

                    string[] ConfigLines = ReplaceConfigLine(ExpConfigLines, "Height_RegionSelector_Region1", WindowHeight.ToString());
                    int BestHz = FindHighestHz(Camera, ConfigLines);

                    List<string> Lines = new List<string>();
                    Lines.Add(ExposureTime.ToString() + "," + WindowHeight.ToString() + "," + BestHz.ToString());
                    File.AppendAllLines(OutputCSVPath, Lines);
                }
            }






            //Status = TryParameters(Camera, ConfigDir + "\\BaseConfiguration.csv");
            //Logger.WriteLine("BaseConfiguration: " + Status.ToString());

            //Status = TryParameters(Camera, ConfigDir + "\\ConfigurationA.csv");
            //Logger.WriteLine("ConfigurationA: " + Status.ToString());



        }*/
        private void SaveScreenshot(string dstfilename)
        {
            try
            {
                double screenLeft = SystemParameters.VirtualScreenLeft;
                double screenTop = SystemParameters.VirtualScreenTop;
                double screenWidth = SystemParameters.VirtualScreenWidth;
                double screenHeight = SystemParameters.VirtualScreenHeight;

                using (Bitmap bmp = new Bitmap((int)screenWidth, (int)screenHeight))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        Opacity = .0;
                        g.CopyFromScreen((int)screenLeft, (int)screenTop, 0, 0, bmp.Size);
                        bmp.Save(dstfilename);
                        Opacity = 1;
                    }

                }
            }
            catch(Exception e)
            {
                Logger.WriteException(e);
            }
        }

        private void ScreenshotTimerCB(object sender, EventArgs e)
        {
            SaveScreenshot(SnapshotDir + "\\Screenshot.png");

            this.Visible = true;
            ScreenshotTimer.Enabled = false;

            ZipFile.CreateFromDirectory(SnapshotDir, SnapshotDir + ".zip");

            System.Windows.Forms.MessageBox.Show("Snapshot has been created at:\n\n" + SnapshotDir, "SNAPSHOT COMPLETED!");
        }
    }
}
