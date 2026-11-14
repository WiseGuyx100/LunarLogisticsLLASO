using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using PixelPackersMission;
using Object=UnityEngine.Object;

internal static partial class FacilityRepair
{
    [Serializable] class SuitMap { public string model,material,texture; public float[] color; }
    [Serializable] class SuitMaps {public SuitMap[] surfaces;}
    static void DownLight(Transform root,Vector3 position,float intensity,float range,float angle)
    {
        var light=new GameObject("Downward warehouse light R5",typeof(Light)).GetComponent<Light>();light.transform.SetParent(root,false);
        light.transform.localPosition=position;light.transform.localRotation=Quaternion.Euler(90,0,0);
        light.type=LightType.Spot;light.spotAngle=angle;light.innerSpotAngle=angle*.65f;light.range=range;light.intensity=intensity;
        light.color=new Color(1,.95f,.86f);light.shadows=LightShadows.Soft;light.shadowBias=.04f;light.shadowNormalBias=.15f;
    }
    static void SetDaylight()
    {
        string path=Folder+"DaylightSkyR5.hdr";var imp=AssetImporter.GetAtPath(path) as TextureImporter;
        if(imp){imp.textureShape=TextureImporterShape.Texture2D;imp.sRGBTexture=false;imp.maxTextureSize=2048;imp.SaveAndReimport();}
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!tex)return;
        string mp=Assets+"DaylightSkyR5.mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(mp);
        if(!sky){sky=new Material(Shader.Find("Skybox/Panoramic"));AssetDatabase.CreateAsset(sky,mp);}
        sky.SetTexture("_MainTex",tex);sky.SetFloat("_Exposure",.8f);sky.SetFloat("_Rotation",120);EditorUtility.SetDirty(sky);
        RenderSettings.skybox=sky;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Skybox;RenderSettings.ambientIntensity=.65f;RenderSettings.reflectionIntensity=.65f;
        var sun=RenderSettings.sun;if(sun){Undo.RecordObject(sun,"Balance daylight");sun.intensity=1.1f;sun.color=new Color(1,.97f,.91f);sun.shadows=LightShadows.Soft;}
        DynamicGI.UpdateEnvironment();
        IlluminateWarehousePanels();
    }
    static void IlluminateWarehousePanels()
    {
        foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(!mf.sharedMesh||!AssetDatabase.GetAssetPath(mf.sharedMesh).Contains("Warehouse_Open"))continue;
            var renderer=mf.GetComponent<MeshRenderer>();if(!renderer)continue;
            var prior=mf.transform.Find("Window daylight R5");if(prior)Undo.DestroyObjectImmediate(prior.gameObject);
            var parent=new GameObject("Window daylight R5").transform;parent.SetParent(mf.transform,false);Undo.RegisterCreatedObjectUndo(parent.gameObject,"Light warehouse panels");
            var mats=renderer.sharedMaterials;var mesh=mf.sharedMesh;var vertices=mesh.vertices;int lamps=0;
            for(int sub=0;sub<Mathf.Min(mesh.subMeshCount,mats.Length);sub++)
            {
                if(!mats[sub]||(!mats[sub].name.ToLowerInvariant().Contains("emissive")&&!mats[sub].name.Contains("WarehouseLamp")&&!mats[sub].name.Contains("Warehouse panel glow")))continue;
                var glow=NewMaterial("Warehouse panel glow R5");glow.SetColor("_BaseColor",new Color(.85f,.91f,1));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.85f,.91f,1)*1.3f);mats[sub]=glow;
                var tris=mesh.GetTriangles(sub);var parents=new Dictionary<Vector3,Vector3>();
                Vector3 Key(Vector3 v)=>new Vector3(Mathf.Round(v.x*1000),Mathf.Round(v.y*1000),Mathf.Round(v.z*1000));
                Vector3 Find(Vector3 k){if(!parents.ContainsKey(k))parents[k]=k;if(parents[k]!=k)parents[k]=Find(parents[k]);return parents[k];}
                for(int i=0;i<tris.Length;i+=3){var a=Find(Key(vertices[tris[i]]));parents[Find(Key(vertices[tris[i+1]]))]=a;parents[Find(Key(vertices[tris[i+2]]))]=a;}
                foreach(var group in tris.Distinct().GroupBy(i=>Find(Key(vertices[i]))))
                {
                    var b=new Bounds(vertices[group.First()],Vector3.zero);foreach(int i in group)b.Encapsulate(vertices[i]);
                    if(lamps++>=32)break;
                    var light=new GameObject("High panel light",typeof(Light)).GetComponent<Light>();light.transform.SetParent(parent,false);
                    var world=mf.transform.TransformPoint(b.center);var direction=renderer.bounds.center-world;direction.y=-Mathf.Max(1,direction.magnitude*.35f);
                    light.transform.position=world+direction.normalized*.12f;light.transform.rotation=Quaternion.LookRotation(direction);
                    light.type=LightType.Spot;light.spotAngle=115;light.innerSpotAngle=80;light.intensity=1.2f;light.range=9;light.color=new Color(.88f,.94f,1);light.shadows=LightShadows.None;
                }
            }
            Undo.RecordObject(renderer,"Restore luminous panels");renderer.sharedMaterials=mats;
        }
    }
    static void AddSmallRockColliders(Scene scene)
    {
        foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>())
            if(t.name=="Original lunar rock"&&!t.GetComponentInChildren<Collider>())
            {
                var rr=t.GetComponentsInChildren<Renderer>();if(rr.Length==0)continue;var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);
                if(b.size.y<.12f||b.size.y>.65f)continue;
                foreach(var mf in t.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh){var c=Undo.AddComponent<MeshCollider>(mf.gameObject);c.sharedMesh=mf.sharedMesh;}
            }
    }
    static void RepairSuitBays()
    {
        var bays=GameObject.Find("Additional suit bays");if(!bays)return;
        var apollo=bays.transform.Find("Suit display 1");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Assets+"ArmstrongR5.fbx"))
        {
            var restored=Instance("ArmstrongR5",bays.transform,ModelMaterials("ArmstrongR5"));
            restored.transform.localRotation=Quaternion.Euler(0,180,0);
            var bounds=BoundsOf(restored);float scale=Mathf.Min(1.9f/bounds.size.y,1.45f/bounds.size.x,.85f/bounds.size.z);restored.transform.localScale*=scale;bounds=BoundsOf(restored);
            restored.transform.position+=bays.transform.TransformPoint(new Vector3(-4,.355f,-18.75f))-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            Undo.RegisterCreatedObjectUndo(restored,"Restore textured Apollo suit");if(apollo)Undo.DestroyObjectImmediate(apollo.gameObject);restored.name="Suit display 1";
        }
        var maps=JsonUtility.FromJson<SuitMaps>(File.ReadAllText(Assets+"suit_textures_r5.json"));
        foreach(var r in bays.GetComponentsInChildren<Renderer>())
        {
            Undo.RecordObject(r,"Restore embedded suit textures");
            r.sharedMaterials=r.sharedMaterials.Select(m=>
            {
                if(!m)return m;
                string key=m.name.Replace(" (Instance)","");
                var entry=maps.surfaces.FirstOrDefault(s=>s.material==key||key==s.texture+"_R2");
                if(entry==null)return m;
                var restored=NewMaterial(entry.texture);
                if(File.Exists(Assets+entry.texture+".png"))restored.SetTexture("_BaseMap",Texture(Assets+entry.texture+".png"));
                else if(entry.color!=null)restored.SetColor("_BaseColor",new Color(entry.color[0],entry.color[1],entry.color[2]));
                restored.SetFloat("_Smoothness",.18f);return restored;
            }).ToArray();
        }
        var panel=Finish("Fac_Grey");var old=bays.transform.Find("Glazed locking doors R5");if(old)Undo.DestroyObjectImmediate(old.gameObject);
        var root=new GameObject("Glazed locking doors R5").transform;root.SetParent(bays.transform,false);Undo.RegisterCreatedObjectUndo(root.gameObject,"Add glazed suit doors");
        var steel=Finish("Fac_DarkSteel");var glass=Finish("Fac_Glass");
        foreach(var r in bays.GetComponentsInChildren<MeshRenderer>())if(r.name=="Cabinet upright"||r.name=="Cabinet canopy") {Undo.RecordObject(r,"Texture bay casing");r.sharedMaterial=panel;}
        foreach(float x in new[]{-4f,-1.9f})
        {
            var hinge=new GameObject("Suit bay locking door R5").transform;hinge.SetParent(root,false);hinge.localPosition=new Vector3(x-.75f,1.4f,-19.25f);
            Support(hinge,new Vector3(.75f,0,0),new Vector3(1.48f,1.98f,.025f),glass);
            foreach(float dx in new[]{0f,1.5f})Support(hinge,new Vector3(dx,0,0),new Vector3(.045f,2.05f,.06f),steel);
            foreach(float dy in new[]{-1f,1f})Support(hinge,new Vector3(.75f,dy,0),new Vector3(1.5f,.045f,.06f),steel);
            var latch=GameObject.CreatePrimitive(PrimitiveType.Cylinder);latch.name="Round keyed latch";latch.transform.SetParent(hinge,false);latch.transform.localPosition=new Vector3(1.38f,0,-.065f);latch.transform.localRotation=Quaternion.Euler(90,0,0);latch.transform.localScale=new Vector3(.075f,.025f,.075f);latch.GetComponent<Renderer>().sharedMaterial=steel;
            var interaction=hinge.gameObject.AddComponent<OfficeInteraction>();interaction.hinged=true;interaction.angle=100;interaction.targetRenderer=latch.GetComponent<Renderer>();
            var col=hinge.gameObject.AddComponent<BoxCollider>();col.center=new Vector3(.75f,0,0);col.size=new Vector3(1.5f,2,.06f);
        }
        // Bay 2's existing front is facing the backing panel. Rotate its imported child once.
        var suit=bays.transform.Find("Suit display 2");
        if(suit&&!suit.Find("Front corrected R5")&&suit.childCount>0)
        {
            var child=suit.GetChild(0);var before=BoundsOf(suit.gameObject).center;Undo.RecordObject(child,"Face EMU toward aisle");child.localRotation=Quaternion.Euler(0,180,0)*child.localRotation;
            Undo.RecordObject(suit,"Recenter EMU");suit.position+=before-BoundsOf(suit.gameObject).center;
            new GameObject("Front corrected R5").transform.SetParent(suit,false);
        }
    }
    static void RouteRackCables(Transform root)
    {
        foreach(float z in new[]{28.4f,32.8f})DownLight(root,new Vector3(6.8f,3.1f,z),1.6f,6,110);
        var black=NewMaterial("Cable rubber R5");black.SetColor("_BaseColor",new Color(.025f,.032f,.038f));black.SetFloat("_Smoothness",.3f);
        var blue=NewMaterial("Data cable blue R5");blue.SetColor("_BaseColor",new Color(.035f,.19f,.38f));
        // Clearly visible bundles inside each tray, with curved drops into every rack.
        foreach(float z in new[]{29.3f,32f})
        {
            for(int line=0;line<4;line++)
                Cable(root,new[]{new Vector3(3.95f,2.7f,z+line*.055f-.08f),new Vector3(9.65f,2.7f,z+line*.055f-.08f)},line==0?blue:black,.025f);
            foreach(float x in new[]{4.45f,6.15f,7.85f,9.55f})for(int line=0;line<3;line++)
                Cable(root,new[]{new Vector3(x-.22f,2.7f,z+line*.045f),new Vector3(x,2.66f,z+line*.045f),new Vector3(x+.12f,2.48f,z+.23f+line*.045f),new Vector3(x+.12f,2.17f,z+.32f+line*.045f)},line==0?blue:black,.025f);
        }
    }
    static void Cable(Transform parent,Vector3[] points,Material material,float width)
    {
        var g=new GameObject("Connected power/data cable",typeof(LineRenderer));g.transform.SetParent(parent,false);
        var line=g.GetComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=points.Length;line.SetPositions(points);line.widthMultiplier=width;line.numCornerVertices=8;line.numCapVertices=6;line.sharedMaterial=material;line.generateLightingData=true;
    }
    static void ReplaceTrusses(Transform root,Mesh original,MeshCollider roof,Material steel)
    {
        var mats=ModelMaterials("HeavyTruss");
        foreach(var m in mats.Values){m.SetTexture("_BaseMap",null);m.SetColor("_BaseColor",new Color(.13f,.15f,.17f));m.SetFloat("_Metallic",.8f);m.SetFloat("_Smoothness",.32f);}
        var vertices=original.vertices;var triangles=original.triangles;var keep=new List<int>();
        // Remove only complete roof-level triangles; retain lower columns and trim.
        for(int i=0;i<triangles.Length;i+=3)
            if(Mathf.Min(vertices[triangles[i]].y,Mathf.Min(vertices[triangles[i+1]].y,vertices[triangles[i+2]].y))<8.8f)
                keep.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
        var mesh=Object.Instantiate(original);mesh.name="Facility lower steel R5";mesh.subMeshCount=1;mesh.SetTriangles(keep,0);mesh.RecalculateBounds();
        string path=Assets+"FacilityLowerSteelR5.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
        var frame=root.Find("Fac_DarkSteel");frame.GetComponent<MeshFilter>().sharedMesh=saved;frame.GetComponent<MeshRenderer>().sharedMaterials=new[]{steel};
        foreach(float z in new[]{4f,12f,20f,28f,36f,44f,50f})
        {
            float top=float.PositiveInfinity;
            foreach(float x in new[]{-17f,0,17f})
            {
                var ray=new Ray(root.TransformPoint(new Vector3(x,30,z)),-root.up);
                if(roof.Raycast(ray,out var hit,50))top=Mathf.Min(top,root.InverseTransformPoint(hit.point).y);
            }
            if(float.IsInfinity(top))throw new Exception("No roof over truss bay "+z);
            var holder=new GameObject("Roof truss bay R5").transform;holder.SetParent(root,false);holder.localPosition=new Vector3(0,0,z);holder.localRotation=Quaternion.Euler(0,90,0);
            PlaceDetail("HeavyTruss",holder,mats,new Vector3(0,top-.8f,0),35.2f);
            // Two roof-bearing plates and positive connections to the end columns.
            foreach(float x in new[]{-17.6f,17.6f})Support(root,new Vector3(x,top-1.52f,z),new Vector3(.55f,.12f,.5f),steel);
        }
    }
    [MenuItem("Tools/Pixel Packers/Install Office Interactions (R5)")]
    public static void InstallOfficeInteractions()
    {
        var office=GameObject.Find("Pixel Packers - Logistics Office");if(!office){Debug.LogWarning("Existing logistics office not found; office interactions skipped.");return;}
        var all=office.GetComponentsInChildren<Transform>(true);
        Transform Find(string n)=>all.FirstOrDefault(t=>t.name==n);
        var chair=Find("Chair_Body");var monitor=Find("Computer");
        if(chair&&monitor&&!chair.GetComponent<OfficeInteraction>())
        {
            var seat=new GameObject("Seated eye position R5").transform;Undo.RegisterCreatedObjectUndo(seat.gameObject,"Add office seat");seat.SetParent(office.transform,false);
            var b=chair.GetComponent<Renderer>().bounds;seat.position=new Vector3(b.center.x,b.min.y+.68f,b.center.z);
            var direction=monitor.GetComponent<Renderer>().bounds.center-b.center;direction.y=0;seat.rotation=Quaternion.LookRotation(direction);
            var c=Undo.AddComponent<OfficeInteraction>(chair.gameObject);c.chair=true;c.seat=seat;c.targetRenderer=chair.GetComponent<Renderer>();
        }
        foreach(var pair in new[]{new[]{"Drawer","Drawer_Handle"},new[]{"Drawer2","Drawer_Handle1"},new[]{"Wardrobe_Door","Drawer_Handle2"}})
        {
            var door=Find(pair[0]);var handle=Find(pair[1]);if(!door||door.GetComponentInParent<OfficeInteraction>())continue;
            var instance=PrefabUtility.GetOutermostPrefabInstanceRoot(door.gameObject);
            if(instance){Undo.RegisterFullObjectHierarchyUndo(instance,"Prepare animated office parts");PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.UserAction);}
            var b=door.GetComponent<Renderer>().bounds;var holder=new GameObject(pair[0]+" moving R5").transform;holder.SetParent(office.transform,false);
            holder.position=b.center;holder.rotation=office.transform.rotation;Undo.RegisterCreatedObjectUndo(holder.gameObject,"Create cabinet hinge");
            var c=Undo.AddComponent<OfficeInteraction>(holder.gameObject);c.targetRenderer=door.GetComponent<Renderer>();
            if(pair[0]=="Wardrobe_Door")
            {
                c.hinged=true;
                // Derive the hinge edge opposite the handle, in world coordinates.
                var across=handle?b.center-handle.GetComponent<Renderer>().bounds.center:office.transform.right;across.y=0;across.Normalize();
                float extent=Mathf.Abs(across.x)*b.extents.x+Mathf.Abs(across.z)*b.extents.z;holder.position=b.center+across*extent;c.angle=95;
            }
            else
            {
                var outward=handle?handle.GetComponent<Renderer>().bounds.center-b.center:-office.transform.forward;outward.y=0;
                c.travel=holder.parent.InverseTransformVector(outward.normalized*.38f);
            }
            Undo.SetTransformParent(door,holder,"Attach moving door");if(handle)Undo.SetTransformParent(handle,holder,"Attach moving handle");
        }
        EditorSceneManager.MarkSceneDirty(office.scene);
    }
    [MenuItem("Tools/Pixel Packers/Repair Underground Layout (R5)")]
    public static void RepairUnderground()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="Lunar Service Level"){EditorUtility.DisplayDialog("Underground layout","Open Lunar Service Level outside Play Mode first.","OK");return;}
        var room=GameObject.Find("Hygiene and fitness");if(!room)throw new Exception("Hygiene and fitness room not found.");
        void Move(string n,Vector3 pos,Vector3? size=null)
        {var t=room.transform.Find(n);if(!t)return;Undo.RecordObject(t,"Rearrange hygiene room");t.localPosition=pos;if(size.HasValue)t.localScale=size.Value;}
        // Recess the privacy screen; leave the entrance and central aisle clear.
        Move("Hygiene partition",new Vector3(8.1f,1.4f,7.7f),new Vector3(5.55f,2.8f,.14f));
        Move("Wash area backing wall",new Vector3(10.62f,1.68f,9.65f),new Vector3(.14f,3.1f,2.5f));
        Move("Fitness mat",new Vector3(8.3f,.032f,5.1f),new Vector3(2.4f,.024f,2));
        var bike=room.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="stationary_exercise_bike");
        if(bike){Undo.RecordObject(bike,"Move exercise bike");bike.localPosition=new Vector3(8.3f,.02f,5.1f);}
        // Bring the imported wash fixtures against their backing wall instead of floating in the room.
        var wash=room.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="shower_room_-_assets");
        if(wash)
        {
            var rr=wash.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
            if(rr.Length>0){var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);Undo.RecordObject(wash,"Mount wash fixtures");wash.position+=new Vector3(10.54f-b.max.x,.025f-b.min.y,0);}
        }
        var medical=GameObject.Find("Medical and plant research");
        if(medical){var partition=medical.transform.Find("Clinical partition");if(partition){Undo.RecordObject(partition,"Clear medical aisle");partition.localScale=new Vector3(3.4f,2.5f,.14f);}}
        EditorSceneManager.MarkSceneDirty(scene);EditorUtility.DisplayDialog("Underground layout updated","Moved hygiene partition back, aligned wash fixtures to their backing wall, widened fitness clearance and shortened the medical screen. Save Lunar Service Level.","OK");
    }
}
