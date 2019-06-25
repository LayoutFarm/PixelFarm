﻿//Apache2, 2014-present, WinterDev

using System;
using System.Collections.Generic;
using System.Text;
using PixelFarm.Drawing;

namespace LayoutFarm.TextEditing
{
    public enum RunKind : byte
    {
        Text,
        Image,
        Solid
    }

    public class CopyRun
    {
        public RunKind RunKind { get; set; }
        public char[] RawContent { get; set; }
        public TextSpanStyle SpanStyle { get; set; }
        public CopyRun() { }
        public CopyRun(string rawContent)
        {
            RawContent = rawContent.ToCharArray();
        }
        public int CharacterCount
        {
            get
            {
                switch (RunKind)
                {
                    case RunKind.Image:
                    case RunKind.Solid: return 1;
                    case RunKind.Text:
                        return RawContent.Length;
                    default: throw new NotSupportedException();
                }
            }
        }
        public void CopyContentToStringBuilder(StringBuilder stbuilder)
        {
            throw new NotSupportedException();
            //if (IsLineBreak)
            //{
            //    stBuilder.Append("\r\n");
            //}
            //else
            //{
            //    stBuilder.Append(_mybuffer);
            //}
        }
    }



    public class TextRangeCopy
    {
        public class TextLine
        {
            LinkedList<CopyRun> _runs = new LinkedList<CopyRun>();
            public TextLine()
            {

            }
            public IEnumerable<CopyRun> GetRunIter()
            {
                var node = _runs.First;
                while (node != null)
                {
                    yield return node.Value;
                    node = node.Next;//**
                }

            }
            public int RunCount => _runs.Count;
            public void Append(CopyRun run) => _runs.AddLast(run);
            public void CopyContentToStringBuilder(StringBuilder stbuilder)
            {
                foreach (CopyRun run in _runs)
                {
                    stbuilder.Append(run.RawContent);
                }
            }
        }

        TextLine _currentLine;
        LinkedList<TextLine> _lines;
        public TextRangeCopy()
        {
            _currentLine = new TextLine();//create default blank lines
        }
        public bool HasSomeRuns
        {
            get
            {
                if (_lines == null)
                {
                    return _currentLine.RunCount > 0;
                }
                else
                {
                    //has more than 1 line (at least we have a line break)
                    return true;
                }
            }
        }
        public IEnumerable<TextLine> GetTextLineIter()
        {
            if (_lines == null)
            {
                yield return _currentLine;
            }
            else
            {
                var node = _lines.First;
                while (node != null)
                {
                    yield return node.Value;
                    node = node.Next;//***
                }
            }
        }
        public void AppendNewLine()
        {
            if (_lines == null)
            {
                _lines = new LinkedList<TextLine>();
                //add current line ot the collection
                _lines.AddLast(_currentLine);
            }
            //new line
            _currentLine = new TextLine();
            _lines.AddLast(_currentLine);
        }
        public void AddRun(CopyRun copyRun)
        {
            _currentLine.Append(copyRun);
        }
        public void Clear()
        {
            if (_lines != null)
            {
                _lines.Clear();
                _lines = null;
            }
            _currentLine = new TextLine();
        }
        public void CopyContentToStringBuilder(StringBuilder stbuilder)
        {
            if (!HasSomeRuns) return;
            //

            if (_lines == null)
            {
                _currentLine.CopyContentToStringBuilder(stbuilder);
            }
            else
            {
                bool passFirstLine = false;
                foreach (TextLine line in _lines)
                {
                    if (passFirstLine)
                    {
                        stbuilder.AppendLine();
                    }
                    line.CopyContentToStringBuilder(stbuilder);
                    passFirstLine = true;
                }
            }

        }
    }


    partial class EditableTextLine
    {
        public static void InnerDoJoinWithNextLine(EditableTextLine line)
        {
            line.JoinWithNextLine();
        }
        void JoinWithNextLine()
        {
            if (!IsLastLine)
            {
                EditableTextLine lowerLine = EditableFlowLayer.GetTextLine(_currentLineNumber + 1);
                this.LocalSuspendLineReArrange();
                int cx = 0;
                EditableRun lastTextRun = (EditableRun)this.LastRun;
                if (lastTextRun != null)
                {
                    cx = lastTextRun.Right;
                }

                foreach (EditableRun r in lowerLine._runs)
                {
                    this.AddLast(r);
                    EditableRun.DirectSetLocation(r, cx, 0);
                    cx += r.Width;
                }
                this.LocalResumeLineReArrange();
                this.EndWithLineBreak = lowerLine.EndWithLineBreak;
                EditableFlowLayer.Remove(lowerLine._currentLineNumber);
            }
        }
        internal void UnsafeDetachFromFlowLayer()
        {
            this.EditableFlowLayer = null;
        }
        public void Copy(TextRangeCopy output)
        {
            LinkedListNode<EditableRun> curNode = this.First;
            while (curNode != null)
            {
                output.AddRun(curNode.Value.Clone());
                curNode = curNode.Next;
            }
        }
        public void Copy(VisualSelectionRange selectionRange, TextRangeCopy output)
        {
            EditableVisualPointInfo startPoint = selectionRange.StartPoint;
            EditableVisualPointInfo endPoint = selectionRange.EndPoint;
            if (startPoint.TextRun != null)
            {
                if (startPoint.TextRun == endPoint.TextRun)
                {
                    CopyRun elem =
                      startPoint.TextRun.Copy(
                        startPoint.RunLocalSelectedIndex,
                        endPoint.LineCharIndex - startPoint.LineCharIndex);
                    if (elem != null)
                    {
                        output.AddRun(elem);
                    }
                }
                else
                {
                    EditableTextLine startLine = null;
                    EditableTextLine stopLine = null;
                    if (startPoint.LineId == _currentLineNumber)
                    {
                        startLine = this;
                    }
                    else
                    {
                        startLine = EditableFlowLayer.GetTextLine(startPoint.LineId);
                    }
                    if (endPoint.LineId == _currentLineNumber)
                    {
                        stopLine = this;
                    }
                    else
                    {
                        stopLine = EditableFlowLayer.GetTextLine(endPoint.LineId);
                    }
                    if (startLine == stopLine)
                    {
                        CopyRun postCutTextRun = startPoint.TextRun.Copy(startPoint.RunLocalSelectedIndex);
                        if (postCutTextRun != null)
                        {
                            output.AddRun(postCutTextRun);
                        }
                        if (startPoint.TextRun.NextTextRun != endPoint.TextRun)
                        {
                            foreach (EditableRun t in EditableFlowLayer.TextRunForward(startPoint.TextRun.NextTextRun, endPoint.TextRun.PrevTextRun))
                            {
                                output.AddRun(t.Clone());
                            }
                        }

                        CopyRun preCutTextRun = endPoint.TextRun.LeftCopy(endPoint.RunLocalSelectedIndex);
                        if (preCutTextRun != null)
                        {
                            output.AddRun(preCutTextRun);
                        }
                    }
                    else
                    {
                        int startLineId = startPoint.LineId;
                        int stopLineId = endPoint.LineId;
                        startLine.RightCopy(startPoint, output);
                        for (int i = startLineId + 1; i < stopLineId; i++)
                        {
                            //begine new line
                            output.AppendNewLine();
                            EditableTextLine line = EditableFlowLayer.GetTextLine(i);
                            line.Copy(output);
                        }
                        if (endPoint.LineCharIndex > -1)
                        {
                            output.AppendNewLine();
                            stopLine.LeftCopy(endPoint, output);
                        }
                    }
                }
            }
            else
            {
                EditableTextLine startLine = null;
                EditableTextLine stopLine = null;
                if (startPoint.LineId == _currentLineNumber)
                {
                    startLine = this;
                }
                else
                {
                    startLine = EditableFlowLayer.GetTextLine(startPoint.LineId);
                }

                if (endPoint.LineId == _currentLineNumber)
                {
                    stopLine = this;
                }
                else
                {
                    stopLine = EditableFlowLayer.GetTextLine(endPoint.LineId);
                }


                if (startLine == stopLine)
                {
                    if (startPoint.LineCharIndex == -1)
                    {
                        foreach (EditableRun t in EditableFlowLayer.TextRunForward(startPoint.TextRun, endPoint.TextRun.PrevTextRun))
                        {
                            output.AddRun(t.Clone());
                        }
                        CopyRun postCutTextRun = endPoint.TextRun.Copy(endPoint.RunLocalSelectedIndex + 1);
                        if (postCutTextRun != null)
                        {
                            output.AddRun(postCutTextRun);
                        }
                    }
                    else
                    {
                        CopyRun postCutTextRun = startPoint.TextRun.Copy(startPoint.RunLocalSelectedIndex + 1);
                        if (postCutTextRun != null)
                        {
                            output.AddRun(postCutTextRun);
                        }

                        foreach (EditableRun t in EditableFlowLayer.TextRunForward(startPoint.TextRun.NextTextRun, endPoint.TextRun.PrevTextRun))
                        {
                            output.AddRun(t.Clone());
                        }

                        CopyRun preCutTextRun = endPoint.TextRun.LeftCopy(startPoint.RunLocalSelectedIndex);
                        if (preCutTextRun != null)
                        {
                            output.AddRun(preCutTextRun);
                        }
                    }
                }
                else
                {
                    int startLineId = startPoint.LineId;
                    int stopLineId = endPoint.LineId;
                    startLine.RightCopy(startPoint, output);
                    for (int i = startLineId + 1; i < stopLineId; i++)
                    {
                        output.AppendNewLine();
                        EditableTextLine line = EditableFlowLayer.GetTextLine(i);
                        line.Copy(output);
                    }
                    stopLine.LeftCopy(endPoint, output);
                }
            }
        }
        internal TextSpanStyle CurrentTextSpanStyle
        {
            get
            {
                return this.OwnerFlowLayer.CurrentTextSpanStyle;
            }
        }
        internal void Remove(VisualSelectionRange selectionRange)
        {
            EditableVisualPointInfo startPoint = selectionRange.StartPoint;
            EditableVisualPointInfo endPoint = selectionRange.EndPoint;
            if (startPoint.TextRun != null)
            {
                if (startPoint.TextRun == endPoint.TextRun)
                {
                    EditableRun removedRun = startPoint.TextRun;
                    EditableRun.InnerRemove(removedRun,
                                    startPoint.RunLocalSelectedIndex,
                                    endPoint.LineCharIndex - startPoint.LineCharIndex, false);
                    if (removedRun.CharacterCount == 0)
                    {
                        if (startPoint.LineId == _currentLineNumber)
                        {
                            this.Remove(removedRun);
                        }
                        else
                        {
                            EditableTextLine line = EditableFlowLayer.GetTextLine(startPoint.LineId);
                            line.Remove(removedRun);
                        }
                    }
                }
                else
                {
                    EditableVisualPointInfo newStartPoint = null;
                    EditableVisualPointInfo newStopPoint = null;
                    EditableTextLine startLine = null;
                    EditableTextLine stopLine = null;
                    if (startPoint.LineId == _currentLineNumber)
                    {
                        startLine = this;
                    }
                    else
                    {
                        startLine = EditableFlowLayer.GetTextLine(startPoint.LineId);
                    }
                    newStartPoint = startLine.Split(startPoint);
                    if (endPoint.LineId == _currentLineNumber)
                    {
                        stopLine = this;
                    }
                    else
                    {
                        stopLine = EditableFlowLayer.GetTextLine(endPoint.LineId);
                    }

                    newStopPoint = stopLine.Split(endPoint);
                    if (startLine == stopLine)
                    {
                        if (newStartPoint.TextRun != null)
                        {
                            LinkedList<EditableRun> tobeRemoveRuns = new LinkedList<EditableRun>();
                            if (newStartPoint.LineCharIndex == 0)
                            {
                                foreach (EditableRun t in EditableFlowLayer.TextRunForward(
                                     newStartPoint.TextRun,
                                     newStopPoint.TextRun))
                                {
                                    tobeRemoveRuns.AddLast(t);
                                }
                            }
                            else
                            {
                                foreach (EditableRun t in EditableFlowLayer.TextRunForward(
                                     newStartPoint.TextRun.NextTextRun,
                                      newStopPoint.TextRun))
                                {
                                    tobeRemoveRuns.AddLast(t);
                                }
                            }
                            startLine.LocalSuspendLineReArrange();
                            foreach (EditableRun t in tobeRemoveRuns)
                            {
                                startLine.Remove(t);
                            }
                            startLine.LocalResumeLineReArrange();
                        }
                        else
                        {
                            //this may be the blank line
                            startLine.Clear();
#if DEBUG
                            //TODO: review here again
                            //System.Diagnostics.Debug.WriteLine("EditableTextLine_adv1");
#endif
                        }
                    }
                    else
                    {
                        int startLineId = newStartPoint.LineId;
                        int stopLineId = newStopPoint.LineId;
                        if (newStopPoint.LineCharIndex > 0)
                        {
                            stopLine.RemoveLeft((EditableRun)newStopPoint.TextRun);
                        }
                        for (int i = stopLineId - 1; i > startLineId; i--)
                        {
                            EditableTextLine line = EditableFlowLayer.GetTextLine(i);
                            line.Clear();
                            line.JoinWithNextLine();
                        }
                        if (newStartPoint.LineCharIndex == 0)
                        {
                            startLine.RemoveRight(newStartPoint.TextRun);
                        }
                        else
                        {
                            EditableRun nextRun = (newStartPoint.TextRun).NextTextRun;
                            if (nextRun != null)// && !nextRun.IsLineBreak)
                            {
                                startLine.RemoveRight(nextRun);
                            }
                        }
                        startLine.JoinWithNextLine();
                    }
                }
            }
            else
            {
                VisualPointInfo newStartPoint = null;
                VisualPointInfo newStopPoint = null;
                EditableTextLine startLine = null;
                EditableTextLine stopLine = null;
                if (startPoint.LineId == _currentLineNumber)
                {
                    startLine = this;
                }
                else
                {
                    startLine = EditableFlowLayer.GetTextLine(startPoint.LineId);
                }
                newStartPoint = startLine.Split(startPoint);
                if (endPoint.LineId == _currentLineNumber)
                {
                    stopLine = this;
                }
                else
                {
                    stopLine = EditableFlowLayer.GetTextLine(endPoint.LineId);
                }
                newStopPoint = stopLine.Split(endPoint);
                if (startLine == stopLine)
                {
                    if (newStartPoint.TextRun != null)
                    {
                        LinkedList<EditableRun> tobeRemoveRuns = new LinkedList<EditableRun>();
                        if (newStartPoint.LineCharIndex == -1)
                        {
                            foreach (EditableRun t in EditableFlowLayer.TextRunForward(
                                 newStartPoint.TextRun,
                                 newStopPoint.TextRun))
                            {
                                tobeRemoveRuns.AddLast(t);
                            }
                        }
                        else
                        {
                            foreach (EditableRun t in EditableFlowLayer.TextRunForward(
                                newStartPoint.TextRun.NextTextRun,
                                newStopPoint.TextRun))
                            {
                                tobeRemoveRuns.AddLast(t);
                            }
                        }
                        foreach (EditableRun t in tobeRemoveRuns)
                        {
                            startLine.Remove(t);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    int startLineId = newStartPoint.LineId;
                    int stopLineId = newStopPoint.LineId;
                    if (newStopPoint.LineCharIndex > -1)
                    {
                        stopLine.RemoveLeft(newStopPoint.TextRun);
                    }
                    for (int i = stopLineId - 1; i > startLineId; i--)
                    {
                        EditableTextLine line = EditableFlowLayer.GetTextLine(i);
                        line.Clear();
                        line.JoinWithNextLine();
                    }
                    if (newStartPoint.LineCharIndex == -1)
                    {
                        //TODO: review here again
                        //at this point newStartPoint.TextRun should always null
                        if (newStartPoint.TextRun != null)
                        {
                            startLine.RemoveRight(newStartPoint.TextRun);
                        }
                    }
                    else
                    {
                        //at this point newStartPoint.TextRun should always null
                        //TODO newStartPoint.TextRun == null???
                        if (newStartPoint.TextRun != null)
                        {
                            EditableRun nextRun = newStartPoint.TextRun.NextTextRun;
                            if (nextRun != null)
                            {
                                startLine.RemoveRight(nextRun);
                            }
                        }

                    }
                    startLine.JoinWithNextLine();
                }
            }
        }

        EditableRun GetPrevTextRun(EditableRun run)
        {
            return run.PrevTextRun;
            //if (IsSingleLine)
            //{
            //    return run.PrevTextRun;
            //}
            //else
            //{
            //    if (IsFirstLine)
            //    {
            //        return run.PrevTextRun;
            //    }
            //    else
            //    {
            //        return run.PrevTextRun; 

            //        //EditableRun prevTextRun = run.PrevTextRun;

            //        //if (prevTextRun.IsLineBreak)
            //        //{
            //        //    return null;
            //        //}
            //        //else
            //        //{
            //        //    return prevTextRun;
            //        //}
            //    }
            //}
        }
        EditableRun GetNextTextRun(EditableRun run)
        {
            if (IsSingleLine)
            {
                return run.NextTextRun;
            }
            else
            {
                if (IsLastLine)
                {
                    return run.NextTextRun;
                }
                else
                {
                    return run.NextTextRun;

                    //EditableRun nextTextRun = run.NextTextRun;
                    //if (nextTextRun.IsLineBreak)
                    //{
                    //    return null;
                    //}
                    //else
                    //{
                    //    return nextTextRun;
                    //}
                }
            }
        }

        Size MeasureCopyRunLength(CopyRun copyRun)
        {
            var txServices = Root.TextServices;
            Size size;
            char[] mybuffer = copyRun.RawContent;
            if (txServices.SupportsWordBreak)
            {
                var textBufferSpan = new TextBufferSpan(mybuffer);
                int len = mybuffer.Length;
                var outputUserCharAdvances = new int[len];
                ILineSegmentList lineSegs = txServices.BreakToLineSegments(ref textBufferSpan);

                txServices.CalculateUserCharGlyphAdvancePos(ref textBufferSpan, lineSegs,
                    this.CurrentTextSpanStyle.ReqFont,
                    outputUserCharAdvances, out int outputTotalW, out int outputLineHeight);
                size = new Size(outputTotalW, outputLineHeight);
            }
            else
            {

                //_content_unparsed = false;
                int len = mybuffer.Length;
                var outputUserCharAdvances = new int[len];
                var textBufferSpan = new TextBufferSpan(mybuffer);
                txServices.CalculateUserCharGlyphAdvancePos(ref textBufferSpan,
                    this.CurrentTextSpanStyle.ReqFont,
                    outputUserCharAdvances, out int outputTotalW, out int outputLineHeight);
                size = new Size(outputTotalW, outputLineHeight);
            }
            return size;
        }
        internal EditableVisualPointInfo[] Split(VisualSelectionRange selectionRange)
        {
            selectionRange.SwapIfUnOrder();
            EditableVisualPointInfo startPoint = selectionRange.StartPoint;
            EditableVisualPointInfo endPoint = selectionRange.EndPoint;
            if (startPoint.TextRun == endPoint.TextRun)
            {
                EditableRun toBeCutTextRun = startPoint.TextRun;
                CopyRun preCutTextRun = toBeCutTextRun.LeftCopy(startPoint.RunLocalSelectedIndex);
                CopyRun middleCutTextRun = toBeCutTextRun.Copy(startPoint.RunLocalSelectedIndex, endPoint.LineCharIndex - startPoint.LineCharIndex);
                CopyRun postCutTextRun = toBeCutTextRun.Copy(endPoint.RunLocalSelectedIndex);
                EditableVisualPointInfo newStartRangePointInfo = null;
                EditableVisualPointInfo newEndRangePointInfo = null;
                EditableTextLine line = this;
                if (startPoint.LineId != _currentLineNumber)
                {
                    line = EditableFlowLayer.GetTextLine(startPoint.LineId);
                }
                line.LocalSuspendLineReArrange();
                if (preCutTextRun != null)
                {
                    line.AddBefore(toBeCutTextRun, preCutTextRun);
                    newStartRangePointInfo = CreateTextPointInfo(
                        startPoint.LineId, startPoint.LineCharIndex, startPoint.X,
                    /*preCutTextRun,*/
                    startPoint.TextRunCharOffset, startPoint.TextRunPixelOffset);
                }
                else
                {
                    EditableRun prevTxtRun = GetPrevTextRun(startPoint.TextRun);
                    if (prevTxtRun != null)
                    {
                        newStartRangePointInfo = CreateTextPointInfo(
                            startPoint.LineId, startPoint.LineCharIndex, startPoint.X, /*prevTxtRun,*/ startPoint.TextRunCharOffset - preCutTextRun.CharacterCount,
                            startPoint.TextRunPixelOffset - prevTxtRun.Width);
                    }
                    else
                    {
                        newStartRangePointInfo = CreateTextPointInfo(
                            startPoint.LineId,
                            startPoint.LineCharIndex,
                            0,
                            0, 0);
                    }
                }

                if (postCutTextRun != null)
                {
                    line.AddAfter(toBeCutTextRun, postCutTextRun);
                    newEndRangePointInfo =
                        CreateTextPointInfo(
                            endPoint.LineId,
                            endPoint.LineCharIndex,
                            endPoint.X,
                            //middleCutTextRun,
                            startPoint.TextRunCharOffset + middleCutTextRun.CharacterCount,
                            startPoint.TextRunPixelOffset + MeasureCopyRunLength(middleCutTextRun).Width);
                }
                else
                {
                    EditableRun nextTxtRun = GetNextTextRun(endPoint.TextRun);
                    if (nextTxtRun != null)
                    {
                        newEndRangePointInfo = CreateTextPointInfo(
                            endPoint.LineId,
                            endPoint.LineCharIndex,
                            endPoint.X,
                            endPoint.TextRunPixelOffset + endPoint.TextRun.CharacterCount,
                            endPoint.TextRunPixelOffset + endPoint.TextRun.Width);
                    }
                    else
                    {
                        newEndRangePointInfo = CreateTextPointInfo(
                            endPoint.LineId,
                            endPoint.LineCharIndex,
                            endPoint.X,
                            //middleCutTextRun,
                            endPoint.TextRunCharOffset,
                            endPoint.TextRunPixelOffset);
                    }
                }

                if (middleCutTextRun != null)
                {
                    line.AddAfter(toBeCutTextRun, middleCutTextRun);
                }
                else
                {
                    throw new NotSupportedException();
                }
                line.Remove(toBeCutTextRun);
                line.LocalResumeLineReArrange();
                return new EditableVisualPointInfo[] { newStartRangePointInfo, newEndRangePointInfo };
            }
            else
            {
                EditableTextLine workingLine = this;
                if (startPoint.LineId != _currentLineNumber)
                {
                    workingLine = EditableFlowLayer.GetTextLine(startPoint.LineId);
                }
                EditableVisualPointInfo newStartPoint = workingLine.Split(startPoint);
                workingLine = this;
                if (endPoint.LineId != _currentLineNumber)
                {
                    workingLine = EditableFlowLayer.GetTextLine(endPoint.LineId);
                }
                EditableVisualPointInfo newEndPoint = workingLine.Split(endPoint);
                return new EditableVisualPointInfo[] { newStartPoint, newEndPoint };
            }
        }

        internal EditableVisualPointInfo Split(EditableVisualPointInfo pointInfo)
        {
            if (pointInfo.LineId != _currentLineNumber)
            {
                throw new NotSupportedException();
            }

            EditableRun tobeCutRun = pointInfo.TextRun;
            if (tobeCutRun == null)
            {
                return CreateTextPointInfo(
                       pointInfo.LineId,
                       pointInfo.LineCharIndex,
                       pointInfo.X,
                       //null,
                       pointInfo.TextRunCharOffset,
                       pointInfo.TextRunPixelOffset);
            }

            this.LocalSuspendLineReArrange();
            EditableVisualPointInfo result = null;
            CopyRun preCutTextRun = tobeCutRun.LeftCopy(pointInfo.RunLocalSelectedIndex);
            CopyRun postCutTextRun = tobeCutRun.Copy(pointInfo.RunLocalSelectedIndex);
            if (preCutTextRun != null)
            {
                this.AddBefore(tobeCutRun, preCutTextRun);
                if (postCutTextRun != null)
                {
                    this.AddAfter(tobeCutRun, postCutTextRun);
                }

                result = CreateTextPointInfo(
                    pointInfo.LineId,
                    pointInfo.LineCharIndex,
                    pointInfo.X,
                    //preCutTextRun,
                    pointInfo.TextRunCharOffset,
                    pointInfo.TextRunPixelOffset);
            }
            else
            {
                if (postCutTextRun != null)
                {
                    this.AddAfter(tobeCutRun, postCutTextRun);
                }
                EditableRun infoTextRun = null;
                if (IsSingleLine)
                {
                    if (tobeCutRun.PrevTextRun != null)
                    {
                        infoTextRun = tobeCutRun.PrevTextRun;
                    }
                    else
                    {
                        infoTextRun = tobeCutRun.NextTextRun;
                    }
                }
                else
                {
                    if (IsFirstLine)
                    {
                        if (tobeCutRun.PrevTextRun != null)
                        {
                            infoTextRun = tobeCutRun.PrevTextRun;
                        }
                        else
                        {
                            if (tobeCutRun.NextTextRun == null)
                            {
                                infoTextRun = null;
                            }
                            else
                            {
                                infoTextRun = tobeCutRun.NextTextRun;
                            }
                        }
                    }
                    else if (IsLastLine)
                    {
                        if (tobeCutRun.PrevTextRun != null)
                        {
                            infoTextRun = tobeCutRun.PrevTextRun;
                            //if (tobeCutRun.PrevTextRun.IsLineBreak)
                            //{
                            //    if (tobeCutRun.NextTextRun != null)
                            //    {
                            //        infoTextRun = tobeCutRun.NextTextRun;
                            //    }
                            //    else
                            //    {
                            //        infoTextRun = null;
                            //    }
                            //}
                            //else
                            //{
                            //    infoTextRun = tobeCutRun.PrevTextRun;
                            //}
                        }
                        else
                        {

                        }
                    }
                    else
                    {
                        //if (!tobeCutRun.NextTextRun.IsLineBreak)
                        //{
                        //    infoTextRun = tobeCutRun.NextTextRun;
                        //}
                        //else
                        //{
                        //    infoTextRun = null;
                        //}
                        if (tobeCutRun.NextTextRun != null)
                        {
                            infoTextRun = tobeCutRun.NextTextRun;
                        }
                        else
                        {
                            infoTextRun = null;
                        }
                    }
                }
                result = CreateTextPointInfo(
                    pointInfo.LineId,
                    pointInfo.LineCharIndex,
                    pointInfo.X,
                    //infoTextRun,
                    pointInfo.TextRunCharOffset,
                    pointInfo.TextRunPixelOffset);
            }

            this.Remove(tobeCutRun);
            this.LocalResumeLineReArrange();
            return result;
        }
        void RightCopy(VisualPointInfo pointInfo, TextRangeCopy output)
        {
            if (pointInfo.LineId != _currentLineNumber)
            {
                throw new NotSupportedException();
            }
            EditableRun tobeCutRun = pointInfo.TextRun;
            if (tobeCutRun == null)
            {
                return;
            }
            CopyRun postCutTextRun = tobeCutRun.Copy(pointInfo.RunLocalSelectedIndex);
            if (postCutTextRun != null)
            {
                output.AddRun(postCutTextRun);
            }
            foreach (EditableRun t in GetVisualElementForward(tobeCutRun.NextTextRun, this.LastRun))
            {
                output.AddRun(t.Clone());
            }
        }

        void LeftCopy(VisualPointInfo pointInfo, TextRangeCopy output)
        {
            if (pointInfo.LineId != _currentLineNumber)
            {
                throw new NotSupportedException();
            }
            EditableRun tobeCutRun = pointInfo.TextRun;
            if (tobeCutRun == null)
            {
                return;
            }

            foreach (EditableRun t in _runs)
            {
                if (t != tobeCutRun)
                {
                    output.AddRun(t.Clone());
                }
                else
                {
                    break;
                }
            }
            CopyRun preCutTextRun = tobeCutRun.LeftCopy(pointInfo.RunLocalSelectedIndex);
            if (preCutTextRun != null)
            {
                output.AddRun(preCutTextRun);
            }
        }

        EditableVisualPointInfo CreateTextPointInfo(
            int lineId, int lineCharIndex, int caretPixelX,
            int textRunCharOffset, int textRunPixelOffset)
        {
            EditableVisualPointInfo textPointInfo = new EditableVisualPointInfo(this, lineCharIndex, null);
            textPointInfo.SetAdditionVisualInfo(textRunCharOffset, caretPixelX, textRunPixelOffset);
            return textPointInfo;
        }

        public VisualPointInfo GetTextPointInfoFromCaretPoint(int caretX)
        {
            int accTextRunWidth = 0;
            int accTextRunCharCount = 0;
            EditableRun lastestTextRun = null;
            foreach (EditableRun t in _runs)
            {
                lastestTextRun = t;
                int thisTextRunWidth = t.Width;
                if (accTextRunWidth + thisTextRunWidth > caretX)
                {
                    EditableRunCharLocation localPointInfo = t.GetCharacterFromPixelOffset(caretX - thisTextRunWidth);
                    var pointInfo = new EditableVisualPointInfo(this, accTextRunCharCount + localPointInfo.RunCharIndex, t);
                    pointInfo.SetAdditionVisualInfo(accTextRunCharCount, caretX, accTextRunWidth);
                    return pointInfo;
                }
                else
                {
                    accTextRunWidth += thisTextRunWidth;
                    accTextRunCharCount += t.CharacterCount;
                }
            }
            if (lastestTextRun != null)
            {
                return null;
            }
            else
            {

                EditableVisualPointInfo pInfo = new EditableVisualPointInfo(this, -1, null);
                pInfo.SetAdditionVisualInfo(accTextRunCharCount, caretX, accTextRunWidth);
                return pInfo;
            }
        }

        public EditableRun GetEditableRun(int charIndex)
        {
            int limit = CharCount - 1;
            if (charIndex > limit)
            {
                charIndex = limit;
            }

            int rCharOffset = 0;
            // int rPixelOffset = 0;
            EditableRun lastestRun = null;
            foreach (EditableRun r in _runs)
            {
                lastestRun = r;
                int thisCharCount = lastestRun.CharacterCount;
                if (thisCharCount + rCharOffset > charIndex)
                {
                    int localCharOffset = charIndex - rCharOffset;
                    //int pixelOffset = lastestRun.GetRunWidth(localCharOffset);
                    return lastestRun;

                    //textPointInfo.SetAdditionVisualInfo(/*lastestRun,*/
                    //    localCharOffset, rPixelOffset + pixelOffset,
                    //    rPixelOffset);
                    //return textPointInfo;
                }
                else
                {
                    rCharOffset += thisCharCount;
                    //rPixelOffset += r.Width;
                }
            }
            return lastestRun;
        }
        public EditableVisualPointInfo GetTextPointInfoFromCharIndex(int charIndex)
        {
            int limit = CharCount - 1;
            if (charIndex > limit)
            {
                charIndex = limit;
            }


            int rCharOffset = 0;
            int rPixelOffset = 0;
            EditableRun lastestRun = null;
            EditableVisualPointInfo textPointInfo = null;
            foreach (EditableRun r in _runs)
            {
                lastestRun = r;
                int thisCharCount = lastestRun.CharacterCount;
                if (thisCharCount + rCharOffset > charIndex)
                {
                    int localCharOffset = charIndex - rCharOffset;
                    int pixelOffset = lastestRun.GetRunWidth(localCharOffset);
                    textPointInfo = new EditableVisualPointInfo(this, charIndex, lastestRun);
                    textPointInfo.SetAdditionVisualInfo(localCharOffset, rPixelOffset + pixelOffset, rPixelOffset);
                    return textPointInfo;
                }
                else
                {
                    rCharOffset += thisCharCount;
                    rPixelOffset += r.Width;
                }
            }


            textPointInfo = new EditableVisualPointInfo(this, charIndex, lastestRun);
            textPointInfo.SetAdditionVisualInfo(rCharOffset - lastestRun.CharacterCount, rPixelOffset, rPixelOffset - lastestRun.Width);
            return textPointInfo;
        }

        internal EditableTextLine SplitToNewLine(EditableRun editableRun)
        {
            LinkedListNode<EditableRun> curNode = GetLineLinkedNode(editableRun);
            EditableTextLine newSplitedLine = EditableFlowLayer.InsertNewLine(_currentLineNumber + 1);
            newSplitedLine.LocalSuspendLineReArrange();
            while (curNode != null)
            {
                LinkedListNode<EditableRun> tobeRemovedNode = curNode;
                curNode = curNode.Next;
                if (tobeRemovedNode.List != null)
                {
                    EditableRun tmpv = tobeRemovedNode.Value;
                    _runs.Remove(tobeRemovedNode);
                    newSplitedLine.AddLast(tmpv);
                }
                else
                {
                }
            }
            newSplitedLine.LocalResumeLineReArrange();
            return newSplitedLine;
        }
    }
}