﻿//Apache2, 2014-present, WinterDev

using System;
using System.Collections.Generic;
using PixelFarm.Drawing;
using PixelFarm.Drawing.WinGdi;
namespace LayoutFarm.UI.GdiPlus
{
    class GdiPlusCanvasViewport : CanvasViewport, IDisposable
    {

        GdiPlusDrawBoard _drawBoard;
        public GdiPlusCanvasViewport(RootGraphic rootgfx, Size viewportSize) :
            base(rootgfx, viewportSize)
        {
            SetupRenderSurfaceAndDrawBoard();
            this.CalculateCanvasPages();
        }
        void SetupRenderSurfaceAndDrawBoard()
        {
            GdiPlusRenderSurface gdiRenderSurface = new GdiPlusRenderSurface(ViewportWidth, ViewportHeight);
            _drawBoard = new GdiPlusDrawBoard(gdiRenderSurface);
        }
        public void Dispose()
        {
            if (_drawBoard != null)
            {
                _drawBoard.Dispose();
                _drawBoard = null;
            }
        }

#if DEBUG
        //int dbugCount;
#endif
        public override void CanvasInvalidateArea(Rectangle r)
        {
            if (_drawBoard != null && _drawBoard.IntersectsWith(r))
            {
                _drawBoard.Invalidate(r);
            }
#if DEBUG
            //System.Diagnostics.Debug.WriteLine("CanvasInvalidateArea:" + (dbugCount++).ToString() + " " + r.ToString());
#endif
        }
        protected override void ResetViewSize(int viewportWidth, int viewportHeight)
        {
            ResizeAllPages(viewportWidth, viewportHeight);
        }
        protected override void CalculateCanvasPages()
        {
            this.FullMode = true;
        }
        public void PaintMe(IntPtr hdc, Rectangle invalidateArea)
        {
            if (this.IsClosed) { return; }
            //------------------------------------ 

            _rootgfx.PrepareRender();
            //---------------
            _rootgfx.BeginRenderPhase();
#if DEBUG
            _rootgfx.dbug_rootDrawingMsg.Clear();
            _rootgfx.dbug_drawLevel = 0;
#endif
            if (this.FullMode)
            {
                RenderToOutputWindowFullMode(
                    _rootgfx.TopWindowRenderBox, hdc,
                    this.ViewportX, this.ViewportY, this.ViewportWidth, this.ViewportHeight);
            }
            else
            {
                //temp to full mode
                //quadPages.RenderToOutputWindowFullMode(rootGraphics.TopWindowRenderBox, hdc, this.ViewportX, this.ViewportY, this.ViewportWidth, this.ViewportHeight);
                RenderToOutputWindowPartialMode2(
                        _rootgfx.TopWindowRenderBox, hdc,
                        this.ViewportX, this.ViewportY, this.ViewportWidth, this.ViewportHeight, invalidateArea);
            }
            _rootgfx.EndRenderPhase();
#if DEBUG

            RootGraphic visualroot = RootGraphic.dbugCurrentGlobalVRoot;
            if (visualroot.dbug_RecordDrawingChain)
            {
                List<dbugLayoutMsg> outputMsgs = dbugOutputWindow.dbug_rootDocDebugMsgs;
                outputMsgs.Clear();
                outputMsgs.Add(new dbugLayoutMsg(null as RenderElement, "[" + debug_render_to_output_count + "]"));
                visualroot.dbug_DumpRootDrawingMsg(outputMsgs);
                dbugOutputWindow.dbug_InvokeVisualRootDrawMsg();
                debug_render_to_output_count++;
            }


            if (dbugHelper01.dbugVE_HighlightMe != null)
            {
                dbugOutputWindow.dbug_HighlightMeNow(dbugHelper01.dbugVE_HighlightMe.dbugGetGlobalRect());
            }
#endif
        }

        public void PaintMe(IntPtr hdc)
        {
            //paint the content to target hdc

            if (this.IsClosed) { return; }
            //------------------------------------ 
            _rootgfx.PrepareRender();
           
            _rootgfx.BeginRenderPhase();
#if DEBUG
            _rootgfx.dbug_rootDrawingMsg.Clear();
            _rootgfx.dbug_drawLevel = 0;
#endif
            if (this.FullMode)
            {
                RenderToOutputWindowFullMode(
                    _rootgfx.TopWindowRenderBox, hdc,
                    this.ViewportX, this.ViewportY, this.ViewportWidth, this.ViewportHeight);
            }
            else
            {
                //temp to full mode
                //quadPages.RenderToOutputWindowFullMode(rootGraphics.TopWindowRenderBox, hdc, this.ViewportX, this.ViewportY, this.ViewportWidth, this.ViewportHeight);
                RenderToOutputWindowPartialMode(
                     _rootgfx.TopWindowRenderBox, hdc,
                     this.ViewportX, this.ViewportY, this.ViewportWidth, this.ViewportHeight);
            }
            _rootgfx.EndRenderPhase();
#if DEBUG

            RootGraphic visualroot = RootGraphic.dbugCurrentGlobalVRoot;
            if (visualroot.dbug_RecordDrawingChain)
            {
                List<dbugLayoutMsg> outputMsgs = dbugOutputWindow.dbug_rootDocDebugMsgs;
                outputMsgs.Clear();
                outputMsgs.Add(new dbugLayoutMsg(null as RenderElement, "[" + debug_render_to_output_count + "]"));
                visualroot.dbug_DumpRootDrawingMsg(outputMsgs);
                dbugOutputWindow.dbug_InvokeVisualRootDrawMsg();
                debug_render_to_output_count++;
            }


            if (dbugHelper01.dbugVE_HighlightMe != null)
            {
                dbugOutputWindow.dbug_HighlightMeNow(dbugHelper01.dbugVE_HighlightMe.dbugGetGlobalRect());
            }
#endif
        }


        //PRESERVE***
        //        public void PaintMe(GdiPlusDrawBoard mycanvas)
        //        {
        //            if (this.IsClosed) { return; }
        //            //------------------------------------ 

        //            _rootGraphics.PrepareRender();
        //            //---------------
        //            _rootGraphics.IsInRenderPhase = true;
        //#if DEBUG
        //            _rootGraphics.dbug_rootDrawingMsg.Clear();
        //            _rootGraphics.dbug_drawLevel = 0;
        //#endif

        //            mycanvas.OffsetCanvasOrigin(-mycanvas.Left, -mycanvas.Top);
        //            Rectangle rect = mycanvas.Rect;
        //            _rootGraphics.TopWindowRenderBox.DrawToThisCanvas(mycanvas, rect);
        //#if DEBUG
        //            _rootGraphics.TopWindowRenderBox.dbugShowRenderPart(mycanvas, rect);
        //#endif

        //            mycanvas.IsContentReady = true;
        //            mycanvas.OffsetCanvasOrigin(mycanvas.Left, mycanvas.Top);
        //            //if (this.FullMode)
        //            //{
        //            //    quadPages.RenderToOutputWindowFullMode(rootGraphics.TopWindowRenderBox, hdc,
        //            //        this.ViewportX, this.ViewportY, this.ViewportWidth, this.ViewportHeight);
        //            //}
        //            //else
        //            //{
        //            //    //temp to full mode
        //            //    //quadPages.RenderToOutputWindowFullMode(rootGraphics.TopWindowRenderBox, hdc, this.ViewportX, this.ViewportY, this.ViewportWidth, this.ViewportHeight);
        //            //    quadPages.RenderToOutputWindowPartialMode(rootGraphics.TopWindowRenderBox, hdc,
        //            //        this.ViewportX, this.ViewportY, this.ViewportWidth, this.ViewportHeight);

        //            //}
        //            _rootGraphics.IsInRenderPhase = false;
        //#if DEBUG

        //            RootGraphic visualroot = RootGraphic.dbugCurrentGlobalVRoot;
        //            if (visualroot.dbug_RecordDrawingChain)
        //            {
        //                List<dbugLayoutMsg> outputMsgs = dbugOutputWindow.dbug_rootDocDebugMsgs;
        //                outputMsgs.Clear();
        //                outputMsgs.Add(new dbugLayoutMsg(null as RenderElement, "[" + debug_render_to_output_count + "]"));
        //                visualroot.dbug_DumpRootDrawingMsg(outputMsgs);
        //                dbugOutputWindow.dbug_InvokeVisualRootDrawMsg();
        //                debug_render_to_output_count++;
        //            }


        //            if (dbugHelper01.dbugVE_HighlightMe != null)
        //            {
        //                dbugOutputWindow.dbug_HighlightMeNow(dbugHelper01.dbugVE_HighlightMe.dbugGetGlobalRect());
        //            }
        //#endif
        //        }
        //PRESERVE***

        public void RenderToOutputWindowFullMode(
            IRenderElement topWindowRenderBox,
            IntPtr destOutputHdc,
            int viewportX, int viewportY, int viewportWidth, int viewportHeight)
        {

            if (_drawBoard != null && !_drawBoard.IsContentReady)
            {
                UpdateAllArea(_drawBoard, topWindowRenderBox);
            }
            _drawBoard.RenderTo(destOutputHdc, viewportX - _drawBoard.Left,
                          viewportY - _drawBoard.Top,
                          new Rectangle(0, 0,
                          viewportWidth,
                          viewportHeight));
        }
        //-------

        static Stack<UpdateArea> _updateAreaPool = new Stack<UpdateArea>();

        static UpdateArea GetFreeUpdateArea() => (_updateAreaPool.Count == 0) ? new UpdateArea() : _updateAreaPool.Pop();

        static void ReleaseUpdateArea(UpdateArea u)
        {
            u.Reset();
            _updateAreaPool.Push(u);
        }

        //-------
        static void UpdateAllArea(GdiPlusDrawBoard d, IRenderElement topWindowRenderBox)
        {
            int enter_canvasX = d.OriginX;
            int enter_canvasY = d.OriginY;
            d.SetCanvasOrigin(enter_canvasX - d.Left, enter_canvasY - d.Top);

            UpdateArea u = GetFreeUpdateArea();
            u.CurrentRect = d.Rect;
            topWindowRenderBox.Render(d, u);

#if DEBUG
            topWindowRenderBox.dbugShowRenderPart(d, u);
#endif

            d.IsContentReady = true;
            d.SetCanvasOrigin(enter_canvasX, enter_canvasY);//restore

            ReleaseUpdateArea(u);
        }

        static void UpdateInvalidArea(GdiPlusDrawBoard d, IRenderElement rootElement)
        {
            int enter_canvasX = d.OriginX;
            int enter_canvasY = d.OriginY;
            d.SetCanvasOrigin(enter_canvasX - d.Left, enter_canvasY - d.Top);

            UpdateArea u = GetFreeUpdateArea();
            u.CurrentRect = d.InvalidateArea;

            if (u.Width > 0 && u.Height > 0)
            {
                rootElement.Render(d, u);
#if DEBUG
                rootElement.dbugShowRenderPart(d, u);
#endif
            }
            else
            {

            }


            d.IsContentReady = true;
            d.SetCanvasOrigin(enter_canvasX, enter_canvasY);//restore

            ReleaseUpdateArea(u);
        }

        public void RenderToOutputWindowPartialMode(
            IRenderElement renderE,
            IntPtr destOutputHdc,
            int viewportX, int viewportY,
            int viewportWidth, int viewportHeight)
        {
            if (!_drawBoard.IsContentReady)
            {
                UpdateInvalidArea(_drawBoard, renderE);
            }

            Rectangle invalidateArea = _drawBoard.InvalidateArea;

            _drawBoard.RenderTo(destOutputHdc, invalidateArea.Left - _drawBoard.Left, invalidateArea.Top - _drawBoard.Top,
                new Rectangle(invalidateArea.Left -
                    viewportX, invalidateArea.Top - viewportY,
                    invalidateArea.Width, invalidateArea.Height));
            _drawBoard.ResetInvalidateArea();
        }
        public void RenderToOutputWindowPartialMode2(
            IRenderElement renderE,
            IntPtr destOutputHdc,
            int viewportX, int viewportY,
            int viewportWidth, int viewportHeight,
            Rectangle windowMsgInvalidateArea)
        {
            if (!_drawBoard.IsContentReady)
            {
                UpdateInvalidArea(_drawBoard, renderE);
            }

            Rectangle invalidateArea = _drawBoard.InvalidateArea;
            if (invalidateArea.Width == 0 || invalidateArea.Height == 0)
            {
                invalidateArea = windowMsgInvalidateArea;// new Rectangle(0, 0, _pageA.Width, _pageA.Height);
            }

            _drawBoard.RenderTo(destOutputHdc, invalidateArea.Left - _drawBoard.Left, invalidateArea.Top - _drawBoard.Top,
                new Rectangle(invalidateArea.Left -
                    viewportX, invalidateArea.Top - viewportY,
                    invalidateArea.Width, invalidateArea.Height));
            _drawBoard.ResetInvalidateArea();
        }
        public void ResizeAllPages(int newWidth, int newHeight)
        {
            //physicalCanvasCollection.Dispose();
            //physicalCanvasCollection.ResizeAllPages(newWidth, newHeight);
            //if (_pageA != null)
            //{
            //    _pageA.IsUnused = true;
            //    _pageA = null;
            //}
            if (_drawBoard != null)
            {
                if (_drawBoard.Height < newHeight || _drawBoard.Width < newWidth)
                {

                    _drawBoard.Dispose();
                    _drawBoard = null;
                }
                else
                {
                    return;
                }
            }
            //
            SetupRenderSurfaceAndDrawBoard();
        }
        public bool IsValid
        {
            get
            {
                if (_drawBoard != null)
                {
                    if (!_drawBoard.IsContentReady)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}