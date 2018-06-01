﻿//Apache2, 2014-2018, WinterDev

using System;
using PixelFarm.Drawing;
using PixelFarm.Agg;

namespace LayoutFarm.UI
{
    public class SvgRenderElement : RenderElement
    {
        public SvgRenderElement(RootGraphic rootGfx, int width, int height)
            : base(rootGfx, width, height)
        {

        }
        public SvgRenderVx RenderVx { get; set; }
        public override void CustomDrawToThisCanvas(DrawBoard canvas, Rectangle updateArea)
        {
            if (RenderVx != null)
            {
                canvas.DrawRenderVx(RenderVx, this.X, this.Y);
            }
        }
        public override void ResetRootGraphics(RootGraphic rootgfx)
        {

        }
    }


    public class UISprite : UIElement
    {
        int _left;
        int _top;
        int _width;
        int _height;
        bool _hide;

        SvgRenderElement _svgRenderElement;
        SvgRenderVx _svgRenderVx;

#if DEBUG
        static int dbugTotalId;
        public readonly int dbugId = dbugTotalId++;
#endif
        public UISprite(int width, int height)
        {
            this._width = width;
            this._height = height;
            //default for box
            this.AutoStopMouseEventPropagation = true;

        }
        public void LoadSvg(SvgRenderVx renderVx)
        {
            _svgRenderVx = renderVx;
            if (_svgRenderElement != null)
            {
                _svgRenderElement.RenderVx = renderVx;
                RectD bound = renderVx.GetBounds();
                this.SetSize((int)bound.Width, (int)bound.Height);
            }
        }
        protected override void OnMouseDown(UIMouseEventArgs e)
        {
            base.OnMouseDown(e);
        }
        public override void Walk(UIVisitor visitor)
        {

        }
        protected override bool HasReadyRenderElement
        {
            get { return _svgRenderElement != null; }
        }
        public override RenderElement CurrentPrimaryRenderElement
        {
            get { return _svgRenderElement; }
        }
        public override RenderElement GetPrimaryRenderElement(RootGraphic rootgfx)
        {
            if (_svgRenderElement == null)
            {
                _svgRenderElement = new SvgRenderElement(rootgfx, 10, 10);
                _svgRenderElement.SetController(this);
                if (_svgRenderVx != null)
                {
                    _svgRenderElement.RenderVx = _svgRenderVx;
                    RectD bound = _svgRenderVx.GetBounds();
                    this.SetSize((int)bound.Width, (int)bound.Height);
                }
            }
            return _svgRenderElement;

        }


        public virtual void Focus()
        {
            //make this keyboard focusable
            if (this.HasReadyRenderElement)
            {
                //focus
                this.CurrentPrimaryRenderElement.Root.SetCurrentKeyboardFocus(this.CurrentPrimaryRenderElement);
            }
        }
        public virtual void Blur()
        {
            if (this.HasReadyRenderElement)
            {
                //focus
                this.CurrentPrimaryRenderElement.Root.SetCurrentKeyboardFocus(null);
            }
        }

        public virtual void SetLocation(int left, int top)
        {
            this._left = left;
            this._top = top;
            if (this.HasReadyRenderElement)
            {
                this.CurrentPrimaryRenderElement.SetLocation(left, top);
            }
        }
        public Point GetGlobalLocation()
        {
            if (this.CurrentPrimaryRenderElement != null)
            {
                return this.CurrentPrimaryRenderElement.GetGlobalLocation();
            }
            return new Point(this.Left, this.Top);
        }
        public virtual void SetSize(int width, int height)
        {
            this._width = width;
            this._height = height;
            if (this.HasReadyRenderElement)
            {
                this.CurrentPrimaryRenderElement.SetSize(_width, _height);
            }
        }

        public void SetBounds(int left, int top, int width, int height)
        {
            SetLocation(left, top);
            SetSize(width, height);
        }
        public int Left
        {
            get
            {
                if (this.HasReadyRenderElement)
                {
                    return this.CurrentPrimaryRenderElement.X;
                }
                else
                {
                    return this._left;
                }
            }
        }
        public int Top
        {
            get
            {
                if (this.HasReadyRenderElement)
                {
                    return this.CurrentPrimaryRenderElement.Y;
                }
                else
                {
                    return this._top;
                }
            }
        }
        public int Right
        {
            get { return this.Left + Width; }
        }
        public int Bottom
        {
            get { return this.Top + Height; }
        }

        public Point Position
        {
            get
            {
                if (this.HasReadyRenderElement)
                {
                    return new Point(CurrentPrimaryRenderElement.X, CurrentPrimaryRenderElement.Y);
                }
                else
                {
                    return new Point(this._left, this._top);
                }
            }
        }
        public int Width
        {
            get
            {
                if (this.HasReadyRenderElement)
                {
                    return this.CurrentPrimaryRenderElement.Width;
                }
                else
                {
                    return this._width;
                }
            }
        }
        public int Height
        {
            get
            {
                if (this.HasReadyRenderElement)
                {
                    return this.CurrentPrimaryRenderElement.Height;
                }
                else
                {
                    return this._height;
                }
            }
        }

        public override void InvalidateGraphics()
        {
            if (this.HasReadyRenderElement)
            {
                this.CurrentPrimaryRenderElement.InvalidateGraphics();
            }
        }
        public void InvalidateOuterGraphics()
        {
            if (this.CurrentPrimaryRenderElement != null)
            {
                this.CurrentPrimaryRenderElement.InvalidateGraphicBounds();
            }
        }



        public virtual bool Visible
        {
            get { return !this._hide; }
            set
            {
                this._hide = !value;
                if (this.HasReadyRenderElement)
                {
                    this.CurrentPrimaryRenderElement.SetVisible(value);
                }
            }
        }

        public virtual void PerformContentLayout()
        {
        }

        //----------------------------------- 
        public object Tag { get; set; }
        //----------------------------------- 


        protected virtual void Describe(UIVisitor visitor)
        {
            visitor.Attribute("left", this.Left);
            visitor.Attribute("top", this.Top);
            visitor.Attribute("width", this.Width);
            visitor.Attribute("height", this.Height);
        }
        public Rectangle Bounds
        {
            get { return new Rectangle(this.Left, this.Top, this.Width, this.Height); }
        }

    }

    class BackBoardRenderElement : LayoutFarm.CustomWidgets.CustomRenderBox
    {

        DrawBoard _canvas;
        public BackBoardRenderElement(RootGraphic rootgfx, int width, int height)
           : base(rootgfx, width, height)
        {

        }
        protected override void DrawBoxContent(DrawBoard canvas, Rectangle updateArea)
        {
            _canvas = canvas;
            base.DrawBoxContent(canvas, updateArea);

        }
    }
    public class BackDrawBoardUI : LayoutFarm.CustomWidgets.EaseBox
    {
        BackBoardRenderElement _backboardRenderE;
        public BackDrawBoardUI(int w, int h)
            : base(w, h)
        {

        }
        public override void Walk(UIVisitor visitor)
        {

        }
        public override RenderElement GetPrimaryRenderElement(RootGraphic rootgfx)
        {
            if (_backboardRenderE != null)
            {
                return _backboardRenderE;
            }
            _backboardRenderE = new BackBoardRenderElement(rootgfx, this.Width, this.Height);
            _backboardRenderE.NeedClipArea = true;

            SetPrimaryRenderElement(_backboardRenderE);
            BuildChildrenRenderElement(_backboardRenderE);

            return _backboardRenderE;
        }
        public void CopyImageBuffer(DrawBoard canvas, int x, int y, int w, int h)
        {
            //copy content image to specific img buffer

        }
    }

}