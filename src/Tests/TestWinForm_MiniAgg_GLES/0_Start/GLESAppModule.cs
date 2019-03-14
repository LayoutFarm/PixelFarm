﻿//BSD, 2014-present, WinterDev

using System;
using PixelFarm.DrawingGL;

using PixelFarm.Drawing;
using LayoutFarm;
using LayoutFarm.UI;


namespace Mini
{



    class GLESAppModule
    {
        //FOR DEMO PROJECT

        //hardware renderer part=> GLES
        //software renderer part => not specific now

        int _myWidth;
        int _myHeight;
        UISurfaceViewportControl _surfaceViewport;
        RootGraphic _rootGfx;
        //
        DemoUI _demoUI;
        DemoBase _demoBase;
        OpenTK.MyGLControl _glControl;
        public GLESAppModule()
        {
        }
        public void BindSurface(LayoutFarm.UI.UISurfaceViewportControl surfaceViewport)
        {



            _surfaceViewport = surfaceViewport;
            _rootGfx = surfaceViewport.RootGfx;

            //----------------------
            _glControl = surfaceViewport.GetOpenTKControl();
            _myWidth = _glControl.Width;
            _myHeight = _glControl.Height;


            IntPtr hh1 = _glControl.Handle; //ensure that contrl handler is created
            _glControl.MakeCurrent();
        }

        public void LoadExample(DemoBase demoBase)
        {
            _glControl.MakeCurrent();

            GLPainterContext pcx = _surfaceViewport.GetGLRenderSurface();
            GLPainter glPainter = _surfaceViewport.GetGLPainter();

            pcx.SmoothMode = SmoothMode.Smooth;//set anti-alias  

            //create text printer for opengl  
            demoBase.Init();
            _demoBase = demoBase;

            _demoUI = new DemoUI(demoBase, _myWidth, _myHeight);
            _demoUI.SetCanvasPainter(pcx, glPainter);
            //-----------------------------------------------
            //demoBase.SetEssentialGLHandlers(
            //    () => _glControl.SwapBuffers(),
            //    () => _glControl.GetEglDisplay(),
            //    () => _glControl.GetEglSurface()
            //);
            //----------------------------------------------- 

            DemoBase.InvokeGLPainterReady(demoBase, pcx, glPainter);
            //Add to RenderTree
            _rootGfx.AddChild(_demoUI.GetPrimaryRenderElement(_surfaceViewport.RootGfx));
        }
        public void Close()
        {

            _demoBase.CloseDemo();
            if (_surfaceViewport != null)
            {
                _surfaceViewport.Close();
                _surfaceViewport = null;
            }
            _rootGfx = null;
            _glControl = null;
        }
        //This is a simple UIElement for testing only
        class DemoUI : UIElement
        {
            DemoBase _demoBase;
            RenderElement _canvasRenderE;
            int _width;
            int _height;
            GLPainterContext _pcx;
            //
            GLPainter _painter;
            public DemoUI(DemoBase demobase, int width, int height)
            {
                _width = width;
                _height = height;
                _demoBase = demobase;
            }

            public void SetCanvasPainter(GLPainterContext pcx, GLPainter painter)
            {
                _pcx = pcx;
                _painter = painter;
            }

            public override RenderElement CurrentPrimaryRenderElement => _canvasRenderE;

            protected override bool HasReadyRenderElement => _canvasRenderE != null;


            public override RenderElement GetPrimaryRenderElement(RootGraphic rootgfx)
            {
                if (_canvasRenderE == null)
                {

                    var glRenderElem = new GLCanvasRenderElement(rootgfx, _width, _height);
                    glRenderElem.SetPainter(_painter);
                    glRenderElem.SetController(this); //connect to event system
                    glRenderElem.LoadDemo(_demoBase);
                    _canvasRenderE = glRenderElem;


                }
                return _canvasRenderE;
            }

            public override void InvalidateGraphics()
            {

            }

            public override void Walk(UIVisitor visitor)
            {

            }

            //handle event
            protected override void OnMouseDown(UIMouseEventArgs e)
            {
                _demoBase.MouseDown(e.X, e.Y, e.Button == UIMouseButtons.Right);
                base.OnMouseDown(e);
            }
            protected override void OnMouseMove(UIMouseEventArgs e)
            {
                if (e.IsDragging)
                {
                    _canvasRenderE.InvalidateGraphics();
                    _demoBase.MouseDrag(e.X, e.Y);
                    _canvasRenderE.InvalidateGraphics();
                }
                base.OnMouseMove(e);
            }
            protected override void OnMouseUp(UIMouseEventArgs e)
            {
                _demoBase.MouseUp(e.X, e.Y);
                base.OnMouseUp(e);
            }
        }

        class GLCanvasRenderElement : RenderElement, IDisposable
        {
            DemoBase _demo;
            GLPainter _painter;
            public GLCanvasRenderElement(RootGraphic rootgfx, int w, int h)
                : base(rootgfx, w, h)
            {
            }
            public void SetPainter(GLPainter canvasPainter)
            {
                _painter = canvasPainter;
            }
            public void LoadDemo(DemoBase demo)
            {
                _demo = demo;
                if (_painter != null)
                {
                    DemoBase.InvokePainterReady(_demo, _painter);
                }
            }
            public override void CustomDrawToThisCanvas(DrawBoard canvas, Rectangle updateArea)
            {
                //****
                //because our demo may run the 'manual GL code', 
                //(out of state-control of the shader share resource/ current program/ render tree/ 
                //***
                _painter.DetachCurrentShader();
                _demo.Draw(_painter);
            }
            public override void ResetRootGraphics(RootGraphic rootgfx)
            {

            }
            public void Dispose()
            {

            }
        }
    }

}