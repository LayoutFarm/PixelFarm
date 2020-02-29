﻿//Apache2, 2014-present, WinterDev

using PixelFarm.Drawing;
using LayoutFarm.UI;
namespace LayoutFarm
{
    [DemoNote("1.4 DemoDrag")]
    class Demo_Drag : App
    {
        protected override void OnStart(AppHost host)
        {
            {
                var box1 = new LayoutFarm.CustomWidgets.Box(50, 50);
                box1.BackColor = Color.Red;
                box1.SetLocation(10, 10);
                //box1.dbugTag = 1;
                SetupActiveBoxProperties(box1);
                host.AddChild(box1);
            }
            //--------------------------------
            {
                var box2 = new LayoutFarm.CustomWidgets.Box(30, 30);
                box2.SetLocation(50, 50);
                //box2.dbugTag = 2;
                SetupActiveBoxProperties(box2);
                host.AddChild(box2);
            }
        }
        static void SetupActiveBoxProperties(LayoutFarm.CustomWidgets.Box box)
        {
            //1. mouse down         
            box.MouseDown += (s, e) =>
            {
                //box.BackColor = KnownColors.FromKnownColor(KnownColor.DeepSkyBlue);
                e.MouseCursorStyle = MouseCursorStyle.Pointer;
            };
            //2. mouse up
            box.MouseUp += (s, e) =>
            {
                e.MouseCursorStyle = MouseCursorStyle.Default;
                //box.BackColor = Color.LightGray;
                box.BackColor = Color.FromArgb(50, KnownColors.FromKnownColor(KnownColor.DeepSkyBlue));
            };
            box.MouseDrag += (s, e) =>
            {
                box.BackColor = Color.FromArgb(180, KnownColors.FromKnownColor(KnownColor.GreenYellow));
                Point pos = box.Position;
                box.SetLocation(pos.X + e.XDiff, pos.Y + e.YDiff);
                e.MouseCursorStyle = MouseCursorStyle.Pointer;
                e.CancelBubbling = true;
            };
        }
    }
}