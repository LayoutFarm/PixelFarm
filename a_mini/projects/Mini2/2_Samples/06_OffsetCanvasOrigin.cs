﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

using PixelFarm.Agg;
using LayoutFarm.DrawingGL;
namespace Mini2
{
    [Info(OrderCode = "06")]
    [Info("OffsetCanvasOrigin")]
    public class OffsetCanvasOrigin : DemoBase
    {
        public override void Load()
        {
            //-----------------------------------------------
            FormTestWinGLControl form = new FormTestWinGLControl();
            CanvasGL2d canvas = new CanvasGL2d();
            var lionFill = new LionFillSprite();
            //-----------------------------------------------


            form.SetGLPaintHandler((o, s) =>
            {
                canvas.Clear(LayoutFarm.Drawing.Color.White);

                canvas.FillColor = LayoutFarm.Drawing.Color.Blue;
                canvas.FillRect(0, 0, 400, 400);
                //draw vxs direct to GL surface 
                lionFill.Draw(canvas); //before offset

                canvas.SetCanvasOrigin(50, 50);
                lionFill.Draw(canvas);
                canvas.SetCanvasOrigin(0, 0);
            });
            form.Show();
        }
    }
}