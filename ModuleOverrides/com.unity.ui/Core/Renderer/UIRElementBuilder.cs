// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements.UIR
{
    abstract class BaseElementBuilder
    {
        public abstract bool RequiresStencilMask(VisualElement ve);

        public void Build(MeshGenerationContext mgc)
        {
            var ve = mgc.visualElement;

            bool isGroupTransform = (ve.renderHints & RenderHints.GroupTransform) == RenderHints.GroupTransform;
            if (isGroupTransform)
                mgc.entryRecorder.PushGroupMatrix();

            bool usesSubRenderTargetMode = ve.subRenderTargetMode != VisualElement.RenderTargetMode.None;
            if (usesSubRenderTargetMode)
                mgc.entryRecorder.PushRenderTexture();

            bool changesDefaultMaterial = ve.defaultMaterial != null;
            if (changesDefaultMaterial)
                mgc.entryRecorder.PushDefaultMaterial(ve.defaultMaterial);

            bool mustPopClipping = false;

            if (ve.visible)
            {
                DrawVisualElementBackground(mgc);
                DrawVisualElementBorder(mgc);
                PushVisualElementClipping(mgc);
                mustPopClipping = true;
                InvokeGenerateVisualContent(mgc);
            }
            else
            {
                var isClippingWithStencil = ve.renderChainData.clipMethod == ClipMethod.Stencil;
                var isClippingWithScissors = ve.renderChainData.clipMethod == ClipMethod.Scissor;

                // Even though the element hidden, we still have to push the stencil shape or setup the scissors in case any children are visible.
                if (isClippingWithScissors || isClippingWithStencil)
                {
                    mustPopClipping = true;
                    PushVisualElementClipping(mgc);
                }
            }

            mgc.entryRecorder.DrawChildren();

            if (mustPopClipping)
                PopVisualElementClipping(mgc);

            if (changesDefaultMaterial)
                mgc.entryRecorder.PopDefaultMaterial();

            if (isGroupTransform)
                mgc.entryRecorder.PopGroupMatrix();

            if (usesSubRenderTargetMode)
                mgc.entryRecorder.BlitAndPopRenderTexture();
        }

        protected abstract void DrawVisualElementBackground(MeshGenerationContext mgc);

        protected abstract void DrawVisualElementBorder(MeshGenerationContext mgc);

        protected abstract void DrawVisualElementStencilMask(MeshGenerationContext mgc);

        void PushVisualElementClipping(MeshGenerationContext mgc)
        {
            var ve = mgc.visualElement;

            if (ve.renderChainData.clipMethod == ClipMethod.Scissor)
            {
                mgc.entryRecorder.PushScissors();
            }
            else if (ve.renderChainData.clipMethod == ClipMethod.Stencil)
            {
                mgc.entryRecorder.BeginStencilMask();
                DrawVisualElementStencilMask(mgc);
                mgc.entryRecorder.EndStencilMask();
            }
            mgc.entryRecorder.PushClippingRect();
        }

        static void PopVisualElementClipping(MeshGenerationContext mgc)
        {
            var ve = mgc.visualElement;

            mgc.entryRecorder.PopClippingRect();

            if (ve.renderChainData.clipMethod == ClipMethod.Scissor)
                mgc.entryRecorder.PopScissors();
            else if (ve.renderChainData.clipMethod == ClipMethod.Stencil)
                mgc.entryRecorder.PopStencilMask();
        }

        static void InvokeGenerateVisualContent(MeshGenerationContext mgc)
        {
            var ve = mgc.visualElement;

            Painter2D.isPainterActive = true;
            ve.InvokeGenerateVisualContent(mgc);
            Painter2D.isPainterActive = false;
        }
    }

    class DefaultElementBuilder : BaseElementBuilder
    {
        RenderChain m_RenderChain;

        public DefaultElementBuilder(RenderChain renderChain)
        {
            m_RenderChain = renderChain;
        }

        public override bool RequiresStencilMask(VisualElement ve)
        {
            return UIRUtility.IsRoundRect(ve) || UIRUtility.IsVectorImageBackground(ve);
        }

        protected override void DrawVisualElementBackground(MeshGenerationContext mgc)
        {
            var ve = mgc.visualElement;

            if (ve.layout.width <= UIRUtility.k_Epsilon || ve.layout.height <= UIRUtility.k_Epsilon)
                return;

            var style = ve.computedStyle;
            if (style.backgroundColor != Color.clear)
            {
                // Draw solid color background
                var rectParams = new MeshGenerator.RectangleParams
                {
                    rect = ve.rect,
                    color = style.backgroundColor,
                    colorPage = ColorPage.Init(m_RenderChain, ve.renderChainData.backgroundColorID),
                    playmodeTintColor = ve.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
                };
                MeshGenerator.GetVisualElementRadii(ve,
                    out rectParams.topLeftRadius,
                    out rectParams.bottomLeftRadius,
                    out rectParams.topRightRadius,
                    out rectParams.bottomRightRadius);

                MeshGenerator.AdjustBackgroundSizeForBorders(ve, ref rectParams.rect);

                mgc.meshGenerator.DrawRectangle(rectParams);
            }

            var slices = new Vector4(
                style.unitySliceLeft,
                style.unitySliceTop,
                style.unitySliceRight,
                style.unitySliceBottom);

            var radiusParams = new MeshGenerator.RectangleParams();
            MeshGenerator.GetVisualElementRadii(ve,
                out radiusParams.topLeftRadius,
                out radiusParams.bottomLeftRadius,
                out radiusParams.topRightRadius,
                out radiusParams.bottomRightRadius);

            var background = style.backgroundImage;
            if (background.texture != null || background.sprite != null || background.vectorImage != null || background.renderTexture != null)
            {
                // Draw background image (be it from a texture or a vector image)
                var rectParams = new MeshGenerator.RectangleParams();
                float sliceScale = ve.resolvedStyle.unitySliceScale;

                if (background.texture != null)
                {
                    rectParams = MeshGenerator.RectangleParams.MakeTextured(
                        ve.rect,
                        new Rect(0, 0, 1, 1),
                        background.texture,
                        ScaleMode.ScaleToFit,
                        ve.panel.contextType);

                    rectParams.rect = new Rect(0, 0, rectParams.texture.width, rectParams.texture.height);
                }
                else if (background.sprite != null)
                {
                    rectParams = MeshGenerator.RectangleParams.MakeSprite(
                        ve.rect,
                        background.sprite,
                        ScaleMode.StretchToFill,
                        ve.panel.contextType,
                        radiusParams.HasRadius(MeshBuilderNative.kEpsilon),
                        ref slices,
                        true);

                    rectParams.rect = new Rect(0, 0, background.sprite.rect.width, background.sprite.rect.height);

                    sliceScale *= UIElementsUtility.PixelsPerUnitScaleForElement(ve, background.sprite);
                }
                else if (background.renderTexture != null)
                {
                    rectParams = MeshGenerator.RectangleParams.MakeTextured(
                        ve.rect,
                        new Rect(0, 0, 1, 1),
                        background.renderTexture,
                        ScaleMode.ScaleToFit,
                        ve.panel.contextType);

                    rectParams.rect = new Rect(0, 0, rectParams.texture.width, rectParams.texture.height);

                }
                else if (background.vectorImage != null)
                {
                    rectParams = MeshGenerator.RectangleParams.MakeVectorTextured(
                        ve.rect,
                        new Rect(0, 0, 1, 1),
                        background.vectorImage,
                        ScaleMode.ScaleToFit,
                        ve.panel.contextType);

                    rectParams.rect = new Rect(0, 0, rectParams.vectorImage.size.x, rectParams.vectorImage.size.y);
                }

                rectParams.topLeftRadius = radiusParams.topLeftRadius;
                rectParams.topRightRadius = radiusParams.topRightRadius;
                rectParams.bottomRightRadius = radiusParams.bottomRightRadius;
                rectParams.bottomLeftRadius = radiusParams.bottomLeftRadius;

                if (slices != Vector4.zero)
                {
                    rectParams.leftSlice = Mathf.RoundToInt(slices.x);
                    rectParams.topSlice = Mathf.RoundToInt(slices.y);
                    rectParams.rightSlice = Mathf.RoundToInt(slices.z);
                    rectParams.bottomSlice = Mathf.RoundToInt(slices.w);

                    rectParams.sliceScale = sliceScale;
                }

                rectParams.color = style.unityBackgroundImageTintColor;
                rectParams.colorPage = ColorPage.Init(m_RenderChain, ve.renderChainData.tintColorID);
                rectParams.backgroundPositionX = style.backgroundPositionX;
                rectParams.backgroundPositionY = style.backgroundPositionY;
                rectParams.backgroundRepeat = style.backgroundRepeat;
                rectParams.backgroundSize = style.backgroundSize;

                MeshGenerator.AdjustBackgroundSizeForBorders(ve, ref rectParams.rect);

                if (rectParams.texture != null)
                {
                    mgc.meshGenerator.DrawRectangleRepeat(rectParams, ve.rect);
                }
                else if (rectParams.vectorImage != null)
                {
                    mgc.meshGenerator.DrawRectangleRepeat(rectParams, ve.rect);
                }
                else
                {
                    mgc.meshGenerator.DrawRectangle(rectParams);
                }
            }
        }

        protected override void DrawVisualElementBorder(MeshGenerationContext mgc)
        {
            var ve = mgc.visualElement;

            if (ve.layout.width >= UIRUtility.k_Epsilon && ve.layout.height >= UIRUtility.k_Epsilon)
            {
                var style = ve.resolvedStyle;
                if (style.borderLeftColor != Color.clear && style.borderLeftWidth > 0.0f ||
                    style.borderTopColor != Color.clear && style.borderTopWidth > 0.0f ||
                    style.borderRightColor != Color.clear && style.borderRightWidth > 0.0f ||
                    style.borderBottomColor != Color.clear && style.borderBottomWidth > 0.0f)
                {
                    var borderParams = new MeshGenerator.BorderParams
                    {
                        rect = ve.rect,
                        leftColor = style.borderLeftColor,
                        topColor = style.borderTopColor,
                        rightColor = style.borderRightColor,
                        bottomColor = style.borderBottomColor,
                        leftWidth = style.borderLeftWidth,
                        topWidth = style.borderTopWidth,
                        rightWidth = style.borderRightWidth,
                        bottomWidth = style.borderBottomWidth,
                        leftColorPage = ColorPage.Init(m_RenderChain, ve.renderChainData.borderLeftColorID),
                        topColorPage = ColorPage.Init(m_RenderChain, ve.renderChainData.borderTopColorID),
                        rightColorPage = ColorPage.Init(m_RenderChain, ve.renderChainData.borderRightColorID),
                        bottomColorPage = ColorPage.Init(m_RenderChain, ve.renderChainData.borderBottomColorID),
                        playmodeTintColor = ve.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
                    };
                    MeshGenerator.GetVisualElementRadii(ve,
                        out borderParams.topLeftRadius,
                        out borderParams.bottomLeftRadius,
                        out borderParams.topRightRadius,
                        out borderParams.bottomRightRadius);
                    mgc.meshGenerator.DrawBorder(borderParams);
                }
            }
        }

        protected override void DrawVisualElementStencilMask(MeshGenerationContext mgc)
        {
            if (UIRUtility.IsVectorImageBackground(mgc.visualElement))
                // In the future, we should initially draw it into a detached context and just re-draw it here without
                // tessellating once more as we do here. However this is better than the previous approach which would
                // intercept entries (and would fail whenever more than 1 is used: e.g. with background-repeat
                DrawVisualElementBackground(mgc);
            else
                GenerateStencilClipEntryForRoundedRectBackground(mgc);
        }

        static void GenerateStencilClipEntryForRoundedRectBackground(MeshGenerationContext mgc)
        {
            var ve = mgc.visualElement;

            if (ve.layout.width <= UIRUtility.k_Epsilon || ve.layout.height <= UIRUtility.k_Epsilon)
                return;

            var resolvedStyle = ve.resolvedStyle;
            Vector2 radTL, radTR, radBL, radBR;
            MeshGenerator.GetVisualElementRadii(ve, out radTL, out radBL, out radTR, out radBR);
            float widthT = resolvedStyle.borderTopWidth;
            float widthL = resolvedStyle.borderLeftWidth;
            float widthB = resolvedStyle.borderBottomWidth;
            float widthR = resolvedStyle.borderRightWidth;

            var rp = new MeshGenerator.RectangleParams()
            {
                rect = ve.rect,
                color = Color.white,

                // Adjust the radius of the inner masking shape
                topLeftRadius = Vector2.Max(Vector2.zero, radTL - new Vector2(widthL, widthT)),
                topRightRadius = Vector2.Max(Vector2.zero, radTR - new Vector2(widthR, widthT)),
                bottomLeftRadius = Vector2.Max(Vector2.zero, radBL - new Vector2(widthL, widthB)),
                bottomRightRadius = Vector2.Max(Vector2.zero, radBR - new Vector2(widthR, widthB)),
                playmodeTintColor = ve.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
            };

            // Only clip the interior shape, skipping the border
            rp.rect.x += widthL;
            rp.rect.y += widthT;
            rp.rect.width -= widthL + widthR;
            rp.rect.height -= widthT + widthB;

            // Skip padding, when requested
            if (ve.computedStyle.unityOverflowClipBox == OverflowClipBox.ContentBox)
            {
                rp.rect.x += resolvedStyle.paddingLeft;
                rp.rect.y += resolvedStyle.paddingTop;
                rp.rect.width -= resolvedStyle.paddingLeft + resolvedStyle.paddingRight;
                rp.rect.height -= resolvedStyle.paddingTop + resolvedStyle.paddingBottom;
            }

            mgc.meshGenerator.DrawRectangle(rp);
        }
    }
}
