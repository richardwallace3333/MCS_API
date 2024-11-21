using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

using System.Windows.Media.Imaging; // For BitmapFrame
using System.Windows.Interop; // For Imaging
using System.Runtime.InteropServices; // For Marshal





namespace ImageTools
{
    public class FastBitmap: IDisposable
    {
        public bool m_bLocked;
        public Bitmap m_Bmp = null;
        public BitmapData m_BData;
        public IntPtr m_pStart;
        public int m_Stride;
        public int m_Width;
        public int m_Height;
        public int m_BytesPerPixel;
        public bool m_Disposed;
        public Graphics m_Graphics = null;

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public int Width { get { return m_Width; } }
        public int Height { get { return m_Height; } }
        public Bitmap Bmp { get { return m_Bmp; } }
        public Graphics G { get { return this.Graphics; } }
        public Graphics Graphics
        {
            get
            {
                if (m_Graphics != null)
                    return m_Graphics;

                Unlock();
                m_Graphics = Graphics.FromImage(m_Bmp);
                return m_Graphics;
            }
        }

        public FastBitmap(string Filename)
        {
            Bitmap Bmp = (Bitmap)Bitmap.FromFile(Filename);
            InitFromBitmap(Bmp);
        }

        public FastBitmap(int aWidth, int aHeight)
        {
            Bitmap Bmp = new Bitmap(aWidth, aHeight, PixelFormat.Format32bppArgb);
            InitFromBitmap(Bmp);
        }


        //public FastBitmap(int aWidth, int aHeight, ushort[] Buffer, System.Windows.Media.Color[] ColorTable)
        public FastBitmap(int aWidth, int aHeight, ushort[] Buffer, byte[] ColorTable)
        {
            Bitmap Bmp = new Bitmap(aWidth, aHeight, PixelFormat.Format32bppArgb);
            InitFromBitmap(Bmp);

            if (!m_bLocked) Lock();



            unsafe
            {
                fixed (byte* pRgbaColors = ColorTable)
                fixed (ushort* pBufferStart = Buffer)
                {
                    ushort* pBuffer = pBufferStart;
                    ushort* pBufferEnd = pBufferStart + Buffer.Length;
                    UInt32* pColor4 = (UInt32*)pRgbaColors;
                    UInt32* pPixel = (UInt32*)m_pStart.ToPointer();

                    while (pBuffer < pBufferEnd)
                    {
                        if (*pBuffer != 0)
                            *pPixel = pColor4[*pBuffer];
                        pPixel += 1;
                        pBuffer += 1;
                    }
                }


                //fixed (byte* pRgbaColors = ColorTable)
                //fixed (ushort* pBufferStart = Buffer)
                //{
                //    ushort* pBuffer = pBufferStart;
                //    ushort* pBufferEnd = pBufferStart + Buffer.Length;
                //    Byte* pPixel = (Byte*)m_pStart.ToPointer();

                //    while( pBuffer < pBufferEnd)
                //    {
                //        if (*pBuffer != 0)
                //        {
                //            int rgbOffset = ((int)(*pBuffer)) << 2;
                //            pPixel[0] = pRgbaColors[rgbOffset + 0];
                //            pPixel[1] = pRgbaColors[rgbOffset + 1];
                //            pPixel[2] = pRgbaColors[rgbOffset + 2];
                //            pPixel[3] = pRgbaColors[rgbOffset + 3];
                //        }
                //        pPixel += 4;
                //        pBuffer += 1;
                //    }
                //}

                //fixed (byte* pRgbaColors = ColorTable)
                //fixed (ushort* pBuffer = Buffer)
                //{
                //    Byte* pPixel = (Byte*)m_pStart.ToPointer();
                //    int buffLen = Buffer.Length;
                //    for (int i = 0; i < buffLen; i++)
                //    {
                //        if (pBuffer[i] != 0)
                //        {
                //            int rgbOffset = ((int)pBuffer[i])<<2;
                //            //System.Windows.Media.Color C = ColorTable[Buffer[i]];
                //            pPixel[0] = pRgbaColors[rgbOffset + 0];
                //            pPixel[1] = pRgbaColors[rgbOffset + 1];
                //            pPixel[2] = pRgbaColors[rgbOffset + 2];
                //            pPixel[3] = pRgbaColors[rgbOffset + 3];
                //        }
                //        pPixel += 4;
                //    }
                //}

                //Byte* pPixel = (Byte*)m_pStart.ToPointer();
                //for (int i = 0; i < Buffer.Length; i++)
                //{
                //    if (Buffer[i] != 0)
                //    {
                //        System.Windows.Media.Color C = ColorTable[Buffer[i]];
                //        pPixel[0] = C.B;
                //        pPixel[1] = C.G;
                //        pPixel[2] = C.R;
                //        pPixel[3] = C.A;
                //    }
                //    pPixel += 4;
                //}
            }


        }


        public BitmapSource GetBitmapSource()
        {
            IntPtr hBitmap = m_Bmp.GetHbitmap();

            try
            {
                // Create a BitmapSource from the HBitmap
                BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                return bitmapSource;// BitmapFrame.Create(bitmapSource);
            }
            finally
            {
                // Release the HBitmap
                DeleteObject(hBitmap);
            }
        }

        public FastBitmap( Bitmap Src )
        {
            InitFromBitmap(Src);
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!m_Disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    Unlock();

                    if (m_Graphics != null) { m_Graphics.Dispose(); m_Graphics = null; }
                    if (m_Bmp != null) { m_Bmp.Dispose(); m_Bmp = null; }
                    m_Bmp = null;
                    m_BData = null;
                }
            }
            m_Disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void InitFromBitmap(Bitmap Src)
        {
            m_bLocked = false;
            m_Bmp = Src;

            if (m_Bmp.PixelFormat == PixelFormat.Format24bppRgb)
                m_BytesPerPixel = 3;
            else
                if (m_Bmp.PixelFormat == PixelFormat.Format32bppArgb)
                    m_BytesPerPixel = 4;
                else
                    Debug.Assert(false);

            // Enforce this for the time being
            //Debug.Assert(m_Bmp.PixelFormat == PixelFormat.Format32bppArgb);

            m_Width = m_Bmp.Width;
            m_Height = m_Bmp.Height;
        }


        public Color GetPixel(int X, int Y)
        {
            if (!m_bLocked) Lock();

            unsafe
            {
                Byte* pTemp = (Byte*)m_pStart.ToPointer();
                Byte* pPixel = pTemp + (Y * m_Stride) + (X * m_BytesPerPixel);
                if( m_BytesPerPixel == 4 )
                    return Color.FromArgb(pPixel[3], pPixel[2], pPixel[1], pPixel[0]);
                else
                    return Color.FromArgb(pPixel[2], pPixel[1], pPixel[0]);
            }
        }


        public void SetPixel(int I, Color C)
        {
            if (!m_bLocked) Lock();

            unsafe
            {
                Byte* pTemp = (Byte*)m_pStart.ToPointer();
                Byte* pPixel = pTemp + (I * m_BytesPerPixel);
                if (m_BytesPerPixel == 4)
                {
                    pPixel[0] = C.B;
                    pPixel[1] = C.G;
                    pPixel[2] = C.R;
                    pPixel[3] = C.A;
                }
                else
                {
                    pPixel[0] = C.B;
                    pPixel[1] = C.G;
                    pPixel[2] = C.R;
                }
            }
        }

        public void SetPixel(int I, System.Windows.Media.Color C)
        {
            if (!m_bLocked) Lock();

            unsafe
            {
                Byte* pTemp = (Byte*)m_pStart.ToPointer();
                Byte* pPixel = pTemp + (I * m_BytesPerPixel);
                if (m_BytesPerPixel == 4)
                {
                    pPixel[0] = C.B;
                    pPixel[1] = C.G;
                    pPixel[2] = C.R;
                    pPixel[3] = C.A;
                }
                else
                {
                    pPixel[0] = C.B;
                    pPixel[1] = C.G;
                    pPixel[2] = C.R;
                }
            }
        }



        public void SetPixel(int X, int Y, Color C)
        {
            if (!m_bLocked) Lock();

            unsafe
            {
                Byte* pTemp = (Byte*)m_pStart.ToPointer();
                Byte* pPixel = pTemp + (Y * m_Stride) + (X * m_BytesPerPixel);
                if (m_BytesPerPixel == 4)
                {
                    pPixel[0] = C.B;
                    pPixel[1] = C.G;
                    pPixel[2] = C.R;
                    pPixel[3] = C.A;
                }
                else
                {
                    pPixel[0] = C.B;
                    pPixel[1] = C.G;
                    pPixel[2] = C.R;
                }
            }
        }

        public void SetPixel(int X, int Y, System.Windows.Media.Color C)
        {
            if (!m_bLocked) Lock();

            unsafe
            {
                Byte* pTemp = (Byte*)m_pStart.ToPointer();
                Byte* pPixel = pTemp + (Y * m_Stride) + (X * m_BytesPerPixel);
                if (m_BytesPerPixel == 4)
                {
                    pPixel[0] = C.B;
                    pPixel[1] = C.G;
                    pPixel[2] = C.R;
                    pPixel[3] = C.A;
                }
                else
                {
                    pPixel[0] = C.B;
                    pPixel[1] = C.G;
                    pPixel[2] = C.R;
                }
            }
        }


        public void Unlock()
        {
            if (m_bLocked && (m_Bmp!=null))
            {
                m_Bmp.UnlockBits(m_BData);
                m_bLocked = false;
            }
        }

        public void Lock()
        {
            if (!m_bLocked)
            {
                if (m_Bmp != null)
                {
                    m_BData = m_Bmp.LockBits(new Rectangle(0, 0, m_Bmp.Width, m_Bmp.Height), ImageLockMode.ReadWrite, m_Bmp.PixelFormat);
                    m_Stride = m_BData.Stride;
                    m_pStart = m_BData.Scan0;
                }

                if (m_Graphics != null)
                {
                    m_Graphics.Dispose();
                    m_Graphics = null;
                }

                m_bLocked = true;
            }
        }

        public void Save(string Filename, ImageFormat Format)
        {
            m_Bmp.Save(Filename, Format);
        }

        public void Load(string Filename)
        {
            Bitmap Bmp = (Bitmap)Bitmap.FromFile(Filename);
            InitFromBitmap(Bmp);
        }
        
    }
}
