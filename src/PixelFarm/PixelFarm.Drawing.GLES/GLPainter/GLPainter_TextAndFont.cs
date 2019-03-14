﻿//MIT, 2016-present, WinterDev
//Apache2, https://xmlgraphics.apache.org/

using System;
using PixelFarm.Drawing;

namespace PixelFarm.DrawingGL
{
    partial class GLPainter
    {
        public Color FontFillColor
        {
            get => _pcx.FontFillColor;
            set => _pcx.FontFillColor = value;
        }
        public ITextPrinter TextPrinter
        {
            get => _textPrinter;
            set
            {
                _textPrinter = value;
                if (value != null && _requestFont != null)
                {
                    _textPrinter.ChangeFont(_requestFont);
                }
            }
        }
        public override RequestFont CurrentFont
        {
            get => _requestFont;
            set
            {
                _requestFont = value;
                if (_textPrinter != null)
                {
                    _textPrinter.ChangeFont(value);
                }
            }
        }
        public override void DrawString(string text, double left, double top)
        {
            _textPrinter?.DrawString(text, left, top);
        }
        public override RenderVxFormattedString CreateRenderVx(string textspan)
        {

            if (_textPrinter != null)
            {
                char[] buffer = textspan.ToCharArray();
                var renderVxFmtStr = new GLRenderVxFormattedString();
                _textPrinter?.PrepareStringForRenderVx(renderVxFmtStr, buffer, 0, buffer.Length);
                return renderVxFmtStr;
            }
            else
            {
                return null;
            }
        }
        public override RenderVxFormattedString CreateRenderVx(char[] textspanBuff, int startAt, int len)
        {
            if (_textPrinter != null)
            {
                var renderVxFmtStr = new GLRenderVxFormattedString();
                _textPrinter?.PrepareStringForRenderVx(renderVxFmtStr, textspanBuff, startAt, len);
                return renderVxFmtStr;
            }
            else
            {
                return null;
            }
        }
        public override void DrawString(RenderVxFormattedString renderVx, double x, double y)
        {
            // 
            _textPrinter?.DrawString(renderVx, x, y);
        }

    }
}