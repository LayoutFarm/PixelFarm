﻿//MIT, 2017-present, WinterDev
using Typography.FontManagement;

namespace YourImplementation
{


    public static class FrameworkInitWinGDI
    {
        public static IInstalledTypefaceProvider GetFontLoader()
        {
            return CommonTextServiceSetup.FontLoader;
        }
        public static void SetupDefaultValues()
        {
            PixelFarm.Drawing.WinGdi.WinGdiPlusPlatform.SetInstalledTypefaceProvider(CommonTextServiceSetup.FontLoader);
        }
    }
}