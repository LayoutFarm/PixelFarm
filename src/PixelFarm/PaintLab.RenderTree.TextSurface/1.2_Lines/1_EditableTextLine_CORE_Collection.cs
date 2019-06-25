﻿//Apache2, 2014-present, WinterDev

using System;
using System.Collections.Generic;
namespace LayoutFarm.TextEditing
{
    partial class EditableTextLine
    {
        void AddNormalRunToLast(EditableRun v)
        {
            v.SetInternalLinkedNode(_runs.AddLast(v), this);
        }
        void AddNormalRunToFirst(EditableRun v)
        {
            v.SetInternalLinkedNode(_runs.AddFirst(v), this);
        }

        static LinkedListNode<EditableRun> GetLineLinkedNode(EditableRun ve)
        {
            return ve.LinkedNodeForEditableRun;
        }
        void AddNormalRunBefore(EditableRun beforeVisualElement, EditableRun v)
        {
            v.SetInternalLinkedNode(_runs.AddBefore(GetLineLinkedNode(beforeVisualElement), v), this);
        }
        void AddNormalRunAfter(EditableRun afterVisualElement, EditableRun v)
        {
            v.SetInternalLinkedNode(_runs.AddAfter(GetLineLinkedNode(afterVisualElement), v), this);
        }
        public void Clear()
        {
            LinkedListNode<EditableRun> curNode = this.First;
            while (curNode != null)
            {
                EditableRun.RemoveParentLink(curNode.Value);
                curNode = curNode.Next;
            }

            _runs.Clear();
        }

        public void Remove(EditableRun v)
        {
//#if DEBUG
//            if (v.IsLineBreak)
//            {
//                throw new NotSupportedException("not support line break");
//            }
//#endif


            _runs.Remove(GetLineLinkedNode(v));
            EditableRun.RemoveParentLink(v);
            if ((_lineFlags & LOCAL_SUSPEND_LINE_REARRANGE) != 0)
            {
                return;
            }

            if (!this.EndWithLineBreak && this.RunCount == 0 && _currentLineNumber > 0)
            {
                if (!EditableFlowLayer.GetTextLine(_currentLineNumber - 1).EndWithLineBreak)
                {
                    EditableFlowLayer.Remove(_currentLineNumber);
                }
            }
            else
            {
                //var ownerVe = editableFlowLayer.OwnerRenderElement;
                //if (ownerVe != null)
                //{
                //    RenderElement.InnerInvalidateLayoutAndStartBubbleUp(ownerVe);
                //}
                //else
                //{
                //    throw new NotSupportedException();
                //}
            }
        }
    }
}