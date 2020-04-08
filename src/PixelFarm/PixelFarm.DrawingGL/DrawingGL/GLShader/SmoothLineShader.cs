﻿//MIT, 2016-present, WinterDev
//we use concept from https://www.mapbox.com/blog/drawing-antialiased-lines/
//
using System;
using OpenTK.Graphics.ES20;
namespace PixelFarm.DrawingGL
{
    class SmoothLineShader : ColorFillShaderBase
    {
        ShaderVtxAttrib4f a_position;

        ShaderUniformVar4 u_solidColor;
        ShaderUniformVar1 u_linewidth;
        ShaderUniformVar1 u_p0;

        public SmoothLineShader(ShaderSharedResource shareRes)
            : base(shareRes)
        {
            //NOTE: during development, 
            //new shader source may not recompile if you don't clear cache or disable cache feature
            //like...
            //EnableProgramBinaryCache = false;

            if (!LoadCompiledShader())
            {
                //we may store this outside the exe ?

                //vertex shader source

                string vs = @"        
                    precision mediump float;
                    attribute vec4 a_position;  
                    uniform vec2 u_ortho_offset; 
                     
                    uniform mat4 u_mvpMatrix; 
                    uniform float u_linewidth;                  
                    varying float v_distance; 
                    varying vec2 v_dir; 
                    void main()
                    {                   
                        float rad = a_position[3];
                        v_distance= a_position[2]; 
                        vec2 delta;
                        if(v_distance <1.0){                                         
                            delta = vec2(-sin(rad) * u_linewidth,cos(rad) * u_linewidth) + u_ortho_offset;                       
                            v_dir = vec2(0.80,0.0);
                        }else{                      
                            delta = vec2(sin(rad) * u_linewidth,-cos(rad) * u_linewidth) + u_ortho_offset;
                            v_dir = vec2(0.0,0.80);  
                        } 
                        gl_Position = u_mvpMatrix*  vec4(a_position[0] +delta[0],a_position[1]+delta[1],0,1);
                    }
                ";



                //version3
                string fs = @"
                    precision mediump float;
                    uniform vec4 u_solidColor;
                    uniform float p0;
                    varying float v_distance;
                    varying vec2 v_dir;                      
                    void main()
                    {   
                         gl_FragColor =vec4(u_solidColor[0],u_solidColor[1],u_solidColor[2], 
                                            u_solidColor[3] *((v_distance* (v_dir[0])+ (1.0-v_distance)* (v_dir[1]))  * (1.0/p0)) * 0.55);  
                    }
                ";

                ////old version 2
                //string fs = @"
                //    precision mediump float;
                //    uniform vec4 u_solidColor;
                //    uniform float p0;
                //    varying float v_distance;                    
                //    void main()
                //    {      
                //        if(v_distance < p0){                        
                //            gl_FragColor =vec4(u_solidColor[0],u_solidColor[1],u_solidColor[2], u_solidColor[3] *(v_distance * (1.0/p0)) * 0.55);
                //        }else{           
                //            gl_FragColor =vec4(u_solidColor[0],u_solidColor[1],u_solidColor[2], u_solidColor[3] *((1.0-v_distance) * (1.0/p0)) * 0.55);
                //        } 
                //    }
                //";
                ////old version 1
                //string fs = @"
                //    precision mediump float;
                //    uniform vec4 u_solidColor;
                //    uniform float p0;
                //    varying float v_distance;                    
                //    void main()
                //    {       

                //        if(v_distance < p0){                        
                //            gl_FragColor =vec4(u_solidColor[0],u_solidColor[1],u_solidColor[2], u_solidColor[3] *(v_distance * (1.0/p0)) * 0.55);
                //        }else if(v_distance >= (1.0-p0)){           
                //            gl_FragColor =vec4(u_solidColor[0],u_solidColor[1],u_solidColor[2], u_solidColor[3] *((1.0-v_distance) * (1.0/p0)) * 0.55);
                //        }else{
                //            gl_FragColor = u_solidColor;
                //        }
                //    }
                //";


                //---------------------
                if (!_shaderProgram.Build(vs, fs))
                {
                    return;
                }
                //
                SaveCompiledShader();
            }


            //-----------------------
            a_position = _shaderProgram.GetAttrV4f("a_position");
            u_ortho_offset = _shaderProgram.GetUniform2("u_ortho_offset");
            u_matrix = _shaderProgram.GetUniformMat4("u_mvpMatrix");
            u_solidColor = _shaderProgram.GetUniform4("u_solidColor");
            u_linewidth = _shaderProgram.GetUniform1("u_linewidth");
            u_p0 = _shaderProgram.GetUniform1("p0");
        }

        static float GetCutPoint(float half_w)
        {
            if (half_w <= 0.5)
            {
                return 0.5f;
            }
            else if (half_w <= 1.0)
            {
                return 0.475f;
            }
            else if (half_w > 1.0 && half_w < 3.0)
            {
                return 0.25f;
            }
            else
            {
                return 0.1f;
            }

        }
        public void DrawLine(float x1, float y1, float x2, float y2)
        {
            //float dx = x2 - x1;
            //float dy = y2 - y1; 
            SetCurrent();
            CheckViewMatrix();
            //--------------------
            _shareRes.AssignStrokeColorToVar(u_solidColor);
            unsafe
            {
                float rad1 = (float)Math.Atan2(
                  y2 - y1,  //dy
                  x2 - x1); //dx
                //float[] vtxs = new float[] {
                //    x1, y1,0,rad1,
                //    x1, y1,1,rad1,
                //    x2, y2,0,rad1,
                //    //-------
                //    x2, y2,1,rad1
                //}; 
                //-------------------- 
                float* vtxs = stackalloc float[4 * 4];
                vtxs[0] = x1; vtxs[1] = y1; vtxs[2] = 0; vtxs[3] = rad1;
                vtxs[4] = x1; vtxs[5] = y1; vtxs[6] = 1; vtxs[7] = rad1;
                vtxs[8] = x2; vtxs[9] = y2; vtxs[10] = 0; vtxs[11] = rad1;
                vtxs[12] = x2; vtxs[13] = y2; vtxs[14] = 1; vtxs[15] = rad1;
                a_position.LoadPureV4fUnsafe(vtxs);
            }

            //because original stroke width is the width of both side of
            //the line, but u_linewidth is the half of the strokeWidth
            float half_w = _shareRes._strokeWidth / 2f;
            u_linewidth.SetValue(half_w);
            //u_p0.SetValue((1 / GetCutPoint(half_w)) * 0.55f);
            u_p0.SetValue(GetCutPoint(half_w));
            GL.DrawArrays(BeginMode.TriangleStrip, 0, 4);
        }
        public void DrawTriangleStrips(float[] coords, int ncount)
        {
            SetCurrent();
            CheckViewMatrix();

            _shareRes.AssignStrokeColorToVar(u_solidColor);
            float half_w = 1.5f / 2f;
            u_linewidth.SetValue(half_w);
            u_p0.SetValue(GetCutPoint(half_w));
            //u_p0.SetValue((1 / GetCutPoint(half_w)) * 0.55f);
            //
            a_position.LoadPureV4f(coords);
            //because original stroke width is the width of both side of
            //the line, but u_linewidth is the half of the strokeWidth            
            GL.DrawArrays(BeginMode.TriangleStrip, 0, ncount);
        }
        public void DrawTriangleStrips(int startAt, int ncount)
        {
            SetCurrent();
            CheckViewMatrix();
            _shareRes.AssignStrokeColorToVar(u_solidColor);
            float half_w = 1.5f / 2f;
            u_linewidth.SetValue(half_w);
            u_p0.SetValue(GetCutPoint(half_w));
            //u_p0.SetValue((1 / GetCutPoint(half_w)) * 0.55f);
            //
            a_position.LoadLatest();
            //because original stroke width is the width of both side of
            //the line, but u_linewidth is the half of the strokeWidth            
            GL.DrawArrays(BeginMode.TriangleStrip, startAt, ncount);
        }
    }
    


    class InvertAlphaLineSmoothShader : ShaderBase
    {
        //for stencil buffer ***
        ShaderVtxAttrib4f a_position;
        ShaderUniformMatrix4 u_matrix;
        ShaderUniformVar4 u_solidColor;
        ShaderUniformVar1 u_linewidth;
        Drawing.Color _strokeColor;
        float _strokeWidth = 0.5f;
        int _orthoviewVersion = -1;
        public InvertAlphaLineSmoothShader(ShaderSharedResource shareRes)
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
                attribute vec4 a_position;    

                uniform mat4 u_mvpMatrix;
                uniform vec4 u_solidColor; 
                uniform float u_linewidth;

                varying vec4 v_color; 
                varying float v_distance;
                varying float p0;
            
                void main()
                {   
                
                    float rad = a_position[3];
                    v_distance= a_position[2];

                    float n_x = sin(rad); 
                    float n_y = cos(rad);  

                    vec4 delta;
                    if(v_distance <1.0){                                         
                        delta = vec4(-n_x * u_linewidth,n_y * u_linewidth,0,0);                       
                    }else{                      
                        delta = vec4(n_x * u_linewidth,-n_y * u_linewidth,0,0);
                    }
    
                    if(u_linewidth <= 0.5){
                        p0 = 0.5;      
                    }else if(u_linewidth <=1.0){
                        p0 = 0.45;  
                    }else if(u_linewidth>1.0 && u_linewidth<3.0){
                    
                        p0 = 0.25;  
                    }else{
                        p0= 0.1;
                    }
                
                    vec4 pos = vec4(a_position[0],a_position[1],0,1) + delta;                 
                    gl_Position = u_mvpMatrix* pos;                

                    v_color= u_solidColor;
                }
                ";
                //fragment source
                //this is invert fragment shader *** 
                //so we 
                string fs = @"
                    precision mediump float;
                    varying vec4 v_color;  
                    varying float v_distance;
                    varying float p0;                
                    void main()
                    {
                        float d0= v_distance; 
                        float p1= 1.0-p0;
                        float factor= 1.0 /p0;
            
                        if(d0 < p0){                        
                            gl_FragColor = vec4(v_color[0],v_color[1],v_color[2], 1.0 -(v_color[3] *(d0 * factor)));
                        }else if(d0> p1){                         
                            gl_FragColor= vec4(v_color[0],v_color[1],v_color[2],1.0-(v_color[3] *((1.0-d0)* factor)));
                        }
                        else{ 
                           gl_FragColor = vec4(0,0,0,0);                        
                        } 
                    }
                ";
                //---------------------
                if (!_shaderProgram.Build(vs, fs))
                {
                    return;
                }

                //-----------------------
                SaveCompiledShader();
            }


            a_position = _shaderProgram.GetAttrV4f("a_position");
            u_matrix = _shaderProgram.GetUniformMat4("u_mvpMatrix");
            u_solidColor = _shaderProgram.GetUniform4("u_solidColor");
            u_linewidth = _shaderProgram.GetUniform1("u_linewidth");
            _strokeColor = Drawing.Color.Black;
        }

        void CheckViewMatrix()
        {
            int version = 0;
            if (_orthoviewVersion != (version = _shareRes.OrthoViewVersion))
            {
                _orthoviewVersion = version;
                u_matrix.SetData(_shareRes.OrthoView.data);
            }
        }

        public void DrawTriangleStrips(float[] coords, int ncount)
        {
            SetCurrent();
            CheckViewMatrix();
            //-----------------------------------
            u_solidColor.SetValue(
                  _strokeColor.R / 255f,
                  _strokeColor.G / 255f,
                  _strokeColor.B / 255f,
                  _strokeColor.A / 255f);
            a_position.LoadPureV4f(coords);
            u_linewidth.SetValue(_strokeWidth);
            GL.DrawArrays(BeginMode.TriangleStrip, 0, ncount);
        }
        public void DrawTriangleStrips(int startAt, int ncount)
        {
            SetCurrent();
            CheckViewMatrix();

            _shareRes.AssignStrokeColorToVar(u_solidColor);
            u_linewidth.SetValue(1.0f / 2f);
            //
            a_position.LoadLatest();
            //because original stroke width is the width of both side of
            //the line, but u_linewidth is the half of the strokeWidth            
            GL.DrawArrays(BeginMode.TriangleStrip, startAt, ncount);
        }
    }

}