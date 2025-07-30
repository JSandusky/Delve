using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DelveLib;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Content;

namespace Delve.Graphics
{
    public enum RenderPass
    {
        Base,
        Lit,
        Alpha,
        LitAlpha
    }

    public enum RenderCmdType
    {
        Quad,
        DrawBatches,
        PingPong,
        Clear,
        SetTarget,
        DeferredLighting,
        ForwardLighting,
        ForwardPlusPrepare
    }

    public abstract class RenderCommand : IDisposable
    {
        public RenderPass pass_;
        public readonly RenderCmdType type_;
        public string key_;
        public string tag_;
        public bool isActive_ = false;

        protected RenderCommand(RenderCmdType t) { type_ = t; }

        public abstract void Read(XmlElement elem);
        public virtual void Dispose() { }

        public virtual void OnScreenResolutionChanged(GraphicsDevice device, int newWidth, int newHeight) { }
        public virtual void ResolveResources(ContentManager contentManager, GraphicsDevice device) { }
    }
    
    public class RenderPipeline : IDisposable
    {
        GraphicsDevice graphicsDevice_;
        List<RenderCommand> commands_ = new List<RenderCommand>();
        List<RenderTarget2D> renderTargets_ = new List<RenderTarget2D>();
        List<RenderTargetMeta> renderTargetMeta_ = new List<RenderTargetMeta>();
        Dictionary<string, int> targetIndices_ = new Dictionary<string, int>();
        List<RenderTargetBinding> bindings_ = new List<RenderTargetBinding>();
        LitBatching litBatching_;
        MeshBatch batch_;

        internal class RenderTargetMeta
        {
            internal string name_;
            internal int? explicitWidth_;
            internal int? explicitHeight_;
            internal float? relativeWidth_;
            internal float? relativeHeight_;
            internal int multisample_ = 1;
            internal SurfaceFormat surfaceFormat_;
            internal DepthFormat depthFormat_;
            internal RenderTargetUsage usage_ = RenderTargetUsage.DiscardContents;

            internal RenderTarget2D CreateTarget(GraphicsDevice device, int screenWidth, int screenHeight)
            {
                int width = 128;
                int height = 128;
                if (explicitWidth_.HasValue)
                    width = explicitWidth_.Value;
                else if (relativeWidth_.HasValue)
                    width = (int)(relativeWidth_.Value * screenWidth);
                if (explicitHeight_.HasValue)
                    height = explicitHeight_.Value;
                else if (relativeHeight_.HasValue)
                    height = (int)(relativeHeight_.Value * screenWidth);

                return new RenderTarget2D(device, width, height, false, surfaceFormat_, depthFormat_, multisample_, usage_);
            }
        }

        VertexPositionTexture[] fullscreenTri = new VertexPositionTexture[]
        {
            new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0,0)),
            new VertexPositionTexture(new Vector3(-1, 3, 0), new Vector2(0,2)),
            new VertexPositionTexture(new Vector3(3, -1, 0), new Vector2(2,0))
        };

        /// <summary>
        /// Rebuilds all render-targets when the screen size is changed
        /// </summary>
        public void OnScreenSizeChanged(int newWidth, int newHeight)
        {
            for (int i = 0; i < renderTargets_.Count; ++i)
                renderTargets_[i].Dispose();

            targetIndices_.Clear();
            renderTargets_.Clear();

            for (int i = 0; i < renderTargetMeta_.Count; ++i)
            {
                renderTargets_.Add(renderTargetMeta_[i].CreateTarget(graphicsDevice_, newWidth, newHeight));
                targetIndices_.Add(renderTargetMeta_[i].name_, i);
            }

            for (int i = 0; i < commands_.Count; ++i)
                commands_[i].OnScreenResolutionChanged(graphicsDevice_, newWidth, newHeight);
        }

        public void Render()
        {
            litBatching_.RenderShadowMaps();
            for (int i = 0; i < commands_.Count; ++i)
            {
                if (commands_[i].isActive_ == false)
                    continue;

                switch (commands_[i].type_)
                {
                case RenderCmdType.SetTarget:
                    {
                        var cmd = commands_[i] as SetTargets;
                        bindings_.Clear();
                        for (int idx = 0; idx < cmd.targets_.Count; ++idx)
                        {
                            if (cmd.targets_[i].targetIndex_ == -1)
                                cmd.targets_[i].targetIndex_ = targetIndices_[cmd.targets_[i].targetName_];
                            bindings_.Add(new RenderTargetBinding(renderTargets_[cmd.targets_[i].targetIndex_]));
                            graphicsDevice_.SetRenderTargets(bindings_.ToArray());
                        }
                        if (cmd.targets_.Count == 0)
                            graphicsDevice_.SetRenderTarget(null);
                    } break;
                case RenderCmdType.Clear:
                    {
                        var cmd = commands_[i] as ClearCommand;
                        if (cmd.clearColor_ && cmd.clearDepth_ && cmd.clearStencil_)
                            graphicsDevice_.Clear(ClearOptions.Target | ClearOptions.Stencil | ClearOptions.DepthBuffer, cmd.color_, 0, 0);
                        else if (cmd.clearColor_ && cmd.clearStencil_)
                            graphicsDevice_.Clear(ClearOptions.Target | ClearOptions.Stencil, cmd.color_, 0, 0);
                        else if (cmd.clearColor_ && cmd.clearDepth_)
                            graphicsDevice_.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, cmd.color_, 0, 0);
                        else if (cmd.clearColor_)
                            graphicsDevice_.Clear(ClearOptions.Target, cmd.color_, 0, 0);
                        else if (cmd.clearDepth_)
                            graphicsDevice_.Clear(ClearOptions.DepthBuffer, cmd.color_, 0, 0);
                        else if (cmd.clearStencil_)
                            graphicsDevice_.Clear(ClearOptions.Stencil, cmd.color_, 0, 0);
                    }
                    break;
                case RenderCmdType.Quad:
                    {
                        var cmd = commands_[i] as TriCommand;
                        cmd.effect_.CurrentTechnique.Passes[0].Apply();
                        graphicsDevice_.DrawUserPrimitives(PrimitiveType.TriangleList, fullscreenTri, 0, 1);
                    }
                    break;
                case RenderCmdType.DrawBatches:
                    {
                        var cmd = commands_[i] as DrawCommand;
                        var oldDepthStencil = graphicsDevice_.DepthStencilState;
                        var oldBlendState = graphicsDevice_.BlendState;
                        if (cmd.depthState_ != null)
                            graphicsDevice_.DepthStencilState = cmd.depthState_;
                        if (cmd.blendState_ != null)
                            graphicsDevice_.BlendState = cmd.blendState_;

                        batch_.Render(null, 0);

                        graphicsDevice_.BlendState = oldBlendState;
                        graphicsDevice_.DepthStencilState = oldDepthStencil;
                    }
                    break;
                case RenderCmdType.ForwardLighting:
                    {
                        var cmd = commands_[i] as ForwardLighting;
                        litBatching_.RenderForwardLights();
                    }
                    break;
                case RenderCmdType.DeferredLighting:
                    litBatching_.RenderDeferredLights();
                    break;
                case RenderCmdType.ForwardPlusPrepare:
                    {
                        var cmd = commands_[i] as ForwardPlusPrepare;
                        cmd.UpdateData(graphicsDevice_);
                    }
                    break;
                }
            }
        }

        public void Dispose()
        {
            foreach (var tgt in renderTargets_)
                tgt.Dispose();
            foreach (var cmd in commands_)
                cmd.Dispose();
        }

        public void Read(XmlDocument document)
        {
            Read(document.DocumentElement);
        }

        public void Read(XmlElement element)
        {
            for (int i = 0; i < element.ChildNodes.Count; ++i)
            {
                var child = element.ChildNodes[i] as XmlElement;
                if (child != null)
                {
                    var lCasename = child.Name.ToLowerInvariant();
                    if (lCasename == "target")
                    {
                        SurfaceFormat targetFormat = (SurfaceFormat)Enum.Parse(typeof(SurfaceFormat), child.GetAttribute("format"));
                        string widthText = child.GetAttribute("width");
                        string heightText = child.GetAttribute("height");
                        DepthFormat depthFormat = DepthFormat.None;
                        if (child.HasAttribute("depth-format"))
                            depthFormat = (DepthFormat)Enum.Parse(typeof(DepthFormat), child.GetAttribute("depth-format"));

                        RenderTargetMeta meta = new RenderTargetMeta();
                        meta.name_ = child.GetAttribute("name");
                        if (widthText.EndsWith("%"))
                            meta.relativeWidth_ = float.Parse(widthText.Substring(0, widthText.Length - 1)) / 100.0f;
                        else
                            meta.explicitWidth_ = int.Parse(widthText);
                        if (heightText.EndsWith("%"))
                            meta.relativeHeight_ = float.Parse(heightText.Substring(0, heightText.Length - 1)) / 100.0f;
                        else
                            meta.explicitHeight_= int.Parse(heightText);

                        meta.surfaceFormat_ = targetFormat;
                        meta.depthFormat_ = depthFormat;
                    }
                    else if (lCasename == "clear")
                    {
                        ClearCommand cmd = new ClearCommand();
                        cmd.Read(child);
                        commands_.Add(cmd);
                    }
                    else if (lCasename == "quad" || lCasename == "tri")
                    {
                        TriCommand cmd = new TriCommand();
                        cmd.Read(child);
                        commands_.Add(cmd);
                    }
                    else if (lCasename == "draw")
                    {
                        DrawCommand cmd = new DrawCommand();
                        cmd.Read(child);
                        commands_.Add(cmd);
                    }
                    else if (lCasename == "forward-lighting")
                    {
                        ForwardLighting cmd = new ForwardLighting();
                        cmd.Read(child);
                        commands_.Add(cmd);
                    }
                    else if (lCasename == "deferred-lighting")
                    {
                        DeferredLighting cmd = new DeferredLighting();
                        cmd.Read(child);
                        commands_.Add(cmd);
                    }
                    else if (lCasename == "forward-plus-prepare")
                    {
                        ForwardPlusPrepare cmd = new ForwardPlusPrepare();
                        cmd.Read(child);
                        commands_.Add(cmd);
                    }
                    else if (lCasename == "settarget")
                    {
                        SetTargets cmd = new SetTargets();
                        cmd.Read(child);
                        commands_.Add(cmd);
                    }
                }
            }
        }
    }

    public class ClearCommand : RenderCommand
    {
        public ClearCommand() : base(RenderCmdType.Clear) {  }

        public Color color_ = Color.TransparentBlack;
        public bool clearColor_ = true;
        public bool clearDepth_ = true;
        public bool clearStencil_ = true;
        public string target_;
        public int targetIndex_ = 0;

        public override void Read(XmlElement elem)
        {
            clearColor_ = elem.GetBoolAttribute("color", true);
            clearDepth_ = elem.GetBoolAttribute("depth", true);
            clearStencil_ = elem.GetBoolAttribute("stencil", true);
            target_ = elem.GetAttribute("target");
            color_ = elem.GetColorAttribute("color-value", Color.TransparentBlack);
        }
    }

    public class TriCommand : RenderCommand
    {
        public TriCommand() : base(RenderCmdType.Quad) { }

        public Effect effect_;
        public string effectName_;

        public override void ResolveResources(ContentManager contentManager, GraphicsDevice device)
        {
            effect_ = contentManager.Load<Effect>(effectName_);
            if (effect_.CurrentTechnique == null)
                effect_.CurrentTechnique = effect_.Techniques[0];
            base.ResolveResources(contentManager, device);
        }

        public override void Read(XmlElement elem)
        {
            effectName_ = elem.GetAttribute("effect");
        }
    }

    public class DrawCommand : RenderCommand
    {
        public DrawCommand() : base(RenderCmdType.DrawBatches) { }

        public Effect effect_;
        public string passName_;
        public DepthStencilState depthState_ = DepthStencilState.Default;
        public BlendState blendState_ = BlendState.Opaque;

        public override void ResolveResources(ContentManager contentManager, GraphicsDevice device)
        {
            base.ResolveResources(contentManager, device);
        }

        public override void Read(XmlElement elem)
        {
            passName_ = elem.GetAttribute("pass");
            bool hasStencilTest = elem.HasAttribute("stencil-test");
            bool hasDepth = elem.HasAttribute("depth-test");
            bool hasDepthWrite = elem.HasAttribute("depth-write");
            bool hasStencilWrite = elem.HasAttribute("stencil-write");
            if (hasStencilTest || hasDepth || hasDepthWrite || hasStencilWrite)
            {
                depthState_ = new DepthStencilState();
                if (hasStencilWrite)
                {
                    depthState_.StencilEnable = elem.GetBoolAttribute("stencil-write");
                    if (depthState_.StencilEnable)
                    {
                        depthState_.ReferenceStencil = elem.GetIntAttribute("stencil-value");
                        depthState_.StencilFunction = CompareFunction.Always;
                        depthState_.StencilPass = StencilOperation.Replace;
                    }
                }
                if (hasDepthWrite)
                    depthState_.DepthBufferWriteEnable = elem.GetBoolAttribute("depth-write");
                if (hasDepth)
                    depthState_.DepthBufferFunction = (CompareFunction)Enum.Parse(typeof(CompareFunction), elem.GetAttribute("depth-test"));
                if (hasStencilTest && elem.GetBoolAttribute("stencil-test"))
                {
                    depthState_.StencilEnable = true;
                    depthState_.ReferenceStencil = elem.GetIntAttribute("stencil-value");
                    depthState_.StencilFunction = CompareFunction.Equal;
                    depthState_.StencilPass = StencilOperation.Keep;
                }
            }
            bool hasColorWrite = elem.HasAttribute("color-write");
            bool hasBlendFunc = elem.HasAttribute("blend");
            if (hasColorWrite || hasBlendFunc)
            {
                blendState_ = new BlendState();
                if (!elem.GetBoolAttribute("color-write", true))
                    blendState_.ColorWriteChannels = ColorWriteChannels.None;
                if (hasBlendFunc)
                {
                    string lCaseBlend = elem.GetAttribute("blend").Trim();
                    if (lCaseBlend == "additive")
                    {
                        blendState_.ColorSourceBlend = Blend.SourceAlpha;
                        blendState_.AlphaSourceBlend = Blend.SourceAlpha;
                        blendState_.ColorDestinationBlend = Blend.One;
                        blendState_.AlphaDestinationBlend = Blend.One;
                    }
                    else if (lCaseBlend == "premultiplied")
                    {
                        blendState_.ColorSourceBlend = Blend.One;
                        blendState_.AlphaSourceBlend = Blend.One;
                        blendState_.ColorDestinationBlend = Blend.InverseSourceAlpha;
                        blendState_.AlphaDestinationBlend = Blend.InverseSourceAlpha;
                    }
                    else if (lCaseBlend == "alpha")
                    {
                        blendState_.ColorSourceBlend = Blend.SourceAlpha;
                        blendState_.AlphaSourceBlend = Blend.SourceAlpha;
                        blendState_.ColorDestinationBlend = Blend.InverseSourceAlpha;
                        blendState_.AlphaDestinationBlend = Blend.InverseSourceAlpha;
                    }
                    else if (lCaseBlend == "multiply")
                    {
                        blendState_.ColorSourceBlend = Blend.DestinationColor;
                        blendState_.AlphaSourceBlend = Blend.DestinationColor;
                        blendState_.ColorDestinationBlend = Blend.Zero;
                        blendState_.AlphaDestinationBlend = Blend.Zero;
                    }
                    else if (lCaseBlend == "subtract")
                    {
                        blendState_.ColorSourceBlend = Blend.One;
                        blendState_.AlphaSourceBlend = Blend.One;
                        blendState_.ColorDestinationBlend = Blend.One;
                        blendState_.AlphaDestinationBlend = Blend.One;
                        blendState_.ColorBlendFunction = BlendFunction.ReverseSubtract;
                        blendState_.AlphaBlendFunction = BlendFunction.ReverseSubtract;
                    }
                    else if (lCaseBlend == "subtract-alpha")
                    {

                        blendState_.ColorSourceBlend = Blend.SourceAlpha;
                        blendState_.AlphaSourceBlend = Blend.SourceAlpha;
                        blendState_.ColorDestinationBlend = Blend.One;
                        blendState_.AlphaDestinationBlend = Blend.One;
                        blendState_.ColorBlendFunction = BlendFunction.ReverseSubtract;
                        blendState_.AlphaBlendFunction = BlendFunction.ReverseSubtract;
                    }
                    else // opaque
                    {
                        blendState_.ColorSourceBlend = Blend.One;
                        blendState_.AlphaSourceBlend = Blend.One;
                        blendState_.ColorDestinationBlend = Blend.Zero;
                        blendState_.AlphaDestinationBlend = Blend.Zero;
                    }
                }
            }
        }
    }

    public class DeferredLighting : RenderCommand
    {
        public DeferredLighting() : base(RenderCmdType.DeferredLighting) { }

        public Effect effect_;
        public string effectName_;
        EffectTechnique pointlightTech_;
        EffectTechnique spotlightTech_;
        EffectTechnique directionalLightTech_;

        public override void ResolveResources(ContentManager contentManager, GraphicsDevice device)
        {
            effect_ = contentManager.Load<Effect>(effectName_);
            pointlightTech_ = effect_.Techniques.FirstOrDefault(fx => fx.Name.ToLowerInvariant().Contains("point"));
            spotlightTech_ = effect_.Techniques.FirstOrDefault(fx => fx.Name.ToLowerInvariant().Contains("spot"));
            directionalLightTech_ = effect_.Techniques.FirstOrDefault(fx => fx.Name.ToLowerInvariant().Contains("direction"));
            base.ResolveResources(contentManager, device);
        }

        public override void Read(XmlElement elem)
        {
            effectName_ = elem.GetAttribute("effect");
        }
    }

    public class ForwardLighting : RenderCommand
    {
        public ForwardLighting() : base(RenderCmdType.ForwardLighting) { }

        public Effect effect_;
        public string effectName_;

        public override void Read(XmlElement elem)
        {
            effectName_ = elem.GetAttribute("pass");
        }
    }

    public class ForwardPlusPrepare : RenderCommand
    {
        int widthPerCell_;
        int heightPerCell_;
        int tilesX_ = 0;
        int tilesY_ = 0;
        int lightListIndex_;
        int lightDataIndex_;
        int lightsPerCell_;

        // instance data
        StructuredBuffer lightList_;
        StructuredBuffer lightData_;
        int cellsX_;
        int cellsY_;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct LightCellRecord
        {
            public int Offset;
            public int LightCount;

            public static int Size { get { return Marshal.SizeOf<LightCellRecord>(); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct LightEntry
        {
            public Vector4 LightColor;
            public Vector4 LightPosRadius;
            public int RampIndex;
            public int ShadowIndex;

            public static int Size { get { return Marshal.SizeOf<LightEntry>(); } }
        }

        public ForwardPlusPrepare() : base(RenderCmdType.ForwardPlusPrepare)
        {

        }

        public void UpdateData(GraphicsDevice device)
        {

        }

        public override void Read(XmlElement elem)
        {
            widthPerCell_ = elem.GetIntAttribute("cell-width");
            heightPerCell_ = elem.GetIntAttribute("cell-height");
            tilesX_ = elem.GetIntAttribute("tiles-x");
            tilesY_ = elem.GetIntAttribute("tiles-y");
            lightsPerCell_ = elem.GetIntAttribute("lights");
            lightListIndex_ = elem.GetIntAttribute("list-bind");
            lightDataIndex_ = elem.GetIntAttribute("data-bind");
        }

        public override void OnScreenResolutionChanged(GraphicsDevice device, int newWidth, int newHeight)
        {
            Dispose();
            cellsX_ = tilesX_ != 0 ? tilesX_ : Math.Max(1, (int)(Math.Ceiling(newWidth / (float)widthPerCell_)));
            cellsY_ = tilesY_ != 0 ? tilesY_ : Math.Max(1, (int)(Math.Ceiling(newHeight / (float)heightPerCell_)));
            lightList_ = new StructuredBuffer(device, cellsX_ * cellsY_ * LightCellRecord.Size, LightCellRecord.Size);
            lightData_ = new StructuredBuffer(device, cellsX_ * cellsY_ * lightsPerCell_ * LightEntry.Size, LightEntry.Size);
        }

        public override void Dispose()
        {
            if (lightList_ != null)
                lightList_.Dispose();
            if (lightData_ != null)
                lightData_.Dispose();
        }
    }

    public class SetTargets : RenderCommand
    {
        public SetTargets() : base(RenderCmdType.SetTarget) { }

        public class Item
        {
            public int targetIndex_ = -1;
            public string targetName_;
        }
        public List<Item> targets_ = new List<Item>();

        public override void Read(XmlElement elem)
        {
            for (int i = 0; i < elem.ChildNodes.Count;  ++i)
            {
                var child = elem.ChildNodes[i] as XmlElement;
                if (child != null)
                {
                    Item item = new Item();
                    item.targetName_ = child.Value;
                    targets_.Add(item);
                }
            }
        }
    }
}
