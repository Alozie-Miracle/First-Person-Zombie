using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace WaterSystem
{
    public class WaterSystemFeature : ScriptableRendererFeature
    {

        #region Water Effects Pass

        class WaterFxPass : ScriptableRenderPass
        {
            private const string k_RenderWaterFXTag = "Render Water FX";
            private ProfilingSampler m_WaterFX_Profile = new ProfilingSampler(k_RenderWaterFXTag);
            private readonly ShaderTagId m_WaterFXShaderTag = new ShaderTagId("WaterFX");
            private readonly Color m_ClearColor = new Color(0.0f, 0.5f, 0.5f, 0.5f); //r = foam mask, g = normal.x, b = normal.z, a = displacement
            private FilteringSettings m_FilteringSettings;
            public WaterFxPass()
            {
                // only wanting to render transparent objects
                m_FilteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            }

            private class PassData
            {
                public RendererListHandle rendererList;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
                UniversalLightData lightData = frameContext.Get<UniversalLightData>();
                UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();

                TextureDesc waterFxDesc = resourceData.cameraDepthTexture.GetDescriptor(renderGraph);
                waterFxDesc.name = "_WaterFXMap";
                waterFxDesc.width /= 2;
                waterFxDesc.height /= 2;
                waterFxDesc.depthBufferBits = DepthBits.None;
                waterFxDesc.format = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
                waterFxDesc.clearBuffer = true;
                waterFxDesc.clearColor = m_ClearColor;
                TextureHandle waterFxTexture = renderGraph.CreateTexture(waterFxDesc);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_RenderWaterFXTag, out var passData))
                {
                    DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(
                        m_WaterFXShaderTag, renderingData, cameraData, lightData,
                        SortingCriteria.CommonTransparent);
                    RendererListParams rendererListParams = new RendererListParams(
                        renderingData.cullResults, drawSettings, m_FilteringSettings);
                    passData.rendererList = renderGraph.CreateRendererList(rendererListParams);

                    builder.UseRendererList(passData.rendererList);
                    builder.SetRenderAttachment(waterFxTexture, 0);
                    builder.SetGlobalTextureAfterPass(waterFxTexture, Shader.PropertyToID("_WaterFXMap"));
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                        context.cmd.DrawRendererList(data.rendererList));
                }
            }
        }

        #endregion

        #region Caustics Pass

        class WaterCausticsPass : ScriptableRenderPass
        {
            private const string k_RenderWaterCausticsTag = "Render Water Caustics";
            private ProfilingSampler m_WaterCaustics_Profile = new ProfilingSampler(k_RenderWaterCausticsTag);
            public Material WaterCausticMaterial;
            private static Mesh m_mesh;

            private class PassData
            {
                public Mesh mesh;
                public Material material;
                public Matrix4x4 matrix;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
                UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
                var cam = cameraData.camera;
                // Stop the pass rendering in the preview or material missing
                if (cam.cameraType == CameraType.Preview || !WaterCausticMaterial)
                    return;

                if (!m_mesh)
                    m_mesh = GenerateCausticsMesh(1000f);

                var sunMatrix = RenderSettings.sun != null
                    ? RenderSettings.sun.transform.localToWorldMatrix
                    : Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-45f, 45f, 0f), Vector3.one);
                WaterCausticMaterial.SetMatrix("_MainLightDir", sunMatrix);

                var position = cam.transform.position;
                position.y = 0;
                Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    k_RenderWaterCausticsTag, out var passData))
                {
                    passData.mesh = m_mesh;
                    passData.material = WaterCausticMaterial;
                    passData.matrix = matrix;
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                        context.cmd.DrawMesh(data.mesh, data.matrix, data.material, 0, 0));
                }
            }

            public static void DisposeMesh()
            {
                CoreUtils.Destroy(m_mesh);
                m_mesh = null;
            }
        }

        #endregion

        WaterFxPass m_WaterFxPass;
        WaterCausticsPass m_CausticsPass;

        public WaterSystemSettings settings = new WaterSystemSettings();
        [HideInInspector][SerializeField] private Shader causticShader;
        [HideInInspector][SerializeField] private Texture2D causticTexture;

        private Material _causticMaterial;

        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int Size = Shader.PropertyToID("_Size");
        private static readonly int CausticTexture = Shader.PropertyToID("_CausticMap");

        public override void Create()
        {
            // WaterFX Pass
            m_WaterFxPass = new WaterFxPass { renderPassEvent = RenderPassEvent.BeforeRenderingOpaques };

            // Caustic Pass
            m_CausticsPass = new WaterCausticsPass();

            causticShader = causticShader ? causticShader : Shader.Find("Hidden/BoatAttack/Caustics");
            if (causticShader == null) return;
            if (_causticMaterial)
            {
                DestroyImmediate(_causticMaterial);
            }
            _causticMaterial = CoreUtils.CreateEngineMaterial(causticShader);
            _causticMaterial.SetFloat("_BlendDistance", settings.causticBlendDistance);

            if (causticTexture == null)
            {
                Debug.Log("Caustics Texture missing, attempting to load.");
#if UNITY_EDITOR
                causticTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.verasl.water-system/Textures/WaterSurface_single.tif");
#endif
            }
            _causticMaterial.SetTexture(CausticTexture, causticTexture);

            switch (settings.debug)
            {
                case WaterSystemSettings.DebugMode.Caustics:
                    _causticMaterial.SetFloat(SrcBlend, 1f);
                    _causticMaterial.SetFloat(DstBlend, 0f);
                    _causticMaterial.EnableKeyword("_DEBUG");
                    m_CausticsPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                    break;
                case WaterSystemSettings.DebugMode.WaterEffects:
                    break;
                case WaterSystemSettings.DebugMode.Disabled:
                    // Caustics
                    _causticMaterial.SetFloat(SrcBlend, 2f);
                    _causticMaterial.SetFloat(DstBlend, 0f);
                    _causticMaterial.DisableKeyword("_DEBUG");
                    m_CausticsPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 1;
                    // WaterEffects
                    break;
            }

            _causticMaterial.SetFloat(Size, settings.causticScale);
            m_CausticsPass.WaterCausticMaterial = _causticMaterial;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_WaterFxPass);
            renderer.EnqueuePass(m_CausticsPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_causticMaterial);
            _causticMaterial = null;

            WaterCausticsPass.DisposeMesh();
        }

        /// <summary>
        /// This function Generates a flat quad for use with the caustics pass.
        /// </summary>
        /// <param name="size">The length of the quad.</param>
        /// <returns></returns>
        private static Mesh GenerateCausticsMesh(float size)
        {
            var m = new Mesh();
            size *= 0.5f;

            var verts = new[]
            {
                new Vector3(-size, 0f, -size),
                new Vector3(size, 0f, -size),
                new Vector3(-size, 0f, size),
                new Vector3(size, 0f, size)
            };
            m.vertices = verts;

            var tris = new[]
            {
                0, 2, 1,
                2, 3, 1
            };
            m.triangles = tris;

            return m;
        }

        [System.Serializable]
        public class WaterSystemSettings
        {
            [Header("Caustics Settings")]
            [Range(0.1f, 1f)]
            public float causticScale = 0.25f;

            public float causticBlendDistance = 3f;

            [Header("Advanced Settings")] public DebugMode debug = DebugMode.Disabled;

            public enum DebugMode
            {
                Disabled,
                WaterEffects,
                Caustics
            }
        }
    }
}