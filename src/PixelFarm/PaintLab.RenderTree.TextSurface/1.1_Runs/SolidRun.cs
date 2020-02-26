﻿//Apache2, 2014-present, WinterDev

using System;
using System.Text;
using PixelFarm.Drawing;

namespace LayoutFarm.TextEditing
{
    class SolidRun : Run
    {
        //TODO: review here=> who should store/handle this handle? , owner TextBox or this run?
        Action<SolidRun, DrawBoard, Rectangle> _externalCustomDraw;
        char[] _mybuffer;
        RenderElement _externalRenderE;

        public SolidRun(char[] copyBuffer, RunStyle style)
            : base(style)
        {
            //check line break? 

            _mybuffer = copyBuffer;
            UpdateRunWidth();
        }

        public SolidRun(string str, RunStyle style)
            : base(style)
        {

            if (str != null && str.Length > 0)
            {
                _mybuffer = str.ToCharArray();
                if (_mybuffer.Length == 1 && _mybuffer[0] == '\n')
                {
                    //this.IsLineBreak = true;
                    throw new NotSupportedException();
                }
                UpdateRunWidth();
            }
            else
            {
                throw new Exception("string must be null or zero length");
            }
        }
        public void SetCustomExternalDraw(Action<SolidRun, DrawBoard, Rectangle> externalCustomDraw)
        {
            _externalCustomDraw = externalCustomDraw;
        }
        public RenderElement ExternalRenderElement
        {
            get => _externalRenderE;
            set => _externalRenderE = value;
        }

        public string RawText { get; set; }

        public override CopyRun CreateCopy()
        {
            return new CopyRun(GetText())
            {
                RunKind = RunKind.Solid,
            };
        }
        public override CopyRun Copy(int startIndex)
        {
            if (startIndex == 0)
            {
                int length = _mybuffer.Length - startIndex;
                if (startIndex > -1 && length > 0)
                {
                    return MakeTextRun(startIndex, length);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        CopyRun MakeTextRun(int sourceIndex, int length)
        {
            if (length > 0)
            {
                sourceIndex = 0;
                length = _mybuffer.Length;
                CopyRun newTextRun = null;
                char[] newContent = new char[length];
                Array.Copy(_mybuffer, sourceIndex, newContent, 0, length);
                //SolidTextRun solidRun = new SolidTextRun(this.Root, newContent, this.SpanStyle) { RawText = this.RawText };
                CopyRun solidRun = new CopyRun(this.RawText);
                solidRun.RunKind = RunKind.Solid;

                //TODO: review this again!
                //solidRun.SetCustomExternalDraw(_externalCustomDraw); //also copy drawing handler?
                //newTextRun = solidRun;
                //newTextRun.IsLineBreak = this.IsLineBreak;
                //newTextRun.UpdateRunWidth();
                return newTextRun;
            }
            else
            {
                throw new Exception("string must be null or zero length");
            }
        }
        public override int GetRunWidth(int charOffset)
        {
            return CalculateDrawingStringSize(_mybuffer, charOffset).Width;
        }
        public override string GetText()
        {
            return new string(_mybuffer);
        }



        public override void UpdateRunWidth()
        {
            Size size;
            //if (IsLineBreak)
            //{
            //    size = new Size(0, (int)Math.Round(Root.TextServices.MeasureBlankLineHeight(GetFont())));
            //}
            //else
            //{
            size = CalculateDrawingStringSize(_mybuffer, _mybuffer.Length);
            //}
            //this.SetSize(size.Width, size.Height);
            DirectSetSize(this, size.Width, size.Height);
            MarkHasValidCalculateSize();
        }
        public override char GetChar(int index)
        {
            return _mybuffer[index];
        }
        public override void CopyContentToStringBuilder(StringBuilder stBuilder)
        {
            //if (IsLineBreak)
            //{
            //    stBuilder.Append("\r\n");
            //}
            //else
            //{
            stBuilder.Append(RawText);
            //}
        }
        public override int CharacterCount
        {
            get
            {
                switch (_mybuffer.Length)
                {
                    case 0: return 0;
                    default: return 1;
                }
            }
        }

        Size CalculateDrawingStringSize(char[] buffer, int length)
        {
            var textBufferSpan = new TextBufferSpan(buffer, 0, length);
            return MeasureString(ref textBufferSpan);
        }

        public override CopyRun Copy(int startIndex, int length)
        {
            if (startIndex > -1 && length > 0)
            {
                return MakeTextRun(startIndex, length);
            }
            else
            {
                return null;
            }
        }
        const int SAME_FONT_SAME_TEXT_COLOR = 0;
        const int SAME_FONT_DIFF_TEXT_COLOR = 1;
        const int DIFF_FONT_SAME_TEXT_COLOR = 2;
        const int DIFF_FONT_DIFF_TEXT_COLOR = 3;
        static int EvaluateFontAndTextColor(DrawBoard canvas, RunStyle spanStyle)
        {
            RequestFont font = spanStyle.ReqFont;
            Color color = spanStyle.FontColor;
            RequestFont currentTextFont = canvas.CurrentFont;
            Color currentTextColor = canvas.CurrentTextColor;
            if (font != null && font != currentTextFont)
            {
                if (currentTextColor != color)
                {
                    return DIFF_FONT_DIFF_TEXT_COLOR;
                }
                else
                {
                    return DIFF_FONT_SAME_TEXT_COLOR;
                }
            }
            else
            {
                if (currentTextColor != color)
                {
                    return SAME_FONT_DIFF_TEXT_COLOR;
                }
                else
                {
                    return SAME_FONT_SAME_TEXT_COLOR;
                }
            }
        }
        // 
        public override void Draw(DrawBoard canvas, Rectangle updateArea)
        {
            if (_externalCustomDraw != null)
            {
                _externalCustomDraw(this, canvas, updateArea);
                return;
            }
            else if (_externalRenderE != null)
            {
                //_externalRenderE.DrawToThisCanvas(canvas, updateArea);                 
                RenderElement.Temp_CustomDrawToThisCanvas(_externalRenderE, canvas, updateArea);
                return;
            }

            int bWidth = this.Width;
            int bHeight = this.Height;

            //1. bg
            canvas.FillRectangle(Color.Yellow, 0, 0, bWidth, bHeight);

            //if (!this.HasStyle)
            //{
            //    canvas.DrawText(_mybuffer, new Rectangle(0, 0, bWidth, bHeight), 0);
            //}
            //else
            //{
            //TODO: review here, we don't need to do this

            RunStyle style = this.RunStyle;
            switch (EvaluateFontAndTextColor(canvas, style))
            {
                case DIFF_FONT_SAME_TEXT_COLOR:
                    {
                        var prevFont = canvas.CurrentFont;
                        canvas.CurrentFont = style.ReqFont;
                        canvas.DrawText(_mybuffer,
                           new Rectangle(0, 0, bWidth, bHeight),
                           style.ContentHAlign);
                        canvas.CurrentFont = prevFont;
                    }
                    break;
                case DIFF_FONT_DIFF_TEXT_COLOR:
                    {
                        var prevFont = canvas.CurrentFont;
                        var prevColor = canvas.CurrentTextColor;
                        canvas.CurrentFont = style.ReqFont;
                        canvas.CurrentTextColor = style.FontColor;
                        canvas.DrawText(_mybuffer,
                           new Rectangle(0, 0, bWidth, bHeight),
                           style.ContentHAlign);
                        canvas.CurrentFont = prevFont;
                        canvas.CurrentTextColor = prevColor;
                    }
                    break;
                case SAME_FONT_DIFF_TEXT_COLOR:
                    {
                        var prevColor = canvas.CurrentTextColor;
                        canvas.DrawText(_mybuffer,
                            new Rectangle(0, 0, bWidth, bHeight),
                            style.ContentHAlign);
                        canvas.CurrentTextColor = prevColor;
                    }
                    break;
                default:
                    {
                        canvas.DrawText(_mybuffer,
                           new Rectangle(0, 0, bWidth, bHeight),
                           style.ContentHAlign);
                    }
                    break;
            }
            //}
        }


        public override CharLocation GetCharacterFromPixelOffset(int pixelOffset)
        {
            if (pixelOffset < Width)
            {
                return new CharLocation(0, 0);
            }
            else
            {
                //exceed than the bound of this run
                return new CharLocation(0, 1);
            }
        }
        //-------------------------------------------
        //
        internal override bool IsInsertable => false;
        //
        public override CopyRun LeftCopy(int index)
        {
            if (index == 0)
            {
                return null;
            }

            if (index > -1)
            {
                return MakeTextRun(0, _mybuffer.Length);
            }
            else
            {
                return null;
            }
        }
        internal override void InsertAfter(int index, char c)
        {
            //TODO: review here
            //solid text run should not be editable
            int oldLexLength = _mybuffer.Length;
            char[] newBuff = new char[oldLexLength + 1];
            if (index > -1 && index < _mybuffer.Length - 1)
            {
                Array.Copy(_mybuffer, newBuff, index + 1);
                newBuff[index + 1] = c;
                Array.Copy(_mybuffer, index + 1, newBuff, index + 2, oldLexLength - index - 1);
            }
            else if (index == -1)
            {
                newBuff[0] = c;
                Array.Copy(_mybuffer, 0, newBuff, 1, _mybuffer.Length);
            }
            else if (index == oldLexLength - 1)
            {
                Array.Copy(_mybuffer, newBuff, oldLexLength);
                newBuff[oldLexLength] = c;
            }
            else
            {
                throw new NotSupportedException();
            }
            _mybuffer = newBuff;
            UpdateRunWidth();
        }

        internal override CopyRun Remove(int startIndex, int length, bool withFreeRun)
        {
            if (startIndex == _mybuffer.Length)
            {
                //at the end
                return null;
            }

            //
            startIndex = 0; //***
            length = _mybuffer.Length;
            CopyRun freeRun = null;
            if (startIndex > -1 && length > 0)
            {
                int oldLexLength = _mybuffer.Length;
                char[] newBuff = new char[oldLexLength - length];
                if (withFreeRun)
                {
                    freeRun = MakeTextRun(startIndex, length);
                }
                if (startIndex > 0)
                {
                    Array.Copy(_mybuffer, 0, newBuff, 0, startIndex);
                }

                Array.Copy(_mybuffer, startIndex + length, newBuff, startIndex, oldLexLength - startIndex - length);
                _mybuffer = newBuff;
                UpdateRunWidth();
            }

            if (withFreeRun)
            {
                return freeRun;
            }
            else
            {
                return null;
            }
        }
    }
}
