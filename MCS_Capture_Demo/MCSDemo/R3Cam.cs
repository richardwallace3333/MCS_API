using Sick.GenIStream;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.IO;
//using System.Threading.Tasks;


//public void ExportParameters(string filePath)
//{
//    genistreamPINVOKE.ICamera_ExportParameters(swigCPtr, filePath);
//    if (genistreamPINVOKE.SWIGPendingException.Pending)
//    {
//        throw genistreamPINVOKE.SWIGPendingException.Retrieve();
//    }
//}

//public ConfigurationResult ImportParameters(string filePath)
//{
//    ConfigurationResult result = new ConfigurationResult(genistreamPINVOKE.ICamera_ImportParameters(swigCPtr, filePath), cMemoryOwn: true);
//    if (genistreamPINVOKE.SWIGPendingException.Pending)
//    {
//        throw genistreamPINVOKE.SWIGPendingException.Retrieve();
//    }

//    return result;
//}


namespace PalletCheck
{
    public class R3Cam
    {
        public enum ConnectionState
        {
            Shutdown,
            Searching,
            Connected,
            BadParameters,
            Disconnected,
        };

        public enum CaptureState
        {
            Stopped,
            Capturing,
        };

        public class Frame
        {
            public DateTime Time;
            public int Width;
            public int Height;
            public ulong FrameID;

            public UInt16[] Range;
            public byte[] Reflectance;
            //public byte[] Scatter;

            public R3Cam Camera;

            public override string ToString()
            {
                string s = "";
                s = string.Format("R3Cam.Frame:  cam:{0}  w:{1}  h:{2}   range:{3}  reflectance:{4}  scatter:{5}", Camera.CameraName, Width, Height,
                    (Range != null), (Reflectance != null), false);
                return s;
            }
        }

        public ICamera Camera { get; private set; }
        public string CameraName { get; private set; }
        public string IPAddressStr { get; private set; }
        public ConnectionState CameraConnectionState { get; private set; }
        public CaptureState CameraCaptureState { get; private set; }

        public Frame LastFrame { get; private set; }


        private CameraDiscovery discovery;
        private FrameGrabber frameGrabber;

        private Thread CamThread;
        private bool StopCamThread = false;

        private bool DoCaptureFrames;

        private bool InSimMode;
        private FileInfo[] SimModeFileList;
        private int SimModeFileIndex;
        private DateTime SimModeNextDT;

        //===============================================================================

        public delegate void NewFrameReceivedCB(R3Cam Cam, R3Cam.Frame Frame);
        public delegate void ConnectionStateChangeCB(R3Cam Cam, ConnectionState NewState);
        public delegate void CaptureStateChangeCB(R3Cam Cam, CaptureState NewState);


        private NewFrameReceivedCB OnFrameReceived;
        private ConnectionStateChangeCB OnConnectionStateChange;
        private CaptureStateChangeCB OnCaptureStateChange;


        //public bool IsStarted => frameGrabber?.IsStarted ?? false;

        //public bool IsStartable => Camera != null &&
        //                           Camera.IsConnected &&
        //                           !IsStarted;

        //public bool Aborted { get; private set; }


        //===============================================================================

        public void Startup(string Name, string IPAddress, 
                            NewFrameReceivedCB NewFrameReceivedCallback,
                            ConnectionStateChangeCB ConnectionStateChangeCallback,
                            CaptureStateChangeCB CaptureStateChangeCallback)
        {
            Camera = null;
            CameraName = Name;
            IPAddressStr = IPAddress;
            OnFrameReceived += NewFrameReceivedCallback;
            OnConnectionStateChange += ConnectionStateChangeCallback;
            OnCaptureStateChange += CaptureStateChangeCallback;

            DoCaptureFrames = false;

            discovery = CameraDiscovery.CreateFromProducerFile("SICKGigEVisionTL.cti");

            StopCamThread = false;

            CamThread = new Thread(CameraProcessingThread);
            CamThread.Start();
        }

        public void StartCapturingFrames()
        {
            DoCaptureFrames = true;
        }

        public void StopCapturingFrames()
        {
            DoCaptureFrames = false;
        }

        public void Shutdown()
        {
            StopCamThread = true;
            Thread.Sleep(100);
            try
            {
                if (Camera != null)
                {
                    if (Camera.IsConnected) Camera.Disconnect();
                    Camera.Dispose();
                    Camera = null;
                }
            }
            catch(Exception exp)
            {
                Logger.WriteException(exp);
            }

        }

        //===============================================================================

        private void SetConnectionState(ConnectionState newState, bool forceCallback = false)
        {
            if((newState!=CameraConnectionState) || forceCallback)
            {
                CameraConnectionState = newState;
                if (OnConnectionStateChange != null)
                    OnConnectionStateChange(this, CameraConnectionState);
            }
        }

        private void SetCaptureState(CaptureState newState, bool forceCallback = false)
        {
            if ((newState != CameraCaptureState) || forceCallback)
            {
                CameraCaptureState = newState;
                if (OnCaptureStateChange != null)
                    OnCaptureStateChange(this, CameraCaptureState);
            }
        }



        //Camera Sim Enabled

        //===============================================================================

        private bool ProcessRealCamera()
        {
            // If camera is distabled...just return
            if (ParamStorage.GetInt("Camera Enabled") == 0)
            {
                Thread.Sleep(100);
                return true;
            }

            //======================================================
            // If we don't have the camera, try finding it
            //======================================================
            if (Camera == null)
            {
                Logger.WriteLine("Searching for camera " + CameraName + " at " + IPAddressStr);
                SetConnectionState(ConnectionState.Searching);

                try
                {
                    Camera = discovery.ConnectTo(System.Net.IPAddress.Parse(IPAddressStr));
                }
                catch (Exception) { }

                if (Camera == null)
                {
                    Logger.WriteLine("Cannot find camera " + CameraName + " at " + IPAddressStr);
                    Thread.Sleep(1000);
                    return false;
                }

                Logger.WriteLine("Camera Connected " + CameraName + " at " + IPAddressStr + "  " + Camera.IsConnected);
                // Camera.ExportParameters("CameraParameters.txt");

                Logger.WriteLine("Setting Camera Parameters " + CameraName + " at " + IPAddressStr);
                if (SetupParameters_Scan(Camera) == false)
                {
                    Logger.WriteLine("Camera Parameter Setup Failed " + CameraName + " at " + IPAddressStr);
                    SetConnectionState(ConnectionState.BadParameters);
                    frameGrabber = null;
                }
                else
                {
                    SetConnectionState(ConnectionState.Connected);
                    Logger.WriteLine("Camera Parameter Setup Succeeded " + CameraName + " at " + IPAddressStr);
                    frameGrabber = Camera.CreateFrameGrabber();
                }
            }

            //======================================================
            // If the camera is no longer connected
            //======================================================
            if (Camera != null && Camera.IsConnected == false)
            {
                Logger.WriteLine("Camera no longer connected " + CameraName + " at " + IPAddressStr + "  " + Camera.IsConnected);

                try
                {
                    Camera.Dispose();
                }
                catch (Exception e) { Logger.WriteException(e); }


                Camera = null;
                frameGrabber = null;
                LastFrame = null;
                SetConnectionState(ConnectionState.Disconnected);
                Thread.Sleep(1000);
                return false;
            }

            //======================================================
            // Can't move forward if parameters are bad
            //======================================================
            if ((Camera != null) && (Camera.IsConnected) && (CameraConnectionState == ConnectionState.BadParameters))
            {
                Logger.WriteLine("Camera Parameter Setup Failed " + CameraName + " at " + IPAddressStr);
                Thread.Sleep(1000);
                return false;
            }

            //======================================================
            // Camera is good, toggle frameGrabber capturing
            //======================================================
            if (frameGrabber != null)
            {
                if (DoCaptureFrames == true)
                {
                    if (!frameGrabber.IsStarted)
                    {
                        frameGrabber.Start();
                        SetCaptureState(CaptureState.Capturing);
                        Logger.WriteLine("Started FrameGrabber for " + CameraName + " at " + IPAddressStr);
                        Thread.Sleep(100);
                    }

                    if(frameGrabber.IsStarted)
                    { 

                        try
                        {
                            Logger.WriteLine("frameGrabber.Grab()...");
                            // BLOCKING HERE!
                            IFrameWaitResult res = frameGrabber.Grab(new TimeSpan(0, 0, 120));
                            if (res != null)
                            {
                                res.IfCompleted(OnNewFrame)
                                    .IfAborted(OnAborted)
                                    .IfTimedOut(OnTimedOut);
                            }
                        }
                        catch (Exception E)
                        {
                            Logger.WriteException(E);
                        }

                        return false;
                    }
                }

                if (DoCaptureFrames == false)
                {
                    if (frameGrabber.IsStarted)
                    {
                        frameGrabber.Stop();
                        SetCaptureState(CaptureState.Stopped);

                        Logger.WriteLine("Stopped FrameGrabber for " + CameraName + " at " + IPAddressStr);
                        Thread.Sleep(100);
                        return false;
                    }
                }
            }

            return true;
        }

        //===============================================================================
        private void UpdateSimCamera(bool restart)
        {
            if (restart)
            {
                Logger.WriteLine("UpdateSimCamera::RESTARTING");
                // Get directory files and reset index
                string RootDir = Environment.GetEnvironmentVariable("MCS_ROOT_DIR");
                DirectoryInfo info = new DirectoryInfo(RootDir+'\\'+ParamStorage.GetString("Camera Sim Directory"));
                SimModeFileList = info.GetFiles("*.r3").OrderBy(p => p.CreationTime).ToArray();
                SimModeFileIndex = 0;
                SimModeNextDT = DateTime.Now;
            }

            if(DateTime.Now >= SimModeNextDT)
            {
                float DT = ParamStorage.GetFloat("Camera Sim Sec Per Pallet");
                SimModeNextDT = DateTime.Now + TimeSpan.FromSeconds(DT);


                if (SimModeFileList.Length > 0)
                {
                    FileInfo FI = SimModeFileList[SimModeFileIndex % SimModeFileList.Length];
                    Logger.WriteLine("UpdateSimCamera::New frame! " + SimModeFileIndex.ToString());
                    Logger.WriteLine(FI.FullName);

                    CaptureBuffer CB = new CaptureBuffer();
                    CB.Load(FI.FullName);

                    R3Cam.Frame F = new Frame();
                    F.Time = DateTime.Now;
                    F.Width = CB.Width;
                    F.Height = CB.Height;
                    F.FrameID = (ulong)SimModeFileIndex;
                    F.Camera = this;
                    F.Range = CB.Buf;
                    F.Reflectance = null;
                    //F.Scatter = null;

                    SimModeFileIndex += 1;

                    if (OnFrameReceived != null)
                    {
                        Logger.WriteLine("--------------------------------------------");
                        Logger.WriteLine("UpdateSimCamera::Calling OnFrameReceived");
                        OnFrameReceived(this, F);
                        Logger.WriteLine("UpdateSimCamera::Calling OnFrameReceived...called");
                        Logger.WriteLine("--------------------------------------------");
                    }
                    else
                    {
                        Logger.WriteLine("UpdateSimCamera::Calling OnFrameReceived == NULL!");
                    }
                }
            }

            Thread.Sleep(100);
        }

        //===============================================================================
        private void CameraProcessingThread()
        {
            //======================================================
            // Init
            //======================================================
            SetConnectionState(ConnectionState.Shutdown, true);
            SetCaptureState(CaptureState.Stopped, true);
            Camera = null;



            //======================================================
            // Repeat forever
            //======================================================
            while (true)
            {
                if (StopCamThread) break;

                if(!InSimMode)
                {
                    if (ParamStorage.GetInt("Camera Sim Enabled") == 1)
                    {
                        // Switching to sim mode
                        InSimMode = true;
                        Logger.WriteLine("Camera SIM MODE Enabled");
                        UpdateSimCamera(true);
                    }
                    else 
                    {
                        ProcessRealCamera();
                    }
                }
                else
                {
                    if (ParamStorage.GetInt("Camera Sim Enabled") == 0)
                    {
                        // Switching to real camera
                        InSimMode = false;
                        Logger.WriteLine("Camera SIM MODE Disabled");
                        ProcessRealCamera();
                    }
                    else
                    {
                        UpdateSimCamera(false);
                    }
                }

            }

            //======================================================
            // Shutdown frameGrabber
            //======================================================
            try
            {
                if (frameGrabber != null)
                {
                    if (frameGrabber.IsStarted) frameGrabber.Stop();
                    frameGrabber.Dispose();
                    frameGrabber = null;
                }
            }
            catch(Exception e) { Logger.WriteException(e); }

            //======================================================
            // Shutdown Camera
            //======================================================
            try
            {
                if (Camera != null)
                {
                    if(Camera.IsConnected) Camera.Disconnect();
                    Camera.Dispose();
                    Camera = null;
                }
            }
            catch (Exception e) { Logger.WriteException(e); }

            //======================================================
            // Clear things out
            //======================================================
            Camera = null;
            frameGrabber = null;
            LastFrame = null;
            SetConnectionState(ConnectionState.Shutdown);
        }

        //===============================================================================
        private static bool SetupParameters_Scan(ICamera camera)
        {
            // HACK
            return true;
            // Set some parameters

            //try
            //{
            //    // Copy current parameters from the camera
            //    CameraParameters parameters = camera.GetCameraParameters();
                

            //    // Modify scan type
            //    parameters.DeviceScanType.Set(DeviceScanType.LINESCAN_3D);
            //    parameters.ChunkModeActive.Set(true);

            //    RegionParameters scan3D = parameters.Region(RegionId.SCAN_3D_EXTRACTION_1);
            //    {
            //        ComponentParameters rangeParams = scan3D.Component(ComponentId.RANGE);
            //        rangeParams.ComponentEnable.Set(true);
            //        rangeParams.PixelFormat.Set(Sick.GenIStream.PixelFormat.COORD_3D_C16);
            //        rangeParams.Dispose();


            //        //ComponentParameters reflectanceParams = scan3D.Component(ComponentId.REFLECTANCE);
            //        //reflectanceParams.ComponentEnable.Set(true);
            //        //reflectanceParams.PixelFormat.Set(Sick.GenIStream.PixelFormat.MONO_16);
            //        //reflectanceParams.Dispose();


            //        //ComponentParameters scatterParams = scan3D.Component(ComponentId.SCATTER);
            //        //scatterParams.ComponentEnable.Set(true);
            //        //scatterParams.PixelFormat.Set(Sick.GenIStream.PixelFormat.MONO_16);
            //        //scatterParams.Dispose();
            //    }
            //    scan3D.Dispose();
            //    return true;
            //}
            //catch (Exception ex)
            //{
            //    Logger.WriteException(ex);
            //    return false;
            //}
        }

        //===============================================================================
        private void OnNewFrame(IFrame frame)
        {
            Logger.WriteLine("NEW FRAME FROM CAMERA! " + frame.GetFrameId().ToString() + "  " + CameraName + " at " + IPAddressStr);

            //if ((DoCaptureFrames == true) && (frameGrabber != null) && (frameGrabber.IsStarted))
            //{
            //    Logger.WriteLine("Requesting new Grab from FrameGrabber...");
            //    try
            //    {
            //        //Thread.Sleep(100);
            //        frameGrabber.Grab()
            //            .IfCompleted(OnNewFrame)
            //            .IfAborted(OnAborted)
            //            .IfTimedOut(OnTimedOut);
            //    }
            //    catch (Exception E)
            //    {
            //        Logger.WriteException(E);
            //    }
            //}

            R3Cam.Frame F = new Frame();
            F.Time = DateTime.Now;
            F.Width = 0;
            F.Height = 0;
            F.FrameID = frame.GetFrameId();
            F.Camera = this;

            F.Range = null;
            F.Reflectance = null;
            //F.Scatter = null;

            
            if (frame.GetRange() != null)
            {
                IComponent component = frame.GetRange();
                F.Width = component.GetWidth();
                F.Height = component.GetHeight();
                IntPtr data = component.GetData();
                int DataSize = (int)component.GetDataSize();
                int BPP = (int)component.GetBitsPerPixel();
                var count = (int)(DataSize / (int)Math.Ceiling(BPP / 8.0));
                Logger.WriteLine(string.Format("OnNewFrame  RANGE  W:{0} H:{1} DS:{2} BPP:{3} count:{4}", F.Width, F.Height, DataSize, BPP, count));
                F.Range = new UInt16[count];
                Copy(data, F.Range, count);
            }

            if (frame.GetReflectance() != null)
            {
                IComponent component = frame.GetReflectance();
                F.Width = component.GetWidth();
                F.Height = component.GetHeight();
                IntPtr data = component.GetData();
                int DataSize = (int)component.GetDataSize();
                int BPP = (int)component.GetBitsPerPixel();
                var count = (int)(DataSize / (int)Math.Ceiling(BPP / 8.0));
                Logger.WriteLine(string.Format("OnNewFrame  REFLC  W:{0} H:{1} DS:{2} BPP:{3} count:{4}", F.Width, F.Height, DataSize, BPP, count));
                F.Reflectance = new byte[count];
                Copy(data, F.Reflectance, count);
            }

            //if (frame.GetScatter() != null)
            //{
            //    IComponent component = frame.GetScatter();
            //    F.Width = component.GetWidth();
            //    F.Height = component.GetHeight();
            //    IntPtr data = component.GetData();
            //    var count = (int)(component.GetDataSize() / (int)Math.Ceiling(component.GetBitsPerPixel() / 8.0));
            //    F.Scatter = new byte[count];
            //    Copy(data, F.Scatter, count);
            //}

            if (F.Range != null)
            {
                bool ValidScan = true;
                int mny=0, mxy=0;
                //if (ParamStorage.GetInt("Invalid Scan Enabled")==1)
                //{
                //    int cx = F.Width / 2;
                //    mny = F.Height;
                //    mxy = 0;
                //    for (int cy = 0; cy < F.Height; cy++)
                //    {
                //        if (F.Range[cy * F.Width + cx] != 0)
                //        {
                //            if (cy < mny) mny = cy;
                //            if (cy > mxy) mxy = cy;
                //        }
                //    }
                //    ValidScan = (mny < 200) && (mxy > F.Height - 200);
                //}

                if (ValidScan)
                {
                    try
                    {
                        if (OnFrameReceived != null)
                            OnFrameReceived(this, F);
                    }
                    catch(Exception exp)
                    {
                        Logger.WriteException(exp);
                    }
                }
                else
                {
                    Logger.WriteLine(string.Format("INVALID SCAN.  MNY,MXY = {0},{1}", mny, mxy));
                }
            }
            else
            {
                Logger.WriteLine(string.Format("INVALID SCAN.  NO RANGE DATA"));
            }



        }

        private void OnAborted()
        {
            Logger.WriteLine("FRAME GRABBER: OnAborted");
            frameGrabber = null;
        }

        private void OnTimedOut()
        {
            Logger.WriteLine("FRAME GRABBER: OnTimedOut");
        }

        //===============================================================================

        public void SaveSettings(string FileName)
        {
            if(Camera!=null && Camera.IsConnected)
            {
                try
                {
                    Camera.ExportParameters(FileName);
                }
                catch(Exception)
                {

                }
            }
        }
        //public void ExportParameters(string filePath)
        //{
        //    genistreamPINVOKE.ICamera_ExportParameters(swigCPtr, filePath);
        //    if (genistreamPINVOKE.SWIGPendingException.Pending)
        //    {
        //        throw genistreamPINVOKE.SWIGPendingException.Retrieve();
        //    }
        //}

        //public ConfigurationResult ImportParameters(string filePath)
        public bool LoadSettings(string FileName)
        {
            if (Camera != null && Camera.IsConnected)
            {
                ConfigurationResult Result = Camera.ImportParameters(FileName);
                if(Result.Status == ConfigurationStatus.OK)
                {
                    return true;
                }
                else
                {
                    // TODO:
                    Logger.WriteLine("CAMERA LOAD SETTINGS IMPORT BAD PARAMETERS: " + Result.Status.ToString());
                    return false;
                }
                
            }
            return true;
        }


        public void SaveInHistory(string Reason)
        {
            DateTime DT = DateTime.Now;
            string dt_str = String.Format("{0:0000}{1:00}{2:00}_{3:00}{4:00}{5:00}", DT.Year, DT.Month, DT.Day, DT.Hour, DT.Minute, DT.Second);

            string SaveDir = MainWindow.CameraConfigHistoryRootDir;

            this.SaveSettings(SaveDir + "\\" + dt_str + "_CAMCONFIG_" + Reason + ".csv");
        }

        //===============================================================================

        public static unsafe void Copy(IntPtr ptrSource, UInt16[] dest, int elements)
        {
            fixed (UInt16* ptrDest = &dest[0])
            {
                CopyMemory((IntPtr)ptrDest, ptrSource, (uint)(elements * 2));    // 2 bytes per element
            }
        }

        public static unsafe void Copy(IntPtr ptrSource, byte[] dest, int elements)
        {
            fixed (byte* ptrDest = &dest[0])
            {
                CopyMemory((IntPtr)ptrDest, ptrSource, (uint)(elements));    // 1 bytes per element
            }
        }

        //public static unsafe void Copy(ushort[] source, IntPtr ptrDest, uint elements)
        //{
        //    fixed (ushort* ptrSource = &source[0])
        //    {
        //        CopyMemory(ptrDest, (IntPtr)ptrSource, elements * 2);    // 2 bytes per element
        //    }
        //}

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint NumBytes);
        //===============================================================================
        //ICamera FindCameraUsingDiscovery()
        //{
        //    Logger.WriteBorder("FindCam");

        //    DiscoveredCameraList foundCameras = discovery.ScanForCameras();
        //    if (foundCameras.Count == 0)
        //    {
        //        Logger.WriteLine("No devices found.");
        //        return null;
        //    }

        //    // A bit contrived to exercise the reconnect method
        //    if (Camera == null)
        //    {
        //        Camera = discovery.ConnectTo(foundCameras.First());
        //    }
        //    else
        //    {
        //        Camera = discovery.Reconnect(Camera);
        //    }
        //    Camera.Disconnected += _camera_Disconnect;
        //    Logger.WriteLine("Camera Connected!");


        //    return Camera;

        //}


        //ICamera FindCameraUsingIP(string IPAddressStr)
        //{
        //    Logger.WriteBorder("FindCameraUsingIP " + IPAddressStr);

        //    Camera = discovery.ConnectTo(System.Net.IPAddress.Parse(IPAddressStr));
        //    if (Camera == null)
        //    {
        //        Logger.WriteLine("No camera found at " + IPAddressStr);
        //        return null;
        //    }

        //    Camera.Disconnected += _camera_Disconnect;
        //    Logger.WriteLine("Camera Connected!");

        //    return Camera;
        //}




        //public bool StartMeasureOneShot()
        //{
        //    if (Camera == null)
        //        return false;

        //    if (IsStarted)
        //        return false;

        //    if (SetupParameters_Scan(Camera) == false)
        //    {
        //        StopMeasure();
        //        return false;
        //    }
        //    //GetFrames = new System.Windows.Forms.Timer();
        //    //GetFrames.Interval = 10;
        //    try
        //    {
        //        if (frameGrabber == null)
        //            frameGrabber = Camera.CreateFrameGrabber();

        //        frameGrabber.Start();
        //        GrabFrames(null, EventArgs.Empty);

        //        //GetFrames.Start();
        //    }
        //    catch(Exception E)
        //    {

        //    }

        //    return true;
        //}


        //public void StopMeasure()
        //{
        //    frameGrabber.Stop();
        //    //GetFrames.Stop();
        //}

        //private void GrabFrames(object sender, EventArgs e)
        //{
        //    if (!Aborted)
        //    {
        //        try
        //        {

        //            frameGrabber.Grab()
        //                .IfCompleted(OnNewFrame)
        //                .IfAborted(OnAborted)
        //                .IfTimedOut(OnAborted);
        //        }
        //        catch(Exception E)
        //        {

        //        }
        //    }
        //    else
        //    {
        //        //GetFrames.Stop();
        //        frameGrabber.Stop();
        //        frameGrabber.Dispose();
        //    }
        //}

        //private void OnAborted()
        //{

        //}

        //private void OnTimedOut()
        //{

        //}
        //private void OnNewFrame(IFrame frame)
        //{
        //    System.Console.WriteLine("New frame! " + frame.GetFrameId().ToString());
        //    IComponent componentRange = frame.GetRange();
        //    //IComponent componentReflectance = frame.GetReflectance();
        //    //IComponent componentScatter = frame.GetScatter();


        //    IntPtr data = componentRange.GetData();

        //    var size = (int)(componentRange.GetDataSize() / (int)Math.Ceiling(componentRange.GetBitsPerPixel() / 8.0));

        //    UInt16[] managedArray = new UInt16[size];

        //    Copy(data, managedArray, size);

        //    if (OnFrameReceived != null)
        //        OnFrameReceived(managedArray);
        //}




        //private void _camera_Disconnect(string deviceId)
        //{
        //    Logger.WriteLine("Camera Disconnected");
        //    Shutdown();
        //}



        //private static void SetupParameters_Image(ICamera camera)
        //{
        //}

        //private static void measureCoG(IFrame frame)
        //{
        //}
    }
}
