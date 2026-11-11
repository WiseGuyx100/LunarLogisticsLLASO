using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelPackersLunarHabitat
{
    [ExecuteAlways]
    public sealed class LunarHabitat : MonoBehaviour
    {
        [Range(46,70)] public float domeRadius=48;
        [Range(20,50)] public float domeHeight=36;
        public bool buildHabitat=true;
        Transform layout;Material ground,structure,dark,glass,floor,white,blue,orange,green;
        public void OnEnable(){if(transform.Find("Habitat Layout")==null)Build();}
        public void Build()
        {
            layout=new GameObject("Habitat Layout").transform;layout.SetParent(transform,false);
            ground=Surface("Lunar regolith",new Color(.43f,.43f,.41f));var texture=Resources.Load<Texture2D>("PixelPackersLunarHabitat/LunarGround");
            if(texture!=null){ground.mainTexture=texture;if(ground.HasProperty("_BaseMap"))ground.SetTexture("_BaseMap",texture);}
            structure=Surface("Habitat ivory structure",new Color(.79f,.81f,.79f));dark=Surface("Dark titanium",new Color(.12f,.15f,.18f));floor=Surface("Industrial floor",new Color(.40f,.44f,.47f));
            white=Surface("Lane markings",new Color(.86f,.88f,.80f));blue=Surface("Water cargo",new Color(.15f,.58f,.82f));orange=Surface("Food cargo",new Color(.95f,.57f,.16f));green=Surface("Medical cargo",new Color(.24f,.70f,.48f));
            glass=Surface("Transparent dome glazing",new Color(.40f,.76f,.85f,.13f),true);
            TerrainMesh();Stars();
            if(buildHabitat)Habitat();
            var sun=new GameObject("Lunar sunlight",typeof(Light));sun.transform.SetParent(layout,false);sun.transform.rotation=Quaternion.Euler(25,-35,0);var light=sun.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.color=new Color(1,.97f,.90f);light.shadows=LightShadows.Soft;
            RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.35f,.40f);RenderSettings.fog=false;
            Boundaries();
            var player=new GameObject("Preview Walker",typeof(CharacterController),typeof(HabitatWalker));player.transform.SetParent(layout,false);player.transform.localPosition=new Vector3(0,.1f,-80);
            var controller=player.GetComponent<CharacterController>();controller.height=1.8f;controller.center=Vector3.up*.9f;controller.radius=.28f;controller.stepOffset=.3f;
            var camera=new GameObject("Habitat Camera",typeof(Camera),typeof(AudioListener));camera.transform.SetParent(player.transform,false);camera.transform.localPosition=Vector3.up*1.65f;camera.tag="MainCamera";
            var view=camera.GetComponent<Camera>();view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=Color.black;view.nearClipPlane=.05f;view.farClipPlane=1400;
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
        void TerrainMesh()
        {
            const int count=129;const float size=900;var vertices=new Vector3[count*count];var uv=new Vector2[vertices.Length];var triangles=new List<int>();
            for(int z=0;z<count;z++)for(int x=0;x<count;x++)
            {
                float px=(x/(float)(count-1)-.5f)*size,pz=(z/(float)(count-1)-.5f)*size;
                float outside=Mathf.Max(Mathf.Abs(px)-82,Mathf.Abs(pz)-122);float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(outside/55));
                float noise=Mathf.PerlinNoise(px*.008f+31,pz*.008f+17);float ridge=1-Mathf.Abs(noise*2-1);
                float height=blend*(3+42*ridge*ridge+15*Mathf.PerlinNoise(px*.021f+10,pz*.021f+4));
                vertices[z*count+x]=new Vector3(px,height-.16f,pz);uv[z*count+x]=new Vector2(px/7,pz/7);
                if(x<count-1&&z<count-1){int i=z*count+x;triangles.AddRange(new[]{i,i+count,i+1,i+1,i+count,i+count+1});}
            }
            var mesh=new Mesh{name="Flat lunar site with mountain perimeter"};mesh.vertices=vertices;mesh.uv=uv;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();MeshObject("Lunar landscape",mesh,ground,true);
            var random=new System.Random(27);
            for(int i=0;i<100;i++)
            {
                float x=(float)random.NextDouble()*600-300,z=(float)random.NextDouble()*600-300;if(Mathf.Abs(x)<88&&Mathf.Abs(z)<128)continue;
                // Rocks remain beyond the flat work area; no obstacles on the delivery apron.
                float outside=Mathf.Max(Mathf.Abs(x)-82,Mathf.Abs(z)-122),blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(outside/55));float ridge=1-Mathf.Abs(Mathf.PerlinNoise(x*.008f+31,z*.008f+17)*2-1);
                float y=blend*(3+42*ridge*ridge+15*Mathf.PerlinNoise(x*.021f+10,z*.021f+4))-.16f;
                var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere);rock.name="Background rock";rock.transform.SetParent(layout,false);rock.transform.localPosition=new Vector3(x,y,z);rock.transform.localScale=new Vector3(1.5f+(float)random.NextDouble()*3,1+(float)random.NextDouble()*2,2);rock.GetComponent<Renderer>().sharedMaterial=ground;
            }
        }
        void Stars()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var random=new System.Random(91);
            for(int i=0;i<1500;i++)
            {
                float y=.04f+(float)random.NextDouble()*.96f,angle=(float)random.NextDouble()*Mathf.PI*2;var direction=new Vector3(Mathf.Cos(angle)*Mathf.Sqrt(1-y*y),y,Mathf.Sin(angle)*Mathf.Sqrt(1-y*y));
                var center=direction*650;var right=Vector3.Cross(direction,Vector3.up).normalized*(.08f+(float)random.NextDouble()*.22f);var up=Vector3.Cross(right.normalized,direction)*right.magnitude;
                int n=vertices.Count;vertices.AddRange(new[]{center-right-up,center-right+up,center+right+up,center+right-up});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3,n+2,n+1,n,n+3,n+2,n});
            }
            var mesh=new Mesh{name="Lunar star field"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            var shader=Shader.Find(GraphicsSettings.currentRenderPipeline!=null?"Universal Render Pipeline/Unlit":"Unlit/Color");var material=new Material(shader){name="Star field light",color=new Color(.85f,.90f,1)};MeshObject("Stars",mesh,material,false).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        void Habitat()
        {
            Box("Habitat foundation",new Vector3(0,-.05f,0),new Vector3(100,.2f,104),floor);
            // A hemisphere with a 14 m wide loading entrance omitted from the lower front glazing.
            const int around=96,levels=24;var v=new List<Vector3>();var t=new List<int>();
            for(int j=0;j<=levels;j++)for(int i=0;i<=around;i++){float p=j/(float)levels*Mathf.PI*.5f,a=i/(float)around*Mathf.PI*2;v.Add(new Vector3(Mathf.Cos(a)*domeRadius*Mathf.Cos(p),.1f+domeHeight*Mathf.Sin(p),Mathf.Sin(a)*domeRadius*Mathf.Cos(p)));}
            for(int j=0;j<levels;j++)for(int i=0;i<around;i++){int a=j*(around+1)+i;var center=(v[a]+v[a+1]+v[a+around+1]+v[a+around+2])*.25f;if(center.z<-35&&Mathf.Abs(center.x)<7&&center.y<8)continue;t.AddRange(new[]{a,a+around+1,a+1,a+1,a+around+1,a+around+2});}
            var shell=new Mesh{name="Glass dome with receiving entrance"};shell.SetVertices(v);shell.SetTriangles(t,0);shell.RecalculateNormals();shell.RecalculateBounds();MeshObject("Dome glass",shell,glass,false).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            for(int rib=0;rib<12;rib++)for(int j=0;j<18;j++)
            {
                float a=rib*Mathf.PI/6,p=j/18f*Mathf.PI*.5f,q=(j+1)/18f*Mathf.PI*.5f;
                var start=new Vector3(Mathf.Cos(a)*domeRadius*Mathf.Cos(p),domeHeight*Mathf.Sin(p),Mathf.Sin(a)*domeRadius*Mathf.Cos(p));var end=new Vector3(Mathf.Cos(a)*domeRadius*Mathf.Cos(q),domeHeight*Mathf.Sin(q),Mathf.Sin(a)*domeRadius*Mathf.Cos(q));Beam("Dome structural rib",start,end,.22f,structure);
            }
            for(int i=0;i<64;i++)
            {float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32;var mid=new Vector3(Mathf.Cos((a+b)*.5f)*domeRadius,0,Mathf.Sin((a+b)*.5f)*domeRadius);if(mid.z<-35&&Mathf.Abs(mid.x)<8)continue;var wall=Box("Perimeter curb and invisible glazing collision",mid+Vector3.up*.35f,new Vector3(.32f,.7f,domeRadius*Mathf.PI/32+.1f),structure);wall.transform.localRotation=Quaternion.Euler(0,-(a+b)*.5f*Mathf.Rad2Deg,0);var barrier=new GameObject("Glass perimeter collision",typeof(BoxCollider));barrier.transform.SetParent(wall.transform,false);barrier.GetComponent<BoxCollider>().center=new Vector3(0,14,0);barrier.GetComponent<BoxCollider>().size=new Vector3(1,40,1);}
            Airlock();Interior();
        }
        void Airlock()
        {
            Box("Delivery apron",new Vector3(0,-.01f,-67),new Vector3(26,.2f,36),floor);
            Box("Loading corridor floor",new Vector3(0,.05f,-51),new Vector3(14,.2f,22),dark);
            Box("Airlock left wall",new Vector3(-7,4,-53),new Vector3(.4f,8,22),structure);Box("Airlock right wall",new Vector3(7,4,-53),new Vector3(.4f,8,22),structure);Box("Airlock canopy",new Vector3(0,8,-53),new Vector3(14.4f,.4f,22),structure);
            for(int side=-1;side<=1;side+=2){Box("Retracted receiving door",new Vector3(side*6.2f,3.4f,-64),new Vector3(1.5f,6.8f,.4f),dark);Box("Door status light",new Vector3(side*7.3f,4,-64.3f),new Vector3(.12f,7,.08f),green,false);}
            Label("DELIVERY AIRLOCK",new Vector3(0,7.1f,-64.3f),.45f);
            for(int i=0;i<6;i++)Box("Approach lane dash",new Vector3(0,.12f,-82+i*5),new Vector3(.25f,.015f,2),white,false);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<4;i++)Box("Apron safety bollard",new Vector3(side*11, .6f,-80+i*7),new Vector3(.3f,1.2f,.3f),orange);
        }
        void Interior()
        {
            Box("Receiving zone",new Vector3(0,.065f,-27),new Vector3(26,.03f,22),dark,false);Label("RECEIVING / UNLOAD",new Vector3(0,5,-38),.4f);
            for(int side=-1;side<=1;side+=2)Box("Main aisle edge",new Vector3(side*5,.13f,-6),new Vector3(.13f,.015f,58),white,false);
            for(int i=0;i<10;i++)Box("Main aisle center dash",new Vector3(0,.13f,-32+i*5),new Vector3(.2f,.015f,2),white,false);
            for(int side=-1;side<=1;side+=2)for(int row=0;row<3;row++)Rack(new Vector3(side*22,0,-19+row*12),row==0?blue:row==1?orange:green,row==0?"WATER":row==1?"FOOD":"MEDICAL");
            for(int i=0;i<8;i++)Crate(new Vector3(-9+(i%4)*6,.65f,-30+(i/4)*7),i%2==0?blue:orange);
            Box("Raised operations deck",new Vector3(0,4.4f,24),new Vector3(48,.45f,18),structure);
            for(int side=-1;side<=1;side+=2)foreach(float z in new[]{17f,31f})Box("Mezzanine column",new Vector3(side*20,2.2f,z),new Vector3(.5f,4.4f,.5f),dark);
            // Wide accessible ramp along the left wall; a single collision surface avoids step snagging.
            var ramp=Box("Operations access ramp",new Vector3(-33,2.2f,15),new Vector3(5,.3f,Mathf.Sqrt(24*24+4.4f*4.4f)),floor);ramp.transform.localRotation=Quaternion.Euler(-Mathf.Atan2(4.4f,24)*Mathf.Rad2Deg,0,0);
            Box("Ramp landing",new Vector3(-27,4.4f,28),new Vector3(10,.4f,6),structure);
            for(int x=-24;x<=24;x+=4){if(x<-18)continue;Beam("Deck guardrail post",new Vector3(x,4.65f,15.2f),new Vector3(x,5.85f,15.2f),.07f,dark);}
            Beam("Deck guardrail",new Vector3(-18,5.85f,15.2f),new Vector3(24,5.85f,15.2f),.09f,dark);
            Box("Operations room rear",new Vector3(4,7.3f,31),new Vector3(28,5.3f,.3f),structure);
            Box("Operations room canopy",new Vector3(4,10,26),new Vector3(28,.25f,10),structure);
            Box("Operations observation window",new Vector3(4,7.3f,21),new Vector3(28,5.3f,.08f),glass,false);
            for(int i=0;i<4;i++){Box("Mission console",new Vector3(-5+i*5,5.4f,25),new Vector3(3,1.5f,1.5f),dark);Box("Console display",new Vector3(-5+i*5,6.6f,25.4f),new Vector3(2.4f,.9f,.12f),blue,false);}
            Label("OPERATIONS",new Vector3(4,8.5f,20.8f),.42f);
            for(int i=0;i<4;i++){var tank=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tank.name="Habitat utility tank";tank.transform.SetParent(layout,false);tank.transform.localPosition=new Vector3(30+i*3,2.1f,14);tank.transform.localScale=new Vector3(2,2.1f,2);tank.GetComponent<Renderer>().sharedMaterial=structure;}
        }
        void Rack(Vector3 position,Material zone,string title)
        {
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box(title+" rack upright",position+new Vector3(x*6,2.7f,z*1.5f),new Vector3(.22f,5.4f,.22f),dark);
            for(int level=0;level<3;level++){Box(title+" rack shelf",position+new Vector3(0,.35f+level*1.8f,0),new Vector3(12.4f,.15f,3.2f),structure);Box(title+" shelf color strip",position+new Vector3(0,.48f+level*1.8f,-1.59f),new Vector3(12,.08f,.05f),zone,false);for(int col=0;col<3;col++)Crate(position+new Vector3(-4+col*4,1.025f+level*1.8f,0),zone);}
            Label(title,position+new Vector3(0,6,-1.7f),.35f);
        }
        void Crate(Vector3 p,Material zone)
        {Box("Cargo container",p,new Vector3(2.5f,1.2f,1.6f),structure);for(int side=-1;side<=1;side+=2)Box("Cargo strap",p+new Vector3(side*.72f,0,-.82f),new Vector3(.1f,1.22f,.04f),dark,false);Box("Cargo zone label",p+new Vector3(0,.05f,-.83f),new Vector3(.8f,.55f,.04f),zone,false);}
        void Label(string value,Vector3 p,float size)
        {var go=new GameObject(value,typeof(TextMesh));go.transform.SetParent(layout,false);go.transform.localPosition=p;var text=go.GetComponent<TextMesh>();text.text=value;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=96;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Color.white;go.GetComponent<Renderer>().sharedMaterial=text.font.material;}
        void Boundaries()
        {
            for(int side=-1;side<=1;side+=2){var x=new GameObject("Invisible site boundary",typeof(BoxCollider));x.transform.SetParent(layout,false);x.transform.localPosition=new Vector3(side*79,15,0);x.GetComponent<BoxCollider>().size=new Vector3(1,40,240);var z=new GameObject("Invisible site boundary",typeof(BoxCollider));z.transform.SetParent(layout,false);z.transform.localPosition=new Vector3(0,15,side*119);z.GetComponent<BoxCollider>().size=new Vector3(160,40,1);}
        }
    }
}
