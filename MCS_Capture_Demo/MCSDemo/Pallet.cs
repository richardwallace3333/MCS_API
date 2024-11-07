using System.Windows;
using System;
using System.Collections.Generic;
//using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;
using PalletCheck.Controls;
using MCS;
using System.Windows.Input;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;
using System.Security.Policy;
using ScottPlot;
using System.Linq;

namespace PalletCheck
{

    public struct PalletPoint
    {
        public int X;
        public int Y;
        public int Z;
        public PalletPoint(int x, int y)
        {
            X = x; Y = y; Z = 0;
        }
        public PalletPoint(int x, int y, int z)
        {
            X = x; Y = y; Z = z;
        }
        public PalletPoint(double x, double y)
        {
            X = (int)x; Y = (int)y; Z = 0;
        }
        public PalletPoint(double x, double y, double z)
        {
            X = (int)x; Y = (int)y; Z = (int)z;
        }
        public static PalletPoint FromList(List<PalletPoint> points)
        {
            double X=0;
            double Y=0;
            double Z=0;
            double C=0;
            foreach(PalletPoint P in points)
            {
                X += P.X;
                Y += P.Y;
                Z += P.Z;
                C += 1;
            }
            if(C>0) { X /= C; Y /= C; Z /= C; }
            return new PalletPoint(X, Y, Z);
        }
    }

    public class PalletProcessor
    {
        List<Pallet> HighInputQueue = new List<Pallet>();
        List<Pallet> HighInProgressList = new List<Pallet>();
        List<Pallet> LowInputQueue = new List<Pallet>();
        List<Pallet> LowInProgressList = new List<Pallet>();

        int MaxHighThreads;
        int MaxLowThreads;
        object LockObject = new object();

        public PalletProcessor(int MaxHighThreads, int MaxLowThreads)
        {
            this.MaxHighThreads = MaxHighThreads;
            this.MaxLowThreads = MaxLowThreads;
        }


        public void ProcessPalletHighPriority(Pallet P, Pallet.palletAnalysisCompleteCB Callback)
        {
            P.OnAnalysisComplete_User += Callback;
            P.UseBackgroundThread = false;

            lock (LockObject)
            {
                HighInputQueue.Add(P);
                UpdateHigh();
            }
        }

        public void OnHighPalletCompleteCB(Pallet P)
        {
            // This runs in the MAIN THREAD

            // Cleanup High Queue
            lock (LockObject)
            {
                if (HighInProgressList.Contains(P))
                    HighInProgressList.Remove(P);

                UpdateHigh();
            }

            // Call pallet complete callback for user
            P.CallUserCallback();
        }

        public void UpdateHigh()
        {
            lock (LockObject)
            {
                // Check if we can launch threads for items in the queues
                if ((HighInputQueue.Count>0) && (HighInProgressList.Count < MaxHighThreads))
                {
                    Pallet P = HighInputQueue[0];
                    HighInputQueue.RemoveAt(0);
                    HighInProgressList.Add(P);

                    if (!Pallet.Shutdown)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            ThreadPriority TPBackup = Thread.CurrentThread.Priority;
                            Logger.WriteLine("HIGH THREAD PRIORITY: " + TPBackup.ToString());
                            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

                            try
                            {
                                P.DoAnalysisBlocking();
                                Thread.CurrentThread.Priority = TPBackup;
                            }
                            catch (Exception e)
                            {
                                Thread.CurrentThread.Priority = TPBackup;
                                Logger.WriteException(e);
                            }

                            if (!Pallet.Shutdown)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    OnHighPalletCompleteCB(P);
                                });
                            }
                        });
                    }
                }
            }
        }



        public void ProcessPalletLowPriority(Pallet P, Pallet.palletAnalysisCompleteCB Callback)
        {
            P.OnAnalysisComplete_User += Callback;
            P.UseBackgroundThread = true;

            lock (LockObject)
            {
                LowInputQueue.Add(P);
                UpdateLow();
            }
        }

        public void OnLowPalletCompleteCB(Pallet P)
        {
            // This runs in the MAIN THREAD

            // Cleanup Low Queue
            lock (LockObject)
            {
                if (LowInProgressList.Contains(P))
                    LowInProgressList.Remove(P);

                UpdateLow();
            }

            // Call pallet complete callback for user
            P.CallUserCallback();
        }

        public void UpdateLow()
        {
            lock (LockObject)
            {
                // Check if we can launch threads for items in the queues
                if ((LowInputQueue.Count>0) && (LowInProgressList.Count < MaxLowThreads))
                {
                    Pallet P = LowInputQueue[0];
                    LowInputQueue.RemoveAt(0);
                    LowInProgressList.Add(P);

                    Task.Factory.StartNew(() =>
                    {
                        ThreadPriority TPBackup = Thread.CurrentThread.Priority;
                        Logger.WriteLine("LOW THREAD PRIORITY: " + TPBackup.ToString());
                        Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                        try
                        {
                            P.DoAnalysisBlocking();
                            Thread.CurrentThread.Priority = TPBackup;
                        }
                        catch (Exception e)
                        {
                            Thread.CurrentThread.Priority = TPBackup;
                            Logger.WriteException(e);
                        }


                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            OnLowPalletCompleteCB(P);
                        });
                    });
                }
            }
        }
    }



    public class Pallet
    {
        public static bool Busy = false;
        public static bool Shutdown = false;

        //public bool NotLive = false;
        public DateTime CreateTime;
        public DateTime AnalysisStartTime;
        public DateTime AnalysisStopTime;
        public double AnalysisTotalSec;

        public bool UseBackgroundThread;
        

        public enum InspectionState
        {
            Unprocessed,
            Pass,
            Fail
        }

        public InspectionState State = InspectionState.Unprocessed;
        public string Directory;
        public string Filename;
       
        public CaptureBuffer OriginalCB;
        public CaptureBuffer Denoised;
        public MCSFrame MCSFrame;
        public List<PalletDefect> AllDefects = new List<PalletDefect>();

        public CaptureBuffer ReflectanceCB;

        public Dictionary<string, CaptureBuffer> CBDict = new Dictionary<string, CaptureBuffer>();
        public List<CaptureBuffer> CBList = new List<CaptureBuffer>();

        delegate CaptureBuffer addBufferCB(string s, UInt16[] buf);
        Thread[] BoardThreads;

        public delegate void palletAnalysisCompleteCB(Pallet P);
        public event palletAnalysisCompleteCB OnAnalysisComplete_User;

        public void CallUserCallback()
        {
            if(OnAnalysisComplete_User != null)
                OnAnalysisComplete_User(this);
        }

        public class Board
        {


            public string BoardName;
            public PalletDefect.DefectLocation Location;
            public CaptureBuffer CB;
            public CaptureBuffer CrackCB;
            public List<PalletPoint>[] Edges = new List<PalletPoint>[2];
            //public int ExpectedAreaPix;
            public int MeasuredAreaPix;
            public List<PalletPoint> Nails;
            public List<PalletPoint> Cracks;
            public int[,] CrackTracker;
            public int[,] BoundaryBlocks;
            public int CrackBlockSize;
            public PalletPoint BoundsP1;
            public PalletPoint BoundsP2;
            public bool IsHoriz;
            public List<PalletDefect> AllDefects;

            public float MaxDeflection;
            public float MissingWoodPercent;


            public Board(string Name, PalletDefect.DefectLocation Location, bool Horiz)
            {
                Edges[0] = new List<PalletPoint>();
                Edges[1] = new List<PalletPoint>();
                Nails = new List<PalletPoint>();
                Cracks = new List<PalletPoint>();
                BoardName = Name;
                this.IsHoriz = Horiz;
                this.Location = Location;
                //ExpectedAreaPix = ExpectedWidth * ExpectedLength;
                AllDefects = new List<PalletDefect>();
            }
        }

        public List<Board> BList;
        public List<PalletDefect> PalletLevelDefects = new List<PalletDefect>();

        public int px010XYIn;
        public int px020XYIn;
        public int px025XYIn;
        public int px050XYIn;
        public int px075XYIn;
        public int px100XYIn;
        public int px025ZIn;
        public int px050ZIn;
        public int px075ZIn;
        public int px100ZIn;
        public int BaselineZ;

        //=====================================================================
        public Pallet(MCSFrame Frame)
        {
            MCSFrame = Frame;
            OriginalCB = Frame.RangeCB;
            ReflectanceCB = Frame.ReflectanceCB;

            CreateTime = DateTime.Now;
            Filename = String.Format("{0:0000}{1:00}{2:00}_{3:00}{4:00}{5:00}.r3",
                                             CreateTime.Year, CreateTime.Month, CreateTime.Day, CreateTime.Hour, CreateTime.Minute, CreateTime.Second);

            Directory = String.Format("{0:0000}{1:00}{2:00}_{3:00}",
                                             CreateTime.Year, CreateTime.Month, CreateTime.Day, CreateTime.Hour);

            InitValues();
        }

        public Pallet(string Filename)
        {
            OriginalCB = null;
            CreateTime = DateTime.Now;
            this.Filename = Filename;
            InitValues();
        }

        private void InitValues()
        {
            float ppiX = (25.4f / ParamStorage.GetFloat("MM Per Pixel Y"));
            float ppiZ = (25.4f / ParamStorage.GetFloat("MM Per Pixel Z"));

            px010XYIn = (int)(ppiX * 0.10);
            px020XYIn = (int)(ppiX * 0.20);
            px025XYIn = (int)(ppiX * 0.25);
            px050XYIn = (int)(ppiX * 0.50);
            px075XYIn = (int)(ppiX * 0.75);
            px100XYIn = (int)(ppiX * 1.00);
            px025ZIn = (int)(ppiZ * 0.25);
            px050ZIn = (int)(ppiZ * 0.50);
            px075ZIn = (int)(ppiZ * 0.75);
            px100ZIn = (int)(ppiZ * 1.00);
            BaselineZ = ParamStorage.GetInt("MCS DstReferenceZ (px)");
        }

        ~Pallet()
        {
            OriginalCB = null;
            Denoised = null;
        }

        //=====================================================================

        //public void DoAsynchronousAnalysis()
        //{
        //    // Launches new thread then executes callback on caller thread

        //    Task.Factory.StartNew(() =>
        //    {
        //        try
        //        {
        //            DoAnalysis();
        //        }
        //        catch(Exception e)
        //        {
        //            Logger.WriteException(e);
        //        }

        //        Application.Current.Dispatcher.Invoke(() =>
        //        {
        //            if (this.OnAnalysisComplete_Processor != null)
        //                this.OnAnalysisComplete_Processor(this);
        //        });
        //    });

        //}

        //public void DoSynchronousAnalysis()
        //{
        //    // Stays on calling thread and blocks
        //    try
        //    {
        //        DoAnalysis();
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.WriteException(e);
        //    }

        //    if (OnAnalysisComplete_Processor != null)
        //        OnAnalysisComplete_Processor(this);
        //}

        //=====================================================================
        /// <summary>
        /// DoAnalysis
        /// </summary>
        public void DoAnalysisBlocking()
        {
            AnalysisStartTime = DateTime.Now;
            Busy = true;
            Logger.WriteLine("Pallet::DoAnalysis START");

            if(OriginalCB == null)
            {
                MCSFrame = new MCSFrame();
                MCSFrame.Load(Filename);
                
                MCSStitcher Stitcher = new MCSStitcher();
                Stitcher.StitchFrame(MCSFrame);


                if (ParamStorage.GetInt("MCS Reverse Conveyor Detect Enabled") != 0)
                {
                    if (Stitcher.IsReverseMotionCutoffBadPalletYuck(MCSFrame))
                        return;
                }


                OriginalCB = MCSFrame.RangeCB;
                ReflectanceCB = MCSFrame.ReflectanceCB;
            }


            //if (OriginalCB == null)
            //{
            //    OriginalCB = new CaptureBuffer();
            //    OriginalCB.Load(Filename);
            //}

            //if (ReflectanceCB == null)
            //{
            //    string reflFilename = Filename.Replace("_rng.r3", "_rfl.r3");
            //    if (File.Exists(reflFilename))
            //    {
            //        ReflectanceCB = new CaptureBuffer();
            //        ReflectanceCB.PaletteType = CaptureBuffer.PaletteTypes.Gray;
            //        ReflectanceCB.Load(reflFilename);
            //    }
            //}

            //if(false)
            //{
            //    if (Original != null) Original.FlipTopBottom();
            //    if (ReflectanceCB != null) ReflectanceCB.FlipTopBottom();
            //}

            if (ParamStorage.GetPixY("MCS Display Individual Ruler CBs") == 1)
            {
                for (int i = 0; i < MCSFrame.RulerRangeCBs.Count; i++)
                {
                    AddCaptureBuffer("Ruler" + (i + 1).ToString() + " Rng", MCSFrame.RulerRangeCBs[i]);
                    AddCaptureBuffer("Ruler" + (i + 1).ToString() + " Rfl", MCSFrame.RulerReflectanceCBs[i]);
                }
            }

            AddCaptureBuffer("Image", ReflectanceCB);

            Logger.WriteLine(String.Format("DoAnalysisBlocking: Original W,H  {0}  {1}", OriginalCB.Width, OriginalCB.Height));
            AddCaptureBuffer("Original", OriginalCB);


            Denoised = DeNoise(OriginalCB);
            if (Denoised == null)
            {
                Logger.WriteLine("DeNoise() returned a NULL capturebuffer!");
                return;
            }

            Logger.WriteLine(String.Format("DoAnalysisBlocking: Denoised W,H  {0}  {1}", Denoised.Width, Denoised.Height));
            //AddCaptureBuffer("DenoisedA", Denoised);

            IsolateAndAnalyzeSurfaces(Denoised);

            //throw new Exception("BOOOM -- HACK.");

            if ( State == InspectionState.Fail )
            {
                Logger.WriteLine("Pallet::DoAnalysis INSPECTION FAILED");
                AnalysisStopTime = DateTime.Now;
                AnalysisTotalSec = (AnalysisStopTime - AnalysisStartTime).TotalSeconds;
                return;
            }

            // Final board size sanity check
            int ExpWidH  = (int)ParamStorage.GetPixY("H Board Width (in)");
            int ExpWidV1 = (int)ParamStorage.GetPixX("V Board Width (in)");
            int ExpWidV2 = (int)ParamStorage.GetPixX("V Board Width (in)");


            // Check for debris causing unusually wide boards
            // TODO Hack enable detecting debris causing unusually wide boards
            if (true)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (BList[i].AllDefects.Count == 0)
                        continue;

                    int H_Wid = BList[i].BoundsP2.Y - BList[i].BoundsP1.Y;
                    if (H_Wid > (ExpWidH * 1.3))
                    {
                        AddDefect(BList[i], PalletDefect.DefectType.possible_debris, "Unusually wide board - Possible Debris");
                        SetDefectMarker(BList[i]);
                    }
                }

                if (BList[3].AllDefects.Count > 0)
                {
                    int V1_Wid = BList[3].BoundsP2.X - BList[3].BoundsP1.X;
                    if (V1_Wid > (ExpWidV1 * 1.3))
                    {
                        AddDefect(BList[3], PalletDefect.DefectType.possible_debris, "Unusually wide board - Possible Debris");
                        SetDefectMarker(BList[3]);
                    }
                }

                if (BList[4].AllDefects.Count > 0)
                {
                    int V1_Wid = BList[4].BoundsP2.X - BList[4].BoundsP1.X;
                    if (V1_Wid > (ExpWidV1 * 1.3))
                    {
                        AddDefect(BList[4], PalletDefect.DefectType.possible_debris, "Unusually wide board - Possible Debris");
                        SetDefectMarker(BList[4]);
                    }
                }
            }


            if (AllDefects.Count > 0)
                CheckForInteriorDebris();


            State = AllDefects.Count > 0 ? InspectionState.Fail : InspectionState.Pass;


            Benchmark.Stop("Total");

            string Res = Benchmark.Report();





            //System.Windows.MessageBox.Show(Res);

            //int nDefects = 0;
            //for (int i = 0; i < BList.Count; i++)
            //{
            //    StringBuilder SB = new StringBuilder();

            //    foreach (PalletDefect BD in BList[i].AllDefects)
            //    {
            //        string DefectName = BD.Name.ToString();
            //        DefectName = DefectName.Replace('_', ' ');
            //        DefectName = DefectName.ToUpper();
            //        SB.AppendLine(DefectName);
            //        //MainWindow.WriteLine("[" + BList[i].BoardName + "] " + DefectName);
            //        nDefects++;
            //    }

            //    string Def = SB.ToString();
            //    if ( !string.IsNullOrEmpty(Def) )
            //    {
            //        int CX = (BList[i].BoundsP1.X + BList[i].BoundsP2.X) / 2;
            //        int CY = (BList[i].BoundsP1.Y + BList[i].BoundsP2.Y) / 2;
            //        System.Windows.Point C = new System.Windows.Point(CX, CY);
            //        int W = Math.Abs(BList[i].BoundsP1.X - BList[i].BoundsP2.X);
            //        int H = Math.Abs(BList[i].BoundsP1.Y - BList[i].BoundsP2.Y); 

            //        //Original.AddMarker_Rect(C, W, H, Def);
            //        //Denoised.AddMarker_Rect(C, W, H, Def);
            //        //Original.MarkDefect(C, W, H, Def);
            //        //Denoised.MarkDefect(C, W, H, Def);

            //    }
            //}


            //MainWindow.SetResult(nDefects == 0 ? "P" : "F", nDefects == 0 ? Color.Green : Color.Red);



            //MainWindow.WriteLine(string.Format("Analysis took {0} ms\n\n", (Stop - Start).TotalMilliseconds));
            Busy = false;
            AnalysisStopTime = DateTime.Now;
            AnalysisTotalSec = (AnalysisStopTime - AnalysisStartTime).TotalSeconds;
            Logger.WriteLine(String.Format("Pallet::DoAnalysis FINISHED  -  {0:0.000} sec", AnalysisTotalSec));

        }

        //=====================================================================




        //=====================================================================
        private void CheckForInteriorDebris()
        {
            // Check inside top area
            int PixCount = 0;
            int TotalCount = 0;
            for ( int x = BList[0].BoundsP1.X + 50; x < BList[1].BoundsP2.X-50; x+=4 )
            {
                for ( int y = BList[0].BoundsP2.Y + 50; y < BList[1].BoundsP1.Y - 50; y+=4)
                {
                    UInt16 V = Denoised.Buf[y * Denoised.Width + x];
                    if (V > 0)
                        PixCount++;
                    TotalCount++;
                }
            }
            if ( (PixCount/((float)TotalCount)) > 0.02f )
            {
                AddDefect(BList[0], PalletDefect.DefectType.possible_debris,"Debris detected between H1 and H2");
                SetDefectMarker(BList[0].BoundsP1.X + 50, BList[0].BoundsP2.Y + 50, BList[1].BoundsP2.X - 50, BList[1].BoundsP1.Y - 50);
            }

            PixCount = 0;
            TotalCount = 0;
            for (int x = BList[1].BoundsP1.X + 50; x < BList[2].BoundsP2.X - 50; x+=4)
            {
                for (int y = BList[1].BoundsP2.Y + 50; y < BList[2].BoundsP1.Y - 50; y+=4)
                {
                    UInt16 V = Denoised.Buf[y * Denoised.Width + x];
                    if (V > 0)
                        PixCount++;
                    TotalCount++;
                }
            }
            if ((PixCount / ((float)TotalCount)) > 0.02f)
            {
                AddDefect(BList[1], PalletDefect.DefectType.possible_debris, "Debris detected between H2 and H3");
                SetDefectMarker(BList[1].BoundsP1.X + 50, BList[1].BoundsP2.Y + 50, BList[2].BoundsP2.X - 50, BList[2].BoundsP1.Y - 50);
            }


        }

        //=====================================================================
        private CaptureBuffer DeNoise(CaptureBuffer SourceCB)
        {
            ushort[] Source = SourceCB.Buf;

            CaptureBuffer CB = new CaptureBuffer(SourceCB);
            CB.PaletteType = CaptureBuffer.PaletteTypes.Baselined;
            ushort[] Res = CB.Buf;


            // Apply ROI Values
            int leftEdge = ParamStorage.GetInt("Raw Capture ROI Left (px)");
            int rightEdge = ParamStorage.GetInt("Raw Capture ROI Right (px)");
            int clipMin = ParamStorage.GetInt("Raw Capture ROI Min Z (px)");
            int clipMax = ParamStorage.GetInt("Raw Capture ROI Max Z (px)");
            int filterSize = ParamStorage.GetInt("Raw Capture Noise Filter Size (px)");

            if (true)
            {
                int CBH = SourceCB.Height;
                int CBW = SourceCB.Width;
                for (int y = 0; y < CBH; y++)
                {
                    int yoffset = y * CBW;
                    for (int x = 0; x < leftEdge; x++) Res[yoffset+x] = 0;
                    for (int x = rightEdge+1; x < CBW; x++) Res[yoffset + x] = 0;

                    for (int x = leftEdge; x <=rightEdge; x++)
                    {
                        int i = yoffset + x;
                        int v = Res[i];
                        if ((v!=0) && ((v > clipMax) || (v < clipMin))) Res[i] = 0;
                    }
                }
            }


            // Filter out small segments
            if(filterSize!=0)
            {
                byte[] DustMask = new byte[CB.Buf.Length];
                int CBH = CB.Height;
                int CBW = CB.Width;

                for (int pass = 0; pass < 3; pass++)
                {
                    // tag short horiz strands
                    for (int y = 0; y < CBH; y++)
                    {
                        int yoffset = y * CBW;
                        int start = -1;

                        for (int x = 0; x < CBW; x++)
                        {
                            if (Res[yoffset + x] != 0)
                            {
                                if (start == -1)
                                    start = x;
                            }
                            else
                            {
                                if (start != -1)
                                {
                                    int end = x - 1;
                                    if ((end - start + 1) <= filterSize)
                                    {
                                        for (int i = start; i <= end; i++)
                                            DustMask[yoffset + i] += 1;
                                    }
                                    start = -1;
                                }
                            }
                        }
                    }

                    // tag short vert strands
                    for (int x = 0; x < CBW; x++)
                    {
                        int start = -1;

                        for (int y = 0; y < CBH; y++)
                        {
                            int yoffset = y * CBW;
                            if (Res[yoffset + x] != 0)
                            {
                                if (start == -1) start = y;
                            }
                            else
                            {
                                if (start != -1)
                                {
                                    int end = y - 1;
                                    if ((end - start + 1) <= filterSize)
                                    {
                                        for (int i = start; i <= end; i++)
                                            DustMask[i * CBW + x] += 1;
                                    }
                                    start = -1;
                                }
                            }
                        }
                    }

                    // Clear masked pixels
                    // tag short horiz strands
                    for (int y = 0; y < CBH; y++)
                    {
                        int yoffset = y * CBW;
                        for (int x = 0; x < CBW; x++)
                        {
                            if (DustMask[yoffset + x] > 0)
                                Res[yoffset + x] = 0;
                        }
                    }
                }
            }

            // Apply Baseline
            if (true)
            {
                ushort[] BaselineData = ParamStorage.GetArray("BaselineData");

                int CBH = SourceCB.Height;
                int CBW = SourceCB.Width;
                int BaselineDataLength = BaselineData.Length;
                int maxX = Math.Min(BaselineDataLength, CBW);
                for (int y = 0; y < CBH; y++)
                {
                    int yoffset = y * CBW;
                    for (int x = 0; x < maxX; x++)
                    {
                        int V = Res[yoffset + x];
                        if(V!=0)
                        {
                            if (BaselineData[x]!=0)
                                V = (BaselineZ + V) - BaselineData[x];

                            Res[yoffset + x] = (ushort)V;  
                        }
                    }
                }
            }

            if (true)
            {
                int topY = FindTopY(CB);
                for (int y = 0; y < topY; y++)
                    for (int x = 0; x < CB.Width; x++)
                    {
                        CB.Buf[y * CB.Width + x] = 0;
                    }
            }

            AddCaptureBuffer("Filtered", CB);
            return CB;
        }

        //=====================================================================
        void AddCaptureBuffer(string Name, CaptureBuffer CB)
        {
            CB.Name = Name;
            CBDict.Add(Name, CB);
            CBList.Add(CB);
        }


        //=====================================================================
        //void AddCaptureBuffer(string Name, UInt16[] Buf, out CaptureBufferBrowser CBB )
        //{
        //    CaptureBufferBrowser CB = (CaptureBufferBrowser)
        //        MainWindow.Singleton.Dispatcher.Invoke(new addBufferCB(MainWindow.AddCaptureBufferBrowser), new object[] { Name, Buf });

        //    CBB = CB;

        //    return CB.Buf;
        //}

        //=====================================================================
        private void DeNoiseInPlace(CaptureBuffer CB)
        {
            UInt16[] Source = CB.Buf;
            UInt16[] Res = (UInt16[])CB.Buf.Clone();

            int leftEdge = ParamStorage.GetInt("Raw Capture ROI Left (px)");
            int rightEdge = ParamStorage.GetInt("Raw Capture ROI Right (px)");

            for (int i = 1; i < Res.Length - 1; i++)
            {
                int x = i % CB.Width;
                if ((x <= leftEdge) || (x >= rightEdge))
                {
                    Res[i] = 0;
                    continue;
                }

                if (Source[i] != 0)
                {
                    if ((Source[i - 1] == 0) &&
                        (Source[i + 1] == 0))
                        Res[i] = 0;
                    else
                        Res[i] = Source[i];
                }
                else
                {
                    if ((Source[i - 1] != 0) &&
                        (Source[i + 1] != 0))
                        Res[i] = (UInt16)((Source[i - 1] + (Source[i + 1])) / 2);
                }
            }

            for (int i = 0; i < Res.Length; i++)
                Source[i] = Res[i];
        }


        //=====================================================================
        private int FindTopY(CaptureBuffer SourceCB)
        {
            //
            // !!! Needs to work with Original and Denoised Buffers
            //

            UInt16[] Source = SourceCB.Buf;
            int LROI = ParamStorage.GetInt("Raw Capture ROI Left (px)");
            int RROI = ParamStorage.GetInt("Raw Capture ROI Right (px)");
            //int MinZTh = ParamStorage.GetInt("Raw Capture ROI Min Z (px)");
            //int MaxZTh = ParamStorage.GetInt("Raw Capture ROI Max Z (px)");

            int[] Hist = new int[200];

            int ylimit = Math.Min(1999,(int)(SourceCB.Height / 2));
            for (int x = LROI; x < RROI; x += 10)
            {
                for (int y = 0; y < ylimit; y += 10)
                {
                    UInt16 Val = Source[y * SourceCB.Width + x];
                    if (Val!=0)
                    {
                        Hist[y / 10]++;
                        break;
                    }
                }
            }

            int HighVal = 0;
            int HighIndex = -1;

            for (int i = 0; i < Hist.Length; i++)
            {
                if (Hist[i] > HighVal)
                {
                    HighVal = Hist[i];
                    HighIndex = i;
                }
            }

            int InitialY = HighIndex * 10;

            // Check upward and see if there is a row completely black
            for(int y= InitialY; y>0; y--)
            {
                int HitCount = 0;
                bool Clear = true;
                for (int x = 0; x < SourceCB.Width; x+=8)
                {
                    UInt16 Val = Source[y * SourceCB.Width + x];
                    if (Val != 0)
                    {
                        HitCount += 1;
                        if (HitCount > 10)
                        {
                            Clear = false;
                            break;
                        }
                    }
                }
                if (Clear)
                {
                    return y;
                }
            }
            return Math.Max(0,InitialY-px100XYIn);
        }

        //=====================================================================
        private void IsolateAndAnalyzeSurfaces(CaptureBuffer SourceCB)
        {
            int nRows = SourceCB.Height;
            int nCols = SourceCB.Width;

            int LROI = ParamStorage.GetInt("Raw Capture ROI Left (px)");
            int RROI = ParamStorage.GetInt("Raw Capture ROI Right (px)");
            int LWid = ParamStorage.GetPixX("V Board Width (in)");
            int RWid = ParamStorage.GetPixX("V Board Width (in)");
            int HBoardWid = ParamStorage.GetPixY("H Board Width (in)");
            int LBL = ParamStorage.GetPixY("V Board Length (in)");

            //UInt16[] Isolated = (UInt16[])(SourceCB.Buf.Clone());

            int SX = LROI + (LWid / 2);
            int EX = RROI - (RWid / 2);

            int TopY = FindTopY(SourceCB);

            BList = new List<Board>();

            if (TopY < 0)
            {
                AddDefect(null, PalletDefect.DefectType.board_segmentation_error, "Could not lock onto top board");
                State = InspectionState.Fail;
                return;
            }

            int ppi = (int)(25.4f/ParamStorage.GetFloat("MM Per Pixel Y"));

            Logger.WriteLine(string.Format("IsolateAndAnalyzeSurfaces:"));
            Logger.WriteLine(string.Format("LWid:{0}  RWid:{1}", LWid, RWid));
            Logger.WriteLine(string.Format("SX:{0}  EX:{1}", SX, EX));
            Logger.WriteLine(string.Format("LBL:{0}  ppi:{1}  TopY:{2}", LBL, ppi, TopY));

            int H1_T = Math.Max(0, TopY-20);// - 100);
            int H1_B = TopY + HBoardWid + ppi*3;
            Logger.WriteLine(string.Format("TopY:{0}  H1_T:{1}  H1_B:{2}", TopY, H1_T, H1_B));

            int H2_T = TopY + (LBL / 2) - ppi * 6;
            int H2_B = TopY + (LBL / 2) + ppi * 6;
            Logger.WriteLine(string.Format("ISOLATE H2_T,H2_B {0} {1}", H2_T, H2_B));

            int H3_T = TopY + LBL - HBoardWid - ppi * 3;
            int H3_B = TopY + LBL + ppi * 2;
            Logger.WriteLine(string.Format("ISOLATE H3_T,H3_B {0} {1}", H3_T, H3_B));

            H3_B = Math.Min(SourceCB.Height - 1, H3_B);

            // The ProbeCB gets modified as boards are found and subtracted...so need a unique copy
            CaptureBuffer ProbeCB = new CaptureBuffer(SourceCB);
            Board H1 = ProbeVertically(ProbeCB, "H1", PalletDefect.DefectLocation.H1, SX, EX, 1, H1_T, H1_B);
            Board H2 = ProbeVertically(ProbeCB, "H2", PalletDefect.DefectLocation.H2, SX, EX, 1, H2_T, H2_B);
            Board H3 = ProbeVertically(ProbeCB, "H3", PalletDefect.DefectLocation.H3, SX, EX, 1, H3_T, H3_B);
            Board V1 = ProbeHorizontally(ProbeCB, "V1", PalletDefect.DefectLocation.V1,H1_T, H3_B, 1, LROI, LROI + (int)(LWid * 2));
            Board V2 = ProbeHorizontally(ProbeCB, "V2", PalletDefect.DefectLocation.V2,H1_T, H3_B, 1, RROI, RROI - (int)(RWid * 2));

            BList.Add(H1);
            BList.Add(H2);
            BList.Add(H3);
            BList.Add(V1);
            BList.Add(V2);


            // Sanity check edges
            for ( int i = 0; i < 3; i++ )
            {
                Board B = BList[i];

                if ( B.Edges[0].Count < 30 )
                {
                    AddDefect(null, PalletDefect.DefectType.board_segmentation_error, "Board segmentation error on " + B.BoardName);
                    return;
                }

                for ( int j = 30; j >= 0; j-- )
                {
                    if ( (B.Edges[1][j].Y - B.Edges[0][j].Y) > ((B.Edges[1][j+1].Y - B.Edges[0][j+1].Y)*1.2f) )
                    {
                        B.Edges[0][j] = B.Edges[0][j + 1];
                        B.Edges[1][j] = B.Edges[1][j + 1];
                    }
                }

                for (int j = 30; j > 0; j--)
                {
                    int w = B.Edges[0].Count;
                    if ((B.Edges[1][w-j].Y - B.Edges[0][w-j].Y) > ((B.Edges[1][w-j-1].Y - B.Edges[0][w-j-1].Y) * 1.2f))
                    {
                        B.Edges[0][w-j] = B.Edges[0][w-j-1];
                        B.Edges[1][w-j] = B.Edges[1][w-j-1];
                    }
                }
            }


            if(false) // TODO HACK Turn on Board Threads
            {
                // RUN BOARD ANALYSIS IN SERIAL
                for (int i = 0; i < BList.Count; i++)
                    ProcessBoard(BList[i]);

            }
            else
            {
                // RUN BOARD ANALYSIS IN THREADS

                // Analyze the boards
                if (BoardThreads == null)
                    BoardThreads = new Thread[5];

                for (int i = 0; i < BList.Count; i++)
                {
                    if (BoardThreads[i] == null)
                        BoardThreads[i] = new Thread(ProcessBoard);

                    BoardThreads[i].Start(BList[i]);
                }

                bool AllDone = false;

                while (!AllDone)
                {
                    Thread.Sleep(1);
                    AllDone = true;
                    for (int i = 0; i < BoardThreads.Length; i++)
                    {
                        if (BoardThreads[i].ThreadState != ThreadState.Stopped)
                        {
                            AllDone = false;
                            break;
                        }
                    }
                }

            }


            // TODO: HACK: Turn on Crack Analysis
            ////for (int i = 0; i < BList.Count; i++)
            ////{
            ////    Board B = BList[i];

            ////    CaptureBuffer CB = new CaptureBuffer(BList[i].CrackCB);
            ////    AddCaptureBuffer(B.BoardName + " Crack M", CB);

            ////    // Draw the cracks
            ////    int ny = B.CrackTracker.GetLength(0);
            ////    int nx = B.CrackTracker.GetLength(1);
            ////    float MaxVal = B.CrackBlockSize * B.CrackBlockSize;
            ////    for (int y = 0; y < ny; y++)
            ////    {
            ////        for (int x = 0; x < nx; x++)
            ////        {
            ////            if (B.CrackTracker[y, x] != 0)
            ////            {
            ////                int V = B.CrackTracker[y, x];
            ////                int x1 = B.BoundsP1.X + (x * B.CrackBlockSize);
            ////                int y1 = B.BoundsP1.Y + (y * B.CrackBlockSize);

            ////                float pct = B.CrackTracker[y, x] / MaxVal;
            ////                //if (pct > BlockCrackMinPct)
            ////                {
            ////                    //DrawBlock(B.CrackBuf, x1, y1, x1 + B.CrackBlockSize, y1 + B.CrackBlockSize, 16 );

            ////                    // TODO-UNCOMMENT
            ////                    //MakeBlock(B.CrackCB, x1 + 8, y1 + 8, 16, (UInt16)(B.CrackTracker[y, x] * 100));
            ////                    MakeBlock(B.CrackCB, x1 + 8, y1 + 8, 16, (UInt16)(B.CrackTracker[y, x] * 100));
            ////                }
            ////            }

            ////            if (B.BoundaryBlocks[y, x] != 0)
            ////            {
            ////                int x1 = B.BoundsP1.X + (x * B.CrackBlockSize);
            ////                int y1 = B.BoundsP1.Y + (y * B.CrackBlockSize);

            ////                //DrawBlock(B.CrackCB, x1, y1, x1 + B.CrackBlockSize, y1 + B.CrackBlockSize, 5500);
            ////                MakeBlock(B.CrackCB, x1 + 8, y1 + 8, 16, 5500);
            ////            }

            ////        }
            ////    }

            ////    AddCaptureBuffer(B.BoardName + " Crack T", BList[i].CrackCB);
            ////}
        }

        //=====================================================================
        //private void ClearAboveH1(ushort[] isolated, Board h1)
        //{
        //    int minY = int.MaxValue;
        //    //B.Edges[0][i].Y - B.Edges[1][i].Y

        //    if ((h1.Edges[0].Count == 0) || (h1.Edges[1].Count == 0))
        //        return;


        //    for (int x = 0; x < h1.Edges.Length; x++)
        //    {
        //        if (h1.Edges[0][x].Y < minY)
        //            minY = h1.Edges[0][x].Y;
        //    }

        //    for (int y = 0; y < minY; y++)
        //    {
        //        for (int x = 0; x < MainWindow.SensorWidth; x++)
        //            isolated[y * MainWindow.SensorWidth + x] = 0;
        //    }
        //}

        //=====================================================================
        private void DrawBlock(CaptureBuffer CB, int x1, int y1, int x2, int y2, UInt16 Val)
        {
            UInt16[] buf = CB.Buf;
            int W = CB.Width;


            try
            {
                for (int y = y1; y <= y2; y++)
                {
                    //V = buf[y * W + x1];
                    //if (V < 32) buf[y * W + x1] = (UInt16)(Val);
                    buf[y * W + x1] = (UInt16)(Val);

                    //V = buf[y * W + x2];
                    //if (V < 32) buf[y * W + x2] = (UInt16)(Val);
                    buf[y * W + x2] = (UInt16)(Val);
                }
                for (int x = x1; x <= x2; x++)
                {
                    //V = buf[y1 * W + x];
                    //if (V < 32) buf[y1 * W + x] = (UInt16)(Val);
                    buf[y1 * W + x] = (UInt16)(Val);

                    //V = buf[y2 * W + x];
                    //if (V < 32) buf[y2 * W + x] = (UInt16)(Val);
                    buf[y2 * W + x] = (UInt16)(Val);
                }
            }
            catch
            {
            }
        }

        //=====================================================================
        private void ProcessBoard(object _B)
        {
            Board B = (Board)_B;

            if (UseBackgroundThread)
            {
                ThreadPriority TPBackup = Thread.CurrentThread.Priority;
                //Logger.WriteLine("ProcessBoard PRIORITY: " + TPBackup.ToString());
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            }

            try
            {

                // Setup basic board bounds
                PalletPoint P1 = FindBoardMinXY(B);
                PalletPoint P2 = FindBoardMaxXY(B);
                P1.X--;
                P1.Y--;
                P2.X++;
                P2.Y++;
                B.BoundsP1 = P1;
                B.BoundsP2 = P2;


                if (ParamStorage.GetInt("Defect Detection Enabled") != 0)
                {
                    if (ParamStorage.GetInt("Crack Detection Enabled") != 0)
                        FindCracks(B);

                    FindRaisedBoard(B);
                    FindRaisedNails(B);
                    CalculateMissingWood(B);
                    CheckForBreaks(B);

                    // Any narrow boards?
                    float FailWidIn = ParamStorage.GetFloat("(BN) Narrow Board Minimum Width (in)");
                    float MeasuredWidthIn = 0;
                    float MeasuredLenIn = 0;
                    bool TooNarrow = CheckNarrowBoard(B, FailWidIn, 0.5f, true, ref MeasuredWidthIn, ref MeasuredLenIn);
                    if (TooNarrow)
                    {
                        AddDefect(B, PalletDefect.DefectType.board_too_narrow, string.Format("Min width {0:0.00} X {1:0.00} in. was less than {2:0.00} in", MeasuredWidthIn, MeasuredLenIn,FailWidIn.ToString()));
                        SetDefectMarker(B);
                    }

                    FailWidIn = ParamStorage.GetFloat("(BN) Missing Chunk Minimum Width (in)");
                    float MinLengthIn = ParamStorage.GetFloat("(BN) Missing Chunk Minimum Length (in)");
                    MeasuredWidthIn = 0;
                    MeasuredLenIn = 0;
                    bool MissingWoodChunk = CheckNarrowBoard(B, FailWidIn, MinLengthIn, true, ref MeasuredWidthIn, ref MeasuredLenIn);
                    if (MissingWoodChunk)
                    {
                        AddDefect(B, PalletDefect.DefectType.missing_wood, string.Format("Missing Chunk {0:0.00} X {1:0.00} in.", MeasuredWidthIn, MeasuredLenIn));
                        SetDefectMarker(B);
                    }
                }

            }
            catch (Exception E)
            {
                Logger.WriteException(E);
                AddDefect(B, PalletDefect.DefectType.board_segmentation_error,"Exception thrown in ProcessBoard()");
                return;
            }

        }

        //=====================================================================

        

        private bool CheckNarrowBoard(Board B, float FailWidIn, float MinMissingWoodLengthIn, bool ExcludeEnds, ref float MeasuredWidth, ref float MeasuredLength)
        {
            MeasuredWidth = 0;
            MeasuredLength = 0;
            double AvgWidth = 0;
            double AvgWidthCount = 0;


            int nBadEdges = 0;
            int Exclusion = ExcludeEnds ? px100XYIn : 0;

            // Horizontal board
            if (B.IsHoriz)
            {
                int FailWidPx = (int)(FailWidIn * px100XYIn);

                for (int i = Exclusion; i < B.Edges[0].Count - Exclusion; i++)
                {
                    int DY = B.Edges[1][i].Y - B.Edges[0][i].Y;

                    if (DY < FailWidPx)
                    {
                        AvgWidth += DY;
                        AvgWidthCount += 1;
                        nBadEdges++;
                    }
                }
                MeasuredLength = nBadEdges / px100XYIn;
            }
            else
            {
                int FailWidPx = (int)(FailWidIn * px100XYIn);

                for (int i = Exclusion; i < B.Edges[0].Count-Exclusion; i++)
                {
                    int DX = B.Edges[1][i].X - B.Edges[0][i].X;

                    if (DX < FailWidPx)
                    {
                        AvgWidth += DX;
                        AvgWidthCount += 1;
                        nBadEdges++;
                    }

                }

                MeasuredLength = nBadEdges / px100XYIn;
            }

            if(AvgWidthCount>0) AvgWidth = Math.Round((AvgWidth/AvgWidthCount)/(float)px100XYIn, 2);
            MeasuredWidth = (float)AvgWidth;

            return (MeasuredLength > MinMissingWoodLengthIn);
        }

        //=====================================================================
        private void FindCracks(Board B)
        {
            if (B.Edges[0].Count < ((6.0*25.4)/ParamStorage.GetFloat("MM Per Pixel X")))
            {
                AddDefect(B, PalletDefect.DefectType.missing_board, "Can't locate board");
                SetDefectMarker(B);
                return;
            }

            PalletPoint P1 = B.BoundsP1;
            PalletPoint P2 = B.BoundsP2;

            B.CrackCB = new CaptureBuffer(B.CB.Width,B.CB.Height);
            B.CrackBlockSize = ParamStorage.GetPixX("Crack Tracker Block Size (in)");
            int CBW = B.CB.Width;
            int CBH = B.CB.Height;

            int MinZDeltaThreshold = ParamStorage.GetPixZ("Crack Tracker Min Delta (in)");

            // Divide into blocks
            int nx = ((P2.X - P1.X) / B.CrackBlockSize) + 1;
            int ny = ((P2.Y - P1.Y) / B.CrackBlockSize) + 1;
            B.CrackTracker = new int[ny, nx];
            B.BoundaryBlocks = new int[ny, nx];

            ushort[,] BlockMaxs = new ushort[ny, nx];
            ushort[,] BlockMins = new ushort[ny, nx];

            Logger.WriteLine(string.Format("FindCracks   blocksize:{0}   nx:{1}   ny:{2}", B.CrackBlockSize, nx, ny));

            try
            {
                int CrackBlockSizeExtra = (int)(B.CrackBlockSize * 0.125f);
                ushort[] Buf = B.CB.Buf;
                for (int cy = 1; cy < ny-1; cy++)
                {
                    for (int cx = 1; cx < nx - 1; cx++)
                    {
                        int y0 = cy * B.CrackBlockSize + P1.Y;
                        int x0 = cx * B.CrackBlockSize + P1.X;
                        int y1 = y0 + B.CrackBlockSize - 1;
                        int x1 = x0 + B.CrackBlockSize - 1;

                        //ushort MinV = +32000;
                        //ushort MaxV = 1;
                        int MaxZDelta = 0;

                        int sry0 = Math.Max(0, y0 - CrackBlockSizeExtra);
                        int srx0 = Math.Max(0, x0 - CrackBlockSizeExtra);
                        int sry1 = Math.Min(CBH - 1, y1 + CrackBlockSizeExtra);
                        int srx1 = Math.Min(CBW - 1, x1 + CrackBlockSizeExtra);

                        for (int y = sry0; y <= sry1; y += 3)
                        {
                            for (int x = srx0; x <= srx1; x += 3)
                            {
                                int V0 = Buf[y * CBW + x];
                                if (V0 != 0)
                                {
                                    int V1 = Buf[y * CBW + x - 16];
                                    int V2 = Buf[y * CBW + x + 16];
                                    int V3 = Buf[(y - 16) * CBW + x];
                                    int V4 = Buf[(y + 16) * CBW + x];

                                    int dZ = 0;
                                    //if ((V1 != 0) && (V1 < BaselineZ)) dZ = Math.Max(dZ, Math.Abs(V1 - V0));
                                    //if ((V2 != 0) && (V2 < BaselineZ)) dZ = Math.Max(dZ, Math.Abs(V2 - V0));
                                    //if ((V3 != 0) && (V3 < BaselineZ)) dZ = Math.Max(dZ, Math.Abs(V3 - V0));
                                    //if ((V4 != 0) && (V4 < BaselineZ)) dZ = Math.Max(dZ, Math.Abs(V4 - V0));

                                    if ((V1 < BaselineZ)) dZ = Math.Max(dZ, Math.Abs(V1 - V0));
                                    if ((V2 < BaselineZ)) dZ = Math.Max(dZ, Math.Abs(V2 - V0));
                                    if ((V3 < BaselineZ)) dZ = Math.Max(dZ, Math.Abs(V3 - V0));
                                    if ((V4 < BaselineZ)) dZ = Math.Max(dZ, Math.Abs(V4 - V0));

                                    MaxZDelta = Math.Max(MaxZDelta, dZ);

                                    //if (V != 0)
                                    //{
                                    //    MinV = Math.Min(MinV, V);
                                    //    MaxV = Math.Max(MaxV, V);
                                    //}
                                }
                            }
                        }

                        if (MaxZDelta > 0)
                        {
                            B.BoundaryBlocks[cy, cx] = 1;
                            if (MaxZDelta > MinZDeltaThreshold)
                            {
                                for (int y = y0; y <= y1; y++)
                                {
                                    for (int x = x0; x <= x1; x++)
                                    {
                                        ushort BlockDelta = (ushort)(MaxZDelta + BaselineZ);
                                        B.CrackCB.Buf[y * CBW + x] = BlockDelta;
                                    }
                                }
                            }
                        }

                        //if (MinV != +32000)
                        //{
                        //    B.BoundaryBlocks[cy, cx] = 1;

                        //    if (MinV < BaselineZ)
                        //    {
                        //        ushort BlockDelta = (ushort)(MaxV - MinV);
                        //        if (BlockDelta > MinZDeltaThreshold)
                        //        {
                        //            BlockDelta = (ushort)(BlockDelta + BaselineZ);
                        //            for (int y = y0; y <= y1; y++)
                        //            {
                        //                for (int x = x0; x <= x1; x++)
                        //                {
                        //                    B.CrackCB.Buf[y * CBW + x] = BlockDelta;
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                    }
                }

            }
            catch (Exception)
            {
                return;
            }


            ClearCracksAroundBlockAreas(B);

            // Remove speckle
            //DeNoiseInPlace(B.CrackCB);

            //for (int y = P1.Y; y <= P2.Y; y++)
            //{
            //    for (int x = P1.X; x <= P2.X; x++)
            //    {
            //        UInt16 Val1 = B.CrackCB.Buf[y * BCBWidth + x];
            //        if (Val1 > 0)
            //        {
            //            int cx = (x - P1.X) / B.CrackBlockSize;
            //            int cy = (y - P1.Y) / B.CrackBlockSize;
            //            B.CrackTracker[cy, cx]++;
            //        }
            //    }
            //}

            for (int cy = 0; cy < ny; cy++)
            {
                for (int cx = 0; cx < nx; cx++)
                {
                    if (B.BoundaryBlocks[cy, cx] != 0)
                    {
                        int y0 = cy * B.CrackBlockSize + P1.Y;
                        int x0 = cx * B.CrackBlockSize + P1.X;

                        if (B.CrackCB.Buf[y0 * CBW + x0] != 0)
                            B.CrackTracker[cy, cx] = 1;
                    }
                }
            }



            // Clean up
            if (!B.IsHoriz)    // Vertical board
            {
                for (int x = 0; x < nx; x++)
                {
                    B.CrackTracker[0, x] = 0;
                    B.CrackTracker[ny - 1, x] = 0;
                    B.CrackTracker[1, x] = 0;
                    B.CrackTracker[ny - 2, x] = 0;
                }

                for (int y = 0; y < ny; y++)
                {
                    int sx = -1;
                    int ex = -1;
                    for (int x = 0; x < nx; x++)
                    {
                        if (B.BoundaryBlocks[y, x] == 1)
                        {
                            sx = x;
                            break;
                        }
                    }
                    for (int x = nx - 1; x >= 0; x--)
                    {
                        if (B.BoundaryBlocks[y, x] == 1)
                        {
                            ex = x;
                            break;
                        }
                    }

                    if ((sx != -1) && (ex != -1))
                    {

                        for (int i = sx + 1; i < ex; i++)
                        {
                            B.BoundaryBlocks[y, i] = 0;
                        }

                        B.CrackTracker[y, sx] = 0;
                        B.CrackTracker[y, ex] = 0;
                    }
                }
            }
            else // Horizontal board
            {
                for (int y = 0; y < ny; y++)
                {
                    B.CrackTracker[y, 0] = 0;
                    B.CrackTracker[y, nx - 1] = 0;
                    B.CrackTracker[y, 1] = 0;
                    B.CrackTracker[y, nx - 2] = 0;
                }

                for (int x = 0; x < nx; x++)
                {
                    int sy = -1;
                    int ey = -1;
                    for (int y = 0; y < ny; y++)
                    {
                        if (B.BoundaryBlocks[y, x] == 1)
                        {
                            sy = y;
                            break;
                        }
                    }
                    for (int y = ny - 1; y >= 0; y--)
                    {
                        if (B.BoundaryBlocks[y, x] == 1)
                        {
                            ey = y;
                            break;
                        }
                    }

                    if ((sy != -1) && (ey != -1))
                    {
                        for (int i = sy + 1; i < ey; i++)
                        {
                            B.BoundaryBlocks[i, x] = 0;
                        }

                        B.CrackTracker[sy, x] = 0;
                        B.CrackTracker[ey, x] = 0;
                    }
                }
            }

            // Cement the crack blocks based on the params
            //float BlockCrackMinPct = ParamStorage.GetFloat("Crack Tracker Min Pct");
            //float MaxVal = B.CrackBlockSize * B.CrackBlockSize;
            //for (int y = 0; y < ny; y++)
            //{
            //    for (int x = 0; x < nx; x++)
            //    {
            //        float pct = B.CrackTracker[y, x] / MaxVal;
            //        if (pct > BlockCrackMinPct)
            //            B.CrackTracker[y, x] = 1;
            //        else
            //            B.CrackTracker[y, x] = 0;
            //    }
            //}

            // Now flood fill each crack and measure 
            FloodFillCracks(B);

            // Now check to see if it's broken
            IsCrackABrokenBoard(B);



            // Overlay boundary blocks
            if (ParamStorage.GetInt("Display Crack CBs Enabled") != 0)
            {
                for (int cy = 1; cy < ny - 1; cy++)
                {
                    for (int cx = 1; cx < nx - 1; cx++)
                    {
                        int y0 = cy * B.CrackBlockSize + P1.Y;
                        int x0 = cx * B.CrackBlockSize + P1.X;
                        int y1 = y0 + B.CrackBlockSize - 1;
                        int x1 = x0 + B.CrackBlockSize - 1;
                        
                        if (B.CrackTracker[cy,cx]!=0)
                        {
                            B.CrackCB.DrawRectangle((ushort)BaselineZ, x0, y0, x1, y1, false);
                            B.CrackCB.DrawRectangle((ushort)BaselineZ, x0+1, y0+1, x1-1, y1-1, false);
                            B.CrackCB.DrawRectangle((ushort)BaselineZ, x0+2, y0+2, x1-2, y1-2, false);
                        }
                        else
                        if (B.BoundaryBlocks[cy, cx] != 0)
                        {
                            bool Fill = B.CrackCB.Buf[y0 * CBW + x0] == 0;
                            B.CrackCB.DrawRectangle((ushort)BaselineZ, x0, y0, x1, y1,Fill);
                        }
                    }
                }
                AddCaptureBuffer(B.BoardName + "_Cracks", B.CrackCB);
            }
        }

        //=====================================================================
        private void FloodFillCracks(Board B)
        {
            int FloodCode = 2;

            int w = B.CrackTracker.GetLength(1);
            int h = B.CrackTracker.GetLength(0);

            // Initialize all crack cells to '1' -- not yet flooded
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (B.CrackTracker[y, x] > 0)
                        B.CrackTracker[y, x] = 1;
                }
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (B.CrackTracker[y, x] == 1)
                    {
                        B.CrackTracker[y, x] = FloodCode++;
                        FloodFillCrack(B, x, y);
                    }
                }
            }

            //Console.WriteLine(B.BoardName + " has " + FloodCode.ToString() + " unique cracks");
        }

        //=====================================================================
        void ClearCracksAroundBlockAreas(Board B)
        {
            float EdgeWidH = ParamStorage.GetFloat("HBoard Edge Crack Exclusion Zone Percentage");
            float EdgeWidV = ParamStorage.GetFloat("VBoard Edge Crack Exclusion Zone Percentage");
            float CenWid = ParamStorage.GetFloat("Board Center Crack Exclusion Zone Percentage");
            // float CrackBlockValueThreshold = ParamStorage.GetFloat("Crack Block Value Threshold");

            int w = B.CrackTracker.GetLength(1);
            int h = B.CrackTracker.GetLength(0);
            int CBW = B.CrackCB.Width;
            int CBH = B.CrackCB.Height;

            PalletPoint P1 = B.BoundsP1;
            PalletPoint P2 = B.BoundsP2;

            if (B.IsHoriz)
            {
                float Len = ParamStorage.GetPixX("H Board Length (in)");

                int x1 = 0;// B.Edges[0][0].X;
                int x2 = B.Edges[0].Count;// B.Edges[0][B.Edges[0].Count - 1].X;
                int xc = (x1 + x2) / 2;

                int edge1 = (int)(x1 + (Len * EdgeWidH));
                int edge2 = (int)(xc - ((Len * CenWid) / 2));
                int edge3 = (int)(xc + ((Len * CenWid) / 2));
                int edge4 = (int)(x2 - (Len * EdgeWidH));

                for (int x = x1; x < edge1; x++)
                {
                    for (int y = B.BoundsP1.Y; y <= B.BoundsP2.Y; y++)
                    {
                        B.CrackCB.Buf[y * CBW + B.Edges[0][x].X] = 0;
                    }
                }
                for (int x = edge2; x <= edge3; x++)
                {
                    for (int y = B.BoundsP1.Y; y <= B.BoundsP2.Y; y++)
                    {
                        B.CrackCB.Buf[y * CBW + B.Edges[0][x].X] = 0;
                    }
                }
                for (int x = edge4; x < x2; x++)
                {
                    for (int y = B.BoundsP1.Y; y <= B.BoundsP2.Y; y++)
                    {
                        B.CrackCB.Buf[y * CBW + B.Edges[0][x].X] = 0;
                    }
                }
            }
            else
            {
                float Len = ParamStorage.GetPixY("V Board Length (in)");

                int y1 = 0;// B.Edges[0][0].X;
                int y2 = B.Edges[0].Count;// B.Edges[0][B.Edges[0].Count - 1].X;
                int yc = (y1 + y2) / 2;

                int edge1 = (int)(y1 + (Len * EdgeWidV));
                int edge2 = (int)(yc - ((Len * CenWid) / 2));
                int edge3 = (int)(yc + ((Len * CenWid) / 2));
                int edge4 = (int)(y2 - (Len * EdgeWidV));

                for (int y = y1; y < edge1; y++)
                {
                    for (int x = B.BoundsP1.X; x < B.BoundsP2.X; x++ )
                    {
                        B.CrackCB.Buf[(B.BoundsP1.Y + y) * CBW + x] = 0;
                    }
                }
                for (int y = edge2; y <= edge3; y++)
                {
                    for (int x = B.BoundsP1.X; x < B.BoundsP2.X; x++)
                    {
                        B.CrackCB.Buf[B.Edges[0][y].Y * CBW + x] = 0;
                    }
                }
                for (int y = edge4; y < y2; y++)
                {
                    for (int x = B.BoundsP1.X; x < B.BoundsP2.X; x++)
                    {
                        B.CrackCB.Buf[B.Edges[0][y].Y * CBW + x] = 0;
                    }
                }

            }
        }


        //=====================================================================
        void IsCrackABrokenBoard(Board B)
        {
            int BlockSize = ParamStorage.GetInt("Crack Tracker Block Size");

            //float MaxDebrisPc = ParamStorage.GetFloat("Crack Block Debris Check Pct");
            

            int w = B.CrackTracker.GetLength(1);
            int h = B.CrackTracker.GetLength(0);

            bool isHoriz = w > h;

            int[] TouchesEdge1 = new int[200];
            int[] TouchesEdge2 = new int[200];

            // New approach...look for cracks that connect edge to edge
            if (isHoriz)
            {
                // HACK
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (B.CrackTracker[y, x] > 0)
                        {
                            int cid = B.CrackTracker[y, x];

                            //if ((y == 0) || (y == 1) || (y == (B.CrackTracker.GetLength(0) - 1)) || (y == (B.CrackTracker.GetLength(0) - 2)))
                            if ((y == 0))
                                TouchesEdge1[cid] = y;
                            else
                            if ((y == (h - 1)))
                                TouchesEdge2[cid] = y;
                            else
                            {
                                if ((B.BoundaryBlocks[y - 1, x] == 1))// && (B.BoundaryBlocks[y + 1, x] == 0) && (B.BoundaryBlocks[y + 1, x] == 0))
                                    TouchesEdge1[cid] = y;

                                if ((B.BoundaryBlocks[y + 1, x] == 1))// && (B.BoundaryBlocks[y - 1, x] == 0) && (B.BoundaryBlocks[y - 1, x] == 0))
                                    TouchesEdge2[cid] = y;
                            }
                        }
                    }
                }
            }
            else
            {
                for (int y = 2; y < h-2; y++)
                {
                    for (int x = 2; x < w-2; x++)
                    {
                        if (B.CrackTracker[y, x] > 0)
                        {
                            int cid = B.CrackTracker[y, x];

                            //if ((x == 0) || (x == 1) || (x == (B.CrackTracker.GetLength(1) - 1)) || (x == (B.CrackTracker.GetLength(1) - 2)))
                            if ((x == 0))
                                TouchesEdge1[cid] = x;
                            else
                            if(x == (w - 1))
                                TouchesEdge2[cid] = x;
                            else
                            {
                                if ((B.BoundaryBlocks[y, x-1] == 1) || (B.BoundaryBlocks[y, x - 2] == 1))// && (B.BoundaryBlocks[y, x + 1] == 0) && (B.BoundaryBlocks[y, x + 1] == 0))
                                    TouchesEdge1[cid] = x;

                                if ((B.BoundaryBlocks[y, x+1] == 1) || (B.BoundaryBlocks[y, x + 2] == 1))// && (B.BoundaryBlocks[y, x - 1] == 0) && (B.BoundaryBlocks[y, x - 1] == 0))
                                    TouchesEdge2[cid] = x;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < TouchesEdge1.Length; i++)
            {
                if ((TouchesEdge1[i] != 0) && (TouchesEdge2[i] != 0))
                {
                    if (Math.Abs(TouchesEdge1[i] - TouchesEdge2[i]) > 5)
                    {
                        AddDefect(B, PalletDefect.DefectType.broken_across_width, String.Format("Broken across width"));
                        SetDefectMarker(B);
                        return;
                    }
                }
            }

            return;

            ////// i = crack_id
            ////for (int i = 2; i < 99; i++)
            ////{
            ////    int minx = int.MaxValue;
            ////    int miny = int.MaxValue;
            ////    int maxx = int.MinValue;
            ////    int maxy = int.MinValue;

            ////    for (int y = 0; y < h; y++)
            ////    {
            ////        for (int x = 0; x < w; x++)
            ////        {
            ////            if (B.CrackTracker[y, x] == i)
            ////            {
            ////                minx = Math.Min(minx, x);
            ////                maxx = Math.Max(maxx, x);
            ////                miny = Math.Min(miny, y);
            ////                maxy = Math.Max(maxy, y);
            ////            }
            ////        }
            ////    }

            ////    // No cracks with this id == we're done
            ////    if (minx == int.MaxValue)
            ////        break;                

            ////    if (isHoriz)
            ////    {
            ////        int sx = minx * BlockSize;
            ////        int ex = maxx * BlockSize;
            ////        int sy = miny * BlockSize;
            ////        int ey = maxy * BlockSize;
            ////        int ylen = (maxy - miny + 1) * BlockSize;

            ////        //// Quick debris check
            ////        //int HyperHighPix = 0;
            ////        //int TotalPix = 0;
            ////        //for (int x = sx; x <= ex; x++)
            ////        //{
            ////        //    int SX = B.BoundsP1.X;
            ////        //    int SY = B.BoundsP1.Y;
            ////        //    for ( int y = SY+sy; y <= (SY+ey); y++ )
            ////        //    {
            ////        //        TotalPix++;
            ////        //        if (B.CB.Buf[y * MainWindow.SensorWidth + (SX+x)] > 1100)
            ////        //            HyperHighPix++;
            ////        //    }
            ////        //}

            ////        for (int x = sx; x <= ex; x++)
            ////        {
            ////            int Wid = (int)((B.Edges[1][x].Y - B.Edges[0][x].Y) * MaxCrackWid);
            ////            if (ylen > Wid)
            ////            {
            ////                AddDefect(B, PalletDefect.DefectType.broken_across_width);

            ////                // Only now should I flag debris
            ////                //if ((((float)HyperHighPix) / TotalPix) > MaxDebrisPc)
            ////                //    AddDefect(B, PalletDefect.DefectType.possible_debris);
            ////            }
            ////        }
            ////    }
            ////    else
            ////    {
            ////        int sy = miny * BlockSize;
            ////        int ey = maxy * BlockSize;
            ////        int xlen = (maxx - minx + 1) * BlockSize;

            ////        for (int y = sy; y <= ey; y++)
            ////        {
            ////            int Wid = (int)((B.Edges[1][y].X - B.Edges[0][y].X) * MaxCrackWid);
            ////            if (xlen > Wid)
            ////            {
            ////                AddDefect(B, PalletDefect.DefectType.broken_across_width);
            ////            }
            ////        }
            ////    }
            ////}
        }


        //=====================================================================
        // Recursive flood fill
        private void FloodFillCrack(Board B, int x, int y)
        {
            int w = B.CrackTracker.GetLength(1);
            int h = B.CrackTracker.GetLength(0);

            // Keep in bounds
            if ((x < 0) || (x >= w) ||
                 (y < 0) || (y >= h))
                return;

            int floodCode = B.CrackTracker[y, x];

            for (int i = 0; i < 8; i++)
            {
                int tx = 0, ty = 0;

                switch (i)
                {
                    case 0: tx = x - 1; ty = y - 1; break;
                    case 1: tx = x - 0; ty = y - 1; break;
                    case 2: tx = x + 1; ty = y - 1; break;
                    case 3: tx = x - 1; ty = y - 0; break;
                    case 4: tx = x + 1; ty = y - 0; break;
                    case 5: tx = x - 1; ty = y + 1; break;
                    case 6: tx = x - 0; ty = y + 1; break;
                    case 7: tx = x + 1; ty = y + 1; break;
                }

                if ((tx < 0) || (tx >= w) ||
                     (ty < 0) || (ty >= h))
                    continue;

                if ((B.CrackTracker[ty, tx] > 0) && (B.CrackTracker[ty, tx] != floodCode))
                {
                    B.CrackTracker[ty, tx] = floodCode;
                    FloodFillCrack(B, tx, ty);
                }
            }
        }


        //=====================================================================
        private void CheckForBreaks(Board B)
        {

            if (B.Edges[0].Count < 5)
            {
                AddDefect(B, PalletDefect.DefectType.missing_board,"Segmented board far too short");
                SetDefectMarker(B);
                return;
            }

            float BoardLenPerc = ParamStorage.GetFloat("(SH) Min Board Len (%)") / 100.0f;

            // Horizontal orientation
            if (B.IsHoriz)
            {

                float LenTh = (float)Math.Round( ParamStorage.GetFloat("H Board Length (in)") * BoardLenPerc,4);
                int BoardLenPx = B.Edges[0][B.Edges[0].Count - 1].X - B.Edges[0][0].X;
                float MeasLen = (float)Math.Round( (float)(BoardLenPx / px100XYIn),4);

                //int Len = (int)ParamStorage.GetFloat("H Board Length (in)");
                //int MinLen = (int)(Len * BoardLenPerc);
                //int MeasLen = B.Edges[0][B.Edges[0].Count - 1].X - B.Edges[0][0].X;

                if (MeasLen < LenTh)
                {
                    AddDefect(B, PalletDefect.DefectType.board_too_short,string.Format("Board len too short {0:0.00} in", MeasLen));
                    SetDefectMarker(B);
                    return;
                }
            }
            else
            // Vertical orientation
            {
                float LenTh = (float)Math.Round(ParamStorage.GetFloat("V Board Length (in)") * BoardLenPerc, 4);
                int BoardLenPx = B.Edges[0][B.Edges[0].Count - 1].Y - B.Edges[0][0].Y;
                float MeasLen = (float)Math.Round((float)(BoardLenPx / px100XYIn), 4);

                //int Len = (int)ParamStorage.GetPixY("V Board Length (in)");
                //int MinLen = (int)(Len * BoardLenPerc);
                //int MeasLen = B.Edges[0][B.Edges[0].Count - 1].Y - B.Edges[0][0].Y;

                if (MeasLen < LenTh)
                {
                    AddDefect(B, PalletDefect.DefectType.board_too_short, string.Format("Board len too short {0:0.00} in", MeasLen));
                    SetDefectMarker(B);
                    return;
                }
            }
        }

        //=====================================================================
        private void CalculateMissingWood(Board B)
        {

            int TotalArea = 0;
            int CBW = B.CB.Width;
            int ExpectedAreaPix = 0;
            ushort[] Buf = B.CB.Buf;

            if (B.Edges[0].Count < 5)
            {
                B.MeasuredAreaPix = 0;
                return;
            }

            // Is Horizontal?
            if (B.IsHoriz)//B.Edges[0][0].X == B.Edges[1][0].X)
            {
                List<PalletPoint> E0s = B.Edges[0];
                List<PalletPoint> E1s = B.Edges[1];
                for (int i = 0; i < E0s.Count; i++)
                {
                    int y0 = E0s[i].Y;
                    int y1 = E1s[i].Y;
                    int x = E0s[i].X;
                    for (int y = y0; y <= y1; y++)
                    {
                        if (Buf[y * CBW + x] > 0) TotalArea++;
                    }
                }

                B.MeasuredAreaPix = TotalArea;
                ExpectedAreaPix = ParamStorage.GetPixX("H Board Width (in)") * ParamStorage.GetPixX("H Board Length (in)");
            }
            else
            {
                List<PalletPoint> E0s = B.Edges[0];
                List<PalletPoint> E1s = B.Edges[1];

                for (int i = 0; i < E0s.Count; i++)
                {
                    int x0 = E0s[i].X;
                    int x1 = E1s[i].X;
                    int yoffset = E0s[i].Y * CBW;
                    for (int x = x0; x <= x1; x++)
                    {
                        if (Buf[yoffset + x] > 0) TotalArea++;
                    }
                }
                B.MeasuredAreaPix = TotalArea;
                ExpectedAreaPix = ParamStorage.GetPixX("V Board Width (in)") * ParamStorage.GetPixX("V Board Length (in)");
            }

            // Percent intact
            float IntactWoodPct = (float)Math.Round(100.0f*Math.Min(1f, (float)B.MeasuredAreaPix / (float)ExpectedAreaPix),0);
            float MissingWoodPct = (int)(100 - IntactWoodPct);

            B.MissingWoodPercent = MissingWoodPct;

            //MainWindow.WriteLine(string.Format("Board {0} is {1:0.00}% intact", B.BoardName, Pct * 100));
            float Allowable = ParamStorage.GetFloat("(MW) Max Allowed Missing Wood (%)");
            if ((MissingWoodPct > Allowable))
            {
                AddDefect(B, PalletDefect.DefectType.missing_wood, "Missing too much wood. " + MissingWoodPct.ToString() + "%");
                SetDefectMarker(B);
            }
        }

        ////=====================================================================
        //private void FindRaisedBoard(Board B)
        //{
        //    int W1 = B.BoundsP2.X - B.BoundsP1.X;
        //    int W2 = B.BoundsP2.Y - B.BoundsP1.Y;
        //    int CBW = B.CB.Width;

        //    int RBTestVal = (int)(BaselineZ + ParamStorage.GetPixZ("(RB) Raised Board Maximum Height (in)"));
        //    float RBPercentage = ParamStorage.GetFloat("(RB) Raised Board Width (%)") / 100f;

        //    int RBCount = 0;
        //    int RBTotal = 0;

        //    bool isHoriz = (W1 > W2);

        //    if ( isHoriz )
        //    {
        //        for ( int x = 0; x < px100XYIn*4; x++ )
        //        {
        //            for ( int y = 0; y < W2; y++ )
        //            {
        //                int tx = B.BoundsP1.X + x;
        //                int ty = B.BoundsP1.Y + y;

        //                UInt16 Val = B.CB.Buf[ty * CBW + tx];
        //                if (Val > RBTestVal)
        //                    RBCount++;
        //                RBTotal++;
        //            }
        //        }

        //        Logger.WriteLine(string.Format("FindRaisedBoard L {0} {1} {2} {3} {4}", B.BoardName, RBCount, RBTotal, (float)RBCount / RBTotal, RBPercentage));
        //        if (((float)RBCount / RBTotal) > RBPercentage)
        //        {
        //            AddDefect(B, PalletDefect.DefectType.raised_board,"Left side of board raised");
        //            SetDefectMarker(B);
        //            //Original.MarkDefect(new System.Windows.Point(B.BoundsP1.X + 100, B.BoundsP1.Y + (W2 / 2)), 40, "RAISED BOARD");
        //        }

        //        RBCount = 0;
        //        RBTotal = 0;

        //        for (int x = W1- px100XYIn * 4; x < W1; x++)
        //        {
        //            for (int y = 0; y < W2; y++)
        //            {
        //                int tx = B.BoundsP1.X + x;
        //                int ty = B.BoundsP1.Y + y;

        //                UInt16 Val = B.CB.Buf[ty * CBW + tx];
        //                if (Val > RBTestVal)
        //                    RBCount++;
        //                RBTotal++;
        //            }
        //        }

        //        Logger.WriteLine(string.Format("FindRaisedBoard R {0} {1} {2} {3} {4}", B.BoardName, RBCount, RBTotal, (float)RBCount / RBTotal, RBPercentage));

        //        if (((float)RBCount / RBTotal) > RBPercentage)
        //        {
        //            AddDefect(B, PalletDefect.DefectType.raised_board, "Right side of board raised");
        //            SetDefectMarker(B);
        //            //Original.MarkDefect(new System.Windows.Point(B.BoundsP2.X - 100, B.BoundsP1.Y + (W2 / 2)), 40, "RAISED BOARD");
        //        }
        //    }
        //}

        //=====================================================================
        private void FindRaisedBoard(Board B)
        {
            int W1 = B.BoundsP2.X - B.BoundsP1.X;
            int W2 = B.BoundsP2.Y - B.BoundsP1.Y;
            int CBW = B.CB.Width;

            int RBTestVal = (int)(BaselineZ + ParamStorage.GetPixZ("(RB) Raised Board Maximum Height (mm)"));
            int RBWidthPx = ParamStorage.GetPixX("(RB) Raised Board Width (mm)");
            int RBAreaPx = RBWidthPx * RBWidthPx;
            float MMPPZ = ParamStorage.GetFloat("MM Per Pixel Z");

            //int RBCount = 0;
            //int RBTotal = 0;
            double AvgDeltaMM = 0;


            if (B.IsHoriz)
            {
                //int NumberListTh = (int)(BaselineZ + (3.0/ MMPPZ));
                List<double> Numbers = new List<double>();

                // Stepping by 4x4
                RBAreaPx = (int)(RBAreaPx / 16);
                for (int x = 0; x < W1; x+=4)
                {
                    for (int y = 0; y < W2; y+=4)
                    {
                        int tx = B.BoundsP1.X + x;
                        int ty = B.BoundsP1.Y + y;
                        UInt16 Val = B.CB.Buf[ty * CBW + tx];
                        if (Val > 0)
                            Numbers.Add(Val);
                    }
                }

                if(Numbers.Count > RBAreaPx)
                {
                    AvgDeltaMM = Numbers.OrderByDescending(n => n) // Sort numbers in descending order
                                        .Take(RBAreaPx) // Take the top 100 values
                                        .Average(); // Calculate the average of these values
                    AvgDeltaMM -= BaselineZ;
                    AvgDeltaMM *= MMPPZ;
                }

                B.MaxDeflection = (float)Math.Round(AvgDeltaMM,2);

                Logger.WriteLine(string.Format("FindRaisedBoard {0} MaxDeflection {1}", B.BoardName, B.MaxDeflection));

                if (B.MaxDeflection >= ParamStorage.GetFloat("(RB) Raised Board Maximum Height (mm)"))
                {
                    AddDefect(B, PalletDefect.DefectType.raised_board, string.Format("Raised Board {0:0.00}mm", B.MaxDeflection));
                    SetDefectMarker(B);
                    //Original.MarkDefect(new System.Windows.Point(B.BoundsP1.X + 100, B.BoundsP1.Y + (W2 / 2)), 40, "RAISED BOARD");
                }
            }


            //if (B.IsHoriz)
            //{
            //    for (int x = 0; x < W1; x++)
            //    {
            //        for (int y = 0; y < W2; y++)
            //        {
            //            int tx = B.BoundsP1.X + x;
            //            int ty = B.BoundsP1.Y + y;

            //            UInt16 Val = B.CB.Buf[ty * CBW + tx];
            //            if (Val > RBTestVal)
            //            {
            //                RBCount++;
            //                AvgDeltaMM += ((Val - BaselineZ) / (double)px100ZIn)*25.4;
            //            }
            //            RBTotal++;
            //        }
            //    }

            //    Logger.WriteLine(string.Format("FindRaisedBoard L {0} {1} {2} {3}", B.BoardName, RBCount, RBTotal, RBAreaPx));
            //    if (RBCount > RBAreaPx)
            //    {
            //        AvgDeltaMM /= RBCount;
            //        AddDefect(B, PalletDefect.DefectType.raised_board, string.Format("Raised Board {0:0.00}mm",AvgDeltaMM));
            //        SetDefectMarker(B);
            //        //Original.MarkDefect(new System.Windows.Point(B.BoundsP1.X + 100, B.BoundsP1.Y + (W2 / 2)), 40, "RAISED BOARD");
            //    }
            //}
        }

        //=====================================================================


        //=====================================================================

        private bool ConfirmItIsANail(Board B, int CX, int CY)
        {
            Logger.WriteLine(String.Format("ConfirmItIsANail  X:{0}  Y:{1}", CX,CY));

            // Check if it is near the board edge
            for (int i = 0; i < B.Edges[0].Count; i++)
            {
                PalletPoint P1 = B.Edges[0][i];
                int dx = P1.X - CX;
                int dy = P1.Y - CY;
                if (Math.Abs(dx)<px050XYIn && Math.Abs(dy)<px050XYIn)
                {
                    // Too close to the edge
                    Logger.WriteLine("ConfirmItIsANail A");
                    return false;
                }

                PalletPoint P2 = B.Edges[1][i];
                dx = P2.X - CX;
                dy = P2.Y - CY;
                if (Math.Abs(dx) < px050XYIn && Math.Abs(dy) < px050XYIn)
                {
                    // Too close to the edge
                    Logger.WriteLine("ConfirmItIsANail B");
                    return false;
                }
            }


            // Do a deeper check that this is a nail

            int W = B.CB.Width;
            int H = B.CB.Height;
            double MNH = ParamStorage.GetPixZ("(RN) Max Nail Height (mm)");
            int NSP = ParamStorage.GetInt("(RN) Nail Neighbor Sample Points");
            int NSR = ParamStorage.GetPixX("(RN) Nail Neighbor Sample Radius (in)");
            int NearTh = NSR / 2;
            int NCSR = ParamStorage.GetPixX("(RN) Nail Confirm Surface Radius (in)");
            int NCHIR = ParamStorage.GetPixX("(RN) Nail Confirm Head Inner Radius (in)");
            int NCHOR = ParamStorage.GetPixX("(RN) Nail Confirm Head Outer Radius (in)");
            int NailDeltaRejectionTh = ParamStorage.GetPixZ("(RN) Nail Max Height Rejection Th (mm)");

            // Get average height of nail head
            double sumZ = 0;
            double sumC = 0;
            int NailZMax = 0;
            for (int dy = -NCHIR; dy < +NCHIR; dy++)
            {
                int y = CY + dy;
                if ((y < 0) || (y >= H)) continue;
                for (int dx = -NCHIR; dx < +NCHIR; dx++)
                {
                    int x = CX + dx;
                    if ((x < 0) || (x >= W)) continue;
                    int z = B.CB.Buf[y * W + x];
                    if ((z != 0) && (z > BaselineZ))
                    {
                        sumZ += (double)z;
                        sumC += 1;
                        NailZMax = Math.Max(NailZMax,z);
                    }
                }
            }
            if (sumC < (NCHIR*2) * (NCHIR*2) * 0.7)
            {
                Logger.WriteLine("ConfirmItIsANail C");
                return false;
            }

            int NailZAvg = (int)Math.Round(sumZ / sumC,0);

            Logger.WriteLine(String.Format("ConfirmItIsANail ({0},{1})  NailZ:{2}  sumZ:{3}  sumC:{4}", CX, CY, NailZAvg, sumZ, sumC));



            // Get average height of pallet around the nail
            sumZ = 0;
            sumC = 0;
            int SurfaceZMax = 0;
            int SurfaceZMin = int.MaxValue;
            for (int dy = -NCSR; dy < +NCSR; dy+=4)
            {
                int y = CY + dy;
                if ((y < 0) || (y >= H) || ((dy>=-NCHOR) && (dy<= NCHOR))) continue;
                for (int dx = -NCSR; dx < +NCSR; dx+=4)
                {
                    int x = CX + dx;
                    if ((x < 0) || (x >= W) || ((dx >= -NCHOR) && (dx <= NCHOR))) continue;
                    int z = B.CB.Buf[y * W + x];
                    if((z!=0) && (z>BaselineZ))
                    {
                        sumZ += (double)z;
                        sumC += 1;
                        SurfaceZMax = Math.Max(SurfaceZMax, z);
                        SurfaceZMin = Math.Min(SurfaceZMin, z);
                    }
                }
            }

            //if (sumC < (NCSR*2) * (NCSR*2) * 0.25)
            //{
            //    Logger.WriteLine("ConfirmItIsANail D");
            //    return false;
            //}

            int SurfaceAvgZ = (int)(sumZ / sumC);

            B.CB.Buf[CY * B.CB.Width + CX-2] = 0;
            B.CB.Buf[CY * B.CB.Width + CX-1] = 0;
            B.CB.Buf[CY * B.CB.Width + CX] = 0;
            B.CB.Buf[CY * B.CB.Width + CX + 1] = (ushort)(NailZAvg + 1000);
            B.CB.Buf[CY * B.CB.Width + CX + 2] = (ushort)(SurfaceAvgZ + 1000);


            Logger.WriteLine(String.Format("ConfirmItIsANail ({0},{1})  SurfaceAvgZ:{2}", CX, CY, SurfaceAvgZ));
            Logger.WriteLine(String.Format("ConfirmItIsANail ({0},{1})  NailDZ:{2}  MNH:{3}", CX, CY, NailZAvg - SurfaceAvgZ, MNH));

            // Check that nail head is above surrounding surface
            if (NailZAvg < SurfaceAvgZ + MNH)
            {
                Logger.WriteLine("ConfirmItIsANail E");
                return false;
            }

            int MaxNailZMinSurfaceZDelta = NailZMax - SurfaceZMin;
            if(MaxNailZMinSurfaceZDelta > NailDeltaRejectionTh)
            {
                Logger.WriteLine("ConfirmItIsANail MaxNailZMinSurfaceZDelta");
                return false;
            }
            //return true;




            // Check that nail head does not have a long tail....a wooden sliver
            int FailedCount = 0;
            int CheckedCount = 0;
            for (int a = 0; a < 75; a++)
            {
                double ang = ((Math.PI * 2) / 75) * a;
                double px = CX + (Math.Sin(ang) * NCHOR);
                double py = CY + (Math.Cos(ang) * NCHOR);
                int ix = (int)px;
                int iy = (int)py;
                if ((ix < 0) || (iy < 0) || (ix >= W) || (iy >= H)) continue;

                int Samp = B.CB.Buf[iy * W + ix];
                if (Samp == 0)
                    continue;

                CheckedCount++;

                if ((NailZMax - Samp) < (MNH / 2))
                {
                    FailedCount++;
                }

                //if (Math.Abs(Samp - NailZAvg) < (MNH/2))
                //{
                //    FailedCount++;
                //}
            }

            Logger.WriteLine(String.Format("ConfirmItIsANail  CheckedCount:{0}  FailedCount:{1}", CheckedCount, FailedCount));

            if (CheckedCount < 10)
            {
                Logger.WriteLine("ConfirmItIsANail F");
                return false;
            }

            if (FailedCount > 0)
            {
                Logger.WriteLine("ConfirmItIsANail G");
                return false;
            }

            return true;
        }

        //=====================================================================

        private void FindRaisedNails(Board B)
        {
            if (B.Edges[0].Count < 5)
            {
                return;
            }

            //if (ParamStorage.GetInt("(RN) Max Nail Height (mm)") < 3) return;

            int MNH = (int)ParamStorage.GetPixZ("(RN) Max Nail Height (mm)");

            int NeighborSamplePointsCount = ParamStorage.GetInt("(RN) Nail Neighbor Sample Points");
            int NeighborSampleRadius = ParamStorage.GetPixX("(RN) Nail Neighbor Sample Radius (in)");
            float RejectionHeightMM = ParamStorage.GetFloat("(RN) Nail Max Height Rejection Th (mm)");



            PalletPoint P1 = FindBoardMinXY(B);
            PalletPoint P2 = FindBoardMaxXY(B);


            int W = B.CB.Width;
            int H = B.CB.Height;
            int SearchStep = Math.Max(1, (int)(px100XYIn / 20));

            List<PalletPoint> Raw = new List<PalletPoint>();

            int[] sinDX = new int[NeighborSamplePointsCount];
            int[] cosDY = new int[NeighborSamplePointsCount];
            for (int a = 0; a < NeighborSamplePointsCount; a++)
            {
                double ang = ((Math.PI * 2) / NeighborSamplePointsCount) * a;
                sinDX[a] = (int)(Math.Sin(ang) * NeighborSampleRadius);
                cosDY[a] = (int)(Math.Cos(ang) * NeighborSampleRadius);
            }

            for (int y = P1.Y; y <= P2.Y; y+= SearchStep)
            {
                if ((y < NeighborSampleRadius + 8) || (y >= H - NeighborSampleRadius - 8)) continue;

                for (int x = P1.X; x <= P2.X; x+= SearchStep)
                {
                    if ((x < NeighborSampleRadius + 8) || (x >= W - NeighborSampleRadius - 8)) continue;

                    //int NailZ = B.CB.Buf[y * W + x];

                    int NailZ = (   B.CB.Buf[y * W + x] +
                                    B.CB.Buf[y * W + x + 4] +
                                    B.CB.Buf[y * W + x - 4] +
                                    B.CB.Buf[y * W + x + W*4] +
                                    B.CB.Buf[y * W + x - W*4]) / 5;

                    if (NailZ == 0)
                        continue;

                    if (NailZ < 10000)
                        continue;

                    int RaisedCount = 0;
                    int NonZeroCount = 0;
                    double AvgNailHeight = 0;

                    //if ((Math.Abs(x - 6964) < 2) && (Math.Abs(y - 5547) < 2))
                     //   RaisedCount *= 2;

                    for (int a = 0; a < NeighborSamplePointsCount; a++)
                    {
                        //double ang = ((Math.PI * 2) / NeighborSamplePointsCount) * a;
                        //double px = x + (Math.Sin(ang) * NeighborSampleRadius);
                        //double py = y + (Math.Cos(ang) * NeighborSampleRadius);
                        int ix = x + sinDX[a];
                        int iy = y + cosDY[a];
                        if ((ix < 0) || (iy < 0) || (ix >= W) || (iy >= H)) continue;

                        UInt16 Samp = B.CB.Buf[iy * W + ix];

                        if (Samp == 0)
                            continue;

                        NonZeroCount++;

                        int Delta = ((int)NailZ - (int)Samp);

                        if (Delta > MNH)
                        {
                            AvgNailHeight += Delta;
                            RaisedCount++;
                        }
                    }

                    if (NonZeroCount == 0)
                        continue;

                    // Can't rely on a bunch of zeros
                    if ((NeighborSamplePointsCount - NonZeroCount) > 2)
                        continue;

                    if (NonZeroCount == RaisedCount)
                    {
                        double NailHeightMM = (((AvgNailHeight / RaisedCount) / px100ZIn) * 25.4f);

                        if (NailHeightMM < RejectionHeightMM)
                        {
                            double NailHeightIn = (int)(((AvgNailHeight / RaisedCount) / px100ZIn) * 1000);
                            Raw.Add(new PalletPoint(x, y, NailHeightIn));
                        }
                        // Nail found, skip ahead
                        //x += px020XYIn;
                    }
                }
            }

            int RequiredConf = ParamStorage.GetInt("Nail Confirmations Required");




            foreach (PalletPoint StartP in Raw)
            {

                //float AvgPX = 0;
                //float AvgPY = 0;
                //float AvgC = 0;

                List<PalletPoint> Neighbors = new List<PalletPoint>();
                foreach (PalletPoint NP in Raw)
                {
                    if ((Math.Abs(StartP.X - NP.X) < px025XYIn) && (Math.Abs(StartP.Y - NP.Y) < px025XYIn))
                    {
                        Neighbors.Add(NP);
                    }
                }

                // Count nearby
                if (Neighbors.Count >= RequiredConf)
                {
                    PalletPoint P = PalletPoint.FromList(Neighbors);

                    //PalletPoint P = new PalletPoint(AvgPX / AvgC, AvgPY / AvgC);

                    // Check if we already have an RN listed as a defect
                    bool foundExisting = false;
                    foreach (PalletPoint ExistingP in B.Nails)
                    {
                        if ((Math.Abs(P.X - ExistingP.X) < px050XYIn) && (Math.Abs(P.Y - ExistingP.Y) < px050XYIn))
                        {
                            foundExisting = true;
                            break;
                        }
                    }

                    if (!foundExisting)
                    {
                        if(ConfirmItIsANail(B, P.X, P.Y))
                        {
                            B.Nails.Add(P);
                            double NailHeightMM = (P.Z / 1000.0)*25.4;
                            AddDefect(B, PalletDefect.DefectType.raised_nail, string.Format("raised nail {0:0.00}mm", NailHeightMM));
                            SetDefectMarker(P.X, P.Y, px050XYIn);
                        }
                    }
                }

            }
        }

        //=====================================================================
        PalletDefect AddDefect(Board B, PalletDefect.DefectType type, string Comment)
        {
            if (B == null)
            {
                PalletDefect Defect = new PalletDefect(PalletDefect.DefectLocation.Pallet, type, Comment);
                PalletLevelDefects.Add(Defect);
                AllDefects.Add(Defect);
                return Defect;
            }
            else
            {
                PalletDefect Defect = new PalletDefect(B.Location, type, Comment);
                B.AllDefects.Add(Defect);
                AllDefects.Add(Defect);
                return Defect;
            }
        }

        void SetDefectMarker(Board B, string Tag="")
        {
            if (AllDefects.Count > 0)
            {
                PalletDefect PD = AllDefects[AllDefects.Count - 1];
                if (Tag == "") Tag = PD.Code;
                PD.SetRectMarker(B.BoundsP1.X, B.BoundsP1.Y, B.BoundsP2.X, B.BoundsP2.Y, Tag);
            }
        }

        void SetDefectMarker(int X, int Y, int R, string Tag = "")
        {
            if (AllDefects.Count > 0)
            {
                PalletDefect PD = AllDefects[AllDefects.Count - 1];
                if (Tag == "") Tag = PD.Code;
                PD.SetCircleMarker(X, Y, R, Tag);
            }
        }

        void SetDefectMarker(int X1, int Y1, int X2, int Y2, string Tag = "")
        {
            if (AllDefects.Count > 0)
            {
                PalletDefect PD = AllDefects[AllDefects.Count - 1];
                if (Tag == "") Tag = PD.Code;
                PD.SetRectMarker(X1, Y1, X2, Y2, Tag);
            }
        }

        //=====================================================================
        //private int CountNearbyNails(PalletPoint p, List<PalletPoint> raw)
        //{
        //    int Count = 0;
        //    foreach (PalletPoint P in raw)
        //    {
        //        if ((Math.Abs(P.X - p.X) < 3) &&
        //             (Math.Abs(P.Y - p.Y) < 3))
        //            Count++;
        //    }

        //    return Count;
        //}

        //=====================================================================
        PalletPoint FindBoardMinXY(Board B)
        {
            if (B.Edges[0].Count < 5)
            {
                return new PalletPoint(0, 0);
            }

            int MinX = int.MaxValue;
            int MinY = int.MaxValue;

            // Is Horizontal?
            if (B.Edges[0][0].X == B.Edges[1][0].X)
            {
                MinX = B.Edges[0][0].X;
                for (int i = 0; i < B.Edges[0].Count; i++)
                {
                    MinY = Math.Min(MinY, B.Edges[0][i].Y);
                }
            }
            else
            {
                MinY = B.Edges[0][0].Y;
                for (int i = 0; i < B.Edges[0].Count; i++)
                {
                    MinX = Math.Min(MinX, B.Edges[0][i].X);
                }
            }

            return new PalletPoint(MinX, MinY);
        }

        //=====================================================================
        PalletPoint FindBoardMaxXY(Board B)
        {
            if (B.Edges[0].Count < 5)
            {
                return new PalletPoint(0, 0);
            }

            int MaxX = int.MinValue;
            int MaxY = int.MinValue;

            // Is Horizontal?
            if (B.Edges[0][0].X == B.Edges[1][0].X)
            {
                MaxX = B.Edges[0][B.Edges[0].Count - 1].X;
                for (int i = 0; i < B.Edges[0].Count; i++)
                {
                    MaxY = Math.Max(MaxY, B.Edges[1][i].Y);
                }
            }
            else
            {
                MaxY = B.Edges[0][B.Edges[0].Count - 1].Y;
                for (int i = 0; i < B.Edges[0].Count; i++)
                {
                    MaxX = Math.Max(MaxX, B.Edges[1][i].X);
                }
            }

            return new PalletPoint(MaxX, MaxY);
        }

        //=====================================================================
        void MakeBlocks(CaptureBuffer CB, List<PalletPoint> P, int Size, UInt16 Val)
        {
            foreach (PalletPoint Pt in P)
            {
                MakeBlock(CB, Pt.X, Pt.Y, Size, Val);
            }
        }

        //=====================================================================
        void MakeBlock(CaptureBuffer CB, int x1, int y1, int x2, int y2, UInt16 Val)
        {
            for (int y = y1; y <= y2; y++)
            {
                for (int x = x1; x <= x2; x++)
                {
                    CB.Buf[y * CB.Width + x] = Val;
                }
            }
        }

        //=====================================================================
        void MakeBlock(CaptureBuffer CB, int x, int y, int size, UInt16 Val)
        {
            MakeBlock(CB, x - (size / 2), y - (size / 2), x + (size / 2), y + (size / 2), Val);
        }

        //=====================================================================
        Board ProbeVertically(CaptureBuffer SourceCB, string Name, PalletDefect.DefectLocation Location,  int StartCol, int EndCol, int Step, int StartRow, int EndRow)
        {
            UInt16[] Buf = SourceCB.Buf;
            List<PalletPoint> P = new List<PalletPoint>();
            int CBW = SourceCB.Width;

            Board B = new Board(Name, Location, true);
            B.CB = new CaptureBuffer(SourceCB.Width, SourceCB.Height);
            B.CB.PaletteType = CaptureBuffer.PaletteTypes.Baselined;

            try
            {

                int Dir = Math.Sign(EndRow - StartRow);
                int Len = Buf.Length;

                List<int> Vals = new List<int>();

                for (int i = StartCol; i < EndCol; i += Step)
                {
                    int Row0 = FindVerticalSpan_Old(SourceCB, i, EndRow, StartRow);
                    int Row1 = FindVerticalSpan_Old(SourceCB, i, StartRow, EndRow);

                    if ((Row0 != -1) && (Row1 != -1))
                    {
                        B.Edges[0].Add(new PalletPoint(i, Math.Min(Row0, Row1)));
                        B.Edges[1].Add(new PalletPoint(i, Math.Max(Row0, Row1)));
                    }
                }


                //int FailWid = (int)(ExpWid * 1.3f);

                bool LastWasGood = false;
                for (int i = 1; i < B.Edges[0].Count - 2; i++)
                {
                    int Delta1 = B.Edges[0][i].X - B.Edges[0][i - 1].X;
                    int Delta2 = B.Edges[1][i].X - B.Edges[1][i - 1].X;

                    if (LastWasGood && ((Delta1 > 1) && (Delta1 < 100) && (Delta2 == Delta1)))
                    {
                        LastWasGood = false;
                        B.Edges[0].Insert(i, new PalletPoint(B.Edges[0][i - 1].X + 1, B.Edges[0][i - 1].Y));
                        B.Edges[1].Insert(i, new PalletPoint(B.Edges[1][i - 1].X + 1, B.Edges[1][i - 1].Y));
                        i--;
                    }
                    else
                        LastWasGood = true;
                }


                for (int i = 0; i < B.Edges[0].Count; i++)
                {
                    // Sanity checks
                    //if (Math.Abs(B.Edges[0][i].Y - B.Edges[1][i].Y) > FailWid)
                    //{
                    //    B.Edges[0].RemoveAt(i);
                    //    B.Edges[1].RemoveAt(i);
                    //    i--;
                    //    continue;
                    //}

                    // Sanity checks
                    if (Math.Abs(B.Edges[0][i].Y - B.Edges[1][i].Y) < 20)
                    {
                        B.Edges[0].RemoveAt(i);
                        B.Edges[1].RemoveAt(i);
                        i--;
                        continue;
                    }
                }

                int ppiX = (int)(25.4f / ParamStorage.GetFloat("MM Per Pixel X"));
                int ppiY = (int)(25.4f / ParamStorage.GetFloat("MM Per Pixel Y"));

                if (B.Edges[0].Count > 300)
                {
                    for (int x = ppiX * 3; x > 0; x--)
                    {
                        if ((B.Edges[0][x].X - B.Edges[0][x - 1].X) > 10)
                        {
                            // trim the rest
                            B.Edges[0].RemoveRange(0, x);
                            B.Edges[1].RemoveRange(0, x);
                            break;
                        }
                    }
                    for (int x = B.Edges[0].Count - ppiX * 3; x < B.Edges[0].Count - 1; x++)
                    {
                        if ((B.Edges[0][x + 1].X - B.Edges[0][x].X) > 10)
                        {
                            // trim the rest
                            B.Edges[0].RemoveRange(x + 1, B.Edges[0].Count - (x + 1));
                            B.Edges[1].RemoveRange(x + 1, B.Edges[1].Count - (x + 1));
                            break;
                        }
                    }
                }


                // Check for end slivers of the V1 boards
                if (B.Edges[0].Count > ppiX * 6)
                {
                    int avgY0 = 0;
                    int avgY1 = 0;
                    int cntY = 0;
                    for (int x = ppiX * 2; x > ppiX; x--)
                    {
                        avgY0 += B.Edges[0][x].Y;
                        avgY1 += B.Edges[1][x].Y;
                        cntY += 1;
                    }
                    if (cntY > 0)
                    {
                        avgY0 /= cntY;
                        avgY1 /= cntY;
                        Logger.WriteLine(string.Format("SLIVERS L {0} {1} {2}", Name, avgY0, avgY1));
                        for (int x = 20; x > 0; x--)
                        {
                            if ((B.Edges[0][x].Y < avgY0 - 20) || (B.Edges[1][x].Y > avgY1 + 20))
                            {
                                // trim the rest
                                Logger.WriteLine("trimming sliver L");
                                B.Edges[0].RemoveRange(0, x);
                                B.Edges[1].RemoveRange(0, x);
                                break;
                            }
                        }
                    }


                    avgY0 = 0;
                    avgY1 = 0;
                    cntY = 0;
                    for (int x = B.Edges[0].Count - ppiX * 2; x < B.Edges[0].Count - ppiX; x++)
                    {
                        avgY0 += B.Edges[0][x].Y;
                        avgY1 += B.Edges[1][x].Y;
                        cntY += 1;
                    }
                    if (cntY > 0)
                    {
                        avgY0 /= cntY;
                        avgY1 /= cntY;
                        Logger.WriteLine(string.Format("SLIVERS R {0} {1} {2}", Name, avgY0, avgY1));
                        for (int x = B.Edges[0].Count - 20; x < B.Edges[0].Count; x++)
                        {
                            if ((B.Edges[0][x].Y < avgY0 - 20) || (B.Edges[1][x].Y > avgY1 + 20))
                            {
                                // trim the rest
                                Logger.WriteLine("trimming sliver R");
                                B.Edges[0].RemoveRange(x + 1, B.Edges[0].Count - (x + 1));
                                B.Edges[1].RemoveRange(x + 1, B.Edges[1].Count - (x + 1));
                                break;
                            }
                        }
                    }
                }

                ushort[] NewBoard = B.CB.Buf;

                for (int i = 0; i < B.Edges[0].Count; i++)
                {
                    PalletPoint P1 = B.Edges[0][i];
                    PalletPoint P2 = B.Edges[1][i];
                    int SpanDir = Math.Sign(P2.Y - P1.Y);

                    for (int y = P1.Y; y != (P2.Y + 1); y += SpanDir)
                    {
                        int Src = y * CBW + P1.X;
                        NewBoard[Src] = Buf[Src];
                        Buf[Src] = 0;
                    }
                }

                // Clean up
                try
                {
                    for (int i = 0; i < B.Edges[0].Count; i++)
                    {
                        PalletPoint P1 = B.Edges[0][i];
                        PalletPoint P2 = B.Edges[1][i];
                        int SpanDir = Math.Sign(P2.Y - P1.Y);

                        for (int y = 1; y < 20; y++)
                        {
                            int Src = (P1.Y - y) * CBW + P1.X;
                            Buf[Src] = 0;
                            Src = (P2.Y + y) * CBW + P1.X;
                            Buf[Src] = 0;
                        }
                    }
                }
                catch (Exception)
                {
                }
    
            }
            catch (Exception)
            {
            }

            AddCaptureBuffer(Name, B.CB);


            //for (int i = 0; i < B.Edges[0].Count; i++)
            //{
            //    MakeBlock(Buf, B.Edges[0][i].X, B.Edges[0][i].Y, 2, 5000);
            //    MakeBlock(Buf, B.Edges[1][i].X, B.Edges[1][i].Y, 2, 5000);
            //}

            return B;
        }


        //=====================================================================
        //int FindVerticalSpan(CaptureBuffer CB, int Col, int StartRow, int EndRow)
        //{
        //    UInt16[] Buf = CB.Buf;
        //    int CBW = CB.Width;
        //    int Dir = Math.Sign(EndRow - StartRow);

        //    //int HitCount = 0;
        //    //int HitRow = -1;

        //    int MinPPix = ParamStorage.GetInt("VerticalHitCountMinPositivePixels");
        //    int MinZPix = ParamStorage.GetInt("VerticalHitCountMinZeroPixels");
        //    int ExpWid = ParamStorage.GetInt("Board Width Y");

        //    int nRows = Math.Abs(EndRow - StartRow) + 1;

        //    int[] ColData = new int[nRows];
        //    int[] PixCnt = new int[nRows];


        //    int Idx = 0;
        //    int FirstIdx = -1;
        //    int LastIdx = -1;
            
        //    for (int y = StartRow; y != EndRow; y += Dir)
        //    {
        //        UInt16 V = Buf[y * CBW + Col];

        //        if ( V > 0 )
        //        {
        //            if (FirstIdx == -1)
        //                FirstIdx = Idx;
        //            LastIdx = Idx;
        //            ColData[Idx] = 1;
        //        }
        //        Idx++;
        //    }

        //    if (LastIdx == -1)
        //        return -1;

        //    if ((LastIdx - FirstIdx) < ExpWid)
        //        return StartRow + FirstIdx;

        //    int CenterIdx = (FirstIdx + LastIdx) / 2;
        //    int StartIdx = CenterIdx - (ExpWid / 2);

        //    // find max pixels
        //    for ( int y = StartIdx; y > FirstIdx; y-- )
        //    {
        //        for ( int yy = y; yy < ExpWid; yy++ )
        //        {
        //            try
        //            {
        //                PixCnt[y] += ColData[yy];
        //            }
        //            catch
        //            {

        //            }
        //        }
        //    }

        //    int Max = 0;
        //    int MaxRow = -1;

        //    for (int i = 0; i < PixCnt.Length; i++)
        //    {
        //        if (PixCnt[i] > Max)
        //        {
        //            Max = PixCnt[i];
        //            MaxRow = (i * Dir) + StartRow;
        //        }
        //    }

        //    //if (Col == 940)
        //    //{
        //    //    System.Console.WriteLine(string.Format("S:{0}   E:{1}   Max:{2}", StartRow, EndRow, MaxRow));
        //    //}

        //    return MaxRow;
        //}





        //=====================================================================
        int FindVerticalSpan_Old(CaptureBuffer CB, int Col, int StartRow, int EndRow)
        {
            UInt16[] Buf = CB.Buf;
            int CBW = CB.Width;
            int Dir = Math.Sign(EndRow - StartRow);

            int HitCount = 0;
            int HitRow = -1;

            int MinPPix = 5;// ParamStorage.GetInt("VerticalHitCountMinPositivePixels");
            //int MinZPix = 15;// ParamStorage.GetInt("VerticalHitCountMinZeroPixels");

            HitCount = 0;

            // Check to make sure we didn't start inside pixels
            for (int i = 0; i < 15; i++)
            {
                UInt16 V = Buf[(StartRow + (i * Dir)) * CBW + Col];
                if (V != 0)
                    HitCount++;
            }

            if (HitCount > 5)
                return -1;


            for (int y = StartRow; y != EndRow; y += Dir)
            {
                UInt16 V = Buf[y * CBW + Col];

                if (V != 0)
                {
                    //if (y == StartRow)
                    //    return -1;

                    if (HitCount == 0)
                        HitRow = y;
                    else
                    {
                        if (HitCount >= MinPPix)
                        {
                            return HitRow;
                        }
                    }

                    HitCount++;
                }
                else
                {
                    HitCount = 0;
                }
            }

            return -1;
        }


        //=====================================================================
        Board ProbeHorizontally(CaptureBuffer SourceCB, string Name, PalletDefect.DefectLocation Location, int StartRow, int EndRow, int Step, int StartCol, int EndCol)
        {
            UInt16[] Buf = SourceCB.Buf;
            int CBW = SourceCB.Width;
            List<PalletPoint> P = new List<PalletPoint>();

            Board B = new Board(Name, Location, false);
            B.CB = new CaptureBuffer(SourceCB.Width, SourceCB.Height);
            B.CB.PaletteType = CaptureBuffer.PaletteTypes.Baselined;

            try
            {
                int Dir = Math.Sign(EndRow - StartRow);
                int Len = Buf.Length;

                int ppiX = (int)(25.4f / ParamStorage.GetFloat("MM Per Pixel X"));
                int ppiY = (int)(25.4f / ParamStorage.GetFloat("MM Per Pixel Y"));


                List<int> Vals = new List<int>();

                for (int i = StartRow; i < EndRow; i += Step)
                {
                    int Col0 = FindHorizontalSpan_Old(SourceCB, i, EndCol, StartCol);
                    int Col1 = FindHorizontalSpan_Old(SourceCB, i, StartCol, EndCol);

                    if ((Col0 != -1) && (Col1 != -1))
                    {
                        B.Edges[0].Add(new PalletPoint(Math.Min(Col0, Col1), i));
                        B.Edges[1].Add(new PalletPoint(Math.Max(Col0, Col1), i));
                    }
                }


                //int FailWid = (int)(ExpWid * 1.2f);

                bool LastWasGood = false;
                for (int i = 1; i < B.Edges[0].Count - 1; i++)
                {
                    int Delta1 = B.Edges[0][i].Y - B.Edges[0][i - 1].Y;
                    int Delta2 = B.Edges[1][i].Y - B.Edges[1][i - 1].Y;

                    if (LastWasGood && ((Delta1 > 1) && (Delta1 < ppiY * 2) && (Delta2 == Delta1)))
                    {
                        //LastWasGood = false;
                        B.Edges[0].Insert(i, new PalletPoint(B.Edges[0][i - 1].X, B.Edges[0][i - 1].Y + 1));
                        B.Edges[1].Insert(i, new PalletPoint(B.Edges[1][i - 1].X, B.Edges[1][i - 1].Y + 1));
                        //i--;
                    }
                    else
                        LastWasGood = true;
                }



                for (int i = 0; i < B.Edges[0].Count; i++)
                {
                    // Sanity checks
                    //if (Math.Abs(B.Edges[0][i].X - B.Edges[1][i].X) > FailWid)
                    //{
                    //    B.Edges[0].RemoveAt(i);
                    //    B.Edges[1].RemoveAt(i);
                    //    i--;
                    //    continue;
                    //}

                    // Sanity checks
                    if (Math.Abs(B.Edges[0][i].X - B.Edges[1][i].X) < 20)
                    {
                        B.Edges[0].RemoveAt(i);
                        B.Edges[1].RemoveAt(i);
                        i--;
                        continue;
                    }
                }

                B.CB = new CaptureBuffer(SourceCB.Width, SourceCB.Height);
                B.CB.PaletteType = CaptureBuffer.PaletteTypes.Baselined;
                ushort[] NewBoard = B.CB.Buf;


                for (int i = 0; i < B.Edges[0].Count; i++)
                {
                    PalletPoint P1 = B.Edges[0][i];
                    PalletPoint P2 = B.Edges[1][i];
                    int SpanDir = Math.Sign(P2.X - P1.X);

                    for (int x = P1.X; x != (P2.X + 1); x += SpanDir)
                    {
                        int Src = P1.Y * CBW + x;
                        NewBoard[Src] = Buf[Src];
                        Buf[Src] = 0;
                    }
                }
            }
            catch (Exception)
            { }

            AddCaptureBuffer(Name, B.CB);


            //for (int i = 0; i < B.Edges[0].Count; i++)
            //{
            //    MakeBlock(Buf, B.Edges[0][i].X, B.Edges[0][i].Y, 2, 5000);
            //    MakeBlock(Buf, B.Edges[1][i].X, B.Edges[1][i].Y, 2, 5000);
            //}

            return B;
        }



        //=====================================================================
        //int FindHorizontalSpan(CaptureBuffer SourceCB, int Row, int StartCol, int EndCol)
        //{
        //    UInt16[] Buf = SourceCB.Buf;
        //    int CBW = SourceCB.Width;
        //    int Dir = Math.Sign(EndCol - StartCol);

        //    //int HitCount = 0;
        //    //int HitCol = -1;

        //    int MinPPix = ParamStorage.GetInt("VerticalHitCountMinPositivePixels");
        //    int MinZPix = ParamStorage.GetInt("VerticalHitCountMinZeroPixels");
        //    int ExpWid = 0;
            
        //    if ( StartCol < (CBW / 2))
        //        ExpWid = ParamStorage.GetInt("Board Width X Left");
        //    else
        //        ExpWid = ParamStorage.GetInt("Board Width X Right");

        //    ExpWid /= 2;

        //    int nCols = Math.Abs(EndCol - StartCol) + 1;

        //    int[] RowData = new int[nCols];
        //    int[] PixCnt = new int[nCols];


        //    int Idx = 0;
        //    for (int x = StartCol; x != EndCol; x += Dir)
        //    {
        //        UInt16 V = Buf[Row * CBW + x];

        //        RowData[Idx++] = V != 0 ? 1 : 0;
        //    }

        //    // find max pixels
        //    int EX = RowData.Length - ExpWid ;
        //    bool SomePixFound = false;
        //    for (int x = 0; x < RowData.Length; x++)
        //    {
        //        if (SomePixFound && (x > EX))
        //            break;

        //        if (RowData[x] != 0)
        //        {
        //            SomePixFound = true;

        //            for (int xx = x; xx < (x+ExpWid); xx++)
        //            {
        //                //try
        //                //{
        //                    PixCnt[x] += RowData[xx];
        //                //}
        //                //catch
        //                //{
        //                //    int adsfsdf = 0;
        //                //}
        //            }
        //        }
        //    }

        //    int Max = 0;
        //    int MaxCol = -1;

        //    for (int i = 0; i < PixCnt.Length; i++)
        //    {
        //        if (PixCnt[i] > Max)
        //        {
        //            Max = PixCnt[i];
        //            MaxCol = (i * Dir) + StartCol;
        //        }
        //    }

        //    return MaxCol;
        //}


        //=====================================================================
        int FindHorizontalSpan_Old(CaptureBuffer SourceCB, int Row, int StartCol, int EndCol)
        {
            UInt16[] Buf = SourceCB.Buf;
            int CBW = SourceCB.Width;
            int Dir = Math.Sign(EndCol - StartCol);

            int HitCount = 0;
            int HitCol = -1;


            int MinPPix = 15;// ParamStorage.GetInt("HorizontalHitCountMinPositivePixels");
            //int MinZPix = 15;// ParamStorage.GetInt("HorizontalHitCountMinZeroPixels");

            HitCount = 0;


            // TEST TEST TEST 
            //if ((Row == 957) && (StartCol < 600))
            //{
            //    BinaryWriter BW = new BinaryWriter(new FileStream("D:/Chep/TestBuf.r3", FileMode.Create));
            //    foreach (ushort V in Buf)
            //        BW.Write(V);
            //    BW.Close();
            //}

            for (int x = StartCol; x != EndCol; x += Dir)
            {
                UInt16 V = Buf[Row * CBW + x];

                if (V != 0)
                {
                    if (HitCount == 0)
                        HitCol = x;
                    else
                    {
                        if (HitCount >= MinPPix)
                        {
                            return HitCol;
                        }
                    }

                    HitCount++;
                }
                else
                {
                    HitCount = 0;
                }
            }

            return -1;
        }




        //=====================================================================
        private UInt16 FindModeVal(UInt16[] Modes, UInt16 MinVal = 1, UInt16 MaxVal = 65535)
        {
            UInt16 Mode = 0;
            UInt16 ModeVal = 0;


            // Ignore zero
            for (int i = MinVal; i < MaxVal; i++)
            {
                if (Modes[i] > Mode)
                {
                    Mode = Modes[i];
                    ModeVal = (UInt16)i;
                }
            }

            return ModeVal;
        }

        //=====================================================================
        private void CheckForRaisedNails(string BoardName)
        {
            throw new NotImplementedException();
        }

        //=====================================================================
        private void FindBoards()
        {
            throw new NotImplementedException();
        }


        //=====================================================================
        void RANSACLines(List<PalletPoint> Points, out PalletPoint bestPointA, out PalletPoint bestPointB)
        {
            // get transform child count
            int amount = Points.Count;
            Random R = new Random();

            // maximum distance to the line, to be considered as an inlier point
            float threshold = 25f;
            float bestScore = float.MaxValue;

            // results array (all the points within threshold distance to line)
            PalletPoint[] bestInliers = new PalletPoint[0];
            bestPointA = new PalletPoint();
            bestPointB = new PalletPoint();

            // how many search iterations we should do
            int iterations = 30;
            for (int i = 0; i < iterations; i++)
            {
                // take 2 points randomly selected from dataset
                int indexA = R.Next(0, amount);
                int indexB = R.Next(0, amount);
                PalletPoint pointA = Points[indexA];
                PalletPoint pointB = Points[indexB];

                // reset score and list for this round of iteration
                float currentScore = 0;
                // temporary list for points found in one search
                List<PalletPoint> currentInliers = new List<PalletPoint>();

                // loop all points in the dataset
                for (int n = 0; n < amount; n++)
                {
                    // take one PalletPoint form all points
                    PalletPoint p = Points[n];

                    // get distance to line, NOTE using editor only helper method
                    var currentError = DistancePointToLine(p, pointA, pointB);

                    // distance is within threshold, add to current inliers PalletPoint list
                    if (currentError < threshold)
                    {
                        currentScore += currentError;
                        currentInliers.Add(p);
                    }
                    else // outliers
                    {
                        currentScore += threshold;
                    }
                } // for-all points

                // check score for the best line found
                if (currentScore < bestScore)
                {
                    bestScore = currentScore;
                    bestInliers = currentInliers.ToArray();
                    bestPointA = pointA;
                    bestPointB = pointB;
                }
            } // for-iterations
        } // Start

        //=====================================================================
        public static float DistancePointToLine(PalletPoint p, PalletPoint a, PalletPoint b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            return Math.Abs((b.X - a.X) * (a.Y - p.Y) - (a.X - p.X) * (b.Y - a.Y)) / distance;
        }





    }
}
