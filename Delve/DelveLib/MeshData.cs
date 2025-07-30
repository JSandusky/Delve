using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DelveLib
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexData : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 Tangent; //W contains handedness
        public Vector2 TextureCoordinate;
        public Vector4 BoneWeights;
        public Vector4 BoneIndices;

        public static readonly VertexDeclaration VertexDeclaration;

        public void SetPosition(Vector3 pos)
        {
            Position = pos;
        }

        public VertexData(Vector3 position, Vector3 normal, Vector2 textureCoordinate)
        {
            this.Position = position;
            this.Normal = normal;
            this.TextureCoordinate = textureCoordinate;
            this.Tangent = new Vector4();

            this.BoneWeights = new Vector4();
            this.BoneIndices = new Vector4(-1, -1, -1, -1);
        }

        public VertexData(Vector3 position, Vector3 normal, Vector4 tangent, Vector2 textureCoordinate) : this(position, normal, textureCoordinate)
        {
            this.Tangent = tangent;
        }

        public VertexData(Vector3 position, Vector3 normal, Vector4 tangent, Vector2 textureCoordinate, Vector4 boneWeights, Vector4 boneIndices) : this(position, normal, tangent, textureCoordinate)
        {
            this.BoneWeights = boneWeights;
            this.BoneIndices = boneIndices;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get
            {
                return VertexDeclaration;
            }
        }
        public override int GetHashCode()
        {
            // TODO: FIc gethashcode
            return 0;
        }

        public override string ToString()
        {
            return string.Format("{{Position:{0} Normal:{1} TextureCoordinate:{2}}}", new object[] { this.Position, this.Normal, this.TextureCoordinate });
        }

        public static bool operator ==(VertexData left, VertexData right)
        {
            return (((left.Position == right.Position) && (left.Normal == right.Normal)) && (left.TextureCoordinate == right.TextureCoordinate));
        }

        public static bool operator !=(VertexData left, VertexData right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() != base.GetType())
                return false;
            return (this == ((VertexData)obj));
        }

        static VertexData()
        {
            VertexElement[] elements = new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),                  // 12 bytes
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),                   // 12 bytes
                new VertexElement(24, VertexElementFormat.Vector4, VertexElementUsage.Tangent, 0),                  // 16 bytes
                new VertexElement(40, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),        // 8 bytes
                new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),              // 16 bytes
                new VertexElement(64, VertexElementFormat.Vector4, VertexElementUsage.BlendIndices, 0)               // 16 bytes
            };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }
    }

    public class MeshBone
    {

    }

    public class MeshData : IDisposable
    {
        public BoundingBox Bounds { get; private set; }
        public Effect Effect { get; set; }
        public IndexBuffer IndexBuffer { get; set; }
        public VertexBuffer VertexBuffer { get; set; }

        List<int> indices;
        List<VertexData> vertices;

        public List<int> GetIndices() { return indices; }
        public List<VertexData> GetVertices() { return vertices; }

        public SkeletonData Skeleton { get; set; }

        public MeshData(List<int> indices, List<VertexData> vertices)
        {
            this.indices = indices;
            this.vertices = vertices;

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var vert in vertices)
            {
                min.X = Math.Min(min.X, vert.Position.X);
                min.Y = Math.Min(min.Y, vert.Position.Y);
                min.Z = Math.Min(min.Z, vert.Position.Z);

                max.X = Math.Max(max.X, vert.Position.X);
                max.Y = Math.Max(max.Y, vert.Position.Y);
                max.Z = Math.Max(max.Z, vert.Position.Z);
            }
            Bounds = new BoundingBox { Min = min, Max = max };
        }

        ~MeshData()
        {
            Dispose();
        }

        public void SetData(List<int> indices, List<VertexData> vertices)
        {
            lock (this)
            {
                this.indices = indices;
                this.vertices = vertices;
                if (VertexBuffer != null)
                    VertexBuffer.Dispose();
                if (IndexBuffer != null)
                    IndexBuffer.Dispose();
            }
        }

        bool disposed_ = false;
        public void Dispose()
        {
            lock (this)
            {
                if (IndexBuffer != null)
                    IndexBuffer.Dispose();
                IndexBuffer = null;
                if (VertexBuffer != null)
                    VertexBuffer.Dispose();
                VertexBuffer = null;
                disposed_ = true;
            }
        }

        public int TriangleCount { get { return indices != null ? indices.Count / 3 : 0; } }
        public int IndexCount { get { return indices != null ? indices.Count : 0; } }
        public int VertexCount { get { return vertices != null ? vertices.Count : 0; } }

        public void Initialize(GraphicsDevice device)
        {
            if (device == null)
                return;
            IndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            VertexBuffer = new VertexBuffer(device, typeof(VertexData), vertices.Count, BufferUsage.WriteOnly);

            IndexBuffer.SetData<int>(indices.ToArray());
            VertexBuffer.SetData(vertices.ToArray());
        }

        /// <summary>
        /// Render to the given graphics device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        /// <param name="leaveTextures"></param>
        public void Draw(GraphicsDevice device, Matrix view, Matrix projection, bool leaveTextures = false)
        {
            if (Effect == null)
                return;

            lock (this)
            {
                if (indices.Count > 0 && vertices.Count > 0)
                {
                    if (VertexBuffer == null || IndexBuffer == null || VertexBuffer.IsDisposed || IndexBuffer.IsDisposed)
                        Initialize(device);

                    if (VertexBuffer == null || IndexBuffer == null)
                        return;

                    device.BlendState = BlendState.NonPremultiplied;
                    device.SetVertexBuffer(VertexBuffer);
                    device.Indices = IndexBuffer;
                    foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexBuffer.IndexCount / 3);
                    }

                    device.SetVertexBuffer(null);
                    device.Indices = null;

                    //device.RasterizerState = originalState;
                }
            }

            if (disposed_)
                Dispose();
        }

        public void DrawInstanced(GraphicsDevice device, Matrix[] transforms, Matrix view, Matrix projection)
        {
            device.SetVertexBuffer(VertexBuffer);
            device.Indices = IndexBuffer;
            Effect.Parameters["InstanceTransforms"].SetValue(transforms);
            foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexBuffer.IndexCount / 3, transforms.Length);
            }
        }

        public void Draw(GraphicsDevice device, Effect effect)
        {
            device.BlendState = BlendState.NonPremultiplied;
            device.SetVertexBuffer(VertexBuffer);
            device.Indices = IndexBuffer;
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                if (IndexBuffer == null || IndexBuffer.GraphicsDevice == null || device == null)
                    break;
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexBuffer.IndexCount / 3);
            }

            device.SetVertexBuffer(null);
            device.Indices = null;
        }

        public MeshData Clone()
        {
            List<VertexData> cloneVerts = new List<VertexData>(vertices.Count);
            for (int i = 0; i < vertices.Count; ++i)
                cloneVerts.Add(new VertexData(vertices[i].Position, vertices[i].Normal, vertices[i].TextureCoordinate));
            return new MeshData(indices, cloneVerts);
        }

        public static MeshData CreateBox()
        {
            var face = new Vector3[6];
            face[0] = new Vector3(-1f, 01f, 0.0f); //TopLeft
            face[1] = new Vector3(-1f, -1f, 0.0f); //BottomLeft
            face[2] = new Vector3(01f, 01f, 0.0f); //TopRight
            face[3] = new Vector3(-1f, -1f, 0.0f); //BottomLeft
            face[4] = new Vector3(01f, -1f, 0.0f); //BottomRight
            face[5] = new Vector3(01f, 01f, 0.0f); //TopRight

            var textureCoords = new Vector2(0f, 0f);
            var vertices = new VertexPositionNormalTexture[36];

            //front face
            for (var i = 0; i <= 2; i++)
            {
                vertices[i] = new VertexPositionNormalTexture(
                    face[i] + Vector3.UnitZ,
                    Vector3.UnitZ, textureCoords);
                vertices[i + 3] = new VertexPositionNormalTexture(
                    face[i + 3] + Vector3.UnitZ,
                    Vector3.UnitZ, textureCoords);
            }

            vertices[0].TextureCoordinate = new Vector2(0, 0);//Top Left
            vertices[1].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[2].TextureCoordinate = new Vector2(1, 0);//Top Right
            vertices[3].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[4].TextureCoordinate = new Vector2(1, 1);//Bottom Right
            vertices[5].TextureCoordinate = new Vector2(1, 0);//TopRight

            //Back face
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 6] = new VertexPositionNormalTexture(
                    face[2 - i] - Vector3.UnitZ,
                    -Vector3.UnitZ, textureCoords);
                vertices[i + 6 + 3] = new VertexPositionNormalTexture(
                    face[5 - i] - Vector3.UnitZ,
                    -Vector3.UnitZ, textureCoords);
            }

            vertices[6].TextureCoordinate = new Vector2(1, 0);//Top Right
            vertices[7].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[8].TextureCoordinate = new Vector2(0, 0);//Top Left
            vertices[9].TextureCoordinate = new Vector2(1, 0);//TopRight
            vertices[10].TextureCoordinate = new Vector2(1, 1);//Bottom Right
            vertices[11].TextureCoordinate = new Vector2(0, 1);//Bottom Left

            //left face
            var rotY90 = Matrix.CreateRotationY(-MathHelper.PiOver2);
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 12] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[i], rotY90) - Vector3.UnitX,
                    -Vector3.UnitX, textureCoords);
                vertices[i + 12 + 3] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[i + 3], rotY90) - Vector3.UnitX,
                    -Vector3.UnitX, textureCoords);
            }

            vertices[14].TextureCoordinate = new Vector2(1, 0);//Top Right
            vertices[13].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[12].TextureCoordinate = new Vector2(0, 0);//Top Left
            vertices[15].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[16].TextureCoordinate = new Vector2(1, 1);//Bottom Right
            vertices[17].TextureCoordinate = new Vector2(1, 0);//TopRight

            //Right face
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 18] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[2 - i], rotY90) + Vector3.UnitX,
                    Vector3.UnitX, textureCoords);
                vertices[i + 18 + 3] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[5 - i], rotY90) + Vector3.UnitX,
                    Vector3.UnitX, textureCoords);
            }

            vertices[18].TextureCoordinate = new Vector2(1, 0);//Top Right
            vertices[19].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[20].TextureCoordinate = new Vector2(0, 0);//Top Left
            vertices[21].TextureCoordinate = new Vector2(1, 0);//TopRight
            vertices[22].TextureCoordinate = new Vector2(1, 1);//Bottom Right
            vertices[23].TextureCoordinate = new Vector2(0, 1);//Bottom Left

            //Top face
            var rotX90 = Matrix.CreateRotationX(-MathHelper.PiOver2);
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 24] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[i], rotX90) + Vector3.UnitY,
                    Vector3.UnitY, textureCoords);
                vertices[i + 24 + 3] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[i + 3], rotX90) + Vector3.UnitY,
                    Vector3.UnitY, textureCoords);
            }

            vertices[26].TextureCoordinate = new Vector2(1, 0);//Top Right
            vertices[25].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[24].TextureCoordinate = new Vector2(0, 0);//Top Left
            vertices[27].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[28].TextureCoordinate = new Vector2(1, 1);//Bottom Right
            vertices[29].TextureCoordinate = new Vector2(1, 0);//TopRight

            //Bottom face
            for (var i = 0; i <= 2; i++)
            {
                vertices[i + 30] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[2 - i], rotX90) - Vector3.UnitY,
                    -Vector3.UnitY, textureCoords);
                vertices[i + 30 + 3] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[5 - i], rotX90) - Vector3.UnitY,
                    -Vector3.UnitY, textureCoords);
            }

            vertices[30].TextureCoordinate = new Vector2(1, 0);//Top Right
            vertices[31].TextureCoordinate = new Vector2(0, 1);//Bottom Left
            vertices[32].TextureCoordinate = new Vector2(0, 0);//Top Left
            vertices[33].TextureCoordinate = new Vector2(1, 0);//TopRight
            vertices[34].TextureCoordinate = new Vector2(1, 1);//Bottom Right
            vertices[35].TextureCoordinate = new Vector2(0, 1);//Bottom Left

            List<int> indices = new List<int>();
            for (int i = 0; i < 12*3; ++i)
                indices.Add(i);
            List<VertexData> verts = new List<VertexData>();
            for (int i = 0; i < vertices.Length; ++i)
                verts.Add(new DelveLib.VertexData { Position = vertices[i].Position, Normal = vertices[i].Normal, TextureCoordinate = vertices[i].TextureCoordinate });

            return new MeshData(indices, verts);
        }

        public static MeshData CreateFromHeightField(System.Drawing.Bitmap bmp, string fileName, float heightScale, bool decimate, float power)
        {
            if (bmp == null)
                return null;

            int dimX = bmp.Width;
            int dimY = bmp.Height;
            List<int> indices = GenerateTerrainIndices(dimX, dimY);
            List<VertexData> vertices = GenerateTerrainVertices(bmp, new Vector2(-1.0f, -1.0f), dimX, dimY, 1.0f, heightScale);

            return new MeshData(indices, vertices);
        }

        public static MeshData CreateMarchingSquares(System.Drawing.Bitmap bmp, bool useEdgeIntercept, string fileName, byte heightCutoff, bool decimate, float power)
        {
            VoxelGrid grid = new VoxelGrid();
            grid.Initialize(Math.Max(bmp.Width, bmp.Height), (float)Math.Max(bmp.Width, bmp.Height), 135.0f);
            grid.Apply(bmp, heightCutoff, useEdgeIntercept);

            return grid.ToMesh();
        }

        static List<VertexData> GenerateTerrainVertices(System.Drawing.Bitmap heightmap, Vector2 startPosition, int vertexCountX, int vertexCountZ, float blockScale = 3.0f, float heightScale = 0.5f)
        {
            float halfTerrainWidth = (vertexCountX - 1) * blockScale * .5f;
            float halfTerrainDepth = (vertexCountZ - 1) * blockScale * .5f;
            float tuDerivative = 1.0f / (vertexCountX - 1);
            float tvDerivative = 1.0f / (vertexCountZ - 1);

            VertexData[] vertices = new VertexData[vertexCountX * vertexCountZ];
            int vertexCount = 0;
            float tu = 0;
            float tv = 0;
            for (float i = -halfTerrainDepth; i <= halfTerrainDepth; i += blockScale)
            {
                tu = 0.0f;
                for (float j = -halfTerrainWidth; j <= halfTerrainWidth; j += blockScale)
                {
                    var heightValue = heightmap.GetPixelBilinear(tu, tv).R;
                    vertices[vertexCount].Position = new Vector3(j, heightValue * heightScale * 0.1f, i);
                    vertices[vertexCount].TextureCoordinate = new Vector2(tu, tv);
                    vertices[vertexCount].Normal = Vector3.UnitY;
                    tu += tuDerivative;
                    vertexCount++;
                }
                tv += tvDerivative;
            }

            return vertices.ToList();
        }

        // remember, dim + 1
        static List<int> GenerateTerrainIndices(int vertexCountX, int vertexCountZ, float blockScale = 3.0f, float heightScale = 0.5f)
        {
            int numTriangles = numTriangles = (vertexCountX - 1) * (vertexCountZ - 1) * 2;
            int numIndices = numTriangles * 3;
            int[] indices = new int[numIndices];
            int indicesCount = 0;
            for (int i = 0; i < (vertexCountZ - 1); i++) //All Rows except last
                for (int j = 0; j < (vertexCountX - 1); j++) //All Columns except last
                {
                    int index = j + i * vertexCountZ; //2D coordinates to linear
                                                      //First Triangle Vertices
                    indices[indicesCount++] = index;
                    indices[indicesCount++] = index + 1;
                    indices[indicesCount++] = index + vertexCountX + 1;

                    //Second Triangle Vertices
                    indices[indicesCount++] = index + vertexCountX + 1;
                    indices[indicesCount++] = index + vertexCountX;
                    indices[indicesCount++] = index;
                }
            return indices.ToList();
        }

        #region Debugging Utilities
        public void DrawTangentFrames(DebugDraw debugDraw, Vector3 offset)
        {
            lock (this)
            {
                if (vertices != null)
                {
                    foreach (var v in vertices)
                    {
                        debugDraw.DrawLine(v.Position + offset, v.Position + offset + v.Normal * 0.07f, Color.CornflowerBlue);
                        debugDraw.DrawLine(v.Position + offset, v.Position + offset + v.Tangent.XYZ() * 0.07f, Color.Magenta);
                        Vector3 cross = Vector3.Cross(v.Normal, v.Tangent.XYZ()) * v.Tangent.W;
                        debugDraw.DrawLine(v.Position + offset, v.Position + cross * 0.07f + offset, Color.Yellow);
                    }
                }
            }
        }
        #endregion
    }
}
