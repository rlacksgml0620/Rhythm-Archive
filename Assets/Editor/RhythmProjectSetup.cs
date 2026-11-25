using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmArchive.EditorTools
{
    /// <summary>
    /// 프로젝트를 처음 열었을 때 메인 씬 생성, 빌드 목록 등록, 모바일 기본 설정을 자동으로 해 준다.
    /// 메뉴: Rhythm Archive > Apply Mobile Player Settings 로 다시 적용할 수 있다.
    /// </summary>
    [InitializeOnLoad]
    static class RhythmProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string SetupDoneKey = "RhythmArchive.SetupDone.v1";

        static RhythmProjectSetup()
        {
            EditorApplication.delayCall += EnsureSetup;
        }

        static void EnsureSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[Rhythm Archive] Main 씬을 만들었습니다: " + ScenePath);
            }

            bool inBuild = false;
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path == ScenePath) inBuild = true;
            if (!inBuild)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = list.ToArray();
            }

            // 프로젝트별로 한 번만 자동 적용
            string key = SetupDoneKey + "." + Application.dataPath.GetHashCode();
            if (!EditorPrefs.GetBool(key, false))
            {
                ApplyMobileSettings();
                EditorPrefs.SetBool(key, true);
            }
        }

        [MenuItem("Rhythm Archive/Apply Mobile Player Settings")]
        static void ApplyMobileSettings()
        {
            PlayerSettings.companyName = "RhythmArchive";
            PlayerSettings.productName = "Rhythm Archive";

            // 세로 화면 고정
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // 앱 ID (스토어 출시 전에 본인 도메인으로 바꾸세요. 예: com.yourname.rhythmarchive)
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.rhythmarchive.game");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.rhythmarchive.game");

            // Google Play는 64비트(ARM64) 필수 → IL2CPP
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;

            PlayerSettings.runInBackground = false;
            Debug.Log("[Rhythm Archive] 모바일 플레이어 설정을 적용했습니다(세로 화면, IL2CPP/ARM64, 앱 ID).");
        }
    }
}
