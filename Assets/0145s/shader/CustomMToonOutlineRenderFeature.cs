
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class CustomMToonOutlineRenderFeature
    : ScriptableRendererFeature
{
    private CustomMToonOutlinePass opaquePass;
    private CustomMToonOutlinePass transparentPass;

    public override void Create()
    {
        opaquePass = new CustomMToonOutlinePass(
            RenderPassEvent.AfterRenderingOpaques,
            RenderQueueRange.opaque,
            SortingCriteria.CommonOpaque
        );

        transparentPass = new CustomMToonOutlinePass(
            RenderPassEvent.BeforeRenderingTransparents,
            RenderQueueRange.transparent,
            SortingCriteria.CommonTransparent
        );
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isPreviewCamera)
            return;

        renderer.EnqueuePass(opaquePass);
        renderer.EnqueuePass(transparentPass);
    }

    private sealed class CustomMToonOutlinePass
        : ScriptableRenderPass
    {
        private static readonly ShaderTagId OutlineTag =
            new ShaderTagId("CustomMToonOutline");

        private readonly RenderQueueRange queueRange;
        private readonly SortingCriteria sortingCriteria;

        public CustomMToonOutlinePass(
            RenderPassEvent passEvent,
            RenderQueueRange range,
            SortingCriteria sorting)
        {
            renderPassEvent = passEvent;
            queueRange = range;
            sortingCriteria = sorting;
        }

        public override void Execute(
            ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;

            var sortingSettings = new SortingSettings(camera)
            {
                criteria = sortingCriteria
            };

            var drawingSettings = new DrawingSettings(
                OutlineTag,
                sortingSettings
            )
            {
                perObjectData = PerObjectData.Lightmaps |
                                PerObjectData.LightProbe |
                                PerObjectData.ReflectionProbes |
                                PerObjectData.OcclusionProbe |
                                PerObjectData.ShadowMask
            };

            var filteringSettings =
                new FilteringSettings(queueRange);

            var renderStateBlock =
                new RenderStateBlock(RenderStateMask.Nothing);

            context.DrawRenderers(
                renderingData.cullResults,
                ref drawingSettings,
                ref filteringSettings,
                ref renderStateBlock
            );
        }
    }
}
