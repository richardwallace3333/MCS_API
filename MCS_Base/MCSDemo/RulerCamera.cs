using Sick.GenIStream;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.IO;


// https://supportportal.sick.com/login/?next=%2F

namespace PalletCheck
{
    public class RulerCamera
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

            public RulerCamera Camera;

            public override string ToString()
            {
                string s = "";
                s = string.Format("Ruler.Frame:  cam:{0}  w:{1}  h:{2}   range:{3}  reflectance:{4}  scatter:{5}", 
                    Camera.CameraName, Width, Height, (Range != null), (Reflectance != null), false);
                return s;
            }
        }

        public ICamera Camera { get; private set; }
        public string CameraName { get; private set; }
        public int CameraIndex { get; private set; }
        public string IPAddressStr { get; private set; }
        public ConnectionState CameraConnectionState { get; private set; }
        public CaptureState CameraCaptureState { get; private set; }

        public Frame LastFrame { get; private set; }


        private static CameraDiscovery discovery;
        private FrameGrabber frameGrabber;

        private Thread CamThread;
        private bool StopCamThread = false;

        private bool DoCaptureFrames;


        //===============================================================================

        public delegate void NewFrameReceivedCB(RulerCamera Cam, RulerCamera.Frame Frame);
        public delegate void ConnectionStateChangeCB(RulerCamera Cam, ConnectionState NewState);
        public delegate void CaptureStateChangeCB(RulerCamera Cam, CaptureState NewState);

        private NewFrameReceivedCB OnFrameReceived;
        private ConnectionStateChangeCB OnConnectionStateChange;
        private CaptureStateChangeCB OnCaptureStateChange;

        //===============================================================================

        public void Startup(string Name, int Index, string IPAddress, 
                            NewFrameReceivedCB NewFrameReceivedCallback,
                            ConnectionStateChangeCB ConnectionStateChangeCallback,
                            CaptureStateChangeCB CaptureStateChangeCallback)
        {
            Camera = null;
            CameraName = Name;
            CameraIndex = Index;
            IPAddressStr = IPAddress;
            OnFrameReceived += NewFrameReceivedCallback;
            OnConnectionStateChange += ConnectionStateChangeCallback;
            OnCaptureStateChange += CaptureStateChangeCallback;

            DoCaptureFrames = false;

            if(discovery==null)
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
                    //Camera.Dispose();
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

        ////===============================================================================

        //private bool ProcessRealCamera()
        //{
        //    // If camera is distabled...just return
        //    if (ParamStorage.GetInt("Camera Enabled") == 0)
        //    {
        //        Thread.Sleep(100);
        //        return true;
        //    }

        //    //======================================================
        //    // If we don't have the camera, try finding it
        //    //======================================================
        //    if (Camera == null)
        //    {
        //        Logger.WriteLine("Searching for camera " + CameraName + " at " + IPAddressStr);
        //        SetConnectionState(ConnectionState.Searching);

        //        try
        //        {
        //            Camera = discovery.ConnectTo(System.Net.IPAddress.Parse(IPAddressStr));
        //        }
        //        catch (Exception) { }

        //        if (Camera == null)
        //        {
        //            Logger.WriteLine("Cannot find camera " + CameraName + " at " + IPAddressStr);
        //            Thread.Sleep(1000);
        //            return false;
        //        }

        //        Logger.WriteLine("Camera Connected " + CameraName + " at " + IPAddressStr + "  " + Camera.IsConnected);
        //        // Camera.ExportParameters("CameraParameters.txt");

        //        //Logger.WriteLine("Setting Camera Parameters " + CameraName + " at " + IPAddressStr);
        //        //if (SetupParameters_Scan(Camera) == false)
        //        //{
        //        //    Logger.WriteLine("Camera Parameter Setup Failed " + CameraName + " at " + IPAddressStr);
        //        //    SetConnectionState(ConnectionState.BadParameters);
        //        //    frameGrabber = null;
        //        //}
        //        //else
        //        //{
        //        //    SetConnectionState(ConnectionState.Connected);
        //        //    Logger.WriteLine("Camera Parameter Setup Succeeded " + CameraName + " at " + IPAddressStr);
        //        //    frameGrabber = Camera.CreateFrameGrabber();
        //        //}

        //        SetConnectionState(ConnectionState.Connected);
        //        // Logger.WriteLine("Camera Parameter Setup Succeeded " + CameraName + " at " + IPAddressStr);
        //        frameGrabber = Camera.CreateFrameGrabber();
        //    }

        //    //======================================================
        //    // If the camera is no longer connected
        //    //======================================================
        //    if (Camera != null && Camera.IsConnected == false)
        //    {
        //        Logger.WriteLine("Camera no longer connected " + CameraName + " at " + IPAddressStr + "  " + Camera.IsConnected);

        //        try
        //        {
        //            Camera.Dispose();
        //        }
        //        catch (Exception e) { Logger.WriteException(e); }


        //        Camera = null;
        //        frameGrabber = null;
        //        LastFrame = null;
        //        SetConnectionState(ConnectionState.Disconnected);
        //        Thread.Sleep(1000);
        //        return false;
        //    }

        //    //======================================================
        //    // Can't move forward if parameters are bad
        //    //======================================================
        //    if ((Camera != null) && (Camera.IsConnected) && (CameraConnectionState == ConnectionState.BadParameters))
        //    {
        //        Logger.WriteLine("Camera Parameter Setup Failed " + CameraName + " at " + IPAddressStr);
        //        Thread.Sleep(1000);
        //        return false;
        //    }

        //    //======================================================
        //    // Camera is good, toggle frameGrabber capturing
        //    //======================================================
        //    if (frameGrabber != null)
        //    {
        //        if (DoCaptureFrames == true)
        //        {
        //            if (!frameGrabber.IsStarted)
        //            {
        //                frameGrabber.Start();
        //                SetCaptureState(CaptureState.Capturing);
        //                Logger.WriteLine("Started FrameGrabber for " + CameraName + " at " + IPAddressStr + " buffcount: "+frameGrabber.GetAvailableCount().ToString());
        //                Thread.Sleep(1000);
        //            }

        //            if(frameGrabber.IsStarted)
        //            { 

        //                try
        //                {
        //                    Logger.WriteLine("frameGrabber.Grab()... " + CameraName);
        //                    // BLOCKING HERE!
        //                    IFrameWaitResult res = frameGrabber.Grab(new TimeSpan(0, 0, 120));
        //                    //IFrameWaitResult res = frameGrabber.Grab(new TimeSpan(0, 0, 5));
        //                    Logger.WriteLine("frameGrabber.Grab()...returned " + CameraName);
        //                    if (res != null)
        //                    {
        //                        res.IfCompleted(OnNewFrame)
        //                            .IfAborted(OnAborted)
        //                            .IfTimedOut(OnTimedOut);
        //                    }
        //                    else
        //                    {
        //                        Logger.WriteLine("frameGrabber.Grab()...returned NULL" + CameraName);
        //                    }
        //                }
        //                catch (Exception E)
        //                {
        //                    Logger.WriteException(E);
        //                }

        //                return false;
        //            }
        //        }

        //        if (DoCaptureFrames == false)
        //        {
        //            if (frameGrabber.IsStarted)
        //            {
        //                frameGrabber.Stop();
        //                SetCaptureState(CaptureState.Stopped);

        //                Logger.WriteLine("Stopped FrameGrabber for " + CameraName + " at " + IPAddressStr);
        //                Thread.Sleep(100);
        //                return false;
        //            }
        //        }
        //    }

        //    return true;
        //}


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
                SetConnectionState(ConnectionState.Connected);
                frameGrabber = Camera.CreateFrameGrabber();
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
                        Logger.WriteLine("Started FrameGrabber for " + CameraName + " at " + IPAddressStr + " buffcount: " + frameGrabber.GetAvailableCount().ToString());
                        Thread.Sleep(1000);
                    }

                    if (frameGrabber.IsStarted)
                    {

                        try
                        {
                            Logger.WriteLine("frameGrabber.Grab()... " + CameraName);
                            // BLOCKING HERE!
                            IFrameWaitResult res = frameGrabber.Grab(new TimeSpan(0, 0, 120));
                            //IFrameWaitResult res = frameGrabber.Grab(new TimeSpan(0, 0, 5));
                            Logger.WriteLine("frameGrabber.Grab()...returned " + CameraName);
                            if (res != null)
                            {
                                res.IfCompleted(OnNewFrame)
                                    .IfAborted(OnAborted)
                                    .IfTimedOut(OnTimedOut);
                            }
                            else
                            {
                                Logger.WriteLine("frameGrabber.Grab()...returned NULL" + CameraName);
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

                ProcessRealCamera();
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
        private void OnNewFrame(IFrame frame)
        {
            ulong frameId = frame.GetFrameId();
            Logger.WriteLine("NEW FRAME FROM CAMERA!   frameID:" + frameId.ToString() + "   " + CameraName + " at " + IPAddressStr);


            RulerCamera.Frame F = new Frame();
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
                F.Range = new UInt16[count];
                Copy(data, F.Range, count);
                double sum = 0;
                for (int i = 0; i < count; i++) sum += F.Range[i];
                Logger.WriteLine(string.Format("OnNewFrame  RANGE  W:{0} H:{1} DS:{2} BPP:{3} count:{4} sum:{5}", F.Width, F.Height, DataSize, BPP, count, sum));
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
                F.Reflectance = new byte[count];
                Copy(data, F.Reflectance, count);

                double sum = 0;
                for (int i = 0; i < count; i++) sum += F.Reflectance[i];
                Logger.WriteLine(string.Format("OnNewFrame  REFLC  W:{0} H:{1} DS:{2} BPP:{3} count:{4} sum:{5}", F.Width, F.Height, DataSize, BPP, count, sum));
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
                if (ValidScan)
                {
                    try
                    {
                        LastFrame = F;
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
                    Logger.WriteLine(string.Format("INVALID SCAN."));//  MNY,MXY = {0},{1}", mny, mxy));
                }
            }
            else
            {
                Logger.WriteLine(string.Format("INVALID SCAN.  NO RANGE DATA"));
            }



        }

        public void ClearFrame()
        {
            LastFrame = null;
        }

        private void OnAborted()
        {
            Logger.WriteLine("FRAME GRABBER: OnAborted "+CameraName);
            frameGrabber = null;
        }

        private void OnTimedOut()
        {
            Logger.WriteLine("FRAME GRABBER: OnTimedOut " + CameraName);
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
    }
}
