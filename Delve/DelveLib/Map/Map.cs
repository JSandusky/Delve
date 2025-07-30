using System;
using System.Xml;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace DelveLib.Map
{
    public abstract class MapObject
    {
        public bool GetBoolFlag(string name, bool defVal = false)
        {
            if (Flags.ContainsKey(name))
                return bool.Parse(Flags[name]);
            return defVal;
        }

        public Dictionary<string, string> Flags { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Properties { get; private set; } = new Dictionary<string, string>();

        public void ReadFlags(XmlElement elem)
        {
            var flagNodes = elem.SelectNodes(".//flag");
            foreach (var flag in flagNodes)
            {
                var flagElem = flag as XmlElement;
                string flagName = flagElem.GetAttribute("name");
                string flagValue = flagElem.GetAttribute("value");
                Flags.Add(flagName, flagValue);
            }
        }

        public void ReadFields(XmlElement elem)
        {
            var flagNodes = elem.SelectNodes(".//field");
            foreach (var flag in flagNodes)
            {
                var flagElem = flag as XmlElement;
                string flagName = flagElem.GetAttribute("name");
                string flagValue = flagElem.GetAttribute("value");
                Properties.Add(flagName, flagValue);
            }
        }

        public virtual void CloneFields(MapObject into)
        {
            foreach (var flag in Flags)
                into.Flags[flag.Key] = flag.Value;
            foreach (var prop in Properties)
                into.Properties[prop.Key] = prop.Value;
        }

        public virtual void Serialize(BinaryWriter stream)
        {
            stream.Write(Flags.Count);
            foreach (var flag in Flags)
            {
                stream.Write(flag.Key);
                stream.Write(flag.Value);
            }
            stream.Write(Properties.Count);
            foreach (var prop in Properties)
            {
                stream.Write(prop.Key);
                stream.Write(prop.Value);
            }
        }

        public virtual void Deserialize(BinaryReader stream)
        {
            int ct = stream.ReadInt32();
            for (int i = 0; i < ct; ++i)
            {
                var key = stream.ReadString();
                var value = stream.ReadString();
                Flags[key] = value;
            }
            ct = stream.ReadInt32();
            for (int i = 0; i < ct; ++i)
            {
                var key = stream.ReadString();
                var value = stream.ReadString();
                Properties[key] = value;
            }
        }
    }

    [DebuggerDisplay("{DebugDisplayString,nq}")]
    public class Vertex : MapObject
    {
        public int Index;
        public Vector2 Position;
        public List<LineDef> Lines = new List<LineDef>();

        internal string DebugDisplayString
        {
            get {
                return string.Format("(V) {0}, {1} {2}", Index, (int)Position.X, (int)Position.Y);
            }
        }

        public float LowestPoint
        {
            get
            {
                float lowest = float.MaxValue;
                foreach (var line in Lines)
                {
                    if (line.Front != null)
                        lowest = Math.Min(lowest, line.Front.Sector.FloorHeight);
                    if (line.Back != null)
                        lowest = Math.Min(lowest, line.Back.Sector.FloorHeight);
                }
                return lowest;
            }
        }
        public float HighestPoint
        {
            get
            {
                float highest = float.MinValue;
                foreach (var line in Lines)
                {
                    if (line.Front != null)
                        highest = Math.Max(highest, line.Front.Sector.CeilingHeight);
                    if (line.Back != null)
                        highest = Math.Max(highest, line.Back.Sector.CeilingHeight);
                }
                return highest;
            }
        }

        public override void CloneFields(MapObject into)
        {
            Vertex v = (Vertex)into;
            v.Index = Index;
            v.Position = Position;
            base.CloneFields(into);
        }

        public override void Serialize(BinaryWriter stream)
        {
            stream.Write(Position.X);
            stream.Write(Position.Y);
            base.Serialize(stream);
        }

        public override void Deserialize(BinaryReader stream)
        {
            var x = stream.ReadSingle();
            var y = stream.ReadSingle();
            Position = new Vector2(x, y);
            base.Deserialize(stream);
        }
    }

    public class SideDefPart : MapObject
    {
        public Vector2 TextureOffset;
        public bool Pegged;
        public string TextureName;

        public override void CloneFields(MapObject into)
        {
            SideDefPart s = (SideDefPart)into;
            s.Pegged = Pegged;
            s.TextureName = TextureName;
            s.TextureOffset = TextureOffset;
            base.CloneFields(into);
        }

        public override void Serialize(BinaryWriter stream)
        {
            stream.Write(TextureOffset.X);
            stream.Write(TextureOffset.Y);
            stream.Write(Pegged);
            stream.Write(TextureName);
            base.Serialize(stream);
        }

        public override void Deserialize(BinaryReader stream)
        {
            var x = stream.ReadSingle();
            var y = stream.ReadSingle();
            TextureOffset = new Vector2(x, y);
            Pegged = stream.ReadBoolean();
            TextureName = stream.ReadString();
            base.Deserialize(stream);
        }
    }

    [DebuggerDisplay("{DebugDisplayString,nq}")]
    public class SideDef : MapObject
    {
        public int Index;
        public LineDef Line;
        public Sector Sector;
        public SideDefPart Upper;
        public SideDefPart Middle;
        public SideDefPart Lower;
        public bool IsFront;
        public Vector2 TextureOffset = new Vector2();

        internal string DebugDisplayString
        {
            get
            {
                return string.Format("(S) {0}, {1} {2}", Index, (int)Line.Start.Index, (int)Line.End.Index);
            }
        }

        public SideDef Opposite
        {
            get { return Line.Front == this ? Line.Back : Line.Front; }
        }

        public static bool IsConnected(SideDef from, SideDef other)
        {
            if (from == null || other == null)
                return false;
            if (from.IsFront)
            {
                if (other.IsFront)
                    return other.Line.Start.Equals(from.Line.End);
                else
                    return other.Line.End.Equals(from.Line.End);
            }
            else
            {
                if (other.IsFront)
                    return other.Line.Start.Equals(from.Line.Start);
                else
                    return other.Line.End.Equals(from.Line.Start);
            }
        }

        public SideDef GetNext(Map map, HashSet<SideDef> ignore = null)
        {
            for (int i = 0; i < map.Sides.Count; ++i)
            {
                SideDef s = map.Sides[i];
                if (s == this)
                    continue;
                if (IsConnected(s, this) && (ignore == null || (ignore != null && !ignore.Contains(s))))
                    return s;
            }
            return null;
        }

        public override void CloneFields(MapObject into)
        {
            SideDef s = (SideDef)into;
            s.Index = Index;
            if (Upper != null)
            {
                s.Upper = new SideDefPart();
                s.Upper.CloneFields(Upper);
            }
            if (Middle != null)
            {
                s.Middle = new SideDefPart();
                s.Middle.CloneFields(Middle);
            }
            if (Lower != null)
            {
                s.Lower = new SideDefPart();
                s.Lower.CloneFields(Lower);
            }
            s.IsFront = IsFront;
            s.TextureOffset = TextureOffset;
            base.CloneFields(into);
        }

        public override void Serialize(BinaryWriter stream)
        {
            stream.Write(Line.Index);
            stream.Write(Sector.Index);
            if (Upper != null)
            {
                stream.Write(true);
                Upper.Serialize(stream);
            }
            else
                stream.Write(false);
            if (Middle != null)
            {
                stream.Write(true);
                Middle.Serialize(stream);
            }
            else
                stream.Write(false);
            if (Lower != null)
            {
                stream.Write(true);
                Lower.Serialize(stream);
            }
            else
                stream.Write(false);

            stream.Write(IsFront);
            stream.Write(TextureOffset.X);
            stream.Write(TextureOffset.Y);
            base.Serialize(stream);
        }

        public override void Deserialize(BinaryReader stream)
        {
            Line = new LineDef() { Index = stream.ReadInt32() };
            Sector = new Sector() { Index = stream.ReadInt32() };
            if (stream.ReadBoolean())
            {
                Upper = new SideDefPart();
                Upper.Deserialize(stream);
            }
            if (stream.ReadBoolean())
            {
                Middle = new SideDefPart();
                Middle.Deserialize(stream);
            }
            if (stream.ReadBoolean())
            {
                Lower = new SideDefPart();
                Lower.Deserialize(stream);
            }
            IsFront = stream.ReadBoolean();
            var x = stream.ReadSingle();
            var y = stream.ReadSingle();
            TextureOffset = new Vector2(x, y);
            base.Deserialize(stream);
        }
    }

    [DebuggerDisplay("{DebugDisplayString,nq}")]
    public class LineDef : MapObject
    {
        public int Index;
        public SideDef Front;
        public SideDef Back;
        public Vertex Start;
        public Vertex End;
        public Vector2 FaceNormal;
        public int ActionCode;
        public int[] ActionParams = new int[5] { 0, 0, 0, 0, 0 };
        public int Tag;

        public bool IsSolid { get { return Meta.Impassable && !Meta.DoubleSided; } }
        public bool IsHidden { get { return Meta.IsHidden; } }
        public bool IsSecret { get { return Meta.IsSecret; } }
        public class MetaData
        {
            public bool Impassable = false;
            public bool DoubleSided = false;
            public bool TaskMark = false;
            public bool IsHidden = false;
            public bool IsSecret = false;
            public bool DontPegBottom = false;
            public bool DontPegTop = false;
        }
        public MetaData Meta = new LineDef.MetaData();

        public bool HasTwoSides { get { return Front != null && Back != null; } }

        internal string DebugDisplayString { get { return string.Format("(L) {0}, {1} {2}", Index, Start.Index, End.Index); } }

        public void NormalizeSides()
        {
            // If only 1 side then swap if it's the back-face
            if (Front == null && Back != null)
                Front = Back;
        }

        public LineDef GetNextSolid()
        {
            for (int i = 0; i < End.Lines.Count; ++i)
            {
                LineDef l = End.Lines[i];
                if (l.GetSharedVertex(this) == End && l.Meta.Impassable)
                    return l;
            }
            return null;
        }

        public LineDef GetPrevSolid()
        {
            for (int i = 0; i < Start.Lines.Count; ++i)
            {
                LineDef l = Start.Lines[i];
                if (l.GetSharedVertex(this) == Start && l.Meta.Impassable)
                    return l;
            }
            return null;
        }

        public LineDef GetNextSolid(Sector s)
        {
            for (int i = 0; i < End.Lines.Count; ++i)
            {
                LineDef l = End.Lines[i];
                if (l.Start == End && l.Meta.Impassable && l.Front.Sector == s)
                    return l;
            }
            return null;
        }

        public LineDef GetPrevSolid(Sector s)
        {
            for (int i = 0; i < End.Lines.Count; ++i)
            {
                LineDef l = End.Lines[i];
                if (l.End == Start && l.Meta.Impassable && l.Front.Sector == s)
                    return l;
            }
            return null;
        }

        public Vertex GetSharedVertex(LineDef rhs)
        {
            if (rhs.Start == Start || rhs.End == Start)
                return Start;
            if (rhs.End == End || rhs.Start == End)
                return End;
            return null;
        }

        public bool IsSameAs(LineDef other)
        {
            if (Start == other.Start && End == other.End)
                return true;
            else if (Start == other.End && End == other.Start)
                return true;
            return false;
        }

        public Vector2 LineVec { get { return End.Position - Start.Position; } }

        public override void CloneFields(MapObject into)
        {
            LineDef l = (LineDef)into;
            l.Index = Index;
            l.ActionCode = ActionCode;
            l.FaceNormal = FaceNormal;
            l.Tag = Tag;
            l.ActionParams[0] = ActionParams[0];
            l.ActionParams[1] = ActionParams[1];
            l.ActionParams[2] = ActionParams[2];
            l.ActionParams[3] = ActionParams[3];
            l.ActionParams[4] = ActionParams[5];
            base.CloneFields(into);
        }

        public override void Serialize(BinaryWriter stream)
        {
            stream.Write(Front != null);
            stream.Write(Front != null ? Front.Index : 0);
            stream.Write(Back != null);
            stream.Write(Back != null ? Front.Index : 0);
            stream.Write(Start.Index);
            stream.Write(End.Index);
            stream.Write(ActionCode);
            stream.Write(ActionParams[0]);
            stream.Write(ActionParams[1]);
            stream.Write(ActionParams[2]);
            stream.Write(ActionParams[3]);
            stream.Write(ActionParams[4]);
            stream.Write(Tag);
            base.Serialize(stream);
        }

        public override void Deserialize(BinaryReader stream)
        {
            if (stream.ReadBoolean())
                Front = new SideDef { Index = stream.ReadInt32() };
            if (stream.ReadBoolean())
                Back = new SideDef { Index = stream.ReadInt32() };
            Start = new DelveLib.Map.Vertex() { Index = stream.ReadInt32() };
            End = new DelveLib.Map.Vertex() { Index = stream.ReadInt32() };
            ActionCode = stream.ReadInt32();
            ActionParams[0] = stream.ReadInt32();
            ActionParams[1] = stream.ReadInt32();
            ActionParams[2] = stream.ReadInt32();
            ActionParams[3] = stream.ReadInt32();
            ActionParams[4] = stream.ReadInt32();
            Tag = stream.ReadInt32();
            base.Deserialize(stream);
        }

        public bool IsDiscontinuity()
        {
            return Front != null && Back != null && (Front.Sector.FloorHeight != Back.Sector.FloorHeight || Front.Sector.CeilingHeight != Back.Sector.CeilingHeight);
        }
    }

    public class Thing : MapObject
    {
        public int Index;
        public int ThingTypeID;
        public Vector3 Position;
        public float Angle;
        public int Tag;
        public int Action;

        public override void CloneFields(MapObject into)
        {
            Thing t = (Thing)into;
            t.Index = Index;
            t.ThingTypeID = ThingTypeID;
            t.Position = Position;
            t.Angle = Angle;
            t.Tag = Tag;
            t.Action = Action;
            base.CloneFields(into);
        }

        public override void Serialize(BinaryWriter stream)
        {
            stream.Write(ThingTypeID);
            stream.Write(Position.X);
            stream.Write(Position.Y);
            stream.Write(Position.Z);
            stream.Write(Angle);
            stream.Write(Tag);
            stream.Write(Action);
            base.Serialize(stream);
        }

        public override void Deserialize(BinaryReader stream)
        {
            ThingTypeID = stream.ReadInt32();
            Position = new Vector3();
            Position.X = stream.ReadSingle();
            Position.Y = stream.ReadSingle();
            Position.Z = stream.ReadSingle();
            Angle = stream.ReadSingle();
            Tag = stream.ReadInt32();
            Action = stream.ReadInt32();
            base.Deserialize(stream);
        }
    }
    
    public class Sector : MapObject
    {
        public int Index;
        public int FloorHeight;
        public int CeilingHeight;
        public int Brightness;
        public int Tag;
        public int Special;
        public Plane? FloorSlope;
        public Plane? CeilingSlope;
        public string CeilingTextureName;
        public string FloorTextureName;

        public List<SideDef> Sides = new List<SideDef>();
        public List<LineDef> Lines = new List<LineDef>();
        public List<Vertex> Vertices = new List<Vertex>();

        public g3.Polygon2d Polygon;
        public Rectangle bounds;

        public Plane FloorPlane
        {
            get {
                if (!FloorSlope.HasValue)
                    return new Plane(Vector3.UnitY, FloorHeight);
                return FloorSlope.Value;
            }
        }

        public Plane CeilingPlane
        {
            get
            {
                if (!CeilingSlope.HasValue)
                    return new Plane(-Vector3.UnitY, CeilingHeight);
                return CeilingSlope.Value;
            }
        }

        public void CalculatePolygon()
        {
            Polygon = this.GetSectorPolygon();
            bounds = new Rectangle((int)Polygon.Bounds.Min.x, (int)Polygon.Bounds.Max.y, (int)Polygon.Bounds.Width, (int)Polygon.Bounds.Height);
        }

        public void CalculateLineNormals()
        {
            for (int i = 0; i < Lines.Count; ++i)
            {
                Vector2 lineVec = Vector2.Normalize(Lines[i].LineVec);
                Lines[i].FaceNormal = Vector2.Normalize(lineVec.Rotate(90.0f));
            }
        }

        public void ClearLineMarks()
        {
            foreach (var l in Lines)
                l.Meta.TaskMark = false;
        }

        public List<SideDef[]> GetLoops()
        {
            List<SideDef[]> ret = new List<SideDef[]>();
            List<SideDef> taken = new List<SideDef>();
            while (taken.Count != Sides.Count)
            {
                List<SideDef> working = new List<SideDef>();
                SideDef cur = Sides[0];
                taken.Add(cur);
                working.Add(cur);
                SideDef next = null;
                do
                {
                    // get whatever's connected to us, and not taken
                    next = Sides.FirstOrDefault(s => s.Line.GetSharedVertex(cur.Line) != null && !taken.Contains(s));
                    if (next != null)
                    {
                        taken.Add(next);
                        working.Add(next);
                        cur = next;
                    }
                } while (next != null);
                if (working.Count > 0)
                    ret.Add(working.ToArray());
            }
            return ret;
        }

        public override void CloneFields(MapObject into)
        {
            Sector s = (Sector)into;
            s.Index = Index;
            s.FloorHeight = FloorHeight;
            s.CeilingHeight = CeilingHeight;
            s.Brightness = Brightness;
            s.Tag = Tag;
            s.Special = Special;
            s.FloorTextureName = FloorTextureName;
            s.CeilingTextureName = CeilingTextureName;
            if (FloorSlope.HasValue)
                s.FloorSlope = new Plane(FloorSlope.Value.Normal, FloorSlope.Value.D);
            if (CeilingSlope.HasValue)
                s.CeilingSlope = new Plane(CeilingSlope.Value.Normal, CeilingSlope.Value.D); ;
            base.CloneFields(into);
        }

        public override void Serialize(BinaryWriter stream)
        {
            stream.Write(FloorHeight);
            stream.Write(CeilingHeight);
            stream.Write(Brightness);
            stream.Write(Tag);
            stream.Write(Special);
            if (FloorSlope.HasValue)
            {
                stream.Write(true);
                stream.Write(FloorSlope.Value.D);
                stream.Write(FloorSlope.Value.Normal.X);
                stream.Write(FloorSlope.Value.Normal.Y);
                stream.Write(FloorSlope.Value.Normal.Z);
            }
            else
                stream.Write(false);
            if (CeilingSlope.HasValue)
            {
                stream.Write(true);
                stream.Write(CeilingSlope.Value.D);
                stream.Write(CeilingSlope.Value.Normal.X);
                stream.Write(CeilingSlope.Value.Normal.Y);
                stream.Write(CeilingSlope.Value.Normal.Z);
            }
            else
                stream.Write(false);
            stream.Write(FloorTextureName);
            stream.Write(CeilingTextureName);

            stream.Write(Sides.Count);
            foreach (var s in Sides)
                stream.Write(s.Index);
            stream.Write(Lines.Count);
            foreach (var l in Lines)
                stream.Write(l.Index);
            foreach (var v in Vertices)
                stream.Write(v.Index);

            base.Serialize(stream);
        }

        public override void Deserialize(BinaryReader stream)
        {
            FloorHeight = stream.ReadInt32();
            CeilingHeight = stream.ReadInt32();
            Brightness = stream.ReadInt32();
            Tag = stream.ReadInt32();
            Special = stream.ReadInt32();
            if (stream.ReadBoolean())
            {
                float d = stream.ReadSingle();
                float x = stream.ReadSingle();
                float y = stream.ReadSingle();
                float z = stream.ReadSingle();
                FloorSlope = new Plane(new Vector3(x, y, z), d);
            }
            if (stream.ReadBoolean())
            {
                float d = stream.ReadSingle();
                float x = stream.ReadSingle();
                float y = stream.ReadSingle();
                float z = stream.ReadSingle();
                CeilingSlope = new Plane(new Vector3(x, y, z), d);
            }
            FloorTextureName = stream.ReadString();
            CeilingTextureName = stream.ReadString();

            int ct = stream.ReadInt32();
            for (int i = 0; i < ct; ++i)
                Sides.Add(new SideDef { Index = stream.ReadInt32() });
            ct = stream.ReadInt32();
            for (int i = 0; i < ct; ++i)
                Lines.Add(new LineDef { Index = stream.ReadInt32() });
            ct = stream.ReadInt32();
            for (int i = 0; i < ct; ++i)
                Vertices.Add(new Vertex { Index = stream.ReadInt32() });
            base.Deserialize(stream);
        }
    }

    public class Map
    {
        public string MapName = "";
        public Vector2 Centroid;
        public List<Vertex> Vertices = new List<Vertex>();
        public List<LineDef> Lines = new List<LineDef>();
        public List<SideDef> Sides = new List<SideDef>();
        public List<Sector> Sectors = new List<Sector>();
        public List<Thing> Things = new List<Thing>();

        /// <summary>
        /// Perform tasks required just after loading, filtering and calculation duties
        /// </summary>
        void PostLoad()
        {
            foreach (var l in Lines)
            {
                if (l.Back == null)
                    l.Meta.Impassable = true;
                else
                    l.Meta.Impassable = l.GetBoolFlag("blockmonsters") || l.GetBoolFlag("1");
                l.Meta.DoubleSided = l.GetBoolFlag("twosided") || l.GetBoolFlag("4");
                l.Meta.IsHidden = l.GetBoolFlag("dontdraw") || l.GetBoolFlag("128");
                l.Meta.IsSecret = l.GetBoolFlag("secret") || l.GetBoolFlag("32");
                l.Meta.DontPegTop = l.GetBoolFlag("dontpegtop") || l.GetBoolFlag("8");
                l.Meta.DontPegBottom = l.GetBoolFlag("dontpegbottom") || l.GetBoolFlag("16");
            }
            LinkVerticesToLines();

            CalculateGeometricData();
        }

        public void CalculateGeometricData()
        {
            CalculateCentroid();
            CalculateFaceNormals();
            for (int i = 0; i < Sectors.Count; ++i)
                Sectors[i].CalculatePolygon();
        }

        public void LinkVerticesToLines()
        {
            foreach (var v in Vertices)
                v.Lines.Clear();
            foreach (var l in Lines)
            {
                l.Start.Lines.Add(l);
                l.End.Lines.Add(l);
            }
        }

        public void CalculateCentroid()
        {
            Vector2 c = new Vector2(0, 0);
            for (int i = 0; i < Vertices.Count; ++i)
                c += Vertices[i].Position;
            if (Vertices.Count > 0)
                Centroid = c / Vertices.Count;
            else
                Centroid = c;
        }

        public void Combine(Map rhs)
        {
            Reindex(rhs);
            Merge(rhs);
        }

        /// <summary>
        /// Reindexes the given map against this one, indexes for this map remain
        /// </summary>
        public void Reindex(Map rhs)
        {
            for (int i = 0; i < rhs.Vertices.Count; ++i)
                rhs.Vertices[i].Index += Vertices.Count;
            for (int i = 0; i < rhs.Lines.Count; ++i)
                rhs.Lines[i].Index += Lines.Count;
            for (int i = 0; i < rhs.Sides.Count; ++i)
                rhs.Sides[i].Index += Sides.Count;
            for (int i = 0; i < rhs.Sectors.Count; ++i)
                rhs.Sectors[i].Index += Sectors.Count;
        }

        /// Reindexes this map
        public void Reindex()
        {
            int i = 0;
            foreach (var v in Vertices)
                v.Index = i++;
            i = 0;
            foreach (var s in Sides)
                s.Index = i++;
            i = 0;
            foreach (var l in Lines)
                l.Index = i++;
            i = 0;
            foreach (var s in Sectors)
                s.Index = i++;
            i = 0;
            foreach (var t in Things)
                t.Index = i++;
                
        }

        public void Merge(Map rhs)
        {
            Vertices.AddRange(rhs.Vertices);
            Lines.AddRange(rhs.Lines);
            Sides.AddRange(rhs.Sides);
            Sectors.AddRange(rhs.Sectors);

            LinkVerticesToLines();
            CalculateGeometricData();
        }

        public void MergeIntoThis(Map rhs)
        {
            List<Vertex> newVerts = new List<Vertex>();
            newVerts.AddRange(rhs.Vertices);
            List<LineDef> newLines = new List<LineDef>();
            newLines.AddRange(rhs.Lines);

            for (int i = 0; i < rhs.Vertices.Count; ++i)
            {
                var vert = rhs.Vertices[i];
                var found = Vertices.FirstOrDefault(v => v.Position == vert.Position);
                if (found != null)
                {
                    newVerts[i] = found;
                }
                else
                    Vertices.Add(vert);
            }

            for (int i = 0; i < rhs.Lines.Count; ++i)
            {
                var line = rhs.Lines[i];
                int startIdx = rhs.Vertices.IndexOf(line.Start);
                int endIdx = rhs.Vertices.IndexOf(line.End);
                line.Start = newVerts[startIdx];
                line.End = newVerts[endIdx];

                var found = Lines.FirstOrDefault(l => l.IsSameAs(line));
                if (found != null)
                    newLines[i] = found;
                else
                    Lines.Add(line);
            }

            for (int i = 0; i < rhs.Sides.Count; ++i)
            {
                var side = rhs.Sides[i];
                int lineIdx = rhs.Lines.IndexOf(side.Line);
                if (side.Line != newLines[lineIdx])
                {
                    side.Line = newLines[lineIdx];
                    if (side.Line.Front == null)
                        side.Line.Front = side;
                    else if (side.Line.Back == null)
                        side.Line.Back = side;
                    else
                    {
                        //TODO: this is an error right?
                        continue;
                    }
                }
                Sides.Add(side);
            }

            for (int i = 0; i < rhs.Sectors.Count; ++i)
            {
                var sector = rhs.Sectors[i];
                for (int v = 0; v < sector.Vertices.Count; ++v)
                {
                    int oldIdx = rhs.Vertices.IndexOf(sector.Vertices[v]);
                    sector.Vertices[v] = newVerts[oldIdx];
                }

                for (int l = 0; l < sector.Lines.Count; ++l)
                {
                    int oldIdx = rhs.Lines.IndexOf(sector.Lines[l]);
                    sector.Lines[l] = newLines[oldIdx];
                }
                Sectors.Add(sector);
            }

            LinkVerticesToLines();
            CalculateGeometricData();
        }

        /// <summary>
        /// Aligns a map to this one so that two of their sidedefs are colinear
        /// </summary>
        /// <param name="thisSide">side of this map that is to be aligned against</param>
        /// <param name="rhs">map that will be transformed</param>
        /// <param name="otherSide">side from the other map that needs to line up with this side</param>
        /// <returns>true if success</returns>
        public bool AlignSides(SideDef thisSide, Map rhs, SideDef otherSide)
        {
            if (thisSide.Line.HasTwoSides || otherSide.Line.HasTwoSides)
                return false;
            if (Math.Abs(thisSide.Line.LineVec.Length() - otherSide.Line.LineVec.Length()) > 0.001f)
                return false;

            LineDef thisLine = thisSide.Line;
            LineDef otherLine = otherSide.Line;

            // rotate so the lines are aligned
            float angleBetween = thisLine.FaceNormal.AngleBetween(otherLine.FaceNormal);
            rhs.Transform(Vector2.Zero, -angleBetween);

            // translate so the lines are on top of each-other
            Vector2 otherLineCenter = otherLine.LineVec * 0.5f;
            Vector2 thisLineCenter = thisLine.LineVec * 0.5f;
            Vector2 delta = thisLineCenter - otherLineCenter;
            rhs.Transform(delta, 0);

            otherLine.Start.Position = thisLine.End.Position;
            otherLine.End.Position = thisLine.Start.Position;

            // once aligned, MergeIntoThis() will take care of things

            return true;
        }

        /// <summary>
        /// Moves and|or rotates all of the vertices in the map.
        /// </summary>
        /// <param name="moveBy">Units to move the vertices by</param>
        /// <param name="rotateBy">degrees to rotate by, rotation is performed first, should only be in units of 90</param>
        public void Transform(Vector2 moveBy, float rotateBy)
        {
            for (int i = 0; i < Vertices.Count; ++i)
            {
                if (rotateBy != 0)
                {
                    Vector2 centroidRel = Vertices[i].Position - Centroid;
                    Vertices[i].Position = centroidRel.Rotate(rotateBy) + Centroid;
                }
                if (moveBy.X != 0 && moveBy.Y != 0)
                    Vertices[i].Position += moveBy;
            }

            Vector3 thingMove = new Vector3(moveBy.X, moveBy.Y, 0);
            for (int i = 0; i < Things.Count; ++i)
            {
                if (rotateBy != 0)
                {
                    Vector2 centroidRel = Things[i].Position.XY() - Centroid;
                    Vector2 newXY = centroidRel.Rotate(rotateBy) + Centroid;
                    Things[i].Position = new Vector3(newXY.X, newXY.Y, Things[i].Position.Z);
                    Vector2 thingAng = new Vector2(1, 0).Rotate(Things[i].Angle);
                    thingAng = thingAng.Rotate(rotateBy);
                    Things[i].Angle = thingAng.AngleBetween(new Vector2(1, 0));
                }

                if (moveBy.X != 0 && moveBy.Y != 0)
                    Things[i].Position += thingMove;
            }

            CalculateGeometricData();
        }

        public void FlipVertical() { Flip(true, false); }

        public void FlipHorizontal() { Flip(false, true); }

        public void Flip(bool vertical, bool horizontal)
        {
            Vector2 mulvec = new Vector2(horizontal ? -1 : 1, vertical ? -1 : 1);
            for (int i = 0; i < Vertices.Count; ++i)
            {
                Vector2 centroidRel = Vertices[i].Position - Centroid;
                centroidRel *= mulvec;
                Vertices[i].Position = Centroid + centroidRel;
            }

            for (int i = 0; i < Things.Count; ++i)
            {
                Vector2 centroidRel = Things[i].Position.XY() - Centroid;
                centroidRel *= mulvec;
                Vector2 adj = Centroid + centroidRel;
                Things[i].Position = new Vector3(adj.X, adj.Y, Things[i].Position.Z);
                Vector2 thingAng = new Vector2(1, 0).Rotate(Things[i].Angle);
                thingAng *= mulvec;
                Things[i].Angle = thingAng.AngleBetween(new Vector2(1, 0));
            }

            CalculateGeometricData();
        }

        public void CalculateFaceNormals()
        {
            for (int i = 0; i < Lines.Count; ++i)
                Lines[i].FaceNormal = Vector2.Zero;
            for (int i = 0; i < Sectors.Count; ++i)
                Sectors[i].CalculateLineNormals();
            for (int i = 0; i < Lines.Count; ++i)
                Lines[i].FaceNormal.Normalize();
        }

        /// <summary>
        /// Determine if the given map overlaps with this one
        /// </summary>
        /// <param name="rhs">map to test against</param>
        /// <returns>true if a sector in the other map overlaps this one</returns>
        public bool Overlaps(Map rhs)
        {
            for (int i = 0; i < Sectors.Count; ++i)
            {
                for (int y = 0; y < rhs.Sectors.Count; ++y)
                {
                    if (Sectors[i].Polygon.Intersects(rhs.Sectors[y].Polygon))
                        return true;
                }
            }
            return false;
        }

        static char[] splitStr = { ',' };
        public void Read(XmlElement fromElem)
        {
            foreach (XmlNode node in fromElem.SelectNodes("//v"))
            {
                XmlElement elem = node as XmlElement;
                if (elem != null)
                {
                    Vertex vert = new Vertex();
                    vert.Index = Vertices.Count;
                    int vertexId = elem.GetIntAttribute("id");
                    float x = elem.GetFloatAttribute("x");
                    float y = elem.GetFloatAttribute("y");
                    vert.Position = new Vector2(x, y);
                    vert.ReadFlags(elem);
                    vert.ReadFields(elem);
                    Vertices.Add(vert);
                }
            }

            foreach (XmlNode node in fromElem.SelectNodes("//line"))
            {
                XmlElement elem = node as XmlElement;
                if (elem != null)
                {
                    LineDef l = new LineDef();
                    l.Index = Lines.Count;
                    int aVert = elem.GetIntAttribute("vert-start");
                    int bVert = elem.GetIntAttribute("vert-end");
                    l.Start = Vertices[aVert];
                    l.End = Vertices[bVert];
                    l.Tag = elem.GetIntAttribute("tag");
                    l.ActionCode = elem.GetIntAttribute("action");
                    string[] args = elem.GetAttribute("action-args").Split(splitStr, StringSplitOptions.RemoveEmptyEntries);
                    if (args != null)
                    {
                        for (int i = 0; i < args.Length && i < 5; ++i)
                            l.ActionParams[i] = int.Parse(args[i]);
                    }
                    l.ReadFlags(elem);
                    l.ReadFields(elem);
                    Lines.Add(l);
                }
            }

            foreach (XmlNode node in fromElem.SelectNodes("//side"))
            {
                XmlElement elem = node as XmlElement;
                if (elem != null)
                {
                    SideDef s = new SideDef();
                    s.Index = Sides.Count;
                    s.Line = Lines[elem.GetIntAttribute("line")];
                    s.IsFront = elem.GetBoolAttribute("front");
                    if (s.IsFront)
                        s.Line.Front = s;
                    else
                        s.Line.Back = s;

                    if (elem.HasAttribute("high-tex"))
                        s.Upper = new SideDefPart { TextureName = elem.GetAttribute("high-tex") };
                    if (elem.HasAttribute("middle-tex"))
                        s.Middle = new SideDefPart { TextureName = elem.GetAttribute("middle-tex") };
                    if (elem.HasAttribute("low-tex"))
                        s.Lower = new SideDefPart { TextureName = elem.GetAttribute("high-tex") };

                    s.TextureOffset.X = elem.GetFloatAttribute("offset-x");
                    s.TextureOffset.Y = elem.GetFloatAttribute("offset-y");

                    s.ReadFlags(elem);
                    s.ReadFields(elem);
                    Sides.Add(s);
                }
            }

            foreach (XmlNode node in fromElem.SelectNodes("//sector"))
            {
                XmlElement elem = node as XmlElement;
                if (elem != null)
                {
                    Sector s = new Sector();
                    s.Index = Sectors.Count;

                    s.FloorHeight = elem.GetIntAttribute("floor-height");
                    s.CeilingHeight = elem.GetIntAttribute("ceiling-height");
                    s.FloorTextureName = elem.GetAttribute("floor-tex");
                    s.CeilingTextureName = elem.GetAttribute("ceiling-tex");
                    s.Brightness = elem.GetIntAttribute("lighting");
                    s.Tag = elem.GetIntAttribute("tag");
                    s.Special = elem.GetIntAttribute("special");
                    s.ReadFlags(elem);
                    s.ReadFields(elem);

                    var sidesElem = elem.SelectSingleNode("sides");
                    if (sidesElem != null)
                    {
                        string[] sides = sidesElem.InnerText.Split(splitStr, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string sideID in sides)
                        {
                            SideDef side = Sides[int.Parse(sideID)];
                            side.Sector = s;
                            s.Sides.Add(side);
                            if (!s.Lines.Contains(side.Line))
                                s.Lines.Add(side.Line);
                        }
                    }

                    // triangulation vertices, will be a multiple of 3
                    var verticesElem = elem.SelectSingleNode("vertices");
                    if (verticesElem != null)
                    {
                        string[] vertices = verticesElem.InnerText.Split(splitStr, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string vertID in vertices)
                            s.Vertices.Add(Vertices[int.Parse(vertID)]);
                    }

                    Sectors.Add(s);
                }
            }

            foreach (XmlNode node in fromElem.SelectNodes("//thing"))
            {
                XmlElement elem = node as XmlElement;
                if (elem != null)
                {
                    Thing t = new DelveLib.Map.Thing();
                    t.Index = Things.Count;
                    Things.Add(t);
                    t.ThingTypeID = elem.GetIntAttribute("type");
                    t.Tag = elem.GetIntAttribute("tag");
                    t.Action = elem.GetIntAttribute("action");
                    t.ReadFlags(elem);
                    t.ReadFields(elem);

                    var pos = elem.SelectSingleNode("pos");
                    if (pos != null)
                    {
                        var posElem = pos as XmlElement;
                        t.Position = new Vector3(posElem.GetFloatAttribute("x"), posElem.GetFloatAttribute("z"), posElem.GetFloatAttribute("y"));
                        t.Angle = posElem.GetFloatAttribute("angle-int");
                    }
                }
            }

            PostLoad();
        }

        /// <summary>
        /// Draws an overhead render to a debug-renderer, Doom automap style
        /// </summary>
        /// <param name="debug">debug-renderer to draw to</param>
        /// <param name="solidColor">color to use for impassable lines</param>
        /// <param name="sectorDiffColor">color to use for lines between sectors of different heights</param>
        /// <param name="secretColor">color to use for secret lines</param>
        public void Draw(DebugRenderer debug, Color solidColor, Color sectorDiffColor, Color secretColor)
        {
            for (int i = 0; i < Lines.Count; ++i)
            {
                if (Lines[i].IsHidden)
                    continue;

                var a = Lines[i].Start.Position;
                var b = Lines[i].End.Position;
                if (Lines[i].IsSecret)
                {
                    debug.DrawLine(
                        new Vector3(a, 0),
                        new Vector3(b, 0),
                        secretColor);
                }
                else if (Lines[i].IsSolid)
                {
                    debug.DrawLine(
                        new Vector3(a, 0), 
                        new Vector3(b, 0),
                        solidColor);
                }
                else if (Lines[i].Front.Sector.FloorHeight != Lines[i].Back.Sector.FloorHeight ||
                        Lines[i].Front.Sector.CeilingHeight != Lines[i].Back.Sector.CeilingHeight)
                {
                    // draw line if the sector heights differ
                    debug.DrawLine(
                        new Vector3(a, 0),
                        new Vector3(b, 0),
                        sectorDiffColor);
                }
            }
        }

        List<T> CloneList<T>(List<T> src) where T : MapObject, new()
        {
            List<T> ret = new List<T>();
            for (int i = 0; i < src.Count; ++i)
            {
                T v = new T();
                v.CloneFields(src[i]);
                ret.Add(v);
            }
            return ret;
        }

        public Map Clone()
        {
            Map ret = new DelveLib.Map.Map();

            ret.Vertices = CloneList(Vertices);
            ret.Sides = CloneList(Sides);
            ret.Lines = CloneList(Lines);
            ret.Sectors = CloneList(Sectors);
            ret.Things = CloneList(Things);

            // Don't need to process verts, `link vertices to lines` will take care of it

            for (int i = 0; i < Lines.Count; ++i)
            {
                var l = Lines[i];
                if (l.Front != null)
                    ret.Lines[i].Front = ret.Sides[l.Front.Index];
                if (l.Back != null)
                    ret.Lines[i].Back = ret.Sides[l.Back.Index];
                ret.Lines[i].Start = ret.Vertices[l.Start.Index];
                ret.Lines[i].End = ret.Vertices[l.End.Index];
            }

            for (int i = 0; i < Sides.Count; ++i)
            {
                var s = Sides[i];
                ret.Sides[i].Line = ret.Lines[s.Line.Index];
                ret.Sides[i].Sector = ret.Sectors[s.Sector.Index];
            }

            for (int i = 0; i < Sectors.Count; ++i)
            {
                var s = Sectors[i];
                var rs = ret.Sectors[i];

                for (int v = 0; v < s.Vertices.Count; ++v)
                    rs.Vertices.Add(ret.Vertices[s.Vertices[v].Index]);
                for (int v = 0; v < s.Lines.Count; ++v)
                    rs.Lines.Add(ret.Lines[s.Lines[v].Index]);
                for (int v = 0; v < s.Sides.Count; ++v)
                    rs.Sides.Add(ret.Sides[s.Sides[v].Index]);
            }

            ret.LinkVerticesToLines();
            ret.CalculateGeometricData();

            return ret;
        }

        public void Serialize(BinaryWriter stream)
        {
            stream.Write(MapName);
            stream.Write(Vertices.Count);
            foreach (var v in Vertices)
                v.Serialize(stream);
            stream.Write(Sides.Count);
            foreach (var s in Sides)
                s.Serialize(stream);
            stream.Write(Lines.Count);
            foreach (var l in Lines)
                l.Serialize(stream);
            stream.Write(Things.Count);
            foreach (var t in Things)
                t.Serialize(stream);
            stream.Write(Sectors.Count);
            foreach (var s in Sectors)
                s.Serialize(stream);
        }

        public void Deserialize(BinaryReader stream)
        {
            MapName = stream.ReadString();
            int ct = stream.ReadInt32();
            for (int i = 0; i < ct; ++i)
            {
                var v = new Vertex { Index = Vertices.Count };
                v.Deserialize(stream);
                Vertices.Add(v);
            }
            ct = stream.ReadInt32();
            for (int i = 0; i < ct; ++i)
            {
                var s = new SideDef { Index = Sides.Count };
                s.Deserialize(stream);
                Sides.Add(s);
            }
            ct = stream.ReadInt32();
            for (int i = 0; i < ct; ++i)
            {
                var l = new LineDef { Index = Sides.Count };
                l.Deserialize(stream);
                Lines.Add(l);
            }
            ct = stream.ReadInt32();
            for (int i = 0; i < ct; ++i)
            {
                var t = new Thing { Index = Things.Count };
                t.Deserialize(stream);
                Things.Add(t);
            }
            ct = stream.ReadInt32();
            for (int i = 0; i < ct; ++i)
            {
                var s = new Sector { Index = Sectors.Count };
                s.Deserialize(stream);
                Sectors.Add(s);
            }

            foreach (var s in Sides)
            {
                s.Line = Lines[s.Line.Index];
                s.Sector = Sectors[s.Sector.Index];
            }
            foreach (var l in Lines)
            {
                if (l.Front != null)
                    l.Front = Sides[l.Front.Index];
                if (l.Back != null)
                    l.Back = Sides[l.Back.Index];
                l.Start = Vertices[l.Start.Index];
                l.End = Vertices[l.End.Index];
            }
            foreach (var s in Sectors)
            {
                for (int i = 0; i < s.Lines.Count; ++i)
                    s.Lines[i] = Lines[s.Lines[i].Index];
                for (int i = 0; i < s.Vertices.Count; ++i)
                    s.Vertices[i] = Vertices[s.Vertices[i].Index];
                for (int i = 0; i < s.Sides.Count; ++i)
                    s.Sides[i] = Sides[s.Sides[i].Index];
            }

            PostLoad();
        }

        public Vector4 GetCoords()
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (var v in Vertices)
            {
                min.X = Math.Min(min.X, v.Position.X);
                min.Y = Math.Min(min.Y, v.Position.Y);
                max.X = Math.Max(max.X, v.Position.X);
                max.Y = Math.Max(max.Y, v.Position.Y);
            }
            return new Vector4(min.X, min.Y, max.X, max.Y);
        }

        public System.Drawing.Bitmap Render(int desiredDim)
        {
            int w = desiredDim;
            int h = desiredDim;

            var v = GetCoords();
            var min = new Vector2(v.X, v.Y);
            var r = new Vector2(v.Z, v.W) - new Vector2(v.X, v.Y);

            System.Drawing.Bitmap img = null;
            if (r.X > r.Y)
                img = new System.Drawing.Bitmap(desiredDim, (int)(desiredDim * r.Y / r.X));
            else
                img = new System.Drawing.Bitmap((int)(desiredDim * r.Y / r.X), desiredDim);

            System.Drawing.Pen solidPen = new System.Drawing.Pen(System.Drawing.Color.Red, 2);
            System.Drawing.Pen discontPen = new System.Drawing.Pen(System.Drawing.Color.Gray);
            System.Drawing.Pen secretPen = new System.Drawing.Pen(System.Drawing.Color.Magenta);
            System.Drawing.Pen actionPen = new System.Drawing.Pen(System.Drawing.Color.Yellow, 2);
            using (var graphics = System.Drawing.Graphics.FromImage(img))
            {
                graphics.Clear(System.Drawing.Color.Black);
                var imgDim = new Vector2(img.Width-1, img.Height-1);

                foreach (var line in Lines)
                {
                    if (line.IsHidden)
                        continue;

                    var a = ((line.Start.Position - min) / r);
                    var b = ((line.End.Position - min) / r);
                    a.Y = 1 - a.Y;
                    b.Y = 1 - b.Y;
                    a *= imgDim;
                    b *= imgDim;

                    if (line.IsSecret)
                        graphics.DrawLine(secretPen, new System.Drawing.PointF(a.X, a.Y), new System.Drawing.PointF(b.X, b.Y));
                    else if (line.ActionCode != 0)
                        graphics.DrawLine(actionPen, new System.Drawing.PointF(a.X, a.Y), new System.Drawing.PointF(b.X, b.Y));
                    else if (line.IsSolid)
                        graphics.DrawLine(solidPen, new System.Drawing.PointF(a.X, a.Y), new System.Drawing.PointF(b.X, b.Y));
                    else if (line.IsDiscontinuity())
                        graphics.DrawLine(discontPen, new System.Drawing.PointF(a.X, a.Y), new System.Drawing.PointF(b.X, b.Y));
                }
            }
            solidPen.Dispose();
            discontPen.Dispose();
            secretPen.Dispose();
            actionPen.Dispose();

            return img;
        }
    }
}
