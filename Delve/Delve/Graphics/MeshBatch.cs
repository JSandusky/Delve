using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using DelveLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Delve.Graphics
{
    /// <summary>
    /// Interface for Xna.Framework.Graphics.Effect implementations to implement if they should be used with the meshbatch.
    /// </summary>
    public interface IMeshBatchEffect
    {
        // Implementation must apply a pass from a technique in the effect, return true if we can render.
        // Use the passID to decide how to setup the Effect
        bool PrepareInstanced(int passID);
        // Implementation must apply a pass from a technique in the effect, return true if we can render
        // Use the passID to decide how to setup the Effect
        bool PrepareOneOff(Matrix transform, int passID);
        // Do any per-frame draw cluster initialization (current-time, deltas, etc), setup technique/pass etc
        // Return true if it can render
        bool EffectSelected(int passID);
        // Do any post-draw cluster tasks, should be none - barring some odd need to mess with GPU state
        void EffectDeselected();
    }

    // Data for the additional stream of per-instance data
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexInstanceTransform : IVertexType
    {
        //?? garbage, not currently using this actual type
        public Matrix transform;

        public static readonly VertexDeclaration vertexDeclaration;
        static VertexInstanceTransform()
        {
            //TODO: replace with a 3x4, using a 4x4 is stupid
            VertexElement[] elements = new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
                new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2),
                new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3),
                new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 4),
            };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            vertexDeclaration = declaration;
        }

        public VertexDeclaration VertexDeclaration
        {
            get { return VertexDeclaration; }
        }
    }

    // Data for an additional stream of per-vertex extra transforms (morph targets, soft-bodies)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PerVertexMutation : IVertexType
    {
        public Vector3 position_;
        public Vector3 normal_;
        public Vector4 tangent_;

        public static readonly VertexDeclaration vertexDeclaration;
        static PerVertexMutation()
        {
            //TODO: replace with a 3x4, using a 4x4 is stupid
            VertexElement[] elements = new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
                new VertexElement(4*3, VertexElementFormat.Vector4, VertexElementUsage.Normal, 1),
                new VertexElement(4*6, VertexElementFormat.Vector4, VertexElementUsage.Tangent, 1),
            };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            vertexDeclaration = declaration;
        }

        public VertexDeclaration VertexDeclaration
        {
            get { return VertexDeclaration; }
        }
    }

    // A sortable container of the necessary information to draw
    public class MeshDraw : IComparable<MeshDraw>
    {
        public VertexBuffer verts;
        public IndexBuffer indices;
        public Matrix transform;
        public IMeshBatchEffect effect;
        public ulong sortCode; // must be set via SortCode()
        public List<Matrix> Transforms = new List<Matrix>();
        public bool canInstance = true;
        public uint lightMask = 0xFFFFFFFF;

        public ulong SortCode(int renderOrder)
        {
            // 8 bit render order, more like a layer
            ulong r = ((ulong)(renderOrder & 0xFF)) << 56;

            // Effect goes in the high-bits, an Effect change likely means changes to texture-units, and other render state       
            ulong fx = (ulong)effect.GetHashCode();
            fx &= 0xFFFF;
            fx <<= 40;
            r |= fx;

            // TODO: combine vertex and index buffers into a geometry object and use that as a hashcode source
            // will free up 16 bits, caveats? should it be 24-bits in that case?
            ulong i = indices != null ? (ulong)indices.GetHashCode() : 0;
            i &= 0xFFFF;
            i <<= 24;
            r |= i;

            ulong v = (ulong)verts.GetHashCode();
            v &= 0xFFFF;
            v <<= 8;
            r |= v;

            r |= canInstance ? (1ul << 7) : 0;
            // 6 bits left

            return r;
        }

        public int CompareTo(MeshDraw other) { return sortCode.CompareTo(other.sortCode); }
    }

    /// <example>
    /// MeshBatch batch = new MeshBatch(graphicsDevice);
    /// batch.Begin(myCamera);
    /// batch.Add(vtxBuffer, idxBuffer, transformMat, effect);
    /// batch.Add(vtxBuffer, idxBuffer, transformMat, effect); (automatic instancing)
    /// batch.End();
    /// </example>
    /// <remarks>
    /// SortedList is unintuitive, insertion overhead
    ///     recording commands into a list and then sorting them is much slower:
    ///     - C# List.Sort is SLOWWW
    ///     - Have to scan through ranges to find what can be instanced into one draw-call
    ///     - Don't care about removal, always done via Clear()
    ///     - Want to get the ordered `Values` as fast as possible
    ///         - Slow to do on SortedDictionary, though SortedDictionary is faster to insert
    /// Pooling isn't used:
    ///     pooling with a Deque was tried but it was actually slower, test cased timed out to:
    ///     - 3.6ms to not pool (avg)
    ///     - 5.1ms to pool (avg)
    ///     test case was for drawing 400 objects (with MAX_INSTANCES at 50), w/ PBR (single light) on an Intel HD4000
    ///     pooling wasn't worth the CPU cost
    ///     garbage produced here is all/mostly gen-0
    /// </remarks>
    public class MeshBatch
    {
        const int MAX_INSTANCES = 4096*2;
        const int MIN_INSTANCES = 0;

        Camera camera_;
        BoundingFrustum frustum_;
        DynamicVertexBuffer instancedTransforms_;
        GraphicsDevice device_;
        // Unintuitive, but this is faster than appending a list, sorting the list, then selecting ranges for automatic instancing
        SortedList<ulong, MeshDraw> draws_ = new SortedList<ulong, MeshDraw>();

        IMeshBatchEffect lastEffect_ = null;
        VertexBuffer lastMesh_ = null;
        IndexBuffer lastIndices_ = null;

        public MeshBatch(GraphicsDevice device)
        {
            device_ = device;
        }

        public SortedList<ulong, MeshDraw> QueuedDraws { get { return draws_; } }
        
        public void Add(Model model, Matrix transform)
        {
            for (int i = 0; i < model.Meshes.Count; ++i)
            {
                var mesh = model.Meshes[i];
                for (int j = 0; j < mesh.MeshParts.Count; ++j)
                {
                    var meshPart = mesh.MeshParts[j];
                    Add(mesh.BoundingSphere, meshPart.VertexBuffer, meshPart.IndexBuffer, transform, (IMeshBatchEffect)meshPart.Effect, 0, model.Bones.Count == 0);
                }
            }
        }

        /// <summary>
        /// Append a rendering task
        /// </summary>
        /// <param name="verts">vertex buffer to draw</param>
        /// <param name="ind">index buffer to draw</param>
        /// <param name="transform">transform of the geometry being drawn</param>
        /// <param name="effect">shader effect / texture-combination</param>
        /// <param name="drawOrder">Sequence, ordered low -> high, use like a layer</param>
        /// <param name="canInstance">Whether to allow automatic instancing or not</param>
        public void Add(VertexBuffer verts, IndexBuffer ind, Matrix transform, IMeshBatchEffect effect, int drawOrder = 0, bool canInstance = true)
        {
            Add(null, verts, ind, transform, effect, drawOrder, canInstance);
        }

        /// <summary>
        /// Adds an existing draw to the queue. The src should wipe it's `transforms` list
        /// KEEP the solo transform
        /// </summary>
        public void Add(MeshDraw draw, BoundingSphere? bounds)
        {
            // NOTE: it's the given MeshDraw's job to have appropriately cleared or populated
            //      the contained `Transforms` list, otherwise things will not work
            //      the list in the MeshDraw instance CANNOT be trusted
            if (bounds.HasValue)
            {
                var bnds = bounds.Value;
                bnds.Center = Vector3.Transform(bnds.Center, draw.transform);
                if (frustum_.Contains(bnds) == ContainmentType.Disjoint)
                    return;
            }

            MeshDraw d = null;
            if (draws_.TryGetValue(draw.sortCode, out d))
            {
                if (d.Transforms.Count == 0)
                    d.Transforms.Add(d.transform);
                else if (draw.Transforms.Count > 0)
                    d.Transforms.AddRange(draw.Transforms);
                else
                    d.Transforms.Add(draw.transform);
            }
            else
                draws_.Add(draw.sortCode, draw);
        }

        /// Specialization of the above to perform culling.
        public void Add(BoundingSphere? bounds, VertexBuffer verts, IndexBuffer ind, Matrix transform, IMeshBatchEffect effect, int drawOrder = 0, bool canInstance = true)
        {
            if (bounds.HasValue)
            {
                var bnds = bounds.Value;
                bnds.Center = Vector3.Transform(bnds.Center, transform);
                if (frustum_.Contains(bnds) == ContainmentType.Disjoint)
                    return;
            }
            
            var draw = new MeshDraw {
                verts = verts,
                indices = ind,
                transform = transform,
                effect = effect,
                canInstance = canInstance
            };
            draw.sortCode = draw.SortCode(drawOrder);

            Graphics.MeshDraw d;
            if (draws_.TryGetValue(draw.sortCode, out d))
            {
                if (d.Transforms.Count == 0)
                    d.Transforms.Add(d.transform);
                d.Transforms.Add(draw.transform);
            }
            else
            { 
                draws_.Add(draw.sortCode, draw);
            }
        }

        /// Prepares state for enqueuing draws
        public void Begin(Camera camera)
        {
            availBuffers.Clear();
            availBuffers.AddRange(buffercache);
            camera_ = camera;
            frustum_ = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix);
            draws_.Clear();
        }

        // Render everything in the list of draws.
        public void Render(Camera camera, int passID)
        {
            lastEffect_ = null;
            lastMesh_ = null;

            var draws = draws_.Values.ToArray();
            for (int i = 0; i < draws.Length; ++i)
            {
                MeshDraw nextDraw = draws[i];
                if (nextDraw.verts != lastMesh_ || nextDraw.effect != lastEffect_)
                {
                    if (nextDraw.effect != lastEffect_)
                    {
                        if (lastEffect_ != null)
                            lastEffect_.EffectDeselected();
                        if (!nextDraw.effect.EffectSelected(passID))
                        {
                            lastEffect_ = null;
                            continue;
                        }
                    }

                    lastMesh_ = nextDraw.verts;
                    lastIndices_ = nextDraw.indices;
                    lastEffect_ = nextDraw.effect;

                    if (nextDraw.canInstance && nextDraw.Transforms?.Count > MIN_INSTANCES)
                    {
                        int ct = nextDraw.Transforms.Count / MAX_INSTANCES;
                        int remaining = nextDraw.Transforms.Count;
                        int idx = 0;
                        while (remaining > 0)
                        {
                            int drawCt = Math.Min(MAX_INSTANCES, remaining);
                            if (MAX_INSTANCES > nextDraw.Transforms.Count)
                                InstancedDraw(nextDraw.verts, nextDraw.indices, nextDraw.Transforms.ToArray(), camera, passID);
                            else
                            {
                                var trans = nextDraw.Transforms.GetRange(idx, drawCt).ToArray();
                                InstancedDraw(nextDraw.verts, nextDraw.indices, trans, camera, passID);
                            }
                            remaining -= drawCt;
                            idx += drawCt;
                        }
                    }
                    else
                    {
                        if (nextDraw.Transforms?.Count > 0)
                        {
                            for (int m = 0; m < nextDraw.Transforms.Count; ++m)
                                OneOffDraw(nextDraw.verts, nextDraw.indices, nextDraw.Transforms[m], camera, passID);
                        }
                        else
                            OneOffDraw(nextDraw.verts, nextDraw.indices, nextDraw.transform, camera, passID);
                    }
                }
            }

            if (lastEffect_ != null)
                lastEffect_.EffectDeselected();

            lastMesh_ = null;
            lastEffect_ = null;
        }

        List<DynamicVertexBuffer> buffercache = new List<DynamicVertexBuffer>();
        List<DynamicVertexBuffer> availBuffers = new List<DynamicVertexBuffer>();

        VertexBuffer GetInstanceBuffer(int ct, Matrix[] transforms)
        {
            if (availBuffers.Count == 0)
            {
                var buffer = new DynamicVertexBuffer(device_, VertexInstanceTransform.vertexDeclaration, MAX_INSTANCES, BufferUsage.WriteOnly);
                buffercache.Add(buffer);
                buffer.SetData(transforms, 0, transforms.Length, SetDataOptions.NoOverwrite);
                return buffer;
            }
            //if (instancedTransforms_ == null)
            //    instancedTransforms_ = new DynamicVertexBuffer(device_, VertexInstanceTransform.vertexDeclaration, ct, BufferUsage.WriteOnly);
            //instancedTransforms_.SetData(transforms, 0, transforms.Length, SetDataOptions.Discard);
            //return instancedTransforms_;

            var buff = availBuffers[availBuffers.Count - 1];
            availBuffers.RemoveAt(availBuffers.Count - 1);
            buff.SetData(transforms, 0, transforms.Length, SetDataOptions.NoOverwrite);
            return buff;
        }

        void OneOffDraw(VertexBuffer verts, IndexBuffer indices, Matrix trans, Camera camera, int passID)
        {
            if (lastEffect_.PrepareOneOff(trans, passID))
            {
                device_.SetVertexBuffer(verts);
                device_.Indices = indices;
                device_.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indices.IndexCount / 3);
            }
        }

        void InstancedDraw(VertexBuffer verts, IndexBuffer indices, Matrix[] matrices, Camera camera, int passID)
        {
            if (lastEffect_.PrepareInstanced(passID))
            {
                device_.Indices = indices;
                VertexBuffer instanceBuff = GetInstanceBuffer(matrices.Length, matrices);
                device_.SetVertexBuffers(new VertexBufferBinding[] {
                    new VertexBufferBinding(verts),
                    new VertexBufferBinding(instanceBuff, 0, 1)
                });
                device_.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, indices.IndexCount / 3, matrices.Length);
            }
        }
    }
}
