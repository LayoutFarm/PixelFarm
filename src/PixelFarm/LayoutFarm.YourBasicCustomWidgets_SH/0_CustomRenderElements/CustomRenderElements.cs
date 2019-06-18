﻿//Apache2, 2014-present, WinterDev

using PixelFarm.Drawing;
namespace LayoutFarm.CustomWidgets
{
    public class CustomRenderBox : RenderBoxBase
    {
        Color _backColor;
        Color _borderColor;
        bool _hasSomeBorderW;

        //these are NOT CSS borders/margins/paddings***
        //we use pixel unit for our RenderBox
        //with limitation of int8 number

        byte _contentLeft;
        byte _contentTop;
        byte _contentRight;
        byte _contentBottom;

        byte _borderLeft;
        byte _borderTop;
        byte _borderRight;
        byte _borderBottom;

        public CustomRenderBox(RootGraphic rootgfx, int width, int height)
            : base(rootgfx, width, height)
        {
            this.BackColor = Color.LightGray;
        }

        public int PaddingLeft
        {
            get => _contentLeft - _borderLeft;
            set => _contentLeft = (byte)(value + _borderLeft);
        }

        public int PaddingTop
        {
            get => _contentTop - _borderTop;
            set => _contentTop = (byte)(value + _borderTop);
        }
        public int PaddingRight
        {
            get => _contentRight - _borderRight;
            set => _contentRight = (byte)(value + _borderRight);

        }
        public int PaddingBottom
        {
            get => _contentBottom - _borderBottom;
            set => _contentBottom = (byte)(value + _borderBottom);
        }
        public void SetPadding(byte left, byte top, byte right, byte bottom)
        {
            _contentLeft = (byte)(left + _borderLeft);
            _contentTop = (byte)(top + _borderTop);
            _contentRight = (byte)(right + _borderRight);
            _contentBottom = (byte)(bottom + _borderBottom);
        }
        public void SetPadding(byte sameValue)
        {
            _contentLeft = (byte)(sameValue + _borderLeft);
            _contentTop = (byte)(sameValue + _borderTop);
            _contentRight = (byte)(sameValue + _borderRight);
            _contentBottom = (byte)(sameValue + _borderBottom);
        }
        //------------------ 
        public int BorderTop
        {
            get => _borderTop;
            set
            {
                _borderTop = (byte)value;
                if (!_hasSomeBorderW) _hasSomeBorderW = value > 0;
            }
        }
        public int BorderBottom
        {
            get => _borderBottom;
            set
            {
                _borderBottom = (byte)value;
                if (!_hasSomeBorderW) _hasSomeBorderW = value > 0;
            }
        }
        public int BorderRight
        {
            get => _borderRight;
            set
            {
                _borderRight = (byte)value;
                if (!_hasSomeBorderW) _hasSomeBorderW = value > 0;
            }
        }
        public int BorderLeft
        {
            get => _borderLeft;
            set
            {
                _borderLeft = (byte)value;
                if (!_hasSomeBorderW) _hasSomeBorderW = value > 0;
            }

        }
        public void SetBorders(byte left, byte top, byte right, byte bottom)
        {
            _borderLeft = left;
            _borderTop = top;
            _borderRight = right;
            _borderBottom = bottom;

            _hasSomeBorderW = ((left | top | right | bottom) > 0);

        }
        public void SetBorders(byte sameValue)
        {
            _borderLeft =
                _borderTop =
                _borderRight =
                _borderBottom = sameValue;

            _hasSomeBorderW = sameValue > 0;
        }
        //-------------

        public int ContentWidth => Width - (_contentLeft + _contentRight);
        public int ContentHeight => Height - (_contentTop + _contentBottom);

        public int ContentLeft
        {
            get => _contentLeft;
            set => _contentLeft = (byte)value;
        }
        public int ContentTop
        {
            get => _contentTop;
            set => _contentTop = (byte)value;
        }
        public int ContentRight
        {
            get => _contentRight;
            set => _contentRight = (byte)value;
        }
        public int ContentBottom
        {
            get => _contentBottom;
            set => _contentBottom = (byte)value;
        }
        public void SetContentOffsets(byte contentLeft, byte contentTop, byte contentRight, byte contentBottom)
        {
            _contentLeft = contentLeft;
            _contentTop = contentTop;
            _contentRight = contentRight;
            _contentBottom = contentBottom;
        }
        public void SetContentOffsets(byte allside)
        {
            _contentLeft = allside;
            _contentTop = allside;
            _contentRight = allside;
            _contentBottom = allside;
        }

        public Color BackColor
        {
            get => _backColor;
            set
            {
                if (_backColor == value) return;

                _backColor = value;
                if (this.HasParentLink)
                {
                    this.InvalidateGraphics();
                }
            }
        }
#if DEBUG
        bool _dbugBorderBreak;
#endif
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor == value) return;
                _borderColor = value;
#if DEBUG
                if (value.A > 0)
                {
                    _dbugBorderBreak = true;
                }
#endif

                if (this.HasParentLink)
                {
                    this.InvalidateGraphics();
                }
            }
        }
        protected override void DrawBoxContent(DrawBoard canvas, Rectangle updateArea)
        {
#if DEBUG
            if (this.dbugBreak)
            {
            }
#endif


            if (this.MayHasViewport)
            {
                //TODO: review here
                //start pos of background fill
                //(0,0) 
                //(viewportX,viewportY)
                //tile or limit
                canvas.FillRectangle(BackColor, ViewportLeft, ViewportTop, this.Width, this.Height);
            }
            else
            {
                canvas.FillRectangle(BackColor, 0, 0, this.Width, this.Height);
            }
            //border is over background color
#if DEBUG
            if (_dbugBorderBreak)
            {
            }
#endif           

            //default content layer
            this.DrawDefaultLayer(canvas, ref updateArea);

            if (_hasSomeBorderW && _borderColor.A > 0)
            {
                canvas.DrawRectangle(_borderColor, 0, 0, this.Width, this.Height);//test
            }

#if DEBUG
            //canvas.dbug_DrawCrossRect(PixelFarm.Drawing.Color.Black,
            //    new Rectangle(0, 0, this.Width, this.Height));

            //canvas.dbug_DrawCrossRect(PixelFarm.Drawing.Color.Black,
            //   new Rectangle(updateArea.Left, updateArea.Top, updateArea.Width, updateArea.Height));
#endif
        }
    }

    public class DoubleBufferCustomRenderBox : CustomRenderBox
    {
        DrawboardBuffer _builtInBackBuffer;
        bool _hasAccumRect;
        Rectangle _invalidateRect;
        bool _enableDoubleBuffer;
        public DoubleBufferCustomRenderBox(RootGraphic rootgfx, int width, int height)
          : base(rootgfx, width, height)
        {
            NeedInvalidateRectEvent = true;
        }
        public bool EnableDoubleBuffer
        {
            get => _enableDoubleBuffer;
            set
            {
                _enableDoubleBuffer = value;
            }
        }


        protected override void OnInvalidateGraphicsNoti(bool fromMe, ref Rectangle totalBounds)
        {
            if (_builtInBackBuffer != null)
            {
                //TODO: review here,
                //in this case, we copy to another rect
                //since we don't want the offset to effect the total bounds 
#if DEBUG
                if (totalBounds.Width == 150)
                {
                    
                }
                System.Diagnostics.Debug.WriteLine("noti, fromMe=" + fromMe + ",bounds" + totalBounds);
#endif
                if (!fromMe)
                {
                    totalBounds.Offset(-this.X, -this.Y);
                }

                _builtInBackBuffer.IsValid = false;

                if (!_hasAccumRect)
                {
                    _invalidateRect = totalBounds;
                    _hasAccumRect = true;
                }
                else
                {
                    _invalidateRect = Rectangle.Union(_invalidateRect, totalBounds);
                }
            }
            else
            {
                totalBounds.Offset(this.X, this.Y);
            }
            //base.OnInvalidateGraphicsNoti(totalBounds);//skip
        }
        protected override void DrawBoxContent(DrawBoard canvas, Rectangle updateArea)
        {
            if (_enableDoubleBuffer)
            {
                MicroPainter painter = new MicroPainter(canvas);
                if (_builtInBackBuffer == null)
                {
                    _builtInBackBuffer = painter.CreateOffscreenDrawBoard(this.Width, this.Height);
                }

                if (!_builtInBackBuffer.IsValid)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("double_buffer_update:" + this.dbug_obj_id + "," + _invalidateRect.ToString());
#endif
                    float backupViewportW = painter.ViewportWidth; //backup
                    float backupViewportH = painter.ViewportHeight; //backup
                    painter.AttachTo(_builtInBackBuffer); //*** switch to builtInBackbuffer 
                    painter.SetViewportSize(this.Width, this.Height);
                    if (!_hasAccumRect)
                    {
                        _invalidateRect = new Rectangle(0, 0, Width, Height);
                    }


                    if (painter.PushLocalClipArea(
                        _invalidateRect.Left, _invalidateRect.Top,
                        _invalidateRect.Width, _invalidateRect.Height))
                    {
#if DEBUG
                        //for debug , test clear with random color
                        //another useful technique to see latest clear area frame-by-frame => use random color
                        //painter.Clear(Color.FromArgb(255, dbugRandom.Next(0, 255), dbugRandom.Next(0, 255), dbugRandom.Next(0, 255)));

                        canvas.Clear(Color.White);
#else
                        canvas.Clear(Color.White);
#endif

                        base.DrawBoxContent(canvas, updateArea);
                    }

                    painter.PopLocalClipArea();
                    //
                    _builtInBackBuffer.IsValid = true;
                    _hasAccumRect = false;

                    painter.AttachToNormalBuffer();//*** switch back
                    painter.SetViewportSize(backupViewportW, backupViewportH);//restore viewport size
                }

#if DEBUG
                else
                {
                    System.Diagnostics.Debug.WriteLine("double_buffer_update:" + dbug_obj_id + " use cache");
                }
#endif
                painter.DrawImage(_builtInBackBuffer.GetImage(), 0, 0, this.Width, this.Height);
            }
            else
            {
                base.DrawBoxContent(canvas, updateArea);
            }
        }


        struct MicroPainter
        {
            float _viewportWidth;
            float _viewportHeight;
            public readonly DrawBoard _drawBoard;
            public MicroPainter(DrawBoard drawBoard)
            {
                _viewportWidth = 0;
                _viewportHeight = 0;
                _drawBoard = drawBoard;
            }
            public float ViewportWidth => _drawBoard.Width;
            public float ViewportHeight => _drawBoard.Height;

            public DrawboardBuffer CreateOffscreenDrawBoard(int width, int height)
            {
                return _drawBoard.CreateBackbuffer(width, height);
            }
            public void AttachTo(DrawboardBuffer attachToBackbuffer)
            {
                //save  
                _drawBoard.AttachToBackBuffer(attachToBackbuffer);
            }
            public void SetViewportSize(float width, float height)
            {
                _viewportWidth = width;
                _viewportHeight = height;
            }
            internal bool PushLocalClipArea(float left, float top, float w, float h)
            {
                Rectangle currentClip = _drawBoard.CurrentClipRect;
                return _drawBoard.PushClipAreaRect((int)left, (int)top, (int)w, (int)h, ref currentClip);
            }
            public void AttachToNormalBuffer()
            {
                _drawBoard.SwitchBackToDefaultBuffer(null);
            }
            internal void PopLocalClipArea()
            {
                //return; 
                _drawBoard.PopClipAreaRect();
            }
            internal Rectangle CurrentClipRect => _drawBoard.CurrentClipRect;
            public void DrawImage(Image img, float x, float y, float w, float h)
            {
                _drawBoard.DrawImage(img, new RectangleF(x, y, w, h));
            }
        }

    }
}