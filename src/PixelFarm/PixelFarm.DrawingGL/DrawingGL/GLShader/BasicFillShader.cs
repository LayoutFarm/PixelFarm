﻿//MIT, 2016-present, WinterDev

using System;
using OpenTK.Graphics.ES20;
namespace PixelFarm.DrawingGL
{
    sealed class BasicFillShader : ShaderBase
    {
        ShaderVtxAttrib2f a_position;
        ShaderUniformMatrix4 u_matrix;
        ShaderUniformVar4 u_solidColor;
        int _orthoviewVersion = -1;
        public BasicFillShader(ShaderSharedResource shareRes)
            : base(shareRes)
        {
            //NOTE: during development, 
            //new shader source may not recompile if you don't clear cache or disable cache feature
            //like...
            //EnableProgramBinaryCache = false;

            if (!LoadCompiledShader())
            {

                //vertex shader source
                string vs = @"        
                    attribute vec2 a_position; 
                    uniform mat4 u_mvpMatrix; 
                    void main()
                    {
                        gl_Position = u_mvpMatrix* vec4(a_position[0],a_position[1],0,1); 
                    }
                ";

                //fragment source
                string fs = @"
                    precision mediump float;
                    uniform vec4 u_solidColor;
                    void main()
                    {
                        gl_FragColor = u_solidColor;
                    }
                ";

                if (!_shaderProgram.Build(vs, fs))
                {
                    throw new NotSupportedException();
                }
                //
                SaveCompiledShader();
            }

            a_position = _shaderProgram.GetAttrV2f("a_position");
            u_matrix = _shaderProgram.GetUniformMat4("u_mvpMatrix");
            u_solidColor = _shaderProgram.GetUniform4("u_solidColor");
        }
        public void FillTriangleStripWithVertexBuffer(float[] linesBuffer, int nelements, Drawing.Color color)
        {
            SetCurrent();
            CheckViewMatrix();
            //--------------------------------------------

            u_solidColor.SetValue((float)color.R / 255f, (float)color.G / 255f, (float)color.B / 255f, (float)color.A / 255f);
            a_position.LoadPureV2f(linesBuffer);
            GL.DrawArrays(BeginMode.TriangleStrip, 0, nelements);
        }
        //--------------------------------------------

        void CheckViewMatrix()
        {
            int version = 0;
            if (_orthoviewVersion != (version = _shareRes.OrthoViewVersion))
            {
                _orthoviewVersion = version;
                u_matrix.SetData(_shareRes.OrthoView.data);
            }
        }
        //--------------------------------------------
        public void FillTriangles(float[] polygon2dVertices, int nelements, Drawing.Color color)
        {
            SetCurrent();
            CheckViewMatrix();
            //--------------------------------------------  

            u_solidColor.SetValue((float)color.R / 255f, (float)color.G / 255f, (float)color.B / 255f, (float)color.A / 255f);
            a_position.LoadPureV2f(polygon2dVertices);
            GL.DrawArrays(BeginMode.Triangles, 0, nelements);
        }
        public void FillTriangles(float[] polygon2dVertices, ushort[] indices, Drawing.Color color)
        {
            SetCurrent();
            CheckViewMatrix();
            //--------------------------------------------  

            u_solidColor.SetValue((float)color.R / 255f, (float)color.G / 255f, (float)color.B / 255f, (float)color.A / 255f);
            a_position.LoadPureV2f(polygon2dVertices);
            GL.DrawElements(BeginMode.Triangles, indices.Length, DrawElementsType.UnsignedShort, indices);
        }

        public void FillTriangles(int first, int count, Drawing.Color color)
        {
            SetCurrent();
            CheckViewMatrix();
            //--------------------------------------------    
            u_solidColor.SetValue((float)color.R / 255f, (float)color.G / 255f, (float)color.B / 255f, (float)color.A / 255f);

            //vbo.Bind();
            a_position.LoadLatest();
            //GL.DrawElements(BeginMode.Triangles, nelements, DrawElementsType.UnsignedShort, 0);
            GL.DrawArrays(BeginMode.Triangles, first, count);
            //vbo.UnBind(); //important, call unbind after finish call.
        }
        //public void FillTriangles(VBOPart vboPart, Drawing.Color color)
        //{
        //    SetCurrent();
        //    CheckViewMatrix();
        //    //--------------------------------------------  
        //    u_solidColor.SetValue((float)color.R / 255f, (float)color.G / 255f, (float)color.B / 255f, (float)color.A / 255f);

        //    //--------------------------------------------  
        //    //note (A):
        //    //from https://www.khronos.org/registry/OpenGL-Refpages/es2.0/xhtml/glVertexAttribPointer.xml
        //    //... If a non-zero named buffer object is bound to the GL_ARRAY_BUFFER target (see glBindBuffer)
        //    //while a generic vertex attribute array is specified,
        //    //pointer is treated as **a byte offset** into the buffer object's data store. 

        //    vboPart.vbo.Bind();
        //    a_position.LoadLatest(vboPart.partRange.beginVertexAt * 4); //*4 => see note (A) above, so offset => beginVertexAt * sizeof(float)
        //    GL.DrawElements(BeginMode.Triangles,
        //        vboPart.partRange.elemCount,
        //        DrawElementsType.UnsignedShort,
        //        vboPart.partRange.beginElemIndexAt * 2);  //*2 => see note (A) above, so offset=> beginElemIndexAt *sizeof(ushort)
        //    vboPart.vbo.UnBind();

        //}

        public unsafe void DrawLineLoopWithVertexBuffer(float* polygon2dVertices, int nelements, Drawing.Color color)
        {
            SetCurrent();
            CheckViewMatrix();
            //--------------------------------------------
            u_solidColor.SetValue((float)color.R / 255f, (float)color.G / 255f, (float)color.B / 255f, (float)color.A / 255f);
            a_position.UnsafeLoadPureV2f(polygon2dVertices);
            GL.DrawArrays(BeginMode.LineLoop, 0, nelements);
        }
        public unsafe void FillTriangleFan(float* polygon2dVertices, int nelements, Drawing.Color color)
        {
            SetCurrent();
            CheckViewMatrix();
            //--------------------------------------------

            u_solidColor.SetValue((float)color.R / 255f, (float)color.G / 255f, (float)color.B / 255f, (float)color.A / 255f);
            a_position.UnsafeLoadPureV2f(polygon2dVertices);
            GL.DrawArrays(BeginMode.TriangleFan, 0, nelements);
        }
        public void DrawLine(float x1, float y1, float x2, float y2, PixelFarm.Drawing.Color color)
        {
            SetCurrent();
            CheckViewMatrix();
            //--------------------------------------------

            u_solidColor.SetValue((float)color.R / 255f, (float)color.G / 255f, (float)color.B / 255f, (float)color.A / 255f);
            unsafe
            {
                float* vtx = stackalloc float[4];
                vtx[0] = x1; vtx[1] = y1;
                vtx[2] = x2; vtx[3] = y2;
                a_position.UnsafeLoadPureV2f(vtx);
            }
            GL.DrawArrays(BeginMode.LineStrip, 0, 2);
        }
    }
}