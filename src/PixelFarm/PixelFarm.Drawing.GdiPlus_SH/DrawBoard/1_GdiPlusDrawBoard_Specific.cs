﻿//BSD, 2014-present, WinterDev
//ArthurHub, Jose Manuel Menendez Poo

// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;


namespace PixelFarm.Drawing.WinGdi
{

    class MyGdiBackbuffer : Backbuffer
    {
        readonly int _w;
        readonly int _h;
        public MyGdiBackbuffer(int w, int h)
        {
            _w = w;
            _h = h;
        }
        public override int Width => _w;
        public override int Height => _h;

        public override Image GetImage()
        {
            throw new NotImplementedException();
        }
    }
    public partial class GdiPlusDrawBoard : DrawBoard, IDisposable
    {

        bool _disposed;
        GdiPlusRenderSurface _gdigsx;
        Painter _painter;
        BitmapBufferProvider _memBmpBinder;
        public GdiPlusDrawBoard(GdiPlusRenderSurface renderSurface)
        {
            _left = 0;
            _top = 0;
            _right = renderSurface.Width;
            _bottom = renderSurface.Height;

            _gdigsx = renderSurface;
            _painter = _gdigsx.GetAggPainter();

            _memBmpBinder = new MemBitmapBinder(renderSurface.GetMemBitmap(), false);
            _memBmpBinder.BitmapFormat = BitmapBufferFormat.BGR;
        }
        public override void SwitchBackToDefaultBuffer(Backbuffer backbuffer)
        {
            throw new NotImplementedException();
        }
        public override void AttachToBackBuffer(Backbuffer backbuffer)
        {
            throw new NotImplementedException();
        }
        public override Backbuffer CreateBackbuffer(int w, int h)
        {
            return new MyGdiBackbuffer(w, h);
        }
        public GdiPlusRenderSurface RenderSurface => _gdigsx;
        public override bool IsGpuDrawBoard => false;
        public override DrawBoard GetCpuBlitDrawBoard() => this;
        //
        public override BitmapBufferProvider GetInternalBitmapProvider() => _memBmpBinder;

#if DEBUG
        public override string ToString()
        {
            return _gdigsx.ToString();
        }
#endif

        public override Painter GetPainter()
        {
            //since painter origin and canvas origin is separated 
            //so must check here
            //TODO: revisit the painter and the surface => shared resource **

            _painter.SetOrigin(_canvasOriginX, _canvasOriginY);
            return _painter;
        }
        public override void RenderTo(Image destImg, int srcX, int srcYy, int srcW, int srcH)
        {

            //render back buffer to target image

            unsafe
            {
                CpuBlit.MemBitmap memBmp = destImg as CpuBlit.MemBitmap;
                if (memBmp != null)
                {
                    CpuBlit.Imaging.TempMemPtr tmpPtr = CpuBlit.MemBitmap.GetBufferPtr(memBmp);
                    byte* head = (byte*)tmpPtr.Ptr;
                    _gdigsx.RenderTo(head);
                    tmpPtr.Dispose();
                }
            }
        }
        public override void Dispose()
        {
            if (_gdigsx != null)
            {
                _gdigsx.CloseCanvas();
                _gdigsx = null;
            }
        }
        public override void CloseCanvas()
        {
            if (_disposed)
            {
                return;
            }

            _gdigsx.CloseCanvas();

            _disposed = true;
            ReleaseUnManagedResource();
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (_disposed)
            {
                return;
            }
            this.CloseCanvas();
        }
        void ClearPreviousStoredValues()
        {
            _gdigsx.ClearPreviousStoredValues();
        }

        void ReleaseUnManagedResource()
        {
            _gdigsx.ReleaseUnManagedResource();
#if DEBUG

            debug_releaseCount++;
#endif
        }

#if DEBUG
        public override void dbug_DrawRuler(int x)
        {
            _gdigsx.dbug_DrawRuler(x);
        }
        public override void dbug_DrawCrossRect(Color color, Rectangle rect)
        {
            _gdigsx.dbug_DrawCrossRect(color, rect);
        }
#endif
    }
}