using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Delve.Graphics
{
    public struct ShadowBatch
    {
        public Light light_;
        public SortedList<ulong, MeshDraw> batches_;
        public Texture shadowTarget_;
    }

    public class RenderQueue
    {
        MeshBatch batch_;

        List<ShadowBatch> shadowBatches_ = new List<ShadowBatch>();

        public void DetermineShadowBatches()
        {
            var draws = batch_.QueuedDraws;
            foreach (var draw in draws)
            {
                for (int i = 0; i < shadowBatches_.Count; ++i)
                {
                }
            }
        }
    }
}
