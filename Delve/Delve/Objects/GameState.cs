using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DelveLib;

namespace Delve.Objects
{
    public class GameState
    {
        public Scene Scene = new Scene();
        public Scene Cached;
        public Camera EditorCamera;
        public Camera ActiveCamera;
        public Gizmo Gizmo;

        public GizmoMode GizMode = GizmoMode.Translation;
        public bool GizIsLocal = false;
    }
}
