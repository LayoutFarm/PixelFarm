﻿//MIT, 2014-present, WinterDev

using System;
using System.Collections.Generic;
using OpenTK.Graphics.ES20;
using PixelFarm.Drawing;
using PixelFarm.CpuBlit.VertexProcessing;

namespace PixelFarm.DrawingGL
{

    public class GLRenderSurface : IDisposable
    {

        public struct InnerGLData
        {
            public readonly int TextureId;
            public readonly int FramebufferId;
            public InnerGLData(int frameBufferId, int textureId)
            {
                TextureId = textureId;
                FramebufferId = frameBufferId;
            }
        }

        internal readonly MyMat4 _orthoView;
        internal readonly MyMat4 _orthoFlipY_and_PullDown;
        Framebuffer _frameBuffer;//default = null, system-provide-framebuffer  (primary)


        internal GLRenderSurface(int width, int height, int viewportW, int viewportH, bool isPrimary)
        {
            Width = width;
            Height = height;
            ViewportW = viewportW;
            ViewportH = viewportH;
            IsPrimary = isPrimary;
            //setup viewport size,
            //we need W:H ratio= 1:1 , square viewport
            int max = Math.Max(width, height);
            _orthoView = MyMat4.ortho(0, max, 0, max, 0, 1); //this make our viewport W:H =1:1
                                                             //init ortho 
            _orthoFlipY_and_PullDown = _orthoView *
                                     MyMat4.scale(1, -1) * //flip Y
                                     MyMat4.translate(new OpenTK.Vector3(0, -viewportH, 0)); //pull-down; //init 
            IsValid = true;
        }

        public GLRenderSurface(int width, int height)
            : this(Math.Max(width, height), Math.Max(width, height), width, height, false)
        {
            //max int for 1:1 ratio

            //create seconday render surface (off-screen)
            _frameBuffer = new Framebuffer(width, height);
            IsValid = _frameBuffer.FrameBufferId != 0;
        }

        public void Dispose()
        {
            if (_frameBuffer != null)
            {
                _frameBuffer.Dispose();
                _frameBuffer = null;
            }
            IsValid = false;
        }
        public int Width { get; }
        public int Height { get; }
        public int ViewportW { get; }
        public int ViewportH { get; }
        public bool IsPrimary { get; }
        public bool IsValid { get; private set; }

        internal int TextureId => (_frameBuffer == null) ? 0 : _frameBuffer.TextureId;
        internal int FramebufferId => (_frameBuffer == null) ? 0 : _frameBuffer.FrameBufferId;

        public GLBitmap GetGLBitmap() => (_frameBuffer == null) ? null : _frameBuffer.GetGLBitmap();

        public InnerGLData GetInnerGLData() => (_frameBuffer != null) ? new InnerGLData(_frameBuffer.FrameBufferId, _frameBuffer.TextureId) : new InnerGLData();

        internal void MakeCurrent() => _frameBuffer?.MakeCurrent();

        internal void ReleaseCurrent(bool updateTexture)
        {
            if (_frameBuffer != null)
            {
                if (updateTexture)
                {
                    _frameBuffer.UpdateTexture();
                }
                _frameBuffer.ReleaseCurrent();
            }
        }

    }



    /// <summary>
    /// GLES2 render Context, This is not intended to be used directly from your code
    /// </summary>
    public sealed class GLPainterContext
    {
        SmoothLineShader _smoothLineShader;
        InvertAlphaLineSmoothShader _invertAlphaFragmentShader;
        BasicFillShader _basicFillShader;

        RectFillShader _rectFillShader;
        RadialGradientFillShader _radialGradientShader;

        GlyphImageStecilShader _glyphStencilShader;
        BGRImageTextureShader _bgrImgTextureShader;
        BGRAImageTextureShader _bgraImgTextureShader;
        BGRAImageTextureWithWhiteTransparentShader _bgraImgTextureWithWhiteTransparentShader;
        ImageTextureWithSubPixelRenderingShader _textureSubPixRendering;
        RGBATextureShader _rgbaTextureShader;
        BlurShader _blurShader;
        Conv3x3TextureShader _conv3x3TextureShader;
        MsdfShader _msdfShader;
        //MsdfShaderSubpix _msdfSubPixelRenderingShader;
        SingleChannelSdf _sdfShader;
        //-----------------------------------------------------------
        ShaderSharedResource _shareRes;
        RenderSurfaceOrientation _originKind;

        GLRenderSurface _primaryRenderSx;
        GLRenderSurface _rendersx;
        int _canvasOriginX = 0;
        int _canvasOriginY = 0;
        int _vwHeight = 0;

        ICoordTransformer _coordTransformer;
        MyMat4 _customCoordTransformer;

        //
        TessTool _tessTool;
        SmoothBorderBuilder _smoothBorderBuilder = new SmoothBorderBuilder();
        int _painterContextId;
        FillingRule _fillingRule;
        Tesselate.Tesselator.WindingRuleType _tessWindingRuleType = Tesselate.Tesselator.WindingRuleType.NonZero;//default

        internal GLPainterContext(int painterContextId, int w, int h, int viewportW, int viewportH)
        {
            //-------------
            //y axis points upward (like other OpenGL)
            //x axis points to right.
            //please NOTE: left lower corner of the canvas is (0,0)
            //------------- 
            _painterContextId = painterContextId;
            //1.
            _shareRes = new ShaderSharedResource();//1.
            //----------------------------------------------------------------------- 
            //2.
            _primaryRenderSx = new GLRenderSurface(w, h, viewportW, viewportH, true);
            _rendersx = _primaryRenderSx;
            GL.Viewport(0, 0, _primaryRenderSx.Width, _primaryRenderSx.Height);
            _vwHeight = _primaryRenderSx.ViewportH;
            _shareRes.OrthoView = (_originKind == RenderSurfaceOrientation.LeftTop) ?
                                                        _rendersx._orthoFlipY_and_PullDown :
                                                        _rendersx._orthoView;
            //----------------------------------------------------------------------- 
            //3.
            _basicFillShader = new BasicFillShader(_shareRes);
            _smoothLineShader = new SmoothLineShader(_shareRes);
            _rectFillShader = new RectFillShader(_shareRes);
            _radialGradientShader = new RadialGradientFillShader(_shareRes);
            //
            _bgrImgTextureShader = new BGRImageTextureShader(_shareRes); //BGR eg. from Win32 surface
            _bgraImgTextureShader = new BGRAImageTextureShader(_shareRes);

            _bgraImgTextureWithWhiteTransparentShader = new BGRAImageTextureWithWhiteTransparentShader(_shareRes);
            _rgbaTextureShader = new RGBATextureShader(_shareRes);
            //
            _glyphStencilShader = new GlyphImageStecilShader(_shareRes);
            _textureSubPixRendering = new ImageTextureWithSubPixelRenderingShader(_shareRes);
            _blurShader = new BlurShader(_shareRes);
            //
            _invertAlphaFragmentShader = new InvertAlphaLineSmoothShader(_shareRes); //used with stencil  ***

            _conv3x3TextureShader = new Conv3x3TextureShader(_shareRes);
            _msdfShader = new MsdfShader(_shareRes);
            //_msdfSubPixelRenderingShader = new MsdfShaderSubpix(_shareRes);
            _sdfShader = new SingleChannelSdf(_shareRes);
            //-----------------------------------------------------------------------
            //tools
            _tessTool = new PixelFarm.CpuBlit.VertexProcessing.TessTool();
            //-----------------------------------------------------------------------

            //GL.Enable(EnableCap.CullFace);
            //GL.FrontFace(FrontFaceDirection.Cw);
            //GL.CullFace(CullFaceMode.Back); 

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);//original **

            //GL.BlendFunc(BlendingFactorSrc.SrcColor, BlendingFactorDest.One);// not apply alpha to src
            //GL.BlendFuncSeparate(BlendingFactorSrc.SrcColor, BlendingFactorDest.OneMinusSrcAlpha,
            //                     BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            //GL.BlendFuncSeparate(BlendingFactorSrc.SrcColor, BlendingFactorDest.OneMinusSrcColor, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.Zero);

            GL.ClearColor(1, 1, 1, 1);
            //-------------------------------------------------------------------------------
            //GL.Viewport(0, 0, width, height);
            //-------------------------------------------------------------------------------
            //1. original GLES (0,0) is on left-lower.
            //2. but our GLRenderSurface use Html5Canvas/SvgCanvas coordinate model 
            // so (0,0) is on LEFT-UPPER => so we need to FlipY

            OriginKind = RenderSurfaceOrientation.LeftTop;
            EnableClipRect();

        }



        static Dictionary<int, GLPainterContext> s_registeredPainterContexts = new Dictionary<int, GLPainterContext>();
        static int s_painterContextTotalId;

        /// <summary>
        /// create primary GL render context
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="viewportW"></param>
        /// <param name="viewportH"></param>
        /// <returns></returns>
        public static GLPainterContext Create(int w, int h, int viewportW, int viewportH, bool register)
        {
            //the canvas may need some init modules
            //so we start the canvass internaly here
            if (!register)
            {
                return new GLPainterContext(0, w, h, viewportW, viewportH);
            }
            else
            {
                int painterContextId = ++s_painterContextTotalId;
                var newPainterContext = new GLPainterContext(painterContextId, w, h, viewportW, viewportH);
                s_registeredPainterContexts.Add(painterContextId, newPainterContext);
                return newPainterContext;
            }
        }

        public static bool TryGetRegisteredPainterContext(int painterContextId, out GLPainterContext found)
        {
            return s_registeredPainterContexts.TryGetValue(painterContextId, out found);
        }
        public void AttachToRenderSurface(GLRenderSurface rendersx, bool updateTextureResult = true)
        {
            if (_rendersx == rendersx)
            {
                //same
                return;
            }
            //else
            //reset this ...

            //detach prev first
            if (_primaryRenderSx != _rendersx)
            {
                _rendersx.ReleaseCurrent(updateTextureResult);
            }

            if (rendersx == null)
            {
                rendersx = _primaryRenderSx;
            }
            //
            _rendersx = rendersx;
            GL.Viewport(0, 0, rendersx.Width, rendersx.Height);
            _vwHeight = rendersx.ViewportH;
            _shareRes.OrthoView = (_originKind == RenderSurfaceOrientation.LeftTop) ?
                                                        _rendersx._orthoFlipY_and_PullDown :
                                                        _rendersx._orthoView;
            rendersx.MakeCurrent();
        }

        public GLRenderSurface CurrentRenderSurface => _rendersx;
        public int OriginX => _canvasOriginX;
        public int OriginY => _canvasOriginY;
        public ICoordTransformer CoordTransformer
        {
            get => _coordTransformer;
            set
            {

                if (value != null)
                {
                    switch (value.Kind)
                    {
                        case CoordTransformerKind.Affine3x2:
                            //current version we support only 
                            {
                                Affine aff1 = (Affine)value;
                                float[] elems = aff1.Get3x3MatrixElements();

                                _customCoordTransformer = new MyMat4(
                                    elems[0], elems[1], elems[2], 0,
                                    elems[3], elems[4], elems[5], 0,
                                    elems[6], elems[7], elems[8], 0,
                                    0, 0, 0, 1
                                    );

                                _coordTransformer = value;
                            }
                            break;
                        default:
                            //this version support only affine 3x2
                            _coordTransformer = null;
                            break;
                    }
                }
                else
                {
                    _coordTransformer = null;
                }
            }
        }

        public FillingRule FillingRule
        {
            get => _fillingRule;
            set
            {
                _fillingRule = value;
                switch (value)
                {
                    default://??
                    case FillingRule.NonZero:
                        _tessWindingRuleType = Tesselate.Tesselator.WindingRuleType.NonZero;
                        break;
                    case FillingRule.EvenOdd:
                        _tessWindingRuleType = Tesselate.Tesselator.WindingRuleType.Odd;
                        break;
                }
            }
        }
        public RenderSurfaceOrientation OriginKind
        {
            get
            {
                return _originKind;
            }
            set
            {
                _originKind = value;
                if (_rendersx != null)
                {
                    _shareRes.OrthoView = (_originKind == RenderSurfaceOrientation.LeftTop) ?
                                                _rendersx._orthoFlipY_and_PullDown :
                                                _rendersx._orthoView;
                }
            }
        }
        internal GLBitmap ResolveForGLBitmap(Image image)
        {
            //1.
            GLBitmap glBmp = image as GLBitmap;
            if (glBmp != null)
            {
                return glBmp;
            }
            //2. 
            glBmp = Image.GetCacheInnerImage(image) as GLBitmap;
            if (glBmp != null)
            {
                return glBmp;
            }
            //
            BitmapBufferProvider imgBinder = image as BitmapBufferProvider;
            if (imgBinder != null)
            {

                glBmp = new GLBitmap(imgBinder);

            }
            else if (image is CpuBlit.MemBitmap)
            {
                glBmp = new GLBitmap((CpuBlit.MemBitmap)image, false);


            }
            else
            {
                ////TODO: review here
                ////we should create 'borrow' method ? => send direct exact ptr to img buffer 
                ////for now, create a new one -- after we copy we, don't use it 
                //var req = new Image.ImgBufferRequestArgs(32, Image.RequestType.Copy);
                //image.RequestInternalBuffer(ref req);
                //int[] copy = req.OutputBuffer32;
                //glBmp = new GLBitmap(image.Width, image.Height, copy, req.IsInvertedImage);
                return null;
            }

            Image.SetCacheInnerImage(image, glBmp, true);//***
            return glBmp;
        }

        public unsafe void CopyPixels(int x, int y, int w, int h, IntPtr outputBuffer)
        {
            GL.ReadPixels(x, y, w, h,
               PixelFormat.AbgrExt,
               PixelType.UnsignedByte,
               outputBuffer);
        }
        //
        public int ViewportWidth => _rendersx.ViewportW;
        public int ViewportHeight => _rendersx.ViewportH;
        //
        public int CanvasWidth => _rendersx.Width;
        public int CanvasHeight => _rendersx.Height;
        //
        public void Dispose()
        {
        }
        public void DetachCurrentShader()
        {
            _shareRes._currentShader = null;
        }
        //
        public SmoothMode SmoothMode { get; set; }
        public PixelFarm.Drawing.Color FontFillColor { get; set; }

        public void Clear()
        {
            GL.ClearStencil(0);
            //actual clear here !
            GL.Clear(ClearBufferMask.ColorBufferBit |
                ClearBufferMask.DepthBufferBit |
                ClearBufferMask.StencilBufferBit);
        }
        public void Clear(PixelFarm.Drawing.Color c)
        {

            GL.ClearColor(
               (float)c.R / 255f,
               (float)c.G / 255f,
               (float)c.B / 255f,
               (float)c.A / 255f);
            GL.ClearStencil(0);
            //actual clear here !
            GL.Clear(ClearBufferMask.ColorBufferBit |
                ClearBufferMask.DepthBufferBit |
                ClearBufferMask.StencilBufferBit);
        }

        public void ClearColorBuffer()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }
        public float StrokeWidth
        {
            get => _shareRes._strokeWidth;
            set => _shareRes._strokeWidth = value;
        }
        public Drawing.Color StrokeColor
        {
            get => _shareRes.StrokeColor;
            set => _shareRes.StrokeColor = value;
        }
        public void DrawLine(float x1, float y1, float x2, float y2)
        {
            switch (this.SmoothMode)
            {
                case SmoothMode.Smooth:
                    {
                        if (y1 == y2)
                        {
                            _basicFillShader.DrawLine(x1, y1, x2, y2, StrokeColor);
                        }
                        else
                        {
                            _smoothLineShader.DrawLine(x1, y1, x2, y2);
                        }
                    }
                    break;
                default:
                    {
                        if (StrokeWidth == 1)
                        {
                            _basicFillShader.DrawLine(x1, y1, x2, y2, StrokeColor);
                        }
                        else
                        {
                            //TODO: review stroke with for smooth line shader again
                            _shareRes._strokeWidth = this.StrokeWidth;
                            _smoothLineShader.DrawLine(x1, y1, x2, y2);
                        }
                    }
                    break;
            }
        }
        //-----------------------------------------------------------------
        public void BlitRenderSurface(GLRenderSurface srcRenderSx, float left, float top, bool isFlipped = true)
        {
            //IMPORTANT: (left,top) != (x,y) 
            //IMPORTANT: left,top position need to be adjusted with 
            //Canvas' origin kind
            //see https://github.com/PaintLab/PixelFarm/issues/43
            //-----------
            if (OriginKind == RenderSurfaceOrientation.LeftTop)
            {
                //***
                top += srcRenderSx.Height;
            }
            //...
            _rgbaTextureShader.Render(srcRenderSx.TextureId, left, top, srcRenderSx.Width, srcRenderSx.Height, isFlipped);
        }

        public void DrawImage(GLBitmap bmp, float left, float top)
        {

            DrawImage(bmp, left, top, bmp.Width, bmp.Height);
        }
        //-----------------------------------------------------------------

        public void DrawSubImage(GLBitmap bmp, float srcLeft, float srcTop, float srcW, float srcH, float targetLeft, float targetTop)
        {
            if (OriginKind == RenderSurfaceOrientation.LeftTop) //***
            {
                targetTop += srcH; //***
            }

            //
            if (bmp.IsBigEndianPixel)
            {
                _rgbaTextureShader.DrawSubImage(bmp, srcLeft, srcTop, srcW, srcH, targetLeft, targetTop);
            }
            else
            {

                if (bmp.BitmapFormat == PixelFarm.Drawing.BitmapBufferFormat.BGR)
                {
                    _bgrImgTextureShader.DrawSubImage(bmp, srcLeft, srcTop, srcW, srcH, targetLeft, targetTop);
                }
                else
                {
                    _bgraImgTextureShader.DrawSubImage(bmp, srcLeft, srcTop, srcW, srcH, targetLeft, targetTop);
                }
            }
        }
        public void DrawSubImage(GLBitmap bmp, ref PixelFarm.Drawing.Rectangle srcRect, float targetLeft, float targetTop)
        {
            DrawSubImage(bmp, srcRect.Left, srcRect.Top, srcRect.Width, srcRect.Height, targetLeft, targetTop);
        }

        public void DrawSubImage(GLBitmap bmp, ref PixelFarm.Drawing.Rectangle srcRect, float targetLeft, float targetTop, float scale)
        {
            if (OriginKind == RenderSurfaceOrientation.LeftTop) //***
            {
                //***
                targetTop += srcRect.Height * scale;  //***
            }

            //
            if (bmp.IsBigEndianPixel)
            {
                _rgbaTextureShader.DrawSubImage(bmp, srcRect.Left, srcRect.Top, srcRect.Width, srcRect.Height, targetLeft, targetTop, scale);
            }
            else
            {
                if (bmp.BitmapFormat == PixelFarm.Drawing.BitmapBufferFormat.BGR)
                {
                    _bgrImgTextureShader.DrawSubImage(bmp, srcRect.Left, srcRect.Top, srcRect.Width, srcRect.Height, targetLeft, targetTop, scale);
                }
                else
                {
                    _bgraImgTextureShader.DrawSubImage(bmp, srcRect.Left, srcRect.Top, srcRect.Width, srcRect.Height, targetLeft, targetTop, scale);
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------------------
        public void DrawSubImageWithMsdf(GLBitmap bmp, ref PixelFarm.Drawing.Rectangle r, float targetLeft, float targetTop)
        {
            //we expect that the bmp supports alpha value

            if (OriginKind == RenderSurfaceOrientation.LeftTop)
            {
                //***
                targetTop += r.Height;
            }

            if (bmp.IsBigEndianPixel)
            {
                _msdfShader.DrawSubImage(bmp, r.Left, r.Top, r.Width, r.Height, targetLeft, targetTop);
            }
            else
            {
                _msdfShader.DrawSubImage(bmp, r.Left, r.Top, r.Width, r.Height, targetLeft, targetTop);
            }
        }
        public void DrawSubImageWithMsdf(GLBitmap bmp, ref PixelFarm.Drawing.Rectangle r, float targetLeft, float targetTop, float scale)
        {
            //we expect that the bmp supports alpha value

            if (OriginKind == RenderSurfaceOrientation.LeftTop)
            {
                //***
                targetTop += r.Height;
            }

            if (bmp.IsBigEndianPixel)
            {
                _msdfShader.DrawSubImage(bmp, r.Left, r.Top, r.Width, r.Height, targetLeft, targetTop, scale);
            }
            else
            {
                _msdfShader.DrawSubImage(bmp, r.Left, r.Top, r.Width, r.Height, targetLeft, targetTop, scale);
            }
        }
        public void DrawSubImageWithMsdf(GLBitmap bmp, float[] coords, float scale)
        {

            if (bmp.IsBigEndianPixel)
            {
                _msdfShader.DrawSubImages(bmp, coords, scale);
            }
            else
            {
                _msdfShader.DrawSubImages(bmp, coords, scale);
            }
        }


        public void DrawImage(GLBitmap bmp,
            float left, float top, float w, float h)
        {
            //IMPORTANT: (left,top) != (x,y) 
            //IMPORTANT: left,top position need to be adjusted with 
            //Canvas' origin kind
            //see https://github.com/PaintLab/PixelFarm/issues/43
            //-----------
            if (OriginKind == RenderSurfaceOrientation.LeftTop)
            {
                //***
                top += h;
            }

            if (bmp.IsBigEndianPixel)
            {

                _rgbaTextureShader.Render(bmp, left, top, w, h);
            }
            else
            {
                if (bmp.BitmapFormat == PixelFarm.Drawing.BitmapBufferFormat.BGR)
                {
                    _bgrImgTextureShader.Render(bmp, left, top, w, h);
                }
                else
                {
                    _bgraImgTextureShader.Render(bmp, left, top, w, h);
                }
            }
        }
        public void DrawImageToQuad(GLBitmap bmp, PixelFarm.CpuBlit.VertexProcessing.Affine affine)
        {
            float[] quad = null;
            if (OriginKind == RenderSurfaceOrientation.LeftTop)
            {
                //left,top (NOT x,y) 
                quad = new float[]
                {
                   0, 0, //left-top
                   bmp.Width , 0, //right-top
                   bmp.Width , bmp.Height , //right-bottom
                   0, bmp.Height  //left bottom
                };
            }
            else
            {
                quad = new float[]
                {
                  0, 0, //left-top
                  bmp.Width , 0, //right-top
                  bmp.Width , -bmp.Height , //right-bottom
                  0, -bmp.Height  //left bottom
                };
            }

            affine.Transform(ref quad[0], ref quad[1]);
            affine.Transform(ref quad[2], ref quad[3]);
            affine.Transform(ref quad[4], ref quad[5]);
            affine.Transform(ref quad[6], ref quad[7]);


            DrawImageToQuad(bmp,
                            new PixelFarm.Drawing.PointF(quad[0], quad[1]),
                            new PixelFarm.Drawing.PointF(quad[2], quad[3]),
                            new PixelFarm.Drawing.PointF(quad[4], quad[5]),
                            new PixelFarm.Drawing.PointF(quad[6], quad[7]));
        }
        public void DrawImageToQuad(GLBitmap bmp,
            PointF left_top,
            PointF right_top,
            PointF right_bottom,
            PointF left_bottom)
        {


            bool flipY = false;
            if (OriginKind == RenderSurfaceOrientation.LeftTop)
            {
                flipY = true;
                //***
                //y_adjust = -bmp.Height;
            }

            if (bmp.IsBigEndianPixel)
            {

                _rgbaTextureShader.Render(bmp,
                    left_top.X, left_top.Y,
                    right_top.X, right_top.Y,
                    right_bottom.X, right_bottom.Y,
                    left_bottom.X, left_bottom.Y, flipY);
            }
            else
            {
                if (bmp.BitmapFormat == PixelFarm.Drawing.BitmapBufferFormat.BGR)
                {
                    _bgrImgTextureShader.Render(bmp,
                        left_top.X, left_top.Y,
                        right_top.X, right_top.Y,
                        right_bottom.X, right_bottom.Y,
                        left_bottom.X, left_bottom.Y, flipY);
                }
                else
                {
                    _bgraImgTextureShader.Render(bmp,
                        left_top.X, left_top.Y,
                        right_top.X, right_top.Y,
                        right_bottom.X, right_bottom.Y,
                        left_bottom.X, left_bottom.Y, flipY);
                }
            }
        }
        public void DrawGlyphImageWithSubPixelRenderingTechnique(GLBitmap bmp, float left, float top)
        {
            PixelFarm.Drawing.Rectangle srcRect = new Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            DrawGlyphImageWithSubPixelRenderingTechnique(bmp, ref srcRect, left, top, 1);
        }
        /// <summary>
        /// draw glyph image with transparent
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void DrawGlyphImage(GLBitmap bmp, float x, float y)
        {
            //TODO: review x,y or left,top 
            _bgraImgTextureWithWhiteTransparentShader.Render(bmp, x, y, bmp.Width, bmp.Height);
        }
        public void DrawGlyphImageWithStecil(GLBitmap bmp, ref PixelFarm.Drawing.Rectangle srcRect, float targetLeft, float targetTop, float scale)
        {
            if (OriginKind == RenderSurfaceOrientation.LeftTop) //***
            {
                //***
                targetTop += srcRect.Height;  //***
            }

            _glyphStencilShader.SetCurrent();
            _glyphStencilShader.SetColor(this.FontFillColor);
            _glyphStencilShader.DrawSubImage(bmp, srcRect.Left, srcRect.Top, srcRect.Width, srcRect.Height, targetLeft, targetTop);
        }

        public void DrawGlyphImageWithStecil_VBO(TextureCoordVboBuilder vboBuilder)
        {
            _glyphStencilShader.SetCurrent();
            _glyphStencilShader.SetColor(this.FontFillColor);
            _glyphStencilShader.DrawWithVBO(vboBuilder);
        }

        public void DrawGlyphImageWithCopy_VBO(TextureCoordVboBuilder vboBuilder)
        {
            _bgraImgTextureShader.DrawWithVBO(vboBuilder);
        }
        public void LoadTexture(GLBitmap bmp)
        {
            _textureSubPixRendering.LoadGLBitmap(bmp);
            _textureSubPixRendering.IsBigEndian = bmp.IsBigEndianPixel;
            _textureSubPixRendering.SetColor(this.FontFillColor);
            _textureSubPixRendering.SetIntensity(1f);
        }
        public void SetAssociatedTextureInfo(GLBitmap bmp)
        {
            _textureSubPixRendering.SetAssociatedTextureInfo(bmp);
        }

        /// <summary>
        ///Technique2: draw glyph by glyph
        /// </summary>
        /// <param name="srcRect"></param>
        /// <param name="targetLeft"></param>
        /// <param name="targetTop"></param>
        /// <param name="scale"></param>
        public void DrawGlyphImageWithSubPixelRenderingTechnique2_GlyphByGlyph(
          ref Drawing.Rectangle srcRect,
          float targetLeft,
          float targetTop,
          float scale)
        {
            if (OriginKind == RenderSurfaceOrientation.LeftTop) //***
            {
                //***
                targetTop += srcRect.Height;  //***
            }
            _textureSubPixRendering.DrawSubImageWithLcdSubPix(
                srcRect.Left,
                srcRect.Top,
                srcRect.Width,
                srcRect.Height, targetLeft, targetTop);
        }

        public void DrawGlyphImageWithSubPixelRenderingTechnique3_DrawElements(TextureCoordVboBuilder vboBuilder)
        {
            //version 3            
            _textureSubPixRendering.DrawSubImages(vboBuilder);
        }
        public void DrawGlyphImageWithSubPixelRenderingTechnique4_FromLoadedVBO(int count, float x, float y)
        {
            _textureSubPixRendering.NewDrawSubImage4FromCurrentLoadedVBO(count, x, y);
        }


        public void DrawGlyphImageWithSubPixelRenderingTechnique(
            GLBitmap bmp,
            ref PixelFarm.Drawing.Rectangle srcRect,
            float targetLeft,
            float targetTop,
            float scale)
        {

            //
            if (OriginKind == RenderSurfaceOrientation.LeftTop)
            {
                //***
                targetTop += bmp.Height;
            }
            //

            if (bmp.IsBigEndianPixel)
            {
                throw new NotSupportedException();
            }
            else
            {
                _textureSubPixRendering.LoadGLBitmap(bmp);
                _textureSubPixRendering.IsBigEndian = bmp.IsBigEndianPixel;
                _textureSubPixRendering.SetColor(this.FontFillColor);
                _textureSubPixRendering.SetIntensity(1f);
                //-------------------------
                //draw a serie of image***
                //-------------------------

                //TODO: review performance here ***

                //1. B , cyan result
                GL.ColorMask(false, false, true, false);
                _textureSubPixRendering.SetCompo(0);
                _textureSubPixRendering.DrawSubImage(srcRect.Left, srcRect.Top, srcRect.Width, srcRect.Height, targetLeft, targetTop);
                //float subpixel_shift = 1 / 9f;
                //textureSubPixRendering.DrawSubImage(r.Left, r.Top, r.Width, r.Height, targetLeft - subpixel_shift, targetTop); //TODO: review this option
                //---------------------------------------------------
                //2. G , magenta result
                GL.ColorMask(false, true, false, false);
                _textureSubPixRendering.SetCompo(1);
                _textureSubPixRendering.DrawSubImage(srcRect.Left, srcRect.Top, srcRect.Width, srcRect.Height, targetLeft, targetTop);
                //textureSubPixRendering.DrawSubImage(r.Left, r.Top, r.Width, r.Height, targetLeft, targetTop); //TODO: review this option
                //1. R , yellow result 
                _textureSubPixRendering.SetCompo(2);
                GL.ColorMask(true, false, false, false);//             
                _textureSubPixRendering.DrawSubImage(srcRect.Left, srcRect.Top, srcRect.Width, srcRect.Height, targetLeft, targetTop);
                //textureSubPixRendering.DrawSubImage(r.Left, r.Top, r.Width, r.Height, targetLeft + subpixel_shift, targetTop); //TODO: review this option
                //enable all color component
                GL.ColorMask(true, true, true, true);
            }

        }
        //-----------------------------------
        public void DrawImageWithBlurY(GLBitmap bmp, float left, float top)
        {
            if (OriginKind == RenderSurfaceOrientation.LeftTop)
            {
                //***
                top += bmp.Height;
            }
            //TODO: review here not complete 
            _blurShader.IsBigEndian = bmp.IsBigEndianPixel;
            _blurShader.IsHorizontal = false;
            _blurShader.Render(bmp, left, top, bmp.Width, bmp.Height);
        }
        public void DrawImageWithBlurX(GLBitmap bmp, float left, float top)
        {

            if (OriginKind == RenderSurfaceOrientation.LeftTop)
            {
                //***
                top += bmp.Height;
            }

            //TODO: review here
            //not complete
            _blurShader.IsBigEndian = bmp.IsBigEndianPixel;
            _blurShader.IsHorizontal = true;
            _blurShader.Render(bmp, left, top, bmp.Width, bmp.Height);
        }
        public void DrawImageWithConv3x3(GLBitmap bmp, float[] kernel3x3, float top, float left)
        {
            if (OriginKind == RenderSurfaceOrientation.LeftTop)
            {
                //***
                top += bmp.Height;
            }
            _conv3x3TextureShader.IsBigEndian = bmp.IsBigEndianPixel;
            _conv3x3TextureShader.SetBitmapSize(bmp.Width, bmp.Height);
            _conv3x3TextureShader.SetConvolutionKernel(kernel3x3);
            _conv3x3TextureShader.Render(bmp, left, top, bmp.Width, bmp.Height);
        }
        public void DrawImageWithMsdf(GLBitmap bmp, float x, float y)
        {
            //TODO: review x,y or lef,top *** 

            _msdfShader.Render(bmp, x, y, bmp.Width, bmp.Height);
        }
        public void DrawImageWithMsdf(GLBitmap bmp, float x, float y, float scale)
        {
            //TODO: review x,y or left,top *** 
            _msdfShader.Render(bmp, x, y, bmp.Width * scale, bmp.Height * scale);
        }
        public void DrawImageWithMsdf(GLBitmap bmp, float x, float y, float scale, Color c)
        {
            //TODO: review x,y or left,top *** 
            _msdfShader.ForegroundColor = c;
            _msdfShader.Render(bmp, x, y, bmp.Width * scale, bmp.Height * scale);
        }
        public void DrawImageWithSubPixelRenderingMsdf(GLBitmap bmp, float x, float y)
        {
            //TODO: review x,y or lef,top ***
            //_msdfSubPixelRenderingShader.ForegroundColor = PixelFarm.Drawing.Color.Black;
            ////msdfSubPixelRenderingShader.BackgroundColor = PixelFarm.Drawing.Color.Blue;//blue is suite for transparent bg
            //_msdfSubPixelRenderingShader.BackgroundColor = PixelFarm.Drawing.Color.White;//opaque white
            //_msdfSubPixelRenderingShader.Render(bmp, x, y, bmp.Width, bmp.Height);
        }
        public void DrawImageWithSubPixelRenderingMsdf(GLBitmap bmp, float x, float y, float scale)
        {
            //TODO: review x,y or lef,top ***

            //_msdfSubPixelRenderingShader.ForegroundColor = PixelFarm.Drawing.Color.Black;
            ////msdfSubPixelRenderingShader.BackgroundColor = PixelFarm.Drawing.Color.Blue;//blue is suite for transparent bg
            //_msdfSubPixelRenderingShader.BackgroundColor = PixelFarm.Drawing.Color.White;//opaque white
            //_msdfSubPixelRenderingShader.Render(bmp, x, y, bmp.Width * scale, bmp.Height * scale);
        }
        public void DrawImageWithSdf(GLBitmap bmp, float x, float y, float scale)
        {
            //TODO: review x,y or lef,top ***

            _sdfShader.ForegroundColor = PixelFarm.Drawing.Color.Black;
            _sdfShader.Render(bmp, x, y, bmp.Width * scale, bmp.Height * scale);
        }

        //-------------------------------------------------------------------------------
        float[] _rect_coords = new float[8]; //resuable rect coord
        public void FillRect(Drawing.Color color, double left, double top, double width, double height)
        {
            //left,bottom,width,height
            SimpleTessTool.CreateRectTessCoordsTriStrip((float)left, (float)(top + height), (float)width, (float)height, _rect_coords);
            FillTriangleStrip(color, _rect_coords, 4);
        }

        public void FillTriangleStrip(Drawing.Color color, float[] coords, int n)
        {
            _basicFillShader.FillTriangleStripWithVertexBuffer(coords, n, color);
        }
        public void FillTriangleFan(Drawing.Color color, float[] coords, int n)
        {
            unsafe
            {
                fixed (float* head = &coords[0])
                {
                    _basicFillShader.FillTriangleFan(head, n, color);
                }
            }
        }
        //-------------------------------------------------------------------------------
        //RenderVx
        public void FillRenderVx(Drawing.Brush brush, Drawing.RenderVx renderVx)
        {
            PathRenderVx glRenderVx = renderVx as PathRenderVx;
            if (glRenderVx == null) return;
            //
            FillGfxPath(brush, glRenderVx);
        }
        public void FillRenderVx(Drawing.Color color, Drawing.RenderVx renderVx)
        {
            PathRenderVx glRenderVx = renderVx as PathRenderVx;
            if (glRenderVx == null) return;

            FillGfxPath(color, glRenderVx);

        }
        public void DrawRenderVx(Drawing.Color color, Drawing.RenderVx renderVx)
        {
            PathRenderVx glRenderVx = renderVx as PathRenderVx;
            if (glRenderVx == null) return;

            DrawGfxPath(color, glRenderVx);
        }
        internal void FillTessArea(Drawing.Color color, float[] coords, ushort[] indices)
        {
            _basicFillShader.FillTriangles(coords, indices, color);
        }

        VBOStream GetVBOStreamOrBuildIfNotExists(PathRenderVx pathRenderVx)
        {
            VBOStream tessVBOStream = pathRenderVx._tessVBOStream;
            if (tessVBOStream == null)
            {
                //create vbo for this render vx
                pathRenderVx._tessVBOStream = tessVBOStream = new VBOStream();
                pathRenderVx._isTessVBOStreamOwner = true;

                pathRenderVx.CreateAreaTessVBOSegment(tessVBOStream, _tessTool, _tessWindingRuleType);
                pathRenderVx.CreateSmoothBorderTessSegment(tessVBOStream, _smoothBorderBuilder);

                //then render with vbo 
                tessVBOStream.BuildBuffer();
            }
            return tessVBOStream;
        }
        public void FillGfxPath(Drawing.Color color, PathRenderVx pathRenderVx)
        {
            switch (SmoothMode)
            {
                case SmoothMode.No:
                    {
                        if (pathRenderVx._enableVBO)
                        {

                            VBOStream tessVBOStream = GetVBOStreamOrBuildIfNotExists(pathRenderVx);

                            tessVBOStream.Bind();

                            _basicFillShader.FillTriangles(
                                pathRenderVx._tessAreaVboSeg.startAt,
                                pathRenderVx._tessAreaVboSeg.vertexCount,
                                color);

                            tessVBOStream.Unbind();
                        }
                        else
                        {
                            int subPathCount = pathRenderVx.FigCount;
                            //alll subpath use the same color setting 
                            if (subPathCount > 1)
                            {
                                float[] tessArea = pathRenderVx.GetAreaTess(_tessTool, _tessWindingRuleType);
                                if (tessArea != null)
                                {
                                    _basicFillShader.FillTriangles(tessArea, pathRenderVx.TessAreaVertexCount, color);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < subPathCount; ++i)
                                {
                                    Figure figure = pathRenderVx.GetFig(i);

                                    float[] tessArea = figure.GetAreaTess(_tessTool, _tessWindingRuleType, TessTriangleTechnique.DrawArray);
                                    if (tessArea != null)
                                    {
                                        _basicFillShader.FillTriangles(tessArea, figure.TessAreaVertexCount, color);
                                    }
                                }
                            }
                        }

                    }
                    break;
                case SmoothMode.Smooth:
                    {
                        float saved_Width = StrokeWidth;
                        Drawing.Color saved_Color = StrokeColor;
                        //temp set stroke width to 2 amd stroke color
                        //to the same as bg color (for smooth border).
                        //and it will be set back later.
                        // 
                        StrokeColor = color;
                        StrokeWidth = 1.5f; //TODO: review this *** 

                        if (pathRenderVx._enableVBO)
                        {
                            VBOStream tessVBOStream = GetVBOStreamOrBuildIfNotExists(pathRenderVx);

                            tessVBOStream.Bind();

                            _basicFillShader.FillTriangles(
                                pathRenderVx._tessAreaVboSeg.startAt,
                                pathRenderVx._tessAreaVboSeg.vertexCount,
                                color);

                            _smoothLineShader.DrawTriangleStrips(
                                pathRenderVx._smoothBorderVboSeg.startAt,
                                pathRenderVx._smoothBorderVboSeg.vertexCount);
                            //
                            tessVBOStream.Unbind();
                        }
                        else
                        {
                            int subPathCount = pathRenderVx.FigCount;
                            //all subpath use the same color setting 
                            //merge all subpath

                            if (subPathCount > 1)
                            {
                                float[] tessArea = pathRenderVx.GetAreaTess(_tessTool, _tessWindingRuleType);
                                if (tessArea != null)
                                {
                                    _basicFillShader.FillTriangles(tessArea, pathRenderVx.TessAreaVertexCount, color);
                                }

                                _smoothLineShader.DrawTriangleStrips(
                                    pathRenderVx.GetSmoothBorders(_smoothBorderBuilder),
                                    pathRenderVx.BorderTriangleStripCount);
                            }
                            else
                            {
                                Figure figure = pathRenderVx.GetFig(0);
                                float[] tessArea;
                                if ((tessArea = figure.GetAreaTess(_tessTool, _tessWindingRuleType, TessTriangleTechnique.DrawArray)) != null)
                                {
                                    //draw area
                                    _basicFillShader.FillTriangles(tessArea, figure.TessAreaVertexCount, color);
                                    //draw smooth border
                                    _smoothLineShader.DrawTriangleStrips(
                                        figure.GetSmoothBorders(_smoothBorderBuilder),
                                        figure.BorderTriangleStripCount);
                                }
                            }
                        }

                        //restore stroke width and color
                        StrokeWidth = saved_Width; //restore back
                        StrokeColor = saved_Color;

                    }
                    break;
            }
        }

        public void FillGfxPath(Drawing.Brush brush, PathRenderVx pathRenderVx)
        {
            switch (brush.BrushKind)
            {
                case Drawing.BrushKind.Solid:
                    {
                        var solidBrush = brush as PixelFarm.Drawing.SolidBrush;
                        FillGfxPath(solidBrush.Color, pathRenderVx);
                    }
                    break;
                case Drawing.BrushKind.LinearGradient:
                case Drawing.BrushKind.CircularGraident:
                case Drawing.BrushKind.Texture:
                case BrushKind.PolygonGradient:
                    {
                        //TODO: review here again
                        //use VBO?
                        //
                        if (pathRenderVx._enableVBO)
                        {

                            VBOStream tessVBOStream = GetVBOStreamOrBuildIfNotExists(pathRenderVx);

                            GL.ClearStencil(0); //set value for clearing stencil buffer 
                                                //actual clear here
                            GL.Clear(ClearBufferMask.StencilBufferBit);
                            //-------------------
                            //disable rendering to color buffer
                            GL.ColorMask(false, false, false, false);
                            //start using stencil
                            GL.Enable(EnableCap.StencilTest);
                            //place a 1 where rendered
                            GL.StencilFunc(StencilFunction.Always, 1, 1);
                            //replace where rendered
                            GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
                            //render  to stencill buffer

                            tessVBOStream.Bind();

                            _basicFillShader.FillTriangles(
                                pathRenderVx._tessAreaVboSeg.startAt,
                                pathRenderVx._tessAreaVboSeg.vertexCount,
                                Color.Black);

                            //-------------------------------------- 
                            //render color
                            //--------------------------------------  
                            //re-enable color buffer 
                            GL.ColorMask(true, true, true, true);
                            //where a 1 was not rendered
                            GL.StencilFunc(StencilFunction.Equal, 1, 1);
                            //freeze stencill buffer
                            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
                            //------------------------------------------
                            //we already have valid ps from stencil step
                            //------------------------------------------

                            //-------------------------------------------------------------------------------------
                            //1.  we draw only alpha chanel of this black color to destination color
                            //so we use  BlendFuncSeparate  as follow ... 
                            //-------------------------------------------------------------------------------------

                            GL.ColorMask(false, false, false, true);
                            //GL.BlendFuncSeparate(
                            //     BlendingFactorSrc.DstColor, BlendingFactorDest.DstColor, //the same
                            //     BlendingFactorSrc.One, BlendingFactorDest.Zero);
                            //use alpha chanel from source***
                            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.Zero);

                            tessVBOStream.Unbind();
                            //at this point alpha component is fill in to destination 
                            {
                                switch (brush.BrushKind)
                                {
                                    case BrushKind.CircularGraident:
                                        {
                                            RadialGradientBrush glGrBrush = RadialGradientBrush.Resolve((Drawing.RadialGradientBrush)brush);
                                            if (glGrBrush._hasSignificateAlphaCompo)
                                            {
                                                _radialGradientShader.Render(
                                                    glGrBrush._v2f,
                                                    glGrBrush._cx,
                                                    _vwHeight - glGrBrush._cy,
                                                    glGrBrush._r,
                                                    glGrBrush._invertedAff,
                                                    glGrBrush._lookupBmp);

                                            }
                                            else
                                            {

                                                tessVBOStream.Bind();

                                                _invertAlphaFragmentShader.DrawTriangleStrips(
                                                    pathRenderVx._smoothBorderVboSeg.startAt,
                                                    pathRenderVx._smoothBorderVboSeg.vertexCount);


                                                tessVBOStream.Unbind();
                                            }
                                        }
                                        break;
                                    default:
                                        {
                                            tessVBOStream.Bind();

                                            _invertAlphaFragmentShader.DrawTriangleStrips(
                                                pathRenderVx._smoothBorderVboSeg.startAt,
                                                pathRenderVx._smoothBorderVboSeg.vertexCount);

                                            tessVBOStream.Unbind();
                                        }
                                        break;
                                }
                            }


                            //-------------------------------------------------------------------------------------
                            //2. then fill again!, 
                            //we use alpha information from dest, 
                            //so we set blend func to ... GL.BlendFunc(BlendingFactorSrc.DstAlpha, BlendingFactorDest.OneMinusDstAlpha)    
                            GL.ColorMask(true, true, true, true);
                            GL.BlendFunc(BlendingFactorSrc.DstAlpha, BlendingFactorDest.OneMinusDstAlpha);
                            {
                                //draw box*** of gradient color
                                switch (brush.BrushKind)
                                {
                                    case BrushKind.CircularGraident:
                                        {
                                            //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusDstAlpha);
                                            //GL.Disable(EnableCap.StencilTest);

                                            RadialGradientBrush glGrBrush = RadialGradientBrush.Resolve((Drawing.RadialGradientBrush)brush);
                                            _radialGradientShader.Render(
                                                glGrBrush._v2f,
                                                glGrBrush._cx,
                                                _vwHeight - glGrBrush._cy,
                                                glGrBrush._r,
                                                glGrBrush._invertedAff,
                                                glGrBrush._lookupBmp);
                                        }
                                        break;
                                    case Drawing.BrushKind.LinearGradient:
                                        {
                                            LinearGradientBrush glGrBrush = LinearGradientBrush.Resolve((Drawing.LinearGradientBrush)brush);
                                            _rectFillShader.Render(glGrBrush._v2f, glGrBrush._colors);
                                        }
                                        break;
                                    case BrushKind.PolygonGradient:
                                        {

                                            PolygonGradientBrush glGrBrush = PolygonGradientBrush.Resolve((Drawing.PolygonGradientBrush)brush, _tessTool);
                                            _rectFillShader.Render(glGrBrush._v2f, glGrBrush._colors);
                                        }
                                        break;
                                    case Drawing.BrushKind.Texture:
                                        {
                                            //draw texture image ***
                                            PixelFarm.Drawing.TextureBrush tbrush = (PixelFarm.Drawing.TextureBrush)brush;
                                            GLBitmap bmpTexture = PixelFarm.Drawing.Image.GetCacheInnerImage(tbrush.TextureImage) as GLBitmap;
                                            //TODO: review here 
                                            //where to start?
                                            this.DrawImage(bmpTexture, 0, 300); //WHY 300=> fix this
                                        }
                                        break;
                                }
                            }
                            //restore back 
                            //3. switch to normal blending mode 
                            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                            GL.Disable(EnableCap.StencilTest);
                        }
                        else
                        {
                            int m = pathRenderVx.FigCount;
                            for (int b = 0; b < m; ++b)
                            {
                                Figure fig = pathRenderVx.GetFig(b);
                                GL.ClearStencil(0); //set value for clearing stencil buffer 
                                                    //actual clear here
                                GL.Clear(ClearBufferMask.StencilBufferBit);
                                //-------------------
                                //disable rendering to color buffer
                                GL.ColorMask(false, false, false, false);
                                //start using stencil
                                GL.Enable(EnableCap.StencilTest);
                                //place a 1 where rendered
                                GL.StencilFunc(StencilFunction.Always, 1, 1);
                                //replace where rendered
                                GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
                                //render  to stencill buffer
                                //-----------------
                                float[] tessArea = fig.GetAreaTess(_tessTool, _tessWindingRuleType, TessTriangleTechnique.DrawArray);
                                //-------------------------------------   
                                if (tessArea != null)
                                {
                                    //create a hole,
                                    //no AA at this step
                                    _basicFillShader.FillTriangles(tessArea, fig.TessAreaVertexCount, PixelFarm.Drawing.Color.Black);
                                }
                                //-------------------------------------- 
                                //render color
                                //--------------------------------------  
                                //re-enable color buffer 
                                GL.ColorMask(true, true, true, true);
                                //where a 1 was not rendered
                                GL.StencilFunc(StencilFunction.Equal, 1, 1);
                                //freeze stencill buffer
                                GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
                                //------------------------------------------
                                //we already have valid ps from stencil step
                                //------------------------------------------

                                //-------------------------------------------------------------------------------------
                                //1.  we draw only alpha chanel of this black color to destination color
                                //so we use  BlendFuncSeparate  as follow ... 
                                //-------------------------------------------------------------------------------------

                                GL.ColorMask(false, false, false, true);
                                //GL.BlendFuncSeparate(
                                //     BlendingFactorSrc.DstColor, BlendingFactorDest.DstColor, //the same
                                //     BlendingFactorSrc.One, BlendingFactorDest.Zero);
                                //use alpha chanel from source***
                                GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.Zero);


                                //at this point alpha component is fill in to destination 
                                {
                                    switch (brush.BrushKind)
                                    {
                                        case BrushKind.CircularGraident:
                                            {
                                                RadialGradientBrush glGrBrush = RadialGradientBrush.Resolve((Drawing.RadialGradientBrush)brush);
                                                if (glGrBrush._hasSignificateAlphaCompo)
                                                {
                                                    _radialGradientShader.Render(
                                                        glGrBrush._v2f,
                                                        glGrBrush._cx,
                                                        _vwHeight - glGrBrush._cy,
                                                        glGrBrush._r,
                                                        glGrBrush._invertedAff,
                                                        glGrBrush._lookupBmp);

                                                }
                                                else
                                                {
                                                    float[] smoothBorder = fig.GetSmoothBorders(_smoothBorderBuilder);
                                                    _invertAlphaFragmentShader.DrawTriangleStrips(smoothBorder, fig.BorderTriangleStripCount);
                                                }
                                            }
                                            break;
                                        default:
                                            {
                                                float[] smoothBorder = fig.GetSmoothBorders(_smoothBorderBuilder);
                                                _invertAlphaFragmentShader.DrawTriangleStrips(smoothBorder, fig.BorderTriangleStripCount);
                                            }
                                            break;
                                    }
                                }
                                //-------------------------------------------------------------------------------------
                                //2. then fill again!, 
                                //we use alpha information from dest, 
                                //so we set blend func to ... GL.BlendFunc(BlendingFactorSrc.DstAlpha, BlendingFactorDest.OneMinusDstAlpha)    
                                GL.ColorMask(true, true, true, true);
                                GL.BlendFunc(BlendingFactorSrc.DstAlpha, BlendingFactorDest.OneMinusDstAlpha);
                                {
                                    //draw box*** of gradient color
                                    switch (brush.BrushKind)
                                    {
                                        case BrushKind.CircularGraident:
                                            {
                                                //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusDstAlpha);
                                                //GL.Disable(EnableCap.StencilTest);

                                                RadialGradientBrush glGrBrush = RadialGradientBrush.Resolve((Drawing.RadialGradientBrush)brush);
                                                _radialGradientShader.Render(
                                                    glGrBrush._v2f,
                                                    glGrBrush._cx,
                                                    _vwHeight - glGrBrush._cy,
                                                    glGrBrush._r,
                                                    glGrBrush._invertedAff,
                                                    glGrBrush._lookupBmp);
                                            }
                                            break;
                                        case Drawing.BrushKind.LinearGradient:
                                            {
                                                LinearGradientBrush glGrBrush = LinearGradientBrush.Resolve((Drawing.LinearGradientBrush)brush);
                                                _rectFillShader.Render(glGrBrush._v2f, glGrBrush._colors);
                                            }
                                            break;
                                        case BrushKind.PolygonGradient:
                                            {

                                                PolygonGradientBrush glGrBrush = PolygonGradientBrush.Resolve((Drawing.PolygonGradientBrush)brush, _tessTool);
                                                _rectFillShader.Render(glGrBrush._v2f, glGrBrush._colors);
                                            }
                                            break;
                                        case Drawing.BrushKind.Texture:
                                            {
                                                //draw texture image ***
                                                PixelFarm.Drawing.TextureBrush tbrush = (PixelFarm.Drawing.TextureBrush)brush;
                                                GLBitmap bmpTexture = PixelFarm.Drawing.Image.GetCacheInnerImage(tbrush.TextureImage) as GLBitmap;
                                                //TODO: review here 
                                                //where to start?
                                                this.DrawImage(bmpTexture, 0, 300); //WHY 300=> fix this
                                            }
                                            break;
                                    }
                                }
                                //restore back 
                                //3. switch to normal blending mode 
                                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                                GL.Disable(EnableCap.StencilTest);
                            }

                        }

                    }
                    break;
            }
        }
        //void FillGfxPath_OLD(Drawing.Brush brush, PathRenderVx pathRenderVx)
        //{
        //    switch (brush.BrushKind)
        //    {
        //        case Drawing.BrushKind.Solid:
        //            {
        //                var solidBrush = brush as PixelFarm.Drawing.SolidBrush;
        //                FillGfxPath(solidBrush.Color, pathRenderVx);
        //            }
        //            break;
        //        case Drawing.BrushKind.LinearGradient:
        //        case Drawing.BrushKind.CircularGraident:
        //        case Drawing.BrushKind.Texture:
        //        case BrushKind.PolygonGradient:
        //            {
        //                //TODO: review here again
        //                //use VBO?
        //                //

        //                int m = pathRenderVx.FigCount;
        //                for (int b = 0; b < m; ++b)
        //                {
        //                    Figure fig = pathRenderVx.GetFig(b);
        //                    GL.ClearStencil(0); //set value for clearing stencil buffer 
        //                    //actual clear here
        //                    GL.Clear(ClearBufferMask.StencilBufferBit);
        //                    //-------------------
        //                    //disable rendering to color buffer
        //                    GL.ColorMask(false, false, false, false);
        //                    //start using stencil
        //                    GL.Enable(EnableCap.StencilTest);
        //                    //place a 1 where rendered
        //                    GL.StencilFunc(StencilFunction.Always, 1, 1);
        //                    //replace where rendered
        //                    GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
        //                    //render  to stencill buffer
        //                    //-----------------
        //                    float[] tessArea = fig.GetAreaTess(_tessTool, _tessWindingRuleType, TessTriangleTechnique.DrawArray);
        //                    //-------------------------------------   
        //                    if (tessArea != null)
        //                    {
        //                        //create a hole,
        //                        //no AA at this step
        //                        _basicFillShader.FillTriangles(tessArea, fig.TessAreaVertexCount, PixelFarm.Drawing.Color.Black);
        //                    }
        //                    //-------------------------------------- 
        //                    //render color
        //                    //--------------------------------------  
        //                    //re-enable color buffer 
        //                    GL.ColorMask(true, true, true, true);
        //                    //where a 1 was not rendered
        //                    GL.StencilFunc(StencilFunction.Equal, 1, 1);
        //                    //freeze stencill buffer
        //                    GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
        //                    //------------------------------------------
        //                    //we already have valid ps from stencil step
        //                    //------------------------------------------

        //                    //-------------------------------------------------------------------------------------
        //                    //1.  we draw only alpha chanel of this black color to destination color
        //                    //so we use  BlendFuncSeparate  as follow ... 
        //                    //-------------------------------------------------------------------------------------

        //                    GL.ColorMask(false, false, false, true);
        //                    //GL.BlendFuncSeparate(
        //                    //     BlendingFactorSrc.DstColor, BlendingFactorDest.DstColor, //the same
        //                    //     BlendingFactorSrc.One, BlendingFactorDest.Zero);
        //                    //use alpha chanel from source***
        //                    GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.Zero);


        //                    //at this point alpha component is fill in to destination 
        //                    {
        //                        switch (brush.BrushKind)
        //                        {
        //                            case BrushKind.CircularGraident:
        //                                {
        //                                    RadialGradientBrush glGrBrush = RadialGradientBrush.Resolve((Drawing.RadialGradientBrush)brush);
        //                                    if (glGrBrush._hasSignificateAlphaCompo)
        //                                    {
        //                                        _radialGradientShader.Render(
        //                                            glGrBrush._v2f,
        //                                            glGrBrush._cx,
        //                                            _vwHeight - glGrBrush._cy,
        //                                            glGrBrush._r,
        //                                            glGrBrush._invertedAff,
        //                                            glGrBrush._lookupBmp);

        //                                    }
        //                                    else
        //                                    {
        //                                        float[] smoothBorder = fig.GetSmoothBorders(_smoothBorderBuilder);
        //                                        _invertAlphaFragmentShader.DrawTriangleStrips(smoothBorder, fig.BorderTriangleStripCount);
        //                                    }
        //                                }
        //                                break;
        //                            default:
        //                                {
        //                                    float[] smoothBorder = fig.GetSmoothBorders(_smoothBorderBuilder);
        //                                    _invertAlphaFragmentShader.DrawTriangleStrips(smoothBorder, fig.BorderTriangleStripCount);
        //                                }
        //                                break;
        //                        }
        //                    }
        //                    //-------------------------------------------------------------------------------------
        //                    //2. then fill again!, 
        //                    //we use alpha information from dest, 
        //                    //so we set blend func to ... GL.BlendFunc(BlendingFactorSrc.DstAlpha, BlendingFactorDest.OneMinusDstAlpha)    
        //                    GL.ColorMask(true, true, true, true);
        //                    GL.BlendFunc(BlendingFactorSrc.DstAlpha, BlendingFactorDest.OneMinusDstAlpha);
        //                    {
        //                        //draw box*** of gradient color
        //                        switch (brush.BrushKind)
        //                        {
        //                            case BrushKind.CircularGraident:
        //                                {
        //                                    //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusDstAlpha);
        //                                    //GL.Disable(EnableCap.StencilTest);

        //                                    RadialGradientBrush glGrBrush = RadialGradientBrush.Resolve((Drawing.RadialGradientBrush)brush);
        //                                    _radialGradientShader.Render(
        //                                        glGrBrush._v2f,
        //                                        glGrBrush._cx,
        //                                        _vwHeight - glGrBrush._cy,
        //                                        glGrBrush._r,
        //                                        glGrBrush._invertedAff,
        //                                        glGrBrush._lookupBmp);
        //                                }
        //                                break;
        //                            case Drawing.BrushKind.LinearGradient:
        //                                {
        //                                    LinearGradientBrush glGrBrush = LinearGradientBrush.Resolve((Drawing.LinearGradientBrush)brush);
        //                                    _rectFillShader.Render(glGrBrush._v2f, glGrBrush._colors);
        //                                }
        //                                break;
        //                            case BrushKind.PolygonGradient:
        //                                {

        //                                    PolygonGradientBrush glGrBrush = PolygonGradientBrush.Resolve((Drawing.PolygonGradientBrush)brush, _tessTool);
        //                                    _rectFillShader.Render(glGrBrush._v2f, glGrBrush._colors);
        //                                }
        //                                break;
        //                            case Drawing.BrushKind.Texture:
        //                                {
        //                                    //draw texture image ***
        //                                    PixelFarm.Drawing.TextureBrush tbrush = (PixelFarm.Drawing.TextureBrush)brush;
        //                                    GLBitmap bmpTexture = PixelFarm.Drawing.Image.GetCacheInnerImage(tbrush.TextureImage) as GLBitmap;
        //                                    //TODO: review here 
        //                                    //where to start?
        //                                    this.DrawImage(bmpTexture, 0, 300); //WHY 300=> fix this
        //                                }
        //                                break;
        //                        }
        //                    }
        //                    //restore back 
        //                    //3. switch to normal blending mode 
        //                    GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        //                    GL.Disable(EnableCap.StencilTest);
        //                }
        //            }
        //            break;
        //    }
        //}
        public void DisableMask()
        {
            //restore back 
            //3. switch to normal blending mode 
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Disable(EnableCap.StencilTest);
        }
        public void EnableMask(PathRenderVx pathRenderVx)
        {

            GL.ClearStencil(0); //set value for clearing stencil buffer 
                                //actual clear here
            GL.Clear(ClearBufferMask.StencilBufferBit);
            //-------------------
            //disable rendering to color buffer
            GL.ColorMask(false, false, false, false);
            //start using stencil
            GL.Enable(EnableCap.StencilTest);
            //place a 1 where rendered
            GL.StencilFunc(StencilFunction.Always, 1, 1);
            //replace where rendered
            GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
            //render  to stencill buffer
            //-----------------

            int m = pathRenderVx.FigCount;
            for (int b = 0; b < m; ++b)
            {
                Figure fig = pathRenderVx.GetFig(b);
                float[] tessArea = fig.GetAreaTess(_tessTool, _tessWindingRuleType, TessTriangleTechnique.DrawArray);
                //-------------------------------------   
                if (tessArea != null)
                {
                    _basicFillShader.FillTriangles(tessArea, fig.TessAreaVertexCount, PixelFarm.Drawing.Color.Black);
                }
            }

            //-------------------------------------- 
            //render color
            //--------------------------------------  
            //reenable color buffer 
            GL.ColorMask(true, true, true, true);
            //where a 1 was not rendered
            GL.StencilFunc(StencilFunction.Equal, 1, 1);
            //freeze stencill buffer
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            //------------------------------------------
            //we already have valid ps from stencil step
            //------------------------------------------

            //-------------------------------------------------------------------------------------
            //1.  we draw only alpha chanel of this black color to destination color
            //so we use  BlendFuncSeparate  as follow ... 
            //-------------------------------------------------------------------------------------
            //1.  we draw only alpha channel of this black color to destination color
            //so we use  BlendFuncSeparate  as follow ... 
            GL.ColorMask(false, false, false, true);
            //GL.BlendFuncSeparate(
            //     BlendingFactorSrc.DstColor, BlendingFactorDest.DstColor, //the same
            //     BlendingFactorSrc.One, BlendingFactorDest.Zero);

            //use alpha chanel from source***
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.Zero);

            //
            //TODO: review smooth border filll here ***
            //
            //float[] smoothBorder = fig.GetSmoothBorders(_smoothBorderBuilder);
            //_invertAlphaFragmentShader.DrawTriangleStrips(smoothBorder, fig.BorderTriangleStripCount);

            //at this point alpha component is fill in to destination 
            //-------------------------------------------------------------------------------------
            //2. then fill again!, 
            //we use alpha information from dest, 
            //so we set blend func to ... GL.BlendFunc(BlendingFactorSrc.DstAlpha, BlendingFactorDest.OneMinusDstAlpha)    
            GL.ColorMask(true, true, true, true);
        }
        public void DrawGfxPath(Drawing.Color color, PathRenderVx igpth)
        {
            //TODO: review here again
            //use VBO?
            //
            switch (SmoothMode)
            {
                case SmoothMode.No:
                    {

                        int subPathCount = igpth.FigCount;
                        for (int i = 0; i < subPathCount; ++i)
                        {
                            Figure f = igpth.GetFig(i);
                            float[] coordXYs = f.coordXYs;
                            unsafe
                            {
                                fixed (float* head = &coordXYs[0])
                                {
                                    _basicFillShader.DrawLineLoopWithVertexBuffer(head, coordXYs.Length / 2, StrokeColor);
                                }
                            }
                        }
                    }
                    break;
                case SmoothMode.Smooth:
                    {
                        //
                        StrokeColor = color;
                        float prevStrokeW = StrokeWidth;
                        if (prevStrokeW < 0.25f)
                        {
                            StrokeWidth = 0.25f;
                        }
                        //
                        int subPathCount = igpth.FigCount;
                        for (int i = 0; i < subPathCount; ++i)
                        {
                            Figure f = igpth.GetFig(i);
                            _smoothLineShader.DrawTriangleStrips(
                                f.GetSmoothBorders(_smoothBorderBuilder),
                                f.BorderTriangleStripCount);
                        }
                        StrokeWidth = prevStrokeW;
                        //
                    }
                    break;
            }
        }
        public void SetCanvasOrigin(int x, int y)
        {
            _canvasOriginX = x;
            _canvasOriginY = y;
            if (_rendersx == null)
            {
                return;
            }


            if (_coordTransformer == null)
            {
                _shareRes.OrthoView = _rendersx._orthoFlipY_and_PullDown *
                                      MyMat4.translate(new OpenTK.Vector3(x, y, 0)); //pull-down 
            }
            else
            {
                _shareRes.OrthoView = _rendersx._orthoFlipY_and_PullDown *
                                      MyMat4.translate(new OpenTK.Vector3(x, y, 0)) *//pull-down 
                                      _customCoordTransformer;
            }


            //old => not correct!   
            //leave here to study
            //: if we set viewport to (x,y,viewport_w,viewport_h)
            // then we draw image that larger (eg.img_h> viewport_h)
            // the image is crop! (eg. see example in scrollview example)
            // so we set ortho metrix instead
            //
            //GL.Viewport(x,
            //    (OriginKind == RenderSurfaceOrientation.LeftTop) ? -y : y,
            //    _width,
            //    _height);
        }
        public void EnableClipRect()
        {
            GL.Enable(EnableCap.ScissorTest);
        }
        public void DisableClipRect()
        {
            GL.Disable(EnableCap.ScissorTest);
        }
        public void SetClipRect(int left, int top, int width, int height)
        {
            if (OriginKind == RenderSurfaceOrientation.LeftTop)
            {
                GL.Scissor(left + _canvasOriginX, _vwHeight - (_canvasOriginY + top + height), width, height);
            }
            else
            {
                GL.Scissor(left + _canvasOriginX, _canvasOriginY + top + height, width, height);
            }
        }
        internal TessTool GetTessTool() => _tessTool;
        internal SmoothBorderBuilder GetSmoothBorderBuilder() => _smoothBorderBuilder;
    }

    static class SimpleTessTool
    {
        /// <summary>
        /// create coord for left-bottom-origin canvas
        /// </summary>
        /// <param name="left"></param>
        /// <param name="bottom"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="output"></param>
        public static void CreateRectTessCoordsTriStrip(float left, float bottom, float w, float h, float[] output)
        {
            //use original GLES coord base => (0,0)= left,bottom 
            output[0] = left; output[1] = bottom - h;
            output[2] = left; output[3] = bottom;
            output[4] = left + w; output[5] = bottom - h;
            output[6] = left + w; output[7] = bottom;
        }
    }



    public class TextureCoordVboBuilder
    {

        int _orgBmpW;
        int _orgBmpH;
        bool _bmpYFlipped;
        float _scale = 1;
        RenderSurfaceOrientation _pcxOrgKind;

        internal PixelFarm.CpuBlit.ArrayList<float> _buffer = new CpuBlit.ArrayList<float>();
        internal PixelFarm.CpuBlit.ArrayList<ushort> _indexList = new CpuBlit.ArrayList<ushort>();

        public TextureCoordVboBuilder()
        {

        }
        public void SetTextureInfo(int width, int height, bool isYFlipped, RenderSurfaceOrientation pcxOrgKind)
        {
            _orgBmpW = width;
            _orgBmpH = height;
            _bmpYFlipped = isYFlipped;
            _pcxOrgKind = pcxOrgKind;
        }


        public void Clear()
        {
            _buffer.Clear();
            _indexList.Clear();
        }
        public void WriteVboToList(
            ref PixelFarm.Drawing.Rectangle srcRect,
            float targetLeft,
            float targetTop)
        {

            if (_pcxOrgKind == RenderSurfaceOrientation.LeftTop) //***
            {
                //***
                targetTop += srcRect.Height;  //***
            }



            // https://developer.apple.com/library/content/documentation/3DDrawing/Conceptual/OpenGLES_ProgrammingGuide/TechniquesforWorkingwithVertexData/TechniquesforWorkingwithVertexData.html

            ushort indexCount = (ushort)_indexList.Count;

            if (indexCount > 0)
            {

                //add degenerative triangle
                float prev_5 = _buffer[_buffer.Count - 5];
                float prev_4 = _buffer[_buffer.Count - 4];
                float prev_3 = _buffer[_buffer.Count - 3];
                float prev_2 = _buffer[_buffer.Count - 2];
                float prev_1 = _buffer[_buffer.Count - 1];

                _buffer.Append(prev_5); _buffer.Append(prev_4); _buffer.Append(prev_3);
                _buffer.Append(prev_2); _buffer.Append(prev_1);


                _indexList.Append((ushort)(indexCount));
                _indexList.Append((ushort)(indexCount + 1));

                indexCount += 2;
            }


            WriteVboStream(_buffer, indexCount > 0,
                srcRect.Left, srcRect.Top, srcRect.Width, srcRect.Height, targetLeft, targetTop,
                _orgBmpW, _orgBmpH, _bmpYFlipped);

            _indexList.Append(indexCount);
            _indexList.Append((ushort)(indexCount + 1));
            _indexList.Append((ushort)(indexCount + 2));
            _indexList.Append((ushort)(indexCount + 3));
            //---
            //add degenerate rect

        }


        static void WriteVboStream(
           PixelFarm.CpuBlit.ArrayList<float> vboList,
            bool duplicateFirst,
            float srcLeft, float srcTop,
            float srcW, float srcH,
            float targetLeft, float targetTop,
            float orgBmpW, float orgBmpH,
            bool bmpYFlipped
        )
        {

            unsafe
            {
                float scale = 1;
                float srcBottom = srcTop + srcH;
                float srcRight = srcLeft + srcW;

                unsafe
                {
                    if (bmpYFlipped)
                    {
                        vboList.Append(targetLeft); vboList.Append(targetTop); vboList.Append(0); //coord 0 (left,top)                                                                                                       
                        vboList.Append(srcLeft / orgBmpW); vboList.Append(srcTop / orgBmpH); //texture coord 0 (left,top)

                        if (duplicateFirst)
                        {
                            //for creating degenerative triangle


                            vboList.Append(targetLeft); vboList.Append(targetTop); vboList.Append(0); //coord 0 (left,top)                                                                                                       
                            vboList.Append(srcLeft / orgBmpW); vboList.Append(srcTop / orgBmpH); //texture coord 0 (left,top)

                        }
                        //---------------------
                        vboList.Append(targetLeft); vboList.Append(targetTop - (srcH * scale)); vboList.Append(0); //coord 1 (left,bottom)
                        vboList.Append(srcLeft / orgBmpW); vboList.Append(srcBottom / orgBmpH); //texture coord 1 (left,bottom)

                        //---------------------
                        vboList.Append(targetLeft + (srcW * scale)); vboList.Append(targetTop); vboList.Append(0); //coord 2 (right,top)
                        vboList.Append(srcRight / orgBmpW); vboList.Append(srcTop / orgBmpH); //texture coord 2 (right,top)

                        //---------------------
                        vboList.Append(targetLeft + (srcW * scale)); vboList.Append(targetTop - (srcH * scale)); vboList.Append(0);//coord 3 (right, bottom)
                        vboList.Append(srcRight / orgBmpW); vboList.Append(srcBottom / orgBmpH); //texture coord 3  (right,bottom) 

                    }
                    else
                    {


                        vboList.Append(targetLeft); vboList.Append(targetTop); vboList.Append(0); //coord 0 (left,top)
                        vboList.Append(srcLeft / orgBmpW); vboList.Append(srcBottom / orgBmpH); //texture coord 0  (left,bottom) 
                        if (duplicateFirst)
                        {
                            //for creating degenerative triangle
                            //https://developer.apple.com/library/content/documentation/3DDrawing/Conceptual/OpenGLES_ProgrammingGuide/TechniquesforWorkingwithVertexData/TechniquesforWorkingwithVertexData.html

                            vboList.Append(targetLeft); vboList.Append(targetTop); vboList.Append(0); //coord 0 (left,top)
                            vboList.Append(srcLeft / orgBmpW); vboList.Append(srcBottom / orgBmpH); //texture coord 0  (left,bottom)
                        }

                        //---------------------
                        vboList.Append(targetLeft); vboList.Append(targetTop - (srcH * scale)); vboList.Append(0); //coord 1 (left,bottom)
                        vboList.Append(srcLeft / orgBmpW); vboList.Append(srcTop / orgBmpH); //texture coord 1  (left,top)

                        //---------------------
                        vboList.Append(targetLeft + (srcW * scale)); vboList.Append(targetTop); vboList.Append(0); //coord 2 (right,top)
                        vboList.Append(srcRight / orgBmpW); vboList.Append(srcBottom / orgBmpH); //texture coord 2  (right,bottom)

                        //---------------------
                        vboList.Append(targetLeft + (srcW * scale)); vboList.Append(targetTop - (srcH * scale)); vboList.Append(0); //coord 3 (right, bottom)
                        vboList.Append(srcRight / orgBmpW); vboList.Append(srcTop / orgBmpH); //texture coord 3 (right,top) 
                    }
                }
            }
        }
    }
}