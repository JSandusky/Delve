using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Delve.Graphics
{
    public interface ICommonEffect
    {
        Matrix Transform { get; set; }
        Matrix WorldViewProjection { get; set; }
        Matrix WorldView { get; set; }

        void Begin(GraphicsDevice device);
        void End(GraphicsDevice device);
    }
}
