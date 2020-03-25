﻿//Apache2, 2014-present, WinterDev

using PixelFarm.Drawing;
namespace LayoutFarm
{
    [DemoNote("1.8 Hinge")]
    class Demo_Hinge : App
    {
        ImageBinder _arrowBmp;
        AppHost _appHost;

        protected override void OnStart(AppHost host)
        {
            _appHost = host;
            var comboBox1 = CreateComboBox(20, 20);
            host.AddChild(comboBox1);
            var comboBox2 = CreateComboBox(50, 50);
            host.AddChild(comboBox2);
            //------------
            var menuItem = CreateMenuItem(50, 100);
            var menuItem2 = CreateMenuItem(5, 5);
            menuItem.AddSubMenuItem(menuItem2);
            host.AddChild(menuItem);
        }

        LayoutFarm.CustomWidgets.ComboBox CreateComboBox(int x, int y)
        {
            var comboBox = new CustomWidgets.ComboBox(400, 20);
            comboBox.SetLocation(x, y);
            //--------------------
            //1. create landing part 
            comboBox.BackColor = Color.Green;

            //add small px to land part
            //image
            //load bitmap with gdi+                
            if (_arrowBmp == null)
            {
                _arrowBmp = _appHost.LoadImageAndBind("../Data/imgs/arrow_open.png");
            }
            LayoutFarm.CustomWidgets.ImageBox imgBox = new CustomWidgets.ImageBox(_arrowBmp.Width, _arrowBmp.Height);
            imgBox.ImageBinder = _arrowBmp;
            //--------------------------------------
            //2. float part
            var floatPart = new LayoutFarm.CustomWidgets.Box(400, 100);
            floatPart.BackColor = Color.Blue;
            comboBox.FloatPart = floatPart;
            //--------------------------------------
            //if click on this image then
            imgBox.MouseDown += (s, e) =>
            {
                e.CancelBubbling = true;
                if (comboBox.IsOpen)
                {
                    comboBox.CloseHinge();
                }
                else
                {
                    comboBox.OpenHinge();
                }
            };
            imgBox.LostMouseFocus += (s, e) =>
            {
                if (comboBox.IsOpen)
                {
                    comboBox.CloseHinge();
                }
            };
            comboBox.Add(imgBox);
            return comboBox;
        }

        LayoutFarm.CustomWidgets.MenuItem CreateMenuItem(int x, int y)
        {
            var mnuItem = new CustomWidgets.MenuItem(150, 20);
            mnuItem.BackColor = KnownColors.OrangeRed;
            mnuItem.SetLocation(x, y);
             
            //--------------------------------------
            //add small px to land part
            //image
            //load bitmap with gdi+        

            if (_arrowBmp == null)
            {
                _arrowBmp = _appHost.LoadImageAndBind("../Data/imgs/arrow_open.png");
            }
            LayoutFarm.CustomWidgets.ImageBox imgBox = new CustomWidgets.ImageBox(_arrowBmp.Width, _arrowBmp.Height);
            imgBox.ImageBinder = _arrowBmp;
            mnuItem.Add(imgBox);
            //--------------------------------------
            //if click on this image then
            imgBox.MouseDown += (s, e) =>
            {
                e.CancelBubbling = true;
                //1. maintenace parent menu***
                mnuItem.MaintenanceParentOpenState();
                //-----------------------------------------------
                if (mnuItem.IsOpened)
                {
                    mnuItem.Close();
                }
                else
                {
                    mnuItem.Open();
                }
            };
            imgBox.MouseUp += (s, e) =>
            {
                mnuItem.UnmaintenanceParentOpenState();
            };
            imgBox.LostMouseFocus += (s, e) =>
            {
                if (!mnuItem.MaintenceOpenState)
                {
                    mnuItem.CloseRecursiveUp();
                }
            };
            //--------------------------------------
            //2. float part
            var floatPart = new LayoutFarm.CustomWidgets.Box(400, 100);
            floatPart.BackColor = KnownColors.Gray;
            mnuItem.FloatPart = floatPart;
            return mnuItem;
        }


    }
}