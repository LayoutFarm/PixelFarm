﻿using System;
using System.Diagnostics;

using Foundation;
using GLKit;
using OpenGLES;
using OpenTK.Graphics.ES20;
using CustomApp01;
using CoreGraphics;
using UIKit;
using System.IO;

namespace TestApp01.iOS
{
    [Register("GameViewController")]
    public class GameViewController : GLKViewController, IGLKViewDelegate
    {

        EAGLContext context { get; set; }
        //[Export("initWithCoder:")]
        public GameViewController()
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Code to start the Xamarin Test Cloud Agent
#if ENABLE_TEST_CLOUD
            //Xamarin.Calabash.Start();
#endif

            context = new EAGLContext(EAGLRenderingAPI.OpenGLES3);

            if (context == null)
            {
                Debug.WriteLine("Failed to create ES context");
            }


            Typography.FontManagement.InstalledTypefaceCollectionExtensions.CustomSystemFontListLoader = LoadFonts;
            var view = (GLKView)View;
            view.Context = context;
            view.DrawableDepthFormat = GLKViewDrawableDepthFormat.Format24;
            _view_width = (int)view.Frame.Width;
            _view_height = (int)view.Frame.Height;
            SetupGL();
        }


        
        void LoadFonts(Typography.FontManagement.InstalledTypefaceCollection fontCollection)
        {
            LoadBundleFont(fontCollection, "DroidSans.ttf");
            LoadBundleFont(fontCollection, "tahoma.ttf");
        }
        static void LoadBundleFont(Typography.FontManagement.InstalledTypefaceCollection fontCollection, string fontFilename)
        {
            
            if (File.Exists(fontFilename))
            {
                using (Stream s = new FileStream(fontFilename, FileMode.Open, FileAccess.Read))
                using (var ms = new MemoryStream())// This is a simple hack because on Xamarin.Android, a `Stream` created by `AssetManager.Open` is not seekable.
                {
                    s.CopyTo(ms);
                    fontCollection.AddFontStreamSource(new BundleResourceFontStreamSource(new MemoryStream(ms.ToArray()), fontFilename));
                }
            }
        }
        class BundleResourceFontStreamSource : Typography.FontManagement.IFontStreamSource
        {
            MemoryStream _ms;
            string _pathName;
            public BundleResourceFontStreamSource(MemoryStream ms, string pathName)
            {
                _ms = ms;
                _pathName = pathName;
            }
            public string PathName => _pathName;
            public Stream ReadFontStream()
            {
                return _ms;
            }
        }

        //---------------------------------
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            TearDownGL();

            if (EAGLContext.CurrentContext == context)
                EAGLContext.SetCurrentContext(null);
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();

            if (IsViewLoaded && View.Window == null)
            {
                View = null;

                TearDownGL();

                if (EAGLContext.CurrentContext == context)
                {
                    EAGLContext.SetCurrentContext(null);
                }
            }

            // Dispose of any resources that can be recreated.
        }

        public override bool PrefersStatusBarHidden()
        {
            return true;
        }


        CustomApp _customApp;
        int _max;
        int _view_width;
        int _view_height;
        void SetupGL()
        {

            EAGLContext.SetCurrentContext(context);
            _customApp = new CustomApp();
            _max = Math.Max(_view_width * 2, _view_height * 2);
            _customApp.Setup(_view_width * 2, _view_height * 2);
        }
        public override void Update()
        {
            GL.Viewport(0, 0, _max, _max);
            _customApp.RenderFrame();

        }
        //----------------
        void TearDownGL()
        {

        }

    }
}