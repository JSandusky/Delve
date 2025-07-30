using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DelveLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Delve.Logic
{
    public enum CameraMode
    {
        ArcBall,
        Flight,
    }

    public class CameraController
    {
        IOCDependency<ViewportSettings> viewportSettings = new IOCDependency<ViewportSettings>();
        IOCDependency<ApplicationState> appState = new IOCDependency<ApplicationState>();

        bool[] mouseDown_ = new bool[3];
        Vector2? lastMouse_ = null;
        //ViewportDelegate viewport_;
        public Camera camera;

        public bool PrimaryIsOrbit = false;
        public float AnimationDistanceMultipler = 1.0f;
        float accelerationCount = 0.0f;
        const float accelerationSteps = 64.0f;

        public Vector3 OrbitOrigin = Vector3.Zero;

        public CameraController(Camera camera)
        {
            this.camera = camera;
            appState.Object.MouseButton += Elem_MouseState;
            appState.Object.MouseMoved += Elem_MouseMove;
            appState.Object.KeyReleased += Elem_KeyUp;
            appState.Object.MouseWheel += Elem_MouseWheel;
        }

        public void Update(float td)
        {
            {
                bool shiftDown = appState.Object.Keyboard.IsKeyDown(Keys.LeftShift) || appState.Object.Keyboard.IsKeyDown(Keys.RightShift);

                float speedFactor = shiftDown ? viewportSettings.Object.FastMovementSpeed : viewportSettings.Object.BaseMovementSpeed;

                if (appState.Object.Keyboard.IsKeyDown(viewportSettings.Object.Forward))
                    camera.Position += camera.Forward * 0.1f * speedFactor;
                if (appState.Object.Keyboard.IsKeyDown(viewportSettings.Object.Backward))
                    camera.Position -= camera.Forward * 0.1f * speedFactor;
                if (appState.Object.Keyboard.IsKeyDown(viewportSettings.Object.PanLeft))
                    camera.Position -= camera.Right * 0.1f * speedFactor;
                if (appState.Object.Keyboard.IsKeyDown(viewportSettings.Object.PanRight))
                    camera.Position += camera.Right * 0.1f * speedFactor;
                if (appState.Object.Keyboard.IsKeyDown(viewportSettings.Object.PanUp))
                    camera.Position += camera.UpDir * 0.1f * speedFactor;
                if (appState.Object.Keyboard.IsKeyDown(viewportSettings.Object.PanDown))
                    camera.Position -= camera.UpDir * 0.1f * speedFactor;
            }
        }

        private void Elem_MouseWheel(object sender, int e)
        {
            if (ImAids.ImGuiHasInput)
                return;

            //if (elem_.IsMouseDirectlyOver)
            {
                if (e > 0)
                {
                    camera.Position += camera.Forward;
                    camera.LookAtDir(camera.Forward);
                }
                else if (e < 0)
                {
                    camera.Position -= camera.Forward;
                    camera.LookAtDir(camera.Forward);
                }
            }
        }

        float CalculateFitmentDistance(out Vector3 centroid)
        {
            centroid = new Vector3();
            return 10.0f;
        }

        public void Focus()
        {
            Vector3 centroid = new Vector3();
            float dist = CalculateFitmentDistance(out centroid);
            Vector3 zoomDir = new Vector3(1, 1, 1) * dist;
            camera.ActiveAnimation = new CameraAnimation(zoomDir, centroid, 0.3f);
        }

        private void Elem_KeyUp(object sender, Keys e)
        {
            //if (!viewport_.IsActive)
            //    return;

            // Camera control commands
            // Home = reset to default view
            // End = Look at selection, or scene center
            // Numpad 8 look at front-side
            // Numpad 2 look at back-side
            // Numpad 4 look at left-side
            // Numpad 6 look at right-side
            // Numpad 5 look at top

            //if (!elem_.IsFocused)
            //    return;

            float pos64 = 64.0f * AnimationDistanceMultipler;
            float neg64 = -64.0f * AnimationDistanceMultipler;
            float pos3 = 3.0f * AnimationDistanceMultipler;
            float neg3 = -3.0f * AnimationDistanceMultipler;

            if (e == viewportSettings.Object.ResetView)
            {
                //if (documentManager.Object.ActiveDocument != null)
                //{
                //    // Set ourself to fit everything into view
                //    if (documentManager.Object.ActiveDocument is SprueKit.Data.SprueModelDocument)
                //    {
                //        Vector3 centroid = new Vector3();
                //        float dist = CalculateFitmentDistance(out centroid);
                //        Vector3 zoomDir = new Vector3(1, 1, 1) * dist;
                //        camera.ActiveAnimation = new VizAnim.CameraAnimation(zoomDir, centroid, 0.3f);
                //        return;
                //    }
                //}

                // Fallback, just set ourself to near the center
                camera.ActiveAnimation = new CameraAnimation(new Vector3(5, 5, 5), Vector3.Zero, 0.3f);
            }
            else if (e == viewportSettings.Object.LookAtSelection)
            {
                Vector3? selPos = SelectedPosition();
                if (selPos.HasValue)
                    camera.ActiveAnimation = new CameraLookAtAnimation(selPos.Value, 0.2f);
                else
                    camera.ActiveAnimation = new CameraLookAtAnimation(Vector3.Zero, 0.2f);
            }
            else if (e == Keys.NumPad4) //look to right, from left
                camera.ActiveAnimation = new CameraAnimation(new Vector3(neg64, pos3, 0), new Vector3(0, pos3, 0), 0.3f);
            else if (e == Keys.NumPad2) //look to front, from back
                camera.ActiveAnimation = new CameraAnimation(new Vector3(0, pos3, neg64), new Vector3(0, pos3, 0), 0.3f);
            else if (e == Keys.NumPad8) //look to back, from front
                camera.ActiveAnimation = new CameraAnimation(new Vector3(0, pos3, pos64), new Vector3(0, pos3, 0), 0.3f);
            else if (e == Keys.NumPad6) //look to left, from right
                camera.ActiveAnimation = new CameraAnimation(new Vector3(pos64, pos3, 0), new Vector3(0, pos3, 0), 0.3f);
            else if (e == Keys.NumPad5) //down from top
                camera.ActiveAnimation = new CameraAnimation(new Vector3(0, pos64, 0.01f), Vector3.Zero, 0.3f);
        }

        private void Elem_MouseMove(object sender, Vector2 e)
        {
            if (ImAids.ImGuiHasInput)
                return;
            //if (!viewport_.IsActive)
            //    return;

            //System.Windows.Point newMouse = e.GetPosition(elem_);
            if (!lastMouse_.HasValue)
            {
                lastMouse_ = appState.Object.Mouse.Position.ToVector2();
                return;
            }

            //if (!elem_.IsMouseDirectlyOver && !elem_.IsVisible)
            //    return;

            if (mouseDown_[0] || mouseDown_[1] || mouseDown_[2])
            {
                float dx = e.X;//float)(newMouse.X - lastMouse_.Value.X);
                float dy = e.Y;//(float)(newMouse.Y - lastMouse_.Value.Y);

                Vector2 vec;// FramePool<Vector2>.obtain();
                vec.X = dx;
                vec.Y = dy;
                OnMouseMove(vec);
                //e.Handled = true;
            }
            lastMouse_ = appState.Object.Mouse.Position.ToVector2();
        }

        private void Elem_MouseState(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled)
                return;

            if (ImAids.ImGuiHasInput)
                return;

            mouseDown_[0] = appState.Object.Mouse.LeftButton == ButtonState.Pressed;
            mouseDown_[1] = appState.Object.Mouse.RightButton == ButtonState.Pressed;
            mouseDown_[2] = appState.Object.Mouse.MiddleButton == ButtonState.Pressed;

            if (mouseDown_[1] && e.Right)
            {
                //??Mouse.Capture(elem_);
                //??Mouse.OverrideCursor = Cursors.SizeAll;
                e.Handled = true;
            }
            else if (e.Right)
            {
                //elem_.ReleaseMouseCapture();
                //Mouse.OverrideCursor = null;
                accelerationCount = 0.0f;
                e.Handled = true;
            }

            if (mouseDown_[2] && e.Middle)
            {
                //Mouse.Capture(elem_);
                //Mouse.OverrideCursor = Cursors.SizeNS;
                e.Handled = true;
            }
            else if (e.Middle)
            {
                //elem_.ReleaseMouseCapture();
                //Mouse.OverrideCursor = null;
                accelerationCount = 0.0f;
                e.Handled = true;
            }
        }

        protected virtual void OnMouseMove(Vector2 delta)
        {
            float accelerationFactor = Math.Min(accelerationCount / accelerationSteps, 1.0f);
            bool shiftDown = appState.Object.Keyboard.IsKeyDown(Keys.LeftShift) || appState.Object.Keyboard.IsKeyDown(Keys.RightShift);

            float multiplier = shiftDown ? viewportSettings.Object.FastMovementSpeed : viewportSettings.Object.BaseMovementSpeed;

            // Angular control
            if (mouseDown_[1])
            {
                const float pitchFactor = 1.0f;
                const float yawFactor = 1.0f;
                bool anyChanges = false;
                float xx = 0.0f;
                float yy = 0.0f;

                // flip the Y axis
                if (viewportSettings.Object.InvertYAxis)
                    delta.Y = -delta.Y;

                // Turn in place
                if ((shiftDown && PrimaryIsOrbit) || (!shiftDown && !PrimaryIsOrbit))
                {
                    if (Math.Abs(delta.Y) > 0)
                    {
                        camera.Pitch(delta.Y * 0.2f);// > 0 ? pitchFactor : -pitchFactor) * 0.4f);
                        anyChanges = true;
                    }
                    if (Math.Abs(delta.X) > 0)
                    {
                        camera.Yaw(delta.X * 0.2f);// > 0 ? yawFactor : -yawFactor) * 0.4f);
                        anyChanges = true;
                    }
                }
                // Orbit around point
                else
                {
                    if (Math.Abs(delta.Y) > 0)
                        yy = (delta.Y > 0 ? 1.0f : -1.0f);
                    if (Math.Abs(delta.X) > 0)
                        xx = (delta.X > 0 ? 1.0f : -1.0f);

                    if (xx != 0 || yy != 0)
                    {
                        anyChanges = true;

                        Vector3? selPos = SelectedPosition();
                        camera.OrbitAround(selPos.HasValue ? selPos.Value : OrbitOrigin, delta.X * accelerationFactor, delta.Y * accelerationFactor);
                    }
                }

                if (!anyChanges)
                    accelerationCount = 0.0f;
                accelerationCount += 1.0f;
            }

            // Dolly
            if (mouseDown_[2])
            {
                if (Math.Abs(delta.Y) > 0)
                {
                    camera.Position += camera.Forward * ((delta.Y > 0 ? 0.5f : -0.5f) * accelerationFactor) * multiplier;
                    accelerationCount += 1.0f;
                }
                else
                    accelerationCount = 0;
            }

            // Picking
            if (mouseDown_[0])
            {

            }
        }

        private Vector3? SelectedPosition()
        {
            //if (documentManager.Object.ActiveDocument != null)
            //{
            //    object sel = documentManager.Object.ActiveDocument.Selection.MostRecentlySelected;
            //    if (sel != null)
            //    {
            //        SprueKit.Data.SpruePiece piece = sel as SprueKit.Data.SpruePiece;
            //        if (piece != null)
            //            return piece.Position;
            //    }
            //}
            return null;
        }
    }

}
