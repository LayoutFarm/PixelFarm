﻿//Apache2, 2020-present, WinterDev

using System.Collections.Generic;
using PixelFarm.Drawing;
namespace LayoutFarm
{
    struct SimplePool<T>
    {
        public delegate T CreateNewDel();
        public delegate void CleanupDel(T d);

        CreateNewDel _createDel;
        CleanupDel _cleanupDel;
        Stack<T> _pool;
        public SimplePool(CreateNewDel createDel, CleanupDel cleanup)
        {
            _pool = new Stack<T>();
            _createDel = createDel;
            _cleanupDel = cleanup;
        }
        public T Borrow()
        {
            return _pool.Count > 0 ? _pool.Pop() : _createDel();
        }
        public void ReleaseBack(T t)
        {
            _cleanupDel?.Invoke(t);
            _pool.Push(t);
        }
        public void Close()
        {

        }
    }


    public class GfxUpdatePlan
    {
        RootGraphic _rootgfx;
        readonly List<RenderElement> _bubbleGfxTracks = new List<RenderElement>();
        public GfxUpdatePlan(RootGraphic rootgfx)
        {
            _rootgfx = rootgfx;
        }

        static RenderElement FindFirstClipedOrOpaqueParent(RenderElement r)
        {
#if DEBUG
            RenderElement dbugBackup = r;
#endif
            r = r.ParentRenderElement;
            while (r != null)
            {
                if (r.NoClipOrBgIsNotOpaque)
                {
                    r = r.ParentRenderElement;
                }
                else
                {
                    //found 1st opaque bg parent
                    return r;
                }
            }
            return null; //not found
        }
        static void BubbleUpGraphicsUpdateTrack(RenderElement r, List<RenderElement> trackedElems)
        {
            while (r != null)
            {
                if (r.IsBubbleGfxUpdateTracked)
                {
                    return;//stop here
                }
                RenderElement.TrackBubbleUpdateLocalStatus(r);
                trackedElems.Add(r);
                r = r.ParentRenderElement;
            }
        }

        public Rectangle AccumUpdateArea { get; private set; }


        /// <summary>
        /// update rect region
        /// </summary>
        class GfxUpdateRectRgn
        {
            readonly List<InvalidateGfxArgs> _invList = new List<InvalidateGfxArgs>();
#if DEBUG
            public GfxUpdateRectRgn() { }
#endif
            public void AddDetail(InvalidateGfxArgs a)
            {
                _invList.Add(a);
            }
            public void Reset(RootGraphic rootgfx)
            {
                List<InvalidateGfxArgs> invList = _invList;
                for (int i = invList.Count - 1; i >= 0; --i)
                {
                    //release back 
                    rootgfx.ReleaseInvalidateGfxArgs(invList[i]);
                }
                invList.Clear();
            }

            public InvalidateGfxArgs GetDetail(int index) => _invList[index];
            public int DetailCount => _invList.Count;
        }

        readonly List<GfxUpdateRectRgn> _gfxUpdateJobList = new List<GfxUpdateRectRgn>();
        readonly SimplePool<GfxUpdateRectRgn> _gfxUpdateJobPool = new SimplePool<GfxUpdateRectRgn>(() => new GfxUpdateRectRgn(), null);

        GfxUpdateRectRgn _currentJob = null;

        public void SetCurrentJob(int jobIndex)
        {
            //reset

            AccumUpdateArea = Rectangle.Empty;
            _bubbleGfxTracks.Clear();
            _currentJob = _gfxUpdateJobList[jobIndex];

            if (_currentJob.DetailCount == 1)
            {
                InvalidateGfxArgs args = _currentJob.GetDetail(0);
                RenderElement.MarkAsGfxUpdateTip(args.StartOn);
                BubbleUpGraphicsUpdateTrack(args.StartOn, _bubbleGfxTracks);
                AccumUpdateArea = args.GlobalRect;
            }
            else
            {

            }

            RenderElement.WaitForStartRenderElement = true;
        }


        public void ClearCurrentJob()
        {
            if (_currentJob != null)
            {
                _currentJob.Reset(_rootgfx);                 
                _gfxUpdateJobPool.ReleaseBack(_currentJob);
                _currentJob = null;
            }
            for (int i = _bubbleGfxTracks.Count - 1; i >= 0; --i)
            {
                RenderElement.ResetBubbleUpdateLocalStatus(_bubbleGfxTracks[i]);
            }
            _bubbleGfxTracks.Clear();
            RenderElement.WaitForStartRenderElement = false;
        }
        public int JobCount => _gfxUpdateJobList.Count;

        void AddNewJob(InvalidateGfxArgs a)
        {
            GfxUpdateRectRgn updateJob = _gfxUpdateJobPool.Borrow();
            updateJob.AddDetail(a);
            _gfxUpdateJobList.Add(updateJob); 
        } 

        public void SetUpdatePlanForFlushAccum()
        {
            //create accumulative plan                
            //merge consecutive
            RenderElement.WaitForStartRenderElement = false;
            List<InvalidateGfxArgs> accumQueue = RootGraphic.GetAccumInvalidateGfxArgsQueue(_rootgfx);
            int j = accumQueue.Count;
            if (j == 0)
            {
                return;
            }
            else if (j > 10) //???
            {
                //default (original) mode                 
                System.Diagnostics.Debug.WriteLine("traditional: " + j);

                for (int i = 0; i < j; ++i)
                {
                    InvalidateGfxArgs a = accumQueue[i];
                    _rootgfx.ReleaseInvalidateGfxArgs(a);
                }
            }
            else
            {
#if DEBUG
                if (j == 2)
                {
                }
                System.Diagnostics.Debug.WriteLine("flush accum:" + j);
                //--------------
                //>>preview for debug
                if (RenderElement.dbugUpdateTrackingCount > 0)
                {
                    throw new System.NotSupportedException();
                }

                for (int i = 0; i < j; ++i)
                {
                    InvalidateGfxArgs a = accumQueue[i];
                    RenderElement srcE = a.SrcRenderElement;
                    if (srcE.NoClipOrBgIsNotOpaque)
                    {
                        srcE = FindFirstClipedOrOpaqueParent(srcE);
                        if (srcE == null)
                        {
                            throw new System.NotSupportedException();
                        }
                    }
                    if (srcE.IsBubbleGfxUpdateTrackedTip)
                    {
                    }
                }
                //<<preview for debug
                //--------------
#endif

                for (int i = 0; i < j; ++i)
                {
                    InvalidateGfxArgs a = accumQueue[i];
                    RenderElement srcE = a.SrcRenderElement;
                    if (srcE.NoClipOrBgIsNotOpaque)
                    {
                        srcE = FindFirstClipedOrOpaqueParent(srcE);
                    }
                    a.StartOn = srcE;
                    AddNewJob(a);
                }
            }

            accumQueue.Clear();
        }

        public void ResetUpdatePlan()
        {
            _currentJob = null;
            _gfxUpdateJobList.Clear();
            RenderElement.WaitForStartRenderElement = false;
        }
    }



}