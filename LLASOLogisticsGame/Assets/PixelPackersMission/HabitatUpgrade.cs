using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace PixelPackersMission
{
    internal static class HabitatUpgrade
    {
        static Material trim,glass,deck,line;
        static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,Material mat,bool collision=true)
            =>MissionFlow.Piece(p,name,PrimitiveType.Cube,pos,size,mat,collision);
        static Transform Find(string name)=>GameObject.Find(name)?.transform;

        internal static void Apply()
        {
            var arrival=Find("01 Arrival and suit bay");
            var airlock=Find("South dome entrance");
            var lab=Find("02 Operations and laboratory");
            var cargo=Find("03 Internal logistics");
            if(!arrival&&!airlock&&!lab&&!cargo)return;
            trim=MissionFlow.Surface("Habitat aluminum framing",new Color(.16f,.22f,.26f),.46f);
            deck=MissionFlow.Surface("Habitat walkway",new Color(.21f,.27f,.31f),.18f);
            line=MissionFlow.Surface("Walkway edge markings",new Color(.84f,.83f,.7f),0);
            glass=MissionFlow.Surface("Clear habitat glazing",new Color(.63f,.83f,.90f,.20f),.1f);
            glass.SetFloat("_Surface",1);glass.SetFloat("_ZWrite",0);
            glass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
            glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            glass.SetOverrideTag("RenderType","Transparent");glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glass.renderQueue=(int)RenderQueue.Transparent;

            SealDome(airlock);
            float operationsEntrance=-4;
            if(cargo) RestoreStaging(cargo);
            if(arrival)
            {
                var wing=arrival.Find("Door right");
                if(wing&&wing.GetComponent<Renderer>())
                {
                    var r=wing.GetComponent<Renderer>();var b=r.bounds;
                    float left=b.min.x+.52f,right=b.max.x-.52f,low=b.min.y+.93f,high=b.max.y-.58f;
                    var mat=r.sharedMaterial;
                    if(right-left>1&&high-low>1)
                    {
                        wing.gameObject.SetActive(false);
                        WallBand(arrival,b.min.x,b.max.x,b.min.y,low,b.center.z,b.size.z,mat);
                        WallBand(arrival,b.min.x,b.max.x,high,b.max.y,b.center.z,b.size.z,mat);
                        WallBand(arrival,b.min.x,left,low,high,b.center.z,b.size.z,mat);
                        WallBand(arrival,right,b.max.x,low,high,b.center.z,b.size.z,mat);
                        Window(arrival,left,right,low,high,b.center.z);
                    }
                }
                Door(arrival,0,-30.35f,1.91f,2.34f);
                Label(arrival,"ARRIVAL / SUIT BAY",new Vector3(0,2.94f,-29.83f),180);
            }
            if(airlock)Door(airlock,0,-50.86f,2.04f,2.38f);

            if(lab)
            {
                var unit=lab.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.StartsWith("Habitat Demonstration Unit",System.StringComparison.OrdinalIgnoreCase));
                var back=lab.Find("Rear service wall");
                var sides=lab.Cast<Transform>().Where(t=>t.name=="Full bay side wall").OrderBy(t=>t.position.x).ToArray();
                if(back&&sides.Length>=2)
                {
                    var b=new Bounds(new Vector3(-17,5.1f,7),new Vector3(13,10,13));
                    if(unit) foreach(var r in unit.GetComponentsInChildren<Renderer>(true)) b.Encapsulate(r.bounds);
                    float originalFront=sides[0].position.z-sides[0].localScale.z/2;
                    float front=Mathf.Min(originalFront,b.min.z-.8f);
                    float rear=Mathf.Max(back.position.z,b.max.z+2.65f);
                    float left=Mathf.Min(sides[0].position.x-1.5f,b.min.x-2.65f);
                    float right=Mathf.Max(sides[sides.Length-1].position.x,b.max.x+1.5f);
                    float bottom=.11f, top=Mathf.Max(4.1f,b.max.y+1.05f);
                    float height=top-bottom, mid=(top+bottom)*.5f;
                    for(int i=0;i<2;i++)
                    {
                        var s=sides[i==0?0:sides.Length-1];s.position=new Vector3(i==0?left:right,mid,(front+rear)/2);
                        s.localScale=new Vector3(.18f,height,rear-front);
                    }
                    back.position=new Vector3((left+right)*.5f,mid,rear);
                    back.localScale=new Vector3(right-left,height,.19f);
                    var floor=lab.Find("Defined work bay floor");
                    if(floor)
                    {
                        floor.position=new Vector3((left+right)*.5f,.12f,(front+rear)*.5f);
                        floor.localScale=new Vector3(right-left,.023f,rear-front);
                    }
                    // Move the existing glass front as one assembly if the model extends past it.
                    foreach(Transform child in lab)
                        if(child.name.StartsWith("Observation ")||child.name=="Operations viewing glass"||
                           child.name=="Bay entrance header"||child.name=="Bay entry post"||
                           (Mathf.Abs(child.position.z-originalFront)<.4f&&child.position.x>-9&&child.position.x<-6))
                            child.position+=Vector3.forward*(front-originalFront);
                    operationsEntrance=front;
                    if(left<-23.5f)WallBand(lab,left,-23.5f,.13f,3.6f,front,.18f,trim);
                    else if(left>-23.5f)WallBand(lab,-23.5f,left,.13f,3.6f,front,.18f,trim);
                    Door(lab,-8.05f,front-.15f,2.07f,2.44f);
                    WallBand(lab,left,right,3.24f,top,front,.22f,trim);
                    WallBand(lab,-6.5f,right,.11f,3.25f,front,.22f,trim);
                    Box(lab,"NASA model enclosure roof",new Vector3((left+right)*.5f,top+.14f,(front+rear)*.5f),new Vector3(right-left+.24f,.28f,rear-front+.24f),deck);
                    foreach(float z in new[]{front+3,rear-3})
                    {
                        var light=Box(lab,"Enclosure overhead light",new Vector3((left+right)*.5f,top-.12f,z),new Vector3(3,.1f,.3f),line,false).AddComponent<Light>();
                        light.type=LightType.Point;light.range=14;light.intensity=3;
                    }
                }
            }
            ConnectWalkways(arrival,operationsEntrance);
        }

        static void RestoreStaging(Transform cargo)
        {
            const float front=-4.5f;
            foreach(float center in new[]{10.4f,21.6f})
            {
                WallBand(cargo,center-3.3f,center+3.3f,.13f,1.04f,front,.21f,trim);
                WallBand(cargo,center-3.3f,center+3.3f,2.68f,3.35f,front,.21f,trim);
                Window(cargo,center-3.3f,center+3.3f,1.04f,2.68f,front);
            }
            Box(cargo,"Cargo bay roof",new Vector3(16,3.34f,4),new Vector3(18.35f,.24f,17.30f),deck);
            for(int i=0;i<4;i++)
            {
                float z=-3+i*4.2f;
                Box(cargo,"Cargo roof crossmember",new Vector3(16,3.15f,z),new Vector3(18,.19f,.23f),trim,false);
                var fixture=Box(cargo,"Cargo bay strip light",new Vector3(16,3.02f,z),new Vector3(2,.08f,.32f),line,false);
                var light=fixture.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.86f,.93f,1);light.intensity=2;light.range=10;
            }
        }

        static void SealDome(Transform airlock)
        {
            var deckSurface=Find("Textured circular pressure deck");
            var shell=Find("Dome glass");
            if(!deckSurface&&!shell)return;
            var visible=deckSurface?deckSurface.GetComponent<Renderer>():null;
            var bounds=visible?visible.bounds:new Bounds(Vector3.zero,new Vector3(95.6f,0,95.6f));
            var center=bounds.center;
            var radius=bounds.extents.x>35?bounds.extents.x:47.8f;
            // Solid overlapping segments block both directions without reading or cooking the glass mesh.
            var root=new GameObject("Solid dome boundary - south airlock opening").transform;
            const float gapHalfWidth=1.93f;
            float gap=airlock?Mathf.Asin(gapHalfWidth/radius):0;
            const int count=96;
            float step=(2*Mathf.PI-2*gap)/count;
            for(int i=0;i<count;i++)
            {
                float angle=gap+(i+.5f)*step;
                var radial=new Vector3(Mathf.Sin(angle),0,-Mathf.Cos(angle));
                var wall=new GameObject("Glass boundary segment",typeof(BoxCollider));
                wall.transform.SetParent(root,false);
                wall.transform.SetPositionAndRotation(center+radial*(radius*Mathf.Cos(step*.5f))+Vector3.up*3.5f,Quaternion.LookRotation(radial));
                wall.GetComponent<BoxCollider>().size=new Vector3(2*radius*Mathf.Sin(step*.5f)+(i==0||i==count-1?0:.18f),7.4f,.42f);
            }
        }

        const float WalkY=.173f, HalfWidth=1.325f, Port=2.45f;
        static Vector2 Left(Vector2 v)=>new Vector2(-v.y,v.x);
        static Vector3 FloorPoint(Vector2 p,float y)=>new Vector3(p.x,y,p.y);

        static void ConnectWalkways(Transform arrival,float operationsEntrance)
        {
            var routes=new GameObject("Habitat floor paths").transform;
            var hatch=arrival?arrival.Find("Service hatch"):null;
            // End on the approach side, clear of both the collar and the moving lid.
            float hatchApproach=hatch?hatch.position.z-1.48f:-22.28f;
            var a=new Vector2(0,-34);
            float cross=Mathf.Min(-10,operationsEntrance-3.5f);
            var b=new Vector2(9,cross);
            var c=new Vector2(0,cross);
            var d=new Vector2(16,cross);
            var n=Vector2.up;var e=Vector2.right;
            Junction(routes,a,-n,e,n);
            Junction(routes,b,-n,e,-e);
            Junction(routes,c,e,n,-e);
            Junction(routes,d,e,n,-e);
            Walk(routes,new Vector2(0,-52),a-n*Port);
            Walk(routes,a+n*Port,new Vector2(0,hatchApproach));
            Walk(routes,a+e*Port,new Vector2(9,-34),b-n*Port);
            Walk(routes,b-e*Port,c+e*Port);
            Walk(routes,c-e*Port,new Vector2(-8.05f,cross),new Vector2(-8.05f,operationsEntrance+1.1f));
            Walk(routes,b+e*Port,d-e*Port);
            Walk(routes,d+n*Port,new Vector2(16,-3.8f));
            Walk(routes,c+n*Port,new Vector2(0,14.8f));
            Walk(routes,d+e*Port,new Vector2(27,cross),new Vector2(27,15),
                new Vector2(8.35f,15),new Vector2(8.35f,25),new Vector2(0,25),new Vector2(0,27.9f));
        }

        static Vector2 Curve(Vector2 a,Vector2 corner,Vector2 b,float t)
            =>Vector2.Lerp(Vector2.Lerp(a,corner,t),Vector2.Lerp(corner,b,t),t);

        static void Walk(Transform parent,params Vector2[] points)
        {
            var centers=new List<Vector2>{points[0]};
            var tangents=new List<Vector2>{(points[1]-points[0]).normalized};
            for(int i=1;i<points.Length-1;i++)
            {
                var p=points[i];var incoming=p-points[i-1];var outgoing=points[i+1]-p;
                float before=Mathf.Min(2.45f,incoming.magnitude*(i==1?.94f:.48f));
                float after=Mathf.Min(2.45f,outgoing.magnitude*(i==points.Length-2?.94f:.48f));
                var a=p-incoming.normalized*before;var b=p+outgoing.normalized*after;
                for(int j=0;j<=16;j++)
                {
                    float t=j/16f;
                    centers.Add(Curve(a,p,b,t));
                    tangents.Add(Vector2.Lerp(p-a,b-p,t).normalized);
                }
            }
            centers.Add(points[points.Length-1]);
            tangents.Add((points[points.Length-1]-points[points.Length-2]).normalized);
            Ribbon(-HalfWidth,HalfWidth,WalkY,deck,"Curved floor path");
            Ribbon(-1.265f,-1.195f,WalkY+.003f,line,"Continuous edge stripe");
            Ribbon(1.195f,1.265f,WalkY+.003f,line,"Continuous edge stripe");
            void Ribbon(float lo,float hi,float y,Material mat,string name)
            {
                var vertices=new List<Vector3>();
                for(int i=0;i<centers.Count;i++)
                {
                    var offset=Left(tangents[i]);
                    vertices.Add(FloorPoint(centers[i]+offset*lo,y));
                    vertices.Add(FloorPoint(centers[i]+offset*hi,y));
                }
                Strip(parent,name,vertices,mat);
            }
        }

        static void Junction(Transform parent,Vector2 center,params Vector2[] directions)
        {
            System.Array.Sort(directions,(a,b)=>Mathf.Atan2(a.y,a.x).CompareTo(Mathf.Atan2(b.y,b.x)));
            var perimeter=new List<Vector3>();
            for(int i=0;i<directions.Length;i++)
            {
                var d=directions[i];var next=directions[(i+1)%directions.Length];
                var edges=new List<Vector3>();
                for(int j=0;j<=12;j++)
                {
                    float t=j/12f;
                    perimeter.Add(FloorPoint(Edge(HalfWidth,t),WalkY));
                    edges.Add(FloorPoint(Edge(1.195f,t),WalkY+.003f));
                    edges.Add(FloorPoint(Edge(1.265f,t),WalkY+.003f));
                }
                Strip(parent,"Junction edge stripe",edges,line);
                Vector2 Edge(float width,float t)
                {
                    var a=center+d*Port+Left(d)*width;
                    var b=center+next*Port-Left(next)*width;
                    // Opposite legs share a straight edge; adjacent legs get a fillet.
                    return Vector2.Dot(d,next)<-.9f?Vector2.Lerp(a,b,t):Curve(a,center+(d+next)*width,b,t);
                }
            }
            int count=perimeter.Count;
            perimeter.Add(FloorPoint(center,WalkY));
            var triangles=new List<int>();
            for(int i=0;i<count;i++) Triangle(perimeter,triangles,count,i,(i+1)%count);
            MakePathMesh(parent,"Joined floor path junction",perimeter,triangles,deck);
        }

        static void Strip(Transform parent,string name,List<Vector3> vertices,Material mat)
        {
            var triangles=new List<int>();
            for(int i=0;i+3<vertices.Count;i+=2)
            {
                Triangle(vertices,triangles,i,i+1,i+2);
                Triangle(vertices,triangles,i+1,i+3,i+2);
            }
            MakePathMesh(parent,name,vertices,triangles,mat);
        }

        static void Triangle(List<Vector3> vertices,List<int> triangles,int a,int b,int c)
        {
            float up=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).y;
            if(Mathf.Abs(up)<.000001f)return;
            triangles.Add(a);triangles.Add(up>0?b:c);triangles.Add(up>0?c:b);
        }

        static void MakePathMesh(Transform parent,string name,List<Vector3> vertices,List<int> triangles,Material mat)
        {
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            obj.transform.SetParent(parent,false);
            var mesh=new Mesh{name=name};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        static void WallBand(Transform p,float left,float right,float low,float high,float z,float thickness,Material mat)
        {
            Box(p,"Window wall panel",new Vector3((left+right)*.5f,(low+high)*.5f,z),new Vector3(right-left,high-low,thickness),mat);
        }
        static void Window(Transform p,float left,float right,float low,float high,float z)
        {
            Box(p,"Fixed glass",new Vector3((left+right)*.5f,(low+high)*.5f,z),new Vector3(right-left,high-low,.04f),glass).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            foreach(float y in new[]{low,high})Box(p,"Window sill rail",new Vector3((left+right)*.5f,y,z),new Vector3(right-left,.11f,.20f),trim);
            for(int i=0;i<=3;i++)Box(p,"Window upright",new Vector3(Mathf.Lerp(left,right,i/3f),(low+high)*.5f,z),new Vector3(.10f,high-low,.18f),trim);
        }
        static void Label(Transform p,string value,Vector3 pos,float rotation)
        {
            Box(p,"Interior wayfinding backing",pos-new Vector3(0,0,.025f),new Vector3(2.5f,.34f,.07f),trim,false);
            var obj=new GameObject("Interior wayfinding",typeof(TextMesh));obj.transform.SetParent(p,false);obj.transform.SetPositionAndRotation(pos+Vector3.forward*.02f,Quaternion.Euler(0,rotation,0));
            var t=obj.GetComponent<TextMesh>();t.text=value;t.anchor=TextAnchor.MiddleCenter;t.fontSize=64;t.characterSize=.017f;t.color=Color.white;
        }
        static void Door(Transform p,float x,float z,float width,float height)
        {
            var obj=new GameObject("Sliding glass entrance doors");obj.transform.SetParent(p,false);
            obj.transform.position=new Vector3(x,.14f,z);
            var controller=obj.AddComponent<HabitatDoor>();
            var size=new Vector3(width*.5f,height,.08f);
            for(int i=0;i<2;i++)
            {
                float sign=i==0?-1:1;
                var center=obj.transform.position+new Vector3(sign*width*.25f,height*.5f,0);
                var leaf=Box(obj.transform,"Glass door leaf",center,size,glass);
                leaf.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                var handle=Box(obj.transform,"Door grip",center+new Vector3(-sign*width*.18f,0,-.065f),new Vector3(.06f,.35f,.09f),trim,false);
                handle.transform.SetParent(leaf.transform,true);
                if(i==0)controller.left=leaf.transform;else controller.right=leaf.transform;
            }
            controller.travel=width*.54f;
        }
    }
    internal sealed class HabitatDoor : MonoBehaviour
    {
        internal Transform left,right;
        internal float travel;
        Vector3 leftClosed,rightClosed;
        bool open;
        float amount;
        AudioSource sound;
        void Start()
        {
            leftClosed=left.localPosition;rightClosed=right.localPosition;
            sound=gameObject.AddComponent<AudioSource>();sound.spatialBlend=1;sound.volume=.37f;
            sound.clip=Resources.Load<AudioClip>("PixelPackersMission/DoorLatch");
        }
        bool Nearby()
        {
            var camera=Camera.main;
            if(!camera||Vector3.Distance(camera.transform.position,transform.position+Vector3.up*1.3f)>2.5f)return false;
            return Vector3.Dot(camera.transform.forward,(transform.position+Vector3.up*1.3f-camera.transform.position).normalized)>.20f;
        }
        void Update()
        {
            if(Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame&&Nearby())
            {
                var camera=Camera.main;
                if(open&&Mathf.Abs(camera.transform.position.z-transform.position.z)<.75f)return;
                open=!open;if(sound.clip)sound.Play();
            }
            amount=Mathf.MoveTowards(amount,open?1:0,Time.deltaTime*1.7f);
            float t=Mathf.SmoothStep(0,1,amount);
            left.localPosition=leftClosed-Vector3.right*(travel*t);
            right.localPosition=rightClosed+Vector3.right*(travel*t);
        }
        void OnGUI()
        {
            if(Nearby())GUI.Box(new Rect(Screen.width/2-95,Screen.height-81,190,28),open?"E  ·  Close glass door":"E  ·  Open glass door");
        }
    }
}







