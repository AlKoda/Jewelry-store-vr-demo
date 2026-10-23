#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

// The Meta Quest 3 standalone app: the same project and the same saved night scene as the
// PC player, built for Android into Builds/Quest/JewelryStoreDemo.apk. Like EditableDelivery
// it never regenerates the scene, so manual layout edits carry over. It sets the Android
// player (IL2CPP, ARM64, Vulkan, ASTC, Android 12L / API 32), assigns the OpenXR loader for
// Android with Meta Quest Support and the Touch / Touch Plus controller profiles, single-pass
// (multiview) stereo, and starts XR with the app. The Windows settings are not touched.
// Runtime differences on the headset come from DemoPlatform (lighter shadows, no PC views)
// and PackImport's Android texture cap. Build-Quest-App.cmd runs it and installs the APK;
// installing over the previous version keeps the app's photos and settings.
public static class QuestBuild
{
    public const string PackageName = "com.alkoda.jewelrystorecrimescene";
    public const string ProductName = "Jewelry Store Crime Scene";

    public static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/Quest/JewelryStoreDemo.apk"));

    [MenuItem("Crime Scene/Delivery/Build Quest 3 app (APK)")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new Exception("QUEST_BUILD_FAIL Android Build Support is not installed for this Unity. In Unity Hub: Installs > 6000.6.2f1 > Add modules > "
                + "Android Build Support, with OpenJDK and Android SDK & NDK Tools.");
        string scene = DemoValidation.ScenesFolder + "/JewelryStoreNight.unity";
        if (!File.Exists(scene)) throw new Exception("QUEST_BUILD_FAIL Night scene missing; generate it once with CombinedBuild.Run (Play-Keyboard.cmd bake).");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android
            && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            throw new Exception("QUEST_BUILD_FAIL Could not switch the project to Android.");
        ConfigurePlayer();
        ConfigureXR();
        AssetDatabase.SaveAssets();
        string output = OutputPath;
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        DemoBuild.CopyLicenses(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new[] { DemoBuild.LauncherScene, scene }, output, BuildTarget.Android, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("QUEST_BUILD_FAIL The Android player build failed; search the log for 'error'.");
        Debug.Log("QUEST_BUILD_PASS " + output + " versionCode " + PlayerSettings.Android.bundleVersionCode
            + " size " + (new FileInfo(output).Length / 1048576) + " MB");
    }

    // -executeMethod QuestBuild.Run (batch mode, with -buildTarget Android).
    public static void Run()
    {
        try { Build(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    public static void ConfigurePlayer()
    {
        NamedBuildTarget android = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(android, PackageName);
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        // Each build installs over the last one: the version code only ever grows (minutes since 2026).
        PlayerSettings.Android.bundleVersionCode = Math.Max(PlayerSettings.Android.bundleVersionCode + 1,
            (int)(DateTime.UtcNow - new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMinutes);
        // Same shader lifetime mitigation as the PC player (EditableDelivery).
        var player = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        SerializedProperty keep = player.FindProperty("keepLoadedShadersAlive");
        if (keep != null) { keep.boolValue = true; player.ApplyModifiedPropertiesWithoutUndo(); }
    }

    // OpenXR for Android, as Quest3Setup does for Windows: loader, features, multiview, XR on at start.
    public static void ConfigureXR()
    {
        Directory.CreateDirectory("Assets/XR/Settings");
        AssetDatabase.Refresh();
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget per))
        {
            per = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(per, "Assets/XR/Settings/XRGeneralSettings.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, per, true);
        }
        BuildTargetGroup group = BuildTargetGroup.Android;
        if (!per.HasSettingsForBuildTarget(group)) per.CreateDefaultSettingsForBuildTarget(group);
        if (!per.HasManagerSettingsForBuildTarget(group)) per.CreateDefaultManagerSettingsForBuildTarget(group);
        XRGeneralSettings settings = per.SettingsForBuildTarget(group);
        settings.InitManagerOnStart = true; // the headset app is VR from the first frame
        if (!XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group))
            throw new Exception("QUEST_BUILD_FAIL OpenXR loader assignment for Android failed");
        Type packageSettings = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.XR.OpenXR.OpenXRPackageSettings")).First(t => t != null);
        packageSettings.GetMethod("GetOrCreateInstance", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(group);
        OpenXRSettings xr = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
        if (!xr) throw new Exception("QUEST_BUILD_FAIL OpenXR settings for Android missing");
        xr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        bool quest = false;
        foreach (OpenXRFeature feature in xr.GetFeatures<OpenXRFeature>())
        {
            string name = feature.GetType().Name;
            if (name == "MetaQuestFeature") { feature.enabled = true; quest = true; }
            else if (name == "FoveatedRenderingFeature") feature.enabled = true; // fixed foveation; NightQuality sets the level at runtime
            else if (feature is OpenXRInteractionFeature)
                feature.enabled = name == "OculusTouchControllerProfile" || name == "MetaQuestTouchPlusControllerProfile";
            EditorUtility.SetDirty(feature);
        }
        if (!quest) throw new Exception("QUEST_BUILD_FAIL The OpenXR package has no Meta Quest Support feature for Android");
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(settings.Manager);
        EditorUtility.SetDirty(per);
        EditorUtility.SetDirty(xr);
    }
}
#endif
