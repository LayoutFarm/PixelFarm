﻿//BSD, 2014-present, WinterDev

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PixelFarm.CpuBlit;


namespace Mini
{
    partial class FormDev : Form
    {

        public FormDev()
        {
            InitializeComponent();
            this.Load += new EventHandler(DevForm_Load);
            this.lstExamples.DoubleClick += new EventHandler(lstExample_DoubleClick);
            this.Text = "DevForm: Double Click The Example!";
            //render backend choices
            LoadRenderBackendChoices();
        }

        enum RenderBackendChoice
        {
            PureAgg,
            AggOnGLES,
            GdiPlus,
            GdiPlusOnGLES, //temporary
            OpenGLES,


            SkiaMemoryBackend,
            SkiaGLBackend,
        }
        void LoadRenderBackendChoices()
        {

            lstBackEndRenderer.Items.Clear();
            lstBackEndRenderer.Items.Add(RenderBackendChoice.PureAgg); //pure software renderer with MiniAgg
            lstBackEndRenderer.Items.Add(RenderBackendChoice.OpenGLES);
            lstBackEndRenderer.Items.Add(RenderBackendChoice.AggOnGLES);
            lstBackEndRenderer.Items.Add(RenderBackendChoice.GdiPlusOnGLES); //temporary ***
            //
            lstBackEndRenderer.Items.Add(RenderBackendChoice.GdiPlus);// legacy ***, for printing

            //lstBackEndRenderer.Items.Add(RenderBackendChoice.SkiaMemoryBackend);
            //lstBackEndRenderer.Items.Add(RenderBackendChoice.SkiaGLBackend);


            lstBackEndRenderer.SelectedIndex = 0;//set default 
            lstBackEndRenderer.DoubleClick += (s, e) => lstExample_DoubleClick(null, EventArgs.Empty);

            lstBackEndRenderer.SelectedIndexChanged += LstBackEndRenderer_SelectedIndexChanged;
        }

        private void LstBackEndRenderer_SelectedIndexChanged(object sender, EventArgs e)
        {
            //show only example the available on the selected rendering backend
            RenderBackendChoice selItem = (RenderBackendChoice)lstBackEndRenderer.SelectedItem;
            lstExamples.Items.Clear();

            List<ExampleAndDesc> selectedExampleList = null;
            switch (selItem)
            {
                default: return;

                case RenderBackendChoice.AggOnGLES:
                case RenderBackendChoice.PureAgg:
                    selectedExampleList = _aggExamples;
                    break;
                case RenderBackendChoice.GdiPlusOnGLES:
                case RenderBackendChoice.GdiPlus:
                    selectedExampleList = _gdiExamples;
                    break;
                case RenderBackendChoice.OpenGLES:
                    selectedExampleList = _glesExamples;
                    break;
            }


            int j = selectedExampleList.Count;
            for (int i = 0; i < j; ++i)
            {
                this.lstExamples.Items.Add(selectedExampleList[i]);
            }
        }


        static DemoBase InitDemo(ExampleAndDesc exampleAndDesc)
        {
            if (!(Activator.CreateInstance(exampleAndDesc.Type) is DemoBase exBase))
            {
                return null;
            }
            exBase.Init();
            return exBase;
        }
        void lstExample_DoubleClick(object sender, EventArgs e)
        {
            //load sample form
            if (!(this.lstExamples.SelectedItem is ExampleAndDesc exAndDesc))
            {
                return; //early exit
            }
            //
            DemoBase demo = InitDemo(exAndDesc);
            if (demo == null) { return; }


            FormTestBed formTestBed = new FormTestBed();
            formTestBed.WindowState = FormWindowState.Maximized;

            RenderBackendChoice selItem = (RenderBackendChoice)lstBackEndRenderer.SelectedItem;
            CpuBlitAppModule cpuBlitContextWinForm = null;
            switch (selItem)
            {

                case RenderBackendChoice.PureAgg:
                    {
                        LayoutFarm.UI.FormCanvasHelper.CreateCanvasControlOnExistingControl(
                            formTestBed.GetLandingControl(),
                            0, 0, 800, 600,
                            LayoutFarm.UI.InnerViewportKind.PureAgg,
                            out LayoutFarm.UI.GraphicsViewRoot surfaceViewport
                            );
                        formTestBed.SetUISurfaceViewportControl(surfaceViewport);
                        formTestBed.Show();

                        cpuBlitContextWinForm = new CpuBlitAppModule();
                        cpuBlitContextWinForm.BindSurface(surfaceViewport);
                        cpuBlitContextWinForm.LoadExample(demo);

                        demo.RequestGraphicRefresh += (s, e1) => cpuBlitContextWinForm.InvalidateUI();

                        formTestBed.FormClosed += (s1, e1) => cpuBlitContextWinForm.Close();
                        formTestBed.LoadExample(exAndDesc, demo);

                    }
                    break;
                case RenderBackendChoice.GdiPlus:
                    {

                        LayoutFarm.UI.FormCanvasHelper.CreateCanvasControlOnExistingControl(
                             formTestBed.GetLandingControl(),
                             0, 0, 800, 600,
                             LayoutFarm.UI.InnerViewportKind.GdiPlus,
                             out LayoutFarm.UI.GraphicsViewRoot surfaceViewport
                             );
                        formTestBed.SetUISurfaceViewportControl(surfaceViewport);

                        cpuBlitContextWinForm = new CpuBlitAppModule();
                        cpuBlitContextWinForm.BindSurface(surfaceViewport);
                        cpuBlitContextWinForm.LoadExample(demo);
                        formTestBed.FormClosed += (s1, e1) => cpuBlitContextWinForm.Close();



                        formTestBed.LoadExample(exAndDesc, demo);
                        formTestBed.Show();
                    }
                    break;
                case RenderBackendChoice.OpenGLES: //gles 2 and 3
                    {
                        //--------------------------------------------
                        LayoutFarm.UI.FormCanvasHelper.CreateCanvasControlOnExistingControl(
                          formTestBed.GetLandingControl(),
                          0, 0, 800, 600,
                          LayoutFarm.UI.InnerViewportKind.GLES,
                          out LayoutFarm.UI.GraphicsViewRoot surfaceViewport
                          );

                        formTestBed.SetUISurfaceViewportControl(surfaceViewport);
                        GLESAppModule appModule = new GLESAppModule();
                        appModule.BindSurface(surfaceViewport);
                        appModule.LoadExample(demo);
                        formTestBed.FormClosing += (s2, e2) => appModule.Close();

                        formTestBed.LoadExample(exAndDesc, demo);
                        formTestBed.Show();
                    }
                    break;
                case RenderBackendChoice.AggOnGLES:
                case RenderBackendChoice.GdiPlusOnGLES:
                    {
                        LayoutFarm.UI.FormCanvasHelper.CreateCanvasControlOnExistingControl(
                            formTestBed.GetLandingControl(),
                            0, 0, 800, 600,
                            LayoutFarm.UI.InnerViewportKind.AggOnGLES,
                            out LayoutFarm.UI.GraphicsViewRoot surfaceViewport
                            );
                        formTestBed.SetUISurfaceViewportControl(surfaceViewport);
                        //

                        CpuBlitOnGLESAppModule appModule = new CpuBlitOnGLESAppModule();
                        appModule.WithGdiPlusDrawBoard = (selItem == RenderBackendChoice.GdiPlusOnGLES);//**
                        appModule.BindSurface(surfaceViewport);
                        appModule.LoadExample(demo);

                        formTestBed.FormClosing += (s2, e2) => appModule.Close();
                        formTestBed.LoadExample(exAndDesc, demo);
                        formTestBed.Show();
                    }
                    break;
#if SKIA_ENABLE
                case RenderBackendChoice.SkiaMemoryBackend:
                    {
                        TestSkia1.FormSkia1 formSkia = new TestSkia1.FormSkia1();
                        formSkia.SelectBackend(TestSkia1.FormSkia1.SkiaBackend.Memory);
                        formSkia.Show();
                        formSkia.LoadExample(exAndDesc);
                    }
                    break;
                case RenderBackendChoice.SkiaGLBackend:
                    {
                        TestSkia1.FormSkia1 formSkia = new TestSkia1.FormSkia1();
                        formSkia.SelectBackend(TestSkia1.FormSkia1.SkiaBackend.GLES);
                        formSkia.Show();
                        formSkia.LoadExample(exAndDesc);
                    }
                    break;
#endif
                default:
                    throw new NotSupportedException();
            }
        }
        static void LoadSamplesFromAssembly(Type srcType, List<ExampleAndDesc> outputList)
        {
            //load examples
            Type[] allTypes = srcType.Assembly.GetTypes();
            Type exBase = typeof(Mini.DemoBase);
            int j = allTypes.Length;
            for (int i = 0; i < j; ++i)
            {
                Type t = allTypes[i];
                if (exBase.IsAssignableFrom(t) && t != exBase && !t.IsAbstract)
                {
                    outputList.Add(new ExampleAndDesc(t, t.Name));
                }
            }
        }


        List<ExampleAndDesc> _glesExamples = new List<ExampleAndDesc>();
        List<ExampleAndDesc> _aggExamples = new List<ExampleAndDesc>();
        List<ExampleAndDesc> _gdiExamples = new List<ExampleAndDesc>();
        List<ExampleAndDesc> _bothHardwareAndSoftwareExamples = new List<ExampleAndDesc>(); //? all?

        void DevForm_Load(object sender, EventArgs e)
        {

            List<ExampleAndDesc> exlist = new List<ExampleAndDesc>();
            LoadSamplesFromAssembly(this.GetType(), exlist);
            LoadSamplesFromAssembly(typeof(GLDemoContext), exlist);

            //-------
            foreach (ExampleAndDesc ex in exlist)
            {
                bool supporedOnSomePlatform = false;
                if (ex.IsAvailableOn(AvailableOn.GLES))
                {
                    _glesExamples.Add(ex);
                    supporedOnSomePlatform = true;
                }
                if (ex.IsAvailableOn(AvailableOn.Agg))
                {
                    _aggExamples.Add(ex);
                    supporedOnSomePlatform = true;
                }
                if (ex.IsAvailableOn(AvailableOn.GdiPlus))
                {
                    _gdiExamples.Add(ex);
                    supporedOnSomePlatform = true;
                }


                if (ex.IsAvailableOn(AvailableOn.BothHardwareAndSoftware))
                {
                    _bothHardwareAndSoftwareExamples.Add(ex);
                    supporedOnSomePlatform = true;
                }

                if (!supporedOnSomePlatform)
                {
                    //add to default  , TODO: review here
                    _aggExamples.Add(ex);
                }
            }

            exlist.Sort((ex1, ex2) => ex1.OrderCode.CompareTo(ex2.OrderCode));

            _glesExamples.Sort((ex1, ex2) => ex1.OrderCode.CompareTo(ex2.OrderCode));
            _aggExamples.Sort((ex1, ex2) => ex1.OrderCode.CompareTo(ex2.OrderCode));
            _gdiExamples.Sort((ex1, ex2) => ex1.OrderCode.CompareTo(ex2.OrderCode));

            lstBackEndRenderer.SelectedIndex = 1;
        }


        private void button2_Click(object sender, EventArgs e)
        {
            using (Bitmap bmp = new Bitmap("test002.png"))
            {
                //MatterHackers.StackBlur2.FastBlur32RGBA(bmp, 15);

                var rct = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
                //assign dimension info and copy buffer 
                var bitmapData = bmp.LockBits(rct, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                int bmpStride = bitmapData.Stride;
                int width = bmp.Width;
                int height = bmp.Height;
                int wh = width * height;
                //var dest = new int[wh];
                //var source = new int[wh];

                var source = new int[width * height];
                var dest = new int[width * height];
                Marshal.Copy(bitmapData.Scan0, source, 0, source.Length);
                PixelFarm.CpuBlit.Imaging.StackBlurARGB.FastBlur32ARGB(source, dest, width, height, 15);
                Marshal.Copy(dest, 0, bitmapData.Scan0, dest.Length);
                bmp.UnlockBits(bitmapData);
                bmp.Save("test002_2.png");
            }
        }

        static void SaveImage(MemBitmap bmp, string filename)
        {
            Bitmap newBmp = new Bitmap(bmp.Width, bmp.Height);
            PixelFarm.CpuBlit.BitmapHelper.CopyToGdiPlusBitmapSameSize(bmp, newBmp);
            newBmp.Save(filename);
        }
        static MemBitmap LoadImage(string filename)
        {
            //read sample image
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(filename))
            {
                //read to image buffer 
                int bmpW = bmp.Width;
                int bmpH = bmp.Height;
                MemBitmap img = new MemBitmap(bmpW, bmpH);
                PixelFarm.CpuBlit.BitmapHelper.CopyFromGdiPlusBitmapSameSizeTo32BitsBuffer(bmp, img);
                return img;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TestPaintFx.FormTestPaintFx test = new TestPaintFx.FormTestPaintFx();
            test.Show();
        }


        private void button3_Click(object sender, EventArgs e)
        {

        }
        private void button4_Click(object sender, EventArgs e)
        {
            //for test msdf gen
            FormTestMsdfGen formTestMsdfGen = new FormTestMsdfGen();
            formTestMsdfGen.Show();
        }


        private void FormDev_Load(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            FormImgResampling formImgResampling = new FormImgResampling();
            formImgResampling.Show();
        }

        private void cmdRBTreeTest_Click(object sender, EventArgs e)
        {

        }
    }
}


