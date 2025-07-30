using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Microsoft.Xna.Framework;

namespace Delve.Graphics
{
    public class OBJObject
    {
        public string Name = "";
        public List<uint> vertexIndices = new List<uint>();
        public List<uint> textureIndices = new List<uint>();
        public List<uint> normalIndices = new List<uint>();
    }

    public class OBJLoader
    {
        public List<OBJObject> Objects = new List<OBJObject>();
        public List<Vector4> vertices = new List<Vector4>();
        public List<Vector3> textureVertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();

        public OBJLoader(Stream stream)
        {
            OBJObject current = null;
            using (var streamReader = new StreamReader(stream))
            {
                while (!streamReader.EndOfStream)
                {
                    List<string> words = new List<string>(streamReader.ReadLine().ToLower().Split(' '));
                    words.RemoveAll(s => s == string.Empty);

                    if (words.Count == 0)
                        continue;

                    string type = words[0];
                    words.RemoveAt(0);

                    switch (type)
                    {
                    // vertex
                    case "v":
                        vertices.Add(new Vector4(float.Parse(words[0]), float.Parse(words[1]),
                                                float.Parse(words[2]), words.Count < 4 ? 1 : float.Parse(words[3])));
                        break;

                    case "vt":
                        textureVertices.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]),
                                                        words.Count < 3 ? 0 : float.Parse(words[2])));
                        break;

                    case "vn":
                        normals.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                        break;

                    // face
                    case "f":
                        foreach (string w in words)
                        {
                            if (w.Length == 0)
                                continue;

                            string[] comps = w.Split('/');

                            // subtract 1: indices start from 1, not 0
                            current.vertexIndices.Add(uint.Parse(comps[0]) - 1);

                            if (comps.Length > 1 && comps[1].Length != 0)
                                current.textureIndices.Add(uint.Parse(comps[1]) - 1);

                            if (comps.Length > 2)
                                current.normalIndices.Add(uint.Parse(comps[2]) - 1);
                        }
                        break;

                    case "o":
                        current = new OBJObject { Name = words[1] };
                        Objects.Add(current);
                        break;

                    default:
                        break;
                    }
                }
            }
            stream.Dispose();
        }

    }
}
