﻿//Apache2, 2014-present, WinterDev

namespace LayoutFarm.TextEditing
{
    public abstract class VisualPointInfo
    {
        readonly int _lineCharIndex;
        int _onTextRunCharOffset;
        int _caretXPos;
        int _onTextRunPixelOffset;
        public VisualPointInfo(int lineCharIndex)
        {
            _lineCharIndex = lineCharIndex;
        }
        public void SetAdditionVisualInfo(int onTextRunCharOffset, int caretXPos, int textRunPixelOffset)
        {
            _caretXPos = caretXPos;
            _onTextRunCharOffset = onTextRunCharOffset;
            _onTextRunPixelOffset = textRunPixelOffset;
        }
        public int LineCharIndex => _lineCharIndex;

        public int TextRunCharOffset => _onTextRunCharOffset;

        internal abstract EditableRun TextRun { get; }

        //public bool IsOnTheBeginOfLine => RunLocalSelectedIndex == -1;
        public bool IsOnTheBeginOfLine => _lineCharIndex == 0;
        public abstract int LineId { get; }
        public abstract int LineTop { get; }
        public abstract int ActualLineHeight { get; }
        public abstract int LineNumber { get; }
        public abstract int CurrentWidth { get; }

        public int RunLocalSelectedIndex => _lineCharIndex - _onTextRunCharOffset;

        public int X => _caretXPos;

        public int TextRunPixelOffset => _onTextRunPixelOffset;

        //#if DEBUG
        //        public override string ToString()
        //        {
        //            if (_copyRun == null)
        //            {
        //                return "null " + " ,local[" + RunLocalSelectedIndex + "]";
        //            }
        //            else
        //            {
        //                return _copyRun.ToString() + " ,local[" + RunLocalSelectedIndex + "]";
        //            }
        //        }
        //#endif

    }

    

    public class EditableVisualPointInfo : VisualPointInfo
    {
        EditableTextLine _line;
        EditableRun _cacheEditableTextRun;
        internal EditableVisualPointInfo(EditableTextLine line, int index, EditableRun cacheEditableTextRun)
            : base(index)
        {
            if (index < 0)
            {
            }
            _line = line;
            _cacheEditableTextRun = cacheEditableTextRun;
        }
        internal override EditableRun TextRun => _cacheEditableTextRun;

        internal EditableTextLine Line => _line;

        internal EditableTextLine EditableLine => _line;

        public override int LineTop => _line.LineTop;

        public override int CurrentWidth => _line.LineWidth;

        public override int ActualLineHeight => _line.ActualLineHeight;

        public override int LineNumber => _line.LineNumber;

        public override int LineId => _line.LineNumber;

    }
}
