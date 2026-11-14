using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class GameTestOffice
{
    const string Office = "Pixel Packers - Logistics Office";

    const string Props = "Assets/LunarOutpost/Models/";
    const string Materials = "Assets/PixelPackersOffice/Materials";

    [MenuItem("Tools/Pixel Packers/Add Office to GameTest")]
    static void Install()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="GameTest")
        {
            Debug.LogWarning("Open GameTest and exit Play Mode first.");return;
        }
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PixelPackersOffice/source/Room_Environment.fbx")
            ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PixelPackersOffice/ModernOffice/source/Room_Environment.fbx");
        if(!asset){Debug.LogWarning("Room_Environment.fbx was not found in PixelPackersOffice/source.");return;}
        var facility=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="LLASO_Facility");
        if(!facility){Debug.LogWarning("Cannot find LLASO_Facility in GameTest.");return;}
        var renderers=facility.GetComponentsInChildren<Renderer>();
        if(renderers.Length==0)return;
        var bounds=renderers[0].bounds;
        foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==Office);
        var root=new GameObject(Office);
        SceneManager.MoveGameObjectToScene(root,scene);
        Undo.RegisterCreatedObjectUndo(root,"Place downloaded office");
        var site=new Vector3(bounds.center.x,bounds.min.y,bounds.max.z+8.0f);
        var terrain=Terrain.activeTerrain;
        if(terrain&&terrain.gameObject.scene==scene)site.y=terrain.SampleHeight(site)+terrain.transform.position.y;
        root.transform.position=site;
        if(!Fit(root.transform,AssetDatabase.GetAssetPath(asset),"Downloaded office",new Vector3(0,1.6f,0),new Vector3(0,3.2f,0),180))
        {Undo.DestroyObjectImmediate(root);Debug.LogWarning("The selected asset contains no renderable office model.");return;}
        ApplyOfficeMaterials(root);
        CompleteOfficeShell(root.transform);
        foreach(var mesh in root.GetComponentsInChildren<MeshFilter>())
        {
            if(!mesh.sharedMesh)continue;
            var collider=mesh.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh=mesh.sharedMesh;
        }
        if(old)Undo.DestroyObjectImmediate(old);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject=root;
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.FrameSelected();
    }

    const string Detail="Assets/PixelPackersOffice/Facility/";

    [MenuItem("Tools/Pixel Packers/Add Roof and Side to Existing Office")]
    static void FinishOffice()
    {
        var selected=Selection.activeTransform;
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().name!="GameTest")return;
        var office=SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g=>g.name==Office);
        var root=selected&&selected.gameObject.scene.IsValid()&&selected.GetComponentsInChildren<MeshFilter>().Any(m=>m.name=="Floor"&&AssetDatabase.GetAssetPath(m.sharedMesh).EndsWith("Room_Environment.fbx"))?selected:office?office.transform:null;
        if(!root){Debug.LogWarning("Select the office in the Hierarchy or use Add Office to GameTest.");return;}
        ApplyOfficeMaterials(root.gameObject);
        CompleteOfficeShell(root);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
    }

    static void CompleteOfficeShell(Transform root)
    {
        var floor=root.GetComponentsInChildren<MeshFilter>().FirstOrDefault(m=>m.name=="Floor"&&m.sharedMesh);
        var wall=root.GetComponentsInChildren<MeshFilter>().FirstOrDefault(m=>m.name=="Wall"&&m.sharedMesh);
        if(!floor||!wall||root.Find("Office roof and side"))return;
        var b=new Bounds();
        foreach(var m in new[]{floor,wall})
        {
            var box=m.sharedMesh.bounds;
            for(int i=0;i<8;i++)
            {
                var v=box.center+Vector3.Scale(box.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var p=root.InverseTransformPoint(m.transform.TransformPoint(v));
                if(m==floor&&i==0)b=new Bounds(p,Vector3.zero);else b.Encapsulate(p);
            }
        }
        var shell=new GameObject("Office roof and side").transform;shell.SetParent(root,false);
        var mat=wall.GetComponent<Renderer>().sharedMaterial;
        var roof=BoxLocal("Roof",new Vector3(b.center.x,b.max.y+.095f,b.center.z),new Vector3(b.size.x+.18f,.19f,b.size.z+.18f));
        var model=root.GetComponentsInChildren<Transform>().FirstOrDefault(t=>AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)).EndsWith("Room_Environment.fbx"));
        var missingSide=model?root.InverseTransformDirection(model.TransformDirection(Vector3.left)):Vector3.left;
        // The supplied FBX has two adjoining walls; close its other X side and leave the front open.
        var d=missingSide.normalized;
        bool x=Mathf.Abs(d.x)>Mathf.Abs(d.z);
        var pos=b.center+(x?Vector3.right*Mathf.Sign(d.x)*(b.extents.x-.075f):Vector3.forward*Mathf.Sign(d.z)*(b.extents.z-.075f));
        BoxLocal("Side wall",pos,x?new Vector3(.15f,b.size.y,b.size.z):new Vector3(b.size.x,b.size.y,.15f));
        Undo.RegisterCreatedObjectUndo(shell.gameObject,"Add office shell");
        GameObject BoxLocal(string name,Vector3 pos,Vector3 size)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;
            g.transform.SetParent(shell,false);g.transform.localPosition=pos;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=mat;return g;
        }
    }

    [MenuItem("Tools/Pixel Packers/Upgrade LLASO Facility Finishes")]
    static void DetailFacility()=>FacilityRepair.RepairFacility();

    [MenuItem("Tools/Pixel Packers/Add Selected Facility Detail Asset")]
    static void AddFacilityAsset()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="GameTest")return;
        var facility=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="LLASO_Facility");
        var asset=Selection.activeObject as GameObject;
        if(!facility||!asset||!EditorUtility.IsPersistent(asset))
        {Debug.LogWarning("Select the imported light or duct model in the Project panel.");return;}
        var g=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);
        g.transform.SetParent(facility.transform,false);
        g.transform.localPosition=new Vector3(0,8.9f,18);
        foreach(var c in g.GetComponentsInChildren<Collider>(true))c.enabled=false;
        Undo.RegisterCreatedObjectUndo(g,"Add facility detail");
        Selection.activeGameObject=g;
        EditorSceneManager.MarkSceneDirty(scene);
    }
    static Material Finish(Material src)
    {
        if(!src)return src;
        string n=src.name;
        string path=Detail+n+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        var color=src.color;
        m.SetColor("_BaseColor",color);
        m.SetFloat("_Metallic",n=="Fac_Floor"?0:.48f);
        m.SetFloat("_Smoothness",n=="Fac_Floor"?.23f:.48f);
        string texture=n=="Fac_Floor"?"concrete_floor_painted":n=="Fac_Roof"?"corrugated_iron":"blue_metal_plate";
        if(n=="Fac_Glass")
        {
            m.SetColor("_BaseColor",new Color(.79f,.88f,.91f,.15f));m.SetFloat("_Metallic",0);
            m.SetFloat("_Smoothness",.91f);m.SetFloat("_Surface",1);m.SetFloat("_ZWrite",0);m.SetInt("_SrcBlend",5);m.SetInt("_DstBlend",10);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;
        }
        else if(n=="Fac_PathMark")
        {m.SetTexture("_BaseMap",null);m.SetColor("_BaseColor",new Color(.26f,.48f,.65f));m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.19f);}
        else if(n=="Fac_Light")
        {m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.57f,.68f,.81f));}
        else
        {
            var tex=FindTexture(texture,"_diff");
            var normal=FindTexture(texture,"_nor_gl");
            m.SetTexture("_BaseMap",n=="Fac_White"||n=="Fac_DarkSteel"?null:tex);
            m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");
            m.SetTextureScale("_BaseMap",Vector2.one*(n=="Fac_Roof"?.65f:1));
            m.SetTextureScale("_BumpMap",m.GetTextureScale("_BaseMap"));
            if(normal)
            {
                var imp=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal)) as TextureImporter;
                if(imp&&imp.textureType!=TextureImporterType.NormalMap){imp.textureType=TextureImporterType.NormalMap;imp.SaveAndReimport();}
                m.SetTexture("_BumpMap",normal);m.SetFloat("_BumpScale",.48f);m.EnableKeyword("_NORMALMAP");
            }
        }
        EditorUtility.SetDirty(m);return m;
    }

    static Texture2D FindTexture(string prefix,string map)
    {
        var bundled=AssetDatabase.LoadAssetAtPath<Texture2D>(Detail+prefix+map+"_2k"+(map=="_diff"?".jpg":".exr"));
        if(bundled)return bundled;
        return AssetDatabase.FindAssets(prefix+" t:Texture2D",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p=>System.IO.Path.GetFileNameWithoutExtension(p).StartsWith(prefix+map,System.StringComparison.OrdinalIgnoreCase))
            .Select(p=>AssetDatabase.LoadAssetAtPath<Texture2D>(p)).FirstOrDefault();
    }

    [MenuItem("Tools/Pixel Packers/Replace GameTest Trees With Pine and Spruce")]
    static void ReplaceTrees()=>FacilityRepair.RepairTrees();
    static void ApplyOfficeMaterials(GameObject root)
    {
        const string folder="Assets/PixelPackersOffice";
        const string output=folder+"/OfficeFinishes";
        if(!AssetDatabase.IsValidFolder(output))AssetDatabase.CreateFolder(folder,"OfficeFinishes");
        string[] search={folder+"/textures",folder+"/ModernOffice/textures"};
        var maps=AssetDatabase.FindAssets("t:Texture2D",search.Where(AssetDatabase.IsValidFolder).ToArray())
            .Select(AssetDatabase.GUIDToAssetPath).ToDictionary(path=>path,System.StringComparer.OrdinalIgnoreCase);
        Texture2D Map(string key)
        {
            var path=maps.Keys.FirstOrDefault(p=>System.IO.Path.GetFileNameWithoutExtension(p).Equals(key,System.StringComparison.OrdinalIgnoreCase));
            return path==null?null:AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        foreach(var renderer in root.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(source=>
            {
                if(!source)return source;
                string name=source.name;
                var baseMap=Map(name+"_Base_color");
                if(!baseMap)return source;
                string path=output+"/"+name+".mat";
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
                m.SetTexture("_BaseMap",baseMap);m.SetColor("_BaseColor",Color.white);
                var normal=Map(name+"_Normal_OpenGL");
                if(normal)
                {
                    var imp=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal)) as TextureImporter;
                    if(imp&&imp.textureType!=TextureImporterType.NormalMap){imp.textureType=TextureImporterType.NormalMap;imp.SaveAndReimport();}
                    m.SetTexture("_BumpMap",normal);m.EnableKeyword("_NORMALMAP");
                }
                m.SetFloat("_Smoothness",name=="M_Glass"?.86f:name=="M_Metal"?.5f:.27f);
                m.SetFloat("_Metallic",name=="M_Metal"?.62f:0);
                if(name=="M_Glass")
                {
                    m.SetColor("_BaseColor",new Color(.84f,.94f,1,.25f));
                    m.SetFloat("_Surface",1);m.SetFloat("_ZWrite",0);m.SetInt("_SrcBlend",5);m.SetInt("_DstBlend",10);
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;
                }
                EditorUtility.SetDirty(m);return m;
            }).ToArray();
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Pixel Packers/Fill Both Suit Bays")]
    static void FillSuitBays()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().name!="Moon Test")
        {Debug.LogWarning("Open Moon Test and exit Play Mode first.");return;}
        if(!GameObject.Find("Additional suit bays"))LunarRooms();
        var bays=GameObject.Find("Additional suit bays");
        if(!bays)return;
        int count=0;
        foreach(var entry in new[]{("Armstrong Apollo Suit.fbx",-4f),("NASA EMU.fbx",-1.9f)})
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PixelPackersOffice/Suits/"+entry.Item1);
            if(!model){Debug.LogWarning("Suit asset missing: "+entry.Item1);continue;}
            Selection.activeObject=model;
            PlaceSuit(entry.Item2);
            if(bays.transform.Find(entry.Item2<-3?"Suit display 1":"Suit display 2"))count++;
        }
        Selection.activeGameObject=bays;
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.FrameSelected();
        Debug.Log("Suit bays populated: "+count+"/2. Save Moon Test.");
    }
    [MenuItem("Tools/Pixel Packers/Set Up Lunar Rooms")]
    internal static void LunarRooms()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="Moon Test")
        {Debug.LogWarning("Open Moon Test and exit Play Mode first.");return;}
        var power=GameObject.Find("Equipment and power annex");
        var suit=GameObject.Find("01 Arrival and suit bay");
        if(!power||!suit){Debug.LogWarning("The lunar rooms were not found.");return;}

        foreach(var name in new[]{"server_rack","industrial_control_panel_box","industrial_storage_cart_2k.blend"})
        {
            var original=power.transform.Find(name);
            if(original){Undo.RecordObject(original.gameObject,"Clear room for equipment racks");original.gameObject.SetActive(false);}
        }

        if (!AssetDatabase.IsValidFolder(Materials)) AssetDatabase.CreateFolder("Assets/PixelPackersOffice","Materials");
        var metal=Mat("Rack frame",new Color(.12f,.17f,.21f));
        var panel=Mat("Suit bay ivory",new Color(.65f,.70f,.73f));
        var deck=Mat("Dark floor hardware",new Color(.16f,.22f,.24f));
        var led=Mat("Status LED",new Color(.14f,.78f,.66f),true);
        if(!power.transform.Find("Power and telemetry expansion")) {
        var gear=new GameObject("Power and telemetry expansion").transform;
        gear.SetParent(power.transform,false);
        Undo.RegisterCreatedObjectUndo(gear.gameObject,"Add lunar equipment");
        Box(gear,"Annex roof",new Vector3(0,3.31f,30),new Vector3(21.22f,.22f,7.22f),metal,true);
        foreach(var z in new[]{29.3f,32.0f})
        foreach(var x in new[]{4.45f,6.15f,7.85f,9.55f})
        {
            var center=new Vector3(x,1.13f,z);
            if(!Fit(gear,Props+"server_rack/Model.fbx","Server cabinet",center,new Vector3(.72f,2f,.78f),z>30?0:180,true))
                Box(gear,"Equipment cabinet",center,new Vector3(.72f,2f,.78f),metal,true);
            Box(gear,"Cabinet status",new Vector3(x,1.93f,z+(z>30?-.405f:.405f)),new Vector3(.27f,.033f,.016f),led,false);
        }
        foreach(var z in new[]{29.3f,32.0f})
            Box(gear,"Overhead cable tray",new Vector3(7.0f,2.67f,z),new Vector3(5.95f,.15f,.35f),metal,false);
        foreach(var x in new[]{-8.4f,-6.8f})
            Fit(gear,Props+"industrial_control_panel_box/Model.fbx","Power test equipment",new Vector3(x,1.24f,31),new Vector3(.65f,.38f,.38f));
        foreach(var x in new[]{-7f,0f,7f})
        {
            Box(gear,"Ceiling light fixture",new Vector3(x,3.13f,30.5f),new Vector3(2.4f,.08f,.27f),panel,false);
            var l=new GameObject("Power room light").AddComponent<Light>(); l.transform.SetParent(gear,false);
            l.transform.localPosition=new Vector3(x,2.98f,30.5f);l.range=7;l.intensity=1.5f;
        }
        }
        if(!suit.transform.Find("Additional suit bays")) {
        var bays=new GameObject("Additional suit bays").transform;
        bays.SetParent(suit.transform,false);
        Undo.RegisterCreatedObjectUndo(bays.gameObject,"Add suit bays");
        foreach(var x in new[]{-4.0f,-1.9f})
        {
            const float z=-18.75f;
            Box(bays,"Suit plinth",new Vector3(x,.235f,z),new Vector3(1.73f,.24f,.95f),deck,true);
            Box(bays,"Back panel",new Vector3(x,1.4f,z+.44f),new Vector3(1.73f,2.1f,.1f),metal,true);
            foreach(var side in new[]{-.82f,.82f})
                Box(bays,"Cabinet upright",new Vector3(x+side,1.41f,z),new Vector3(.09f,2.16f,.94f),panel,true);
            Box(bays,"Cabinet canopy",new Vector3(x,2.55f,z),new Vector3(1.73f,.15f,.95f),panel,true);
            Box(bays,"Locker light strip",new Vector3(x,2.46f,z),new Vector3(1.26f,.025f,.11f),led,false);
            var l=new GameObject("Suit display light").AddComponent<Light>(); l.transform.SetParent(bays,false);
            l.transform.localPosition=new Vector3(x,2.35f,z-.1f);l.range=2.3f;l.intensity=.85f;
        }
        }
        AddRoof("04 Crew and service access","Crew services roof",0,17,13.25f,7.25f,metal);
        AddRoof("03 Internal logistics","Cargo bay roof",16,4,18.35f,17.30f,metal);
        var lab=GameObject.Find("02 Operations and laboratory");
        if(lab&&!lab.transform.Find("NASA model enclosure roof"))
        {
            var unit=lab.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.StartsWith("Habitat Demonstration Unit"));
            var bounds=new Bounds(new Vector3(-17,5.1f,7),new Vector3(13,10,13));
            if(unit)foreach(var r in unit.GetComponentsInChildren<Renderer>(true))bounds.Encapsulate(r.bounds);
            float l=Mathf.Min(-25,bounds.min.x-2.65f),rgt=Mathf.Max(-6.5f,bounds.max.x+1.5f);
            float f=Mathf.Min(-4,bounds.min.z-.8f),b=Mathf.Max(12,bounds.max.z+2.65f);
            Box(lab.transform,"NASA model enclosure roof",new Vector3((l+rgt)*.5f,bounds.max.y+1.19f,(f+b)*.5f),
                new Vector3(rgt-l+.24f,.28f,b-f+.24f),metal,true);
            Undo.RegisterCreatedObjectUndo(lab.transform.Find("NASA model enclosure roof").gameObject,"Add operations roof");
        }
        var support=GameObject.Find("Operations support");
        var crew=GameObject.Find("04 Crew and service access");
        GroundProp(support,"workbench",-13.4f,10.4f,0,.132f);
        GroundProp(support,"metal-bench",-8.25f,9,90,.132f);
        GroundProp(crew,"metal-bench",-4.65f,19.1f,0,.132f);
        GroundProp(crew,"nft_hydroponic_system",4.45f,18.9f,0,.132f);
        GroundProp(suit,"metal-bench",5.65f,-21.9f,90,.108f);
        var baysRoot=suit.transform.Find("Additional suit bays");
        var emu=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PixelPackersOffice/Suits/NASA EMU.fbx");
        if(emu&&baysRoot&&!baysRoot.Find("Suit display 2"))
        { Selection.activeObject=emu; PlaceSuit(-1.9f); }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject=power;
    }

    static void AddRoof(string roomName,string name,float x,float z,float w,float d,Material mat)
    {
        var room=GameObject.Find(roomName);
        if(!room||room.transform.Find(name))return;
        Box(room.transform,name,new Vector3(x,3.34f,z),new Vector3(w,.24f,d),mat,true);
        Undo.RegisterCreatedObjectUndo(room.transform.Find(name).gameObject,"Add room roof");
        if(!name.StartsWith("Crew"))return;
        for(int i=-1;i<=1;i++)
        {
            var lamp=new GameObject("Crew ceiling light").AddComponent<Light>();
            lamp.transform.SetParent(room.transform,false);
            lamp.transform.localPosition=new Vector3(x+i*4,3.05f,z);
            lamp.type=LightType.Point;lamp.range=6;lamp.intensity=1.25f;
            Undo.RegisterCreatedObjectUndo(lamp.gameObject,"Add ceiling light");
        }
    }

    static void GroundProp(GameObject group,string name,float x,float z,float yaw,float floor)
    {
        if(!group)return;
        var prop=group.transform.Find(name);
        if(!prop)return;
        Undo.RecordObject(prop,"Align lunar furniture");
        prop.SetPositionAndRotation(new Vector3(x,floor,z),Quaternion.Euler(0,yaw,0));
        var renderers=prop.GetComponentsInChildren<Renderer>();
        if(renderers.Length>0)prop.position+=Vector3.up*(floor-renderers.Min(r=>r.bounds.min.y));
    }
    [MenuItem("Tools/Pixel Packers/Place NASA EMU in Bay 2")]
    static void PlaceEmu()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().name!="Moon Test")
        {Debug.LogWarning("Open Moon Test and exit Play Mode first.");return;}
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PixelPackersOffice/Suits/NASA EMU.fbx");
        if(!model){Debug.LogWarning("NASA EMU model is missing.");return;}
        if(!GameObject.Find("Additional suit bays")) LunarRooms();
        Selection.activeObject=model;
        PlaceSuit(-1.9f);
    }
    [MenuItem("Tools/Pixel Packers/Put Selected Suit in Bay 1")]
    static void Suit1()=>PlaceSuit(-4.0f);
    [MenuItem("Tools/Pixel Packers/Put Selected Suit in Bay 2")]
    static void Suit2()=>PlaceSuit(-1.9f);
    static void PlaceSuit(float x)
    {
        var bays=GameObject.Find("Additional suit bays");
        var asset=Selection.activeObject as GameObject;
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().name!="Moon Test"||
           !bays||!asset||!EditorUtility.IsPersistent(asset))
        {Debug.LogWarning("Set up Lunar Rooms, then select the imported suit model or prefab in the Project panel.");return;}
        string slot=x<-3?"Suit display 1":"Suit display 2";
        var old=bays.transform.Find(slot);
        if(!Fit(bays.transform,AssetDatabase.GetAssetPath(asset),slot,new Vector3(x,1.305f,-18.83f),new Vector3(-1.45f,1.9f,.85f),x<-3?180:0))return;
        if(old)Undo.DestroyObjectImmediate(old.gameObject);
        EditorSceneManager.MarkSceneDirty(bays.scene);
    }
    static Material Mat(string name,Color color,bool emissive=false)
    {
        var path=Materials+"/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat) return mat;
        mat=new Material(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"));
        mat.color=color; mat.SetFloat("_Smoothness",.24f);
        if(emissive){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.62f);}
        AssetDatabase.CreateAsset(mat,path);return mat;
    }

    static void Box(Transform p,string name,Vector3 pos,Vector3 size,Material mat,bool solid)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;
        g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;
        var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=mat;
        if(!solid) Object.DestroyImmediate(g.GetComponent<BoxCollider>());
        if(mat.renderQueue>=3000) r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static bool Fit(Transform p,string path,string name,Vector3 center,Vector3 size,float yaw=0,bool solid=false)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!prefab)return false;
        var holder=new GameObject(name);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,p.gameObject.scene);
        model.transform.SetParent(holder.transform,false);
        model.transform.localPosition=Vector3.zero;
        model.transform.localRotation=Quaternion.Euler(0,yaw,0)*model.transform.localRotation;
        var renderers=model.GetComponentsInChildren<Renderer>();
        if(renderers.Length==0){Object.DestroyImmediate(holder);return false;}
        if(path.StartsWith(Props,System.StringComparison.Ordinal))
        {
            string folder=System.IO.Path.GetDirectoryName(path).Replace('\\','/');
            var surfaces=AssetDatabase.FindAssets("t:Material",new[]{folder})
                .Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m=>m).ToArray();
            foreach(var r in renderers)
                r.sharedMaterials=r.sharedMaterials.Select(m=>surfaces.FirstOrDefault(s=>m&&s.name==m.name)??(surfaces.Length==1?surfaces[0]:m)).ToArray();
        }
        var bounds=renderers[0].bounds;
        foreach(var r in renderers) bounds.Encapsulate(r.bounds);
        var dims=bounds.size;
        float ratio=size.y/Mathf.Max(.001f,dims.y);
        if(size.x<0)ratio=Mathf.Min(ratio,Mathf.Abs(size.x)/Mathf.Max(.001f,dims.x),size.z/Mathf.Max(.001f,dims.z));
        if(size.x<0)center.y+=(dims.y*ratio-size.y)*.5f;
        holder.transform.localScale=size.x<=0?Vector3.one*ratio:new Vector3(size.x/Mathf.Max(.001f,dims.x),ratio,size.z/Mathf.Max(.001f,dims.z));
        holder.transform.SetParent(p,false);
        holder.transform.localPosition=center-Vector3.Scale(bounds.center,holder.transform.localScale);
        foreach(var c in model.GetComponentsInChildren<Collider>(true)) c.enabled=false;
        if(solid){var collider=holder.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=dims;}
        Undo.RegisterCreatedObjectUndo(holder,"Place downloaded model");
        return true;
    }
}










