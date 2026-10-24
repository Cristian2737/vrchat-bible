using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using System.IO;

namespace VRChatBibleUI.Editor
{
    public static class BiblePackageInstaller
    {
        [MenuItem("Tools/VRChat Bible/Add Bible to Open Scene")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before adding the Bible.");
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open a world scene first.");
            var readers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BibleController>(true)).ToArray();
            if (readers.Length > 1) throw new InvalidOperationException("Keep one Bible system per world scene so shortcuts and saved data have one owner.");
            if (readers.Length == 1)
            {
                BiblePackageValidator.Validate(readers[0]);
                Selection.activeGameObject = readers[0].gameObject;
                Debug.Log("[Bible] This scene already has a Bible; its configuration was preserved.");
                return;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BiblePrefabBuilder.AssetPath("Prefabs/VRChatBible.prefab"));
            if (prefab == null) throw new InvalidOperationException("The Bible prefab is missing. Import the complete release package.");
            EnsureTextSettings();
            var rootObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            try { BiblePackageValidator.Validate(rootObject.GetComponentInChildren<BibleController>(true)); }
            catch { UnityEngine.Object.DestroyImmediate(rootObject); throw; }
            Undo.RegisterCreatedObjectUndo(rootObject, "Add VRChat Bible");
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = rootObject;
            Debug.Log("[Bible] Added to the open scene. Place the launcher/station, then save your scene.");
        }

        private static void EnsureTextSettings()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return;
            const string target = "Assets/VRChatBibleGenerated/Resources/TMP Settings.asset";
            if (File.Exists(target)) throw new InvalidOperationException("Existing generated TMP settings could not be loaded. Repair that asset before installing.");
            Directory.CreateDirectory("Assets/VRChatBibleGenerated/Resources");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!AssetDatabase.CopyAsset(BiblePrefabBuilder.AssetPath("Editor/DefaultTMPSettings.asset"), target))
                throw new InvalidOperationException("Could not create missing TMP settings.");
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            if (TMP_Settings.LoadDefaultSettings() == null) throw new InvalidOperationException("TMP settings could not be initialized.");
        }

        [MenuItem("Tools/VRChat Bible/Validate Bible in Open Scene")]
        public static void Validate()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var readers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BibleController>(true)).ToArray();
            if (readers.Length != 1) throw new InvalidOperationException("Expected one Bible in the open scene.");
            BiblePackageValidator.Validate(readers[0]);
        }
    }
}
