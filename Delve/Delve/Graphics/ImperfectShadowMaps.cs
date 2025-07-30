using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Delve.Graphics
{
    public class ImperfectShadowMaps
    {
        Effect ismShader_;
        Texture2D ismAtlas_;
        int width_;
        int height_;
        int smSize_ = 64;

        public ImperfectShadowMaps(GraphicsDevice device)
        {
            width_ = 1024;
            height_ = 1024;
            ismAtlas_ = new Texture2D(device, width_, height_);
        }

        public ImperfectShadowMaps(GraphicsDevice device, int rowCt)
        {
            width_ = smSize_ * rowCt;
            height_ = smSize_ * rowCt;
            ismAtlas_ = new Texture2D(device, width_, height_);
        }
    }
}
