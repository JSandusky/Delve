using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DelveLib;
using Microsoft.Xna.Framework;

namespace Delve.Objects
{
    public class SceneObjectAnimation : DelveLib.VisualAnimation3D
    {
        float duration_;
        float currentTime_ = 0.0f;

        public Vector3 startPos_;
        public Quaternion startRot_;
        public Vector3 startScale_;

        public Vector3 endPos_;
        public Quaternion endRot_;
        public Vector3 endScale_;

        public SceneObjectAnimation(Matrix end, float duration)
        {
            duration_ = duration_;
            end.Decompose(out endScale_, out endRot_, out endScale_);
        }

        public override void Prepare(Visual3D owner)
        {
            GameObject obj = owner as GameObject;
            obj.Transform.Decompose(out startScale_, out startRot_, out startPos_);
        }

        public override bool Update(Visual3D target, float timeStep)
        {
            GameObject obj = target as GameObject;
            currentTime_ += timeStep;
            float fract = currentTime_ / duration_;

            if (fract >= 1.0f)
            {
                obj.Position = endPos_;
                obj.Rotation = endRot_;
                obj.Scale = endScale_;
                return true;
            }
            Vector3 newPos = Vector3.Lerp(startPos_, endPos_, fract);
            Vector3 newScale = Vector3.Lerp(startScale_, endScale_, fract);
            Quaternion newRot = Quaternion.Slerp(startRot_, endRot_, fract);
            obj.Position = newPos;
            obj.Rotation = newRot;
            obj.Scale = newScale;
            return false;
        }

        public override void ForceFinished(Visual3D target)
        {
            GameObject obj = target as GameObject;
            obj.Position = endPos_;
            obj.Rotation = endRot_;
            obj.Scale = endScale_;
        }
    }

    public class CycleSceneObjectAnimation : DelveLib.VisualAnimation3D
    {
        float duration_;
        float currentTime_ = 0.0f;

        public Vector3 startPos_;
        public Quaternion startRot_;
        public Vector3 startScale_;

        public Vector3 endPos_;
        public Quaternion endRot_;
        public Vector3 endScale_;

        public CycleSceneObjectAnimation(Matrix end, float duration)
        {
            duration_ = duration_;
            end.Decompose(out endScale_, out endRot_, out endScale_);
        }

        public override void Prepare(Visual3D owner)
        {
            GameObject obj = owner as GameObject;
            obj.Transform.Decompose(out startScale_, out startRot_, out startPos_);
        }

        public override bool Update(Visual3D target, float timeStep)
        {
            GameObject obj = target as GameObject;
            currentTime_ += timeStep;
            float fract = PingPongMod(currentTime_, duration_);
            Vector3 newPos = Vector3.Lerp(startPos_, endPos_, fract);
            Vector3 newScale = Vector3.Lerp(startScale_, endScale_, fract);
            Quaternion newRot = Quaternion.Slerp(startRot_, endRot_, fract);
            obj.Position = newPos;
            obj.Rotation = newRot;
            obj.Scale = newScale;
            return false;
        }

        public override void ForceFinished(Visual3D target)
        {
            GameObject obj = target as GameObject;
            obj.Position = endPos_;
            obj.Rotation = endRot_;
            obj.Scale = endScale_;
        }

        float PingPongMod(float x, float mod)
        {
            x = x % (mod * 2.0f);
            return x >= mod ? (2.0f * mod - x) : x;
        }
    }
}
