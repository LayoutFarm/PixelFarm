﻿//BSD, 2014-present, WinterDev
//MatterHackers

using System;
using System.Diagnostics;
using PixelFarm.Drawing;
using PixelFarm.CpuBlit.FragmentProcessing;

using Mini;
namespace PixelFarm.CpuBlit.Sample_Gouraud
{
    [Info(OrderCode = "07_2", AvailableOn = AvailableOn.Agg)]
    [Info("Gouraud shading. It's a simple method of interpolating colors in a triangle. There's no 'cube' drawn"
                + ", there're just 6 triangles. You define a triangle and colors in its vertices. When rendering, the "
                + "colors will be linearly interpolated. But there's a problem that appears when drawing adjacent "
                + "triangles with Anti-Aliasing. Anti-Aliased polygons do not 'dock' to each other correctly, there "
                + "visual artifacts at the edges appear. I call it “the problem of adjacent edges”. AGG has a simple"
                + " mechanism that allows you to get rid of the artifacts, just dilating the polygons and/or changing "
                + "the gamma-correction value. But it's tricky, because the values depend on the opacity of the polygons."
                + " In this example you can change the opacity, the dilation value and gamma. Also you can drag the "
                + "Red, Green and Blue corners of the “cube”.")]
    public class GouraudApplication : DemoBase
    {
        double[] _x = new double[3];
        double[] _y = new double[3];
        double _dx;
        double _dy;
        int _idx;
        Stopwatch _stopwatch = new Stopwatch();
        public GouraudApplication()
        {
            _idx = (-1);
            _x[0] = 57; _y[0] = 60;
            _x[1] = 369; _y[1] = 170;
            _x[2] = 143; _y[2] = 310;
            this.DilationValue = 0.175;
            this.LinearGamma = 0.809f;
            this.AlphaValue = 1;
        }

        [DemoConfig(MaxValue = 1)]
        public double DilationValue
        {
            get;
            set;
        }

        [DemoConfig(MaxValue = 1)]
        public float LinearGamma
        {
            get;
            set;
        }
        [DemoConfig(MaxValue = 1)]
        public float AlphaValue
        {
            get;
            set;
        }
        //template<class Scanline, class Ras> 
        public void RenderGourand(Painter p)
        {
            float alpha = this.AlphaValue;
            float brc = 1;
#if SourceDepth24
            pixfmt_alpha_blend_rgb pf = new pixfmt_alpha_blend_rgb(backBuffer, new blender_bgr());
#else

#endif
            ////var destImage = gx.DestImage;
            ////span_allocator span_alloc = new span_allocator(); 

            //specific for agg
            AggPainter painter = p as AggPainter;
            if (painter == null) { return; }

            //
            AggRenderSurface aggsx = painter.RenderSurface;
            RGBAGouraudSpanGen gouraudSpanGen = new RGBAGouraudSpanGen();
            GouraudVerticeBuilder grBuilder = new GouraudVerticeBuilder();

            aggsx.ScanlineRasterizer.ResetGamma(new GammaLinear(0.0f, this.LinearGamma));

            grBuilder.DilationValue = (float)this.DilationValue;
            // Six triangles

            double xc = (_x[0] + _x[1] + _x[2]) / 3.0;
            double yc = (_y[0] + _y[1] + _y[2]) / 3.0;
            double x1 = (_x[1] + _x[0]) / 2 - (xc - (_x[1] + _x[0]) / 2);
            double y1 = (_y[1] + _y[0]) / 2 - (yc - (_y[1] + _y[0]) / 2);
            double x2 = (_x[2] + _x[1]) / 2 - (xc - (_x[2] + _x[1]) / 2);
            double y2 = (_y[2] + _y[1]) / 2 - (yc - (_y[2] + _y[1]) / 2);
            double x3 = (_x[0] + _x[2]) / 2 - (xc - (_x[0] + _x[2]) / 2);
            double y3 = (_y[0] + _y[2]) / 2 - (yc - (_y[0] + _y[2]) / 2);
            grBuilder.SetColor(ColorEx.Make(1, 0, 0, alpha),
                               ColorEx.Make(0, 1, 0, alpha),
                               ColorEx.Make(brc, brc, brc, alpha));
            grBuilder.SetTriangle(_x[0], _y[0], _x[1], _y[1], xc, yc);

            GouraudVerticeBuilder.CoordAndColor c0, c1, c2;
            grBuilder.GetArrangedVertices(out c0, out c1, out c2);
            gouraudSpanGen.SetColorAndCoords(c0, c1, c2);

            using (VxsTemp.Borrow(out var v1))
            {
                painter.Fill(grBuilder.MakeVxs(v1), gouraudSpanGen);
                v1.Clear();

                //
                grBuilder.SetColor(
                                ColorEx.Make(0, 1, 0, alpha),
                                ColorEx.Make(0, 0, 1, alpha),
                                ColorEx.Make(brc, brc, brc, alpha));
                grBuilder.SetTriangle(_x[1], _y[1], _x[2], _y[2], xc, yc);
                grBuilder.GetArrangedVertices(out c0, out c1, out c2);
                gouraudSpanGen.SetColorAndCoords(c0, c1, c2);

                painter.Fill(grBuilder.MakeVxs(v1), gouraudSpanGen);
                v1.Clear();

                //
                grBuilder.SetColor(ColorEx.Make(0, 0, 1, alpha),
                                ColorEx.Make(1, 0, 0, alpha),
                                ColorEx.Make(brc, brc, brc, alpha));
                grBuilder.SetTriangle(_x[2], _y[2], _x[0], _y[0], xc, yc);
                grBuilder.GetArrangedVertices(out c0, out c1, out c2);
                gouraudSpanGen.SetColorAndCoords(c0, c1, c2);
                painter.Fill(grBuilder.MakeVxs(v1), gouraudSpanGen);
                v1.Clear();
                //
                brc = 1 - brc;
                grBuilder.SetColor(ColorEx.Make(1, 0, 0, alpha),
                                  ColorEx.Make(0, 1, 0, alpha),
                                 ColorEx.Make(brc, brc, brc, alpha));
                grBuilder.SetTriangle(_x[0], _y[0], _x[1], _y[1], x1, y1);
                grBuilder.GetArrangedVertices(out c0, out c1, out c2);
                gouraudSpanGen.SetColorAndCoords(c0, c1, c2);
                painter.Fill(grBuilder.MakeVxs(v1), gouraudSpanGen);
                v1.Clear();

                grBuilder.SetColor(ColorEx.Make(0, 1, 0, alpha),
                              ColorEx.Make(0, 0, 1, alpha),
                              ColorEx.Make(brc, brc, brc, alpha));
                grBuilder.SetTriangle(_x[1], _y[1], _x[2], _y[2], x2, y2);
                grBuilder.GetArrangedVertices(out c0, out c1, out c2);
                gouraudSpanGen.SetColorAndCoords(c0, c1, c2);
                painter.Fill(grBuilder.MakeVxs(v1), gouraudSpanGen);
                v1.Clear();
                //
                grBuilder.SetColor(ColorEx.Make(0, 0, 1, alpha),
                                ColorEx.Make(1, 0, 0, alpha),
                               ColorEx.Make(brc, brc, brc, alpha));
                grBuilder.SetTriangle(_x[2], _y[2], _x[0], _y[0], x3, y3);
                grBuilder.GetArrangedVertices(out c0, out c1, out c2);
                gouraudSpanGen.SetColorAndCoords(c0, c1, c2);
                painter.Fill(grBuilder.MakeVxs(v1), gouraudSpanGen);
                v1.Clear();
            }

        }

        public override void Draw(Painter p)
        {
            p.Clear(Drawing.Color.White);
#if true
            RenderGourand(p);
#else
            agg.span_allocator span_alloc = new span_allocator();
            span_gouraud_rgba span_gen = new span_gouraud_rgba(new rgba8(255, 0, 0, 255), new rgba8(0, 255, 0, 255), new rgba8(0, 0, 255, 255), 320, 220, 100, 100, 200, 100, 0);
            span_gouraud test_sg = new span_gouraud(new rgba8(0, 0, 0, 255), new rgba8(0, 0, 0, 255), new rgba8(0, 0, 0, 255), 320, 220, 100, 100, 200, 100, 0);
            ras.add_path(test_sg);
            renderer_scanlines.render_scanlines_aa(ras, sl, ren_base, span_alloc, span_gen);
            //renderer_scanlines.render_scanlines_aa_solid(ras, sl, ren_base, new rgba8(0, 0, 0, 255));
#endif
            //graphics2D.ScanlineRasterizer.ResetGamma(new GammaNone());***
            //m_dilation.Render(ras, sl, ren_base);
            //m_gamma.Render(ras, sl, ren_base);
            //m_alpha.Render(ras, sl, ren_base);
        }
        public override void MouseDown(int mx, int my, bool isRightButton)
        {
            int i;
            if (isRightButton)
            {
                //ScanlineUnpacked8 sl = new ScanlineUnpacked8();
                //ScanlineRasterizer ras = new ScanlineRasterizer();
                //stopwatch.Restart();
                _stopwatch.Stop();
                _stopwatch.Reset();
                _stopwatch.Start();
                for (i = 0; i < 100; i++)
                {
                    //render_gouraud(sl, ras);
                }

                _stopwatch.Stop();
                string buf;
                buf = "Time=" + _stopwatch.ElapsedMilliseconds.ToString() + "ms";
                throw new NotImplementedException();
                //guiSurface.ShowSystemMessage(buf);
            }

            if (!isRightButton)
            {
                double x = mx;
                double y = my;
                for (i = 0; i < 3; i++)
                {
                    if (Math.Sqrt((x - _x[i]) * (x - _x[i]) + (y - _y[i]) * (y - _y[i])) < 10.0)
                    {
                        _dx = x - _x[i];
                        _dy = y - _y[i];
                        _idx = (int)i;
                        break;
                    }
                }
                if (i == 3)
                {
                    if (AggMath.point_in_triangle(_x[0], _y[0],
                                              _x[1], _y[1],
                                              _x[2], _y[2],
                                              x, y))
                    {
                        _dx = x - _x[0];
                        _dy = y - _y[0];
                        _idx = 3;
                    }
                }
            }
        }

        public override void MouseDrag(int mx, int my)
        {
            double x = mx;
            double y = my;
            if (_idx == 3)
            {
                double dx = x - _dx;
                double dy = y - _dy;
                _x[1] -= _x[0] - dx;
                _y[1] -= _y[0] - dy;
                _x[2] -= _x[0] - dx;
                _y[2] -= _y[0] - dy;
                _x[0] = dx;
                _y[0] = dy;
            }
            else if (_idx >= 0)
            {
                _x[_idx] = x - _dx;
                _y[_idx] = y - _dy;
            }
        }
        public override void MouseUp(int x, int y)
        {
            _idx = -1;
        }
    }
}




