﻿
//some parts are adapted from ...
//The MIT License(MIT)

//Copyright(c) 2016 Alberto Rodriguez & LiveCharts Contributors

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.


using System;
using System.Collections.Generic;

using PixelFarm.Drawing;
using LayoutFarm.UI;
using PixelFarm.CpuBlit;

namespace LayoutFarm.ColorBlenderSample
{
    [DemoNote("4.2 DemoSampleCharts")]
    class DemoSampleCharts : App
    {

        //sample chart! 
        class PlotBox : LayoutFarm.CustomWidgets.AbstractBox
        {
            public PlotBox(int w, int h)
                : base(w, h)
            {

            }
            public int Index { get; set; }
        }

        class LineRenderElement : RenderElement
        {
            internal VxsRenderVx _stroke;

            public LineRenderElement(RootGraphic rootGfx, int width, int height)
                : base(rootGfx, width, height)
            {

            }
            protected override void RenderClientContent(DrawBoard d, UpdateArea updateArea)
            {
                //draw line
                //we can use vxs/path to render a complex line part 

                //this render element dose not have child node, so
                //if WaitForStartRenderElement == true,
                //then we skip rendering its content
                //else if this renderElement has more child, we need to walk down)
                if (WaitForStartRenderElement) return;

                if (_stroke != null)
                {
                    using (d.SetSmoothMode(SmoothingMode.AntiAlias))
                    {
                        d.DrawRenderVx(_stroke, X0, Y0);//?
                    }
                }
                else
                {
                    using (d.SaveStroke())
                    using (d.SetSmoothMode(SmoothingMode.AntiAlias))
                    {
                        d.StrokeWidth = 3;
                        d.DrawLine(X0, Y0, X1, Y1);
                    }
                }

            }
            public override void ResetRootGraphics(RootGraphic rootgfx)
            {

            }
            public float X0;
            public float Y0;
            public float X1;
            public float Y1;
        }


        class PlotLine : UIElement
        {
            LineRenderElement _lineRendeE;
            List<PointF> _points = new List<PointF>();
            List<PlotBox> _controls = new List<PlotBox>();

            PlotBox p0;
            PlotBox p1;


            public PlotLine(PlotBox p0, PlotBox p1)
            {
                UpdateControlPoints(p0, p1);
            }
            //-------------
            public override void InvalidateGraphics()
            {
                if (this.HasReadyRenderElement)
                {
                    this.CurrentPrimaryRenderElement.InvalidateGraphics();
                }
            }
            protected override bool HasReadyRenderElement => _lineRendeE != null;

            public override RenderElement GetPrimaryRenderElement(RootGraphic rootgfx)
            {
                if (_lineRendeE == null)
                {
                    using (Tools.BorrowStroke(out var stroke))
                    using (Tools.BorrowVxs(out var vxs, out var strokeVxs))
                    {
                        stroke.Width = 3;
                        vxs.AddMoveTo(p0.Left, p0.Top);
                        vxs.AddLineTo(p1.Left, p1.Top);
                        stroke.MakeVxs(vxs, strokeVxs);
                        //---
                        //---

                        _lineRendeE = new LineRenderElement(rootgfx, 10, 10);
                        _lineRendeE._stroke = new VxsRenderVx(strokeVxs);

                        _lineRendeE.X0 = p0.Left;
                        _lineRendeE.Y0 = p0.Top;
                        _lineRendeE.X1 = p1.Left;
                        _lineRendeE.Y1 = p1.Top;
                    }

                }
                return _lineRendeE;
            }

            public override RenderElement CurrentPrimaryRenderElement => _lineRendeE;


            public void UpdateControlPoints(PlotBox p0, PlotBox p1)
            {
                this.p0 = p0;
                this.p1 = p1;


                int m = _controls.Count;
                for (int n = 0; n < m; ++n)
                {
                    _controls[n].RemoveSelf();
                }
                _controls.Clear(); //***
                _points.Clear();

                //2. create new control points... 

            }
            void SetupCornerBoxController(PlotBox box)
            {
                Color c = KnownColors.FromKnownColor(KnownColor.Orange);
                box.BackColor = new Color(100, c.R, c.G, c.B);

                //controllerBox1.dbugTag = 3;
                box.Visible = true;
                SetupCornerProperties(box);
                //
                _controls.Add(box);
            }

            void SetupCornerProperties(PlotBox cornerBox)
            {
                ////for controller box  
                //cornerBox.MouseDrag += (s, e) =>
                //{
                //    Point pos = cornerBox.Position;
                //    int newX = pos.X + e.XDiff;
                //    int newY = pos.Y + e.YDiff;
                //    cornerBox.SetLocation(newX, newY);
                //    //var targetBox = cornerBox.TargetBox;
                //    //if (targetBox != null)
                //    //{
                //    //    //move target box too
                //    //    targetBox.SetLocation(newX + 5, newY + 5);
                //    //}
                //    e.CancelBubbling = true;

                //    //then update the vxs shape


                //};
            }
        }



        int _chartHeight = 300;

        protected override void OnStart(AppHost host)
        {
            var sampleButton = new LayoutFarm.CustomWidgets.Box(100, _chartHeight);
            host.AddChild(sampleButton);
            int count = 0;
            sampleButton.MouseDown += (s, e2) =>
            {
                Console.WriteLine("click :" + (count++));
            };

            TestSimplePlot1(host);

        }
        void TestSimplePlot1(AppHost host)
        {
            //------------
            //create sample data
            //1. basic data=> a list of (x,y) point

            List<PointF> pointList = new List<PointF>(10);
            pointList.Add(new PointF(10 * 3, 20 * 3));
            pointList.Add(new PointF(10 * 3, 80 * 3));
            pointList.Add(new PointF(15 * 3, 30 * 3));
            pointList.Add(new PointF(18 * 3, 40 * 3));
            pointList.Add(new PointF(20 * 3, 20 * 3));
            pointList.Add(new PointF(25 * 3, 25 * 3));
            pointList.Add(new PointF(30 * 3, 10 * 3));

            //2. from data create a presentation of that data



            int j = pointList.Count;
            List<PlotBox> plotBoxes = new List<PlotBox>(j);
            for (int i = 0; i < j; ++i)
            {
                PlotBox pt = new PlotBox(5, 5);
                PointF data = pointList[i];
                pt.SetLocation((int)data.X, _chartHeight - (int)data.Y); //invertY
                pt.BackColor = Color.Red;

                plotBoxes.Add(pt);
                host.AddChild(pt);
            }


            //3. create connected line between each plotbox
            //...

            for (int i = 0; i < j - 1; ++i)
            {
                PlotBox p0 = plotBoxes[i];
                PlotBox p1 = plotBoxes[i + 1];
                PlotLine line = new PlotLine(p0, p1);
                host.AddChild(line);
            }
        }


    }
}