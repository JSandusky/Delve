using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DelveLib
{
    public class Camera : Visual3D
    {
        private Vector3 position_ = new Vector3(0,0,0);
        private Vector3 direction_ = new Vector3(0, 1, 0);
        private Vector3 upDir_ = new Vector3(0,1,0);

        private Matrix _projectionMatrix;
        private Matrix _viewMatrix;

        public Matrix ProjectionMatrix { get { return _projectionMatrix; } set { _projectionMatrix = value; } }
        public Matrix ViewMatrix { get { return _viewMatrix; } set { _viewMatrix = value; } }

        public Matrix CombinedMatrix { get { return _viewMatrix * _projectionMatrix; } }

        public Vector3 Position { get { return position_; } set { position_ = value; UpdateMatrix(); } }
        public Vector3 UpDir { get { return upDir_; } set { upDir_ = value; } }

        public Vector3 Forward { get { return direction_; } set { direction_ = value; direction_.Normalize(); UpdateMatrix(); } }
        public Vector3 Up { get { return Vector3.Transform(upDir_, _viewMatrix.Rotation); } }
        public Vector3 Right { get { return Vector3.Cross(upDir_, direction_); } }

        Vector2 orthoScaling_ = new Vector2(1, 1);
        public Vector2 OrthoScaling { get { return orthoScaling_; } set { orthoScaling_ = value; } }

        public Camera(GraphicsDevice device, float fov)
        {
            SetToPerspective(device, fov);
            LookAtPoint(Vector3.Zero);
        }

        public static Camera CreateOrtho(GraphicsDevice device)
        {
            Camera ret = new Camera(device, 45);
            ret.LookAtPoint(new Vector3(0, 0, 1), Vector3.Zero);
            ret.SetToOrthoGraphic(device, 0, 0);
            return ret;
        }

        public void SetToPerspective(GraphicsDevice graphicsDevice, float fov)
        {
            _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(fov),
                (float)graphicsDevice.Viewport.Width /
                (float)graphicsDevice.Viewport.Height,
                0.1f, 500.0f) * Matrix.CreateScale(-1.0f, 1.0f, 1.0f);
        }

        public void SetToOrthoGraphic(GraphicsDevice graphicsDevice, float width, float height)
        {
            width = (float)graphicsDevice.Viewport.Width * OrthoScaling.X;
            height = (float)graphicsDevice.Viewport.Height * OrthoScaling.Y;
            //TODO: hard-coded units should go away
            _projectionMatrix = Matrix.CreateOrthographic(width / 512, height / 512, 0.1f, 100.0f);
            //_projectionMatrix = Matrix.CreateOrthographicOffCenter(-(height/8),width / 64, -(height / 128), height / 32, 0.1f, 100.0f);
        }

        public Ray GetPickRay(Viewport viewport, float x, float y)
        {
            Vector3 nearPoint = new Vector3(x, y, 0);
            Vector3 farPoint = new Vector3(x, y, 0.2f);
            nearPoint = viewport.Unproject(nearPoint, ProjectionMatrix, ViewMatrix, Matrix.Identity);
            farPoint = viewport.Unproject(farPoint, ProjectionMatrix, ViewMatrix, Matrix.Identity);
            Vector3 dir = (farPoint - nearPoint);
            dir.Normalize();
            return new Ray(nearPoint, dir);
        }

        public Vector2 GetViewportPos(Viewport viewport, float x, float y)
        {
            Vector3 nearPoint = new Vector3(x, y, 0);
            nearPoint = viewport.Unproject(nearPoint, ProjectionMatrix, ViewMatrix, Matrix.Identity);
            return nearPoint.XY();
        }

        public void LookAtDir(Vector3 dir)
        {
            direction_ = dir;
            direction_.Normalize();
            UpdateMatrix();
        }

        public void LookAtDir(Vector3 pos, Vector3 dir)
        {
            position_ = pos;
            LookAtDir(dir);
        }

        public void LookAtPoint(Vector3 tgt)
        {
            direction_ = (tgt - position_);
            direction_.Normalize();
            UpdateMatrix();
        }

        public void LookAtPoint(Vector3 pt, Vector3 tgt)
        {
            position_ = pt;
            LookAtPoint(tgt);
        }

        public void OrbitAround(Vector3 orbitAround, Vector2 delta)
        {
            TurnView(delta.X, delta.Y);
            Vector3 d = (position_ - orbitAround);
            position_ = orbitAround - Vector3.Transform(new Vector3(0, 0, d.Length()), _viewMatrix.Rotation);
            LookAtPoint(orbitAround);
        }

        public void TurnView(float yaw, float pitch)
        {
            Yaw(yaw);
            Pitch(pitch);
        }

        public void Yaw(float amount)
        {
            LookAtDir(Vector3.Transform(Forward, Matrix.CreateFromAxisAngle(UpDir, MathHelper.ToRadians(amount))));
        }

        Vector3 GetPitchVector(float amount)
        {
            var left = Vector3.Cross(UpDir, Forward);
            left.Normalize();
            return Vector3.Transform(Forward, Matrix.CreateFromAxisAngle(left, MathHelper.ToRadians(amount)));
        }

        public void Pitch(float amount)
        {            
            LookAtDir(GetPitchVector(amount));
        }

        public void OrbitAround(Vector3 orbitPt, float x, float y)
        {
            Yaw(x);
            float val = Math.Abs(Vector3.Dot(GetPitchVector(y), Vector3.UnitY));
            if (val < 0.999f)
                Pitch(y);
            position_ = orbitPt - direction_ * (orbitPt - position_).Length();
            UpdateMatrix();
        }

        void UpdateMatrix()
        {
            Vector3 dir = position_ + direction_;
            Matrix.CreateLookAt(ref position_, ref dir, ref upDir_, out _viewMatrix);
        }
    }
}
