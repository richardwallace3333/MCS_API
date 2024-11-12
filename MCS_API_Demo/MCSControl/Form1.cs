using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetMQ.Sockets;
using NetMQ;

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

        // ZMQ
        private Task _listenerTask;
        private bool _isListening;
        public Form1()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            this.Size = new Size(1012, 532);
            this.Text = "MCS API"; // Set your application name here
            handleDisconnect();
            StartZMQListener();
        }

        private void StartZMQListener()
        {
            _isListening = true;
            _listenerTask = Task.Run(() =>
            {
                using (var server = new ResponseSocket("@tcp://*:6666"))
                {
                    while (_isListening)
                    {
                        var message = server.ReceiveFrameString();
                        Console.WriteLine("Received: " + message);
                        var mcsMessage = JsonConvert.DeserializeObject<MCSMessage>(message);
                        this.Invoke((MethodInvoker)delegate
                        {
                            ProcessCommand(mcsMessage, server);
                        });
                    }
                }
            });
        }
        private void ProcessCommand(MCSMessage message, ResponseSocket server)
        {
            switch (message.Command)
            {
                case MCSCommand.CaptureId:
                    label2.Text = message.Parameter1;
                    break;
            }
            var jsonMessage = JsonConvert.SerializeObject(message);
            server.SendFrame(jsonMessage);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            (bool, string) connection = mcsAPI.Connect();
            if (connection.Item1)
            {
                handleConnect(connection.Item2);
                updateParamSettings();
                updateStatusSettings();
            }
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
            if (capturedImages.Item1 != null)
            {
                pictureBox1.Image = capturedImages.Item1;
            }
            if (capturedImages.Item2 != null)
            {
                pictureBox2.Image = capturedImages.Item2;
            }
        }

        private void btnGetStatus_Click(object sender, EventArgs e)
        {
            label3.Text = mcsAPI.GetStatus();
        }

        private void btnGetCameraCount_Click(object sender, EventArgs e)
        {
            label4.Text = mcsAPI.GetCameraCount();
        }
        private void handleConnect(string status)
        {
            disableButton(button1);
            enableButton(button2);
            if (status == "START")
            {
                enableButton(button3);
                disableButton(button4);
            }
            else
            {
                disableButton(button3);
                enableButton(button4);
            }
            enableButton(button5);
            enableButton(button6);
            enableButton(button7);
            enableButton(button8);
            enableButton(button9);
            enableButton(button10);
            enableButton(button11);
            enableButton(button12);
            enableButton(button13);
            enableButton(button14);
            enableButton(button15);
            label1.Text = "Status: Connected";
        }
        private void handleDisconnect()
        {
            enableButton(button1);
            disableButton(button2);
            disableButton(button3);
            disableButton(button4);
            disableButton(button5);
            disableButton(button6);
            disableButton(button7);
            disableButton(button8);
            disableButton(button9);
            disableButton(button10);
            disableButton(button11);
            disableButton(button12);
            disableButton(button13);
            disableButton(button14);
            disableButton(button15);
            label1.Text = "Status: Disconnected";
        }

        private void handleStart()
        {
            disableButton(button3);
            enableButton(button4);
        }

        private void handleStop()
        {
            enableButton(button3);
            disableButton(button4);
        }

        private void enableButton(Button button)
        {
            button.Enabled = true;
            button.ForeColor = Color.White;
            if (button == button4)
            {
                button.BackColor = Color.Red;
            }
            else if (button == button11 || button == button12)
            {
                button.BackColor = Color.Purple;
            }
            else
            {
                button.BackColor = ColorTranslator.FromHtml("#2D5774");
            }
        }

        private void disableButton(Button button)
        {
            button.Enabled = false;
            button.ForeColor = Color.Black;
            button.BackColor = Color.Gray;
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
        private void btnSetRecordingRootDIR_Click(object sender, EventArgs e)
        {
            var RecordingRootDir = mcsAPI.GetRecordingRootDIR();
            FolderBrowserDialog OFD = new FolderBrowserDialog();
            OFD.RootFolder = Environment.SpecialFolder.MyComputer;
            OFD.SelectedPath = RecordingRootDir;
            OFD.ShowNewFolderButton = false;
            if (OFD.ShowDialog() == DialogResult.OK)
            {
                mcsAPI.setRecordingRootDIR(OFD.SelectedPath);
                label5.Text = mcsAPI.GetRecordingRootDIR();
            }
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

        private void btnSave_Click(object sender, EventArgs e)
        {
            mcsAPI.saveParamSettings();
        }
    }
}
