﻿//Apache2, 2014-present, WinterDev

using System.Collections.Generic;
namespace LayoutFarm.UI
{
    static class UISystem
    {
        static Queue<UIElement> s_layoutQueue = new Queue<UIElement>();
        static UISystem()
        {
            LayoutFarm.EventQueueSystem.CentralEventQueue.RegisterEventQueue(ClearLayoutQueue);
        }
        internal static void AddToLayoutQueue(UIElement ui)
        {
            if (ui.IsInLayoutQueue) return;
            s_layoutQueue.Enqueue(ui);
            ui.IsInLayoutQueue = true;
        }
        public static void ClearLayoutQueue()
        {
            int count = s_layoutQueue.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                UIElement ui = s_layoutQueue.Dequeue();
                ui.IsInLayoutQueue = false;
                UIElement.InvokeContentLayout(ui);

#if DEBUG
                if (ui.IsInLayoutQueue)
                {
                    //should not occur
                    throw new System.NotSupportedException();
                }
#endif
            }
        }
    }

    public abstract partial class UIElement : IUIEventListener
    {

#if DEBUG
        public bool dbugBreakMe;
#endif
        bool _hide;

        //bounds
        float _left;
        float _top;
        float _right;
        float _bottom;
        //object _tag;
        UIElement _parent;
        internal LinkedListNode<UIElement> _collectionLinkNode;

        public UIElement()
        {
        }
        public abstract RenderElement GetPrimaryRenderElement(RootGraphic rootgfx);
        public abstract RenderElement CurrentPrimaryRenderElement { get; }
        protected abstract bool HasReadyRenderElement { get; }
        public abstract void InvalidateGraphics();



        public virtual object Tag
        {
            get => null;
            set
            {
                throw new System.NotSupportedException("user must override this");
            }
        }

        public virtual void Focus()
        {
            //make this keyboard focusable
            if (this.HasReadyRenderElement)
            {
                //focus
                this.CurrentPrimaryRenderElement.Root.SetCurrentKeyboardFocus(this.CurrentPrimaryRenderElement);
            }
        }
        public virtual void Blur()
        {
            if (this.HasReadyRenderElement)
            {
                //focus
                this.CurrentPrimaryRenderElement.Root.SetCurrentKeyboardFocus(null);
            }
        }
        public UIElement ParentUI
        {
            get => _parent;
            internal set => _parent = value;
        }

        public UIElement NextUIElement
        {
            get
            {
                if (_collectionLinkNode != null)
                {
                    return _collectionLinkNode.Next.Value;
                }
                return null;
            }
        }
        public UIElement PrevUIElement
        {
            get
            {
                if (_collectionLinkNode != null)
                {
                    return _collectionLinkNode.Previous.Value;
                }
                return null;
            }
        }
        //------------------------------
        public virtual void RemoveChild(UIElement ui)
        {
#if DEBUG
            throw new System.NotSupportedException("user must impl this");
#endif
        }
        public virtual void ClearChildren()
        {
#if DEBUG
            throw new System.NotSupportedException("user must impl this");
#endif
        }
        public virtual void RemoveSelf()
        {
            if (_parent != null)
            {
                _parent.RemoveChild(this);
            }
            this.InvalidateOuterGraphics();
#if DEBUG
            if (_collectionLinkNode != null || _parent != null)
            {
                throw new System.Exception("");
            }
#endif
        }

        public virtual void AddFirst(UIElement ui) { }
        public virtual void AddAfter(UIElement afterUI, UIElement ui) { }
        public virtual void AddBefore(UIElement beforeUI, UIElement ui) { }
        public virtual void AddChild(UIElement ui) { }
        public virtual void BringToTopMost()
        {
            if (_parent != null)
            {
                //after RemoveSelf_parent is set to null
                //so we backup it before RemoveSelf
                UIElement parentUI = _parent;
                parentUI.RemoveChild(this);
                parentUI.AddChild(this);
                this.InvalidateGraphics();
            }
        }
        public virtual void BringToTopOneStep()
        {
            if (_parent != null)
            {
                //find next element
                UIElement next = this.NextUIElement;
                if (next != null)
                {
                    UIElement parentUI = _parent;
                    parentUI.RemoveChild(this);
                    parentUI.AddAfter(next, this);
                    this.InvalidateGraphics();
                }
            }
        }
        public virtual void SendToBackMost()
        {
            if (_parent != null)
            {
                //after RemoveSelf_parent is set to null
                //so we backup it before RemoveSelf

                UIElement parentUI = _parent;
                parentUI.RemoveChild(this);
                parentUI.AddFirst(this);
                this.InvalidateGraphics();
            }
        }
        public virtual void SendOneStepToBack()
        {
            if (_parent != null)
            {
                //find next element
                UIElement prev = this.PrevUIElement;
                if (prev != null)
                {
                    UIElement parentUI = _parent;
                    parentUI.RemoveChild(this);
                    parentUI.AddBefore(prev, this);
                }
            }
        }

        //------------------------------
        public virtual void InvalidateOuterGraphics()
        {

        }
        public virtual bool Visible
        {
            get => !_hide;
            set
            {
                _hide = !value;
                if (this.HasReadyRenderElement)
                {
                    this.CurrentPrimaryRenderElement.SetVisible(value);
                }
            }
        }
        public PixelFarm.Drawing.Point GetGlobalLocation()
        {
            if (this.CurrentPrimaryRenderElement != null)
            {
                return this.CurrentPrimaryRenderElement.GetGlobalLocation();
            }
            return new PixelFarm.Drawing.Point((int)_left, (int)_top);
        }
        public virtual void GetViewport(out int left, out int top)
        {
            left = top = 0;
        }
        public void GetElementBounds(
           out float left,
           out float top,
           out float right,
           out float bottom)
        {
            left = _left;
            top = _top;
            right = _right;
            bottom = _bottom;
        }
        protected void SetElementBoundsWH(float width, float height)
        {

            _right = _left + width;
            _bottom = _top + height;
        }
        protected void SetElementBoundsLTWH(float left, float top, float width, float height)
        {

            //change 'TransparentBounds' => not effect visual presentation
            _left = left;
            _top = top;
            _right = left + width;
            _bottom = top + height;
        }
        protected void SetElementBounds(float left, float top, float right, float bottom)
        {
            //change 'TransparentBounds' => not effect visual presentation
            _left = left;
            _top = top;
            _right = right;
            _bottom = bottom;
        }
        protected void SetElementBoundsLT(float left, float top)
        {
            _bottom = top + (_bottom - _top);
            _right = left + (_right - _left);
            _left = left;
            _top = top;
        }
        //-------------------------------------------------------
        protected float BoundWidth => _right - _left;
        protected float BoundHeight => _bottom - _top;
        protected float BoundTop => _top;
        protected float BoundLeft => _left;
        //-------------------------------------------------------
        //layout ...
        public virtual bool NeedContentLayout => false;
        internal bool IsInLayoutQueue { get; set; }
        //-------------------------------------------------------
        //events ...
        bool _transparentAllMouseEvents; //TODO: review here
        public bool TransparentAllMouseEvents
        {
            get => _transparentAllMouseEvents;
            set
            {
                _transparentAllMouseEvents = value;
                if (this.HasReadyRenderElement)
                {
                    this.CurrentPrimaryRenderElement.TransparentForAllEvents = value;
                }
            }
        }
        //
        public bool AutoStopMouseEventPropagation { get; set; }
        //
        protected virtual void OnShown()
        {
        }
        protected virtual void OnHide()
        {
        }
        protected virtual void OnLostKeyboardFocus(UIFocusEventArgs e)
        {
        }
        protected virtual void OnLostMouseFocus(UIMouseEventArgs e)
        {
        }
        protected virtual void OnGotKeyboardFocus(UIFocusEventArgs e)
        {
        }
        protected virtual void OnDoubleClick(UIMouseEventArgs e)
        {
        }
        //-------------------------------------------------------
        protected virtual void OnMouseDown(UIMouseEventArgs e)
        {
        }
        protected virtual void OnMouseMove(UIMouseEventArgs e)
        {
        }
        protected virtual void OnMouseUp(UIMouseEventArgs e)
        {
        }
        protected virtual void OnMouseEnter(UIMouseEventArgs e)
        {
        }
        protected virtual void OnMouseLeave(UIMouseEventArgs e)
        {
        }
        protected virtual void OnMouseWheel(UIMouseEventArgs e)
        {
        }
        protected virtual void OnMouseHover(UIMouseEventArgs e)
        {
        }

        //------------------------------------------------------------
        protected virtual void OnKeyDown(UIKeyEventArgs e)
        {
        }
        protected virtual void OnKeyUp(UIKeyEventArgs e)
        {
        }
        protected virtual void OnKeyPress(UIKeyEventArgs e)
        {
        }
        protected virtual bool OnProcessDialogKey(UIKeyEventArgs e)
        {
            return false;
        }
        //------------------------------------------------------------
        public void InvalidateLayout()
        {
            //add to layout queue
            UISystem.AddToLayoutQueue(this);
        }
        public virtual void NotifyContentUpdate(UIElement childContent)
        {
            //
        }
        internal static void InvokeContentLayout(UIElement ui)
        {
            ui.OnContentLayout();
        }

        protected virtual void OnContentLayout()
        {
        }
        protected virtual void OnContentUpdate()
        {
        }
        protected virtual void OnInterComponentMsg(object sender, int msgcode, string msg)
        {
        }
        protected virtual void OnElementChanged()
        {
        }
        //
        public abstract void Walk(UIVisitor visitor);
        protected virtual void OnGuestTalk(UIGuestTalkEventArgs e)
        {
        }

#if DEBUG
        object dbugTagObject;
        public object dbugTag
        {
            get
            {
                return this.dbugTagObject;
            }
            set
            {
                this.dbugTagObject = value;
            }
        }
#endif
    }
}