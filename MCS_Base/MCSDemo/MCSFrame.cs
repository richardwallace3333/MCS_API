using System.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;
using PalletCheck;
using System.Security.Cryptography;

namespace MCS
{
    public class MCSFrame
    {
        public DateTime CreateTime;
        public ulong FrameID;

        public CaptureBuffer RangeCB;
        public CaptureBuffer ReflectanceCB;

        public List<CaptureBuffer> RulerRangeCBs = new List<CaptureBuffer>();
        public List<CaptureBuffer> RulerReflectanceCBs = new List<CaptureBuffer>();

        //public override string ToString()
        //{
        //    string s = "";
        //    s = string.Format("MCS.Frame:  cam:{0}  w:{1}  h:{2}", Camera.CameraName, Width, Height);
        //    return s;
        //}

        public MCSFrame()
        {
            CreateTime = DateTime.Now;
        }


        public void Save(string SavePath, bool Threaded=false)
        {
            RangeCB.Save(SavePath.Replace(".r3", "_0_rng.r3"), Threaded);
            RangeCB.SaveImage(SavePath.Replace(".r3", "_0_rng.png"), false, Threaded);
            ReflectanceCB.Save(SavePath.Replace(".r3", "_0_rfl.r3"), Threaded);
            ReflectanceCB.SaveImage(SavePath.Replace(".r3", "_0_rfl.png"), true, Threaded);

            for (int i = 0; i < RulerRangeCBs.Count; i++)
            {
                string pfx = "_" + (i + 1).ToString();

                RulerRangeCBs[i].Save(SavePath.Replace(".r3", pfx + "_rng.r3"), Threaded);
                RulerRangeCBs[i].SaveImage(SavePath.Replace(".r3", pfx + "_rng.png"), false, Threaded);
                RulerReflectanceCBs[i].Save(SavePath.Replace(".r3", pfx + "_rfl.r3"), Threaded);
                RulerReflectanceCBs[i].SaveImage(SavePath.Replace(".r3", pfx + "_rfl.png"), true, Threaded);
            }
        }

        public void Load(string LoadPath)
        {
            //| *_0_rng.r3"

            RangeCB = new CaptureBuffer();
            RangeCB.Load(LoadPath);

            ReflectanceCB = new CaptureBuffer();
            ReflectanceCB.Load(LoadPath.Replace("rng.r3", "rfl.r3"));
            ReflectanceCB.PaletteType = CaptureBuffer.PaletteTypes.Gray;

            for (int i = 0; i < 5; i++)
            {
                string pfx = "_" + (i + 1).ToString();
                string rngPath = LoadPath.Replace("_0_rng.r3", pfx + "_rng.r3");
                string rflPath = LoadPath.Replace("_0_rng.r3", pfx + "_rfl.r3");
                if (!System.IO.File.Exists(rngPath)) break;
                if (!System.IO.File.Exists(rflPath)) break;

                CaptureBuffer rngCB = new CaptureBuffer();
                rngCB.Load(rngPath);
                RulerRangeCBs.Add(rngCB);

                CaptureBuffer rflCB = new CaptureBuffer();
                rflCB.Load(rflPath);
                rflCB.PaletteType = CaptureBuffer.PaletteTypes.Gray;
                RulerReflectanceCBs.Add(rflCB);
            }
        }
    }

}