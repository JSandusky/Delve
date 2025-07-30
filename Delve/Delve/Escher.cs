using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DelveLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Delve
{
    public class Escher : Game
    {
        GraphicsDeviceManager graphics;
        protected Objects.GameState gameState_ = new Objects.GameState();
        protected ViewportSettings viewportSettings_ = new ViewportSettings();
        protected ApplicationState appState_ = new ApplicationState();
        protected ExtBasicEffect basicEffect_;
        protected Graphics.PBREffect pbrEffect_;
        protected SpriteBatch spriteBatch_;
        protected DebugDraw debugDraw_;
        protected DebugMesh debugMesh_;
        protected DebugRenderer debugRenderer_;
        protected Texture2D white_;
        protected Texture2D black_;
        protected TextureCube ibl_;
        protected Graphics.MeshBatch meshBatch_;
        protected bool drawDebug_ = true;
        protected bool postProcess_ = false;
        protected MRT renderTargets_;
        protected SSAO ssaoRenderer_;

        public DebugDraw DebugDraw { get { return debugDraw_; } }
        public DebugMesh DebugMesh { get { return debugMesh_; } }
        public DebugRenderer DebugRenderer { get { return debugRenderer_; } }

        public Escher()
        {
            DelveLib.EC.Test eventTest = new DelveLib.EC.Test();
            DelveLib.EC.TestOther receiveTest = new DelveLib.EC.TestOther();
            receiveTest.Link(eventTest);
            eventTest.Stuff();
            receiveTest.Unsubscribe();

            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            IsFixedTimeStep = false;
            graphics.SynchronizeWithVerticalRetrace = false;
            Content.RootDirectory = "Content";
            Window.Title = "MCE";
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += Window_ClientSizeChanged;

            IOCLite.Register(this);
            IOCLite.Register(gameState_);
            IOCLite.Register(appState_);
            IOCLite.Register(viewportSettings_);
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            
        }

        protected override void Initialize()
        {
            IsMouseVisible = false;
            base.Initialize();
            gameState_.EditorCamera = new Camera(GraphicsDevice, 70);
            gameState_.EditorCamera.Position = -Vector3.UnitZ * 4;
            gameState_.EditorCamera.LookAtPoint(Vector3.Zero);

            graphics.PreferredBackBufferWidth = 1280;  // set this value to the desired width of your window
            graphics.PreferredBackBufferHeight = 720;   // set this value to the desired height of your window
            graphics.ApplyChanges();
        }

        protected override void LoadContent()
        {
            Graphics.SLATextureRenderer texRen = new Graphics.SLATextureRenderer(GraphicsDevice);

            /*
            using (System.IO.Stream str = Content.OpenStream_Explicit("Doom2_MAP01.xml"))
            {
                DelveLib.Map.Map map = new DelveLib.Map.Map();
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.Load(str);
                map.Read(doc.DocumentElement);

                var img = map.Render(1024);
                img.Save("TestDoom.png", System.Drawing.Imaging.ImageFormat.Png);
                img.Dispose();

                DelveLib.Map.MapProcessor mp = new DelveLib.Map.MapProcessor();

                List<Vector2> colPts = new List<Vector2>();
                colPts.Add(new Vector2(40, 60));
                colPts.Add(new Vector2(20, 55));
                colPts.Add(new Vector2(20, 30));
                colPts.Add(new Vector2(40, 30));
                colPts.Add(new Vector2(40, 0));

                List<Vector2> loftPts = new List<Vector2>();
                loftPts.Add(new Vector2(20, 100));
                loftPts.Add(new Vector2(0, 90));

                loftPts.Add(new Vector2(0, 45));
                loftPts.Add(new Vector2(10, 45));
                loftPts.Add(new Vector2(10, 35));
                loftPts.Add(new Vector2(0, 35));

                loftPts.Add(new Vector2(0, 30));
                loftPts.Add(new Vector2(20, 30));
                loftPts.Add(new Vector2(20,0));

                List<g3.DMesh3> meshes = new List<g3.DMesh3>();
                //foreach (var s in map.Sectors)
                //{
                //    var result = mp.LoftSector(s, DelveLib.Map.MapProcessor.FitCurveToWall(s.FloorHeight, s.CeilingHeight, loftPts));
                //    if (result.Count > 0)
                //        meshes.AddRange(result);
                //}

                SortedSet<DelveLib.Map.HardCorner> corners = new SortedSet<DelveLib.Map.HardCorner>();
                foreach (var s in map.Sectors)
                {
                    var found = mp.GetSectorHardPoints(s, 0.4f);
                    if (found.Count > 0)
                    {
                        foreach (var f in found)
                            corners.Add(f);
                    }
                }

                //var columnMeshes = mp.ColumnizeHardCorners(corners, colPts);
                //if (columnMeshes.Count > 0)
                //    meshes.AddRange(columnMeshes);

                //mp.PolygonizeSectors(map.Sectors, true, meshes);
                //mp.PolygonizeSectors(map.Sectors, false, meshes);

                map.Sectors.ForEach(s => mp.SkirtSector(s, 20, meshes));

                if (meshes.Count >0 )
                {
                    g3.StandardMeshWriter w = new g3.StandardMeshWriter();
                    w.Write("TestExport.obj", meshes.ConvertAll(m => new g3.WriteMesh(m)), new g3.WriteOptions());
                }
            }*/

            white_ = Content.Load<Texture2D>("Textures/White");
            black_ = Content.Load<Texture2D>("Textures/Black");
            ibl_ = Content.Load<TextureCube>("Textures/DayIBL");
            spriteBatch_ = new SpriteBatch(GraphicsDevice);
            renderTargets_ = new MRT(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            ssaoRenderer_ = new SSAO(GraphicsDevice, Content);
            debugDraw_ = new DebugDraw(GraphicsDevice);
            debugMesh_ = new DebugMesh(GraphicsDevice);
            debugRenderer_ = new DebugRenderer();
            debugRenderer_.SetEffect(new BasicEffect(GraphicsDevice)
            {
                LightingEnabled = false,
                VertexColorEnabled = true,
                TextureEnabled = false,
                World = Matrix.CreateScale(-1, 1, 1)
            });

            meshBatch_ = new Graphics.MeshBatch(GraphicsDevice);
            pbrEffect_ = new Graphics.PBREffect(GraphicsDevice, Content);
            basicEffect_ = new ExtBasicEffect(GraphicsDevice, Content, "Effects/ExtBasicEffect")
            {
                AmbientLightColor = Vector3.One,
                LightingEnabled = true,
                DiffuseColor = Vector3.One,
                TextureEnabled = false
            };
        }

        protected override void UnloadContent()
        {

        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            appState_.Update();
            float td = gameTime.ElapsedGameTime.Milliseconds / 1000.0f;
        }

        static Color bg = new Color(0.2f, 0.2f, 0.2f);
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(bg);
            GraphicsDevice.Viewport = new Viewport(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height, 0, 1);

            float td = gameTime.ElapsedGameTime.Milliseconds / 1000.0f;

            renderTargets_.SetTargetSize(Window.ClientBounds.Width, Window.ClientBounds.Height);
            renderTargets_.Begin();
            PreDraw(td);
            DrawContent(td);
            debugRenderer_.Render(GraphicsDevice, gameState_.ActiveCamera.CombinedMatrix);
            renderTargets_.End();

            if (postProcess_)
                ssaoRenderer_.RunSSAO(renderTargets_, Window.ClientBounds.Width, Window.ClientBounds.Height);
            if (postProcess_)
                spriteBatch_.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, ssaoRenderer_.darkener_);
            else
                spriteBatch_.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            spriteBatch_.Draw(renderTargets_.colorTarget_, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.White);
            spriteBatch_.End();
            
            PostDraw(td);
        }

        protected virtual void PreDraw(float td)
        {
            if (drawDebug_)
            {
                var View = gameState_.ActiveCamera.ViewMatrix;
                var Projection = gameState_.ActiveCamera.ProjectionMatrix;

                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                debugDraw_.Begin(View, Projection);
                debugMesh_.Begin(View, Projection);
                debugRenderer_.Begin();

                debugDraw_.DrawWireGrid(Vector3.UnitX * 32, Vector3.UnitZ * 32, new Vector3(-16,0,-16), 64, 64, Color.DarkGray);
                if (gameState_.Scene != null)
                {
                    for (int i = 0; i < gameState_.Scene.Objects.Count; ++i)
                        gameState_.Scene.Objects[i].DrawDebug(debugRenderer_);
                }

                debugDraw_.DrawLine(Vector3.Zero, Vector3.UnitX * 10, Color.Red);
                debugDraw_.DrawLine(Vector3.Zero, Vector3.UnitY * 10, Color.LimeGreen);
                debugDraw_.DrawLine(Vector3.Zero, Vector3.UnitZ * 10, Color.Cyan);

                debugDraw_.End();
                debugMesh_.End();
            }

            if (gameState_.Gizmo != null)
            {
                var View = gameState_.ActiveCamera.ViewMatrix;
                var Projection = gameState_.ActiveCamera.ProjectionMatrix;

                gameState_.Gizmo.State.WorldSpace = !gameState_.GizIsLocal;
                gameState_.Gizmo.State.Mode = gameState_.GizMode;
                gameState_.Gizmo.State.ShiftHeld = appState_.Keyboard.IsKeyDown(Keys.LeftShift) || appState_.Keyboard.IsKeyDown(Keys.RightShift);
                gameState_.Gizmo.State.AltHeld = appState_.Keyboard.IsKeyDown(Keys.LeftAlt) || appState_.Keyboard.IsKeyDown(Keys.RightAlt);
                gameState_.Gizmo.State.MouseDown = false;
                if (!ImAids.ImGuiHasInput)
                    gameState_.Gizmo.State.MouseDown = appState_.Mouse.LeftButton == ButtonState.Pressed;
                gameState_.Gizmo.State.PickingRay = gameState_.ActiveCamera.GetPickRay(GraphicsDevice.Viewport, appState_.Mouse.Position.X, appState_.Mouse.Position.Y);
                gameState_.Gizmo.State.DeltaTime += td;
                gameState_.Gizmo.Draw();

            }
        }

        float _rotation;
        MeshData cubeMesh;
        protected virtual void DrawContent(float td)
        {
            if (gameState_.ActiveCamera != null && gameState_.Scene != null)
            {
                meshBatch_.Begin(gameState_.ActiveCamera);
                gameState_.Scene.Draw(meshBatch_);
                meshBatch_.Render(gameState_.ActiveCamera, 0);
            }

            _rotation += 1;
            if (cubeMesh == null)
            {
                cubeMesh = MeshData.CreateBox();
                cubeMesh.Initialize(GraphicsDevice);
            }

            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            meshBatch_.Begin(gameState_.ActiveCamera);

            List<Matrix> transforms = new List<Matrix>();
            pbrEffect_.DiffuseTexture = white_;
            pbrEffect_.RoughnessTexture = white_;
            pbrEffect_.MetalnessTexture = black_;
            pbrEffect_.IBLMap = ibl_;
            pbrEffect_.CameraPosition = gameState_.ActiveCamera.Position;
            pbrEffect_.LightDirection = Vector3.Normalize(new Vector3(0, -1, 0));
            pbrEffect_.Ambient = 0.2f;
            pbrEffect_.WorldViewProjection = gameState_.ActiveCamera.CombinedMatrix;
            pbrEffect_.CurrentTechnique = pbrEffect_.Techniques.Last();
            for (int x = -125; x < 125; ++x)
            {
                for (int z = -125; z < 125; ++z)
                {
                    Matrix m = Matrix.CreateRotationY(MathHelper.ToRadians(_rotation))
                                            //*Matrix.CreateTranslation(_modelPosition)
                                            * Matrix.CreateRotationX(MathHelper.ToRadians(_rotation / 4f))
                                            * Matrix.CreateTranslation(x * 2, x * 0.2f, z * 2);

                    meshBatch_.Add(null, cubeMesh.VertexBuffer, cubeMesh.IndexBuffer, m, pbrEffect_);
                    //meshBatch_.Add(null, cubeMesh.VertexBuffer, cubeMesh.IndexBuffer,
                    //     Matrix.CreateTranslation(x * 2, x * 0.2f, z * 2),
                    //    pbrEffect_);
                }
            }
            meshBatch_.Render(gameState_.ActiveCamera, 0);
        }

        protected virtual void PostDraw(float td)
        {
            //debugRenderer_.Render(GraphicsDevice, gameState_.ActiveCamera.CombinedMatrix);
        }
    }
}
