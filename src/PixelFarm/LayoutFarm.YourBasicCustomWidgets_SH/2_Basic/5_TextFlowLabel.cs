﻿//MIT, 2019-present, WinterDev

using PixelFarm.Drawing;
using LayoutFarm.UI;
using LayoutFarm.TextEditing;

namespace LayoutFarm.CustomWidgets
{
    public class FlowLabel : AbstractRectUI
    {

        Color _textColor;
        Color _backColor;
        RunStyle _runStyle;
        RequestFont _font;
        TextFlowRenderBox _textFlowRenderBox;
        PlainTextDocument _doc;

        public FlowLabel(int w, int h) : base(w, h)
        {
            _textColor = PixelFarm.Drawing.Color.Black; //default?, use Theme?
        }
        public RequestFont RequestFont
        {
            get => _font;
            set
            {
                _font = value;
                if (_textFlowRenderBox != null)
                {
                    //apply new font to all text in the flow render box
                }
            }
        }
        RunStyle GetDefaultRunStyle()
        {
            if (_runStyle == null)
            {
                return _runStyle = new RunStyle(_textFlowRenderBox.Root.TextServices)
                {
                    FontColor = Color.Black,
                    ReqFont = _font
                };
            }
            else
            {
                return _runStyle;
            }
        }

        public ContentTextSplitter TextSplitter
        {
            get;
            set;
        }
        public override RenderElement CurrentPrimaryRenderElement => _textFlowRenderBox;

        public override RenderElement GetPrimaryRenderElement(RootGraphic rootgfx)
        {
            if (_textFlowRenderBox == null)
            {
                if (_font == null)
                {
                    _font = new RequestFont("tahoma", 11);
                }

                _runStyle = new RunStyle(rootgfx.TextServices) { FontColor = _textColor, ReqFont = _font };

                var txtFlowRenderBox = new TextFlowRenderBox(rootgfx, this.Width, this.Height, true);
                //txtFlowRenderBox.BackgroundColor = _backColor;
                txtFlowRenderBox.SetLocation(this.Left, this.Top);
                txtFlowRenderBox.SetViewport(this.ViewportLeft, this.ViewportTop);
                txtFlowRenderBox.SetVisible(this.Visible);
                txtFlowRenderBox.SetController(this);

                //
                _textFlowRenderBox = txtFlowRenderBox;
                ReloadDocument();
            }
            return _textFlowRenderBox;
        }
        public string Text
        {
            get
            {
                return null;
            }
            set
            {
                //_text = value;
                _doc = PlainTextDocumentHelper.CreatePlainTextDocument(value);
                if (_textFlowRenderBox != null)
                {
                    ReloadDocument();
                }
            }
        }
        void ReloadDocument()
        {
            if (_doc == null)
            {
                return;
            }

            _textFlowRenderBox.ClearAllChildren();
            int lineCount = 0;

            RunStyle runstyle = GetDefaultRunStyle();
            foreach (PlainTextLine line in _doc.GetLineIter())
            {
                if (lineCount > 0)
                {
                    _textFlowRenderBox.SplitCurrentLineToNewLine();
                }
                //we create an unparse text run***
                _textFlowRenderBox.AddTextLine(line);
                lineCount++;
            }

            this.InvalidateGraphics();
        }

        //----------
        protected override void OnMouseDown(UIMouseEventArgs e)
        {
            this.Focus();
            e.MouseCursorStyle = MouseCursorStyle.IBeam;
            e.CancelBubbling = true;
            e.CurrentContextElement = this;
            _textFlowRenderBox.HandleMouseDown(e);
        }
        protected override void OnMouseMove(UIMouseEventArgs e)
        {
            if (e.IsDragging)
            {
                _textFlowRenderBox.HandleDrag(e);
                e.CancelBubbling = true;
                e.MouseCursorStyle = MouseCursorStyle.IBeam;
            }
        }
        protected override void OnMouseUp(UIMouseEventArgs e)
        {
            if (e.IsDragging)
            {
                _textFlowRenderBox.HandleDragEnd(e);
            }
            else
            {
                _textFlowRenderBox.HandleMouseUp(e);
            }
            e.MouseCursorStyle = MouseCursorStyle.Default;
            e.CancelBubbling = true;
        }
        protected override void OnMouseWheel(UIMouseEventArgs e)
        {
            //mouse wheel on  
            _textFlowRenderBox.HandleMouseWheel(e);
            e.CancelBubbling = true;
        }
        protected override void OnDoubleClick(UIMouseEventArgs e)
        {
            _textFlowRenderBox.HandleDoubleClick(e);
            e.CancelBubbling = true;
        }
        protected override void OnMouseLeave(UIMouseEventArgs e)
        {
            e.MouseCursorStyle = MouseCursorStyle.Arrow;
        }

        //
        protected override void OnKeyPress(UIKeyEventArgs e)
        {
            //eg. mask text
            //we collect actual key and send the mask to to the background  
            _textFlowRenderBox.HandleKeyPress(e);
            e.CancelBubbling = true;
        }
        protected override void OnKeyDown(UIKeyEventArgs e)
        {
            _textFlowRenderBox.HandleKeyDown(e);
            e.CancelBubbling = true;
        }
        protected override void OnKeyUp(UIKeyEventArgs e)
        {
            _textFlowRenderBox.HandleKeyUp(e);
            e.CancelBubbling = true;
        }
        protected override bool OnProcessDialogKey(UIKeyEventArgs e)
        {
            if (_textFlowRenderBox.HandleProcessDialogKey(e))
            {
                e.CancelBubbling = true;
                return true;
            }
            return false;
        }
    }
}