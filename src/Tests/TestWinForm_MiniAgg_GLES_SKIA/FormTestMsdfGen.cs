﻿//MIT, 2019-present, WinterDev
using System;
using System.Collections.Generic;
using System.Drawing;

using System.IO;
using System.Windows.Forms;

using Typography.OpenFont;
using Typography.Rendering;
using Typography.Contours;

using PixelFarm.Drawing;
using PixelFarm.CpuBlit;
using PixelFarm.CpuBlit.VertexProcessing;

namespace Mini
{
    public partial class FormTestMsdfGen : Form
    {
        public FormTestMsdfGen()
        {
            InitializeComponent();
        }


        static void CreateSampleMsdfTextureFont(string fontfile, float sizeInPoint, ushort startGlyphIndex, ushort endGlyphIndex, string outputFile)
        {
            //sample
            var reader = new OpenFontReader();

            using (var fs = new FileStream(fontfile, FileMode.Open))
            {
                //1. read typeface from font file
                Typeface typeface = reader.Read(fs);
                //sample: create sample msdf texture 
                //-------------------------------------------------------------
                var builder = new GlyphPathBuilder(typeface);
                //builder.UseTrueTypeInterpreter = this.chkTrueTypeHint.Checked;
                //builder.UseVerticalHinting = this.chkVerticalHinting.Checked;
                //-------------------------------------------------------------
                var atlasBuilder = new SimpleFontAtlasBuilder();


                for (ushort gindex = startGlyphIndex; gindex <= endGlyphIndex; ++gindex)
                {
                    //build glyph
                    builder.BuildFromGlyphIndex(gindex, sizeInPoint);

                    var glyphContourBuilder = new GlyphContourBuilder();
                    //glyphToContour.Read(builder.GetOutputPoints(), builder.GetOutputContours());
                    var genParams = new MsdfGenParams();
                    builder.ReadShapes(glyphContourBuilder);
                    //genParams.shapeScale = 1f / 64; //we scale later (as original C++ code use 1/64)
                    GlyphImage glyphImg = MsdfGlyphGen.CreateMsdfImage(glyphContourBuilder, genParams);
                    atlasBuilder.AddGlyph(gindex, glyphImg);

                    using (Bitmap bmp = new Bitmap(glyphImg.Width, glyphImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        int[] buffer = glyphImg.GetImageBuffer();

                        var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, glyphImg.Width, glyphImg.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                        System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpdata.Scan0, buffer.Length);
                        bmp.UnlockBits(bmpdata);
                        bmp.Save("d:\\WImageTest\\a001_xn2_" + gindex + ".png");
                    }
                }

                GlyphImage glyphImg2 = atlasBuilder.BuildSingleImage();
                using (Bitmap bmp = new Bitmap(glyphImg2.Width, glyphImg2.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, glyphImg2.Width, glyphImg2.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                    int[] intBuffer = glyphImg2.GetImageBuffer();

                    System.Runtime.InteropServices.Marshal.Copy(intBuffer, 0, bmpdata.Scan0, intBuffer.Length);
                    bmp.UnlockBits(bmpdata);
                    bmp.Save(outputFile);
                }
                atlasBuilder.SaveFontInfo("d:\\WImageTest\\a_info.bin");
                //
                //-----------
                //test read texture info back
                var atlasBuilder2 = new SimpleFontAtlasBuilder();
                var readbackFontAtlas = atlasBuilder2.LoadFontInfo("d:\\WImageTest\\a_info.bin");
            }
        }

        static void CreateSampleMsdfImg(GlyphContourBuilder tx, string outputFile)
        {
            //sample

            MsdfGenParams msdfGenParams = new MsdfGenParams();
            GlyphImage glyphImg = MsdfGlyphGen.CreateMsdfImage(tx, msdfGenParams);
            int w = glyphImg.Width;
            int h = glyphImg.Height;
            using (Bitmap bmp = new Bitmap(glyphImg.Width, glyphImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                int[] imgBuffer = glyphImg.GetImageBuffer();
                System.Runtime.InteropServices.Marshal.Copy(imgBuffer, 0, bmpdata.Scan0, imgBuffer.Length);
                bmp.UnlockBits(bmpdata);
                bmp.Save(outputFile);
            }
        }




        static void GetPoints(
           ExtMsdfgen.EdgeSegment edge_A,
           ExtMsdfgen.EdgeSegment edge_B,
           List<ExtMsdfgen.Vec2Info> points)
        {

            switch (edge_A.SegmentKind)
            {
                default: throw new NotSupportedException();
                case ExtMsdfgen.EdgeSegmentKind.LineSegment:
                    {
                        ExtMsdfgen.LinearSegment seg = (ExtMsdfgen.LinearSegment)edge_A;
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.Touch1, x = seg.P0.x, y = seg.P0.y });
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P1.x, y = seg.P1.y });
                    }
                    break;
                case ExtMsdfgen.EdgeSegmentKind.QuadraticSegment:
                    {
                        ExtMsdfgen.QuadraticSegment seg = (ExtMsdfgen.QuadraticSegment)edge_A;
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.Touch1, x = seg.P0.x, y = seg.P0.y });
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.C2, x = seg.P1.x, y = seg.P1.y });
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P2.x, y = seg.P2.y });
                    }
                    break;
                case ExtMsdfgen.EdgeSegmentKind.CubicSegment:
                    {
                        ExtMsdfgen.CubicSegment seg = (ExtMsdfgen.CubicSegment)edge_A;
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.Touch1, x = seg.P0.x, y = seg.P0.y });
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.C3, x = seg.P1.x, y = seg.P1.y });
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.C3, x = seg.P2.x, y = seg.P2.y });
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P3.x, y = seg.P3.y });
                    }
                    break;
            }

            switch (edge_B.SegmentKind)
            {
                default: throw new NotSupportedException();
                case ExtMsdfgen.EdgeSegmentKind.LineSegment:
                    {
                        ExtMsdfgen.LinearSegment seg = (ExtMsdfgen.LinearSegment)edge_B;
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P1.x, y = seg.P1.y });
                    }
                    break;
                case ExtMsdfgen.EdgeSegmentKind.QuadraticSegment:
                    {
                        ExtMsdfgen.QuadraticSegment seg = (ExtMsdfgen.QuadraticSegment)edge_B;
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.C2, x = seg.P1.x, y = seg.P1.y });
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P2.x, y = seg.P2.y });
                    }
                    break;
                case ExtMsdfgen.EdgeSegmentKind.CubicSegment:
                    {
                        ExtMsdfgen.CubicSegment seg = (ExtMsdfgen.CubicSegment)edge_B;
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.C3, x = seg.P1.x, y = seg.P1.y });
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.C3, x = seg.P2.x, y = seg.P2.y });
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P3.x, y = seg.P3.y });
                    }
                    break;
            }
        }


        static void FlattenPoints(ExtMsdfgen.EdgeSegment segment, bool isLastSeg, List<ExtMsdfgen.Vec2Info> points)
        {
            switch (segment.SegmentKind)
            {
                default: throw new NotSupportedException();
                case ExtMsdfgen.EdgeSegmentKind.LineSegment:
                    {
                        ExtMsdfgen.LinearSegment seg = (ExtMsdfgen.LinearSegment)segment;
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.Touch1, x = seg.P0.x, y = seg.P0.y });
                        //if (isLastSeg)
                        //{
                        //    points.Add(new Vec2Info() { Kind = Vec2PointKind.Touch2, x = seg.P1.x, y = seg.P1.y });
                        //}

                    }
                    break;
                case ExtMsdfgen.EdgeSegmentKind.QuadraticSegment:
                    {
                        ExtMsdfgen.QuadraticSegment seg = (ExtMsdfgen.QuadraticSegment)segment;
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.Touch1, x = seg.P0.x, y = seg.P0.y });
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.C2, x = seg.P1.x, y = seg.P1.y });
                        //if (isLastSeg)
                        //{
                        //    points.Add(new Vec2Info() { Kind = Vec2PointKind.Touch2, x = seg.P2.x, y = seg.P2.y });
                        //}
                    }
                    break;
                case ExtMsdfgen.EdgeSegmentKind.CubicSegment:
                    {
                        ExtMsdfgen.CubicSegment seg = (ExtMsdfgen.CubicSegment)segment;
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.Touch1, x = seg.P0.x, y = seg.P0.y });
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.C3, x = seg.P1.x, y = seg.P1.y });
                        points.Add(new ExtMsdfgen.Vec2Info() { Kind = ExtMsdfgen.Vec2PointKind.C3, x = seg.P2.x, y = seg.P2.y });
                        //if (isLastSeg)
                        //{
                        //    points.Add(new Vec2Info() { Kind = Vec2PointKind.Touch2, x = seg.P3.x, y = seg.P3.y });
                        //}
                    }
                    break;
            }

        }
        static List<ExtMsdfgen.ShapeCornerArms> CreateCornerAndArmList(List<ExtMsdfgen.Vec2Info> points)
        {
            List<ExtMsdfgen.ShapeCornerArms> cornerAndArms = new List<ExtMsdfgen.ShapeCornerArms>();
            int j = points.Count;

            for (int i = 1; i < j - 1; ++i)
            {
                ExtMsdfgen.Vec2Info p0 = points[i - 1];
                ExtMsdfgen.Vec2Info p1 = points[i];
                ExtMsdfgen.Vec2Info p2 = points[i + 1];
                ExtMsdfgen.ShapeCornerArms cornerArm = new ExtMsdfgen.ShapeCornerArms();
                cornerArm.leftPoint = new PixelFarm.Drawing.PointF((float)p0.x, (float)p0.y);
                cornerArm.LeftPointKind = p0.Kind;

                cornerArm.middlePoint = new PixelFarm.Drawing.PointF((float)p1.x, (float)p1.y);
                cornerArm.MiddlePointKind = p1.Kind;

                cornerArm.rightPoint = new PixelFarm.Drawing.PointF((float)p2.x, (float)p2.y);
                cornerArm.RightPointKind = p2.Kind;
                //
                cornerArm.dbugLeftIndex = i - 1;
                cornerArm.dbugMiddleIndex = i;
                cornerArm.dbugRightIndex = i + 1;


                cornerArm.CornerNo = cornerAndArms.Count; //**
                cornerAndArms.Add(cornerArm);
            }

            {
                //
                //PixelFarm.Drawing.PointF p0 = points[j - 2];
                //PixelFarm.Drawing.PointF p1 = points[j - 1];
                //PixelFarm.Drawing.PointF p2 = points[0];
                ExtMsdfgen.Vec2Info p0 = points[j - 2];
                ExtMsdfgen.Vec2Info p1 = points[j - 1];
                ExtMsdfgen.Vec2Info p2 = points[0];

                ExtMsdfgen.ShapeCornerArms cornerArm = new ExtMsdfgen.ShapeCornerArms();
                cornerArm.leftPoint = new PixelFarm.Drawing.PointF((float)p0.x, (float)p0.y);
                cornerArm.LeftPointKind = p0.Kind;

                cornerArm.middlePoint = new PixelFarm.Drawing.PointF((float)p1.x, (float)p1.y);
                cornerArm.MiddlePointKind = p1.Kind;

                cornerArm.rightPoint = new PixelFarm.Drawing.PointF((float)p2.x, (float)p2.y);
                cornerArm.RightPointKind = p2.Kind;
#if DEBUG
                cornerArm.dbugLeftIndex = j - 2;
                cornerArm.dbugMiddleIndex = j - 1;
                cornerArm.dbugRightIndex = 0;
#endif
                cornerArm.CornerNo = cornerAndArms.Count; //**
                cornerAndArms.Add(cornerArm);
            }

            {
                //
                //PixelFarm.Drawing.PointF p0 = points[j - 1];
                //PixelFarm.Drawing.PointF p1 = points[0];
                //PixelFarm.Drawing.PointF p2 = points[1];

                ExtMsdfgen.Vec2Info p0 = points[j - 1];
                ExtMsdfgen.Vec2Info p1 = points[0];
                ExtMsdfgen.Vec2Info p2 = points[1];
                ExtMsdfgen.ShapeCornerArms cornerArm = new ExtMsdfgen.ShapeCornerArms();
                cornerArm.leftPoint = new PixelFarm.Drawing.PointF((float)p0.x, (float)p0.y);
                cornerArm.LeftPointKind = p0.Kind;

                cornerArm.middlePoint = new PixelFarm.Drawing.PointF((float)p1.x, (float)p1.y);
                cornerArm.MiddlePointKind = p1.Kind;

                cornerArm.rightPoint = new PixelFarm.Drawing.PointF((float)p2.x, (float)p2.y);
                cornerArm.RightPointKind = p2.Kind;

#if DEBUG
                cornerArm.dbugLeftIndex = j - 1;
                cornerArm.dbugMiddleIndex = 0;
                cornerArm.dbugRightIndex = 1;
#endif

                cornerArm.CornerNo = cornerAndArms.Count; //**
                cornerAndArms.Add(cornerArm);
            }
            return cornerAndArms;
        }
        static List<ExtMsdfgen.ShapeCornerArms> CreateCornerArms(ExtMsdfgen.Contour contour)
        {

            //create corner-arm relation for a given contour
            List<ExtMsdfgen.EdgeHolder> edges = contour.edges;
            int j = edges.Count;

            List<ExtMsdfgen.Vec2Info> flattenPoints = new List<ExtMsdfgen.Vec2Info>();
            for (int i = 0; i < j; ++i)
            {
                ExtMsdfgen.EdgeSegment edge_A = edges[i].edgeSegment;
                FlattenPoints(edge_A, i == j - 1, flattenPoints);
            }
            return CreateCornerAndArmList(flattenPoints);

            //            for (int i = 1; i < j; ++i)
            //            {
            //                //
            //                ExtMsdfgen.EdgeSegment edge_A = edges[i - 1].edgeSegment;
            //                ExtMsdfgen.EdgeSegment edge_B = edges[i].edgeSegment;

            //                //
            //                GetPoints(edge_A, edge_B, flattenPoints);
            //                int m = flattenPoints.Count;
            //#if DEBUG
            //                if (m < 3)
            //                {
            //                    throw new System.NotSupportedException();
            //                }
            //#endif
            //                for (int n = 2; n < m;)
            //                {
            //                    ExtMsdfgen.ShapeCornerArms cornerArms = new ExtMsdfgen.ShapeCornerArms();


            //                    Vec2Info c0 = flattenPoints[n - 2];
            //                    Vec2Info c1 = flattenPoints[n - 1];
            //                    Vec2Info c2 = flattenPoints[n];

            //                    cornerArms.leftPoint = new PixelFarm.Drawing.PointF((float)c0.x, (float)c0.y);
            //                    cornerArms.middlePoint = new PixelFarm.Drawing.PointF((float)c1.x, (float)c1.y);
            //                    cornerArms.rightPoint = new PixelFarm.Drawing.PointF((float)c2.x, (float)c2.y);
            //                    //----------
            //#if DEBUG
            //                    cornerArms.dbugLeftIndex = n - 2;
            //                    cornerArms.dbugMiddleIndex = n - 1;
            //                    cornerArms.dbugRightIndex = n;
            //#endif
            //                    //----------
            //                    cornerArms.CornerNo = cornerArmsList.Count;
            //                    cornerArmsList.Add(cornerArms);
            //                    n += 1;
            //                }

            //                //--------


            //                flattenPoints.Clear();

            //            }

            //            //--------------------------------------------
            //            {
            //                //
            //                ExtMsdfgen.EdgeSegment edge_A = edges[j - 1].edgeSegment;
            //                ExtMsdfgen.EdgeSegment edge_B = edges[0].edgeSegment;

            //                GetPoints(edge_A, edge_B, flattenPoints);
            //                int m = flattenPoints.Count;
            //#if DEBUG
            //                if (m < 3)
            //                {
            //                    throw new System.NotSupportedException();
            //                }
            //#endif
            //                for (int n = 2; n < m;)
            //                {
            //                    ExtMsdfgen.ShapeCornerArms cornerArms = new ExtMsdfgen.ShapeCornerArms();


            //                    Vec2Info c0 = flattenPoints[n - 2];
            //                    Vec2Info c1 = flattenPoints[n - 1];
            //                    Vec2Info c2 = flattenPoints[n];

            //                    cornerArms.leftPoint = new PixelFarm.Drawing.PointF((float)c0.x, (float)c0.y);
            //                    cornerArms.middlePoint = new PixelFarm.Drawing.PointF((float)c1.x, (float)c1.y);
            //                    cornerArms.rightPoint = new PixelFarm.Drawing.PointF((float)c2.x, (float)c2.y);
            //                    //----------
            //#if DEBUG
            //                    cornerArms.dbugLeftIndex = n - 2;
            //                    cornerArms.dbugMiddleIndex = n - 1;
            //                    cornerArms.dbugRightIndex = n;
            //#endif
            //                    //----------
            //                    cornerArms.CornerNo = cornerArmsList.Count;
            //                    cornerArmsList.Add(cornerArms);
            //                    n += 1;
            //                }
            //            }
            //            {
            //                //
            //                PixelFarm.Drawing.PointF p0 = points[j - 1];
            //                PixelFarm.Drawing.PointF p1 = points[0];
            //                PixelFarm.Drawing.PointF p2 = points[1];


            //                ExtMsdfgen.ShapeCornerArms cornerArm = new ExtMsdfgen.ShapeCornerArms();
            //                cornerArm.leftPoint = p0;
            //                cornerArm.middlePoint = p1;
            //                cornerArm.rightPoint = p2;
            //#if DEBUG
            //                cornerArm.dbugLeftIndex = j - 1;
            //                cornerArm.dbugMiddleIndex = 0;
            //                cornerArm.dbugRightIndex = 1;
            //#endif

            //                cornerArm.CornerNo = cornerAndArms.Count; //**
            //                cornerAndArms.Add(cornerArm);
            //            }


        }
        static ExtMsdfgen.Shape CreateShape(VertexStore vxs, out ExtMsdfgen.BmpEdgeLut bmpLut)
        {
            List<ExtMsdfgen.EdgeSegment> flattenEdges = new List<ExtMsdfgen.EdgeSegment>();
            ExtMsdfgen.Shape shape1 = new ExtMsdfgen.Shape();

            int i = 0;
            double x, y;
            VertexCmd cmd;
            ExtMsdfgen.Contour cnt = null;
            double latestMoveToX = 0;
            double latestMoveToY = 0;
            double latestX = 0;
            double latestY = 0;

            List<ExtMsdfgen.ShapeCornerArms> cornerAndArms = null;

            while ((cmd = vxs.GetVertex(i, out x, out y)) != VertexCmd.NoMore)
            {
                switch (cmd)
                {
                    case VertexCmd.Close:
                        {
                            //close current cnt

                            if ((latestMoveToX != latestX) ||
                                (latestMoveToY != latestY))
                            {
                                //add line to close the shape
                                if (cnt != null)
                                {
                                    flattenEdges.Add(cnt.AddLine(latestX, latestY, latestMoveToX, latestMoveToY));
                                }

                            }
                            if (cnt != null)
                            {
                                //***
                                cornerAndArms = CreateCornerArms(cnt);
                                shape1.contours.Add(cnt);
                                //***
                                cnt = null;
                            }
                        }
                        break;
                    case VertexCmd.P2c:
                        {

                            //C3 curve (Quadratic)
                            if (cnt == null)
                            {
                                cnt = new ExtMsdfgen.Contour();
                            }

                            VertexCmd cmd1 = vxs.GetVertex(i + 1, out double x1, out double y1);
                            i++;

                            if (cmd1 != VertexCmd.LineTo)
                            {
                                throw new NotSupportedException();
                            }

                            flattenEdges.Add(cnt.AddQuadraticSegment(latestX, latestY, x, y, x1, y1));

                            latestX = x1;
                            latestY = y1;

                        }
                        break;
                    case VertexCmd.P3c:
                        {
                            //C4 curve (Cubic)
                            if (cnt == null)
                            {
                                cnt = new ExtMsdfgen.Contour();
                            }

                            VertexCmd cmd1 = vxs.GetVertex(i + 1, out double x2, out double y2);
                            VertexCmd cmd2 = vxs.GetVertex(i + 2, out double x3, out double y3);
                            i += 2;

                            if (cmd1 != VertexCmd.P3c || cmd2 != VertexCmd.LineTo)
                            {
                                throw new NotSupportedException();
                            }

                            flattenEdges.Add(cnt.AddCubicSegment(latestX, latestY, x, y, x2, y2, x3, y3));

                            latestX = x3;
                            latestY = y3;

                        }
                        break;
                    case VertexCmd.LineTo:
                        {
                            if (cnt == null)
                            {
                                cnt = new ExtMsdfgen.Contour();
                            }
                            ExtMsdfgen.LinearSegment lineseg = cnt.AddLine(latestX, latestY, x, y);
                            flattenEdges.Add(lineseg);

                            latestX = x;
                            latestY = y;
                        }
                        break;
                    case VertexCmd.MoveTo:
                        {
                            latestX = latestMoveToX = x;
                            latestY = latestMoveToY = y;
                            if (cnt != null)
                            {
                                shape1.contours.Add(cnt);
                                cnt = null;
                            }
                        }
                        break;
                }
                i++;
            }

            if (cnt != null)
            {
                shape1.contours.Add(cnt);
                cnt = null;
            }

            //from a given shape we create a corner-arm for each corner 

            if (cornerAndArms != null)
            {
                ConnectToOthers(cornerAndArms);
            }


            bmpLut = new ExtMsdfgen.BmpEdgeLut(cornerAndArms, flattenEdges);

            return shape1;
        }

        private void cmdTestMsdfGen_Click(object sender, EventArgs e)
        {
            List<PixelFarm.Drawing.PointF> points = new List<PixelFarm.Drawing.PointF>();
            points.AddRange(new PixelFarm.Drawing.PointF[]{
                    new PixelFarm.Drawing.PointF(10, 20),
                    new PixelFarm.Drawing.PointF(50, 60),
                    new PixelFarm.Drawing.PointF(80, 20),
                    new PixelFarm.Drawing.PointF(50, 10),
                    //new PixelFarm.Drawing.PointF(10, 20)
            });
            //1. 
            ExtMsdfgen.Shape shape1 = null;
            RectD bounds = RectD.ZeroIntersection;
            using (VxsTemp.Borrow(out var v1))
            using (VectorToolBox.Borrow(v1, out PathWriter w))
            {
                int count = points.Count;
                PixelFarm.Drawing.PointF pp = points[0];
                w.MoveTo(pp.X, pp.Y);
                for (int i = 1; i < count; ++i)
                {
                    pp = points[i];
                    w.LineTo(pp.X, pp.Y);
                }
                w.CloseFigure();

                bounds = v1.GetBoundingRect();
                shape1 = CreateShape(v1, out var bmpLut);
            }

            //using (VxsTemp.Borrow(out var v1))
            //using (VectorToolBox.Borrow(v1, out PathWriter w))
            //{


            //    w.MoveTo(15, 20);
            //    w.LineTo(50, 60);
            //    w.LineTo(60, 20);
            //    w.LineTo(50, 10);
            //    w.CloseFigure();

            //    bounds = v1.GetBoundingRect();
            //    shape1 = CreateShape(v1);
            //}

            //2.
            ExtMsdfgen.MsdfGenParams msdfGenParams = new ExtMsdfgen.MsdfGenParams();
            ExtMsdfgen.GlyphImage glyphImg = ExtMsdfgen.MsdfGlyphGen.CreateMsdfImage(shape1, msdfGenParams);
            using (Bitmap bmp = new Bitmap(glyphImg.Width, glyphImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                int[] buffer = glyphImg.GetImageBuffer();

                var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, glyphImg.Width, glyphImg.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpdata.Scan0, buffer.Length);
                bmp.UnlockBits(bmpdata);
                bmp.Save("d:\\WImageTest\\msdf_shape.png");
                //
            }
        }

        static List<ExtMsdfgen.ShapeCornerArms> CreateCornerAndArmList(List<PixelFarm.Drawing.PointF> points)
        {
            List<ExtMsdfgen.ShapeCornerArms> cornerAndArms = new List<ExtMsdfgen.ShapeCornerArms>();
            int j = points.Count;

            for (int i = 1; i < j - 1; ++i)
            {
                PixelFarm.Drawing.PointF p0 = points[i - 1];
                PixelFarm.Drawing.PointF p1 = points[i];
                PixelFarm.Drawing.PointF p2 = points[i + 1];
                ExtMsdfgen.ShapeCornerArms cornerArm = new ExtMsdfgen.ShapeCornerArms();
                cornerArm.leftPoint = p0;
                cornerArm.middlePoint = p1;
                cornerArm.rightPoint = p2;

                cornerArm.dbugLeftIndex = i - 1;
                cornerArm.dbugMiddleIndex = i;
                cornerArm.dbugRightIndex = i + 1;


                cornerArm.CornerNo = cornerAndArms.Count; //**
                cornerAndArms.Add(cornerArm);
            }

            {
                //
                PixelFarm.Drawing.PointF p0 = points[j - 2];
                PixelFarm.Drawing.PointF p1 = points[j - 1];
                PixelFarm.Drawing.PointF p2 = points[0];


                ExtMsdfgen.ShapeCornerArms cornerArm = new ExtMsdfgen.ShapeCornerArms();
                cornerArm.leftPoint = p0;
                cornerArm.middlePoint = p1;
                cornerArm.rightPoint = p2;

#if DEBUG
                cornerArm.dbugLeftIndex = j - 2;
                cornerArm.dbugMiddleIndex = j - 1;
                cornerArm.dbugRightIndex = 0;
#endif
                cornerArm.CornerNo = cornerAndArms.Count; //**
                cornerAndArms.Add(cornerArm);
            }

            {
                //
                PixelFarm.Drawing.PointF p0 = points[j - 1];
                PixelFarm.Drawing.PointF p1 = points[0];
                PixelFarm.Drawing.PointF p2 = points[1];


                ExtMsdfgen.ShapeCornerArms cornerArm = new ExtMsdfgen.ShapeCornerArms();
                cornerArm.leftPoint = p0;
                cornerArm.middlePoint = p1;
                cornerArm.rightPoint = p2;
#if DEBUG
                cornerArm.dbugLeftIndex = j - 1;
                cornerArm.dbugMiddleIndex = 0;
                cornerArm.dbugRightIndex = 1;
#endif

                cornerArm.CornerNo = cornerAndArms.Count; //**
                cornerAndArms.Add(cornerArm);
            }
            return cornerAndArms;
        }
        void TranslateArms(List<ExtMsdfgen.ShapeCornerArms> cornerArms, double dx, double dy)
        {
            //test 2 if each edge has unique color
            int j = cornerArms.Count;
            for (int i = 0; i < j; ++i)
            {
                ExtMsdfgen.ShapeCornerArms arm = cornerArms[i];
                arm.Offset((float)dx, (float)dy);
            }
        }


        static void ConnectToOthers(List<ExtMsdfgen.ShapeCornerArms> cornerArms)
        {
            //test 2 if each edge has unique color
            // 
            //int currentColor = 0;
            int j = cornerArms.Count;

            //List<PixelFarm.Drawing.Color> colorList = new List<PixelFarm.Drawing.Color>();
            //for (int i = 0; i < j + 1; ++i)
            //{
            //    colorList.Add(new PixelFarm.Drawing.Color(255, (byte)(100 + i * 20), (byte)(100 + i * 20), (byte)(100 + i * 20)));
            //}

            //int max_colorCount = colorList.Count;

            for (int i = 1; i < j; ++i)
            {
                ExtMsdfgen.ShapeCornerArms c_prev = cornerArms[i - 1];
                ExtMsdfgen.ShapeCornerArms c_current = cornerArms[i];
                //
                //PixelFarm.Drawing.Color selColor = colorList[currentColor];
                //c_prev.rightExtendedColor = c_current.leftExtededColor = selColor; //same color
                //
                c_prev.leftExtendedPointDest_Outer = c_current.rightExtendedPoint_Outer;
                c_prev.leftExtendedPointDest_Inner = c_current.rightExtendedPoint_Inner;
                //
                c_current.rightExtendedPointDest_Outer = c_prev.leftExtendedPoint_Outer;
                c_current.rightExtendedPointDest_Inner = c_prev.leftExtendedPoint_Inner;
                //
                //currentColor++;
                //if (currentColor > max_colorCount)
                //{
                //    //make it ready for next round
                //    currentColor = 0;
                //}
            }

            {
                //the last one
                ExtMsdfgen.ShapeCornerArms c_prev = cornerArms[j - 1];
                ExtMsdfgen.ShapeCornerArms c_current = cornerArms[0];
                //PixelFarm.Drawing.Color selColor = colorList[currentColor];
                //c_prev.rightExtendedColor = c_current.leftExtededColor = selColor; //same color
                //
                c_prev.leftExtendedPointDest_Outer = c_current.rightExtendedPoint_Outer;
                c_prev.leftExtendedPointDest_Inner = c_current.rightExtendedPoint_Inner;
                //
                c_current.rightExtendedPointDest_Outer = c_prev.leftExtendedPoint_Outer;
                c_current.rightExtendedPointDest_Inner = c_prev.leftExtendedPoint_Inner;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            //test fake msdf

            List<PixelFarm.Drawing.PointF> points = new List<PixelFarm.Drawing.PointF>();
            points.AddRange(new PixelFarm.Drawing.PointF[]{
                    new PixelFarm.Drawing.PointF(10, 20),
                    new PixelFarm.Drawing.PointF(50, 60),
                    new PixelFarm.Drawing.PointF(80, 20),
                    new PixelFarm.Drawing.PointF(50, 10),
                    //new PixelFarm.Drawing.PointF(10, 20)
            });
            //--------------------
            //create outside connected line
            List<ExtMsdfgen.ShapeCornerArms> cornerAndArms = CreateCornerAndArmList(points);


            using (VxsTemp.Borrow(out var v1))
            using (VectorToolBox.Borrow(v1, out PathWriter w))
            {
                int count = points.Count;
                PixelFarm.Drawing.PointF pp = points[0];
                w.MoveTo(pp.X, pp.Y);
                for (int i = 1; i < count; ++i)
                {
                    pp = points[i];
                    w.LineTo(pp.X, pp.Y);
                }
                w.CloseFigure();

                RectD bounds = v1.GetBoundingRect();
                bounds.Inflate(15);

                //---------
                //Poly2Tri.Polygon polygon = CreatePolygon(points, bounds);
                //Poly2Tri.P2T.Triangulate(polygon);


                using (MemBitmap bmp = new MemBitmap(100, 100))
                using (AggPainterPool.Borrow(bmp, out AggPainter painter))
                {
                    painter.Clear(PixelFarm.Drawing.Color.Black);
                    painter.Fill(v1, PixelFarm.Drawing.Color.White);
                    //DrawTessTriangles(polygon, painter);


                    painter.StrokeColor = PixelFarm.Drawing.Color.Red;
                    painter.StrokeWidth = 1;

                    int cornerArmCount = cornerAndArms.Count;
                    for (int n = 1; n < cornerArmCount; ++n)
                    {
                        ExtMsdfgen.ShapeCornerArms c0 = cornerAndArms[n - 1];
                        ExtMsdfgen.ShapeCornerArms c1 = cornerAndArms[n];

                        using (VxsTemp.Borrow(out var v2))
                        using (VectorToolBox.Borrow(v2, out PathWriter writer))
                        {
                            //outer
                            writer.MoveTo(c0.middlePoint.X, c0.middlePoint.Y);
                            writer.LineTo(c0.rightExtendedPoint_Outer.X, c0.rightExtendedPoint_Outer.Y);
                            writer.LineTo(c0.rightExtendedPointDest_Outer.X, c0.rightExtendedPointDest_Outer.Y);
                            writer.LineTo(c1.middlePoint.X, c1.middlePoint.Y);
                            writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                            writer.CloseFigure();
                            //
                            painter.Fill(v2, c0.OuterColor);

                            //inner
                            v2.Clear();
                            writer.MoveTo(c0.middlePoint.X, c0.middlePoint.Y);
                            writer.LineTo(c0.rightExtendedPoint_Inner.X, c0.rightExtendedPoint_Inner.Y);
                            writer.LineTo(c0.rightExtendedPointDest_Inner.X, c0.rightExtendedPointDest_Inner.Y);
                            writer.LineTo(c1.middlePoint.X, c1.middlePoint.Y);
                            writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                            writer.CloseFigure();
                            //
                            painter.Fill(v2, c0.InnerColor);

                        }
                    }
                    //--------------------------------------------------------------------------------
                    {
                        //the last one
                        ExtMsdfgen.ShapeCornerArms c0 = cornerAndArms[cornerArmCount - 1];
                        ExtMsdfgen.ShapeCornerArms c1 = cornerAndArms[0];

                        using (VxsTemp.Borrow(out var v2))
                        using (VectorToolBox.Borrow(v2, out PathWriter writer))
                        {
                            //
                            writer.MoveTo(c0.middlePoint.X, c0.middlePoint.Y);
                            writer.LineTo(c0.rightExtendedPoint_Outer.X, c0.rightExtendedPoint_Outer.Y);
                            writer.LineTo(c0.rightExtendedPointDest_Outer.X, c0.rightExtendedPointDest_Outer.Y);
                            writer.LineTo(c1.middlePoint.X, c1.middlePoint.Y);
                            writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                            writer.CloseFigure();
                            //
                            painter.Fill(v2, c0.OuterColor);

                            //inner
                            v2.Clear();
                            writer.MoveTo(c0.middlePoint.X, c0.middlePoint.Y);
                            writer.LineTo(c0.rightExtendedPoint_Inner.X, c0.rightExtendedPoint_Inner.Y);
                            writer.LineTo(c0.rightExtendedPointDest_Inner.X, c0.rightExtendedPointDest_Inner.Y);
                            writer.LineTo(c1.middlePoint.X, c1.middlePoint.Y);
                            writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                            writer.CloseFigure();
                            //
                            painter.Fill(v2, c0.InnerColor);
                        }
                    }
                    painter.Fill(v1, PixelFarm.Drawing.Color.White);

                    //foreach (ShapeCornerArms cornerArm in cornerAndArms)
                    //{

                    //    //right arm
                    //    painter.StrokeColor = cornerArm.rightExtendedColor;
                    //    painter.DrawLine(cornerArm.middlePoint.X, cornerArm.middlePoint.Y,
                    //        cornerArm.rightExtendedPoint.X, cornerArm.rightExtendedPoint.Y);

                    //    //left arm
                    //    painter.StrokeColor = cornerArm.leftExtededColor;
                    //    painter.DrawLine(cornerArm.middlePoint.X, cornerArm.middlePoint.Y,
                    //        cornerArm.leftExtendedPoint.X, cornerArm.leftExtendedPoint.Y);

                    //    using (VxsTemp.Borrow(out var v2))
                    //    using (VectorToolBox.Borrow(v2, out PathWriter writer))
                    //    {
                    //        writer.MoveTo(cornerArm.middlePoint.X, cornerArm.middlePoint.Y);
                    //        writer.LineTo(cornerArm.rightExtendedPoint.X, cornerArm.rightExtendedPoint.Y);
                    //        writer.LineTo(cornerArm.rightDestConnectedPoint.X, cornerArm.rightDestConnectedPoint.Y);
                    //        writer.LineTo(cornerArm.rightDestConnectedPoint.X, cornerArm.rightDestConnectedPoint.Y);

                    //    } 
                    //}
                    bmp.SaveImage("d:\\WImageTest\\msdf_fake1.png");
                }

            }
        }

        void DrawTessTriangles(Poly2Tri.Polygon polygon, AggPainter painter)
        {
            return;
            foreach (var triangle in polygon.Triangles)
            {
                Poly2Tri.TriangulationPoint p0 = triangle.P0;
                Poly2Tri.TriangulationPoint p1 = triangle.P1;
                Poly2Tri.TriangulationPoint p2 = triangle.P2;


                ////we do not store triangulation points (p0,p1,02)
                ////an EdgeLine is created after we create GlyphTriangles.

                ////triangulate point p0->p1->p2 is CCW ***             
                //e0 = NewEdgeLine(p0, p1, tri.EdgeIsConstrained(tri.FindEdgeIndex(p0, p1)));
                //e1 = NewEdgeLine(p1, p2, tri.EdgeIsConstrained(tri.FindEdgeIndex(p1, p2)));
                //e2 = NewEdgeLine(p2, p0, tri.EdgeIsConstrained(tri.FindEdgeIndex(p2, p0)));

                painter.RenderQuality = RenderQuality.HighQuality;
                painter.StrokeColor = PixelFarm.Drawing.Color.Green;
                painter.StrokeWidth = 1.5f;
                painter.DrawLine(p0.X, p0.Y, p1.X, p1.Y);
                painter.DrawLine(p1.X, p1.Y, p2.X, p2.Y);
                painter.DrawLine(p2.X, p2.Y, p0.X, p0.Y);
            }
        }



        /// <summary>
        /// create polygon from GlyphContour
        /// </summary>
        /// <param name="cnt"></param>
        /// <returns></returns>
        static Poly2Tri.Polygon CreatePolygon(List<PixelFarm.Drawing.PointF> flattenPoints, double dx, double dy)
        {
            List<Poly2Tri.TriangulationPoint> points = new List<Poly2Tri.TriangulationPoint>();

            //limitation: poly tri not accept duplicated points! *** 
            double prevX = 0;
            double prevY = 0;

            int j = flattenPoints.Count;
            //pass
            for (int i = 0; i < j; ++i)
            {
                PixelFarm.Drawing.PointF pp = flattenPoints[i];

                double x = pp.X + dx; //start from original X***
                double y = pp.Y + dy; //start from original Y***

                if (x == prevX && y == prevY)
                {
                    if (i > 0)
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    var triPoint = new Poly2Tri.TriangulationPoint(prevX = x, prevY = y) { userData = pp };
                    //#if DEBUG
                    //                    p.dbugTriangulationPoint = triPoint;
                    //#endif
                    points.Add(triPoint);

                }
            }

            return new Poly2Tri.Polygon(points.ToArray());

        }

        static Poly2Tri.Polygon CreateInvertedPolygon(List<PixelFarm.Drawing.PointF> flattenPoints, RectD bounds)
        {

            Poly2Tri.Polygon mainPolygon = new Poly2Tri.Polygon(new Poly2Tri.TriangulationPoint[]
            {
                new Poly2Tri.TriangulationPoint( bounds.Left,   bounds.Bottom),
                new Poly2Tri.TriangulationPoint( bounds.Right,  bounds.Bottom),
                new Poly2Tri.TriangulationPoint( bounds.Right,  bounds.Top),
                new Poly2Tri.TriangulationPoint( bounds.Left,   bounds.Top)
            });

            //find bounds

            List<Poly2Tri.TriangulationPoint> points = new List<Poly2Tri.TriangulationPoint>();

            //limitation: poly tri not accept duplicated points! *** 
            double prevX = 0;
            double prevY = 0;

            int j = flattenPoints.Count;
            //pass
            for (int i = 0; i < j; ++i)
            {
                PixelFarm.Drawing.PointF pp = flattenPoints[i];

                double x = pp.X; //start from original X***
                double y = pp.Y; //start from original Y***

                if (x == prevX && y == prevY)
                {
                    if (i > 0)
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    var triPoint = new Poly2Tri.TriangulationPoint(prevX = x, prevY = y) { userData = pp };
                    //#if DEBUG
                    //                    p.dbugTriangulationPoint = triPoint;
                    //#endif
                    points.Add(triPoint);

                }
            }

            Poly2Tri.Polygon p2 = new Poly2Tri.Polygon(points.ToArray());

            mainPolygon.AddHole(p2);
            return mainPolygon;
        }


        void GetExampleVxs(VertexStore outputVxs)
        {
            //counter-clockwise 
            //a triangle
            //outputVxs.AddMoveTo(10, 20);
            //outputVxs.AddLineTo(50, 60);
            //outputVxs.AddLineTo(70, 20);
            //outputVxs.AddCloseFigure();

            //a quad
            //outputVxs.AddMoveTo(10, 20);
            //outputVxs.AddLineTo(50, 60);
            //outputVxs.AddLineTo(70, 20);
            //outputVxs.AddLineTo(50, 10);
            //outputVxs.AddCloseFigure();

            //curve
            //outputVxs.AddMoveTo(5, 5);
            //outputVxs.AddLineTo(50, 60);
            //outputVxs.AddCurve4To(70, 20, 50, 10, 10, 20);
            //outputVxs.AddCloseFigure();

            outputVxs.AddMoveTo(5, 5);
            outputVxs.AddLineTo(50, 60);
            outputVxs.AddCurve4To(70, 20, 50, 10, 10, 5);
            outputVxs.AddCloseFigure();


            //outputVxs.AddMoveTo(5, 5);
            //outputVxs.AddLineTo(50, 60);
            //outputVxs.AddLineTo(70, 20);
            //outputVxs.AddLineTo(50, 10);

            //outputVxs.AddCloseFigure();

            //
            //write example data to outputVxs 
            //counter-clockwise
            //outputVxs.AddMoveTo(10, 20);
            //outputVxs.AddLineTo(30, 80);
            //outputVxs.AddLineTo(50, 20);
            //outputVxs.AddLineTo(40, 20);
            //outputVxs.AddLineTo(30, 50);
            //outputVxs.AddLineTo(20, 20);
            //outputVxs.AddCloseFigure();

        }
        //List<PixelFarm.Drawing.PointF> GetSamplePointList()
        //{
        //    List<PixelFarm.Drawing.PointF> points = new List<PixelFarm.Drawing.PointF>();

        //    //counter-clockwise
        //    //points.AddRange(new PixelFarm.Drawing.PointF[]{
        //    //        new PixelFarm.Drawing.PointF(10 , 20),
        //    //        new PixelFarm.Drawing.PointF(50 , 60),
        //    //        new PixelFarm.Drawing.PointF(70 , 20),
        //    //        //new PixelFarm.Drawing.PointF(50 , 10),
        //    //       //close figure
        //    //});
        //    //points.AddRange(new PixelFarm.Drawing.PointF[]{
        //    //        new PixelFarm.Drawing.PointF(10 , 20),
        //    //        new PixelFarm.Drawing.PointF(50 , 60),
        //    //        new PixelFarm.Drawing.PointF(70 , 20),
        //    //        new PixelFarm.Drawing.PointF(50 , 10),
        //    //       //close figure
        //    //});
        //    ////counter-clockwise
        //    points.AddRange(new PixelFarm.Drawing.PointF[]{
        //            new PixelFarm.Drawing.PointF(10 , 20),
        //            new PixelFarm.Drawing.PointF(30 , 80),
        //            new PixelFarm.Drawing.PointF(50 , 20 ),
        //            new PixelFarm.Drawing.PointF(40 , 20 ),
        //            new PixelFarm.Drawing.PointF(30 , 50 ),
        //            new PixelFarm.Drawing.PointF(20 , 20 ),
        //            //close figure
        //    });

        //    float scale = 0.25f;
        //    int j = points.Count;
        //    for (int i = 0; i < j; ++i)
        //    {
        //        PixelFarm.Drawing.PointF p = points[i];
        //        points[i] = new PixelFarm.Drawing.PointF(p.X * scale, p.Y * scale);
        //    }

        //    return points;
        //}

        class CustomBlendOp1 : BitmapBufferEx.CustomBlendOp
        {
            const int WHITE = (255 << 24) | (255 << 16) | (255 << 8) | 255;
            const int BLACK = (255 << 24);
            const int GREEN = (255 << 24) | (255 << 8);
            const int RED = (255 << 24) | (255 << 16);

            public override int Blend(int currentExistingColor, int inputColor)
            {
                //this is our custom blending 
                if (currentExistingColor != WHITE && currentExistingColor != BLACK)
                {
                    //return RED;
                    //WINDOWS: ABGR
                    int existing_R = currentExistingColor & 0xFF;
                    int existing_G = (currentExistingColor >> 8) & 0xFF;
                    int existing_B = (currentExistingColor >> 16) & 0xFF;

                    int new_R = inputColor & 0xFF;
                    int new_G = (inputColor >> 8) & 0xFF;
                    int new_B = (inputColor >> 16) & 0xFF;

                    if (new_R == existing_R && new_B == existing_B)
                    {
                        return inputColor;
                    }

                    //***
                    //Bitmap extension arrange this to ARGB?
                    return RED;
                    //return base.Blend(currentExistingColor, inputColor);
                }
                else
                {
                    return inputColor;
                }
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            //test fake msdf (this is not real msdf gen)
            //--------------------  
            using (VxsTemp.Borrow(out var v1))
            using (VectorToolBox.Borrow(v1, out PathWriter w))
            {
                //--------
                GetExampleVxs(v1);
                //--------

                ExtMsdfgen.Shape shape1 = CreateShape(v1, out ExtMsdfgen.BmpEdgeLut bmpLut7);
                ExtMsdfgen.MsdfGenParams msdfGenParams = new ExtMsdfgen.MsdfGenParams();
                ExtMsdfgen.MsdfGlyphGen.PreviewSizeAndLocation(
                   shape1,
                   msdfGenParams,
                   out int imgW, out int imgH,
                   out ExtMsdfgen.Vector2 translateVec);

                //---------
                List<ExtMsdfgen.ShapeCornerArms> cornerAndArms = bmpLut7.CornerArms;
                TranslateArms(cornerAndArms, translateVec.x, translateVec.y);
                //---------


                //Poly2Tri.Polygon polygon1 = CreatePolygon(points, translateVec.x, translateVec.y);
                //Poly2Tri.P2T.Triangulate(polygon1);
                //---------

                using (MemBitmap bmpLut = new MemBitmap(imgW, imgH))
                using (VxsTemp.Borrow(out var v5, out var v6))
                using (VectorToolBox.Borrow(out CurveFlattener flattener))
                using (AggPainterPool.Borrow(bmpLut, out AggPainter painter))
                {

                    painter.RenderQuality = RenderQuality.Fast;
                    painter.Clear(PixelFarm.Drawing.Color.Black);

                    v1.TranslateToNewVxs(translateVec.x, translateVec.y, v5);
                    flattener.MakeVxs(v5, v6);
                    painter.Fill(v6, PixelFarm.Drawing.Color.White);

                    painter.StrokeColor = PixelFarm.Drawing.Color.Red;
                    painter.StrokeWidth = 1;

                    CustomBlendOp1 customBlendOp1 = new CustomBlendOp1();

                    int cornerArmCount = cornerAndArms.Count;
                    for (int n = 1; n < cornerArmCount; ++n)
                    {
                        ExtMsdfgen.ShapeCornerArms c0 = cornerAndArms[n - 1];
                        ExtMsdfgen.ShapeCornerArms c1 = cornerAndArms[n];

                        using (VxsTemp.Borrow(out var v2))
                        using (VectorToolBox.Borrow(v2, out PathWriter writer))
                        {
                            painter.CurrentBxtBlendOp = customBlendOp1; //**

                            //counter-clockwise
                            if (c0.MiddlePointKindIsTouchPoint && c0.RightPointKindIsTouchPoint)
                            {
                                writer.MoveTo(c0.middlePoint.X, c0.middlePoint.Y);
                                writer.LineTo(c0.leftExtendedPoint_Outer.X, c0.leftExtendedPoint_Outer.Y);
                                writer.LineTo(c0.leftExtendedPointDest_Outer.X, c0.leftExtendedPointDest_Outer.Y);
                                writer.LineTo(c1.middlePoint.X, c1.middlePoint.Y);
                                writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                writer.CloseFigure();
                                // 
                                painter.Fill(v2, c0.OuterColor);
                            }


                            //------------------
                            //inner
                            v2.Clear();
                            if (c0.MiddlePointKindIsTouchPoint && c0.RightPointKindIsTouchPoint)
                            {
                                writer.MoveTo(c0.leftExtendedPoint_Inner.X, c0.leftExtendedPoint_Inner.Y);
                                writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                writer.LineTo(c1.middlePoint.X, c1.middlePoint.Y);
                                writer.LineTo(c1.rightExtendedPoint_Inner.X, c1.rightExtendedPoint_Inner.Y);
                                writer.LineTo(c0.leftExtendedPoint_Inner.X, c0.leftExtendedPoint_Inner.Y);
                                writer.CloseFigure();
                                ////
                                painter.Fill(v2, c0.InnerColor);
                            }
                            //------------------
                            //outer gap
                            v2.Clear();
                            //writer.MoveTo(c0.middlePoint.X, c0.middlePoint.Y);
                            //writer.LineTo(c0.rightExtendedPoint_OuterGap.X, c0.rightExtendedPoint_OuterGap.Y);
                            //writer.LineTo(c0.leftExtendedPoint_OuterGap.X, c0.leftExtendedPoint_OuterGap.Y);
                            //writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                            //writer.CloseFigure();
                            painter.CurrentBxtBlendOp = null;//**

                            if (c0.MiddlePointKindIsTouchPoint && c0.RightPointKindIsTouchPoint)
                            {
                                //large corner that cover gap
                                writer.MoveTo(c0.leftExtendedPoint_Inner.X, c0.leftExtendedPoint_Inner.Y);
                                writer.LineTo(c0.rightExtendedPoint_Outer.X, c0.rightExtendedPoint_Outer.Y);
                                writer.LineTo(c0.leftExtendedPoint_Outer.X, c0.leftExtendedPoint_Outer.Y);
                                writer.LineTo(c0.rightExtendedPoint_Inner.X, c0.rightExtendedPoint_Inner.Y);
                                writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                writer.CloseFigure();
                                painter.Fill(v2, PixelFarm.Drawing.Color.Red);
                            }
                        }
                    }
                    {
                        //the last one
                        ExtMsdfgen.ShapeCornerArms c0 = cornerAndArms[cornerArmCount - 1];
                        ExtMsdfgen.ShapeCornerArms c1 = cornerAndArms[0];

                        using (VxsTemp.Borrow(out var v2))
                        using (VectorToolBox.Borrow(v2, out PathWriter writer))
                        {
                            painter.CurrentBxtBlendOp = customBlendOp1; //**
                                                                        //counter-clockwise

                            //------------------
                            //outer
                            if (c0.MiddlePointKindIsTouchPoint && c0.RightPointKindIsTouchPoint)
                            {
                                writer.MoveTo(c0.middlePoint.X, c0.middlePoint.Y);
                                writer.LineTo(c0.leftExtendedPoint_Outer.X, c0.leftExtendedPoint_Outer.Y);
                                writer.LineTo(c0.leftExtendedPointDest_Outer.X, c0.leftExtendedPointDest_Outer.Y);
                                writer.LineTo(c1.middlePoint.X, c1.middlePoint.Y);
                                writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                writer.CloseFigure();
                                painter.Fill(v2, c0.OuterColor);
                            }
                            //                            

                            //inner
                            v2.Clear();
                            if (c0.MiddlePointKindIsTouchPoint && c0.RightPointKindIsTouchPoint)
                            {
                                writer.MoveTo(c0.leftExtendedPoint_Inner.X, c0.leftExtendedPoint_Inner.Y);
                                writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                writer.LineTo(c1.middlePoint.X, c1.middlePoint.Y);
                                writer.LineTo(c1.rightExtendedPoint_Inner.X, c1.rightExtendedPoint_Inner.Y);
                                writer.LineTo(c0.leftExtendedPoint_Inner.X, c0.leftExtendedPoint_Inner.Y);
                                writer.CloseFigure();
                                //
                                painter.Fill(v2, c0.InnerColor);
                            }

                            painter.CurrentBxtBlendOp = null;//**

                            //------------------
                            //outer gap
                            v2.Clear();
                            if (c0.MiddlePointKindIsTouchPoint && c0.RightPointKindIsTouchPoint)
                            {
                                writer.MoveTo(c0.leftExtendedPoint_Inner.X, c0.leftExtendedPoint_Inner.Y);
                                writer.LineTo(c0.rightExtendedPoint_Outer.X, c0.rightExtendedPoint_Outer.Y);
                                writer.LineTo(c0.leftExtendedPoint_Outer.X, c0.leftExtendedPoint_Outer.Y);
                                writer.LineTo(c0.rightExtendedPoint_Inner.X, c0.rightExtendedPoint_Inner.Y);
                                writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                writer.CloseFigure();
                                painter.Fill(v2, PixelFarm.Drawing.Color.Red);
                            }
                            //------------------ 
                        }
                    }

                    //DrawTessTriangles(polygon1, painter); 

                    bmpLut.SaveImage("d:\\WImageTest\\msdf_shape_lut2.png");

                    //
                    int[] lutBuffer = bmpLut.CopyImgBuffer(bmpLut.Width, bmpLut.Height);
                    //ExtMsdfgen.BmpEdgeLut bmpLut2 = new ExtMsdfgen.BmpEdgeLut(bmpLut.Width, bmpLut.Height, lutBuffer);


                    //bmpLut2 = null;
                    var bmp5 = MemBitmap.LoadBitmap("d:\\WImageTest\\msdf_shape_lut.png");

                    int[] lutBuffer5 = bmp5.CopyImgBuffer(bmpLut.Width, bmpLut.Height);
                    bmpLut7.SetBmpBuffer(bmpLut.Width, bmpLut.Height, lutBuffer5);

                    ExtMsdfgen.GlyphImage glyphImg = ExtMsdfgen.MsdfGlyphGen.CreateMsdfImage(shape1, msdfGenParams, bmpLut7);
                    //                     
                    using (Bitmap bmp3 = new Bitmap(glyphImg.Width, glyphImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        int[] buffer = glyphImg.GetImageBuffer();

                        var bmpdata = bmp3.LockBits(new System.Drawing.Rectangle(0, 0, glyphImg.Width, glyphImg.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp3.PixelFormat);
                        System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpdata.Scan0, buffer.Length);
                        bmp3.UnlockBits(bmpdata);
                        bmp3.Save("d:\\WImageTest\\msdf_shape.png");
                        //
                    }
                }
            }
        }
    }
}
