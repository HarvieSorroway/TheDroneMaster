using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster
{
    public class PostEffect : MonoBehaviour
    {
        public Shader effectShader;
        public Shader bufferShader;

        protected Material effectMat;
        protected Material bufferMat;


        public RenderTexture accumulationTexture;
        public bool IsSupported { get; private set; }

        public float Strength = 0;
        public float BlendStrength = 1f;
        public float StripStrength = 0.1f;
        public float Split = 0.5f;
        public Vector2 Center = Vector2.one / 2f;
        public Color BlendColor = new Color(1, 0.26f, 0.45f, 1);

        public void Start()
        {
            effectShader = Plugin.postShade;
            bufferShader = Plugin.bufferShader;
            try
            {
                effectMat = new Material(effectShader);
                bufferMat = new Material(bufferShader);

                Debug.Log("[BlastLaserCat]" + bufferMat.ToString());
                IsSupported = true;

            }
            catch (Exception e)
            {
                Plugin.instance.config.UsingHUDEffect.Value = false;
                IsSupported = false;
                Debug.LogException(e);
            }
        }

        void OnDisable()
        {
            DestroyImmediate(accumulationTexture);
        }

        protected virtual bool CheckSupported(out string msg)
        {
            msg = effectShader.isSupported ? "post shader supported" : $"Post shader unsupported {effectShader.name}";

            return effectShader.isSupported;
        }

        protected virtual void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (IsSupported && Plugin.instance.config.UsingHUDEffect.Value)
            {
                if (accumulationTexture == null || accumulationTexture.width != src.width || accumulationTexture.height != src.height)
                {
                    DestroyImmediate(accumulationTexture);
                    accumulationTexture = new RenderTexture(src.width, src.height, 0);
                    accumulationTexture.hideFlags = HideFlags.HideAndDontSave;
                    Graphics.Blit(src, accumulationTexture);
                }

                accumulationTexture.MarkRestoreExpected();

                bufferMat.SetFloat("_Strength", (1f - Strength * 0.75f));

                effectMat.SetFloat("_Strength", Strength);
                effectMat.SetFloat("_Split", Split);
                effectMat.SetFloat("_BlendStrength", BlendStrength);
                effectMat.SetFloat("_StripStrength", StripStrength);
                effectMat.SetVector("_Center", new Vector4(Center.x, Center.y, 1f, 1f));
                effectMat.SetColor("_BlendColor", BlendColor);
                effectMat.SetFloat("_StripeRoll", -Time.time * 2f);

                Graphics.Blit(src, accumulationTexture, bufferMat);
                Graphics.Blit(accumulationTexture, dest, effectMat);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
    }
}
