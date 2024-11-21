using System.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;
using PalletCheck;

namespace MCS
{

    public class MCSStitcher
    {
        public MCSStitcher() { }


        public int CalcXOffsetAdjustment(int BuffW, int BuffH, ushort[] Buff0, int X0, ushort[] Buff1, int X1) 
        {
            int R = 12;
            // Use X0 column as reference and search X1+-R for best match

            int BestX1 = X1;
            double BestErr = 1E99;
            for(int r=-R; r<=R; r++)
            {
                int XT = X1 + r;
                double AvgErr = 0;
                double AvgCnt = 0;
                for(int y=0; y<BuffH; y+=1)
                {
                    int yoffset = y * BuffW;

                    // Centered on XT
                    int v0 = Buff0[yoffset + X0];
                    int v1 = Buff1[yoffset + XT];
                    if( (v0!=0) && (v1!=0) )
                    {
                        AvgErr += Math.Abs(v0 - v1);
                        AvgCnt += 1;
                    }

                    // Left Pixels
                    v0 = Buff0[yoffset + X0-5];
                    v1 = Buff1[yoffset + XT-5];
                    if ((v0 != 0) && (v1 != 0))
                    {
                        AvgErr += Math.Abs(v0 - v1);
                        AvgCnt += 1;
                    }

                    // Right Pixels
                    v0 = Buff0[yoffset + X0+5];
                    v1 = Buff1[yoffset + XT+5];
                    if ((v0 != 0) && (v1 != 0))
                    {
                        AvgErr += Math.Abs(v0 - v1);
                        AvgCnt += 1;
                    }

                }
                if (AvgCnt != 0) AvgErr /= AvgCnt;

                if (AvgErr < BestErr)
                {
                    BestErr = AvgErr;
                    BestX1 = XT;
                }
            }

            return BestX1-X1;
        }
        
        public void StitchFrame(MCSFrame F)
        {

            int SrcWidth = F.RulerRangeCBs[0].Width;
            int SrcHeight = F.RulerRangeCBs[0].Height;

            // Create the MCS buffer
            int DstWidth = ParamStorage.GetInt("MCS DstWidth (px)");
            float SrcMMPPY = ParamStorage.GetFloat("MCS SrcMMPPY");
            float DstMMPPY = ParamStorage.GetFloat("MCS DstMMPPY");
            int DstHeight = (int)(SrcHeight * SrcMMPPY / DstMMPPY);

            // Enforce sanity on DstHeight
            //DstHeight = Math.Max(DstWidth, Math.Min(DstWidth * 2, DstHeight));
            DstHeight = (int)Math.Max(DstWidth/2, Math.Min(DstWidth * 2, DstHeight));
            float DstToSrcYScale = (float)SrcHeight / (float)DstHeight;

            float SrcMMPPZ = ParamStorage.GetFloat("MCS SrcMMPPZ");
            float DstMMPPZ = ParamStorage.GetFloat("MCS DstMMPPZ");
            float SrcBaselineZ = ParamStorage.GetFloat("MCS SrcReferenceZ (px)");
            float DstBaselineZ = ParamStorage.GetFloat("MCS DstReferenceZ (px)");
            float SrcZToDstZScale = (SrcMMPPZ / DstMMPPZ);

            int SrcReferenceX0L = ParamStorage.GetInt("MCS SrcReferenceX0L (px)");
            int SrcReferenceX0R = ParamStorage.GetInt("MCS SrcReferenceX0R (px)");
            int SrcReferenceX1L = ParamStorage.GetInt("MCS SrcReferenceX1L (px)");
            int SrcReferenceX1R = ParamStorage.GetInt("MCS SrcReferenceX1R (px)");
            int SrcReferenceX2L = ParamStorage.GetInt("MCS SrcReferenceX2L (px)");
            int SrcReferenceX2R = ParamStorage.GetInt("MCS SrcReferenceX2R (px)");


            bool BlendingEnabled = ParamStorage.GetInt("MCS Blending Enabled") != 0;
            bool RescaleZEnabled = ParamStorage.GetInt("MCS Rescale Z Enabled") != 0;
            bool RescaleYEnabled = ParamStorage.GetInt("MCS Rescale Y Enabled") != 0;
            if (!RescaleYEnabled)
                DstToSrcYScale = 1.0f;

            float[] wbuff0 = new float[SrcWidth];
            float[] wbuff1 = new float[SrcWidth];
            float[] wbuff2 = new float[SrcWidth];
            if (BlendingEnabled)
            {
                for (int x = 0; x < SrcWidth; x++)
                {
                    float mean = 2048f;
                    float stdDev = 512f;
                    float scale = 1283.697f;
                    float trans = 0.0f;

                    double power = -((x - mean) * (x - mean)) / (2 * stdDev * stdDev);
                    double w = (1 / (stdDev * Math.Sqrt(2 * Math.PI))) * Math.Exp(power);
                    w = (w * scale) + trans;
                    w = Math.Min(1, Math.Max(0, w));
                    wbuff0[x] = (float)w;
                    wbuff1[x] = (float)w;
                    wbuff2[x] = (float)w;
                }
            }
            else
            {
                float[] wbuff = new float[SrcWidth];
                for (int x = 0; x < SrcWidth; x++)
                {
                    wbuff0[x] = 0.001f;
                    wbuff1[x] = 1.000f;
                    wbuff2[x] = 0.001f;
                }
            }


            // Range and Reflection
            F.RangeCB = new CaptureBuffer(DstWidth, DstHeight);
            F.ReflectanceCB = new CaptureBuffer(DstWidth, DstHeight);
            F.ReflectanceCB.PaletteType = CaptureBuffer.PaletteTypes.Gray;
            {
                ushort[] dstRngBuff = F.RangeCB.Buf;
                ushort[] cb0Rngbuff = F.RulerRangeCBs[0].Buf;
                ushort[] cb1Rngbuff = F.RulerRangeCBs[1].Buf;
                ushort[] cb2Rngbuff = F.RulerRangeCBs[2].Buf;

                ushort[] dstRflBuff = F.ReflectanceCB.Buf;
                ushort[] cb0Rflbuff = F.RulerReflectanceCBs[0].Buf;
                ushort[] cb1Rflbuff = F.RulerReflectanceCBs[1].Buf;
                ushort[] cb2Rflbuff = F.RulerReflectanceCBs[2].Buf;



                //========================================================
                // Calculate X-Offsets and overlap regions
                //========================================================
                int XCorrection01 = CalcXOffsetAdjustment(SrcWidth, SrcHeight, cb1Rflbuff, SrcReferenceX1L, cb0Rflbuff, SrcReferenceX0R);
                int XCorrection12 = CalcXOffsetAdjustment(SrcWidth, SrcHeight, cb1Rflbuff, SrcReferenceX1R, cb2Rflbuff, SrcReferenceX2L);

                float XCorrection01mm = (float)Math.Round(XCorrection01 * ParamStorage.GetFloat("MCS SrcMMPPX"), 4);
                float XCorrection12mm = (float)Math.Round(XCorrection12 * ParamStorage.GetFloat("MCS SrcMMPPX"), 4);
                string Xstr = string.Format("[0->1] {0}px {1}mm   [2->1]  {2}px {3}mm", XCorrection01, XCorrection01mm, XCorrection12, XCorrection12mm);
                Logger.WriteLine("MCS XCorrections: " + Xstr);
                StatusStorage.Set("MCS XCorrections", Xstr);

                SrcReferenceX0R += XCorrection01;
                SrcReferenceX2L += XCorrection12;
                int DstOriginX1 = (DstWidth / 2) - (SrcWidth / 2);
                int DstOriginX0 = DstOriginX1 + SrcReferenceX1L - SrcReferenceX0R;
                int DstOriginX2 = DstOriginX1 + SrcReferenceX1R - SrcReferenceX2L;




                //========================================================
                // Calculate Z-Offsets
                //========================================================

                double Avg01ZOffset = 0;
                double Avg01ZCount = 0;
                double Avg12ZOffset = 0;
                double Avg12ZCount = 0;
                for (int dsty = 0; dsty < DstHeight; dsty+=10)
                {
                    int dstyoffset = dsty * DstWidth;
                    int srcy = (int)(dsty * DstToSrcYScale);
                    if (srcy < 0 || srcy >= SrcHeight) continue;

                    int srcyoffset = srcy * SrcWidth;
                    for (int dstx = 0; dstx < DstWidth; dstx+=10)
                    {

                        int cb0X = dstx - DstOriginX0;
                        int cb0Rng = 0;
                        if (cb0X >= 0 && cb0X < SrcWidth)
                            cb0Rng = cb0Rngbuff[srcyoffset + cb0X];

                        int cb1X = dstx - DstOriginX1;
                        int cb1Rng = 0;
                        if (cb1X >= 0 && cb1X < SrcWidth)
                            cb1Rng = cb1Rngbuff[srcyoffset + cb1X];

                        int cb2X = dstx - DstOriginX2;
                        int cb2Rng = 0;
                        if (cb2X >= 0 && cb2X < SrcWidth)
                            cb2Rng = cb2Rngbuff[srcyoffset + cb2X];

                        if (cb1Rng!=0)
                        {
                            if (cb0Rng != 0)
                            {
                                Avg01ZOffset += (cb1Rng - cb0Rng);
                                Avg01ZCount += 1;
                            }
                            if (cb2Rng != 0)
                            {
                                Avg12ZOffset += (cb1Rng - cb2Rng);
                                Avg12ZCount += 1;
                            }
                        }
                    }
                }
                if (Avg01ZCount > 1000) Avg01ZOffset = Math.Round(Avg01ZOffset / Avg01ZCount,4);
                else Avg01ZOffset = 0;
                if (Avg12ZCount > 1000) Avg12ZOffset = Math.Round(Avg12ZOffset / Avg12ZCount,4);
                else Avg12ZOffset = 0;

                int ZCorrection01 = (int)Avg01ZOffset;
                int ZCorrection12 = (int)Avg12ZOffset;
                float ZCorrection01mm = (float)Math.Round(ZCorrection01 * ParamStorage.GetFloat("MCS SrcMMPPZ"),4);
                float ZCorrection12mm = (float)Math.Round(ZCorrection12 * ParamStorage.GetFloat("MCS SrcMMPPZ"),4);

                string Zstr = string.Format("[0->1] {0}px {1}mm   [2->1]  {2}px {3}mm", ZCorrection01, ZCorrection01mm, ZCorrection12, ZCorrection12mm);
                Logger.WriteLine("MCS ZCorrections: " + Zstr);
                StatusStorage.Set("MCS ZCorrections", Zstr);


                //========================================================
                // Do Stitching
                //========================================================
                for (int dsty = 0; dsty < DstHeight; dsty++)
                {
                    int dstyoffset = dsty * DstWidth;
                    int srcy = (int)(dsty * DstToSrcYScale);
                    if (srcy < 0 || srcy >= SrcHeight) continue;

                    int srcyoffset = srcy * SrcWidth; 
                    for (int dstx = 0; dstx < DstWidth; dstx++)
                    {
                        float rangeV = 0;
                        float reflV = 0;
                        float wsum = 0;
                        int offset;
                        float w;

                        int cb0X = dstx - DstOriginX0;
                        offset = srcyoffset + cb0X;
                        if (cb0X >= 0 && cb0X < SrcWidth && cb0Rngbuff[offset] != 0)
                        {   
                            w = wbuff0[cb0X];
                            rangeV += (cb0Rngbuff[offset] + ZCorrection01) * w;
                            reflV += cb0Rflbuff[offset] * w;
                            wsum += w;
                        }

                        int cb1X = dstx - DstOriginX1;
                        offset = srcyoffset + cb1X;
                        if (cb1X >= 0 && cb1X < SrcWidth && cb1Rngbuff[offset] != 0)
                        {
                            w = wbuff1[cb1X];
                            rangeV += cb1Rngbuff[offset] * w;
                            reflV += cb1Rflbuff[offset] * w;
                            wsum += w;
                        }

                        int cb2X = dstx - DstOriginX2;
                        offset = srcyoffset + cb2X;
                        if (cb2X >= 0 && cb2X < SrcWidth && cb2Rngbuff[offset] != 0)
                        {
                            w = wbuff2[cb2X];
                            rangeV += (cb2Rngbuff[offset] + ZCorrection12) * w;
                            reflV += cb2Rflbuff[offset] * w;
                            wsum += w;
                        }


                        if (wsum == 0)
                        {
                            dstRngBuff[dstyoffset + dstx] = 0;
                            dstRflBuff[dstyoffset + dstx] = 0;
                        }
                        else
                        {
                            float rngZ = rangeV / wsum;
                            if(RescaleZEnabled) rngZ = ((rngZ - SrcBaselineZ) * SrcZToDstZScale) + DstBaselineZ;
                            if (rngZ < 0) rngZ = 0;
                            dstRngBuff[dstyoffset + dstx] = (ushort)(rngZ);

                            float rflZ = reflV / wsum;
                            if (rflZ < 0) rflZ = 0;
                            dstRflBuff[dstyoffset + dstx] = (ushort)(rflZ);
                        }
                    }
                }
            }
        }

        public bool IsReverseMotionCutoffBadPalletYuck(MCSFrame F)
        {
            int SrcWidth = F.RangeCB.Width;
            ushort[] Buf = F.RangeCB.Buf;
            int count = 0;
            for (int x = 0; x < SrcWidth; x++)
            {
                if (Buf[x] != 0) count += 1;
            }
            return (count>1000);
        }
    }
}