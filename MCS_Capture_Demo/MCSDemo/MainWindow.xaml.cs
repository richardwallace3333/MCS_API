using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; 
using System.Windows.Media;
using PalletCheck.Controls;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using static PalletCheck.Pallet;
using MCS;
using System.Windows.Shapes;
using NetMQ.Sockets;
using NetMQ;
using Newtonsoft.Json;
using System.Runtime.Remoting.Contexts;
using System.Windows.Media.Imaging;
using MCSControl;
using System.Data;

namespace PalletCheck
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //public static int           SensorWidth = 2560;
        public MCSCamera Camera;
        public static MainWindow Singleton;
        List<UInt16[]> IncomingScan = new List<UInt16[]>();

        public bool RecordModeEnabled { get; private set; }
        public string RecordDir = "";

        InspectionReport IR;
        PalletProcessor PProcessor;

        Statistics Stats = new Statistics();

        Brush SaveSettingsButtonBrush;

        string[] AllFilesInFolder;
        int LastFileIdx = -1;

        bool BypassModeEnabled = false;
        //static string LastPalletSavedName = "";

        public static string Version = "v1.0";
        public static string RootDir = "";
        public static string ConfigRootDir = "";
        public static string HistoryRootDir = "";
        public static string RecordingRootDir = "";
        public static string ProcessPalletLastDir = "";
        public static string SegmentationErrorRootDir = "";
        public static string LoggingRootDir = "";
        public static string ExceptionsRootDir = "";
        public static string SettingsHistoryRootDir = "";
        public static string CameraConfigHistoryRootDir = "";
        public static string SnapshotsRootDir = "";
        public static string ExeRootDir = "";
        public static string SiteName = "";

        // UInt16[] IncomingBuffer;
        delegate CaptureBufferBrowser addBufferCB(string s, CaptureBuffer CB);

        PLCComms PLC;
        StorageWatchdog Storage;
        Logger Log = new Logger();

        static bool cam_error = false;
        static bool segmentation_error = false;
        static bool file_storage_error = false;
        static bool unknown_error = false;

        int ProcessRecordingTotalPalletCount = 0;
        int ProcessRecordingFinishedPalletCount = 0;

        Brush ButtonBackgroundBrush;
        MCSFrame tempFrame;
        //MCSAPI mcsAPI = new MCSAPI();

        public static string _LastUsedParamFile = "";
        public static string LastUsedParamFile
        {
            get
            {
                return _LastUsedParamFile;
            }
            set
            {
                _LastUsedParamFile = value;
                System.IO.File.WriteAllText(HistoryRootDir + "\\LastUsedParamFile.txt", _LastUsedParamFile);
            }
        }

        // Production stats
        public class PalletStat
        {
            public DateTime Start;
            public DateTime End;
            public int Total;
            public int Fail;

            public PalletStat()
            {
                Start = DateTime.Now;
                Total = 0;
                Fail = 0;
            }
        }

        // ZMQ
        private Task _listenerTask;
        private bool _isListening;

        public List<PalletStat> PalletStats = new List<PalletStat>();

        public static Process PriorProcess()
        // Returns a System.Diagnostics.Process pointing to
        // a pre-existing process with the same name as the
        // current one, if any; or null if the current process
        // is unique.
        {
            Process curr = Process.GetCurrentProcess();
            Process[] procs = Process.GetProcessesByName(curr.ProcessName);
            foreach (Process p in procs)
            {
                if ((p.Id != curr.Id) &&
                    (p.MainModule.FileName == curr.MainModule.FileName))
                    return p;
            }
            return null;
        }

        //=====================================================================
        public MainWindow()
        {
            Process prior = PriorProcess();
            if (prior != null)
            {
                if (MessageBox.Show("Another instance of PalletCheck is already running!\n\nDo you want to start a NEW instance?", "PALLETCHECK Already Running", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    prior.Kill();
                    System.Threading.Thread.Sleep(2000);
                }
                else
                {
                    return;
                }
            }


            Trace.WriteLine("MainWindow");
            Singleton = this;

            // Global exception handling  
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => CurrentDomainOnUnhandledException(args);


            // Setup root directories
            RootDir = Environment.GetEnvironmentVariable("MCS_ROOT_DIR");
            if (RootDir == null)
            {
                MessageBox.Show("Could not find MCS_ROOT_DIR env var");
                Close();
                return;
            }
            RootDir = System.IO.Path.GetFullPath(RootDir).TrimEnd('\\');

            ConfigRootDir = RootDir + "\\Config";
            HistoryRootDir = RootDir + "\\History";
            if (File.Exists(HistoryRootDir + "\\LastUsedRecordingDIRFile.txt"))
            {
                RecordingRootDir = File.ReadAllText(HistoryRootDir + "\\LastUsedRecordingDIRFile.txt");
            }
            else
            {
                RecordingRootDir = RootDir + "\\Recordings";
            }

            SegmentationErrorRootDir = RecordingRootDir + "\\SegmentationErrors";
            LoggingRootDir = RootDir + "\\Logging";
            ExceptionsRootDir = RootDir + "\\Logging\\Exceptions";
            SettingsHistoryRootDir = HistoryRootDir + "\\SettingsHistory";
            CameraConfigHistoryRootDir = HistoryRootDir + "\\CameraConfigHistory";
            SnapshotsRootDir = HistoryRootDir + "\\Snapshots";
            ExeRootDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Trim();

            System.IO.Directory.CreateDirectory(ConfigRootDir);
            System.IO.Directory.CreateDirectory(HistoryRootDir);
            System.IO.Directory.CreateDirectory(RecordingRootDir);
            System.IO.Directory.CreateDirectory(SegmentationErrorRootDir);
            System.IO.Directory.CreateDirectory(LoggingRootDir);
            System.IO.Directory.CreateDirectory(ExceptionsRootDir);
            System.IO.Directory.CreateDirectory(SettingsHistoryRootDir);
            System.IO.Directory.CreateDirectory(SnapshotsRootDir);
            System.IO.Directory.CreateDirectory(CameraConfigHistoryRootDir);




            Log.Startup(LoggingRootDir);
            Logger.WriteBorder("PalletCheck MCS STARTUP");
            Logger.WriteLine("ExeRootDir:                    " + ExeRootDir);
            Logger.WriteLine("ConfigRootDir:                 " + ConfigRootDir);
            Logger.WriteLine("HistoryRootDir:                " + HistoryRootDir);
            Logger.WriteLine("RecordingRootDir:              " + RecordingRootDir);
            Logger.WriteLine("SegmentationErrorRootDir:      " + SegmentationErrorRootDir);
            Logger.WriteLine("LoggingRootDir:                " + LoggingRootDir);
            Logger.WriteLine("ExceptionsRootDir:             " + ExceptionsRootDir);
            Logger.WriteLine("SettingsHistoryRootDir:        " + SettingsHistoryRootDir);
            Logger.WriteLine("CameraConfigHistoryRootDir:    " + CameraConfigHistoryRootDir);
            Logger.WriteLine("SnapshotsRootDir:              " + SnapshotsRootDir);

            SiteName = Environment.GetEnvironmentVariable("PALLETCHECK_MCS_SITE_NAME");
            if (SiteName == null)
                SiteName = "";

            Logger.WriteLine("");
            StorageWatchdog.AddDoNotDeleteDirs(ConfigRootDir);
            StorageWatchdog.AddDoNotDeleteDirs(HistoryRootDir);
            StorageWatchdog.AddDoNotDeleteDirs(RecordingRootDir);
            StorageWatchdog.AddDoNotDeleteDirs(SegmentationErrorRootDir);
            StorageWatchdog.AddDoNotDeleteDirs(LoggingRootDir);
            StorageWatchdog.AddDoNotDeleteDirs(ExceptionsRootDir);
            StorageWatchdog.AddDoNotDeleteDirs(SettingsHistoryRootDir);
            StorageWatchdog.AddDoNotDeleteDirs(SnapshotsRootDir);
            StorageWatchdog.AddDoNotDeleteDirs(CameraConfigHistoryRootDir);

            InitializeComponent();

            //MainWindow.Icon = BitmapFrame.Create(Application.GetResourceStream(new Uri("LiveJewel.png", UriKind.RelativeOrAbsolute)).Stream);
            StartZMQListener();
        }

        private void StartZMQListener()
        {
            _isListening = true;
            _listenerTask = Task.Run(() =>
            {
                using (var server = new ResponseSocket("@tcp://*:5555"))
                {
                    while (_isListening)
                    {
                        var message = server.ReceiveFrameString();
                        Console.WriteLine("Received: " + message);
                        var mcsMessage = JsonConvert.DeserializeObject<MCSMessage>(message);
                        Dispatcher.Invoke(() =>
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
                case MCSCommand.Connect:
                    message.Parameter1 = btnStart.Content.ToString();
                    break;
                case MCSCommand.Disconnect:
                    break;
                case MCSCommand.Start:
                    onStart();
                    break;
                case MCSCommand.Stop:
                    onStop();
                    break;
                case MCSCommand.GetLatestCaptureId:
                    if (tempFrame != null)
                    {
                        message.Parameter1 = tempFrame.FrameID.ToString();
                    }
                    break;
                case MCSCommand.GetCaptureImages:
                    if (tempFrame != null)
                    {
                        message.Parameter1 = BitmapSourceConverter.ConvertBitmapSourceToString(tempFrame.RangeCB.BuildWPFBitmap());
                        message.Parameter2 = BitmapSourceConverter.ConvertBitmapSourceToString(tempFrame.ReflectanceCB.BuildWPFBitmap());
                    }
                    break;
                case MCSCommand.GetStatus:
                    message.Parameter1 = btnStart.Content.ToString();
                    break;
                case MCSCommand.GetCameraCount:
                    Dictionary<string, string> camera = ParamStorage.Categories["Camera"];
                    message.Parameter1 = (camera.Count - 1).ToString();
                    break;
                case MCSCommand.GetParamSettings:
                    message.Parameter1 = JsonConvert.SerializeObject(ParamStorage.Categories);
                    break;
                case MCSCommand.UpdateSettings:
                    ParamStorage.Categories = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(message.Parameter1);
                    break;
                case MCSCommand.AddCamera:
                    ParamStorage.AddCamera();
                    break;
                case MCSCommand.RemoveCamera:
                    ParamStorage.RemoveCamera();
                    break;
                case MCSCommand.GetRecordingRootDIR:
                    message.Parameter1 = RecordingRootDir;
                    break;
                case MCSCommand.SetRecordingRootDIR:
                    RecordingRootDir = message.Parameter1;
                    File.WriteAllText(HistoryRootDir + "\\LastUsedRecordingDIRFile.txt", RecordingRootDir);
                    break;
                case MCSCommand.GetStatusSettings:
                    message.Parameter1 = JsonConvert.SerializeObject(StatusStorage.Categories);
                    break;
                case MCSCommand.HeartBeat:
                    message.Parameter1 = "OK";
                    break;
                case MCSCommand.SaveParamSettings:
                    ParamStorage.Save(MainWindow.LastUsedParamFile);
                    break;
            }
            var jsonMessage = JsonConvert.SerializeObject(message);
            server.SendFrame(jsonMessage);
        }


        private static void CurrentDomainOnUnhandledException(UnhandledExceptionEventArgs args)
        {
            Exception exception = args.ExceptionObject as Exception;

            Logger.WriteBorder("CurrentDomainOnUnhandledException");
            Logger.WriteException(exception);

            string errorMessage = string.Format("An application error occurred.\nPlease check whether your data is correct and repeat the action. If this error occurs again there seems to be a more serious malfunction in the application, and you better close it.\n\nError: {0}\n\nDo you want to continue?\n(if you click Yes you will continue with your work, if you click No the application will close)",

            exception.Message + (exception.InnerException != null ? "\n" + exception.InnerException.Message : null));

            if (MessageBox.Show(errorMessage, "Application Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error) == MessageBoxResult.No)
            {
                if (MessageBox.Show("WARNING: The application will close. Any changes will not be saved!\nDo you really want to close it?", "Close the application!", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            this.Title = "MCS  " + Version + " - " + SiteName;
            StatusStorage.Set("Version Number", Version);

            ButtonBackgroundBrush = btnStart.Background;


            //if (Keyboard.IsKeyDown(Key.LeftCtrl))
            //{
            //    string s = "";
            //    s += "RootDir:\n   " + RootDir + "\n\n";
            //    s += "ConfigRootDir:\n   " + ConfigRootDir + "\n\n";
            //    s += "HistoryRootDir:\n   " + HistoryRootDir + "\n\n";
            //    s += "RecordingRootDir:\n   " + RecordingRootDir + "\n\n";
            //    s += "LoggingRootDir:\n   " + LoggingRootDir + "\n\n";
            //    s += "ExeRootDir:\n   " + ExeRootDir;
            //    MessageBox.Show(s, "ROOT DIRECTORIES");
            //}



            // Try to load starting parameters
            if (File.Exists(HistoryRootDir + "\\LastUsedParamFile.txt"))
            {
                _LastUsedParamFile = File.ReadAllText(HistoryRootDir + "\\LastUsedParamFile.txt");
                if (File.Exists(_LastUsedParamFile))
                {
                    ParamStorage.Load(_LastUsedParamFile);
                }
                else
                    MessageBox.Show("Can't open the last known config file. It was supposed to be at " + _LastUsedParamFile);
            }
            else
            {
                ParamStorage.Load(ConfigRootDir + "\\DefaultParams.txt");
            }

            //int PPIX = ParamStorage.GetInt("Pixels Per Inch X");


            // Open PLC connection port
            int Port = ParamStorage.GetInt("TCP Server Port");
            string IP = ParamStorage.GetString("TCP Server IP");
            PLC = new PLCComms(IP, Port);
            PLC.Start();

            Storage = new StorageWatchdog();
            Storage.Start();

            System.Windows.Forms.Timer T = new System.Windows.Forms.Timer();
            T.Interval = 500;
            T.Tick += TimerUpdate500ms;
            T.Start();

            imgPassSymbol.Visibility = Visibility.Hidden;
            imgPassText.Visibility = Visibility.Hidden;
            imgFailSymbol.Visibility = Visibility.Hidden;
            imgFailText.Visibility = Visibility.Hidden;

            System.Windows.Forms.Timer T2 = new System.Windows.Forms.Timer();
            T2.Interval = 5000;
            T2.Tick += Timer5Sec;
            T2.Start();


            ClearAnalysisResults();
            ProgressBar_Clear();

            PProcessor = new PalletProcessor(3, 3);


            ParamStorage.HasChangedSinceLastSave = false;
            SaveSettingsButtonBrush = btnSettingsControl.Background;
            System.Windows.Forms.Timer T3 = new System.Windows.Forms.Timer();
            T3.Interval = 500;
            T3.Tick += SaveSettingsColorUpdate;
            T3.Start();

            if (Camera == null)
            {
                string CameraIP = ParamStorage.GetString("Camera1 IP");
                Camera = new MCSCamera();
                Camera.Startup("Camera1", CameraIP, OnNewFrameReceived, OnConnectionStateChange, OnCaptureStateChange);
                System.Threading.Thread.Sleep(1000);
            }

            ParamStorage.SaveInHistory("STARTUP");
            Camera.SaveInHistory("STARTUP");


            if (ParamStorage.GetInt("Auto Start Capturing") == 1)
            {
                System.Threading.Thread.Sleep(2000);
                btnStart_Click(null, null);
            }
        }








        private void OnConnectionStateChange(MCSCamera Cam, RulerCamera.ConnectionState NewState)
        {
            // !!! THIS IS CALLED FROM THE MCSCamera THREAD !!!!
            Logger.WriteLine(Cam.CameraName + "  ConnectionState: " + NewState.ToString());
            var rulers = Cam.getRulerCamers();
            foreach (RulerCamera Ruler in rulers)
            {
                StatusStorage.Set("Camera." + Ruler.CameraName + ".ConnState", Ruler.CameraConnectionState.ToString());
            }
        }

        private void OnCaptureStateChange(MCSCamera Cam, MCSCamera.CaptureState NewState)
        {
            // !!! THIS IS CALLED FROM THE MCSCamera THREAD !!!!
            Logger.WriteLine(Cam.CameraName + "  CaptureState: " + NewState.ToString());
            var rulers = Cam.getRulerCamers();
            foreach (RulerCamera Ruler in rulers)
            {
                StatusStorage.Set("Camera." + Ruler.CameraName + ".CaptureState", Ruler.CameraCaptureState.ToString());
            }
        }


        private void OnNewFrameReceived(MCSCamera Cam, MCSFrame Frame)
        {
            tempFrame = Frame;
            Logger.WriteBorder("NEW SCAN RECEIVED    Frame: " + Frame.FrameID.ToString());
            Logger.WriteLine("MainWindow::OnNewFrameReceived ()");
            // !!! THIS IS CALLED FROM THE MCSCamera THREAD !!!!
            //string FrameStr = Frame.ToString();
            //Logger.WriteLine(FrameStr);
            //StatusStorage.Set("Camera." + Cam.CameraName + ".Frame", FrameStr);
            //IncomingBuffer = (UInt16[])ProfileData.Clone();



            //ClearAnalysisResults();

            //CaptureBuffer NewBuf = new CaptureBuffer(Frame.Range,Frame.Width,Frame.Height);
            //CaptureBuffer ReflBuf = new CaptureBuffer(Frame.Reflectance, Frame.Width, Frame.Height);
            //ReflBuf.PaletteType = CaptureBuffer.PaletteTypes.Gray;

            Pallet P = new Pallet(Frame);
            //P.NotLive = true;
            PProcessor.ProcessPalletHighPriority(P, Pallet_OnLivePalletAnalysisComplete);


            //Dispatcher.Invoke(new addBufferCB(MainWindow.AddCaptureBufferBrowser), new object[] { "Live", IncomingBuffer });
            // Console.WriteLine("BUFFER RECEIVED! " + DateTime.Now.ToString());
            Logger.WriteLine("MainWindow::OnNewFrameReceived completed");
            /*if (tempFrame != null)
            {
                mcsAPI.sendCaptureId(tempFrame.FrameID.ToString());
            }*/
        }
        private void SaveSettingsColorUpdate(object sender, EventArgs e)
        {
            if (ParamStorage.HasChangedSinceLastSave)
            {
                if (btnSettingsControl.Background == SaveSettingsButtonBrush)
                    btnSettingsControl.Background = new SolidColorBrush(Color.FromArgb(255, 196, 196, 0));
                else
                    btnSettingsControl.Background = SaveSettingsButtonBrush;
            }
            else
            {
                btnSettingsControl.Background = SaveSettingsButtonBrush;
            }
        }

        //private void UpdatePalletProcessQueue(object sender, EventArgs e)
        //{
        //    int MaxThreads = 2;

        //    if(PalletProcessInputQueue.Count > 0)
        //    {
        //        // Can we kick off another thread
        //        if(PalletInProgressList.Count < MaxThreads)
        //        {
        //            Pallet P = PalletProcessInputQueue[0];
        //            PalletProcessInputQueue.RemoveAt(0);
        //            PalletInProgressList.Add(P);
        //            P.DoAsynchronousAnalysis();
        //        }
        //    }

        //}

        private void Timer5Sec(object sender, EventArgs e)
        {
            Logger.WriteHeartbeat();

            if (ParamStorage.GetInt("TCP Server Heartbeat") == 1)
            {
                DateTime D = DateTime.Now;
                string Text = D.ToShortDateString() + " " + D.ToLongTimeString();
                PLC.SendMessage(Text + " --- TCP Server Heartbeat\n");
            }

            // Keep a handle on garbage collection so we keep a steady footprint
            GC.Collect();
            //GC.WaitForPendingFinalizers();

            //Logger.WriteLine(String.Format("Current Memory Usage: {0:N0}", GC.GetTotalMemory(false)));
        }

        private void TimerUpdate500ms(object sender, EventArgs e)
        {
            DateTime D = DateTime.Now;
            string Text = D.ToShortDateString() + " " + D.ToLongTimeString();
            CurDateTime.Text = Text;
            StatusStorage.Set("Date/Time", Text);
            StatusStorage.Set("SettingsChanged", ParamStorage.HasChangedSinceLastSave.ToString());


            if (Camera.CameraConnectionState != RulerCamera.ConnectionState.Connected)
            {
                if (btnStatusControl.Background == SaveSettingsButtonBrush)
                    btnStatusControl.Background = new SolidColorBrush(Color.FromArgb(255, 245, 245, 39));
                else
                    btnStatusControl.Background = SaveSettingsButtonBrush;
            }
            else
            {
                btnStatusControl.Background = SaveSettingsButtonBrush;
            }
        }

        public void ProgressBar_SetText(string text)
        {
            ProgressBarText.Text = text;
        }

        public void ProgressBar_SetProgress(double Current, double Total)
        {
            ProgressBar.Value = Math.Round(100 * (Current / Total), 0);
        }
        public void ProgressBar_Clear()
        {
            ProgressBar.Value = 0;
            ProgressBarText.Text = "";
        }


        //=====================================================================

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            Logger.ButtonPressed(btnStart.Content.ToString());
            onStartStop();
        }

        private void onStartStop()
        {
            ParamStorage.SaveInHistory("LINESTART");
            Camera.SaveInHistory("LINESTART");
            if (btnStart.Content.ToString() == "START")
            {
                onStart();
            }
            else if (btnStart.Content.ToString() == "STOP")
            {
                onStop();
            }
        }

        private void onStart()
        {
            Camera.StartCapturingFrames();
            btnStart.Content = "STOP";
            ModeStatus.Text = "RUNNING";
            btnStart.Background = Brushes.Red;
        }

        private void onStop()
        {
            Camera.StopCapturingFrames();
            btnStart.Content = "START";
            ModeStatus.Text = "IDLING";
            btnStart.Background = ButtonBackgroundBrush;
        }


        //=====================================================================
        public static CaptureBufferBrowser AddCaptureBufferBrowser(string Name, CaptureBuffer CB, string SelectName)
        {
            //bool isLive = (string.Compare(Name, "Live") == 0);
            CB.Name = Name;

            CaptureBufferBrowser CBB = new CaptureBufferBrowser();
            CBB.SetCB(CB);

            Button B = new Button();
            B.Content = Name;
            B.Click += Singleton.ImageTab_Click;
            B.Margin = new Thickness(5);
            B.Background = Singleton.btnProcessPallet.Background;
            B.Foreground = Singleton.btnProcessPallet.Foreground;
            B.FontSize = 16;
            B.Tag = CBB;

            Singleton.CBB_Button_List.Children.Add(B);
            Singleton.CBB_Container.Children.Add(CBB);

            CBB.Refresh();
            CBB.Visibility = Visibility.Hidden;

            if ((Name == SelectName))
                Singleton.ImageTab_Click(B, new RoutedEventArgs());

            return CBB;
        }

        //=====================================================================

        void ClearAnalysisResults()
        {
            imgPassSymbol.Visibility = Visibility.Hidden;
            imgPassText.Visibility = Visibility.Hidden;
            imgFailSymbol.Visibility = Visibility.Hidden;
            imgFailText.Visibility = Visibility.Hidden;

            foreach (CaptureBufferBrowser CBC in CBB_Container.Children)
            {
                CBC.Clear();
            }
            CBB_Container.Children.Clear();
            CBB_Button_List.Children.Clear();
            PalletName.Text = "";

            defectTable.Items.Clear();
        }

        //=====================================================================
        private void LoadAndProcessCaptureFile(string FileName, bool RebuildList = false)
        {
            Logger.WriteLine("Loading CaptureBuffer from: " + FileName);

            ClearAnalysisResults();

            if (RebuildList)
            {
                string Search = System.IO.Path.GetDirectoryName(FileName);

                DirectoryInfo info = new DirectoryInfo(Search);
                FileInfo[] files = info.GetFiles("*rng.r3").OrderBy(p => p.CreationTime).ToArray();

                List<string> AF = new List<string>();
                foreach (FileInfo F in files)
                    AF.Add(F.FullName);
                AllFilesInFolder = AF.ToArray();
                LastFileIdx = AllFilesInFolder.ToList().IndexOf(FileName);
            }


            Pallet P = new Pallet(FileName);
            //P.NotLive = true;

            PProcessor.ProcessPalletHighPriority(P, Pallet_OnProcessSinglePalletAnalysisComplete);
            //PProcessor.ProcessPalletHighPriority(P, Pallet_OnLivePalletAnalysisComplete); // HACK
        }


        //=====================================================================
        private void UpdateProductivityStats(Pallet P)
        {
            Stats.OnNewPallet(P);
            statisticsTable.Items.Clear();

            // Send defects to the defectTable
            for (int i = 0; i < Stats.Entries.Count; i++)
            {
                statisticsTable.Items.Add(Stats.Entries[i]);
            }
        }


        //=====================================================================
        private void ImageTab_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button B = (Button)sender;
                CaptureBufferBrowser CBB = (CaptureBufferBrowser)B.Tag;

                Logger.ButtonPressed("CaptureBuffer." + B.Content.ToString());

                if (CBB.FB == null)
                    CBB.FB = CBB.CB.BuildFastBitmap();

                for (int i = 0; i < CBB_Container.Children.Count; i++)
                    CBB_Container.Children[i].Visibility = Visibility.Hidden;

                CBB.Visibility = Visibility.Visible;
            }
            catch (Exception exp)
            {
                Logger.WriteException(exp);
            }
        }

        //=====================================================================
        private void btnProcessPallet_Click(object sender, RoutedEventArgs e)
        {
            Logger.ButtonPressed(btnProcessPallet.Content.ToString());

            OpenFileDialog OFD = new OpenFileDialog();

            OFD.Filter = "R3 Files|*_0_rng.r3";
            OFD.InitialDirectory = MainWindow.RecordingRootDir;
            if (MainWindow.ProcessPalletLastDir != "") {
                OFD.InitialDirectory = MainWindow.ProcessPalletLastDir;
            }
            try
            {
                if (OFD.ShowDialog() == true)
                {
                    MainWindow.ProcessPalletLastDir = System.IO.Path.GetDirectoryName(OFD.FileName);
                    LoadAndProcessCaptureFile(OFD.FileName, true);
                }
                else
                {
                    Logger.ButtonPressed("btnProcessPallet_Click open file dialog cancelled");
                }
            } catch (Exception exp) { }

        }

        //=====================================================================
        //private void SendPLCResults(Pallet P)
        //{
        //    segmentation_error = P.BList.Count == 5 ? false : true;

        //    string PLCString = "";

        //    DateTime DT = P.CreateTime;
        //    PLCString += string.Format("{0:00}:{1:00}:{2:0000} {3:00}:{4:00}:{5:00} ",
        //                                DT.Month, DT.Day, DT.Year,
        //                                DT.Hour, DT.Minute, DT.Second);

        //    PLCString += BypassModeEnabled ? "B" : "N";
        //    PLCString += (RecordModeEnabled ? "A" : "N");
        //    PLCString += cam_error ? "T" : "F";
        //    PLCString += segmentation_error ? "T" : "F";
        //    PLCString += file_storage_error ? "T" : "F";
        //    PLCString += unknown_error ? "T" : "F";

        //    PLC.SendMessage(PLCString);

        //    string[] defectCodes = PalletDefect.GetPLCCodes();
        //    for (int i = 0; i < P.BList.Count; i++)
        //    {
        //        string boardDefList = "";

        //        for (int j = 0; j < defectCodes.Length; j++)
        //        {
        //            string DefectCode = defectCodes[j];
        //            int nDefs = 0;

        //            for (int k = 0; k < P.BList[i].AllDefects.Count; k++)
        //                if (P.BList[i].AllDefects[k].Code == DefectCode)
        //                    nDefs += 1;

        //            if (nDefs < 9)
        //                DefectCode += nDefs.ToString();
        //            else
        //                DefectCode += "9";

        //            boardDefList += DefectCode;
        //        }

        //        PLC.SendMessage(P.BList[i].BoardName + boardDefList);
        //    }
        //}

        private void SendPLCResults(Pallet P)
        {
            segmentation_error = P.BList.Count == 5 ? false : true;

            string PLCString = "";

            DateTime DT = P.CreateTime;
            PLCString += string.Format("{0:00}-{1:00}-{2:0000} {3:00}:{4:00}:{5:00} ",
                                        DT.Month, DT.Day, DT.Year,
                                        DT.Hour, DT.Minute, DT.Second);

            PLCString += BypassModeEnabled ? "B" : "N";
            PLCString += (RecordModeEnabled ? "A" : "N");
            PLCString += cam_error ? "T" : "F";
            PLCString += segmentation_error ? "T" : "F";
            PLCString += file_storage_error ? "T" : "F";
            PLCString += unknown_error ? "T" : "F";
            PLCString += " ";

            //PLC.SendMessage(PLCString);

            string[] defectCodes = PalletDefect.GetPLCCodes();
            for (int i = 0; i < P.BList.Count; i++)
            {
                PLCString += P.BList[i].BoardName;


                // Get count of all board defects
                if (P.BList[i].AllDefects.Count == 0)
                    PLCString += "ND1";
                else
                    PLCString += "ND0";

                for (int j = 0; j < defectCodes.Length; j++)
                {
                    string DefectCode = defectCodes[j];
                    int nDefs = 0;

                    for (int k = 0; k < P.BList[i].AllDefects.Count; k++)
                        if (P.BList[i].AllDefects[k].Code == DefectCode)
                            nDefs += 1;

                    if (nDefs < 9)
                        DefectCode += nDefs.ToString();
                    else
                        DefectCode += "9";

                    PLCString += DefectCode;
                }

                PLCString += " ";
            }

            PLCString += "\r\n";
            PLC.SendMessage(PLCString);


            Logger.WriteLine("SendPLCResults for " + P.Filename + " completed.");
        }
        //=====================================================================

        private void Pallet_OnProcessSinglePalletAnalysisComplete(Pallet P)
        {
            Logger.WriteLine("Pallet_OnProcessSinglePalletAnalysisComplete");

            //SendPLCResults(P); // HACK HACK HACK

            UpdateUIWithPalletResults(P);
            //Testing code for image & id expose
            tempFrame = P.MCSFrame;
        }

        private void Pallet_OnProcessMultiplePalletAnalysisComplete(Pallet P)
        {
            Logger.WriteLine("Pallet_OnProcessMultiplePalletAnalysisComplete");

            if (P.State == Pallet.InspectionState.Unprocessed)
            {
                Logger.WriteLine("InspectionState.Unprocessed -- " + P.Filename);
            }

            ProcessRecordingFinishedPalletCount += 1;
            ProgressBar_SetText(string.Format("Processing Recording... Finished {0} of {1}", ProcessRecordingFinishedPalletCount, ProcessRecordingTotalPalletCount));
            ProgressBar_SetProgress(ProcessRecordingFinishedPalletCount, ProcessRecordingTotalPalletCount);

            IR.AddPallet(P);

            if (ProcessRecordingFinishedPalletCount >= ProcessRecordingTotalPalletCount)
            {
                ProgressBar_SetText("Processing Recording... COMPLETE");
                IR.Save();
            }
        }


        //=====================================================================

        private void UpdateUIWithPalletResults(Pallet P)
        {
            ClearAnalysisResults();

            string fullDir = System.IO.Path.GetDirectoryName(P.Filename);
            string lastDir = fullDir.Split(System.IO.Path.DirectorySeparatorChar).Last();
            Logger.WriteLine("pallet filename: " + P.Filename);
            //Logger.WriteLine("fullDir: "+ fullDir);
            //Logger.WriteLine("lastDir: "+ lastDir);
            PalletName.Text = lastDir + "\\" + System.IO.Path.GetFileName(P.Filename);


            // Send defects to the defectTable
            int c = 0;
            if (P.BList != null)
            {
                for (int i = 0; i < P.BList.Count; i++)
                {
                    List<PalletDefect> BoardDefects = P.BList[i].AllDefects;
                    for (int j = 0; j < BoardDefects.Count; j++)
                    {
                        PalletDefect PD = BoardDefects[j];
                        defectTable.Items.Add(PD);
                        c += 1;
                        Logger.WriteLine(string.Format("DEFECT | {0:>2} | {1:>2}  {2:>4}  {3:>8}  {4}", c, P.BList[i].BoardName, PD.Code, PD.Name, PD.Comment));
                    }
                }
            }

            List<PalletDefect> FullPalletDefects = P.PalletLevelDefects;
            for (int j = 0; j < FullPalletDefects.Count; j++)
            {
                PalletDefect PD = FullPalletDefects[j];
                defectTable.Items.Add(PD);
                c += 1;
                Logger.WriteLine(string.Format("DEFECT | {0:>2} | {1:>2}  {2:>4}  {3:>8}  {4}", c, "Pallet", PD.Code, PD.Name, PD.Comment));
            }

            Logger.WriteLine(string.Format("DEFECT COUNT {0}", c));

            // Show Pass/Fail Icons
            if (P.AllDefects.Count > 0)
            {
                imgPassSymbol.Visibility = Visibility.Hidden;
                imgPassText.Visibility = Visibility.Hidden;
                imgFailSymbol.Visibility = Visibility.Visible;
                imgFailText.Visibility = Visibility.Visible;
            }
            else
            {
                imgPassSymbol.Visibility = Visibility.Visible;
                imgPassText.Visibility = Visibility.Visible;
                imgFailSymbol.Visibility = Visibility.Hidden;
                imgFailText.Visibility = Visibility.Hidden;
            }


            // Build CaptureBufferBrowsers
            if (true)
            {
                // Check if segmentation error
                bool HasSegmentationError = false;
                if ((P.PalletLevelDefects.Count > 0) && (P.PalletLevelDefects[0].Type == PalletDefect.DefectType.board_segmentation_error))
                {
                    HasSegmentationError = true;
                }


                foreach (CaptureBuffer CB in P.CBList)
                {
                    CaptureBufferBrowser CBB = AddCaptureBufferBrowser(CB.Name, CB, HasSegmentationError ? "Original" : "Filtered");

                    foreach (PalletDefect PD in P.AllDefects)
                    {
                        if (PD.MarkerRadius > 0)
                        {
                            CBB.MarkDefect(new Point(PD.MarkerX1, PD.MarkerY1), PD.MarkerRadius, PD.MarkerTag);
                        }
                        else
                        if ((PD.MarkerRadius == 0) && (PD.MarkerX1 != 0))
                        {
                            CBB.MarkDefect(new Point(PD.MarkerX1, PD.MarkerY1), new Point(PD.MarkerX2, PD.MarkerY2), PD.MarkerTag);
                        }
                    }

                    //CBB.RedrawDefects();
                    //CBB.MarkDefect()
                }
            }

        }

        private void Pallet_OnLivePalletAnalysisComplete(Pallet P)
        {
            Logger.WriteLine("Pallet_OnLivePalletAnalysisComplete Start");
            if (!BypassModeEnabled)
            {
                // Update production stats
                UpdateProductivityStats(P);
            }

            // Notify PLC of pallet results
            SendPLCResults(P);

            Logger.WriteBorder(string.Format("Pallet Reporting Complete for {0} {1}", P.MCSFrame.FrameID, P.Filename));

            UpdateUIWithPalletResults(P);

            // Save Results to Disk
            {
                try
                {
                    string FullDirectory = RecordingRootDir + "\\" + P.Directory;

                    if (RecordModeEnabled && (RecordDir != ""))
                        FullDirectory = RecordingRootDir + "\\" + RecordDir;

                    System.IO.Directory.CreateDirectory(FullDirectory);
                    string FullFilename = FullDirectory + "\\" + P.Filename;
                    Logger.WriteLine("Saving Pallet to " + FullFilename);

                    if (P.AllDefects.Count > 0) FullFilename = FullFilename.Replace(".r3", "_F.r3");
                    else FullFilename = FullFilename.Replace(".r3", "_P.r3");


                    // Save to disk using background threads
                    P.MCSFrame.Save(FullFilename, true);

                    // Save inspection report
                    //string InspectionReportFilename = RecordingRootDir + "\\ir.csv"; // HACK
                    string InspectionReportFilename = FullFilename.Replace(".r3", ".csv");
                    InspectionReport PalletIR = new InspectionReport(InspectionReportFilename);
                    PalletIR.AddPallet(P);
                    PalletIR.Save();


                    //bool HasSegmentationError = false;
                    //foreach (PalletDefect PD in P.AllDefects)
                    //    if (PD.Code == "ER")
                    //        HasSegmentationError = true;

                    //if (HasSegmentationError)
                    //{
                    //    // TODO: HACK: Turn back on saving to segmentation error directory
                    //    //FullFilename = SegmentationErrorRootDir + "\\" + P.Filename;
                    //    //Logger.WriteLine("Saving Pallet to " + FullFilename);
                    //    //P.MCSFrame.Save(FullFilename);
                    //}
                }
                catch (Exception e)
                {
                    Logger.WriteException(e);
                }
            }

            Logger.WriteLine("Pallet_OnLivePalletAnalysisComplete Complete");
            Logger.WriteBorder("PALLET COMPLETE: " + P.Filename);

        }


        //=====================================================================
        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            Logger.ButtonPressed(btnRecord.Content.ToString());

            if (RecordModeEnabled)
            {
                RecordModeEnabled = false;
                btnRecord.Background = ButtonBackgroundBrush;

            }
            else
            {
                RecordModeEnabled = true;
                btnRecord.Background = Brushes.Green;

                DateTime DT = DateTime.Now;
                RecordDir = String.Format("RECORDING_{0:0000}{1:00}{2:00}_{3:00}{4:00}{5:00}", DT.Year, DT.Month, DT.Day, DT.Hour, DT.Minute, DT.Second);
            }
        }

        private void btnBypassClick(object sender, RoutedEventArgs e)
        {
            Logger.ButtonPressed(btnBypass.Content.ToString());


            BypassModeEnabled = !BypassModeEnabled;

            if (BypassModeEnabled) btnBypass.Background = Brushes.Green;
            if (!BypassModeEnabled) btnBypass.Background = ButtonBackgroundBrush;
        }

        private void btnDefects_Click(object sender, RoutedEventArgs e)
        {
            Logger.ButtonPressed(btnDefects.Content.ToString());
        }

        private void btnStatistics_Click(object sender, RoutedEventArgs e)
        {
            Logger.ButtonPressed(btnStatistics.Content.ToString());
            if (MessageBox.Show("Do you want to clear current statistics?", "Clear Current Statistics?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                statisticsTable.Items.Clear();
                Stats.Clear();
            }
        }

        //=====================================================================

        private void btnProcessRecording_Click(object sender, RoutedEventArgs e)
        {
            Logger.ButtonPressed(btnProcessRecording.Content.ToString());

            //if (ProcessRecordingWindow != null)
            //    return;

            IR = new InspectionReport(RootDir + "\\InspectionReport.csv");

            System.Windows.Forms.FolderBrowserDialog OFD = new System.Windows.Forms.FolderBrowserDialog();
            OFD.RootFolder = Environment.SpecialFolder.MyComputer;
            OFD.SelectedPath = RecordingRootDir;
            OFD.ShowNewFolderButton = false;
            if (OFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (System.IO.Directory.Exists(OFD.SelectedPath))
                {
                    DirectoryInfo info = new DirectoryInfo(OFD.SelectedPath);
                    FileInfo[] files = info.GetFiles("*_0_rng.r3", SearchOption.AllDirectories).OrderBy(p => p.LastWriteTime).ToArray();

                    List<string> ProcessFileList = new List<string>();
                    ProcessFileList.Clear();
                    foreach (FileInfo F in files)
                        ProcessFileList.Add(F.FullName);

                    if (ProcessFileList.Count == 0)
                    {
                        MessageBox.Show("No pallets were found in this folder.");
                        return;
                    }

                    System.Windows.Forms.Timer T = new System.Windows.Forms.Timer();

                    Logger.WriteLine("ProcessRecordingWindow_ProcessPalletGroup START");
                    //StartProcessingRecordedFile(ProcessFileList);

                    ProgressBar_Clear();
                    ProgressBar_SetText("Processing Recording");
                    ProcessRecordingTotalPalletCount = ProcessFileList.Count;
                    ProcessRecordingFinishedPalletCount = 0;

                    for (int iFile = 0; iFile < ProcessFileList.Count; iFile++)
                    {
                        string Filename = ProcessFileList[iFile];

                        string ImageFilename = Filename.Replace(".r3", ".png");

                        if (true)//(!File.Exists(ImageFilename))
                        {
                            Pallet P = new Pallet(Filename);
                            PProcessor.ProcessPalletLowPriority(P, Pallet_OnProcessMultiplePalletAnalysisComplete);
                        }
                    }

                    ProcessFileList.Clear();

                    Logger.WriteLine("ProcessRecordingWindow_ProcessPalletGroup STOP");
                }
            }
            else
            {
                Logger.WriteLine("btnProcessRecording_Click cancelled directory selection");
            }
        }

        //=====================================================================
        private void btnSettingsControl_Click(object sender, RoutedEventArgs e)
        {
            Logger.ButtonPressed(btnSettingsControl.Content.ToString());

            if (Password.DlgOpen)
                return;

            Password PB = new Password();
            PB.Closed += (sender2, e2) =>
            {
                if (Password.Passed)
                {
                    ParamConfig PC = new ParamConfig();
                    PC.Show();
                }
            };
            PB.Show();

        }

        //=====================================================================
        private void btnAPISettings_Click(object sender, RoutedEventArgs e)
        {
            Logger.ButtonPressed(btnAPISettings.Content.ToString());
            APIWindow AW = new APIWindow();
            AW.Show();
        }

        //=====================================================================
        private void btnStatusControl_Click(object sender, RoutedEventArgs e)
        {
            Logger.ButtonPressed(btnStatusControl.Content.ToString());
            StatusWindow SW = new StatusWindow();
            SW.Show();
        }

        //=====================================================================
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if ( (AllFilesInFolder != null) && (AllFilesInFolder.Length > 0) )
            {

                if ( e.Key == Key.Left )
                {
                    if ( LastFileIdx < AllFilesInFolder.Length-1 )
                    {
                        LastFileIdx++;
                        LoadAndProcessCaptureFile(AllFilesInFolder[LastFileIdx]);
                    }
                }
                if (e.Key == Key.Right)
                {
                    if (LastFileIdx > 0)
                    {
                        LastFileIdx--;
                        LoadAndProcessCaptureFile(AllFilesInFolder[LastFileIdx]);
                    }
                }
            }
        }

        //=====================================================================

        private void Window_Closed(object sender, EventArgs e)
        {
            //Logger.WriteBorder("PalletCheck SHUTDOWN");
            //Log.Shutdown();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Pallet.Shutdown = true;
                PLC.Stop();
                Storage.Stop();
                Camera.Shutdown();
                Logger.WriteBorder("PalletCheck SHUTDOWN");
                Log.Shutdown();
            }
            catch(Exception exp)
            {
                Logger.WriteException(exp);
            }
        }

    }
}
