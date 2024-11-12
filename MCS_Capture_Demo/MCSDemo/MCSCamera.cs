using Sick.GenIStream;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using PalletCheck;

namespace MCS
{


    public class MCSCamera
    {
        public enum CaptureState
        {
            Stopped,
            Capturing,
        };

        
        private List<RulerCamera> Rulers = new List<RulerCamera>();
        private List<RulerCamera.Frame> RulerFrames = new List<RulerCamera.Frame>();

        public string CameraName { get; private set; }
        public string IPAddressStr { get; private set; }
        public RulerCamera.ConnectionState CameraConnectionState { get; private set; }
        public CaptureState CameraCaptureState { get; private set; }

        public MCSFrame LastFrame { get; private set; }


        private Thread MCSProcessingThread;
        private bool StopMCSProcessingThread = false;

        private bool DoCaptureFrames;
        private int RulerCount = 3;

        private MCSStitcher Stitcher;

        //===============================================================================

        public delegate void NewFrameReceivedCB(MCSCamera Cam, MCSFrame Frame);
        public delegate void ConnectionStateChangeCB(MCSCamera Cam, RulerCamera.ConnectionState NewState);
        public delegate void CaptureStateChangeCB(MCSCamera Cam, CaptureState NewState);


        private NewFrameReceivedCB OnFrameReceived;
        private ConnectionStateChangeCB OnConnectionStateChange;
        private CaptureStateChangeCB OnCaptureStateChange;


        //===============================================================================

        public void Startup(string Name, string IPAddress, 
                            NewFrameReceivedCB NewFrameReceivedCallback,
                            ConnectionStateChangeCB ConnectionStateChangeCallback,
                            CaptureStateChangeCB CaptureStateChangeCallback)
        {
            CameraName = Name;
            IPAddressStr = IPAddress;
            OnFrameReceived += NewFrameReceivedCallback;
            OnConnectionStateChange += ConnectionStateChangeCallback;
            OnCaptureStateChange += CaptureStateChangeCallback;


            Stitcher = new MCSStitcher();

            DoCaptureFrames = false;

            for( int i=0; i<RulerCount; i++)
            {
                string CameraName = "Ruler" + (i+1).ToString();
                string CameraIP = ParamStorage.GetString(CameraName+" IP");
                Logger.WriteLine(string.Format("MCS Creating " + CameraName + " at " + CameraIP));

                RulerCamera Ruler = new RulerCamera();
                Rulers.Add(Ruler);
                Ruler.Startup(CameraName, i, CameraIP, null, null, null);
                System.Threading.Thread.Sleep(1000);
            }

            UpdateConnectionStates();
            SetCaptureState(CaptureState.Stopped);

            StopMCSProcessingThread = false;
            MCSProcessingThread = new Thread(MCSProcessingThreadFunc);
            MCSProcessingThread.Start();
        }

        public void StartCapturingFrames()
        {
            DoCaptureFrames = true;
            foreach(RulerCamera Ruler in Rulers)
            {
                Ruler.StartCapturingFrames();
            }
            SetCaptureState(CaptureState.Capturing);
        }

        public void StopCapturingFrames()
        {
            DoCaptureFrames = false;
            foreach (RulerCamera Ruler in Rulers)
            {
                Ruler.StopCapturingFrames();
            }
            SetCaptureState(CaptureState.Stopped);
        }

        public List<RulerCamera> getRulerCamers()
        {
            return Rulers;
        }

        public void Shutdown()
        {
            try
            { 
                StopMCSProcessingThread = true;
                Thread.Sleep(100);

                // Tell the Rulers to shutdown
                foreach (RulerCamera Ruler in Rulers)
                {
                    Ruler.Shutdown();
                }

                Thread.Sleep(100);
                Rulers.Clear();
            }
            catch (Exception exp)
            {
                Logger.WriteException(exp);
            }
        }

        //===============================================================================

        private void SetConnectionState(RulerCamera.ConnectionState newState, bool forceCallback = false)
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


        //===============================================================================
        private void UpdateConnectionStates()
        {
            // Runs in MCSProcessingThreadFunc
            RulerCamera.ConnectionState state = RulerCamera.ConnectionState.Connected;
            foreach (RulerCamera Ruler in Rulers)
                if (Ruler.CameraConnectionState != RulerCamera.ConnectionState.Connected)
                {
                    state = Ruler.CameraConnectionState;
                }

            SetConnectionState(state);
        }

        //===============================================================================
        private void MCSProcessingThreadFunc()
        {
            DateTime lastCaptureDT = DateTime.Now;
            DateTime lastConnectionStateDT = DateTime.Now;

            //======================================================
            // Repeat forever
            //======================================================
            while (true)
            {
                if (StopMCSProcessingThread)
                {
                    break;
                }

                if (Rulers.Count < RulerCount)
                {
                    Thread.Sleep(100);
                    continue;
                }

                // Check if there is a new set of frames from the cameras
                if (Rulers[0].LastFrame != null && Rulers[0].LastFrame.Time > lastCaptureDT)
                {
                    // Check if all cameras have new frames
                    int completedCaptures = 0;
                    foreach (RulerCamera Ruler in Rulers)
                        if (Ruler.LastFrame != null)
                        {
                            double secs = (Ruler.LastFrame.Time - Rulers[0].LastFrame.Time).TotalSeconds;
                            if (secs < 0.5) completedCaptures += 1;
                        }

                    if (completedCaptures == Rulers.Count)
                    {
                        // All cameras have new captures
                        RulerFrames.Clear();
                        foreach (RulerCamera Ruler in Rulers)
                        {
                            RulerFrames.Add(Ruler.LastFrame);
                            Ruler.ClearFrame();
                        }

                        // Process New Capture
                        lastCaptureDT = RulerFrames[0].Time;

                        ProcessNewCapture();
                    }
                    else
                    {
                        // We are close to having all frames
                        Thread.Sleep(10);
                    }
                }
                else
                {
                    // Nothing new from first camera
                    Thread.Sleep(100);
                }


                // See if we should update connection states
                if ((DateTime.Now - lastConnectionStateDT).TotalSeconds > 1.0)
                {
                    lastConnectionStateDT = DateTime.Now;
                    UpdateConnectionStates();
                }

            }
        }


        //===============================================================================

        public void SaveSettings(string FileName)
        {
            foreach(RulerCamera Ruler in Rulers)
            {
                try
                {
                    Ruler.SaveSettings(FileName);
                }
                catch(Exception)
                {

                }
            }
        }

        //public bool LoadSettings(string FileName)
        //{
        //    if (Camera != null && Camera.IsConnected)
        //    {
        //        ConfigurationResult Result = Camera.ImportParameters(FileName);
        //        if(Result.Status == ConfigurationStatus.OK)
        //        {
        //            return true;
        //        }
        //        else
        //        {
        //            // TODO:
        //            Logger.WriteLine("CAMERA LOAD SETTINGS IMPORT BAD PARAMETERS: " + Result.Status.ToString());
        //            return false;
        //        }
                
        //    }
        //    return true;
        //}


        public void SaveInHistory(string Reason)
        {
            DateTime DT = DateTime.Now;
            string dt_str = String.Format("{0:0000}{1:00}{2:00}_{3:00}{4:00}{5:00}", DT.Year, DT.Month, DT.Day, DT.Hour, DT.Minute, DT.Second);

            string SaveDir = MainWindow.CameraConfigHistoryRootDir;

            this.SaveSettings(SaveDir + "\\" + dt_str + "_CAMCONFIG_" + Reason + ".csv");
        }

        //===============================================================================

        private void ProcessNewCapture()
        {
            MCSFrame F = new MCSFrame();
            F.FrameID = RulerFrames[0].FrameID;

            // Runs in MCSProcessingThreadFunc
            Logger.WriteLine("MCSCamera::ProcessNewCapture() "+ F.FrameID.ToString());


            // Convert the raw ruler buffers into capture buffers
            F.RulerRangeCBs.Clear();
            F.RulerReflectanceCBs.Clear();
            foreach (RulerCamera.Frame RFrame in RulerFrames)
            {
                CaptureBuffer RRangeCB = new CaptureBuffer(RFrame.Range, RFrame.Width, RFrame.Height);
                F.RulerRangeCBs.Add(RRangeCB);

                CaptureBuffer RReflectanceCB = new CaptureBuffer(RFrame.Reflectance, RFrame.Width, RFrame.Height);
                RReflectanceCB.PaletteType = CaptureBuffer.PaletteTypes.Gray;
                F.RulerReflectanceCBs.Add(RReflectanceCB);
            }

            Stitcher.StitchFrame(F);

            if( ParamStorage.GetInt("MCS Reverse Conveyor Detect Enabled") != 0)
            {
                if (Stitcher.IsReverseMotionCutoffBadPalletYuck(F))
                {
                    Logger.WriteLine("MCSCamera::ProcessNewCapture Reverse Cutoff Discovered " + F.FrameID.ToString());
                    return;
                }
            }

            Logger.WriteLine("MCSCamera::ProcessNewCapture Complete " + F.FrameID.ToString());

            LastFrame = F;
            if (OnFrameReceived != null)
                OnFrameReceived(this, F);
        }

        //===============================================================================

    }
}
