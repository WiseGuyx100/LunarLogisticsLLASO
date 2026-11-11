using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class LunarTransferSetup
{
    const string Folder="Assets/LunarOutpost/";
    const string Menu="Assets/PixelPackersMenuKit/Scenes/PixelPackersMenu_Demo.unity";
    static readonly string[] Scenes={"Moon Test","Lunar Service Level","Lunar Flight Preview"};

    [MenuItem("Assets/Connect Pixel Packers Lunar Map")]
    static void Connect()
    {
        var paths=Scenes.Select(n=>Folder+n+".unity").ToArray();
        if(paths.Any(p=>!AssetDatabase.LoadAssetAtPath<SceneAsset>(p)))
        {Debug.LogError("Import all three lunar scenes first.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;

        var entries=EditorBuildSettings.scenes.ToList();
        foreach(var path in paths)
        {
            int index=entries.FindIndex(s=>s.path==path);
            var entry=new EditorBuildSettingsScene(path,true);
            if(index>=0)entries[index]=entry;else entries.Add(entry);
        }
        EditorBuildSettings.scenes=entries.ToArray();

        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Menu))
        {
            var before=EditorSceneManager.GetSceneManagerSetup();
            var scene=EditorSceneManager.OpenScene(Menu);
            foreach(var component in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if(!component||component.GetType().FullName!="PixelPackersMenuKit.PortableMenuController")continue;
                using var serialized=new SerializedObject(component);
                var field=serialized.FindProperty("lunarScene");
                if(field!=null){field.stringValue=Scenes[0];serialized.ApplyModifiedPropertiesWithoutUndo();}
            }
            EditorSceneManager.SaveScene(scene);
            if(before.Any(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(before);
        }
        Debug.Log("Lunar scenes added to the global scene list. If your build profile overrides that list, add the same three scenes to that profile. Test the menu and both hatch directions before retiring the previous scene.");
    }
}
