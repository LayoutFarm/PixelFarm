﻿//
// Copyright (c) 2014 The ANGLE Project Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.
//

//            Based on Hello_Triangle.c from
// Book:      OpenGL(R) ES 2.0 Programming Guide
// Authors:   Aaftab Munshi, Dan Ginsburg, Dave Shreiner
// ISBN-10:   0321502795
// ISBN-13:   9780321502797
// Publisher: Addison-Wesley Professional
// URLs:      http://safari.informit.com/9780321563835
//            http://www.opengles-book.com 

using System;
using OpenTK.Graphics.ES20;
using Mini;
namespace OpenTkEssTest
{
    [Info(OrderCode = "051", AvailableOn = AvailableOn.GLES)]
    [Info("T51_View2d")]
    public class T51_View2d : DemoBase
    {
        MiniShaderProgram shaderProgram;
        protected override void OnReadyForInitGLShaderProgram()
        {
            shaderProgram = new MiniShaderProgram();
            string vs = @"
                attribute vec3 a_position;
                attribute vec2 a_texCoord;
                varying vec2 v_texCoord;
                void main()
                {
                    gl_Position = vec4(a_position[0],a_position[1],0,1);
                    v_texCoord = a_texCoord;
                 }	 
                ";
            string fs = @"
                      precision mediump float;
                      varying vec2 v_texCoord;
                      uniform sampler2D s_texture;
                      void main()
                      {
                         gl_FragColor = texture2D(s_texture, v_texCoord);
                      }
                ";
            if (!shaderProgram.Build(vs, fs))
            {
            }

            //-------------------------------------------

            // Get the attribute locations
            a_position = shaderProgram.GetAttrV3f("a_position");// GL.GetAttribLocation(mProgram, "a_position");
            a_textCoord = shaderProgram.GetAttrV2f("a_texCoord");
            // Get the sampler location
            s_texture = shaderProgram.GetUniform1("s_texture");
            //// Load the texture
            mTexture = ES2Utils2.CreateSimpleTexture2D();
            GL.ClearColor(0, 0, 0, 0);
            //================================================================================

        }
        protected override void OnGLRender(object sender, EventArgs args)
        {
            float[] vertices = new float[] {
                    -0.5f,  0.5f, 0.0f,  // Position 0
                     0.0f,  0.0f,        // TexCoord 0
                    -0.5f, -0.5f, 0.0f,  // Position 1
                     0.0f,  1.0f,        // TexCoord 1
                     0.5f, -0.5f, 0.0f,  // Position 2
                     1.0f,  1.0f,        // TexCoord 2
                     0.5f,  0.5f, 0.0f,  // Position 3
                     1.0f,  0.0f         // TexCoord 3
                     };
            ushort[] indices = new ushort[] { 0, 1, 2, 0, 2, 3 };
            GL.Viewport(0, 0, this.Width, this.Height);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            shaderProgram.UseProgram();
            unsafe
            {
                fixed (float* head = &vertices[0])
                {
                    a_position.UnsafeLoadMixedV3f(head, 5);
                    a_textCoord.UnsafeLoadMixedV2f(head + 3, 5);
                }
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, mTexture);
            s_texture.SetValue(0);
            GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, indices);
            SwapBuffers();
        }
        protected override void DemoClosing()
        {
            shaderProgram.DeleteProgram();
            GL.DeleteTexture(mTexture);
            mTexture = 0;
        }

        // Attribute locations
        ShaderVtxAttrib3f a_position;
        ShaderVtxAttrib2f a_textCoord;
        // Sampler location
        ShaderUniformVar1 s_texture;
        // Texture handle
        int mTexture;
    }
}
