using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

internal static partial class FacilityRepair
{
    const string PrefabsR6="Assets/PixelPackersOffice/DownloadedPrefabs";
    static readonly string[] NamesR6={"BollardR6","DeskLampR6","ExtinguisherR6","FireAlarmR6","FirstAidR6","LaunchButtonR6","OfficeChairR6","FilingCabinetR6","PowerBoxR6","SecurityCameraR6"};
    static readonly float[] HeightsR6={1,.6f,.6f,.18f,.16f,.26f,1.05f,1.2f,.6f,.28f};
    [MenuItem("Tools/Pixel Packers/Apply Downloaded Assets and Fixes (R6)")]
    public static void ApplyR6()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorUtility.DisplayDialog("R6","Exit Play Mode first.","OK");return;}
        if(scene.name!="GameTest"&&scene.name!="Moon Test"&&scene.name!="Lunar Service Level"){EditorUtility.DisplayDialog("R6","Open GameTest, Moon Test or Lunar Service Level.","OK");return;}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Apply downloaded details R6");
        try
        {
            manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Assets+"surfaces.json"));
            PreparePrefabsR6();
            if(scene.name=="GameTest"){EarthDetailsR6();InstallOfficeInteractions();FixLightOutputR6();}
            if(scene.name=="Lunar Service Level")ServiceLayoutR6();
            if(scene.name=="Moon Test")MoonDetailsR6();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(group);
            Debug.Log("R6 applied to "+scene.name+". Save this scene. Built-in office chair and cabinets retained.");
            EditorUtility.DisplayDialog("R6 applied",scene.name+" updated. Save this scene (Ctrl+S).\nRun the same R6 command in GameTest, Lunar Service Level and Moon Test.\nDownloaded chair/cabinet alternatives are available in PixelPackersOffice/DownloadedPrefabs; the office uses its built-in furniture.","OK");
        }
        catch(Exception e){Undo.RevertAllDownToGroup(group);Debug.LogException(e);EditorUtility.DisplayDialog("R6 stopped",e.Message,"OK");}
    }
    static void PreparePrefabsR6()
    {
        if(!AssetDatabase.IsValidFolder(PrefabsR6))AssetDatabase.CreateFolder("Assets/PixelPackersOffice","DownloadedPrefabs");
        for(int i=0;i<NamesR6.Length;i++)
        {
            var g=Instance(NamesR6[i],new GameObject("Temporary R6 export").transform,ModelMaterials(NamesR6[i]));var owner=g.transform.parent.gameObject;
            try
            {
                var b=BoundsOf(g);g.transform.localScale*=HeightsR6[i]/Mathf.Max(.001f,b.size.y);b=BoundsOf(g);g.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
                PrefabUtility.SaveAsPrefabAsset(owner,PrefabsR6+"/"+NamesR6[i]+".prefab");
                if(NamesR6[i]=="LaunchButtonR6")
                {
                    const string resource="Assets/Resources/PixelPackersMission";
                    if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");
                    if(!AssetDatabase.IsValidFolder(resource))AssetDatabase.CreateFolder("Assets/Resources","PixelPackersMission");
                    PrefabUtility.SaveAsPrefabAsset(owner,resource+"/LaunchButtonR6.prefab");
                }
            }
            finally{Object.DestroyImmediate(owner);}
        }
    }
    static Transform FreshR6(Transform parent,string name)
    {
        var old=parent.Find(name);if(old)Undo.DestroyObjectImmediate(old.gameObject);
        var g=new GameObject(name);g.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(g,"Place downloaded details");return g.transform;
    }
    static GameObject PropR6(string name,Transform parent,Vector3 floor,float yaw=0,float scale=1)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsR6+"/"+name+".prefab");
        if(!prefab)throw new Exception("Missing prepared prefab: "+name);
        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent.gameObject.scene);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=floor;g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one*scale;return g;
    }
    static void EarthDetailsR6()
    {
        var site=GameObject.Find("LLASO_Facility");if(!site)throw new Exception("LLASO_Facility missing.");
        var mesh=site.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m=>m.sharedMesh&&AssetDatabase.GetAssetPath(m.sharedMesh).EndsWith("/LLASO_Facility.obj"));
        if(!mesh)throw new Exception("Original facility anchor missing.");
        var root=FreshR6(mesh.transform,"Downloaded decorations R6");
        PropR6("FireAlarmR6",root,new Vector3(-8,1.35f,50.95f));
        PropR6("ExtinguisherR6",root,new Vector3(-8.7f,.15f,50.7f));
        PropR6("PowerBoxR6",root,new Vector3(15.8f,1.4f,50.7f),0);
        PropR6("SecurityCameraR6",root,new Vector3(-9,3.15f,50.8f));
        PropR6("SecurityCameraR6",root,new Vector3(15.5f,3.4f,3.2f),180);
        // Replace only the narrow original entrance-post triangles in the split finish.
        ReplaceEntrancePostsR6(mesh.transform,root);
        var office=GameObject.Find("Pixel Packers - Logistics Office");
        if(office)
        {
            var detail=FreshR6(office.transform,"Office accessories R6");
            var desk=office.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name=="Desk");
            var cupboard=office.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name=="Wall_Shelves");
            if(desk)
            {
                var b=desk.bounds;var lamp=PropR6("DeskLampR6",detail,Vector3.zero,0,.7f);
                lamp.transform.position=new Vector3(b.min.x+.13f,b.max.y-.035f,b.max.z-.12f);
                var l=new GameObject("Desk task light",typeof(Light)).GetComponent<Light>();l.transform.SetParent(lamp.transform,false);l.transform.localPosition=new Vector3(0,.45f,0);l.transform.rotation=Quaternion.Euler(90,0,0);l.type=LightType.Spot;l.spotAngle=95;l.range=2.6f;l.intensity=2.2f;l.color=new Color(1,.94f,.84f);
            }
            if(cupboard){var b=cupboard.bounds;var kit=PropR6("FirstAidR6",detail,Vector3.zero);kit.transform.position=new Vector3(b.center.x,b.max.y+.01f,b.center.z);}
        }
    }
    static void ReplaceEntrancePostsR6(Transform facility,Transform root)
    {
        var repaired=facility.Find("Facility repaired R2");if(!repaired)return;
        // Existing OBJ posts sit outside the front wall. Keep all interior structural parts.
        foreach(var mf in repaired.GetComponentsInChildren<MeshFilter>())
        {
            if(mf.name!="Fac_DarkBlue")continue;
            string source=Assets+mf.name+".obj";var asset=AssetDatabase.LoadAssetAtPath<GameObject>(source);if(!asset)continue;
            var original=asset.GetComponentInChildren<MeshFilter>().sharedMesh;var vs=original.vertices;var tris=original.triangles;var kept=new System.Collections.Generic.List<int>();bool changed=false;
            for(int i=0;i<tris.Length;i+=3)
            {
                bool post=true;for(int j=0;j<3;j++){var v=vs[tris[i+j]];post&=v.z>=52.04f&&v.z<=54.56f&&v.y>=-.001f&&v.y<=1.001f&&Mathf.Abs(v.x)>=5.94f&&Mathf.Abs(v.x)<=6.26f;}
                if(post)changed=true;else kept.AddRange(new[]{tris[i],tris[i+1],tris[i+2]});
            }
            if(!changed)continue;
            var copy=Object.Instantiate(original);copy.subMeshCount=1;copy.SetTriangles(kept,0);string path=Assets+mf.name+"_NoPostsR6.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(copy,existing);Object.DestroyImmediate(copy);}else{AssetDatabase.CreateAsset(copy,path);existing=copy;}
            Undo.RecordObject(mf,"Replace entrance post visuals");mf.sharedMesh=existing;
        }
        foreach(float x in new[]{-6.1f,6.1f})foreach(float z in new[]{52.2f,54.4f})PropR6("BollardR6",root,new Vector3(x,0,z));
    }
    static void MoonDetailsR6()
    {
        var power=GameObject.Find("Equipment and power annex");if(!power)throw new Exception("Equipment and power annex missing.");
        var root=FreshR6(power.transform,"Safety equipment R6");PropR6("PowerBoxR6",root,new Vector3(10.5f,1.3f,30.5f),90);PropR6("FireAlarmR6",root,new Vector3(-10.4f,1.35f,28),-90);PropR6("ExtinguisherR6",root,new Vector3(-10.2f,.13f,28.5f));
    }
    static void ServiceLayoutR6()
    {
        var room=GameObject.Find("Hygiene and fitness");if(!room)throw new Exception("Hygiene and fitness room missing.");
        Transform Required(string name)=>room.transform.Find(name)??throw new Exception("Missing room object: "+name);
        void Set(string name,Vector3 p,Vector3 size){var t=Required(name);Undo.RecordObject(t,"Rebuild hygiene layout R6");t.localPosition=p;t.localRotation=Quaternion.identity;t.localScale=size;}
        Set("Hygiene partition",new Vector3(8.35f,1.36f,9.05f),new Vector3(.14f,2.7f,3.65f));
        Set("Fitness mat",new Vector3(6.1f,.032f,4.65f),new Vector3(2.3f,.024f,2));
        var bike=Required("stationary_exercise_bike");Undo.RecordObject(bike,"Move bike away from entry");bike.localPosition=new Vector3(6.1f,.02f,4.65f);bike.localRotation=Quaternion.Euler(0,90,0);
        var wash=Required("shower_room_-_assets");Undo.RecordObject(wash,"Align complete wash fixtures");wash.localRotation=Quaternion.identity;
        int restored=0;
        foreach(var r in wash.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.name.StartsWith("Object_"))continue;int id;if(!int.TryParse(r.name.Substring(7),out id))continue;
            Undo.RecordObject(r,"Restore missing toilet components");r.enabled=id>=4&&id<=58&&id!=32&&id!=34;if(r.enabled)restored++;
            r.sharedMaterials=r.sharedMaterials.Select(m=>m?AssetDatabase.LoadAssetAtPath<Material>("Assets/LunarOutpost/Models/shower_room_-_assets/"+m.name.Replace(" (Instance)","")+".mat")??m:m).ToArray();
        }
        var rr=wash.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(rr.Length<15)throw new Exception("Incomplete wash model: expected toilet and basin pieces.");
        var bounds=rr[0].bounds;foreach(var r in rr.Skip(1))bounds.Encapsulate(r.bounds);
        wash.position+=new Vector3(10.55f-bounds.max.x,.025f-bounds.min.y,9.05f-bounds.center.z);
        var oldCollider=wash.GetComponent<BoxCollider>();if(oldCollider){Undo.RecordObject(oldCollider,"Resize wash collision");oldCollider.enabled=false;}
        foreach(var mf in wash.GetComponentsInChildren<MeshFilter>())if(mf.GetComponent<Renderer>()&&mf.GetComponent<Renderer>().enabled&&!mf.GetComponent<Collider>()){var c=Undo.AddComponent<MeshCollider>(mf.gameObject);c.sharedMesh=mf.sharedMesh;}
        Set("Wash area backing wall",new Vector3(10.68f,1.65f,9.05f),new Vector3(.14f,3.25f,3.7f));
        var root=FreshR6(room.transform,"Service details R6");PropR6("FireAlarmR6",root,new Vector3(3.16f,1.35f,8.5f),-90);
        var medical=GameObject.Find("Medical and plant research");if(medical)
        {var med=FreshR6(medical.transform,"Medical supplies R6");var table=medical.transform.Find("metal_office_desk_2k.blend");if(table){var b=BoundsOf(table.gameObject);var kit=PropR6("FirstAidR6",med,Vector3.zero);kit.transform.position=new Vector3(b.center.x,b.max.y+.01f,b.center.z);} }
        Selection.activeGameObject=room;if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.FrameSelected();
        Debug.Log("R6: restored "+restored+" wash-fixture pieces; privacy screen now behind the entry aisle.");
    }
    static void FixLightOutputR6()
    {
        var site=GameObject.Find("LLASO_Facility");if(!site)return;var structure=site.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Facility repaired R2");if(!structure)throw new Exception("Run the R5 facility repair once before R6.");
        var root=FreshR6(structure,"Working fixture lights R6");var glow=NewMaterial("Frosted diffuser R6");glow.SetColor("_BaseColor",new Color(.94f,.97f,1));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.9f,.95f,1)*5);
        foreach(var light in structure.GetComponentsInChildren<Light>())if(light.name=="Downward warehouse light R5"){Undo.RecordObject(light,"Replace dim fixture lighting");light.enabled=false;}
        foreach(float x in new[]{-8f,8f})foreach(float z in new[]{10f,22f,34f,46f})
        {
            Support(root,new Vector3(x,8.805f,z),new Vector3(.30f,.012f,2.18f),glow);
            DownLight(root,new Vector3(x,8.70f,z),22,19,115);
        }
        IlluminateWarehousePanels();
        foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.name=="High panel light")
        {Undo.RecordObject(light,"Improve high-panel lighting");light.intensity=7;light.range=13;light.spotAngle=125;light.innerSpotAngle=85;light.shadows=LightShadows.Soft;light.shadowNormalBias=.12f;light.transform.position+=light.transform.forward*.3f;}
    }
}
