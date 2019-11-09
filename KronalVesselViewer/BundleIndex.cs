using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KronalUtils
{
    class BundleIndex : MonoBehaviour
    {

        //KSPAssets.AssetDefinition[] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KronalUtils.Properties.Resources.ShaderFXAA, typeof(Shader));
        /*
        public Dictionary<string, string> ShaderData = new Dictionary<string, string> { 
            { "MaterialFXAA", KronalUtils.Properties.Resources.ShaderFXAA }, 
            { "MaterialColorAdjust", KSP.IO.File.ReadAllText<KVrVesselShot>("coloradjust") }, 
            { "MaterialEdgeDetect", KSP.IO.File.ReadAllText<KVrVesselShot>("edn2") }, 
            { "MaterialBluePrint", KSP.IO.File.ReadAllText<KVrVesselShot>("blueprint") }, 
        };*/
        internal static Dictionary<string, Font> loadedFonts = new Dictionary<string, Font>();
        // internal static Dictionary<string, Shader> loadedShaders = new Dictionary<string, Shader>();
        /*public Dictionary<string, string> ShaderData = new Dictionary<string, string> {
            { "MaterialFXAA", "KVV/Hidden/SlinDev/Desktop/PostProcessing/FXAA" },
            { "MaterialColorAdjust", "KVV/Color Adjust" },
            { "MaterialEdgeDetect", "Hidden/Edge Detect Normals2" },
        };*/
        //  public Dictionary<string, KSPAssets.Loaders.AssetLoader> KVrShaders = new Dictionary<string, KSPAssets.Loaders.AssetLoader>();
        //  public Dictionary<string, KSPAssets.Loaders.AssetLoader> KVrShaders2 = new Dictionary<string, KSPAssets.Loaders.AssetLoader>();
        public BundleIndex()
        {
            /*
            this.Effects = new Dictionary<string, ShaderMaterial>() {
                {"Color Adjust",MaterialColorAdjust},
                {"Edge Detect", MaterialEdgeDetect},
                {"Blue Print", MaterialBluePrint},
                {"FXAA", MaterialFXAA}
            };*/
            //KSPAssets.AssetDefinition[] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KronalUtils.Properties.Resources.ShaderFXAA, typeof(Shader));



            InitShaders();
        }

        public bool BundleLoaded = false;
        public static Dictionary<string, Shader> LoadedShaders = new Dictionary<string, Shader>();
        private IEnumerator coroutine;


        public void LoadBundle()
        {
            if (BundleLoaded)
                return;
            log.debug("LoadBundle");
            coroutine = doLoadBundle();

            StartCoroutine(coroutine);
        }
        IEnumerator doLoadBundle()
        {
            log.debug(string.Format("Application.platform: {0}", Application.platform));
            string bundleName;
            switch (Application.platform)
            {
                case RuntimePlatform.OSXPlayer:
                    bundleName = "shaders.osx.bundle";
                    break;

                case RuntimePlatform.LinuxPlayer:
                    bundleName = "shaders.linux.bundle";
                    break;

                case RuntimePlatform.WindowsPlayer:
                    bundleName = "shaders.windows.bundle";
                    break;

                default:
                    bundleName = "shaders";
                    break;
            }

            log.debug(string.Format("Loading shader bundle file: {0}", bundleName));
            WWW www = new WWW("file://" + KSPUtil.ApplicationRootPath + "GameData/KronalVesselViewer/Resources/" + bundleName);
            yield return www;
            {
                if (www.error != null)
                    log.error("Shaders bundle not found");

                AssetBundle bundle = www.assetBundle;

                Shader[] shaders = bundle.LoadAllAssets<Shader>();

                foreach (Shader shader in shaders)
                {
                    log.debug(string.Format("Found shader {0} in asset bundle", shader.name));
                    LoadedShaders.Add(shader.name, shader);
                }

                bundle.Unload(false);
                www.Dispose();

                BundleLoaded = true;
            }
        }


        private void InitShaders()
        {
            LoadBundle();

#if false
            log.debug(string.Format("InitShaders 1: {0}", KSPAssets.Loaders.AssetLoader.ApplicationRootPath));

            string KVrPath = KVrUtilsCore.ModRoot();
            log.debug(string.Format("KVrPath: {0}", KVrPath));

            string KVrAssetPath = Path.GetDirectoryName(KVrPath + Path.DirectorySeparatorChar);
            log.debug(string.Format("KVrAssetPath: {0}", KVrAssetPath));
            KVrAssetPath = "KronalUtils";
            log.debug(string.Format("InitShaders 2 KVrPath{0} \n\t- Directory  Path.GetFileName( KVrAssetPath ...) {1} ", KVrPath, Path.Combine(KVrAssetPath, "kvv")));
            //, String.Join("\n\t- ", System.IO.Directory.GetFiles(KVrPath))
            KSPAssets.AssetDefinition[] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KVrAssetPath + "/kvv", typeof(Shader));//path to kvv.ksp
            if (KVrShaders == null || KVrShaders.Length == 0)
            {
                log.error(string.Format("Failed to load Asset Package KronalUtils/kvv.ksp in {0}.", KVrPath));
                return;
            }else {
                KSPAssets.Loaders.AssetLoader.LoadAssets(ShadersLoaded, KVrShaders[0]);
            }

            log.debug(string.Format("InitShaders 4"));
            /*
            foreach (KeyValuePair<string, string> itKey in ShaderData)
            {
                //KSPAssets.AssetDefinition[itKey.Key] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KronalUtils.Properties.Resources.ShaderFXAA, typeof(Shader));
                //KSPAssets.Loaders.AssetLoader newShad = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(ShaderData[itKey.Key], typeof(Shader));
                //KVrShaders.Add(itKey.Key, new KSPAssets.Loaders.AssetLoader());
                //KVrShaders[itKey.Key] = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(ShaderData[itKey.Key], typeof(UnityEngine.Shader));
                // KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KronalUtils.Properties.Resources.ShaderFXAA, typeof(Shader));
            }*/
#endif
        }
        public Shader getShaderById(string idIn)
        {
            return (LoadedShaders.ContainsKey(idIn) ? LoadedShaders [idIn] : null);
        }
#if false
        private void ShadersLoaded(KSPAssets.Loaders.AssetLoader.Loader loader) // thanks moarDV - https://github.com/Mihara/RasterPropMonitor/blob/5c9fa8b259dd391892fe121724519413ccbb6b59/RasterPropMonitor/Core/UtilityFunctions.cs
        {
            log.debug(string.Format("ShadersLoaded"));
            string aShaderName = string.Empty;
            for (int i = 0; i < loader.objects.Length; ++i)
            {
                UnityEngine.Object o = loader.objects[i];
                if (o != null && o is Shader)
                {
                    // We'll remember the name of whichever shader we were
                    // able to load.
                    aShaderName = o.name;
                    break;
                }
            }

            if (string.IsNullOrEmpty(aShaderName))
            {
                log.warning(string.Format("Unable to find a named shader \"{0}\".", aShaderName));
                return;
            }

            var loadedBundles = KSPAssets.Loaders.AssetLoader.LoadedBundles;
            if (loadedBundles == null)
            {
                log.warning(string.Format("Unable to find any loaded bundles in AssetLoader."));
                return;
            }

            // Iterate over all loadedBundles.  Experimentally, my bundle was
            // the only one in the array, but I expect that to change as other
            // mods use asset bundles (maybe none of the mods I have load this
            // early).
            for (int i = 0; i < loadedBundles.Count; ++i)
            {
                Shader[] shaders = null;
                Font[] fonts = null;
                bool theRightBundle = false;

                try
                {
                    // Try to get a list of all the shaders in the bundle.
                    shaders = loadedBundles[i].LoadAllAssets<Shader>();
                    if (shaders != null)
                    {
                        // Look through all the shaders to see if our named
                        // shader is one of them.  If so, we assume this is
                        // the bundle we want.
                        for (int shaderIdx = 0; shaderIdx < shaders.Length; ++shaderIdx)
                        {
                            if (shaders[shaderIdx].name == aShaderName)
                            {
                                theRightBundle = true;
                                break;
                            }
                        }
                    }
                    fonts = loadedBundles[i].LoadAllAssets<Font>();
                }
                catch { }

                if (theRightBundle)
                {
                    // If we found our bundle, set up our parsedShaders
                    // dictionary and bail - our mission is complete.
                    for (int j = 0; j < shaders.Length; ++j)
                    {
                        if (!shaders[j].isSupported)
                        {
                            log.debug(string.Format("Shader {0} - unsupported in this configuration", shaders[j].name));
                        }
                        loadedShaders[shaders[j].name] = shaders[j];
                    }
                    for (int j = 0; j < fonts.Length; ++j)
                    {
                        log.debug(string.Format("Adding KSP-Bundle-included font {0} / {1}", fonts[j].name, fonts[j].fontSize));
                        loadedFonts[fonts[j].name] = fonts[j];
                    }
                    return;
                }
            }

            log.error(string.Format("Failed to load shaders  - how did this callback execute?"));
        }
#endif
    }

}
