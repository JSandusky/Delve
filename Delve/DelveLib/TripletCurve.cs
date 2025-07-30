using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace DelveLib
{
    public class TripletCurve
    {
        public Curve XCurve { get; set; } = new Curve();
        public Curve YCurve { get; set; } = new Curve();
        public Curve ZCurve { get; set; } = new Curve();

        public int Count { get { return XCurve.Keys.Count; } }

        public Vector3 GetKey(int pos)
        {
            return new Vector3(XCurve.Keys[pos].Value, YCurve.Keys[pos].Value, ZCurve.Keys[pos].Value);
        }

        public void RemoveLast()
        {
            if (XCurve.Keys.Count == 0)
                return;
            XCurve.Keys.RemoveAt(XCurve.Keys.Count - 1);
            YCurve.Keys.RemoveAt(YCurve.Keys.Count - 1);
            ZCurve.Keys.RemoveAt(ZCurve.Keys.Count - 1);
            ComputeTangents();
        }

        public void SetKey(int pos, Vector3 v)
        {
            XCurve.Keys[pos].Value = v.X;
            YCurve.Keys[pos].Value = v.Y;
            ZCurve.Keys[pos].Value = v.Z;
            ComputeTangents();
        }

        public void AddKey(float pos, Vector3 value)
        {
            XCurve.Keys.Add(new CurveKey(pos, value.X));
            YCurve.Keys.Add(new CurveKey(pos, value.Y));
            ZCurve.Keys.Add(new CurveKey(pos, value.Z));
        }

        public void ComputeTangents()
        {
            XCurve.ComputeTangents(CurveTangent.Smooth);
            YCurve.ComputeTangents(CurveTangent.Smooth);
            ZCurve.ComputeTangents(CurveTangent.Smooth);
        }

        public Vector3 Evaluate(float td)
        {
            return new Vector3(XCurve.Evaluate(td), YCurve.Evaluate(td), ZCurve.Evaluate(td));
        }
    }
}
