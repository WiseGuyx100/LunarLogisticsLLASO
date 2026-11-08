using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelPackersLunarBlank
{
    [ExecuteAlways]
    public sealed class BlankLunarHabitat : MonoBehaviour
    {
        [Range(46,70)] public float domeRadius=48;
        [Range(20,50)] public float domeHeight=36;
        public TerrainData originalTerrain;
        public Material originalSky;
        public GameObject[] rockPrefabs;
        public Texture2D rockAlbedo,rockNormal;
        Terrain nativeTerrain;
        float nativeHalfX=125,nativeHalfZ=125;
        Transform layout;Material ground,structure,glass;
        public void OnEnable(){if(transform.Find("Habitat Layout")==null)Build();}
        public void Build()
        {
            layout=new GameObject("Habitat Layout").transform;layout.SetParent(transform,false);
            ground=Surface("Original lunar ground",Color.white);
            var texture=originalTerrain!=null&&originalTerrain.terrainLayers.Length>0?originalTerrain.terrainLayers[0].diffuseTexture:null;
            if(texture!=null){ground.mainTexture=texture;if(ground.HasProperty("_BaseMap"))ground.SetTexture("_BaseMap",texture);}
            structure=Surface("Dome ivory structure",new Color(.79f,.81f,.79f));
            glass=Surface("Transparent dome glazing",new Color(.40f,.76f,.85f,.13f),true);
            OriginalLandscape();TerrainMesh();
            EmptyDome();
            var sun=new GameObject("Lunar sunlight",typeof(Light));sun.transform.SetParent(layout,false);sun.transform.rotation=Quaternion.Euler(25,-35,0);var light=sun.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.color=new Color(1,.97f,.90f);light.shadows=LightShadows.Soft;
            RenderSettings.skybox=originalSky;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.35f,.40f);RenderSettings.fog=false;
            Boundaries();
            var player=new GameObject("Preview Walker",typeof(CharacterController),typeof(HabitatWalker));player.transform.SetParent(layout,false);player.transform.localPosition=new Vector3(0,.2f,-78);
            var controller=player.GetComponent<CharacterController>();controller.height=1.8f;controller.center=Vector3.up*.9f;controller.radius=.28f;controller.stepOffset=.3f;
            var camera=new GameObject("Habitat Camera",typeof(Camera),typeof(AudioListener));camera.transform.SetParent(player.transform,false);camera.transform.localPosition=Vector3.up*1.65f;camera.tag="MainCamera";
            var view=camera.GetComponent<Camera>();view.clearFlags=CameraClearFlags.Skybox;view.backgroundColor=Color.black;view.nearClipPlane=.05f;view.farClipPlane=1400;
        }
        Material Surface(string name,Color color,bool transparent=false)
        {
            var shader=Shader.Find(GraphicsSettings.currentRenderPipeline!=null?"Universal Render Pipeline/Lit":"Standard");if(shader==null)shader=Shader.Find("Standard");
            var m=new Material(shader){name=name,color=color};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",transparent?.9f:.3f);
            if(transparent)
            {
                if(m.HasProperty("_Surface")){m.SetFloat("_Surface",1);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetFloat("_Blend",0);}
                if(m.HasProperty("_Mode")){m.SetFloat("_Mode",3);m.EnableKeyword("_ALPHABLEND_ON");}
                m.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);m.SetInt("_ZWrite",0);m.SetInt("_Cull",(int)CullMode.Off);m.renderQueue=3000;
            }
            return m;
        }
        GameObject Box(string name,Vector3 position,Vector3 size,Material material,bool collision=true)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(layout,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;if(!collision){go.GetComponent<Collider>().enabled=false;Remove(go.GetComponent<Collider>());}return go;}
        void Beam(string name,Vector3 a,Vector3 b,float width,Material material)
        {var go=Box(name,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),material,false);go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        public static void Remove(Object item){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        GameObject MeshObject(string name,Mesh mesh,Material material,bool collider)
        {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(layout,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;if(collider)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;}
        void OriginalLandscape()
        {
            if(originalTerrain==null){Debug.LogError("Original lunar TerrainData is missing. Copy the supplied Lunar Landscape 3D assets too.");return;}
            var data=Instantiate(originalTerrain);data.name="Restored lunar terrain - editable copy";
            var size=data.size;nativeHalfX=size.x*.5f;nativeHalfZ=size.z*.5f;
            float level=data.GetInterpolatedHeight(.5f,.5f)/size.y;
            int resolution=data.heightmapResolution;var heights=data.GetHeights(0,0,resolution,resolution);
            for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)
            {
                float px=x*size.x/(resolution-1)-nativeHalfX,pz=z*size.z/(resolution-1)-nativeHalfZ;
                float outside=Mathf.Max(Mathf.Abs(px)-80,Mathf.Abs(pz)-105);
                float flat=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(outside/15));heights[z,x]=Mathf.Lerp(heights[z,x],level,flat);
                float edge=Mathf.Min(nativeHalfX-Mathf.Abs(px),nativeHalfZ-Mathf.Abs(pz));
                heights[z,x]=Mathf.Lerp(level,heights[z,x],Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/10)));
            }
            data.SetHeights(0,0,heights);
            var go=Terrain.CreateTerrainGameObject(data);go.name="Original lunar terrain and ground layers";go.transform.SetParent(layout,false);go.transform.localPosition=new Vector3(-nativeHalfX,-level*size.y,-nativeHalfZ);
            nativeTerrain=go.GetComponent<Terrain>();nativeTerrain.drawInstanced=true;nativeTerrain.drawTreesAndFoliage=false;
            var shader=Shader.Find(GraphicsSettings.currentRenderPipeline!=null?"Universal Render Pipeline/Terrain/Lit":"Nature/Terrain/Standard");
            nativeTerrain.materialTemplate=new Material(shader){name="Restored lunar terrain surface"};
            if(rockPrefabs==null||rockPrefabs.Length==0)return;
            var material=Surface("Original lunar rocks",Color.white);material.mainTexture=rockAlbedo;if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",rockAlbedo);
            if(rockNormal!=null){material.SetTexture("_BumpMap",rockNormal);material.EnableKeyword("_NORMALMAP");}
            var random=new System.Random(487);
            for(int i=0;i<850;i++)
            {
                float x=((float)random.NextDouble()*2-1)*(nativeHalfX-3),z=((float)random.NextDouble()*2-1)*(nativeHalfZ-3);
                if(x*x+z*z<(domeRadius+5)*(domeRadius+5))continue;
                var prefab=rockPrefabs[random.Next(rockPrefabs.Length)];if(prefab==null)continue;
                var rock=Instantiate(prefab,layout);rock.name="Original lunar rock";rock.transform.localPosition=new Vector3(x,0,z);rock.transform.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                var renderers=rock.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                float desired=.12f+Mathf.Pow((float)random.NextDouble(),3)*1.5f;rock.transform.localScale*=desired/Mathf.Max(.01f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
                foreach(var r in renderers)r.sharedMaterial=material;
                float y=nativeTerrain.SampleHeight(transform.TransformPoint(new Vector3(x,0,z)))+nativeTerrain.transform.position.y;
                rock.transform.position=transform.TransformPoint(new Vector3(x,y-transform.position.y,z));
                foreach(var f in rock.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh!=null&&f.GetComponent<Collider>()==null&&desired>.45f)f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
            }
        }
        void TerrainMesh()
        {
            const int count=181;const float size=900;var vertices=new Vector3[count*count];var uv=new Vector2[vertices.Length];var triangles=new List<int>();
            for(int z=0;z<count;z++)for(int x=0;x<count;x++)
            {
                float px=(x/(float)(count-1)-.5f)*size,pz=(z/(float)(count-1)-.5f)*size;
                float outside=Mathf.Max(Mathf.Abs(px)-nativeHalfX,Mathf.Abs(pz)-nativeHalfZ);float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(outside/55));
                float noise=Mathf.PerlinNoise(px*.008f+31,pz*.008f+17);float ridge=1-Mathf.Abs(noise*2-1);
                float height=blend*(3+42*ridge*ridge+15*Mathf.PerlinNoise(px*.021f+10,pz*.021f+4));
                vertices[z*count+x]=new Vector3(px,height-.01f,pz);uv[z*count+x]=new Vector2(px/15,pz/15);
                if(x<count-1&&z<count-1){float cx=px+size/(count-1)*.5f,cz=pz+size/(count-1)*.5f;if(Mathf.Abs(cx)<nativeHalfX&&Mathf.Abs(cz)<nativeHalfZ)continue;int i=z*count+x;triangles.AddRange(new[]{i,i+count,i+1,i+1,i+count,i+count+1});}
            }
            var mesh=new Mesh{name="Flat lunar site with mountain perimeter"};mesh.vertices=vertices;mesh.uv=uv;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();MeshObject("Lunar landscape",mesh,ground,true);
        }
        void EmptyDome()
        {

            // Empty dome: glass and structural ribs only.
            const int around=96,levels=24;var v=new List<Vector3>();var t=new List<int>();
            for(int j=0;j<=levels;j++)for(int i=0;i<=around;i++){float p=j/(float)levels*Mathf.PI*.5f,a=i/(float)around*Mathf.PI*2;v.Add(new Vector3(Mathf.Cos(a)*domeRadius*Mathf.Cos(p),.1f+domeHeight*Mathf.Sin(p),Mathf.Sin(a)*domeRadius*Mathf.Cos(p)));}
            for(int j=0;j<levels;j++)for(int i=0;i<around;i++){int a=j*(around+1)+i;var center=(v[a]+v[a+1]+v[a+around+1]+v[a+around+2])*.25f;t.AddRange(new[]{a,a+around+1,a+1,a+1,a+around+1,a+around+2});}
            var shell=new Mesh{name="Glass dome with receiving entrance"};shell.SetVertices(v);shell.SetTriangles(t,0);shell.RecalculateNormals();shell.RecalculateBounds();MeshObject("Dome glass",shell,glass,false).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            for(int rib=0;rib<12;rib++)for(int j=0;j<18;j++)
            {
                float a=rib*Mathf.PI/6,p=j/18f*Mathf.PI*.5f,q=(j+1)/18f*Mathf.PI*.5f;
                var start=new Vector3(Mathf.Cos(a)*domeRadius*Mathf.Cos(p),domeHeight*Mathf.Sin(p),Mathf.Sin(a)*domeRadius*Mathf.Cos(p));var end=new Vector3(Mathf.Cos(a)*domeRadius*Mathf.Cos(q),domeHeight*Mathf.Sin(q),Mathf.Sin(a)*domeRadius*Mathf.Cos(q));Beam("Dome structural rib",start,end,.22f,structure);
            }
        }
        void Boundaries()
        {
            for(int side=-1;side<=1;side+=2){var x=new GameObject("Invisible site boundary",typeof(BoxCollider));x.transform.SetParent(layout,false);x.transform.localPosition=new Vector3(side*79,15,0);x.GetComponent<BoxCollider>().size=new Vector3(1,40,240);var z=new GameObject("Invisible site boundary",typeof(BoxCollider));z.transform.SetParent(layout,false);z.transform.localPosition=new Vector3(0,15,side*119);z.GetComponent<BoxCollider>().size=new Vector3(160,40,1);}
        }
    }
}
