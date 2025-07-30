using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Delve.Graphics
{
    public class PBREffect : Effect, ICommonEffect, IMeshBatchEffect
    {
        static Texture2D iblLUT_;

        public PBREffect(GraphicsDevice device, ContentManager content) :
            base(content.Load<Effect>("Effects/PBREffect"))
        {
            // load default matcap
            CurrentTechnique = Techniques[0];
            if (iblLUT_ == null)
                iblLUT_ = content.Load<Texture2D>("Textures/brdfLUT");
        }

        public Matrix WorldViewProjection { get; set; }
        public Matrix WorldView { get; set; }
        public Matrix Transform { get; set; } = Matrix.Identity;
        public Vector3 CameraPosition { get; set; }
        public Vector3 LightDirection { get; set; } = new Vector3(0, -1, 0);
        public float Ambient { get; set; } = 0.2f;
        public bool FlipCulling { get; set; } = false;

        public TextureCube IBLMap { get; set; }
        public Texture2D DiffuseTexture { get; set; }
        public Texture2D NormalMapTexture { get; set; }
        public Texture2D AOTexture { get; set; }
        public Texture2D HeightTexture { get; set; }
        public Texture2D EmissiveMaskTexture { get; set; }
        public Texture2D SubsurfaceColorTexture { get; set; }
        public Texture2D SubsurfaceDepthTexture { get; set; }
        public Texture2D ClearcoatTexture { get; set; }

        // there's fuding happening with these 4, only 2 real shader textures are used (fitting us in at 
        public Texture2D RoughnessTexture { get; set; }
        public Texture2D GlossinessTexture { get; set; }
        public Texture2D MetalnessTexture { get; set; }
        public Texture2D SpecularTexture { get; set; }
        public Vector2 UVTiling { get; set; } = new Vector2(1, 1);

        public void Begin(GraphicsDevice device)
        {

        }

        public void End(GraphicsDevice device)
        {
        }

        protected override void OnApply()
        {
            Parameters["UVTiling"].SetValue(UVTiling);
            Parameters["WorldViewProjection"].SetValue(WorldViewProjection);
            //Parameters["InverseWorldView"].SetValue(Matrix.Invert(WorldView));
            Parameters["Transform"].SetValue(Transform);
            Parameters["CameraPosition"].SetValue(CameraPosition);
            Parameters["LightDir"].SetValue(LightDirection);
            Parameters["AmbientBrightness"].SetValue(Ambient);
            Parameters["FlipCulling"].SetValue(FlipCulling ? -1 : 1);

            if (Parameters["DiffuseTex"] != null)
                Parameters["DiffuseTex"].SetValue(DiffuseTexture);
            if (Parameters["NormalMapTex"] != null)
                Parameters["NormalMapTex"].SetValue(NormalMapTexture);

            if (Parameters["IBLLUTTex"] != null)
                Parameters["IBLLUTTex"].SetValue(iblLUT_);

            if (Parameters["RoughnessTex"] != null)
                Parameters["RoughnessTex"].SetValue(RoughnessTexture);
            if (Parameters["MetalnessTex"] != null)
                Parameters["MetalnessTex"].SetValue(MetalnessTexture);
            if (Parameters["IBLTex"] != null)
                Parameters["IBLTex"].SetValue(IBLMap);
            if (Parameters["HeightMapTex"] != null)
                Parameters["HeightMapTex"].SetValue(HeightTexture);
            if (Parameters["AOTex"] != null)
                Parameters["AOTex"].SetValue(AOTexture);
            if (Parameters["EmissiveMaskTex"] != null)
                Parameters["EmissiveMaskTex"].SetValue(EmissiveMaskTexture);
            if (Parameters["SubsurfaceColorTex"] != null)
                Parameters["SubsurfaceColorTex"].SetValue(SubsurfaceColorTexture);
            if (Parameters["SubsurfaceDepthTex"] != null)
                Parameters["SubsurfaceDepthTex"].SetValue(SubsurfaceDepthTexture);
        }

        public bool PrepareInstanced(int passID)
        {
            CurrentTechnique = Techniques.Last();
            CurrentTechnique.Passes[0].Apply();
            return true;
        }

        public bool PrepareOneOff(Matrix transform, int passID)
        {
            Parameters["Transform"].SetValue(transform);
            CurrentTechnique = Techniques.First();
            CurrentTechnique.Passes[0].Apply();
            return true;
        }

        public bool EffectSelected(int passID)
        {
            return true;
        }

        public void EffectDeselected()
        {
            
        }
    }
}
