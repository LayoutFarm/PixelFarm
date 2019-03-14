﻿//Apache2, 2014-present, WinterDev

using PixelFarm.Drawing;
namespace LayoutFarm.CustomWidgets
{
    public class CustomContainerRenderBox : CustomRenderBox
    {
#if DEBUG
        public bool dbugBreak;
#endif
        public CustomContainerRenderBox(RootGraphic rootgfx, int width, int height)
            : base(rootgfx, width, height)
        {
            this.BackColor = Color.LightGray;
        }
        public override void ClearAllChildren()
        {
        }
    }
}