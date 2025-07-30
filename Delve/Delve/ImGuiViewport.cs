using ImGuiCLI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.ComponentModel;
using System.Collections.Generic;
using System;
using DelveLib;
using ImGuiControls;

namespace ImGuiCLITest
{
    public class TreeSelection : ImGuiControls.GenericObjectTree.ISelection
    {
        HashSet<object> selection = new HashSet<object>();
        public void Deselect(object obj)
        {
            selection.Remove(obj);
        }

        public void Drop(object onto, string key)
        {
            
        }

        public bool IsSelected(object obj)
        {
            return selection.Contains(obj);
        }

        public void Select(object obj, bool additive)
        {
            if (!additive)
                selection.Clear();
            if (!selection.Contains(obj))
                selection.Add(obj);
        }
    }
    public enum TestEnum
    {
        ValueA,
        ValueB,
        ValueC
    }
    public class TestData
    {
        [Description("A boolean value")]
        public bool TestBool { get; set; }
        [Category("Basic")]
        public int TestInt { get; set; }
        [Category("Basic")]
        public string TestString { get; set; }
        [Category("Basic")]
        [Description("Demonstrates a Vector2 in action")]
        public Vector2 TestVec2 { get; set; } = new Vector2();
        public Vector3 TestVec3 { get; set; } = new Vector3();
        public Vector4 TestVec4 { get; set; } = new Vector4();
        public Color Color{ get; set; } = new Color();
        [Editor("Color", "")]
        public Vector4 ColorVec { get; set; } = new Vector4();
        public TestEnum EnumValue { get; set; } = TestEnum.ValueA;
        public Quaternion Quat { get; set; } = Quaternion.Identity;

        [PropertyData.PropertyIgnore(EditorSpecific = "DataGrid")]
        [PropertyData.PropertyIgnore(EditorSpecific = "GenericObjectTree")]
        public Vector2[] ArrayTest { get; set; } = new Vector2[1];

        [Description("Collection of children in the object tree")]
        [PropertyData.PropertyIgnore(EditorSpecific = "DataGrid")]
        public List<TestData> Children { get; set; } = new List<TestData>();

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public ResponseCurve Curve = new ResponseCurve();
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public Curve RegularCurve = new Curve();
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [Editor("Transform", "")]
        public Matrix Transform { get; set; } = Matrix.Identity;

        public TestData()
        {
            for (int i = 0; i < 3; ++i)
                Children.Add(new ImGuiCLITest.TestData(1));

            RegularCurve.Keys.Add(new CurveKey(0, 0));
            RegularCurve.Keys.Add(new CurveKey(0.2f, 0.4f));
            RegularCurve.Keys.Add(new CurveKey(0.4f, 0.2f));
            RegularCurve.Keys.Add(new CurveKey(1, 1));
            RegularCurve.ComputeTangents(CurveTangent.Smooth);
        }

        public TestData(int depth)
        {
            if (depth < 3)
                for (int i = 0; i < 3; ++i)
                    Children.Add(new ImGuiCLITest.TestData(depth + 1));
        }
    }

    public class GradientDrawer : PropertyData.ImPropertyHandler
    {
        public bool RequiresLabel { get { return true; } }

        public void EmitUI(ReflectionCache.CachedPropertyInfo propertyInfo, object targetObject)
        {
            Curve obj = propertyInfo.GetValue(targetObject) as Curve;
            if (Delve.ImAids.DrawGradient(ref obj))
                propertyInfo.SetValue(targetObject, obj);
        }

        public string GenerateCode(string targetName, string accessorName, bool contextRequestsLabel) { throw new NotImplementedException(); }
    }

    public class ResponseCurveDrawer : PropertyData.ImPropertyHandler
    {
        public bool RequiresLabel { get { return true; } }

        public void EmitUI(ReflectionCache.CachedPropertyInfo propertyInfo, object targetObject)
        {
            ResponseCurve obj = propertyInfo.GetValue(targetObject) as ResponseCurve;
            if (Delve.ImAids.DrawResponseCurve(ref obj))
                propertyInfo.SetValue(targetObject, obj);
        }

        public string GenerateCode(string targetName, string accessorName, bool contextRequestsLabel) { throw new NotImplementedException(); }
    }

    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class ImGuiViewport : Game
    {
        TestData data = new TestData();
        TestData[] datas = new[]
        {
            new TestData(),new TestData(),new TestData(),new TestData(),
            new TestData(),new TestData(),new TestData(),new TestData(),
            new TestData(),new TestData(),new TestData(),new TestData(),
            new TestData(),new TestData(),new TestData(),new TestData(),
        };
        ImGuiContext imguiContext_;
        GraphicsDeviceManager graphics;
        ImGuiControls.FileBrowser browser_;
        ImGuiControls.Inspector inspector_;
        ImGuiControls.InspectorGrid grid_;
        ImGuiControls.GenericObjectTree tree_;
        TextEditor editor_;
        DebugDraw debugDraw_;

        bool IsFocused
        {
            get
            {
                return ((MonoGame.Framework.WinFormsGameWindow)Window).Form.Focused;
            }
        }

        public ImGuiViewport()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";
            Window.Title = "MonoGame ImGui-Viewport";
            Window.AllowUserResizing = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        public const int WM_MOUSEHWHEEL = 0x020E;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            IsMouseVisible = true;
            base.Initialize();
            //	imguiInit();						// I'll init imgui while running (note: if initing in 
            // Initialize, the Depth bug also happens

            debugDraw_ = new DebugDraw(GraphicsDevice);
        }


        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here        
            imguiContext_?.Shutdown();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                ImGuiDock.SaveDock();
                Exit();
            }
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        /// 
        static int scrollValue = 0;
        static bool someValue = false;
        static MouseCursor lastCursor = MouseCursor.Arrow;

        bool imguiShowing = false;
        bool imguiInited = false;

        protected override void Draw(GameTime gameTime)
        {

            // click mouse to start showing
            if (Mouse.GetState().LeftButton == ButtonState.Pressed) imguiShowing = true;

            GraphicsDevice.Clear(Color.Red);
            myRender();

            debugDraw_.Begin(Matrix.CreateLookAt(new Vector3(0, 0, -5), Vector3.Zero, Vector3.Up), Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                GraphicsDevice.Viewport.AspectRatio,
                0.1f,
                200f));

            debugDraw_.DrawLine(Vector3.Zero, Vector3.UnitX * 100, Color.DarkRed);
            debugDraw_.DrawLine(Vector3.Zero, Vector3.UnitY * 100, Color.Green);
            debugDraw_.DrawLine(Vector3.Zero, Vector3.UnitZ * 100, Color.Cyan);
            debugDraw_.End();

            // init and show imgui , if desired
            if (imguiShowing)
            {
                if (!imguiInited) { imguiInited = true; imguiInit(); }
                imguiDraw(gameTime);
            }

            base.Draw(gameTime);

        }

        #region IMGUI init & draw

        void imguiInit()
        {
            System.IntPtr device = ((SharpDX.Direct3D11.Device)GraphicsDevice.Handle).NativePointer;
            System.IntPtr deviceContext = ((SharpDX.Direct3D11.DeviceContext)GraphicsDevice.ContextHandle).NativePointer;
            System.IntPtr backBuffer = ((SharpDX.Direct3D11.RenderTargetView)GraphicsDevice.BackBuffer).NativePointer;

            var form = ((MonoGame.Framework.WinFormsGameWindow)Window).Form;
            form.SignalNativeMessages.Add(WM_MOUSEHWHEEL);
            form.SignalNativeMessages.Add(WM_KEYDOWN);
            form.SignalNativeMessages.Add(WM_KEYUP);
            form.SignalNativeMessages.Add(WM_CHAR);
            form.NotifyMessage += (o, e) =>
            {
                if (e.Msg == WM_KEYDOWN)
                    ImGuiIO.SetKeyState(e.WParam.ToInt32(), true);
                else if (e.Msg == WM_KEYUP)
                    ImGuiIO.SetKeyState(e.WParam.ToInt32(), false);
                else if (e.Msg == WM_CHAR && e.WParam.ToInt64() > 0 && e.WParam.ToInt64() < 0x10000)
                    ImGuiIO.AddText((ushort)e.WParam.ToInt64());
            };
            imguiContext_ = new ImGuiContext(this.Window.Handle, device, deviceContext, backBuffer);

            browser_ = new ImGuiControls.FileBrowser(GraphicsDevice);
            var reflector = new ImGuiControls.ReflectionCache(true, true);
            reflector.TypeHandlers.Add(typeof(ResponseCurve), new ResponseCurveDrawer());
            reflector.TypeHandlers.Add(typeof(Curve), new GradientDrawer());
            inspector_ = new ImGuiControls.Inspector(reflector);
            grid_ = new ImGuiControls.InspectorGrid(reflector);
            editor_ = new TextEditor();
            editor_.Text = System.IO.File.ReadAllText("C:\\dev\\Git\\Delve\\Delve\\Inspector.cs");
            tree_ = new ImGuiControls.GenericObjectTree(reflector);
            tree_.ContextMenu = (object o) =>
            {
                if (ImGuiCli.MenuItem("Set name"))
                    (o as TestData).TestString = "context menu set this!";
                if (ImGuiCli.MenuItem("Clear Name"))
                    (o as TestData).TestString = "";
                if (ImGuiCli.MenuItem("Show in inspector"))
                    inspector_.Inspecting = o;
            };
            tree_.DragConverter = (object o) =>
            {
                TestData d = o as TestData;
                if (!string.IsNullOrWhiteSpace(d.TestString))
                    return d.TestString;
                return "< unnamed >";
            };
            tree_.StringConverter = (object o) => 
            {
                TestData d = o as TestData;
                if (d == null && o != null)
                    return o.ToString();
                if (!string.IsNullOrWhiteSpace(d.TestString))
                    return d.TestString;
                return "< unnamed >";
            };
            tree_.Selection = new TreeSelection();
            tree_.RootObjects.Add(data);

            ImGuiDock.LoadDock();
        }
        
        void imguiDraw(GameTime gameTime)
        {
            //GraphicsDevice.Clear(Color.CornflowerBlue);
            //GraphicsDevice.Viewport = new Viewport(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height, 0, 1);

            float td = gameTime.ElapsedGameTime.Milliseconds / 1000.0f;
            ImGuiIO.DeltaTime = td;
            var mousePos = Mouse.GetState().Position;
            int newScroll = Mouse.GetState().ScrollWheelValue;
            int scrollDelta = newScroll - scrollValue;
            scrollValue = newScroll;
            if (IsFocused)
                ImGuiIO.SetMouseWheel(scrollDelta / 120.0f);

            ImGuiIO.SetMouseButton(0, Mouse.GetState().LeftButton == ButtonState.Pressed);
            ImGuiIO.SetMouseButton(1, Mouse.GetState().RightButton == ButtonState.Pressed);
            ImGuiIO.SetMouseButton(2, Mouse.GetState().MiddleButton == ButtonState.Pressed);
            switch (ImGuiIO.MouseCursor)
            {
            case ImGuiMouseCursor_.None:
                break;
            case ImGuiMouseCursor_.Arrow:
                if (lastCursor != MouseCursor.Arrow)
                    Mouse.SetCursor(MouseCursor.Arrow);
                lastCursor = MouseCursor.Arrow;
                break;
            case ImGuiMouseCursor_.ResizeAll:
                if (lastCursor != MouseCursor.SizeAll)
                    Mouse.SetCursor(MouseCursor.SizeAll);
                lastCursor = MouseCursor.SizeAll;
                break;
            case ImGuiMouseCursor_.ResizeEW:
                if (lastCursor != MouseCursor.SizeWE)
                    Mouse.SetCursor(MouseCursor.SizeWE);
                lastCursor = MouseCursor.SizeWE;
                break;
            case ImGuiMouseCursor_.ResizeNS:
                if (lastCursor != MouseCursor.SizeNS)
                    Mouse.SetCursor(MouseCursor.SizeNS);
                lastCursor = MouseCursor.SizeNS;
                break;
            case ImGuiMouseCursor_.ResizeNESW:
                if (lastCursor != MouseCursor.SizeNESW)
                    Mouse.SetCursor(MouseCursor.SizeNESW);
                lastCursor = MouseCursor.SizeNESW;
                break;
            case ImGuiMouseCursor_.ResizeNWSE:
                if (lastCursor != MouseCursor.SizeNWSE)
                    Mouse.SetCursor(MouseCursor.SizeNWSE);
                lastCursor = MouseCursor.SizeNWSE;
                break;
            case ImGuiMouseCursor_.TextInput:
                if (lastCursor != MouseCursor.IBeam)
                    Mouse.SetCursor(MouseCursor.IBeam);
                lastCursor = MouseCursor.IBeam;
                break;
            }
            imguiContext_.NewFrame(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            ImGuiDock.RootDock(new Vector2(Window.ClientBounds.Left, Window.ClientBounds.Top), new Vector2(Window.ClientBounds.Width, Window.ClientBounds.Height));

            browser_.DrawAsDock(ICON_FA.DESKTOP + " Asset Browser");

            inspector_.DrawAsDock(ICON_FA.LIST + " Inspector");

            grid_.Objects = datas;
            grid_.DrawAsDock(ICON_FA.TABLE + " Data Grid");

            tree_.DrawAsDock(ICON_FA.TREE + " Tree View");
            //if (ImGuiDock.BeginDock("Text Editor", ImGuiWindowFlags_.NoScrollbar))
            //{
            //    ImGuiEx.PushMonoFont();
            //    editor_.Render("##editor", ImGuiCli.GetWindowSize() - ImGuiStyle.FramePadding*8, true);
            //    ImGuiEx.PopFont();
            //}
            //ImGuiDock.EndDock();

            ImGuiCli.ShowMetricsWindow();

            imguiContext_.RenderAndDraw(((SharpDX.Direct3D11.RenderTargetView)GraphicsDevice.BackBuffer).NativePointer);

            GraphicsDevice.Textures[0] = null;
            GraphicsDevice.SetRenderTarget(null);
        }
        
        #endregion

        #region Cube render code

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // create checkered 
            Texture2D tex = new Texture2D(GraphicsDevice, 2, 2);
            Color[] data = new Color[4];
            data[0] = Color.White;
            data[1] = Color.Black;
            data[2] = Color.Black;
            data[3] = Color.White;
            tex.SetData(data);

            _basicEffect = new BasicEffect(GraphicsDevice)
            {
                AmbientLightColor = Vector3.One,
                LightingEnabled = true,
                DiffuseColor = Vector3.One,
                TextureEnabled = true,
                Texture = tex
            };
            _cubeVertices = CreateCube();
        }



        private BasicEffect _basicEffect;
        private VertexPositionNormalTexture[] _cubeVertices;
        float _rotation;


        void myRender()
        {
            _rotation += 1f;

            //GraphicsDevice.Clear(Color.Red);
            GraphicsDevice.Viewport = new Viewport(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 1);
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            _basicEffect.World = Matrix.CreateRotationY(MathHelper.ToRadians(_rotation))
                                    //*Matrix.CreateTranslation(_modelPosition)
                                    * Matrix.CreateRotationX(MathHelper.ToRadians(_rotation / 4f));

            _basicEffect.View = Matrix.CreateLookAt(new Vector3(0, 0, -5), Vector3.Zero, Vector3.Up);

            _basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                GraphicsDevice.Viewport.AspectRatio,
                0.1f,
                200f);

            _basicEffect.EnableDefaultLighting();

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _cubeVertices, 0, 12);
            }
        }

        // Cube code taken from https://github.com/jonashw/MonoGame-CubeTest/tree/master/MonoGameCubeTest
        public static VertexPositionNormalTexture[] CreateCube()
        {
            var face = new Vector3[6];
            face[0] = new Vector3(-1f, 01f, 0.0f); //TopLeft
            face[1] = new Vector3(-1f, -1f, 0.0f); //BottomLeft
            face[2] = new Vector3(01f, 01f, 0.0f); //TopRight
            face[3] = new Vector3(-1f, -1f, 0.0f); //BottomLeft
            face[4] = new Vector3(01f, -1f, 0.0f); //BottomRight
            face[5] = new Vector3(01f, 01f, 0.0f); //TopRight

            var textureCoords = new Vector2(0f, 0f);
            var vertices = new VertexPositionNormalTexture[36];

            //front face
            for (var i = 0; i <= 2; i++)
            {
                vertices[i] = new VertexPositionNormalTexture(
                    face[i] + Vector3.UnitZ,
                    Vector3.UnitZ, textureCoords);
                vertices[i + 3] = new VertexPositionNormalTexture(
                    face[i + 3] + Vector3.UnitZ,
                    Vector3.UnitZ, textureCoords);
            }

            vertices[0].TextureCoordinate = new Vector2(0, 0);//Top Left
            vertices[1].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[2].TextureCoordinate = new Vector2(1, 0);//Top Right
            vertices[3].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[4].TextureCoordinate = new Vector2(1, 1);//Bottom Right
            vertices[5].TextureCoordinate = new Vector2(1, 0);//TopRight

            //Back face
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 6] = new VertexPositionNormalTexture(
                    face[2 - i] - Vector3.UnitZ,
                    -Vector3.UnitZ, textureCoords);
                vertices[i + 6 + 3] = new VertexPositionNormalTexture(
                    face[5 - i] - Vector3.UnitZ,
                    -Vector3.UnitZ, textureCoords);
            }

            vertices[6].TextureCoordinate = new Vector2(1, 0);//Top Right
            vertices[7].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[8].TextureCoordinate = new Vector2(0, 0);//Top Left
            vertices[9].TextureCoordinate = new Vector2(1, 0);//TopRight
            vertices[10].TextureCoordinate = new Vector2(1, 1);//Bottom Right
            vertices[11].TextureCoordinate = new Vector2(0, 1);//Bottom Left

            //left face
            var rotY90 = Matrix.CreateRotationY(-MathHelper.PiOver2);
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 12] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[i], rotY90) - Vector3.UnitX,
                    -Vector3.UnitX, textureCoords);
                vertices[i + 12 + 3] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[i + 3], rotY90) - Vector3.UnitX,
                    -Vector3.UnitX, textureCoords);
            }

            vertices[14].TextureCoordinate = new Vector2(1, 0);//Top Right
            vertices[13].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[12].TextureCoordinate = new Vector2(0, 0);//Top Left
            vertices[15].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[16].TextureCoordinate = new Vector2(1, 1);//Bottom Right
            vertices[17].TextureCoordinate = new Vector2(1, 0);//TopRight

            //Right face
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 18] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[2 - i], rotY90) + Vector3.UnitX,
                    Vector3.UnitX, textureCoords);
                vertices[i + 18 + 3] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[5 - i], rotY90) + Vector3.UnitX,
                    Vector3.UnitX, textureCoords);
            }

            vertices[18].TextureCoordinate = new Vector2(1, 0);//Top Right
            vertices[19].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[20].TextureCoordinate = new Vector2(0, 0);//Top Left
            vertices[21].TextureCoordinate = new Vector2(1, 0);//TopRight
            vertices[22].TextureCoordinate = new Vector2(1, 1);//Bottom Right
            vertices[23].TextureCoordinate = new Vector2(0, 1);//Bottom Left

            //Top face
            var rotX90 = Matrix.CreateRotationX(-MathHelper.PiOver2);
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 24] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[i], rotX90) + Vector3.UnitY,
                    Vector3.UnitY, textureCoords);
                vertices[i + 24 + 3] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[i + 3], rotX90) + Vector3.UnitY,
                    Vector3.UnitY, textureCoords);
            }

            vertices[26].TextureCoordinate = new Vector2(1, 0);//Top Right
            vertices[25].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[24].TextureCoordinate = new Vector2(0, 0);//Top Left
            vertices[27].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[28].TextureCoordinate = new Vector2(1, 1);//Bottom Right
            vertices[29].TextureCoordinate = new Vector2(1, 0);//TopRight

            //Bottom face
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 30] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[2 - i], rotX90) - Vector3.UnitY,
                    -Vector3.UnitY, textureCoords);
                vertices[i + 30 + 3] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[5 - i], rotX90) - Vector3.UnitY,
                    -Vector3.UnitY, textureCoords);
            }

            vertices[30].TextureCoordinate = new Vector2(1, 0);//Top Right
            vertices[31].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[32].TextureCoordinate = new Vector2(0, 0);//Top Left
            vertices[33].TextureCoordinate = new Vector2(1, 0);//TopRight
            vertices[34].TextureCoordinate = new Vector2(1, 1);//Bottom Right
            vertices[35].TextureCoordinate = new Vector2(0, 1);//Bottom Left

            return vertices;
        }

        #endregion Cube render code

    }
}