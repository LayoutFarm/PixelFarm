﻿////MIT, 2014-present, WinterDev   

//using System.Collections.Generic;

//namespace PixelFarm.Drawing.Fonts
//{
//    class GdiPathFontFace
//    {
//        Dictionary<int, GdiPathFont> stockFonts = new Dictionary<int, GdiPathFont>();
//        public GdiPathFontFace(string facename)
//        {
//            this.FaceName = facename;
//        }
//        protected void OnDispose()
//        {
//        }
//        public string FaceName
//        {
//            get;
//            private set;
//        }
//        public GdiPathFont GetFontAtSpecificSize(int emsize)
//        {
//            GdiPathFont found;
//            if (!stockFonts.TryGetValue(emsize, out found))
//            {
//                found = new GdiPathFont(this, emsize);
//                stockFonts.Add(emsize, found);
//            }
//            return found;
//        }
//    }
//}