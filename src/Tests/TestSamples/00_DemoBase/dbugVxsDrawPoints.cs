﻿//MIT, 2014-present, WinterDev

using System;
using System.Collections.Generic;
using PixelFarm.CpuBlit;
using PixelFarm.Drawing;
namespace Mini
{
#if DEBUG
    public static class dbugVxsDrawPoints
    {
        public static void DrawVxsPoints(VertexStore vxs, PixelFarm.Drawing.Painter p)
        {
            int j = vxs.Count;
            for (int i = 0; i < j; ++i)
            {
                double x, y;
                var cmd = vxs.GetVertex(i, out x, out y);
                switch (cmd)
                {
                    case VertexCmd.MoveTo:
                        {
                            p.FillColor = PixelFarm.Drawing.Color.Blue;
                            p.FillRect(x, y, 5, 5);
                            p.DrawString(i.ToString(), x, y + 5);
                        }
                        break;
                    case VertexCmd.C3:
                        {
                            p.FillColor = PixelFarm.Drawing.Color.Red;
                            p.FillRect(x, y, 5, 5);
                            p.DrawString(i.ToString(), x, y + 5);
                        }
                        break;
                    case VertexCmd.C4:
                        {
                            p.FillColor = PixelFarm.Drawing.Color.Gray;
                            p.FillRect(x, y, 5, 5);
                            p.DrawString(i.ToString(), x, y + 5);
                        }
                        break;
                    case VertexCmd.LineTo:
                        {
                            p.FillColor = PixelFarm.Drawing.Color.Yellow;
                            p.FillRect(x, y, 5, 5);
                            p.DrawString(i.ToString(), x, y + 5);
                        }
                        break;
                }
            }
        }
    }

#endif
}