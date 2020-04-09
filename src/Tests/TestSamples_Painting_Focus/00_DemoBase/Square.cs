﻿//BSD, 2014-present, WinterDev
//MattersHackers
//AGG 2.4


using PixelFarm.Drawing;
using PixelFarm.CpuBlit.Rasterization;
namespace PixelFarm.CpuBlit
{

    public class Square
    {
        double _size;
        public Square(double size)
        {
            _size = size;
        }

        public void Draw(
            DestBitmapRasterizer bmpRast,
            ScanlineRasterizer ras,
            Scanline sl,
            PixelProcessing.IBitmapBlender destImage, Color color,
            double x, double y)
        {

            ras.Reset();
            ras.MoveTo(x * _size, y * _size);
            ras.LineTo(x * _size + _size, y * _size);
            ras.LineTo(x * _size + _size, y * _size + _size);
            ras.LineTo(x * _size, y * _size + _size);
            bmpRast.RenderWithColor(destImage, ras, sl, color);
        }
    }
}