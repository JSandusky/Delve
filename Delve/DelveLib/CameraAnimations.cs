using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace DelveLib
{
    public class CameraAnimation : VisualAnimation3D
    {
        Vector3 initialPos_;
        Vector3 initialLook_;

        Vector3 posTarget_;
        Vector3 lookAtTarget_;
        float duration_;

        float currentTime_ = 0.0f;

        public CameraAnimation(Vector3 positionTarget, Vector3 lookAtPoint, float duration)
        {
            posTarget_ = positionTarget;
            lookAtTarget_ = lookAtPoint;
            duration_ = duration;
        }

        public override void Prepare(Visual3D owner)
        {
            Camera cam = owner as Camera;
            float targetDistance = (lookAtTarget_ - posTarget_).Length();
            initialPos_ = cam.Position;
            initialLook_ = cam.Position + cam.Forward * targetDistance; // Equalize src distance so things stay sane
        }

        public override void ForceFinished(Visual3D target)
        {
            Camera cam = target as Camera;
            cam.LookAtPoint(posTarget_, lookAtTarget_);
        }

        public override bool Update(Visual3D target, float timeStep)
        {
            currentTime_ += timeStep;
            float fract = currentTime_ / duration_;

            Camera cam = target as Camera;
            if (fract >= 1.0f)
            {
                cam.LookAtPoint(posTarget_, lookAtTarget_);
                return true;
            }

            Vector3 animPos = Vector3.Lerp(initialPos_, posTarget_, fract);
            Vector3 lookPt = Vector3.Lerp(initialLook_, lookAtTarget_, fract);
            cam.LookAtPoint(animPos, lookPt);
            return false;
        }
    }

    public class CameraPositionAnimation : VisualAnimation3D
    {
        Vector3 initial_;
        Vector3 target_;
        float duration_;
        float currentTime_ = 0.0f;

        public CameraPositionAnimation(Vector3 target, float duration)
        {
            duration_ = duration;
            target_ = target;
        }

        public override void Prepare(Visual3D owner)
        {
            initial_ = (owner as Camera).Position;
        }

        public override void ForceFinished(Visual3D target)
        {
            (target as Camera).Position = target_;
        }

        public override bool Update(Visual3D target, float timeStep)
        {
            currentTime_ += timeStep;
            float fract = currentTime_ / duration_;

            Camera cam = target as Camera;
            if (fract >= 1.0f)
            {
                cam.Position = target_;
                return true;
            }
            Vector3 newPos = Vector3.Lerp(initial_, target_, fract);
            cam.Position = newPos;
            return false;
        }
    }

    public class CameraLookAtAnimation : VisualAnimation3D
    {
        Vector3 initialLook_;
        Vector3 lookAtTarget_;
        float duration_;

        float currentTime_ = 0.0f;

        public CameraLookAtAnimation(Vector3 lookAtPoint, float duration)
        {
            lookAtTarget_ = lookAtPoint;
            duration_ = duration;
        }

        public override void Prepare(Visual3D owner)
        {
            Camera cam = owner as Camera;
            float targetDistance = (lookAtTarget_ - cam.Position).Length();
            initialLook_ = cam.Position + cam.Forward * targetDistance; // Equalize src distance so things stay sane
        }

        public override void ForceFinished(Visual3D target)
        {
            Camera cam = target as Camera;
            cam.LookAtPoint(lookAtTarget_);
        }

        public override bool Update(Visual3D target, float timeStep)
        {
            currentTime_ += timeStep;
            float fract = currentTime_ / duration_;

            Camera cam = target as Camera;
            if (fract >= 1.0f)
            {
                cam.LookAtPoint(lookAtTarget_);
                return true;
            }
            Vector3 lookPt = Vector3.Lerp(initialLook_, lookAtTarget_, fract);
            cam.LookAtPoint(lookPt);
            return false;
        }
    }
}
