﻿//Apache2, 2014-present, WinterDev

namespace LayoutFarm
{
    [DemoNote("1.12 MultipleImages")]
    class Demo_SmallAnimation : App
    {
        protected override void OnStart(AppHost host)
        {
            ImageBinder imgBinder = host.LoadImageAndBind("../Data/imgs/favorites32.png");

            //GlobalRootGraphic.BlockGraphicsUpdate();
            for (int i = 0; i < 100; ++i)
            {
                //share 1 img binder with multiple img boxes
                var imgBox = new CustomWidgets.ImageBox(
                    imgBinder.Width,
                    imgBinder.Height);

                imgBox.ImageBinder = imgBinder;
                imgBox.SetLocation(i * 32, 20);
                imgBox.MouseDown += (s, e) =>
                {
                    //test start animation  
                    int nsteps = 40;
                    UIPlatform.RegisterTimerTask(20, timTask =>
                    {
                        imgBox.SetLocation(imgBox.Left, imgBox.Top + 10);
                        nsteps--;
                        if (nsteps <= 0)
                        {
                            timTask.RemoveSelf();
                        }
                    });
                };
                host.AddChild(imgBox);
            }
            //GlobalRootGraphic.ReleaseGraphicsUpdate();
        }

    }
}