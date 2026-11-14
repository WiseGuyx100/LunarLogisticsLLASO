using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

// Repairs visuals only. Existing facility meshes/colliders and cargo are retained.
internal static class FacilityRepair
{
    const string Folder="Assets/PixelPackersOffice/Facility/";
    const string Assets=Folder+"RepairAssets/";
    const string Version="2026.11.14-r4";
    [Serializable] class Surface { public string model,material; public bool texture,alpha,cutout; public float[] color; }
    [Serializable] class Manifest { public Surface[] surfaces; }
    static Manifest manifest;
    static List<string> report;

    [MenuItem("Tools/Pixel Packers/Repair Facility and Trees (R4)")]
    public static void RepairAll()=>Run(true,true);
    public static void RepairFacility()=>Run(true,false);
    public static void RepairTrees()=>Run(false,true);
    static void Run(bool facility,bool trees)
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="GameTest")
        {EditorUtility.DisplayDialog("Repair GameTest","Open GameTest and exit Play Mode first.","OK");return;}
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Repair facility and trees R2");
        report=new List<string>{"Visual repair "+Version};
        try
        {
            if(!Shader.Find("Universal Render Pipeline/Lit"))throw new Exception("URP/Lit shader is unavailable. No scene changes were applied.");
            manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Assets+"surfaces.json"));
            // Earlier packages omitted importer settings. Unity interpreted some images
            // as cubemaps, which cannot be loaded as Texture2D even after reimporting.
            foreach(var path in Directory.GetFiles(Folder,"*",SearchOption.AllDirectories)
                .Where(p=>new[]{".png",".jpg",".exr"}.Contains(Path.GetExtension(p).ToLowerInvariant())))
                Texture(path.Replace('\\','/'),path.Contains("_nor_gl"));
            if(facility)FixFacility(scene);
            if(facility)PrepareLaunchFlame();
            if(trees)FixTrees(scene);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undo);
            report.Add("Cargo, original colliders, and building placement preserved. Save GameTest to keep the repair.");
            Debug.Log(string.Join("\n",report));
            EditorUtility.DisplayDialog("Repair complete",string.Join("\n",report),"OK");
        }
        catch(Exception e)
        {
            Undo.RevertAllDownToGroup(undo);
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Repair stopped",e.Message+"\nScene changes from this run were undone. See Console for details.","OK");
        }
    }
    static Material NewMaterial(string key)
    {
        string path=Assets+key+"_R2.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        else Undo.RecordObject(m,"Repair surface material");
        m.shader=Shader.Find("Universal Render Pipeline/Lit");m.shaderKeywords=Array.Empty<string>();
        m.SetFloat("_Surface",0);m.SetFloat("_Blend",0);m.SetFloat("_ZWrite",1);
        m.SetFloat("_SrcBlend",1);m.SetFloat("_DstBlend",0);
        m.SetFloat("_SrcBlendAlpha",1);m.SetFloat("_DstBlendAlpha",0);
        m.SetFloat("_AlphaClip",0);m.SetFloat("_Cull",0);m.SetFloat("_Metallic",0);
        m.SetFloat("_Smoothness",.25f);m.SetColor("_BaseColor",Color.white);
        m.SetTexture("_BaseMap",null);m.SetTexture("_BumpMap",null);
        m.SetTextureScale("_BaseMap",Vector2.one);m.SetTextureOffset("_BaseMap",Vector2.zero);
        m.SetOverrideTag("RenderType","Opaque");m.renderQueue=2000;
        m.SetShaderPassEnabled("ShadowCaster",true);m.enableInstancing=true;
        EditorUtility.SetDirty(m);return m;
    }
    [MenuItem("Tools/Pixel Packers/Upgrade Moon Rooms (R4)")]
    public static void UpgradeMoon()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="Moon Test")
        {EditorUtility.DisplayDialog("Moon rooms","Open Moon Test and exit Play Mode first.","OK");return;}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
        try
        {
            manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Assets+"surfaces.json"));
            GameTestOffice.LunarRooms();
            var power=GameObject.Find("Equipment and power annex");
            if(!power)throw new Exception("Equipment and power annex not found.");
            var previous=power.transform.Find("Downloaded equipment R4");
            var root=new GameObject("Downloaded equipment R4");root.transform.SetParent(power.transform,false);Undo.RegisterCreatedObjectUndo(root,"Detail lunar rooms");
            var tray=ModelMaterials("CableTray");var cable=ModelMaterials("Cables");
            PlaceDetail("CableTray",root.transform,tray,new Vector3(6.8f,2.6f,29.3f),5.8f);
            PlaceDetail("CableTray",root.transform,tray,new Vector3(6.8f,2.6f,32f),5.8f);
            // Lay the trays across the rack rows, not through them.
            foreach(Transform child in root.transform)child.localRotation=Quaternion.Euler(0,90,0)*child.localRotation;
            var dark=NewMaterial("Room mounting metal");dark.SetColor("_BaseColor",new Color(.16f,.2f,.23f));
            foreach(float z in new[]{29.3f,32f})foreach(float x in new[]{4.4f,9.1f})
                Support(root.transform,new Vector3(x,2.96f,z),new Vector3(.035f,.7f,.035f),dark);
            PlaceProp("Cables",root.transform,cable,new Vector3(8.8f,.18f,32.2f),1.1f);
            PlaceProp("ToolChest",root.transform,ModelMaterials("ToolChest"),new Vector3(-3.5f,.14f,31.8f),1.05f);
            PlaceProp("Compressor",root.transform,ModelMaterials("Compressor"),new Vector3(-.6f,.14f,31.8f),.9f);
            PlaceProp("Toolbox",root.transform,ModelMaterials("Toolbox"),new Vector3(-7.4f,1.2f,31f),.3f);
            if(previous)Undo.DestroyObjectImmediate(previous.gameObject);
            var oldTray=power.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Overhead cable tray").ToArray();
            foreach(var t in oldTray){Undo.RecordObject(t.gameObject,"Replace plain trays");t.gameObject.SetActive(false);}
            var wallMat=Finish("Fac_Grey");int walls=0;
            foreach(string name in new[]{"Equipment and power annex","01 Arrival and suit bay","02 Operations and laboratory","03 Internal logistics","04 Crew and service access"})
            {
                var room=GameObject.Find(name);if(!room)continue;
                foreach(var r in room.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mf=r.GetComponent<MeshFilter>();
                    // Only room-shell primitives; never paint imported equipment or glass.
                    if(!mf||!mf.sharedMesh||mf.sharedMesh.name!="Cube"||!r.name.ToLowerInvariant().Contains("wall"))continue;
                    Undo.RecordObject(r,"Texture lunar wall");r.sharedMaterial=wallMat;walls++;
                }
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(group);
            Selection.activeGameObject=power;
            EditorUtility.DisplayDialog("Moon rooms updated","Added downloaded cable trays, cables, tool chest, compressor and toolbox to equipment and power; textured "+walls+" room wall sections. Existing cargo retained. Save Moon Test.","OK");
        }
        catch(Exception e){Undo.RevertAllDownToGroup(group);Debug.LogException(e);EditorUtility.DisplayDialog("Moon upgrade stopped",e.Message,"OK");}
    }
    static void PlaceProp(string name,Transform root,Dictionary<string,Material> mats,Vector3 floor,float height)
    {
        var g=Instance(name,root,mats);var b=BoundsOf(g);g.transform.localScale*=height/Mathf.Max(.01f,b.size.y);b=BoundsOf(g);
        g.transform.position+=root.TransformPoint(floor)-new Vector3(b.center.x,b.min.y,b.center.z);
    }
    [MenuItem("Tools/Pixel Packers/Connect Downloaded Launch Flame (R4)")]
    public static void PrepareLaunchFlame()
    {
        const string src="Assets/Solodream/VFX_FlamePack/Prefabs/Flame_02.prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(src);
        if(!prefab){Debug.LogWarning("Flame_02.prefab is missing. Continuous engine cores will still run; import the downloaded Flame Pack to add its VFX.");return;}
        const string folder="Assets/Resources/PixelPackersMission";
        if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources","PixelPackersMission");
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try{PrefabUtility.SaveAsPrefabAsset(instance,folder+"/LaunchFlame.prefab");}
        finally{Object.DestroyImmediate(instance);}
        Debug.Log("Downloaded Flame_02 connected to launch exhaust. Re-enter Play Mode to test.");
    }
    static Texture2D Texture(string path,bool normal=false)
    {
        if(!File.Exists(path))throw new Exception("Required texture missing: "+path+". Import all RepairAssets files from the package.");
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
        var imp=AssetImporter.GetAtPath(path) as TextureImporter;
        if(imp==null)throw new Exception("Unity did not recognize this texture: "+path);
        bool change=imp.textureShape!=TextureImporterShape.Texture2D||imp.textureType!=(normal?TextureImporterType.NormalMap:TextureImporterType.Default)||(!normal&&!imp.alphaIsTransparency)||imp.sRGBTexture==normal;
        if(change)
        {
            imp.textureShape=TextureImporterShape.Texture2D;
            imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            imp.sRGBTexture=!normal;imp.alphaIsTransparency=!normal;
            imp.alphaSource=TextureImporterAlphaSource.FromInput;
            imp.mipmapEnabled=true;
            imp.SaveAndReimport();
        }
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!tex)throw new Exception("Texture failed to import: "+path);
        return tex;
    }
    static Material Finish(string name)
    {
        var m=NewMaterial(name);
        Color c=new Color(.65f,.69f,.72f);
        if(name=="Fac_White")c=new Color(.72f,.75f,.78f);
        if(name=="Fac_DarkSteel")c=new Color(.17f,.20f,.23f);
        if(name=="Fac_DarkBlue"||name=="Fac_PathMark")c=new Color(.055f,.14f,.23f);
        if(name=="Fac_Purple"||name=="Fac_Green")c=new Color(.37f,.42f,.45f);
        m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",.45f);
        if(name=="Fac_Glass")
        {
            m.SetColor("_BaseColor",new Color(.70f,.84f,.88f,.13f));m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.92f);
            m.SetFloat("_Surface",1);m.SetFloat("_ZWrite",0);m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",10);
            m.SetFloat("_DstBlendAlpha",10);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;m.SetShaderPassEnabled("ShadowCaster",false);
        }
        if(name=="Fac_Floor"||name=="Fac_Roof")
        {
            string tex=name=="Fac_Floor"?"concrete_floor_painted":"corrugated_iron";
            m.SetTexture("_BaseMap",Texture(Folder+tex+"_diff_2k.jpg"));
            m.SetTexture("_BumpMap",Texture(Folder+tex+"_nor_gl_2k.exr",true));
            m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.25f);
            m.SetColor("_BaseColor",new Color(.82f,.84f,.85f,1));
            m.SetFloat("_Metallic",name=="Fac_Roof"?.65f:0);
            m.SetTextureScale("_BaseMap",Vector2.one*.4f);
        }
        if(name=="Fac_PathMark")m.SetFloat("_Metallic",0);
        if(name=="Fac_Purple"||name=="Fac_Grey"||name=="Fac_Green")
        {
            m.SetTexture("_BaseMap",Texture(Folder+"blue_metal_plate_diff_2k.jpg"));
            m.SetTexture("_BumpMap",Texture(Folder+"blue_metal_plate_nor_gl_2k.exr",true));
            m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.18f);
            m.SetTextureScale("_BaseMap",Vector2.one*.35f);
            m.SetColor("_BaseColor",new Color(.72f,.75f,.77f));
        }
        return m;
    }
    static GameObject Model(string name)
    {
        var path=Assets+name+".fbx";
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!model)throw new Exception("Model missing: "+path);
        return model;
    }
    static Dictionary<string,Material> ModelMaterials(string name)
    {
        var result=new Dictionary<string,Material>();
        foreach(var s in manifest.surfaces.Where(x=>x.model==name))
        {
            var m=NewMaterial(s.material);
            if(s.texture)m.SetTexture("_BaseMap",Texture(Assets+s.material+"_Base.png"));
            else if(s.color!=null&&s.color.Length>=3)m.SetColor("_BaseColor",new Color(s.color[0],s.color[1],s.color[2]));
            if(s.cutout)
            {
                m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.35f);m.EnableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=2450;
            }
            result.Add(s.material,m);
        }
        if(result.Count==0)throw new Exception("No material definitions for "+name);
        return result;
    }
    static GameObject Instance(string name,Transform parent,Dictionary<string,Material> mats)
    {
        var holder=new GameObject(name);holder.transform.SetParent(parent,false);
        var g=(GameObject)PrefabUtility.InstantiatePrefab(Model(name),parent.gameObject.scene);
        g.transform.SetParent(holder.transform,false); // Keep imported FBX rotation and scale.
        foreach(var r in g.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.sharedMaterials=r.sharedMaterials.Select(m=>
            {
                string key=m?m.name.Replace(" (Instance)",""):"";
                if(!mats.TryGetValue(key,out var material))throw new Exception("Unknown material slot on "+name+": "+key);
                return material;
            }).ToArray();
        }
        foreach(var c in g.GetComponentsInChildren<Collider>(true))c.enabled=false;
        return holder;
    }
    static Bounds BoundsOf(GameObject g)
    {
        var rr=g.GetComponentsInChildren<Renderer>(true);
        if(rr.Length==0)throw new Exception("No renderable mesh: "+g.name);
        var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    static void FixTrees(Scene scene)
    {
        var original=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Trees"||x.name=="Trees (original collision)");
        if(!original)throw new Exception("Original Trees group not found. Keep it in the Hierarchy so positions can be recovered.");
        var rr=original.GetComponentsInChildren<MeshRenderer>(true);
        var groups=rr.GroupBy(r=>
        {
            var t=r.transform;
            // TreesNorth/East/West are rows, not individual trees. Stop BELOW
            // a row, preserving each prefab's trunk and foliage as one tree.
            while(t.parent&&t.parent!=original.transform&&
                t.parent.name!="TreesNorth"&&t.parent.name!="TreesEast"&&t.parent.name!="TreesWest")t=t.parent;
            return t;
        }).Select(g=>{var b=g.First().bounds;foreach(var r in g.Skip(1))b.Encapsulate(r.bounds);return b;})
            .Where(b=>b.size.y>.5f).ToArray();
        if(groups.Length<4)throw new Exception("Only "+groups.Length+" tree locations found. Tree rows could not be separated; existing trees were retained.");
        string[] names={"PineTree","NorwaySpruce","FirTree"};
        var mats=names.ToDictionary(n=>n,n=>{Model(n);return ModelMaterials(n);});
        var root=new GameObject("Realistic trees R2");SceneManager.MoveGameObjectToScene(root,scene);Undo.RegisterCreatedObjectUndo(root,"Repair trees");
        float height=Mathf.Clamp(groups.Select(b=>b.size.y).OrderBy(h=>h).ElementAt(groups.Length/2),6,16);
        for(int i=0;i<groups.Length;i++)
        {
            string name=names[i%names.Length];var tree=Instance(name,root.transform,mats[name]);
            tree.transform.localRotation=Quaternion.Euler(0,i*137.508f,0);
            var b=BoundsOf(tree);
            if(b.size.y<.1f||b.size.y<Mathf.Max(b.size.x,b.size.z))throw new Exception("Tree is not upright: "+name);
            tree.transform.localScale*=height/b.size.y;b=BoundsOf(tree);
            tree.transform.position+=new Vector3(groups[i].center.x-b.center.x,groups[i].min.y-b.min.y,groups[i].center.z-b.center.z);
        }
        foreach(var old in scene.GetRootGameObjects().Where(x=>x!=root&&(x.name=="Realistic spruce trees"||x.name=="Realistic trees R2")).ToArray())Undo.DestroyObjectImmediate(old);
        foreach(var r in rr){Undo.RecordObject(r,"Hide original tree visuals");r.enabled=false;}
        Undo.RecordObject(original,"Name original collision group");original.name="Trees (original collision)";
        report.Add(groups.Length+" upright, textured trees; pine, spruce and fir at "+height.ToString("F1")+" m height.");
    }
    static void FixFacility(Scene scene)
    {
        var site=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="LLASO_Facility");
        if(!site)throw new Exception("LLASO_Facility root not found.");
        var original=site.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(x=>x.sharedMesh&&AssetDatabase.GetAssetPath(x.sharedMesh).EndsWith("/LLASO_Facility.obj"));
        if(!original)throw new Exception("Original LLASO_Facility mesh not found.");
        string[] surfaces={"Fac_Floor","Fac_Grey","Fac_PathMark","Fac_DarkBlue","Fac_LightBlue","Fac_Green","Fac_White","Fac_Roof","Fac_Glass","Fac_Purple","Fac_DarkSteel"};
        var meshes=surfaces.ToDictionary(n=>n,n=>
        {
            var g=AssetDatabase.LoadAssetAtPath<GameObject>(Assets+n+".obj");var mf=g?g.GetComponentInChildren<MeshFilter>():null;
            if(!mf||!mf.sharedMesh)throw new Exception("Missing facility surface: "+n);
            return mf.sharedMesh;
        });
        var mats=surfaces.ToDictionary(n=>n,Finish);
        var lightMats=ModelMaterials("CeilingLight");var ductMats=ModelMaterials("RoundDuct");
        var root=new GameObject("Facility repaired R2");root.transform.SetParent(original.transform,false);Undo.RegisterCreatedObjectUndo(root,"Repair facility");
        foreach(var name in surfaces)
        {
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root.transform,false);
            g.GetComponent<MeshFilter>().sharedMesh=meshes[name];
            var r=g.GetComponent<MeshRenderer>();r.sharedMaterials=Enumerable.Repeat(mats[name],meshes[name].subMeshCount).ToArray();
            if(name=="Fac_Glass")r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var roofProbe=root.transform.Find("Fac_Roof").gameObject.AddComponent<MeshCollider>();
        roofProbe.sharedMesh=meshes["Fac_Roof"];
        Physics.SyncTransforms();
        void Hang(Vector3 bottom)
        {
            var ray=new Ray(root.transform.TransformPoint(bottom+Vector3.up*30),-root.transform.up);
            if(!roofProbe.Raycast(ray,out var hit,60))throw new Exception("No roof above fixture support at "+bottom);
            float top=root.transform.InverseTransformPoint(hit.point).y;
            if(top<=bottom.y)throw new Exception("Fixture is above its roof mounting point.");
            Support(root.transform,new Vector3(bottom.x,(bottom.y+top)*.5f,bottom.z),new Vector3(.035f,top-bottom.y,.035f),mats["Fac_DarkSteel"]);
            Support(root.transform,new Vector3(bottom.x,top-.05f,bottom.z),new Vector3(.18f,.08f,.18f),mats["Fac_DarkSteel"]);
        }
        // Roof-anchored fixtures and ducts that join end-to-end.
        foreach(float x in new[]{-8f,8f})foreach(float z in new[]{10f,22f,34f,46f})
        {
            PlaceDetail("CeilingLight",root.transform,lightMats,new Vector3(x,8.9f,z),2.4f);
            foreach(float dz in new[]{-.8f,.8f})Hang(new Vector3(x,8.98f,z+dz));
        }
        foreach(float z in new[]{7f,15f,23f,31f,39f,47f})
        {
            PlaceDetail("RoundDuct",root.transform,ductMats,new Vector3(13f,8.45f,z),8f);
            foreach(float dz in new[]{-3.7f,3.7f})
            {
                Support(root.transform,new Vector3(13,8.16f,z+dz),new Vector3(.8f,.06f,.1f),mats["Fac_DarkSteel"]);
                foreach(float dx in new[]{-.34f,.34f})Hang(new Vector3(13+dx,8.19f,z+dz));
            }
        }
        Object.DestroyImmediate(roofProbe);
        foreach(var t in original.transform.Cast<Transform>().Where(t=>t!=root.transform&&(t.name=="Detailed facility finish"||t.name=="Facility repaired R2")).ToArray())Undo.DestroyObjectImmediate(t.gameObject);
        var old=original.GetComponent<Renderer>();Undo.RecordObject(old,"Hide original facility visuals");old.enabled=false;
        report.Add("Facility: textured wall panels, 8 mounted lights, connected round duct with support brackets. Existing roof trusses retained pending downloaded replacement.");
    }
    static void PlaceDetail(string name,Transform parent,Dictionary<string,Material> mats,Vector3 center,float length)
    {
        var g=Instance(name,parent,mats);
        // Fit in parent coordinates, independent of the facility's world rotation.
        Bounds LocalBounds()
        {
            bool first=true;var b=new Bounds();
            foreach(var mf in g.GetComponentsInChildren<MeshFilter>())
            for(int i=0;i<8;i++)
            {
                var v=mf.sharedMesh.bounds.center+Vector3.Scale(mf.sharedMesh.bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                v=parent.InverseTransformPoint(mf.transform.TransformPoint(v));
                if(first){b=new Bounds(v,Vector3.zero);first=false;}else b.Encapsulate(v);
            }
            return b;
        }
        var local=LocalBounds();
        if(local.size.x>local.size.z)g.transform.localRotation=Quaternion.Euler(0,90,0);
        local=LocalBounds();
        float width=name=="RoundDuct"?.52f:.38f,height=name=="RoundDuct"?.52f:.16f;
        g.transform.localScale=new Vector3(width/Mathf.Max(.001f,local.size.x),height/Mathf.Max(.001f,local.size.y),length/Mathf.Max(.001f,local.size.z));
        // The exported fixtures run along Z; scale in parent axes via an extra holder
        // when a source has needed a quarter turn.
        if(g.transform.localRotation!=Quaternion.identity)
        {var s=g.transform.localScale;g.transform.localScale=new Vector3(s.z,s.y,s.x);}
        var b=BoundsOf(g);
        g.transform.position+=parent.TransformPoint(center)-b.center;
    }
    static void Support(Transform parent,Vector3 center,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Fixture mounting bracket";
        g.transform.SetParent(parent,false);g.transform.localPosition=center;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(g.GetComponent<Collider>());
    }
}
