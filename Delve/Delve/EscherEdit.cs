using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DelveLib;
using ImGuiCLI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Delve
{
    public class EscherEdit : Escher
    {
        Logic.CameraController cameraControl_;
        IOCDependency<Objects.GameState> GameState = new IOCDependency<Objects.GameState>();
        RT renderTarget_;
        ImGuiContext imguiContext_;

        static MouseCursor lastCursor = MouseCursor.Arrow;
        static int scrollValue = 0;

        public const int WM_MOUSEHWHEEL = 0x020E;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;

        bool IsFocused
        {
            get
            {
                return ((MonoGame.Framework.WinFormsGameWindow)Window).Form.Focused;
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
            gameState_.ActiveCamera = gameState_.EditorCamera;
            cameraControl_ = new Logic.CameraController(gameState_.EditorCamera);

            appState_.MouseButton += AppState__MouseButton;
            appState_.MouseMoved += AppState__MouseMoved;

            IsMouseVisible = true;
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
        }

        private void AppState__MouseMoved(object sender, Vector2 e)
        {
            
        }

        private void AppState__MouseButton(object sender, MouseButtonEventArgs e)
        {
            if (ImAids.ImGuiHasInput)
                return;

            if (e.Handled)
                return;

            if (gameState_.Gizmo != null && (gameState_.Gizmo.State.Status != GizmoStatus.None || gameState_.Gizmo.State.Status != gameState_.Gizmo.State.OldStatus))
            {
            }
            else
            {
                if (e.Left)
                {
                    gameState_.Gizmo = null;
                    var p = appState_.Mouse.Position.ToVector2();
                    Ray pickRay = gameState_.EditorCamera.GetPickRay(GraphicsDevice.Viewport, p.X, p.Y);
                    Objects.GameObject hitObject = gameState_.Scene.Raycast(ref pickRay);
                    if (hitObject != null)
                        hitObject.PostRay(ref pickRay, 0);
                }
            }
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
            imguiContext_.Shutdown();
        }

        bool showMetrics_ = false;
        bool showViewportSettings_ = false;
        protected override void PreDraw(float td)
        {
            base.PreDraw(td);
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

            if (ImGuiCli.BeginMainMenuBar())
            {
                if (ImGuiCli.BeginMenu("Data", true))
                {
                    if (ImGuiCli.MenuItem("New Scene"))
                        gameState_.Scene = new Objects.Scene();
                    if (ImGuiCli.MenuItem("Open Scene"))
                    {
                        System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
                        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            gameState_.Scene = Objects.Scene.Load(dlg.FileName);
                        
                    }
                    if (ImGuiCli.MenuItem("Save Scene"))
                    {
                        ImAids.PushModal(new ImModal("Save Scene", "",
                            () => {
                                if (!string.IsNullOrWhiteSpace(gameState_.Scene.SceneName))
                                    gameState_.Scene.Save(gameState_.Scene.SceneName.Trim());
                            },
                            () => { },
                            () => {
                                ImGuiCli.InputText("Name", ref gameState_.Scene.SceneName);
                            }));
                    }
                    ImGuiCli.Separator();
                    if (ImGuiCli.MenuItem("Exit"))
                        ImAids.PushModal(new ImModal("Are you sure?", "Do you really want to exit the program", () => { Exit(); }, () => { }));
                    ImGuiCli.EndMenu();
                }
                if (ImGuiCli.BeginMenu("Windows", true))
                {
                    ImGuiCli.MenuItem("Game Objects", ref showSceneContents_);
                    ImGuiCli.MenuItem("Lighting Setup");
                    ImGuiCli.Separator();
                    ImGuiCli.MenuItem("Viewport Settings", ref showViewportSettings_);
                    ImGuiCli.MenuItem("Debug Metrics", ref showMetrics_);
                    ImGuiCli.EndMenu();
                }

                ImGuiCli.Checkbox("Draw Debug", ref drawDebug_);
                ImGuiCli.Checkbox("Postprocess", ref postProcess_);
                ImGuiCli.Spacing();
                ImGuiCli.Button(ICON_FA.PLAY + " Play!");

                bool isTrans = gameState_.GizMode == DelveLib.GizmoMode.Translation;
                bool isRotate = gameState_.GizMode == DelveLib.GizmoMode.Rotation;
                bool isScale = gameState_.GizMode == DelveLib.GizmoMode.Scale;

                ImGuiCli.Text("   ^2Gizmo:");
                if (ImGuiCli.Checkbox("Move", ref isTrans))
                    gameState_.GizMode = DelveLib.GizmoMode.Translation;
                if (ImGuiCli.Checkbox("Rotate", ref isRotate))
                    gameState_.GizMode = DelveLib.GizmoMode.Rotation;
                if (ImGuiCli.Checkbox("Scale", ref isScale))
                    gameState_.GizMode = DelveLib.GizmoMode.Scale;

                ImGuiCli.Checkbox("Local", ref gameState_.GizIsLocal);
            }
            ImGuiCli.EndMainMenuBar();

            DrawSceneContents();

            if (showMetrics_)
                ImGuiCli.ShowMetricsWindow();

            if (showViewportSettings_)
            {
                if (ImGuiCli.Begin("Viewport Settings", ref showViewportSettings_, ImGuiWindowFlags_.None))
                {
                    ImGuiCli.Text("Speed");
                    ImGuiCli.Separator();

                    ImGuiCli.DragFloat("Base Speed", ref viewportSettings_.BaseMovementSpeed);
                    ImGuiCli.DragFloat("Fast Speed", ref viewportSettings_.FastMovementSpeed);
                    ImGuiCli.Checkbox("Invert Y", ref viewportSettings_.InvertYAxis);

                    ImGuiCli.Spacing();
                    ImGuiCli.Spacing();

                    ImGuiCli.Text("Controls");
                    ImGuiCli.Separator();
                    ImAids.EnumCombo<Keys>("Forward", ref viewportSettings_.Forward);
                    ImAids.EnumCombo<Keys>("Backwards", ref viewportSettings_.Backward);
                    ImAids.EnumCombo<Keys>("Pan Left", ref viewportSettings_.PanLeft);
                    ImAids.EnumCombo<Keys>("Pan Right", ref viewportSettings_.PanRight);
                    ImAids.EnumCombo<Keys>("Pan Up", ref viewportSettings_.PanUp);
                    ImAids.EnumCombo<Keys>("Pan Down", ref viewportSettings_.PanDown);
                    ImGuiCli.Separator();
                    ImAids.EnumCombo<Keys>("Look At Selection", ref viewportSettings_.LookAtSelection);
                }
                ImGuiCli.End();
            }  
        }

        protected override void PostDraw(float td)
        {
            ImAids.ProcessModals();
            imguiContext_.RenderAndDraw(((SharpDX.Direct3D11.RenderTargetView)GraphicsDevice.BackBuffer).NativePointer);
            GraphicsDevice.Viewport = new Viewport(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height, 0, 1);
            GraphicsDevice.Textures[0] = null;
            GraphicsDevice.SetRenderTarget(null);
            base.PostDraw(td);

            if (!ImAids.ImGuiHasInput)
                cameraControl_.Update(td);
        }

        protected override void DrawContent(float td)
        {
            base.DrawContent(td);
        }

        bool showSceneContents_ = true;
        private void DrawSceneContents()
        {
            if (!showSceneContents_)
                return;
            
            if (ImGuiCli.Begin("Scene Content", ref showSceneContents_, ImGuiWindowFlags_.MenuBar))
            {
                if (ImGuiCli.BeginMenuBar())
                {
                    ImGuiCli.Button("Add Folder");
                    if (ImGuiCli.BeginMenu("Create", true))
                    {
                        if (ImGuiCli.MenuItem("Box") && GameState.Object.Scene != null)
                            GameState.Object.Scene.Add(new Objects.GameObject());
                        if (ImGuiCli.MenuItem("Curve") && GameState.Object.Scene != null)
                            GameState.Object.Scene.Add(new Objects.BillboardStrip());
                        ImGuiCli.EndMenu();
                    }
                    ImGuiCli.EndMenuBar();
                }
                if (ImGuiCli.BeginChild("##tree", Vector2.Zero, true))
                {
                    if (GameState.Object.Scene != null)
                    {
                        for (int g = 0; g < GameState.Object.Scene.groups_.Count; ++g)
                        {
                            var grp = GameState.Object.Scene.groups_[g];
                            if (ImGuiCli.TreeNode(grp.GroupName))
                            {
                                for (int i = 0; i < grp.Objects.Count; ++i)
                                {
                                    if (ImGuiCli.TreeNode(grp.Objects[i].GetHashCode().ToString()))
                                    {
                                        grp.Objects[i].DrawEditor();
                                        ImGuiCli.TreePop();
                                    }
                                }

                                ImGuiCli.TreePop();
                            }
                        }
                    }
                }
                ImGuiCli.EndChild();
            }
            ImGuiCli.End();
        }
    }
}
