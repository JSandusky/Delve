using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DelveLib
{
    public enum GizmoMode
    {
        None,
        Translation,
        Rotation,
        Scale,
        Axial,
        UserHandle
    }

    [Flags]
    public enum GizmoAxis
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4,
        XY = X | Y,
        XZ = X | Z,
        YZ = Y | Z,
        XYZ = X | Y | Z,
        Axis = 1 << 3,
        Direction = 1 << 4,
        UserHandle1 = 1 << 5,
        UserHandle2 = 1 << 6,
        UserHandle3 = 1 << 7,
        UserHandle4 = 1 << 8,
    }

    public enum GizmoStatus
    {
        None,
        MouseOver,
        MouseHit,
        Changed
    }

    public abstract class GizmoAccessor
    {
        public abstract Matrix GetTransform();
        public abstract void SetTransform(Matrix matrix);

        public static Matrix ApplySnapping(Matrix mat, float positionSnap, float rotationSnap)
        {
            Vector3 trans = new Vector3();
            Quaternion rot = new Quaternion();
            Vector3 scale = new Vector3();
            mat.Decompose(out scale, out rot, out trans);

            trans.X = trans.X - (float)Math.Floor(trans.X) > 0.5 ? (float)Math.Floor(trans.X) * positionSnap : (float)Math.Ceiling(trans.X) * positionSnap;
            trans.Y = trans.Y - (float)Math.Floor(trans.Y) > 0.5 ? (float)Math.Floor(trans.Y) * positionSnap : (float)Math.Ceiling(trans.Y) * positionSnap;
            trans.Z = trans.Z - (float)Math.Floor(trans.Z) > 0.5 ? (float)Math.Floor(trans.Z) * positionSnap : (float)Math.Ceiling(trans.Z) * positionSnap;

            Vector3 euler = rot.ToEuler();
            euler.X = (float)Math.Floor(euler.X) * rotationSnap;
            euler.Y = (float)Math.Floor(euler.Y) * rotationSnap;
            euler.Z = (float)Math.Floor(euler.Z) * rotationSnap;

            return Matrix.CreateScale(scale) * euler.MatrixFromEuler() * Matrix.CreateTranslation(trans);
        }
    }

    public class GizmoState
    {
        public float? RotationStart;
        public float? RotationAngle;
        public Vector3? StartHitPosition;
        public Ray? LastRay;
        public Ray? PickingRay;
        public BoundingBox? ObjectBounds;
        public bool MouseDown;
        public bool ShiftHeld;
        public bool AltHeld;
        public float DeltaTime = 0.0f;
        public object GizmoObject = null;

        public event EventHandler<Gizmo> OnChange;

        public void SendChangeEvent(Gizmo g)
        {
            if (OnChange != null)
                OnChange(this, g);
        }

        bool deltaTimeWasUsed = false;

        public void CleanupState()
        {
            if (!deltaTimeWasUsed)
                DeltaTime = 0;
            deltaTimeWasUsed = false;
        }

        public float GetAnimTime() { deltaTimeWasUsed = true; return DeltaTime * 6.28318530718f * 2; }

        public float GetTimeCurve() { return 0.5f + Mathf.Sin(GetAnimTime()) * 0.5f; }

        bool worldSpace_ = false;
        public bool WorldSpace
        {
            get { return worldSpace_; }
            set { worldSpace_ = value; }
        }

        GizmoAxis axis_ = GizmoAxis.None;
        public GizmoAxis Axis
        {
            get { return axis_; }
            set { axis_ = value; }
        }

        public GizmoStatus OldStatus = GizmoStatus.None;
        GizmoStatus status_ = GizmoStatus.None;
        public GizmoStatus Status
        {
            get { return status_; }
            set { status_ = value; }
        }

        GizmoMode mode_ = GizmoMode.Translation;
        public GizmoMode Mode
        {
            get { return mode_; }
            set { mode_ = value; }
        }
    }

    [Serializable]
    public class ViewportSettings
    {
        [Description("Use flightstick style mouse control")]
        public bool InvertYAxis = false;

        [Description("Moves the camera forward along the viewing direction")]
        public Keys Forward = Keys.W;
        [Description("Moves the camera backward along the viewing direction")]
        public Keys Backward = Keys.S;
        [Description("Pans the camera to th eleft")]
        public Microsoft.Xna.Framework.Input.Keys PanLeft = Keys.A;
        [Description("Pans the camera to the right")]
        public Keys PanRight = Keys.D;
        [Description("Raises the camera vertically, maintains viewing direction")]
        public Keys PanUp = Keys.E;
        [Description("Lowers the camera vertically, maintains viewing direction")]
        public Keys PanDown = Keys.C;

        [Description("Resets the view to a neutral 3/4 axis")]
        public Keys ResetView = Keys.Home;
        [Description("Camera will pivot to look at the selected object or scene center")]
        public Keys LookAtSelection = Keys.End;

        [Description("Sets the speed of camera movement when SHIFT is NOT pressed")]
        public float BaseMovementSpeed = 1.0f;
        [Description("Speed of camera movement when SHIFT key is held")]
        public float FastMovementSpeed = 3.0f;

        [Description("Position transforms will snap to this grid unit")]
        public float PositionSnap = 1.0f;
        [Description("Whether position transforms are to be snapped or not")]
        public bool PositionSnapActive = false;
        [Description("Rotation transforms will snap increments of this angle")]
        public float RotationSnap = 45.0f;
        [Description("Whether rotation transforms are to be snapped or not")]
        public bool RotationSnapActive = false;
    }

    public class Gizmo
    {
        IOCDependency<ViewportSettings> viewportSettings = new IOCDependency<ViewportSettings>();

        DebugRenderer lineDrawer = null;
        Camera camera = null;
        float screenFactor_ = 0.5f;
        GizmoState state = new GizmoState();

        /// <summary>
        /// Invoked whenever the transform has changed
        /// </summary>
        public EventHandler<Matrix> TransformChanged;

        /// <summary>
        /// Holding down ALT will ask the gizmo to 'clone' whatever it is attached to
        /// Invoked whenever a cloning action is taken, it is an intercept, everything must be completed by the handler before returning
        /// </summary>
        public EventHandler<Matrix> DoClone;
        bool wasSending_ = false;

        // Note: these colors aren't truely transparent, just mostly so
        static Color TransparentWhite = new Color(Color.White, 0.2f);
        static Color TransRed = new Color(Color.Red, 0.3f);
        static Color TransGreen = new Color(Color.LimeGreen, 0.3f);
        static Color TransBlue = new Color(Color.CornflowerBlue, 0.3f);

        //Local copies

        // The transform of the gizmo, this will be in the snapped transform
        Matrix transform_;
        // The unsnapped transform used for drawing
        Matrix drawingTransform_;
        // Extracted translation, reduces matrix accuracy issues
        Vector3 translation_;
        // Extracted rotation, reduces matrix accuracy issues, rotation tends to cause scaling to drift
        Quaternion rotation_;
        // Extracted scale, reduces matrix accuracy issues
        Vector3 scale_;
        // Sentinel for preventing massive chains of cloning, can not clone again until either the mouse-button or ALT are released
        bool doneClone_ = false;

        /// <param name="value">Value to snap</param>
        /// <param name="step">stepping interval, relative to 0</param>
        /// <returns>Snaps the given input to a grid or interval</returns>
        float SnapValue(float value, float step)
        {
            float fractional = value / step;
            float modulo = value % step;
            float modFrac = modulo / step;

            float a = ((float)Math.Floor(fractional)) * step + step;
            float b = ((float)Math.Floor(fractional)) * step;

            return Math.Abs(value - a) > Math.Abs(value - b) ? b : a;
        }

        /// <summary>
        /// Called to send the event that we've been transformed and the target object needs to update
        /// </summary>
        void PostTransform()
        {
            if (state.AltHeld && !doneClone_)
            {
                if (DoClone != null)
                {
                    doneClone_ = true;
                    DoClone(this, transform_);
                }
            }

            wasSending_ = true;
            if (TransformChanged != null)
            {
                if (viewportSettings.Object.PositionSnapActive && state.Mode == GizmoMode.Translation)
                {
                    float amt = viewportSettings.Object.PositionSnap;
                    Vector3 pos = transform_.Translation;
                    pos.X = SnapValue(pos.X, amt);
                    pos.Y = SnapValue(pos.Y, amt);
                    pos.Z = SnapValue(pos.Z, amt);
                    TransformChanged(this, Matrix.CreateScale(transform_.Scale) * Matrix.CreateFromQuaternion(transform_.Rotation) * Matrix.CreateTranslation(pos));
                }
                else if (viewportSettings.Object.RotationSnapActive && state.Mode == GizmoMode.Rotation)
                {
                    float amt = viewportSettings.Object.RotationSnap;
                    Quaternion rot = transform_.Rotation;
                    Vector3 euler = rot.ToEuler();
                    euler.X = (float)Math.Floor(euler.X / amt) * amt;
                    euler.Y = (float)Math.Floor(euler.Y / amt) * amt;
                    euler.Z = (float)Math.Floor(euler.Z / amt) * amt;
                    TransformChanged(this, Matrix.CreateScale(scale_) * Matrix.CreateFromQuaternion(euler.QuaternionFromEuler()) * Matrix.CreateTranslation(transform_.Translation));
                }
                else
                {
                    TransformChanged(this, transform_);
                }
            }
            wasSending_ = false;
            SetDrawingTransform();
        }

        /// <summary>
        /// Construct the gizmo
        /// </summary>
        public Gizmo(Camera camera, DebugRenderer draw)
        {
            this.camera = camera;
            lineDrawer = draw;
            translation_ = new Vector3(3, 3, 0);
            rotation_ = Quaternion.Identity;
            scale_ = new Vector3(1, 1, 1);
            transform_ = Matrix.CreateTranslation(3, 3, 0);
        }

        public void OnTransformChanged(object sender, PropertyChangedEventArgs args)
        {
            if (wasSending_)
                return;
            if (sender == state.GizmoObject)
            {
                //TODO: SetTransform(((SprueKit.Data.SpruePiece)state.GizmoObject).Transform);
            }
        }

        /// <summary>
        /// Explicitly sets the transform, does not signal a transform change
        /// </summary>
        public void SetTransform(Matrix mat)
        {
            mat.Decompose(out scale_, out rotation_, out translation_);
            transform_ = mat;
            SetDrawingTransform();
        }

        /// <summary>
        /// Updates the rendering location transform
        /// </summary>
        void SetDrawingTransform()
        {
            drawingTransform_ = Matrix.CreateFromQuaternion(rotation_) * Matrix.CreateTranslation(translation_);
        }

        /// <summary>
        /// Returns the raw edit transform
        /// </summary>
        public Matrix GetTransform()
        {
            return transform_;
        }

        /// <summary>
        /// Current state of the gizmo
        /// </summary>
        public GizmoState State { get { return state; } }

        /// <summary>
        /// Renders and performs input updates of the gizmo
        /// </summary>
        /// <returns>True if any inputs were handled</returns>
        public bool Draw()
        {
            ComputeScreenFactor();

            state.OldStatus = state.Status;
            if (state.OldStatus == GizmoStatus.MouseHit && !state.MouseDown)
                state.OldStatus = GizmoStatus.None;

            state.Status = GizmoStatus.None;
            // Draw nothing then
            if (state.Mode == GizmoMode.None)
            {
                state.CleanupState();
                return false;
            }

            Vector3[] displayAxes =
            {
                Vector3.UnitX * screenFactor_,
                Vector3.UnitY * screenFactor_,
                Vector3.UnitZ * screenFactor_,
            };

            Vector3[] frameAxes =
            {
                Vector3.UnitX * screenFactor_,
                Vector3.UnitY * screenFactor_,
                Vector3.UnitZ * screenFactor_,
            };

            Vector3[] localAxes =
            {
                Vector3.UnitX * screenFactor_,
                Vector3.UnitY * screenFactor_,
                Vector3.UnitZ * screenFactor_,
            };

            GetAxes(ref frameAxes, false, false);
            GetAxes(ref localAxes, false, true);
            GetAxes(ref displayAxes, false);

            // Draw object axis system
            lineDrawer.DrawLine(translation_, translation_ + localAxes[0], Color.Red, DebugDrawDepth.Always);
            lineDrawer.DrawLine(translation_, translation_ + localAxes[1], Color.LimeGreen, DebugDrawDepth.Always);
            lineDrawer.DrawLine(translation_, translation_ + localAxes[2], Color.CornflowerBlue, DebugDrawDepth.Always);

            if (state.ObjectBounds.HasValue)
                DrawBoundsHelper(state.ObjectBounds.Value);

            switch (state.Mode)
            {
            case GizmoMode.Translation:
                Phantomhandle(translation_, -camera.Forward, Color.Transparent, GizmoAxis.XYZ);
                DrawQuad(translation_, 0.5f, false, displayAxes[0], displayAxes[2], Color.LimeGreen, GizmoAxis.XZ);
                DrawQuad(translation_, 0.5f, false, displayAxes[0], displayAxes[1], Color.CornflowerBlue, GizmoAxis.XY);
                DrawQuad(translation_, 0.5f, false, displayAxes[1], displayAxes[2], Color.Red, GizmoAxis.YZ);

                DrawAxis(translation_, displayAxes[0], displayAxes[1], displayAxes[2], 0.1f, 0.75f, Color.Red, GizmoAxis.X);
                DrawAxis(translation_, displayAxes[1], displayAxes[0], displayAxes[2], 0.1f, 0.75f, Color.LimeGreen, GizmoAxis.Y);
                DrawAxis(translation_, displayAxes[2], displayAxes[0], displayAxes[1], 0.1f, 0.75f, Color.CornflowerBlue, GizmoAxis.Z);
                break;
            case GizmoMode.Rotation:
                DrawCircle(translation_, Vector3.UnitX, displayAxes[1], displayAxes[2], Color.Red, GizmoAxis.X);
                DrawCircle(translation_, -Vector3.UnitY, displayAxes[0], displayAxes[2], Color.LimeGreen, GizmoAxis.Y);
                DrawCircle(translation_, Vector3.UnitZ, displayAxes[0], displayAxes[1], Color.CornflowerBlue, GizmoAxis.Z);
                DrawScreenCircle(translation_, ref displayAxes, 1.2f, Color.Magenta);
                break;
            case GizmoMode.Scale:
                PhantomScale(translation_, -camera.Forward, Color.Transparent, GizmoAxis.XYZ);
                DrawTriangle(translation_, 0.6f, false, localAxes[1], localAxes[2], Color.Red, GizmoAxis.YZ);
                DrawTriangle(translation_, 0.6f, false, localAxes[0], localAxes[2], Color.CornflowerBlue, GizmoAxis.XZ);
                DrawTriangle(translation_, 0.6f, false, localAxes[0], localAxes[1], Color.LimeGreen, GizmoAxis.XY);

                DrawScaleAxis(translation_, Vector3.UnitX, localAxes[0], localAxes[1], Color.Red, GizmoAxis.X);
                DrawScaleAxis(translation_, Vector3.UnitY, localAxes[1], localAxes[0], Color.LimeGreen, GizmoAxis.Y);
                DrawScaleAxis(translation_, Vector3.UnitZ, localAxes[2], localAxes[0], Color.CornflowerBlue, GizmoAxis.Z);
                break;
            }

            if (state.PickingRay.HasValue)
                state.LastRay = state.PickingRay.Value;
            else
                state.LastRay = null;
            state.CleanupState();

            if (state.Status == GizmoStatus.None)
            {
                state.Axis = GizmoAxis.None;
                state.RotationAngle = null;
                state.StartHitPosition = null;
            }

            if (!state.AltHeld && doneClone_)
                doneClone_ = false;

            if (state.Status != GizmoStatus.None || state.Status != state.OldStatus)
                state.SendChangeEvent(this);

            return state.Status != GizmoStatus.None || state.Status != state.OldStatus;
        }

        /// <summary>
        /// Draws and handles the bounds helper
        /// The bounds helper provides a view-based combined scale-translation widget to size to fit
        /// </summary>
        void DrawBoundsHelper(BoundingBox bounds)
        {
            int axis = BestViewAxis(camera.ViewMatrix);
            Vector3 bbMax = bounds.Max;
            Vector3 bbMin = bounds.Min;
            Vector3 halfPt = Vector3.Lerp(bbMin, bbMax, 0.5f);
            switch (axis)
            {
            case 0:
                // min-Y Z line, then max-Y
                DrawDottedLine(Vector3.Transform(new Vector3(halfPt.X, bbMin.Y, bbMin.Z), drawingTransform_), Vector3.Transform(new Vector3(halfPt.X, bbMin.Y, bbMax.Z), drawingTransform_), TransparentWhite);
                DrawDottedLine(Vector3.Transform(new Vector3(halfPt.X, bbMax.Y, bbMin.Z), drawingTransform_), Vector3.Transform(new Vector3(halfPt.X, bbMax.Y, bbMax.Z), drawingTransform_), TransparentWhite);
                // min-Z Y line, then max-Z
                DrawDottedLine(Vector3.Transform(new Vector3(halfPt.X, bbMin.Y, bbMin.Z), drawingTransform_), Vector3.Transform(new Vector3(halfPt.X, bbMax.Y, bbMin.Z), drawingTransform_), TransparentWhite);
                DrawDottedLine(Vector3.Transform(new Vector3(halfPt.X, bbMin.Y, bbMax.Z), drawingTransform_), Vector3.Transform(new Vector3(halfPt.X, bbMax.Y, bbMax.Z), drawingTransform_), TransparentWhite);

                // Z-sizers
                DrawSphereHandle(new BoundingSphere(Vector3.Transform(new Vector3(halfPt.X, halfPt.Y, bbMin.Z), drawingTransform_), 0.2f), TransBlue);
                DrawSphereHandle(new BoundingSphere(Vector3.Transform(new Vector3(halfPt.X, halfPt.Y, bbMax.Z), drawingTransform_), 0.2f), TransBlue);
                // Y-sizers
                DrawSphereHandle(new BoundingSphere(Vector3.Transform(new Vector3(halfPt.X, bbMin.Y, halfPt.Z), drawingTransform_), 0.2f), TransGreen);
                DrawSphereHandle(new BoundingSphere(Vector3.Transform(new Vector3(halfPt.X, bbMax.Y, halfPt.Z), drawingTransform_), 0.2f), TransGreen);

                break;
            case 1:
                // min-X Z line, then max-X
                DrawDottedLine(Vector3.Transform(new Vector3(bbMin.X, halfPt.Y, bbMin.Z), drawingTransform_), Vector3.Transform(new Vector3(bbMin.X, halfPt.Y, bbMax.Z), drawingTransform_), TransparentWhite);
                DrawDottedLine(Vector3.Transform(new Vector3(bbMax.X, halfPt.Y, bbMin.Z), drawingTransform_), Vector3.Transform(new Vector3(bbMax.X, halfPt.Y, bbMax.Z), drawingTransform_), TransparentWhite);
                // min-Z X line, then max-Z
                DrawDottedLine(Vector3.Transform(new Vector3(bbMin.X, halfPt.Y, bbMin.Z), drawingTransform_), Vector3.Transform(new Vector3(bbMax.X, halfPt.Y, bbMin.Z), drawingTransform_), TransparentWhite);
                DrawDottedLine(Vector3.Transform(new Vector3(bbMin.X, halfPt.Y, bbMax.Z), drawingTransform_), Vector3.Transform(new Vector3(bbMax.X, halfPt.Y, bbMax.Z), drawingTransform_), TransparentWhite);

                // Z-sizers
                DrawSphereHandle(new BoundingSphere(Vector3.Transform(new Vector3(halfPt.X, halfPt.Y, bbMin.Z), drawingTransform_), 0.2f), TransBlue);
                DrawSphereHandle(new BoundingSphere(Vector3.Transform(new Vector3(halfPt.X, halfPt.Y, bbMax.Z), drawingTransform_), 0.2f), TransBlue);
                // X-sizers
                DrawSphereHandle(new BoundingSphere(Vector3.Transform(new Vector3(bbMin.X, halfPt.Y, halfPt.Z), drawingTransform_), 0.2f), TransRed);
                DrawSphereHandle(new BoundingSphere(Vector3.Transform(new Vector3(bbMax.X, halfPt.Y, halfPt.Z), drawingTransform_), 0.2f), TransRed);
                break;
            case 2:
                // X
                DrawDottedLine(Vector3.Transform(new Vector3(bbMin.X, bbMin.Y, halfPt.Z), drawingTransform_), Vector3.Transform(new Vector3(bbMin.X, bbMax.Y, halfPt.Z), drawingTransform_), TransparentWhite);
                DrawDottedLine(Vector3.Transform(new Vector3(bbMax.X, bbMin.Y, halfPt.Z), drawingTransform_), Vector3.Transform(new Vector3(bbMax.X, bbMax.Y, halfPt.Z), drawingTransform_), TransparentWhite);
                // Y
                DrawDottedLine(Vector3.Transform(new Vector3(bbMin.X, bbMin.Y, halfPt.Z), drawingTransform_), Vector3.Transform(new Vector3(bbMax.X, bbMin.Y, halfPt.Z), drawingTransform_), TransparentWhite);
                DrawDottedLine(Vector3.Transform(new Vector3(bbMin.X, bbMax.Y, halfPt.Z), drawingTransform_), Vector3.Transform(new Vector3(bbMax.X, bbMax.Y, halfPt.Z), drawingTransform_), TransparentWhite);

                // X-sizers
                DrawSphereHandle(new BoundingSphere(Vector3.Transform(new Vector3(bbMin.X, halfPt.Y, halfPt.Z), drawingTransform_), 0.2f), TransRed);
                DrawSphereHandle(new BoundingSphere(Vector3.Transform(new Vector3(bbMax.X, halfPt.Y, halfPt.Z), drawingTransform_), 0.2f), TransRed);
                // Y-sizers
                DrawSphereHandle(new BoundingSphere(Vector3.Transform(new Vector3(halfPt.X, bbMin.Y, halfPt.Z), drawingTransform_), 0.2f), TransGreen);
                DrawSphereHandle(new BoundingSphere(Vector3.Transform(new Vector3(halfPt.X, bbMax.Y, halfPt.Z), drawingTransform_), 0.2f), TransGreen);
                break;
            }
        }

        bool DrawSphereHandle(BoundingSphere sphere, Color color)
        {
            if (state.PickingRay.HasValue)
            {
                if (state.PickingRay.Value.Intersects(sphere).HasValue)
                    color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());
            }
            //meshDrawer.DrawSphere(sphere, color);
            lineDrawer.DrawWireSphere(sphere.Center, sphere.Radius, color, DebugDrawDepth.Always);
            return false;
        }

        /// <summary>
        /// Finds the most opposing axis for the bounds resize plane
        /// </summary>
        int BestViewAxis(Matrix transform)
        {
            Vector3 transformedCamera = camera.Forward; //Vector3.TransformNormal(camera.Forward, transform);
            float xDot = Mathf.Abs(Vector3.Dot(transformedCamera, Vector3.TransformNormal(Vector3.UnitX, drawingTransform_)));
            float yDot = Mathf.Abs(Vector3.Dot(transformedCamera, Vector3.TransformNormal(Vector3.UnitY, drawingTransform_)));
            float zDot = Mathf.Abs(Vector3.Dot(transformedCamera, Vector3.TransformNormal(Vector3.UnitZ, drawingTransform_)));
            if (xDot > yDot && xDot > zDot)
                return 0;
            else if (yDot > xDot && yDot > zDot)
                return 1;
            return 2;
        }

        void GetAxes(ref Vector3[] axes, bool forceNonGlobal, bool forceGlobal = false)
        {
            if (!state.WorldSpace && !forceNonGlobal || forceGlobal)
            {
                axes[0] = Vector3.TransformNormal(Vector3.UnitX, drawingTransform_) * screenFactor_;
                axes[1] = Vector3.TransformNormal(Vector3.UnitY, drawingTransform_) * screenFactor_;
                axes[2] = Vector3.TransformNormal(Vector3.UnitZ, drawingTransform_) * screenFactor_;
            }
            else
            {
                axes[0] = Vector3.UnitX * screenFactor_;
                axes[1] = Vector3.UnitY * screenFactor_;
                axes[2] = Vector3.UnitZ * screenFactor_;
            }
        }

        void DrawCircle(Vector3 origin, Vector3 normal, Vector3 vtx, Vector3 vty, Color color, GizmoAxis axis)
        {
            const int size = 50;
            Vector3[] vertices = new Vector3[size];
            for (int i = 0; i < size; i++)
            {
                Vector3 vt;
                vt = vtx * Mathf.Cos((2 * Mathf.PI / size) * i);
                vt += vty * Mathf.Sin((2 * Mathf.PI / size) * i);
                vt += origin;
                vertices[i] = vt;
            }

            if (state.Axis == axis && state.OldStatus == GizmoStatus.MouseHit && state.RotationAngle.HasValue)
            {
                Vector3 surfaceNormal = Vector3.Normalize(Vector3.Cross(vtx, vty));
                Plane p = XNAExt.CreatePlane(origin, surfaceNormal);

                bool badSide = Vector3.Dot(surfaceNormal, camera.Forward) > 0.0f;
                Vector3 oldHit = state.LastRay.Value.Intersection(p);
                if (!state.StartHitPosition.HasValue)
                    state.StartHitPosition = oldHit;
                Vector3 newHit = state.PickingRay.Value.Intersection(p);

                var newHitPos = newHit;

                //if ((newHitPos - oldHit).Length() > 0.0001f)
                {
                    var v = oldHit - origin;
                    var u = newHitPos - origin;
                    v.Normalize();
                    u.Normalize();

                    var currentAxis = Vector3.Cross(u, v);
                    var mainAxis = Vector3.Cross(vtx, vty);// this.Transform.Transform(this.Axis.ToVector3D()).ToVector3();
                    float sign = -Vector3.Dot(mainAxis, currentAxis);
                    float theta = MathHelper.ToRadians((float)(Math.Sign(sign) * Math.Asin(currentAxis.Length()) / Math.PI * 180));

                    //if (badSide)
                    //    theta = -theta;
                    state.RotationAngle += theta;

                    if (theta != 0)
                    {
                        Quaternion quaternion = Quaternion.CreateFromAxisAngle(normal, theta);
                        if (this.State.WorldSpace || axis == GizmoAxis.Direction)
                            rotation_ = quaternion * this.rotation_;
                        else
                            rotation_ = this.rotation_ * quaternion;
                        transform_ = Matrix.CreateScale(scale_) * Matrix.CreateFromQuaternion(rotation_) * Matrix.CreateTranslation(translation_);
                        PostTransform();
                    }

                    Vector3 uVec = Vector3.Normalize(state.StartHitPosition.Value - origin) * vtx.Length();
                    Vector3 vVec = Vector3.Normalize(Vector3.Cross(uVec, mainAxis)) * vty.Length();
                    DrawCamembert(origin, uVec, vVec, -state.RotationAngle.Value, color);

                }
                color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());

                state.Status = state.MouseDown ? GizmoStatus.MouseHit : GizmoStatus.MouseOver;
                state.Axis = axis;
            }
            else if (state.OldStatus == GizmoStatus.None && state.Axis == GizmoAxis.None)
            {
                if (state.PickingRay.HasValue)
                {
                    Vector3 lookDir = Vector3.Cross(vtx, vty);
                    lookDir.Normalize();

                    if (CheckRotationPlane(origin, lookDir, vtx.Length(), state.PickingRay.Value, color))
                    {
                        color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());
                        state.Status = state.MouseDown ? GizmoStatus.MouseHit : GizmoStatus.MouseOver;
                        state.Axis = axis;
                        if (state.MouseDown)
                            state.RotationAngle = 0.0f;
                    }
                }
            }
            else if (state.Axis == axis)
                color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());

            // Line loop
            for (int i = 0; i < size - 1; ++i)
                lineDrawer.DrawLine(vertices[i], vertices[i + 1], color, DebugDrawDepth.Always);
            lineDrawer.DrawLine(vertices[size - 1], vertices[0], color, DebugDrawDepth.Always);
        }

        void DrawScreenCircle(Vector3 origin, ref Vector3[] axes, float radius, Color color)
        {
            Vector3 dir = translation_ - camera.Position;// .Translation();
            dir.Normalize();
            dir *= screenFactor_;

            Vector3 right = dir;
            right = Vector3.Cross(right, axes[1]);
            right.Normalize();

            Vector3 up = dir;
            up = Vector3.Cross(up, right);
            up.Normalize();

            right = dir;
            right = Vector3.Cross(right, up);
            right.Normalize();
            dir.Normalize();
            DrawCircle(translation_, dir, up * screenFactor_ * radius, right * screenFactor_ * radius, Color.Magenta, GizmoAxis.Direction);
        }

        static Vector3[] cone_mesh = new Vector3[51];

        /// <summary>
        /// Draw an arrow axis for translation
        /// </summary>
        /// <param name="origin">Centerpoint of the gizmo</param>
        /// <param name="handleAxis">axis of the gizmo handle</param>
        /// <param name="vtx">bitangent vector</param>
        /// <param name="vty">tangent vector</param>
        /// <param name="fct"></param>
        /// <param name="fct2"></param>
        /// <param name="color">color to draw</param>
        /// <param name="axis">Axis being rendered, activity will be recorded into gizmo state</param>
        void DrawAxis(Vector3 origin, Vector3 handleAxis, Vector3 vtx, Vector3 vty, float fct, float fct2, Color color, GizmoAxis axis)
        {
            // cone_mesh must be (N + 1) * 3 where n is the number of steps
            const int steps = 16;
            const float stepArc = 16.0f;
            for (int i = 0, j = 0; i <= steps; i++)
            {
                Vector3 pt;
                pt = vtx * Mathf.Cos(((2 * Mathf.PI) / stepArc) * i) * fct;
                pt += vty * Mathf.Sin(((2 * Mathf.PI) / stepArc) * i) * fct;
                pt += handleAxis * fct2;
                pt += origin;
                cone_mesh[j++] = pt;
                pt = vtx * Mathf.Cos(((2 * Mathf.PI) / stepArc) * (i + 1)) * fct;
                pt += vty * Mathf.Sin(((2 * Mathf.PI) / stepArc) * (i + 1)) * fct;
                pt += handleAxis * fct2;
                pt += origin;
                cone_mesh[j++] = pt;
                cone_mesh[j++] = origin + handleAxis;
            }

            if (state.OldStatus == GizmoStatus.MouseHit && state.Axis == axis)
            {
                Vector3 prev = state.LastRay.Value.Position + state.LastRay.Value.Direction;
                Vector3 cur = state.PickingRay.Value.Position + state.PickingRay.Value.Direction;

                var b = cur - prev;
                var ab = Vector3.Dot(handleAxis, b);
                var aa = Vector3.Dot(handleAxis, handleAxis);
                var ba = (ab / aa) * handleAxis;
                Vector3 newHit = prev + ba;

                var delta = newHit - prev;
                translation_ += delta * (camera.Position - translation_).Length() * GetDeltaMultiplier();
                transform_ = Matrix.CreateScale(scale_) * Matrix.CreateFromQuaternion(rotation_) * Matrix.CreateTranslation(translation_);
                PostTransform();

                state.Status = GizmoStatus.MouseHit;
                state.Axis = axis;

                lineDrawer.DrawLine(origin, origin + handleAxis * 10, color, DebugDrawDepth.Always);
                lineDrawer.DrawLine(origin, origin - handleAxis * 10, color, DebugDrawDepth.Always);

                color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());
            }
            else if (state.OldStatus == GizmoStatus.None && state.Axis == GizmoAxis.None)
            {
                if (state.PickingRay.HasValue)
                {
                    if (CheckAxis(origin, origin + handleAxis, 0.1f, state.PickingRay.Value))
                    {
                        color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());
                        state.Status = state.MouseDown ? GizmoStatus.MouseHit : GizmoStatus.MouseOver;
                        state.Axis = axis;
                    }
                }
            }
            else if (((int)axis & (int)state.Axis) != 0)
                color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());

            // Line
            lineDrawer.DrawLine(origin, origin + handleAxis, color, DebugDrawDepth.Always);
            // Triangle fan cone, culing disabled and flat shaded - so a bottom is pointless ATM
            for (int i = 2; i < cone_mesh.Length; ++i)
                lineDrawer.DrawTriangle(cone_mesh[0], cone_mesh[i - 1], cone_mesh[i], color, DebugDrawDepth.Always);
        }

        void QUAD(Vector3 A, Vector3 B, Vector3 C, Vector3 D, Color COLOR)
        {
            float box_size = 1.0f;
            lineDrawer.DrawTriangle(Vector3.Transform((A) * box_size, drawingTransform_), Vector3.Transform((B) * box_size, drawingTransform_), Vector3.Transform((C) * box_size, drawingTransform_), COLOR, DebugDrawDepth.Always);
            lineDrawer.DrawTriangle(Vector3.Transform((C) * box_size, drawingTransform_), Vector3.Transform((B) * box_size, drawingTransform_), Vector3.Transform((D) * box_size, drawingTransform_), COLOR, DebugDrawDepth.Always);
        }

        static readonly Vector3[] BoxVertices = {
            // Top
            new Vector3(-0.1f, 0.1f, 0.1f),     // front left
            new Vector3(-0.1f, 0.1f, -0.1f),    // back left
            new Vector3(0.1f, 0.1f, 0.1f),      // front right
            new Vector3(0.1f, 0.1f, -0.1f),     // back right
            // Bottom
            new Vector3(-0.1f, -0.1f, 0.1f),    // front left
            new Vector3(-0.1f, -0.1f, -0.1f),   // back left
            new Vector3(0.1f, -0.1f, 0.1f),     // front right
            new Vector3(0.1f, -0.1f, -0.1f),    // back right
        };

        void DrawScaleAxis(Vector3 origin, Vector3 naturalAxis, Vector3 m_axis, Vector3 vtx, Color color, GizmoAxis axis)
        {
            float box_size = 0.1f * screenFactor_;

            Vector3 box_pos = naturalAxis * m_axis.Length();// origin;
            //box_pos += naturalAxis;// * (screenFactor_ - (box_size * 0.5f));///*Vector3.Normalize(m_axis) */ (m_axis.Length() - (box_size * 0.5f));

            if (state.OldStatus == GizmoStatus.MouseHit && state.Axis == axis)
            {
                Vector3 prev = state.LastRay.Value.Position + state.LastRay.Value.Direction;
                Vector3 cur = state.PickingRay.Value.Position + state.PickingRay.Value.Direction;

                var b = cur - prev;
                var ab = Vector3.Dot(m_axis, b);
                var aa = Vector3.Dot(m_axis, m_axis);
                var ba = (ab / aa) * m_axis;
                Vector3 newHit = prev + ba;

                var delta = newHit - prev;
                switch (axis)
                {
                case GizmoAxis.X:
                    scale_.X += Vector3.Dot(delta, m_axis);
                    break;
                case GizmoAxis.Y:
                    scale_.Y += Vector3.Dot(delta, m_axis);
                    break;
                case GizmoAxis.Z:
                    scale_.Z += Vector3.Dot(delta, m_axis);
                    break;
                }
                transform_ = Matrix.CreateScale(scale_) * Matrix.CreateFromQuaternion(rotation_) * Matrix.CreateTranslation(translation_);
                PostTransform();

                color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());
                state.Status = state.MouseDown ? GizmoStatus.MouseHit : GizmoStatus.MouseOver;
                state.Axis = axis;
            }
            else if (state.OldStatus == GizmoStatus.None && state.Axis == GizmoAxis.None)
            {
                if (state.PickingRay.HasValue)
                {
                    if (CheckAxis(origin, origin + m_axis, 0.1f, state.PickingRay.Value))
                    {
                        color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());
                        state.Status = state.MouseDown ? GizmoStatus.MouseHit : GizmoStatus.MouseOver;
                        state.Axis = axis;
                    }
                }
            }
            else if ((axis & state.Axis) > 0)
            {
                color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());
            }

            lineDrawer.DrawLine(origin, origin + m_axis, color, DebugDrawDepth.Always);

            box_size = 1.0f;
            // Top
            QUAD(box_pos + BoxVertices[0] * screenFactor_, box_pos + BoxVertices[1] * screenFactor_, box_pos + BoxVertices[2] * screenFactor_, box_pos + BoxVertices[3] * screenFactor_, color);
            // Bottom
            QUAD(box_pos + BoxVertices[4] * screenFactor_, box_pos + BoxVertices[5] * screenFactor_, box_pos + BoxVertices[6] * screenFactor_, box_pos + BoxVertices[7] * screenFactor_, color);
            // Left
            QUAD(box_pos + BoxVertices[0] * screenFactor_, box_pos + BoxVertices[1] * screenFactor_, box_pos + BoxVertices[4] * screenFactor_, box_pos + BoxVertices[5] * screenFactor_, color);
            // Right
            QUAD(box_pos + BoxVertices[2] * screenFactor_, box_pos + BoxVertices[3] * screenFactor_, box_pos + BoxVertices[6] * screenFactor_, box_pos + BoxVertices[7] * screenFactor_, color);
            // Front
            QUAD(box_pos + BoxVertices[0] * screenFactor_, box_pos + BoxVertices[2] * screenFactor_, box_pos + BoxVertices[4] * screenFactor_, box_pos + BoxVertices[6] * screenFactor_, color);
            // Back
            QUAD(box_pos + BoxVertices[1] * screenFactor_, box_pos + BoxVertices[3] * screenFactor_, box_pos + BoxVertices[5] * screenFactor_, box_pos + BoxVertices[7] * screenFactor_, color);
        }

        static readonly Vector3[] camembertVerts = new Vector3[52];
        void DrawCamembert(Vector3 origin, Vector3 vtx, Vector3 vty, float ng, Color color)
        {
            int j = 0;
            camembertVerts[j++] = origin;
            for (int i = 0; i <= 50; i++)
            {
                Vector3 vt = new Vector3();
                vt = vtx * Mathf.Cos((ng / 50) * i);
                vt += vty * Mathf.Sin((ng / 50) * i);
                vt += origin;
                camembertVerts[j++] = vt;
            }

            Color transColor = new Color(color, 0.5f);

            // Triangle fan
            for (int i = 2; i < camembertVerts.Length; ++i)
                lineDrawer.DrawTriangle(camembertVerts[0], camembertVerts[i - 1], camembertVerts[i], transColor, DebugDrawDepth.Always);

            // Line loop?
            //ofMesh(OF_PRIMITIVE_LINE_LOOP, vertices).draw();
        }

        void Phantomhandle(Vector3 origin, Vector3 norm, Color color, GizmoAxis axis)
        {
            Plane hitPlane = XNAExt.CreatePlane(origin, Vector3.Normalize(norm));

            if (state.OldStatus == GizmoStatus.MouseHit && state.Axis == axis)
            {
                Vector3 oldPos = state.LastRay.Value.Intersection(hitPlane);
                Vector3 newPos = state.PickingRay.Value.Intersection(hitPlane);

                translation_ += (newPos - oldPos) * GetDeltaMultiplier();
                transform_ = Matrix.CreateScale(scale_) * Matrix.CreateFromQuaternion(rotation_) * Matrix.CreateTranslation(translation_);
                PostTransform();

                state.Status = state.MouseDown ? GizmoStatus.MouseHit : GizmoStatus.MouseOver;
                state.Axis = axis;
            }
            else if (state.PickingRay.HasValue && state.OldStatus != GizmoStatus.MouseHit)
            {
                // Debug
                //lineDrawer.DrawWireBox(new BoundingBox(origin - new Vector3(0.1f * screenFactor_), origin + new Vector3(0.1f * screenFactor_)), Color.Magenta);

                if ((state.PickingRay.Value.Intersection(hitPlane) - origin).Length() < 0.1f * screenFactor_)
                {
                    state.Status = state.MouseDown ? GizmoStatus.MouseHit : GizmoStatus.MouseOver;
                    state.Axis = axis;
                }
            }
        }

        void PhantomScale(Vector3 origin, Vector3 norm, Color color, GizmoAxis axis)
        {
            Plane hitPlane = XNAExt.CreatePlane(origin, Vector3.Normalize(norm));

            if (state.OldStatus == GizmoStatus.MouseHit && state.Axis == axis)
            {
                Vector3 oldPos = state.LastRay.Value.Intersection(hitPlane);
                Vector3 newPos = state.PickingRay.Value.Intersection(hitPlane);

                float factor = (newPos - origin).Length() - (oldPos - origin).Length();
                scale_.X += factor;
                scale_.Y += factor;
                scale_.Z += factor;

                transform_ = Matrix.CreateScale(scale_) * Matrix.CreateFromQuaternion(rotation_) * Matrix.CreateTranslation(translation_);
                PostTransform();

                state.Status = state.MouseDown ? GizmoStatus.MouseHit : GizmoStatus.MouseOver;
                state.Axis = axis;
            }
            else if (state.PickingRay.HasValue && (state.OldStatus == GizmoStatus.None && state.Axis == GizmoAxis.None))
            {
                //// Debug
                //lineDrawer.DrawWireBox(new BoundingBox(origin - new Vector3(0.1f * screenFactor_), origin + new Vector3(0.15f * screenFactor_)), Color.Magenta);
                if ((state.PickingRay.Value.Intersection(hitPlane) - origin).Length() < 0.1f * screenFactor_)
                {
                    state.Status = state.MouseDown ? GizmoStatus.MouseHit : GizmoStatus.MouseOver;
                    state.Axis = axis;
                }
            }
        }

        void DrawQuad(Vector3 origin, float size, bool is_selected, Vector3 m_axis_u, Vector3 m_axis_v, Color color, GizmoAxis axis, bool fake = false)
        {
            if (!fake)
                origin += ((m_axis_u + m_axis_v) * size) * 0.2f;
            m_axis_u *= 0.8f;
            m_axis_v *= 0.8f;

            Vector3[] pts = new Vector3[4];
            pts[0] = origin;
            pts[1] = origin + (m_axis_u * size);
            pts[2] = origin + (m_axis_u + m_axis_v) * size;
            pts[3] = origin + (m_axis_v * size);

            if (state.OldStatus == GizmoStatus.MouseHit && state.Axis == axis)
            {
                Plane p = XNAExt.CreatePlane(origin, Vector3.Normalize(Vector3.Cross(m_axis_u, m_axis_v)));
                Vector3 oldPt = state.LastRay.Value.Intersection(p);
                Vector3 newPt = state.PickingRay.Value.Intersection(p);

                translation_ += (newPt - oldPt) * GetDeltaMultiplier();
                transform_ = Matrix.CreateScale(scale_) * Matrix.CreateFromQuaternion(rotation_) * Matrix.CreateTranslation(translation_);
                PostTransform();

                state.Status = GizmoStatus.MouseHit;
                state.Axis = axis;
                color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());
            }
            else if (state.PickingRay.HasValue && (state.OldStatus == GizmoStatus.None && state.Axis == GizmoAxis.None))
            {
                if (state.PickingRay.Value.Intersects(pts[0], pts[1], pts[2]) || state.PickingRay.Value.Intersects(pts[2], pts[1], pts[0]) ||
                    state.PickingRay.Value.Intersects(pts[0], pts[2], pts[3]) || state.PickingRay.Value.Intersects(pts[3], pts[2], pts[0]))
                {
                    color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());
                    state.Status = state.MouseDown ? GizmoStatus.MouseHit : GizmoStatus.MouseOver;
                    state.Axis = axis;
                }
            }
            else if (axis == state.Axis || state.Axis == GizmoAxis.XYZ)
            {
                color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());
            }

            Color transColor = new Color(color, is_selected ? 0.8f : 0.4f);

            if (!fake)
            {
                // Triangle fan
                for (int i = 2; i < pts.Length; ++i)
                    lineDrawer.DrawTriangle(pts[0], pts[i - 1], pts[i], transColor, DebugDrawDepth.Always);

                // Line loop border
                transColor = new Color(color, is_selected ? 1.0f : 0.8f);
                lineDrawer.DrawLine(pts[0], pts[1], transColor, DebugDrawDepth.Always);
                lineDrawer.DrawLine(pts[1], pts[2], transColor, DebugDrawDepth.Always);
                lineDrawer.DrawLine(pts[2], pts[3], transColor, DebugDrawDepth.Always);
                lineDrawer.DrawLine(pts[3], pts[0], transColor, DebugDrawDepth.Always);
            }
        }

        void DrawTriangle(Vector3 origin, float size, bool is_selected, Vector3 m_axis_u, Vector3 m_axis_v, Color color, GizmoAxis axis)
        {
            origin += ((m_axis_u + m_axis_v) * size) * 0.2f;
            m_axis_u *= 0.8f;
            m_axis_v *= 0.8f;

            Vector3[] pts = new Vector3[3];
            pts[0] = origin;
            pts[1] = (m_axis_u * size) + origin;
            pts[2] = (m_axis_v * size) + origin;

            color = new Color(color, is_selected ? 0.8f : 0.4f);

            if (state.OldStatus == GizmoStatus.MouseHit && state.Axis == axis)
            {
                Plane p = XNAExt.CreatePlane(origin, Vector3.Normalize(Vector3.Cross(m_axis_u, m_axis_v)));
                Vector3 oldPt = state.LastRay.Value.Intersection(p);
                Vector3 newPt = state.PickingRay.Value.Intersection(p);
                Vector3 delta = (newPt - oldPt) * 0.5f;
                float amount = delta.MaxElement();

                float multiplier = GetDeltaMultiplier();
                if ((axis & GizmoAxis.X) > 0)
                    scale_.X += delta.X;
                if ((axis & GizmoAxis.Y) > 0)
                    scale_.Y += delta.Y;
                if ((axis & GizmoAxis.Z) > 0)
                    scale_.Z += delta.Z;

                transform_ = Matrix.CreateScale(scale_) * Matrix.CreateFromQuaternion(rotation_) * Matrix.CreateTranslation(translation_);
                PostTransform();

                color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());
                state.Status = state.MouseDown ? GizmoStatus.MouseHit : GizmoStatus.MouseOver;
                state.Axis = axis;
            }
            else if (state.PickingRay.HasValue && (state.OldStatus != GizmoStatus.MouseHit || state.Axis == GizmoAxis.None))
            {
                if ((state.PickingRay.Value.Intersects(pts[0], pts[1], pts[2]) || state.PickingRay.Value.Intersects(pts[2], pts[1], pts[0])))
                {
                    color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());
                    state.Status = state.MouseDown ? GizmoStatus.MouseHit : GizmoStatus.MouseOver;
                    state.Axis = axis;
                }
            }
            if (state.Axis == GizmoAxis.XYZ)
                color = Color.Lerp(color, Color.Gold, state.GetTimeCurve());

            // Triangles
            Color transColor = new Color(color, is_selected ? 0.8f : 0.4f);
            for (int i = 0; i < pts.Length; i += 3)
                lineDrawer.DrawTriangle(pts[i], pts[i + 1], pts[i + 2], color, DebugDrawDepth.Always);

            // Line loop
            transColor = new Color(color, is_selected ? 1.0f : 0.8f);
            lineDrawer.DrawLine(pts[0], pts[1], transColor, DebugDrawDepth.Always);
            lineDrawer.DrawLine(pts[1], pts[2], transColor, DebugDrawDepth.Always);
            lineDrawer.DrawLine(pts[0], pts[2], transColor, DebugDrawDepth.Always);
        }

        static BoundingBox axisBox = new BoundingBox();
        bool CheckAxis(Vector3 start, Vector3 end, float tailCutoff, Ray ray)
        {
            bool hit = ray.RayLineDistance(start, end) < 0.1f * screenFactor_;

            //BoundingSphere sphere = new BoundingSphere(start, 0.1f * screenFactor_);
            //for (int i = 0; i < 8; ++i)
            //{
            //    //// Debug drawing
            //    //lineDrawer.DrawWireSphere(sphere, Color.Magenta);
            //    sphere.Center += (end - start) * (1.0f / 8);
            //    if (ray.Intersects(sphere).HasValue)
            //    {
            //        hit = true;
            //        break;
            //    }
            //}

            // Debug drawing, here we can see the fitment around the arrows
            //lineDrawer.DrawWireBox(axisBox, Color.Magenta);

            if (hit)//hitDist.HasValue)
            {
                //Vector3 hitPt = ray.Position + ray.Direction * hitDist.Value;
                //if ((hitPt - start).LengthSquared() < 0.15f)
                //    return false;
                //if (XNAExt.CreatePlane(start, dir).DotCoordinate(hitPt) > 0.0f)
                return true;
            }
            return false;
        }

        bool CheckRotationPlane(Vector3 origin, Vector3 normal, float factor, Ray ray, Color c)
        {
            Plane m_plane = XNAExt.CreatePlane(origin, normal);
            float? dist = ray.Intersects(m_plane);
            if (!dist.HasValue)
                return false;

            Vector3 intersection = ray.Position + ray.Direction * dist.Value;
            Vector3 df = intersection - origin;
            //df /= screenFactor_;

            float dflen = df.Length();

            if ((dflen > factor - 0.1f * screenFactor_) && (dflen < factor + 0.1f * screenFactor_))
                return true;

            return false;
        }

        bool RayQuad(Ray ray, Vector3 origin, float size, Vector3 axis_u, Vector3 axis_v)
        {
            origin += ((axis_u + axis_v) * size) * 0.2f;
            axis_u *= 0.8f;
            axis_v *= 0.8f;

            Vector3[] pts = {
                origin,
                origin + (axis_u * size),
                origin + (axis_u + axis_v) * size,
                origin + (axis_v * size)
            };

            float hitDistance = 0.0f;
            Vector2 coords = new Vector2();
            return ray.Intersects(pts[0], pts[1], pts[2], ref hitDistance, ref coords) || ray.Intersects(pts[0], pts[2], pts[3], ref hitDistance, ref coords);
            //float hitDistance = Math.Min(ray.HitDistance(pts[0], pts[1], pts[2]), ray.HitDistance(pts[0], pts[2], pts[3]));
            //if (hitDistance == Urho3D::M_INFINITY)
            //                return false;
            //            return true;
        }

        void DrawDottedLine(Vector3 a, Vector3 b, Color c)
        {
            const float lineCt = 10.0f;

            float distance = (b - a).Length();
            float stepSize = distance / ((lineCt * 2) + 1);

            Vector3 cur = a;
            Vector3 dir = (b - a);
            dir.Normalize();
            for (int i = 0; i < lineCt + 1; ++i)
            {
                lineDrawer.DrawLine(cur, cur + dir * stepSize, c, DebugDrawDepth.Always);
                cur = cur + dir * (stepSize * 2);
            }
        }

        void ComputeScreenFactor()
        {
            screenFactor_ = (translation_ - camera.Position).Length() * 0.15f;
        }

        IOCDependency<ApplicationState> appState = new IOCDependency<ApplicationState>();
        float GetDeltaMultiplier()
        {
            return appState.Object.Keyboard.IsKeyDown(Keys.LeftShift) || appState.Object.Keyboard.IsKeyDown(Keys.RightShift) ?
                viewportSettings.Object.FastMovementSpeed : viewportSettings.Object.BaseMovementSpeed;
        }
    }
}
