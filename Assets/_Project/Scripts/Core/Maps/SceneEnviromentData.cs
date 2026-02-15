using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Core.Maps
{
    [ExecuteInEditMode]
    public class SceneEnviromentData : MonoBehaviour
    {
        public Material skybox;
        public Light sun;

        [Space(9)]
        public Color subtractiveShadowColor;

        [Space(9)]
        public AmbientMode ambientMode;
        public Color ambientLight;
        public Color ambientSkyColor;
        public Color ambientEquatorColor;
        public Color ambientGroundColor;
        public float ambientIntensity;

        [Space(9)]
        public DefaultReflectionMode defaultReflectionMode;
        public int defaultReflectionResolution;
        public Texture customReflectionTexture;
        public float reflectionIntensity;
        public int reflectionBounces;

        [Space(9)]
        public bool fog;
        public Color fogColor;
        public FogMode fogMode;
        public float fogStartDistance;
        public float fogEndDistance;
        public float fogDensity;


        private void Update()
        {
            if (Application.isPlaying) return;

            skybox = RenderSettings.skybox;
            sun = RenderSettings.sun;

            subtractiveShadowColor = RenderSettings.subtractiveShadowColor;

            ambientMode = RenderSettings.ambientMode;
            ambientLight = RenderSettings.ambientLight;
            ambientSkyColor = RenderSettings.ambientSkyColor;
            ambientEquatorColor = RenderSettings.ambientEquatorColor;
            ambientGroundColor = RenderSettings.ambientGroundColor;
            ambientIntensity = RenderSettings.ambientIntensity;

            defaultReflectionMode = RenderSettings.defaultReflectionMode;
            defaultReflectionResolution = RenderSettings.defaultReflectionResolution;
            customReflectionTexture = RenderSettings.customReflectionTexture;
            reflectionIntensity = RenderSettings.reflectionIntensity;
            reflectionBounces = RenderSettings.reflectionBounces;

            fog = RenderSettings.fog;
            fogColor = RenderSettings.fogColor;
            fogMode = RenderSettings.fogMode;
            fogStartDistance = RenderSettings.fogStartDistance;
            fogEndDistance = RenderSettings.fogEndDistance;
            fogDensity = RenderSettings.fogDensity;
        }

        public void Apply()
        {
            RenderSettings.skybox = skybox;
            RenderSettings.sun = sun;

            RenderSettings.subtractiveShadowColor = subtractiveShadowColor;

            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambientLight;
            RenderSettings.ambientSkyColor = ambientSkyColor;
            RenderSettings.ambientEquatorColor = ambientEquatorColor;
            RenderSettings.ambientGroundColor = ambientGroundColor;
            RenderSettings.ambientIntensity = ambientIntensity;

            RenderSettings.defaultReflectionMode = defaultReflectionMode;
            RenderSettings.defaultReflectionResolution = defaultReflectionResolution;
            RenderSettings.customReflectionTexture = customReflectionTexture;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            RenderSettings.reflectionBounces = reflectionBounces;

            RenderSettings.fog = fog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;
            RenderSettings.fogDensity = fogDensity;
        }

        public static bool TryApplyOnScene(Scene scene)
        {
            foreach (var obj in scene.GetRootGameObjects())
            {
                if (!obj.TryGetComponent(out SceneEnviromentData enviromentData)) continue;
                enviromentData.Apply();
                return true;
            }

            return false;
        }
    }
}