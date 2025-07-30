using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using DelveLib;
using ImGuiCLI;

using System.Reflection;
using System.Runtime.InteropServices;

namespace Delve
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Delve : Game
    {
        RT renderTarget_;
        ImGuiContext imguiContext_;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        DelveLib.StructArray<DelveLib.VerletParticle> particles_ = new DelveLib.StructArray<DelveLib.VerletParticle>(1500, Marshal.SizeOf<DelveLib.VerletParticle>());
        Texture2D whiteBall_;
        Texture2D white_;

        static float[] plotValues = new float[] {
            0.2f, 0.1f, 0.7f, 0.9f
        };
        float progress = 0.0f;

        bool IsFocused
        {
            get
            {
                return ((MonoGame.Framework.WinFormsGameWindow)Window).Form.Focused;
            }
        }

        public Delve()
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
            System.IntPtr device = ((SharpDX.Direct3D11.Device)GraphicsDevice.Handle).NativePointer;
            System.IntPtr deviceContext = ((SharpDX.Direct3D11.DeviceContext)GraphicsDevice.ContextHandle).NativePointer;
            System.IntPtr backBuffer = ((SharpDX.Direct3D11.RenderTargetView)GraphicsDevice.BackBuffer).NativePointer;

            renderTarget_ = new RT(GraphicsDevice, 64, 64);

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
            ImGuiDock.LoadDock();
        }
        
        void Reset()
        {
            particles_ = new StructArray<VerletParticle>(10000, Marshal.SizeOf<DelveLib.VerletParticle>());
            var rand = new System.Random();
            int z = 0;
            for (int x = 0; x < 1000; ++x, ++z)
            {
                int xx = x + 1;
                for (int y = 0; y < 1; ++y, ++z)
                {
                    int yy = y + 1;
                    particles_.Add(new Vector3(rand.Rand(xx * 5, x * 5), 30 + rand.Rand(10 * yy, 30 * yy), 0), 
                        rand.Rand(8, 10), 
                        rand.Rand(10, 10), 
                        rand.Rand(0.4f, 0.5f));
                    //particles_.items_[z].SetVelocity(new Vector3(rand.Rand(-5, 5), rand.Rand(0, 1), 0));
                }
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            white_ = Content.Load<Texture2D>("Textures/White");
            whiteBall_ = Content.Load<Texture2D>("Textures/WhiteBall");
            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here        
            ImGuiDock.SaveDock();
            imguiContext_.Shutdown();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        bool didRest = false;
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            if (Keyboard.GetState().IsKeyDown(Keys.R) && !didRest)
            {
                Reset();
                didRest = true;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                var r = new System.Random();
                for (int i = 0; i < particles_.Count; ++i)
                {
                    particles_.items_[i].SetVelocity(
                        XNAExt.Random(r, new Vector3(-3, 3, 0), new Vector3(3,6,0))
                    );
                }
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Left))
            {
                var r = new System.Random();
                for (int i = 0; i < particles_.Count; ++i) 
                    particles_.items_[i].SetVelocity(particles_.items_[i].Velocity + new Vector3(-0.3f, 0, 0));
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Right))
            {
                var r = new System.Random();
                for (int i = 0; i < particles_.Count; ++i) 
                    particles_.items_[i].SetVelocity(particles_.items_[i].Velocity + new Vector3(0.3f, 0, 0));
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Up))
            {
                var r = new System.Random();
                for (int i = 0; i < particles_.Count; ++i) 
                    particles_.items_[i].SetVelocity(particles_.items_[i].Velocity + new Vector3(0, 0.3f, 0));
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Down))
            {
                var r = new System.Random();
                for (int i = 0; i < particles_.Count; ++i) 
                    particles_.items_[i].SetVelocity(particles_.items_[i].Velocity + new Vector3(0, -0.3f, 0));
            }

            // TODO: Add your update logic here
            if (particles_.Count > 0)
            { 
                float td = gameTime.ElapsedGameTime.Milliseconds / 1000.0f;
                particles_.Update(td);
                particles_.Collide();
                
                particles_.Collide(new Plane(new Vector3(0, -1, 0), 800));
                particles_.Collide(new Plane(new Vector3(-1, 0, 0), 1500));
                particles_.Collide(new Plane(new Vector3(0, 1, 0), 0.0f));
                particles_.Collide(new Plane(new Vector3(1, 0, 0), 0.0f));
                particles_.Collide();
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
        static int listSelectedIdx = -1;
        static string[] listItems = { "Item #1", "Item #2", "Item #3" };
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            GraphicsDevice.Viewport = new Viewport(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height, 0, 1);

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
                Mouse.SetCursor(MouseCursor.SizeAll);
                lastCursor = MouseCursor.SizeAll;
                break;
            case ImGuiMouseCursor_.ResizeEW:
                Mouse.SetCursor(MouseCursor.SizeWE);
                lastCursor = MouseCursor.SizeWE;
                break;
            case ImGuiMouseCursor_.ResizeNS:
                Mouse.SetCursor(MouseCursor.SizeNS);
                lastCursor = MouseCursor.SizeNS;
                break;
            case ImGuiMouseCursor_.ResizeNESW:
                Mouse.SetCursor(MouseCursor.SizeNESW);
                lastCursor = MouseCursor.SizeNESW;
                break;
            case ImGuiMouseCursor_.ResizeNWSE:
                Mouse.SetCursor(MouseCursor.SizeNWSE);
                lastCursor = MouseCursor.SizeNWSE;
                break;
            case ImGuiMouseCursor_.TextInput:
                Mouse.SetCursor(MouseCursor.IBeam);
                lastCursor = MouseCursor.IBeam;
                break;
            }
            imguiContext_.NewFrame(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            
            ImGuiDock.RootDock(new Vector2(Window.ClientBounds.Left, Window.ClientBounds.Top), new Vector2(Window.ClientBounds.Width, Window.ClientBounds.Height));

            if (ImGuiDock.BeginDock("Trial window #1"))
            {
                ImGuiCli.Text("Drawing some text");
                ImGuiCli.Checkbox("Checkbox", ref someValue);
            } ImGuiDock.EndDock();

            if (ImGuiDock.BeginDock("Instructions")) {
                ImGuiCli.Text("Use 'R' to spawn verlet balls (or respawn them).");
                ImGuiCli.Text("<spacebar> to add substantial random upward force.");
                ImGuiCli.Text("<arrow keys> apply directional force.");
                ImGuiCli.PlotLines("An example plot", plotValues);
                ImGuiCli.ProgressBar(progress);
                progress += td;
                if (progress > 1.0f)
                    progress = 0;
                ImGuiCli.Checkbox("Checkbox", ref someValue);
            } ImGuiDock.EndDock();

            if (ImGuiDock.BeginDock("Render Target as Image", 0, DockFlags.NoPad)) {
                Vector2 pad = ImGuiStyle.WindowPadding;
                var size = ImGuiCli.GetWindowContentRegionMax() - ImGuiCli.GetWindowContentRegionMin();
                renderTarget_.SetRenderTargetSize((int)size.X, (int)size.Y);
                renderTarget_.Draw();
                ImGuiCli.Image(renderTarget_.target_, size);
            } ImGuiDock.EndDock();

            if (ImGuiDock.BeginDock("Trial window #4")) { ImGuiCli.Text("Drawing some text"); ImGuiCli.Checkbox("Checkbox", ref someValue); } ImGuiDock.EndDock();

            if (ImGuiDock.BeginDock("Physics Objects"))
            {
                if (particles_ != null && particles_.Count > 0)
                {
                    for (int i = 0; i < particles_.Count; ++i)
                    {
                        if (ImGuiCli.TreeNode("Item " + (i + 1)))
                        {
                            ImGuiCli.Indent();
                            ImGuiCli.Text(string.Format("Position: {0}", particles_.items_[i].position_.ToString()));
                            ImGuiCli.Text(string.Format("Velocity: {0}", particles_.items_[i].Velocity.ToString()));
                            ImGuiCli.Text(string.Format("Radius: {0}", particles_.items_[i].radius_.ToString()));
                            ImGuiCli.Text(string.Format("Mass: {0}", particles_.items_[i].mass_.ToString()));
                            ImGuiCli.Unindent();
                            ImGuiCli.TreePop();
                        }
                    }
                }
            }
            ImGuiDock.EndDock();

            imguiContext_.RenderAndDraw(((SharpDX.Direct3D11.RenderTargetView)GraphicsDevice.BackBuffer).NativePointer);
            GraphicsDevice.Viewport = new Viewport(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height, 0, 1);
            GraphicsDevice.Textures[0] = null;
            GraphicsDevice.SetRenderTarget(null);
            // TODO: Add your drawing code here
            Color[] Colors = new Color[]
            {
                new Color(50, 60, 255, 255),
                new Color(100, 80, 255, 255),
                new Color(100, 60, 255, 255),
                new Color(60, 60, 255, 255),
                new Color(100, 120, 255, 255),
            };
            Color blue = new Color(0, 120, 255);
            spriteBatch.Begin();
            
            for (int i = 0; i < particles_.Count; ++i)
            {
                Vector3 p = particles_.items_[i].position_;
                float r = particles_.items_[i].radius_;
                if (i % 80 == 0)
                    spriteBatch.Draw(whiteBall_,
                        new Rectangle((int)(p.X - r), (int)(800 - p.Y - r), (int)r * 2, (int)r * 2),
                        Color.Red);// * (1.0f + p.Z));
                else
                    spriteBatch.Draw(whiteBall_,
                        new Rectangle((int)(p.X-r), (int)(800 - p.Y-r), (int)r*2, (int)r*2),
                        blue);//Colors[i%5]);// * (1.0f + p.Z));
            }
            spriteBatch.Draw(white_, new Rectangle(0, 800, 1500, 2), Color.DarkCyan);
            spriteBatch.Draw(white_, new Rectangle(1500, 0, 2, 800), Color.DarkCyan);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
