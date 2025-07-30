using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DelveLib
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Particle : IVertexType
    {
        public Vector4 position_; // W contains texture index
        public Vector4 velocity_; // W contains time
        public Vector2 size_;
        public Color color_; // Particle color

        static VertexDeclaration decl = new VertexDeclaration(new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
                new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(32, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
                new VertexElement(40, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            });
        public VertexDeclaration VertexDeclaration { get { return decl; } }
    }

    public enum LookAtMode
    {
        CameraXYZ,
        CameraPole,
        Velocity
    }

    public enum EmissionShape
    {
        Sphere,
        Box,
        Ring,
        Surface
    }

    public class ParticleEmitterData
    {
        public int minCount_;
        public int maxCount_;
        public float emitDurationMin_;
        public float emitDurationMax_;
        public float minLife_;
        public float maxLife_;
        public Vector3 minInitialVelocity_;
        public Vector3 maxInitialVelocity_;
        public Vector3 constantForce_;
        public Vector2 minSize_;
        public Vector2 maxSize_;
        public Vector3 emitterSize_;
        public EmissionShape shape_;
        public LookAtMode lookAt_;
        public float dampingForce_;

        internal Vector3 GetEmissionPosition(System.Random r)
        {
            if (shape_ == EmissionShape.Box)
                return XNAExt.Random(r, -emitterSize_, emitterSize_);
            else if (shape_ == EmissionShape.Ring)
            {
                Vector3 vec = XNAExt.Random(r, -emitterSize_, emitterSize_);
                vec.Y = 0;
                vec.Normalize();
                return vec * emitterSize_;
            }
            else if (shape_ == EmissionShape.Surface)
            {

            }
            else
            {
                Vector3 vec = XNAExt.Random(r, -emitterSize_, emitterSize_);
                vec.Normalize();
                return vec * emitterSize_;
            }
            return new Vector3(0, 0, 0);
        }
    }

    public class ParticleEmitter
    {
        public ParticleEmitterData data_;
        public bool isEmitting_ = true;
        public float emittingTime_ = 0.0f;
        public Vector3 Position = Vector3.Zero;
        public Vector3 MinBounds = Vector3.Zero;
        public Vector3 MaxBounds = Vector3.Zero;

        public StructArray<Particle> particles_;

        public bool IsFinished { get { return isEmitting_ == false; } }

        public ParticleEmitter(ParticleEmitterData data)
        {
            data_ = data;
        }

        public void Start(System.Random r)
        {
            if (particles_ == null)
                particles_ = new StructArray<Particle>(data_.maxCount_, Marshal.SizeOf<Particle>());
            if (particles_.Capacity != data_.maxCount_)
                particles_.Resize(data_.maxCount_);
            particles_.Count = 0;
            emittingTime_ = (float)(data_.emitDurationMin_ + (r.NextDouble() * (data_.emitDurationMax_ - data_.emitDurationMin_)));
            isEmitting_ = true;
        }

        public void Update(float td, System.Random r)
        {
            for (int i = 0; i < particles_.Count; ++i)
            {
                particles_.items_[i].velocity_.X += (td * data_.constantForce_.X);
                particles_.items_[i].velocity_.Y += (td * data_.constantForce_.Y);
                particles_.items_[i].velocity_.Z += (td * data_.constantForce_.Z);

                particles_.items_[i].velocity_.X += -data_.dampingForce_ * (td * particles_.items_[i].velocity_.X);
                particles_.items_[i].velocity_.Y += -data_.dampingForce_ * (td * particles_.items_[i].velocity_.Y);
                particles_.items_[i].velocity_.Z += -data_.dampingForce_ * (td * particles_.items_[i].velocity_.Z);

                particles_.items_[i].velocity_.W -= td;
            }

            for (int i = 0; i < particles_.Count; ++i)
                if (particles_.items_[i].velocity_.W <= 0)
                    particles_.Remove(i);

            for (int i = 0; i < particles_.Count; ++i)
            {
                particles_.items_[i].position_.X += particles_.items_[i].velocity_.X;
                particles_.items_[i].position_.X += particles_.items_[i].velocity_.Y;
                particles_.items_[i].position_.X += particles_.items_[i].velocity_.Z;
            }

            isEmitting_ = particles_.Count > 0 && emittingTime_ > 0;
            if (particles_.Count < data_.maxCount_ && isEmitting_)
            {
                int idx = particles_.GetNextIndex();
                if (idx != -1)
                {
                    Vector3 vel = XNAExt.Random(r, data_.minInitialVelocity_, data_.maxInitialVelocity_);
                    Vector3 newPos = data_.GetEmissionPosition(r);

                    particles_.items_[idx].position_.X = newPos.X;
                    particles_.items_[idx].position_.Y = newPos.Y;
                    particles_.items_[idx].position_.Z = newPos.Z;

                    particles_.items_[idx].velocity_.X = vel.X;
                    particles_.items_[idx].velocity_.Y = vel.Y;
                    particles_.items_[idx].velocity_.Z = vel.Z;
                    particles_.items_[idx].velocity_.W = (float)(data_.minLife_ + (r.NextDouble() * (data_.maxLife_ - data_.minLife_)));
                }
            }

            MinBounds.X = MinBounds.Y = MinBounds.Z = float.MaxValue;
            MaxBounds.X = MaxBounds.Y = MaxBounds.Z = float.MinValue;
            for (int i = 0; i < particles_.Count && isEmitting_; ++i)
            {
                MinBounds = Vector3.Min(MinBounds, particles_.items_[i].position_.XYZ());
                MaxBounds = Vector3.Max(MaxBounds, particles_.items_[i].position_.XYZ());
            }

            // Expand bounds to encapsulate the maximum possible
            float maxDim = Math.Max(data_.maxSize_.X, data_.maxSize_.Y) * 0.5f;
            MinBounds.X -= maxDim;
            MinBounds.Y -= maxDim;
            MinBounds.Z -= maxDim;
            MaxBounds.X += maxDim;
            MaxBounds.Y += maxDim;
            MaxBounds.Z += maxDim;

            emittingTime_ -= td;
        }

        DynamicVertexBuffer vertexBuffer_;
        public void Draw(GraphicsDevice device, Effect renderEffect)
        {
            if (particles_.Count == 0)
                return;

            if (vertexBuffer_ == null)
                vertexBuffer_ = new DynamicVertexBuffer(device, typeof(Particle), particles_.Capacity, BufferUsage.WriteOnly);

            vertexBuffer_.SetData(particles_.items_, 0, particles_.Count);
            renderEffect.CurrentTechnique.Passes[0].Apply();
            device.SetVertexBuffer(vertexBuffer_);
            device.DrawPrimitives(PrimitiveType.PointList, 0, particles_.Count);
        }
    }

    public struct VerletParticle
    {
        int qX_;
        int qY_;
        public Vector3 position_;
        public Vector3 oldPosition_;
        public float mass_;
        public float invMass_; // cached
        public float drag_;
        public float bounce_;
        public float radius_;
        public uint mask_;
        public bool pinned_;
        public bool collided_;

        public Vector3 Velocity { get { return position_ - oldPosition_; } }
        public Vector3 VelocityDir { get { return Vector3.Normalize(position_ - oldPosition_); } }

        public void SetVelocity(Vector3 velVec)
        {
            oldPosition_ = position_ - velVec;
        }

        public void AddVelocity(Vector3 velVec)
        {
            oldPosition_ -= velVec;
        }

        public void UpdateMass()
        {
            invMass_ = 1.0f / mass_;
        }

        static Vector3 gravity = new Vector3(0, -9.7f, 0);
        public void Update(float td)
        {
            if (td == 0)
                return;
            if (pinned_)
            {
                oldPosition_ = position_;
                return;
            }
            Vector3 curPos = position_;
            MoveBy(Velocity, false);
            oldPosition_ = curPos;
            qX_ = (int)position_.X / 16;
            qY_ = (int)position_.Y / 16;
            if (Velocity.Length() > 10)
                SetVelocity(VelocityDir * 10);
            if (!collided_)
                position_ += gravity * td * 1.5f;
            collided_ = false;
        }

        public void MoveBy(Vector3 v, bool retainVelocity)
        {
            if (pinned_)
                return;
            position_ += v;
            if (!position_.X.IsFinite())
                throw new Exception("dead");
            if (retainVelocity)
                oldPosition_ += v;
        }

        public void ConstrainTo(ref VerletParticle rhs, float springLen, float springStrength, float maxForce)
        {
            //if (collided_ || rhs.collided_)
            //    return;
            var pos1 = position_;
            var pos2 = rhs.position_;
            var dist = Vector3.Distance(position_, rhs.position_);
            
            float force = (springLen - dist)*0.5f;
            if (Math.Abs(force) < 0.01f)
                return;
            
            var direction = Vector3.Normalize(position_ - rhs.position_);
            var acceleration = direction * force;
            
            MoveBy(acceleration, false);
            rhs.MoveBy(-acceleration, false);

            //Vector3 delta = rhs.position_ - position_;
            //float dLen2 = delta.LengthSquared();
            //float dLen = (float)Math.Sqrt(dLen2);
            //float force = dLen > 0 ? (springStrength * (dLen - springLen)) / (dLen * (invMass_ + rhs.invMass_)) : 0;
            //force = MathHelper.Clamp(force, 0, maxForce);
            //delta = Vector3.Normalize(delta);
            //Vector3 dForce = delta * force;
            //this.MoveBy(dForce * invMass_, false);
            //rhs.MoveBy(-dForce * rhs.invMass_, false);
        }

        public void OrbitAround(Vector3 pt, Vector3 axis, float dist2, float orbitalVelocity)
        {
            Vector3 diff = position_ - pt;
            float distFactor = 1.0f - (diff.LengthSquared() / dist2);
            if (distFactor > 0)
            {
                Quaternion rot = Quaternion.CreateFromAxisAngle(axis, orbitalVelocity * distFactor * invMass_);
                position_ = Vector3.Transform(position_, rot);
            }
        }

        public void AttractTo(Vector3 pt, float dist2, float power)
        {
            Vector3 diff = position_ - pt;
            float distFactor = 1.0f - (diff.LengthSquared() / dist2);
            if (distFactor > 0)
                position_ += diff * (-distFactor * power * invMass_);
        }

        public void CollideWith(ref VerletParticle rhs)
        {
            //??if (Math.Abs(rhs.qX_ - qX_) > 3 || Math.Abs(rhs.qY_ - qY_) > 3)
            //??    return;
            float restLen = rhs.radius_ + radius_;
            Vector3 delta = rhs.position_ - position_;
            float dLen2 = delta.LengthSquared();
            if (dLen2 > restLen * restLen)
                return;

            float dLen = (float)Math.Sqrt(dLen2);
            float force = (dLen - restLen) / (dLen * (invMass_ + rhs.invMass_));
            if (!force.IsFinite())
                force = 0.1f;

            var normDelta = Vector3.Normalize(delta);
            if (!normDelta.X.IsFinite())
                normDelta = Vector3.Up;
            var thisReflect = Vector3.Reflect(VelocityDir, normDelta);
            var rhsReflect = Vector3.Reflect(rhs.VelocityDir, -normDelta);

            Vector3 deltaForce = (delta * force);
            MoveBy(thisReflect * force * invMass_ * (0.5f), false);
            rhs.MoveBy(rhsReflect * force * -rhs.invMass_ * (0.5f), false);

            MoveBy(deltaForce * invMass_ * bounce_, false);
            rhs.MoveBy(deltaForce * -rhs.invMass_ * rhs.bounce_, false);
        }
    }

    public static class Verlet
    {
        public static void Update(this StructArray<VerletParticle> particles, float td)
        {
            unchecked
            {
                //ref VerletParticle p = ref particles.GetNextItem();
                for (int i = 0; i < particles.Count; ++i)
                    particles.items_[i].Update(td);

                for (int i = 0; i < particles.Count - 2; i += 3)
                {
                    //if (i > 0)
                    //    particles.items_[i].ConstrainTo(ref particles.items_[i-1], 10.0f, 0.2f, 1.0f);
                    //particles.items_[i].ConstrainTo(ref particles.items_[i + 1], 40.0f, 0.2f, 1.0f);
                    //particles.items_[i+1].ConstrainTo(ref particles.items_[i + 2], 40.0f, 0.2f, 1.0f);
                }
            }
        }

        public static void Collide(this StructArray<VerletParticle> particles)
        {
            unsafe
            {
                //Map.BlockMap<Map.BlockEntry> blocks = new Map.BlockMap<Map.BlockEntry>(new Rectangle(-2000, -2000, 4000, 4000), 64);
                Cluster<int> blocks = new Cluster<int>(1500, 32);

                for (int i = 0; i < particles.Count; ++i)
                { 
                    //blocks.AddParticle(particles.items_[i]);
                    blocks.Add(new Vector2(
                        particles.items_[i].position_.X, 
                        particles.items_[i].position_.Y), 
                        i);

                    if (particles.items_[i].position_.Z != 0)
                        throw new Exception("fucked");
                }

                fixed (VerletParticle* p = &particles.items_[0])
                {
                    VerletParticle* v = p;
                    for (int i = 0; i < particles.Count; ++i)
                    {
                        var found = blocks.GetBox(
                            v->position_.X - v->radius_ * 2,
                            v->position_.Y + v->radius_ * 2,
                            v->position_.X + v->radius_ * 2,
                            v->position_.Y - v->radius_ * 2
                        );
                        
                        for (int op = 0; op < found.Count; ++op)
                        {
                            v->CollideWith(ref *(p + found[op]));
                        }

                        //var found = blocks.GetSquareRange(new Map.RectangleF { 
                        //    Left = v->position_.X - v->radius_,
                        //    Right = v->position_.X + v->radius_,
                        //    Bottom = v->position_.Y - v->radius_,
                        //    Top = v->position_.Y + v->radius_,
                        //});
                        //
                        //for (int y = 0; y < found.Count; ++y)
                        //{
                        //    for (int op = 0; op < found[y].Particles.Count; ++op)
                        //    {
                        //        var pp = found[y].Particles[op];
                        //        v->CollideWith(ref pp);
                        //    }
                        //}
                        
                        //for (int y = 0; y < particles.Count; ++y)
                        //{
                        //    if (vi == v)
                        //        continue;
                        //    v->CollideWith(ref *vi);
                        //    ++vi;
                        //}
                        ++v;
                    }
                }
                //for (int x = 0; x < particles.Count; ++x)
                //{
                //    for (int y = 0; y < particles.Count; ++y)
                //    {
                //        if (x == y)
                //            continue;
                //        particles.items_[x].CollideWith(ref particles.items_[y]);
                //    }
                //}
            }
        }

        public static void Collide(this StructArray<VerletParticle> particles, Plane plane)
        {
            unsafe
            {
                fixed(VerletParticle* p = &particles.items_[0])
                {
                    VerletParticle* v = p;
                    for (int i = 0; i < particles.Count; ++i)
                    {
                        if (!v->position_.X.IsFinite())
                            throw new Exception("dead");

                        float rad = v->radius_;
                        float d = plane.DotCoordinate(v->position_) - rad;
                        if (d < 0)
                        {
                            float bounce = v->bounce_;
                            Vector3 velocity = v->Velocity;
                            Vector3 reflected = Vector3.Reflect(velocity, plane.Normal);
                            v->position_ = plane.Project(v->position_, -rad);// + reflected * bounce;
                            v->oldPosition_ = v->position_ + reflected * -bounce;// + velocity * particles.items_[i].bounce_;
                            if (v->Velocity.LengthSquared() > 0.01f)
                                v->collided_ = true;
                        }
                        v += 1;
                    }
                
                }
                //for (int i = 0; i < particles.Count; ++i)
                //{
                //    float rad = particles.items_[i].radius_;
                //    float d = plane.DotCoordinate(particles.items_[i].position_) - rad;
                //    if (d < 0)
                //    {
                //        ref VerletParticle part = ref particles.items_[i];
                //        if (part.Velocity.LengthSquared() > 0.01f)
                //        {
                //            Vector3 reflected = Vector3.Reflect(part.Velocity, plane.Normal);
                //            part.position_ = plane.Project(part.position_, -rad);
                //            part.oldPosition_ = part.position_ + reflected * -part.bounce_;
                //            part.collided_ = true;
                //        }
                //    }
                //}
            }
        }

        public static void Add(this StructArray<VerletParticle> particles, Vector3 pos, float radius, float mass, float bounciness)
        {
            int idx = particles.GetNextIndex();
            if (idx != -1)
            {
                particles.items_[idx].position_ = pos;
                particles.items_[idx].oldPosition_ = pos;
                particles.items_[idx].radius_ = radius;
                particles.items_[idx].mass_ = mass;
                particles.items_[idx].bounce_ = bounciness;
                particles.items_[idx].drag_ = 0.2f;
                particles.items_[idx].UpdateMass();
            }
        }
    }
}
