using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using glTFLoader;

namespace DelveLib
{
    public class GLTFBuffer
    {
        uint magic_;
        uint version_;
        uint length_;
        public List<Chunk> chunks_ = new List<Chunk>();

        public class Chunk
        {
            public uint chunkLength_;
            public uint chunkType_;
            public uint chunkStart_;

            public float[] ToFloat(GLTFBuffer buffer) { return ToFloat(buffer, (int)chunkStart_, (int)chunkLength_); }
            public int[] ToInt(GLTFBuffer buffer) { return ToInt(buffer, (int)chunkStart_, (int)chunkLength_); }
            public byte[] ToByte(GLTFBuffer buffer) { return ToByte(buffer, (int)chunkStart_, (int)chunkLength_); }
            public ushort[] ToUShort(GLTFBuffer buffer) { return ToUShort(buffer, (int)chunkStart_, (int)chunkLength_); }
            public Vector2[] ToVec2(GLTFBuffer buffer) { return ToVec2(buffer, (int)chunkStart_, (int)chunkLength_); }
            public Vector3[] ToVec3(GLTFBuffer buffer) { return ToVec3(buffer, (int)chunkStart_, (int)chunkLength_); }
            public Vector4[] ToVec4(GLTFBuffer buffer) { return ToVec4(buffer, (int)chunkStart_, (int)chunkLength_); }

            public float[] ToFloat(GLTFBuffer buffer, int start, int length)
            { 
                float[] ret = new float[length / sizeof(float)];
                Buffer.BlockCopy(buffer.data_, start, ret, 0, length);
                return ret;
            }

            public int[] ToInt(GLTFBuffer buffer, int start, int length)
            {
                int[] ret = new int[length / sizeof(int)];
                Buffer.BlockCopy(buffer.data_, start, ret, 0, length);
                return ret;
            }

            public byte[] ToByte(GLTFBuffer buffer, int start, int length)
            {
                byte[] ret = new byte[length];
                Buffer.BlockCopy(buffer.data_, start, ret, 0, length);
                return ret;
            }

            public ushort[] ToUShort(GLTFBuffer buffer, int start, int length)
            {
                ushort[] ret = new ushort[length / sizeof(ushort)];
                Buffer.BlockCopy(buffer.data_, start, ret, 0, length);
                return ret;
            }

            public Vector2[] ToVec2(GLTFBuffer buffer, int start, int length)
            {
                Vector2[] ret = new Vector2[length / (sizeof(float)*2)];
                Buffer.BlockCopy(buffer.data_, start, ret, 0, length);
                return ret;
            }

            public Vector3[] ToVec3(GLTFBuffer buffer, int start, int length)
            {
                Vector3[] ret = new Vector3[length / (sizeof(float) * 3)];
                Buffer.BlockCopy(buffer.data_, start, ret, 0, length);
                return ret;
            }

            public Vector4[] ToVec4(GLTFBuffer buffer, int start, int length)
            {
                Vector4[] ret = new Vector4[length / (sizeof(float) * 4)];
                Buffer.BlockCopy(buffer.data_, start, ret, 0, length);
                return ret;
            }
        }

        public string name_;
        public byte[] data_;

        public GLTFBuffer(System.IO.Stream stream, int size)
        {
            stream.Read(data_, 0, size);
        }

        public GLTFBuffer(byte[] data, string name)
        {
            data_ = data;
            name_ = name;
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(data))
            using (System.IO.BinaryReader rdr = new System.IO.BinaryReader(stream))
            {
                magic_ = rdr.ReadUInt32();
                version_ = rdr.ReadUInt32();
                length_ = rdr.ReadUInt32();

                while (stream.Position < stream.Length)
                {
                    Chunk chunk = new Chunk();
                    chunk.chunkLength_ = rdr.ReadUInt32();
                    chunk.chunkType_ = rdr.ReadUInt32();
                    chunk.chunkStart_ = (uint)stream.Position;
                    stream.Seek(chunk.chunkLength_ + chunk.chunkLength_ % 4, System.IO.SeekOrigin.Current);
                }
            }
        }

    }

    public class GLTF
    {
        Model model_;
        List<ModelBone> bones_ = new List<ModelBone>();
        List<ModelMesh> meshes_ = new List<ModelMesh>();
        List<GLTFBuffer> buffers_ = new List<GLTFBuffer>();

        public bool Load(GraphicsDevice device, string filePath)
        {

            var glModel = glTFLoader.Interface.LoadModel(filePath);

            if (glModel != null)
            {
                if (glModel.Buffers != null)
                {
                    for (int i = 0; i < glModel.Buffers.Length; ++i)
                    {
                        byte[] data = glModel.LoadBinaryBuffer(i, filePath);
                        buffers_.Add(new GLTFBuffer(data, glModel.Buffers[i].Name));
                    }
                }

                foreach (var node in glModel.Nodes)
                    LoadMeshes(glModel, node);

                model_ = new Model(device, bones_, meshes_);


                return true;
            }   
            return false;
        }

        void LoadMeshes(glTFLoader.Schema.Gltf file, glTFLoader.Schema.Node node)
        {
            if (node.Mesh.HasValue)
            {
                int meshIdx = node.Mesh.Value;
                foreach (var prim in file.Meshes[meshIdx].Primitives)
                {
                    foreach (var attr in prim.Attributes)
                    {
                        string attrTypeName = attr.Key;
                        int attrBufferIndex = attr.Value;
                        var view = buffers_[file.BufferViews[attrBufferIndex].Buffer];
                        switch (attrTypeName)
                        {
                        case "POSITION":
                            break;
                        case "NORMAL":
                            break;
                        case "TANGENT":
                            break;
                        case "COLOR_0":
                            break;
                        case "TEXCOORD_0":
                            break;
                        case "TEXCOORD_1":
                            break;
                        case "JOINTS_0":
                            break;
                        case "WEIGHTS_0":
                            break;
                        }
                    }
                    if (prim.Indices.HasValue)
                    {
                        
                    }
                }
            }
        }
    }
}
