using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ImGuiCLI;
using DelveLib;
using Microsoft.Xna.Framework;

namespace Delve.Objects
{
    public partial class BillboardStrip : GameObject
    {
        IOCDependency<GameState> gameState_ = new IOCDependency<GameState>();
        IOCDependency<Escher> game_ = new IOCDependency<Escher>();

        public TripletCurve Curve = new TripletCurve();

        public override float? RayTest(ref Ray ray)
        {
            if (Curve.Count == 0)
                return null;
            float minDist = float.MaxValue;
            for (int i  = 0; i < Curve.Count; ++i)
            {
                var pt = Curve.GetKey(i);
                float? val = ray.Intersects(new BoundingSphere(pt, 0.5f));
                if (val.HasValue && val.Value < minDist)
                    minDist = val.Value;
            }
            if (minDist == float.MaxValue)
                return null;
            return minDist;
        }

        public override void PostRay(ref Ray ray, int codeID)
        {
            float minDist = float.MaxValue;
            int knotIdx = 0;
            for (int i = 0; i < Curve.Count; ++i)
            {
                var pt = Curve.GetKey(i);
                float? val = ray.Intersects(new BoundingSphere(pt, 0.5f));
                if (val.HasValue && val.Value < minDist)
                {
                    minDist = val.Value;
                    knotIdx = i;
                }
            }

            Vector3 knotPos = Curve.GetKey(knotIdx);
            gameState_.Object.Gizmo = new DelveLib.Gizmo(gameState_.Object.ActiveCamera, game_.Object.DebugRenderer);
            gameState_.Object.Gizmo.SetTransform(Matrix.CreateTranslation(knotPos));
            gameState_.Object.Gizmo.State.OnChange += (o, e) =>
            {
                Curve.SetKey(knotIdx, e.GetTransform().Translation);
            };
        }

        public override void DrawEditor()
        {
            ImGuiCli.PushID(GetHashCode());
            ImGuiCli.Text("Billboard Strip");
            for (int i = 0; i < Curve.Count; ++i)
            {
                Vector3 v = Curve.GetKey(i);
                if (ImGuiEx.DragFloatN_Colored(i.ToString(), ref v))
                    Curve.SetKey(i, v);
            }
            if (ImGuiCli.Button("Add Key"))
            {
                if (Curve.Count == 0)
                    Curve.AddKey(0, Vector3.Zero);
                else if (Curve.Count == 1)
                    Curve.AddKey(1, Vector3.UnitZ);
                else
                {
                    var delta = Curve.GetKey(Curve.Count - 1) - Curve.GetKey(Curve.Count - 2);
                    Curve.AddKey(Curve.Count, Curve.GetKey(Curve.Count - 1) + delta);
                }
            }
            ImGuiCli.SameLine();
            if (ImGuiCli.Button("Remove Key"))
                Curve.RemoveLast();
            ImGuiCli.PopID();
        }

        public override void DrawDebug(DebugRenderer lines)
        {
            var pos = Curve.Evaluate(0);
            for (int i = 0; i < 128; ++i)
            {
                var nextPos = Curve.Evaluate((i / 128.0f) * Curve.Count);
                lines.DrawLine(pos, nextPos, Color.Pink, DebugDrawDepth.Pass);
                lines.DrawLine(pos, nextPos, Color.DarkRed, DebugDrawDepth.Fail);
                pos = nextPos;
            }
            if (Curve.Count >= 3)
            {
                var a = Curve.GetKey(0);
                var b = Curve.GetKey(1);
                var c = Curve.GetKey(2);
                lines.DrawTriangle(a, b, c, Color.Cyan, DebugDrawDepth.Pass);
                lines.DrawTriangle(a, b, c, new Color(Color.DarkCyan, 0.1f), DebugDrawDepth.Fail);
                lines.DrawWireTriangle(a, b, c, Color.DarkBlue, DebugDrawDepth.Always);
            }
            for (int i = 0; i < Curve.Count; ++i)
            {
                Vector3 v = Curve.GetKey(i);
                lines.DrawCross(v, 0.5f, Color.Yellow, DebugDrawDepth.Always);
            }
        }
    }
}
