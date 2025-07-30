using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Delve
{
    public class RT
    {
        public RenderTarget2D target_;
        GraphicsDevice device_;
        bool withExtras_ = true;

        public RT(GraphicsDevice device, int w, int h, bool wExtras = true)
        {
            device_ = device;
            withExtras_ = wExtras;
            SetRenderTargetSize(w, h);
        }

        public void SetRenderTargetSize(int w, int h)
        {
            if (w <= 0 || h <= 0)
                return;
            if (target_ == null || (target_ != null && target_.Width != w && target_.Height != h))
            {
                if (target_ != null)
                    target_.Dispose();

                if (withExtras_)
                    target_ = new RenderTarget2D(device_, w, h, false, SurfaceFormat.Color, DepthFormat.Depth24);
                else
                    target_ = new RenderTarget2D(device_, w, h, false, SurfaceFormat.Color, DepthFormat.None);
            }
        }

        public void Draw()
        {
            device_.SetRenderTarget(target_);
            device_.Clear(Color.DarkKhaki);
            device_.SetRenderTarget(null);
        }

        public IntPtr GetTargetData()
        {
            return target_.GetNativeHandle();
        }
    }

    public class MRT
    {
        public RenderTarget2D colorTarget_;
        public RenderTarget2D depthTarget_;
        public GraphicsDevice device_;

        public MRT(GraphicsDevice device, int w, int h)
        {
            device_ = device;
            SetTargetSize(w, h);
        }

        public void SetTargetSize(int w, int h)
        {
            if (w <= 0 || h <= 0)
                return;
            if (colorTarget_ == null || (colorTarget_ != null && colorTarget_.Width != w && colorTarget_.Height != h))
            {
                if (colorTarget_ != null)
                    colorTarget_.Dispose();
                if (depthTarget_ != null)
                    depthTarget_.Dispose();

                colorTarget_ = new RenderTarget2D(device_, w, h, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
                depthTarget_ = new RenderTarget2D(device_, w, h, false, SurfaceFormat.Single, DepthFormat.None);
            }
        }

        RenderTargetBinding[] bindings_;

        public void Begin()
        {
            if (bindings_ == null)
            {
                bindings_ = new RenderTargetBinding[] {
                    new RenderTargetBinding(colorTarget_),
                    new RenderTargetBinding(depthTarget_),
                };
            }
            device_.SetRenderTargets(new RenderTargetBinding[] {
                    new RenderTargetBinding(colorTarget_),
                    new RenderTargetBinding(depthTarget_),
                });
            device_.Clear(new Color(0.2f,0.2f,0.2f));
        }

        public void End()
        {
            device_.SetRenderTarget(null);
        }
    }

    public class SSAO
    {
        GraphicsDevice device_;
        SpriteBatch batch_;
        public Effect ssaoBuildEffect_;
        public Effect ssaoCombineEffect_;
        public Effect darkener_;
        public RT ssaoTarget_;
        public RT combineTarget_;

        public SSAO(GraphicsDevice device, ContentManager content)
        {
            device_ = device;
            ssaoTarget_ = new RT(device, 128, 128, false);
            combineTarget_ = new RT(device, 128, 128, false);
            ssaoBuildEffect_ = content.Load<Effect>("Effects/SSAO");
            ssaoCombineEffect_ = content.Load<Effect>("Effects/SSAOCombine");
            darkener_ = content.Load<Effect>("Effects/Darkener");
            batch_ = new SpriteBatch(device);
        }

        public void RunSSAO(MRT mrtTarget, int w, int h)
        {
            ssaoTarget_.SetRenderTargetSize(w/2, h/2);
            combineTarget_.SetRenderTargetSize(w, h);

            // Calculate SSAO values
            device_.SetRenderTarget(ssaoTarget_.target_);
            batch_.Begin(SpriteSortMode.Deferred, null, null, null, null, ssaoBuildEffect_);
            batch_.Draw(mrtTarget.depthTarget_, new Rectangle(0, 0, w/2, h/2), Color.White);
            batch_.End();

            // Blit MRT color over into a texture for SSAO combine
            device_.SetRenderTarget(combineTarget_.target_);
            batch_.Begin();
            batch_.Draw(mrtTarget.colorTarget_, new Rectangle(0, 0, w, h), Color.White);
            batch_.End();

            // Combine with color target
            device_.SetRenderTarget(mrtTarget.colorTarget_);
            ssaoCombineEffect_.Parameters["SSAOTexture"].SetValue(ssaoTarget_.target_);
            batch_.Begin(SpriteSortMode.Deferred, null, null, null, null, ssaoCombineEffect_);
            batch_.Draw(combineTarget_.target_, new Rectangle(0, 0, w, h), Color.White);
            batch_.End();

            device_.SetRenderTarget(null);
        }
    }
}
