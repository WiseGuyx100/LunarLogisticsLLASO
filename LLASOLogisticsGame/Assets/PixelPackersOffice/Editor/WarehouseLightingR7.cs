using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

internal static partial class FacilityRepair
{
    [MenuItem("Tools/Pixel Packers/Fix Cargo Warehouse and Office (R7)")]
    public static void ApplyR7()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="GameTest"){EditorUtility.DisplayDialog("R7","Open GameTest outside Play Mode.","OK");return;}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Warehouse lighting and details R7");
        try
        {
            var warehouse=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).FirstOrDefault(m=>m.sharedMesh&&AssetDatabase.GetAssetPath(m.sharedMesh).Contains("Warehouse_Open"));
            if(!warehouse)throw new Exception("Curved cargo warehouse mesh not found.");
            manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Assets+"surfaces.json"));
            PreparePrefabsR6();
            int panels=WarehouseEmittersR7(warehouse);
            FacilityFixturesR7();WallTexturesR7();OfficeLightsR7();MoveWarehousePropsR7(warehouse);LaunchControlsR7();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(group);
            Debug.Log("R7: "+panels+" luminous cargo-warehouse surfaces; ceiling fixtures, office lighting, textured white walls, warehouse props and visible launch control installed.");
            EditorUtility.DisplayDialog("R7 applied",panels+" warehouse emitters installed. Office and facility lighting corrected, white walls textured, props moved into the cargo warehouse, and launch control placed in the scene. Save GameTest.","OK");
        }
        catch(Exception e){Undo.RevertAllDownToGroup(group);Debug.LogException(e);EditorUtility.DisplayDialog("R7 stopped",e.Message,"OK");}
    }
    static Material GlowR7(string name,Color color)
    {
        var m=NewMaterial(name);m.SetColor("_BaseColor",color);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*4);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;EditorUtility.SetDirty(m);return m;
    }
    static Light SpotR7(Transform parent,string name,Vector3 position,Vector3 direction,float intensity,float range,float angle,bool shadow=false)
    {
        var g=new GameObject(name,typeof(Light));g.transform.SetParent(parent,false);g.transform.position=position;g.transform.rotation=Quaternion.LookRotation(direction);
        var l=g.GetComponent<Light>();l.type=LightType.Spot;l.color=new Color(1,.96f,.89f);l.intensity=intensity;l.range=range;l.spotAngle=angle;l.innerSpotAngle=angle*.6f;l.shadows=shadow?LightShadows.Soft:LightShadows.None;l.shadowBias=.03f;l.shadowNormalBias=.1f;l.renderMode=LightRenderMode.ForcePixel;return l;
    }
    static GameObject BoxR7(Transform root,string name,Vector3 center,Vector3 size,Material mat)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.position=center;g.transform.rotation=Quaternion.identity;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static int WarehouseEmittersR7(MeshFilter mf)
    {
        // The emissive submesh includes both the rectangular wall panels and ceiling strips.
        // Copy its actual faces just inside the housing, avoiding assumptions about model axes.
        var old=mf.transform.Find("Window daylight R5");if(old)Undo.DestroyObjectImmediate(old.gameObject);
        var root=FreshR6(mf.transform,"Luminous warehouse panels R7");
        var mesh=mf.sharedMesh;var renderer=mf.GetComponent<Renderer>();var vertices=mesh.vertices;var mats=renderer.sharedMaterials;int count=0;
        var glow=GlowR7("Warehouse diffuser R7",new Color(.91f,.96f,1));
        for(int sub=0;sub<Mathf.Min(mesh.subMeshCount,mats.Length);sub++)
        {
            string name=mats[sub]?mats[sub].name.ToLowerInvariant():"";
            if(!name.Contains("emissive")&&!name.Contains("warehouselamp")&&!name.Contains("warehouse panel glow"))continue;
            var tris=mesh.GetTriangles(sub);var parents=new Dictionary<Vector3,Vector3>();
            Vector3 Key(Vector3 v)=>new Vector3(Mathf.Round(v.x*100000),Mathf.Round(v.y*100000),Mathf.Round(v.z*100000));
            Vector3 Find(Vector3 k){if(!parents.ContainsKey(k))parents[k]=k;if(parents[k]!=k)parents[k]=Find(parents[k]);return parents[k];}
            for(int i=0;i<tris.Length;i+=3){var a=Find(Key(vertices[tris[i]]));parents[Find(Key(vertices[tris[i+1]]))]=a;parents[Find(Key(vertices[tris[i+2]]))]=a;}
            foreach(var group in Enumerable.Range(0,tris.Length/3).GroupBy(i=>Find(Key(vertices[tris[i*3]]))))
            {
                var ids=group.SelectMany(i=>new[]{tris[i*3],tris[i*3+1],tris[i*3+2]}).ToArray();
                var center=Vector3.zero;foreach(int i in ids)center+=mf.transform.TransformPoint(vertices[i]);center/=ids.Length;
                var inward=renderer.bounds.center-center;inward.y=0;if(inward.sqrMagnitude<.01f)inward=Vector3.down;inward.Normalize();
                var b=new Bounds(center,Vector3.zero);foreach(int i in ids)b.Encapsulate(mf.transform.TransformPoint(vertices[i]));
                bool wall=b.size.y>.15f&&center.y<renderer.bounds.min.y+renderer.bounds.size.y*.65f;
                var offset=wall?inward*.045f:Vector3.down*.035f;
                var verts=ids.Select(i=>root.InverseTransformPoint(mf.transform.TransformPoint(vertices[i])+offset)).ToArray();
                var copy=new Mesh{name="Warehouse emitting face R7"};copy.vertices=verts;copy.triangles=Enumerable.Range(0,verts.Length).ToArray();copy.RecalculateNormals();copy.RecalculateBounds();
                string path=Assets+"WarehouseEmitterR7_"+count+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(copy,saved);Object.DestroyImmediate(copy);}else{AssetDatabase.CreateAsset(copy,path);saved=copy;}
                var g=new GameObject("Luminous "+(wall?"wall panel ":"ceiling strip ")+count,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root,false);g.GetComponent<MeshFilter>().sharedMesh=saved;g.GetComponent<MeshRenderer>().sharedMaterial=glow;g.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                SpotR7(root,"Warehouse panel illumination",center+(wall?inward*.25f:Vector3.down*.15f),wall?(inward+Vector3.down*.65f).normalized:Vector3.down,wall?5:7,wall?9:10,wall?115:110);
                count++;
            }
        }
        if(count==0)throw new Exception("No warehouse emissive surfaces found.");
        // Older point/spot lights compete for the per-object light budget and duplicate these fixtures.
        foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.name.StartsWith("Warehouse_Ceiling")){Undo.RecordObject(l,"Replace old warehouse lights");l.enabled=false;}
        return count;
    }
    static void FacilityFixturesR7()
    {
        var site=GameObject.Find("LLASO_Facility");if(!site)throw new Exception("Facility missing.");
        var structure=site.GetComponentsInChildren<Transform>().First(t=>t.name=="Facility repaired R2");
        var old=structure.Find("Working fixture lights R6");if(old)Undo.DestroyObjectImmediate(old.gameObject);
        foreach(var l in structure.GetComponentsInChildren<Light>()){Undo.RecordObject(l,"Replace fixture output");l.enabled=false;}
        var root=FreshR6(site.transform,"Fixture output R7");root.SetParent(null,true);root.localScale=Vector3.one;root.rotation=Quaternion.identity;root.position=Vector3.zero;
        var glow=GlowR7("Ceiling diffuser R7",new Color(.96f,.98f,1));
        foreach(var mf in structure.GetComponentsInChildren<MeshFilter>().Where(m=>m.sharedMesh&&AssetDatabase.GetAssetPath(m.sharedMesh).EndsWith("/CeilingLight.fbx")))
        {
            var b=mf.GetComponent<Renderer>().bounds;
            BoxR7(root,"Frosted glowing underside",new Vector3(b.center.x,b.min.y-.035f,b.center.z),new Vector3(b.size.x*.92f,.018f,b.size.z*.80f),glow).GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            SpotR7(root,"Facility ceiling illumination",new Vector3(b.center.x,b.min.y-.12f,b.center.z),Vector3.down,16,17,110,true);
        }
        root.SetParent(site.transform,true);
    }
    static void WallTexturesR7()
    {
        var site=GameObject.Find("LLASO_Facility");
        foreach(var mf in site.GetComponentsInChildren<MeshFilter>().Where(m=>new[]{"Fac_White","Fac_Grey","Fac_Purple","Fac_Green"}.Contains(m.name)))
        {
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(Assets+mf.name+".obj").GetComponentInChildren<MeshFilter>().sharedMesh;
            var m=Object.Instantiate(original);var vs=m.vertices;var ns=m.normals;var uv=new Vector2[vs.Length];
            for(int i=0;i<vs.Length;i++){var p=mf.transform.TransformPoint(vs[i]);var n=mf.transform.TransformDirection(ns[i]);var a=new Vector3(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z));uv[i]=a.y>a.x&&a.y>a.z?new Vector2(p.x,p.z):a.x>a.z?new Vector2(p.z,p.y):new Vector2(p.x,p.y);}
            m.uv=uv;m.RecalculateTangents();string path=Assets+mf.name+"_WallUVR7.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(m,saved);Object.DestroyImmediate(m);}else{AssetDatabase.CreateAsset(m,path);saved=m;}
            Undo.RecordObject(mf,"Project wall texture UVs");mf.sharedMesh=saved;
            var mat=NewMaterial(mf.name+" painted wall R7");mat.SetTexture("_BaseMap",Texture(Folder+"painted_plaster_wall_diff_2k.jpg"));mat.SetTexture("_BumpMap",Texture(Folder+"painted_plaster_wall_nor_gl_2k.jpg",true));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.65f);mat.SetFloat("_Smoothness",.18f);mat.SetTextureScale("_BaseMap",Vector2.one*.65f);mat.SetTextureScale("_BumpMap",Vector2.one*.65f);mat.SetColor("_BaseColor",new Color(.84f,.86f,.87f));
            var r=mf.GetComponent<Renderer>();Undo.RecordObject(r,"Apply wall texture");r.sharedMaterial=mat;EditorUtility.SetDirty(mat);
        }
    }
    static void OfficeLightsR7()
    {
        var office=GameObject.Find("Pixel Packers - Logistics Office");if(!office)throw new Exception("Office missing.");
        var old=office.transform.Find("Office lighting R7");if(old)Undo.DestroyObjectImmediate(old.gameObject);
        var g=new GameObject("Office lighting R7");Undo.RegisterCreatedObjectUndo(g,"Office lighting");var root=g.transform;root.SetParent(office.transform,true);
        var roof=office.GetComponentsInChildren<Renderer>().First(r=>r.name=="Roof");var b=roof.bounds;
        var glow=GlowR7("Office ceiling diffuser R7",new Color(1,.97f,.91f));var housing=NewMaterial("Office light housing R7");housing.SetColor("_BaseColor",new Color(.2f,.23f,.25f));
        foreach(float x in new[]{b.center.x-1.45f,b.center.x+1.45f})
        {
            BoxR7(root,"Office mounted light housing",new Vector3(x,b.min.y-.055f,b.center.z),new Vector3(.42f,.10f,1.25f),housing);
            BoxR7(root,"Office luminous diffuser",new Vector3(x,b.min.y-.115f,b.center.z),new Vector3(.36f,.016f,1.15f),glow);
            SpotR7(root,"Office reading light",new Vector3(x,b.min.y-.16f,b.center.z),Vector3.down,5.5f,7.5f,130,true);
        }
        SpotR7(root,"Office desk and cabinet wash",new Vector3(b.center.x,b.min.y-.3f,b.center.z-1.5f),new Vector3(0,-.35f,1).normalized,6f,8f,120);
        var fill=new GameObject("Office soft fill",typeof(Light));fill.transform.SetParent(root,false);fill.transform.position=new Vector3(b.center.x,2.1f,b.center.z);var fl=fill.GetComponent<Light>();fl.type=LightType.Point;fl.intensity=2.5f;fl.range=7;fl.color=new Color(1,.96f,.89f);fl.shadows=LightShadows.None;
        var lamp=office.transform.Find("Office accessories R6/DeskLampR6");var desk=office.GetComponentsInChildren<Renderer>().First(r=>r.name=="Desk");
        if(lamp)
        {
            Undo.RecordObject(lamp,"Turn office lamp toward work surface");var before=BoundsOf(lamp.gameObject);lamp.rotation=Quaternion.Euler(0,180,0);var after=BoundsOf(lamp.gameObject);lamp.position+=new Vector3(before.center.x-after.center.x,before.min.y-after.min.y,before.center.z-after.center.z);
            foreach(var l in lamp.GetComponentsInChildren<Light>()){Undo.RecordObject(l,"Replace misplaced desk light");l.enabled=false;}
            var lb=BoundsOf(lamp.gameObject);var head=new Vector3(lb.center.x,lb.max.y-.045f,lb.center.z-.10f);SpotR7(root,"Desk work surface light",head,(desk.bounds.center-head).normalized,1.4f,2.3f,90);
            var lit=lamp.GetComponentsInChildren<Renderer>();foreach(var r in lit){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i]&&mats[i].name.Contains("Surface_1"))mats[i]=glow;Undo.RecordObject(r,"Illuminate desk lamp diffuser");r.sharedMaterials=mats;}
        }
    }
    static void MoveWarehousePropsR7(MeshFilter warehouse)
    {
        var site=GameObject.Find("LLASO_Facility");var old=site.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Downloaded decorations R6");
        if(old)foreach(Transform t in old)if(t.name!="BollardR6"){Undo.RecordObject(t.gameObject,"Move props out of glass facility");t.gameObject.SetActive(false);}
        var root=FreshR6(warehouse.transform,"Cargo warehouse safety details R7");root.SetParent(null,true);root.localScale=Vector3.one;root.rotation=Quaternion.identity;root.position=Vector3.zero;
        var b=warehouse.GetComponent<Renderer>().bounds;float z=b.min.z+.32f;
        PropR6("ExtinguisherR6",root,new Vector3(b.min.x+1.3f,b.min.y+.12f,z));
        PropR6("FireAlarmR6",root,new Vector3(b.min.x+1.3f,b.min.y+1.45f,z));
        PropR6("PowerBoxR6",root,new Vector3(b.max.x-1.5f,b.min.y+1.2f,z));
        PropR6("SecurityCameraR6",root,new Vector3(b.min.x+2.5f,b.min.y+2.6f,z),180);
        // Parent with world pose preserved: props stay at human scale despite the FBX's 150x transform.
        root.SetParent(warehouse.transform,true);
    }
    static void LaunchControlsR7()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsR6+"/LaunchButtonR6.prefab");if(!source)throw new Exception("Downloaded launch button prefab missing.");
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);try{PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);PrefabUtility.SaveAsPrefabAsset(instance,"Assets/Resources/PixelPackersMission/LaunchButtonR7.prefab");}finally{Object.DestroyImmediate(instance);}
        var old=GameObject.Find("Rocket launch controls R7");Vector3 pos;
        if(old){pos=old.transform.position;Undo.DestroyObjectImmediate(old);}else{var camera=Camera.main;if(!camera)throw new Exception("Player camera missing.");pos=camera.transform.position+Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized*2.2f;pos.y=camera.transform.position.y-1.6f;}
        Physics.SyncTransforms();var groundHits=Physics.RaycastAll(new Vector3(pos.x,Camera.main.transform.position.y+.1f,pos.z),Vector3.down,50).Where(h=>h.normal.y>.6f && !h.collider.transform.root.name.Contains("Player")).OrderByDescending(h=>h.point.y).ToArray();if(groundHits.Length>0)pos.y=groundHits[0].point.y;
        var root=new GameObject("Rocket launch controls R7").transform;root.position=pos;Undo.RegisterCreatedObjectUndo(root.gameObject,"Place visible launch controls");
        var dark=NewMaterial("Launch console graphite R7");dark.SetColor("_BaseColor",new Color(.11f,.14f,.17f));BoxR7(root,"Launch console stand",pos+Vector3.up*.5f,new Vector3(.38f,1,.38f),dark);
        var button=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/PixelPackersMission/LaunchButtonR7.prefab"));button.name="Industrial rocket launch button";button.transform.SetParent(root,false);button.transform.localPosition=Vector3.up;
        var label=new GameObject("LAUNCH ROCKET label",typeof(TextMesh));label.transform.SetParent(root,false);label.transform.localPosition=new Vector3(0,.83f,-.196f);label.transform.localRotation=Quaternion.identity;var text=label.GetComponent<TextMesh>();text.text="LAUNCH\nROCKET";text.fontSize=48;text.characterSize=.014f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.white;
    }
}



