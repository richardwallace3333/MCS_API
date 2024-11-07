using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace MCSControl
{
    public partial class Form1 : Form
    {
        MCSAPI mcsAPI = new MCSAPI();
        Dictionary<string, Dictionary<string, string>> ParamCategories;
        Dictionary<string, Dictionary<string, string>> StatusCategories;

        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        Dictionary<string, DataGridViewRow> rowDict = new Dictionary<string, DataGridViewRow>();

        public Form1()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            (bool, string) connection = mcsAPI.Connect();
            handleConnect(connection.Item1, connection.Item2);
            updateParamSettings();
            updateStatusSettings();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            mcsAPI.Disconnect();
            handleDisconnect();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            mcsAPI.Start();
            handleStart();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            mcsAPI.Stop();
            handleStop();
        }

        private void btnGetId_Click(object sender, EventArgs e)
        {
            label2.Text = mcsAPI.GetLatestCaptureId();
        }

        private void btnGetImages_Click(object sender, EventArgs e)
        {
            (Image, Image) capturedImages = mcsAPI.GetCaptureImages();
            pictureBox1.Image = capturedImages.Item1;
            pictureBox2.Image = capturedImages.Item2;
        }

        private void btnGetStatus_Click(object sender, EventArgs e)
        {
            label3.Text = mcsAPI.GetStatus();
        }

        private void btnGetCameraCount_Click(object sender, EventArgs e)
        {
            label4.Text = mcsAPI.GetCameraCount();
        }
        private void handleConnect(bool isConnected, string status)
        {
            if (isConnected)
            {
                button1.Enabled = false;
                button2.Enabled = true;
                if (status == "START")
                {
                    button3.Enabled = true;
                    button4.Enabled = false;
                }
                else
                {
                    button3.Enabled = false;
                    button4.Enabled = true;
                }
                button5.Enabled = true;
                button6.Enabled = true;
                button7.Enabled = true;
                button8.Enabled = true;
                button11.Enabled = true;
                label1.Text = "Status: Connected";
            }
        }
        private void handleDisconnect()
        {
            button1.Enabled = true;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button8.Enabled = false;
            button11.Enabled = false;
            label1.Text = "Status: Disconnected";
        }

        private void handleStart()
        {
            button3.Enabled = false;
            button4.Enabled = true;
        }

        private void handleStop()
        {
            button3.Enabled = true;
            button4.Enabled = false;
        }
        private void btnAddCamera_Click(object sender, EventArgs e)
        {
            mcsAPI.AddCamera();
            updateParamSettings();
        }

        private void btnRemoveCamera_Click(object sender, EventArgs e)
        {
            mcsAPI.RemoveCamera();
            updateParamSettings();
        }


        private void btnGetRecordingRootDIR_Click(object sender, EventArgs e)
        {
            label5.Text = mcsAPI.GetRecordingRootDIR();
        }

        private void updateParamSettings()
        {
            ParamCategories = mcsAPI.GetParamSettings();
            BuildPagesFromParamStorage();
        }
        private void updateStatusSettings()
        {
            StatusCategories = mcsAPI.GetStatusSettings();
            BuildPagesFromStatusStorage();
        }

        private void btnSetRecordingRootDIR_Click(object sender, EventArgs e)
        {
            
        }

        private void btnUpdateParamSettings_Click(object sender, EventArgs e)
        {
            updateParamSettings();
        }

        private void btnUpdateStatusSettings_Click(object sender, EventArgs e)
        {
            updateStatusSettings();
        }

        private void BuildPagesFromParamStorage()
        {
            tabControl1.TabPages.Clear();
            foreach (KeyValuePair<string, Dictionary<string, string>> kvp in ParamCategories)
            {
                TabPage TP = new TabPage(kvp.Key);
                if (kvp.Key != "Pass / Fail Settings" && kvp.Key != "Defect Algorithm Settings" && kvp.Key != "Pallet Dimensions" && kvp.Key != "BlockROIAreas")
                {
                    TP.Tag = kvp.Value;
                    tabControl1.TabPages.Add(TP);
                }

                DataGridView DGV = new DataGridView();
                DGV.Columns.Clear();
                DGV.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                DGV.Columns.Add("Parameter", "Parameter");
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

                    int Row = DGV.Rows.Add();
                    DGV.Rows[Row].SetValues(new string[] { Params.Key, Params.Value });
                    DGV.Rows[Row].Tag = kvp.Value;
                }

                DGV.CellValueChanged += DGV_CellValueChanged;
            }

            void DGV_CellValueChanged(object sender, DataGridViewCellEventArgs e)
            {
                DataGridView DGV = (DataGridView)sender;
                DataGridViewRow DVR = DGV.Rows[e.RowIndex];

                Dictionary<string, string> Param = DVR.Tag as Dictionary<string, string>;
                var test1 = DVR.Cells[0].Value as string;
                var test2 = DVR.Cells[1].Value as string;
                Param[DVR.Cells[0].Value as string] = DVR.Cells[1].Value as string;
                mcsAPI.UpdateSettings(ParamCategories);
            }
        }

        private void BuildPagesFromStatusStorage()
        {
            tabControl2.TabPages.Clear();
            rowDict.Clear();

            // Build controls
            foreach (KeyValuePair<string, Dictionary<string, string>> kvp in StatusCategories)
            {
                TabPage TP = new TabPage(kvp.Key);
                TP.Tag = kvp.Value;
                tabControl2.TabPages.Add(TP);

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
            }
        }
    }
}
