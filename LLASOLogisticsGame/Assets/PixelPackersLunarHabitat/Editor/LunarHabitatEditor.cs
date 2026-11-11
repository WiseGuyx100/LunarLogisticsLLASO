using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelPackersLunarHabitat.Editor
{
    public static class HabitatSceneTools
    {
        public const string ScenePath="Assets/PixelPackersLunarHabitat/Scenes/LunarHabitat_Demo.unity";
        [MenuItem("Tools/Pixel Packers Lunar Habitat/Open Scene")]
        public static void OpenScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){EditorUtility.DisplayDialog("Stop Play first","Stop Play mode before opening the editable scene.","OK");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var habitat=Find(scene);if(habitat==null)throw new InvalidOperationException("LunarHabitat component was not imported. Reimport the complete package.");
            if(habitat.transform.Find("Habitat Layout")==null)habitat.Build();Bake(habitat);EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject=habitat.gameObject;
            if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(0,8,0),Quaternion.Euler(25,0,0),95);
        }
        [InitializeOnLoadMethod]
        static void Register()
        {EditorSceneManager.sceneOpened-=Opened;EditorSceneManager.sceneOpened+=Opened;}
        static void Opened(Scene scene,OpenSceneMode mode)
        {if(scene.path!=ScenePath||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.delayCall+=()=>{if(scene.IsValid()&&scene.isLoaded){var habitat=Find(scene);if(habitat!=null)Bake(habitat);}};}
        static LunarHabitat Find(Scene scene)
        {foreach(var root in scene.GetRootGameObjects()){var h=root.GetComponentInChildren<LunarHabitat>(true);if(h!=null)return h;}return null;}
        public static void Bake(LunarHabitat habitat)
        {
            const string directory="Assets/PixelPackersLunarHabitat/SceneAssets";Directory.CreateDirectory(directory);AssetDatabase.Refresh();
            foreach(var filter in habitat.GetComponentsInChildren<MeshFilter>(true))Persist(filter.sharedMesh,directory);
            foreach(var collider in habitat.GetComponentsInChildren<MeshCollider>(true))Persist(collider.sharedMesh,directory);
            foreach(var renderer in habitat.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)Persist(material,directory);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(habitat.gameObject.scene);
        }
        static void Persist(UnityEngine.Object asset,string directory)
        {if(asset==null||!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))return;string name=asset.name;foreach(char c in Path.GetInvalidFileNameChars())name=name.Replace(c,'_');var path=AssetDatabase.GenerateUniqueAssetPath(directory+"/"+name+(asset is Material?".mat":".asset"));AssetDatabase.CreateAsset(asset,path);}
    }
    [CustomEditor(typeof(LunarSceneLauncher))]
    public sealed class LunarSceneLauncherEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Pixel Packers / Lunar Habitat",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Open the editable dome habitat and lunar work area. The first open generates and saves its meshes and materials in this project.",MessageType.Info);
            if(GUILayout.Button("Open Scene",GUILayout.Height(45)))HabitatSceneTools.OpenScene();
        }
    }
    [CustomEditor(typeof(LunarHabitat))]
    public sealed class LunarHabitatInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();var habitat=(LunarHabitat)target;
            EditorGUILayout.HelpBox("All layout objects are ordinary editable scene objects under Habitat Layout. Save the scene after editing. Rebuild replaces that layout.",MessageType.Info);
            if(GUILayout.Button("Open Scene"))HabitatSceneTools.OpenScene();
            if(GUILayout.Button("Rebuild Habitat Layout")&&EditorUtility.DisplayDialog("Rebuild layout?","This replaces the Habitat Layout subtree, including edits inside it. Objects outside that subtree are retained.","Rebuild","Cancel"))
            {var old=habitat.transform.Find("Habitat Layout");if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);habitat.Build();HabitatSceneTools.Bake(habitat);}
            if(GUILayout.Button("Save Editable Scene")){HabitatSceneTools.Bake(habitat);EditorSceneManager.SaveScene(habitat.gameObject.scene);}
        }
    }
}
