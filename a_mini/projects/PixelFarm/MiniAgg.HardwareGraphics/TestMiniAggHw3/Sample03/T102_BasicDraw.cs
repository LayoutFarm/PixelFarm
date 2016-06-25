﻿//MIT, 2014-2016,WinterDev

using System;
using Mini;
using PixelFarm.DrawingGL;
namespace OpenTkEssTest
{
    [Info(OrderCode = "102")]
    [Info("T102_BasicDraw")]
    public class T102_BasicDraw : PrebuiltGLControlDemoBase
    {
        CanvasGL2d canvas2d;
        GLCanvasPainter painter;
        PixelFarm.Drawing.RenderVx polygon1;
        PixelFarm.Drawing.RenderVx polygon2;
        protected override void OnInitGLProgram(object sender, EventArgs args)
        {
            int max = Math.Max(this.Width, this.Height);
            canvas2d = new CanvasGL2d(max, max);
            painter = new GLCanvasPainter(canvas2d, max, max);
            polygon1 = painter.CreatePolygonRenderVx(new float[]
            {
                50,200,
                250,200,
                125,350
            });
            polygon2 = painter.CreatePolygonRenderVx(new float[]
            {
                250,400,
                450,400,
                325,550
});
        }
        protected override void DemoClosing()
        {
            canvas2d.Dispose();
        }
        protected override void OnGLRender(object sender, EventArgs args)
        {
            Test2();
        }
        void Test2()
        {
            canvas2d.ClearColorBuffer();
            canvas2d.SmoothMode = CanvasSmoothMode.Smooth;
            canvas2d.StrokeColor = PixelFarm.Drawing.Color.Blue;
            ////line
            painter.FillColor = PixelFarm.Drawing.Color.Green;
            painter.FillRectLBWH(100, 100, 50, 50);
            //canvas2d.FillRect(PixelFarm.Drawing.Color.Green, 100, 100, 50, 50);
            canvas2d.DrawLine(50, 50, 200, 200);
            canvas2d.DrawRect(10, 10, 50, 50);
            painter.FillRenderVx(polygon2);
            painter.DrawRenderVx(polygon2);
            //-------------------------------------------
            ////polygon 
            painter.DrawRenderVx(polygon1);
            canvas2d.StrokeColor = PixelFarm.Drawing.Color.Green;
            //--------------------------------------------
            canvas2d.DrawCircle(100, 100, 25);
            canvas2d.DrawEllipse(200, 200, 25, 50);
            //

            canvas2d.FillCircle(PixelFarm.Drawing.Color.OrangeRed, 100, 400, 25);
            canvas2d.StrokeColor = PixelFarm.Drawing.Color.OrangeRed;
            canvas2d.DrawCircle(100, 400, 25);
            //
            canvas2d.FillEllipse(PixelFarm.Drawing.Color.OrangeRed, 200, 400, 25, 50);
            canvas2d.StrokeColor = PixelFarm.Drawing.Color.OrangeRed;
            canvas2d.DrawEllipse(200, 400, 25, 50);
            miniGLControl.SwapBuffers();
        }
    }
}
