PixelFarm
=========
Hardware and Software 2D Rendering Library

1.  Hardware Rendering Technology:
    
    1.1  OpenGL ES 2.0  + AA Shader:
    This based on OpenGL ES2 and Its Shading Langauge (GLSL).
    
    ![gles2_aa_shader](https://cloud.githubusercontent.com/assets/7447159/20637925/221cc87a-b3c9-11e6-94a5-47c3b1026fd9.png)
    ---
    
 
	 
	
2. Software Rendering Technology: 

      2.1 Agg-Sharp

      >Agg-Sharp is the C# port of Anti-Grain Geometry (AGG)  version (version 2.4, BSD license) 
	
    This provides 'classic' (pure) software based rendering technology.

    Big thanks go to https://github.com/MatterHackers/agg-sharp
	
    ![agg_software](https://cloud.githubusercontent.com/assets/7447159/20637922/0b017956-b3c9-11e6-8c3b-41baad33af67.png)
	
    ---

    
    2.3  GDI+ , System.Drawing: as usual :)	
    
    ![gdiplus](https://cloud.githubusercontent.com/assets/7447159/20637923/1d0e1f78-b3c9-11e6-80d2-3c335bbca025.png)
    
	

3. PixelFarm's Typography :
   Agg's Subpixel Rendering 
   
	![lcd_05](https://cloud.githubusercontent.com/assets/7447159/22738636/ceba4840-ee3a-11e6-8cd6-400b9d356fd7.png)
   
    ---
	![lcd_07](https://cloud.githubusercontent.com/assets/7447159/22779712/6e1512c2-eeee-11e6-9352-8c0c4fc1dc95.png)

	---
	![lcd_08](https://cloud.githubusercontent.com/assets/7447159/22780442/590abe10-eef1-11e6-93f6-bf4bbcfa3f34.png)


	---
 
	![lcd_09](https://cloud.githubusercontent.com/assets/7447159/22780526/a0e65712-eef1-11e6-948a-eca8e8158aaa.png)

	![typography_thanamas](https://user-images.githubusercontent.com/7447159/44314099-d4357180-a43e-11e8-95c3-56894bfea1e4.png)


	--- 
	
![autofit_hfit01](https://cloud.githubusercontent.com/assets/7447159/26182259/282de0f4-3ba1-11e7-83ab-84ac1911526d.png)

 
---

The HtmlRenderer example!
---


 ![htmlbox_gles_with_selection](https://user-images.githubusercontent.com/7447159/49267623-fc952900-f48d-11e8-8ac8-03269c571c2c.png)
 
_pic 1: HtmlRenderer on GLES2 surface, text are renderered with the Typography, please note the text selection on the Html Surface._



  


(HtmlRender => https://github.com/LayoutFarm/HtmlRenderer,

Typography => https://github.com/LayoutFarm/Typography)


 
 
 

---
 
Ghost script's Tiger.svg
---

(https://commons.wikimedia.org/wiki/File:Ghostscript_Tiger.svg)


![tiger](https://user-images.githubusercontent.com/7447159/34709205-cdf2a2de-f548-11e7-8075-1958c087a883.png)

_pic 1: PixelFarm's Agg (1) vs Chrome (2)_


![tiger2](https://user-images.githubusercontent.com/7447159/34709373-8e048286-f549-11e7-8cbc-2941b7b9fa4e.png)

_pic 2: Agg's result, bitmap zoom-in to see fine details_ 
 
 
 
 
---
**HOW TO BUILD**

see https://github.com/PaintLab/PixelFarm/issues/37

---


 
**License:**

The project is based on multiple open-sourced projects (listed below) all using permissive licenses.

A license for a whole project is [**MIT**](https://opensource.org/licenses/MIT).

but if you use some part of the code please check each source file's header for the licensing info.



**Geometry**

BSD, 2002-2005, Maxim Shemanarev, Anti-Grain Geometry - Version 2.4, http://www.antigrain.com

BSD, 2007-2014, Lars Brubaker, agg-sharp, https://github.com/MatterHackers/agg-sharp

ZLIB, 2015, burningmine, CurveUtils. https://github.com/burningmime/curves

Boost, 2010-2014, Angus Johnson, Clipper.

BSD, 2009-2010, Poly2Tri Contributors, https://github.com/PaintLab/poly2tri-cs

SGI, 2000, Eric Veach, Tesselate.

MS-PL, 2018, SVG.NET, https://github.com/vvvv/SVG

MIT, 2018, Rohaan Hamid, https://github.com/rohaanhamid/simplify-csharp

**Image Processing**

MIT, 2008, dotPDN LLC, Rick Brewster, Chris Crosetto, Tom Jackson, Michael Kelsey, Brandon Ortiz, Craig Taylor, Chris Trevino, and Luke Walker., OpenPDN v 3.36.7 (Paint.NET), https://github.com/rivy/OpenPDN

BSD, 2002-2005, Maxim Shemanarev, Anti-Grain Geometry - Version 2.4, http://www.antigrain.com

MIT, 2016, Viktor Chlumsky, https://github.com/Chlumsky/msdfgen

MIT, 2009-2015, Bill Reiss, Rene Schulte and WriteableBitmapEx Contributors, https://github.com/teichgraf/WriteableBitmapEx

Apache2, 2012, Hernán J. González, https://github.com/leonbloy/pngcs

Apache2, 2010, Sebastian Stehle, .NET Image Tools Development Group. , https://imagetools.codeplex.com/ 

MIT, 2018, Tomáš Pažourek, Colourful, https://github.com/tompazourek/Colourful

MIT, 2011, Inedo, https://github.com/Inedo/iconmaker

**Font**

Apache2, 2016-2017, WinterDev, Samuel Carlsson, Sam Hocevar and others, https://github.com/LayoutFarm/Typography

Apache2, 2014-2016, Samuel Carlsson, https://github.com/vidstige/NRasterizer

MIT, 2015, Michael Popoloski, https://github.com/MikePopoloski/SharpFont

The FreeType Project LICENSE (3-clauses BSD style),2003-2016, David Turner, Robert Wilhelm, and Werner Lemberg and others, https://www.freetype.org/

**Platforms**

MIT, 2015-2015, Xamarin, Inc., https://github.com/mono/SkiaSharp

MIT, 2006-2009,  Stefanos Apostolopoulos and other Open Tool Kit Contributors, https://github.com/opentk/opentk

MIT, 2013, Antonie Blom,  https://github.com/andykorth/Pencil.Gaming

**Demo**

MIT, 2017, Wiesław Šoltés, ColorBlender, https://github.com/wieslawsoltes/ColorBlender

BSD, 2015, Darren David darren-code@lookorfeel.com, https://github.com/nobutaka/EasingCurvePresets
