﻿//Apache2, 2014-present, WinterDev

using LayoutFarm.UI;
namespace LayoutFarm
{
    public static class RenderElementExtension
    {
        public static void AddChild(this RenderElement renderBox, UIElement ui)
        {
            renderBox.AddChild(ui.GetPrimaryRenderElement(renderBox.Root));
        }
    }
}

