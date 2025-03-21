// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Camera/GraphicsSettings.h")]
    [StaticAccessor("GetGraphicsSettings()", StaticAccessorType.Dot)]
    public sealed partial class GraphicsSettings : Object
    {
        private GraphicsSettings() {}

        extern public static TransparencySortMode   transparencySortMode { get; set; }
        extern public static Vector3                transparencySortAxis { get; set; }
        extern public static bool realtimeDirectRectangularAreaLights { get; set; }
        extern public static bool lightsUseLinearIntensity   { get; set; }
        extern public static bool lightsUseColorTemperature  { get; set; }
        extern public static uint defaultRenderingLayerMask { get; set; }
        extern public static Camera.GateFitMode defaultGateFitMode { get; set; }
        extern public static bool useScriptableRenderPipelineBatching { get; set; }
        extern public static bool logWhenShaderIsCompiled { get; set; }
        extern public static bool disableBuiltinCustomRenderTextureUpdate { get; set; }
        extern public static VideoShadersIncludeMode videoShadersIncludeMode
        {
            get;
            set;
        }
        extern public static LightProbeOutsideHullStrategy lightProbeOutsideHullStrategy { get; set; }

        extern public static bool HasShaderDefine(GraphicsTier tier, BuiltinShaderDefine defineHash);
        public static bool HasShaderDefine(BuiltinShaderDefine defineHash)
        {
            return HasShaderDefine(Graphics.activeTier, defineHash);
        }

        [NativeName("CurrentRenderPipeline")] extern private static ScriptableObject INTERNAL_currentRenderPipeline { get; }
        public static RenderPipelineAsset currentRenderPipeline
        {
            get { return INTERNAL_currentRenderPipeline as RenderPipelineAsset; }
        }

        public static bool isScriptableRenderPipelineEnabled => INTERNAL_currentRenderPipeline != null;

        public static Type currentRenderPipelineAssetType => isScriptableRenderPipelineEnabled ? INTERNAL_currentRenderPipeline.GetType() : null;

        //[Obsolete("renderPipelineAsset has been deprecated. Use defaultRenderPipeline instead (UnityUpgradable) -> defaultRenderPipeline", true)]
        // TODO: SRP package needs updating (not break ABV) once that is done we can remove this
        public static RenderPipelineAsset renderPipelineAsset
        {
            get { return defaultRenderPipeline; }
            set { defaultRenderPipeline = value; }
        }

        [NativeName("DefaultRenderPipeline")] extern private static ScriptableObject INTERNAL_defaultRenderPipeline { get; set; }
        public static RenderPipelineAsset defaultRenderPipeline
        {
            get { return INTERNAL_defaultRenderPipeline as RenderPipelineAsset; }
            set { INTERNAL_defaultRenderPipeline = value; }
        }

        [NativeName("GetAllConfiguredRenderPipelinesForScript")] extern static private ScriptableObject[] GetAllConfiguredRenderPipelines();

        public static RenderPipelineAsset[] allConfiguredRenderPipelines
        {
            get
            {
                return GetAllConfiguredRenderPipelines().Cast<RenderPipelineAsset>().ToArray();
            }
        }

        [FreeFunction] extern public static Object GetGraphicsSettings();

        [NativeName("SetShaderModeScript")]   extern static public void                 SetShaderMode(BuiltinShaderType type, BuiltinShaderMode mode);
        [NativeName("GetShaderModeScript")]   extern static public BuiltinShaderMode    GetShaderMode(BuiltinShaderType type);

        [NativeName("SetCustomShaderScript")] extern static public void     SetCustomShader(BuiltinShaderType type, Shader shader);
        [NativeName("GetCustomShaderScript")] extern static public Shader   GetCustomShader(BuiltinShaderType type);


        #region Default Settings for Render Pipelines
        static public void RegisterRenderPipelineSettings<T>(RenderPipelineGlobalSettings settings) where T : RenderPipeline
        {
            RegisterRenderPipeline(typeof(T).FullName, settings);
        }

        [NativeName("RegisterRenderPipelineSettings")] extern static void RegisterRenderPipeline(string renderpipelineName, Object settings);

        static public void UnregisterRenderPipelineSettings<T>() where T : RenderPipeline
        {
            UnregisterRenderPipeline(typeof(T).FullName);
        }

        [NativeName("UnregisterRenderPipelineSettings")] extern static void UnregisterRenderPipeline(string renderpipelineName);

        static public RenderPipelineGlobalSettings GetSettingsForRenderPipeline<T>() where T : RenderPipeline
        {
            RenderPipelineGlobalSettings settings = GetSettingsForRenderPipeline(typeof(T).FullName) as RenderPipelineGlobalSettings;
            return settings;
        }

        [NativeName("GetSettingsForRenderPipeline")] extern static Object GetSettingsForRenderPipeline(string renderpipelineName);

        #endregion
    }
}
