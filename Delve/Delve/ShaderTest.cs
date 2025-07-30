using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace Testing_Project
{

    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            //Content.RootDirectory = @"\Content";
        }

        GeometryShader _geometryShader;
        protected override void Initialize()
        {
            IsMouseVisible = true;

            var CompiledGS = ShaderBytecode.CompileFromFile(@"Content\Effects\WaterGS.hlsl", "MainGS", "gs_4_0");
            _geometryShader = new GeometryShader((Device)GraphicsDevice.Handle, CompiledGS.Bytecode);

            base.Initialize();
        }

        Effect _effect;
        VertexPositionColor[] _cubeVertices;
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            _cubeVertices = CreateCube();

            _effect = Content.Load<Effect>("Content/Effects/WaterShader");
            _effect.Parameters["LightColor"].SetValue(Color.Red.ToVector3());
            _effect.Parameters["LightDirection"].SetValue(Vector3.Normalize(-Vector3.UnitY + -Vector3.UnitZ));
            return;
        }

        protected override void UnloadContent()
        {
        }
        
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        void CopyCBuffers()
        {
            var buffers = ((DeviceContext)GraphicsDevice.ContextHandle).VertexShader.GetConstantBuffers(0, 8);
            if (buffers != null)
            {
                for (int i = 0; i < buffers.Length; ++i)
                    ((DeviceContext)GraphicsDevice.ContextHandle).GeometryShader.SetConstantBuffer(i, buffers[i]);
            }
        }

        float _rotation;
        protected override void Draw(GameTime gameTime)
        {
            _rotation += 1f;
            GraphicsDevice.Clear(Color.CornflowerBlue);

            GraphicsDevice.RasterizerState = Microsoft.Xna.Framework.Graphics.RasterizerState.CullNone;

            Matrix world = Matrix.CreateRotationY(MathHelper.ToRadians(_rotation)) * Matrix.CreateRotationX(MathHelper.ToRadians(_rotation / 4f));

            Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, -5), Vector3.Zero, Vector3.Up);

            Matrix projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                GraphicsDevice.Viewport.AspectRatio,
                0.1f,
                200f);

            Matrix wvp = world * view * projection;
            _effect.Parameters["WorldViewProjection"].SetValue(wvp);
            _effect.Parameters["ModelTransform"].SetValue(world);

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                ((DeviceContext)GraphicsDevice.ContextHandle).GeometryShader.Set(_geometryShader);
                CopyCBuffers();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _cubeVertices, 0, 12);
            }
            ((DeviceContext)GraphicsDevice.ContextHandle).GeometryShader.Set(null);

            base.Draw(gameTime);
        }

        // Cube code taken from https://github.com/jonashw/MonoGame-CubeTest/tree/master/MonoGameCubeTest
        public static VertexPositionColor[] CreateCube()
        {
            var face = new Vector3[6];
            face[0] = new Vector3(-1f, 01f, 0.0f); //TopLeft
            face[1] = new Vector3(-1f, -1f, 0.0f); //BottomLeft
            face[2] = new Vector3(01f, 01f, 0.0f); //TopRight
            face[3] = new Vector3(-1f, -1f, 0.0f); //BottomLeft
            face[4] = new Vector3(01f, -1f, 0.0f); //BottomRight
            face[5] = new Vector3(01f, 01f, 0.0f); //TopRight

            var textureCoords = new Vector2(0f, 0f);
            var vertices = new VertexPositionColor[36];

            //front face
            for (var i = 0; i <= 2; i++)
            {
                vertices[i] = new VertexPositionColor(
                    face[i] + Vector3.UnitZ,
                    Color.DarkGray);
                vertices[i + 3] = new VertexPositionColor(
                    face[i + 3] + Vector3.UnitZ,
                    Color.DarkGray);
            }

            //Back face
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 6] = new VertexPositionColor(
                    face[2 - i] - Vector3.UnitZ,
                    Color.DarkOrange);
                vertices[i + 6 + 3] = new VertexPositionColor(
                    face[5 - i] - Vector3.UnitZ,
                    Color.DarkOrange);
            }

            //left face
            var rotY90 = Matrix.CreateRotationY(-MathHelper.PiOver2);
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 12] = new VertexPositionColor(
                    Vector3.Transform(face[i], rotY90) - Vector3.UnitX,
                    Color.DarkGoldenrod);
                vertices[i + 12 + 3] = new VertexPositionColor(
                    Vector3.Transform(face[i + 3], rotY90) - Vector3.UnitX,
                    Color.DarkGoldenrod);
            }

            //Right face
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 18] = new VertexPositionColor(
                    Vector3.Transform(face[2 - i], rotY90) + Vector3.UnitX,
                    Color.DarkGreen);
                vertices[i + 18 + 3] = new VertexPositionColor(
                    Vector3.Transform(face[5 - i], rotY90) + Vector3.UnitX,
                    Color.DarkGreen);
            }

            //Top face
            var rotX90 = Matrix.CreateRotationX(-MathHelper.PiOver2);
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 24] = new VertexPositionColor(
                    Vector3.Transform(face[i], rotX90) + Vector3.UnitY,
                    Color.DarkKhaki);
                vertices[i + 24 + 3] = new VertexPositionColor(
                    Vector3.Transform(face[i + 3], rotX90) + Vector3.UnitY,
                    Color.DarkKhaki);
            }

            //Bottom face
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 30] = new VertexPositionColor(
                    Vector3.Transform(face[2 - i], rotX90) - Vector3.UnitY,
                    Color.Gold);
                vertices[i + 30 + 3] = new VertexPositionColor(
                    Vector3.Transform(face[5 - i], rotX90) - Vector3.UnitY,
                    Color.Gold);
            }

            return vertices;
        }
    }
}