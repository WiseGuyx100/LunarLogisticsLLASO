using UnityEngine;

namespace PixelPackersMission
{
    // Continuous, attached engine cores; downloaded flame VFX adds turbulent edges.
    public sealed class RocketExhaust : MonoBehaviour
    {
        Transform[] jets;
        public static RocketExhaust Create(Transform rocket)
        {
            var root=new GameObject("Attached rocket engine flames");root.transform.SetParent(rocket,false);
            var effect=root.AddComponent<RocketExhaust>();effect.Build();return effect;
        }
        void Build()
        {
            jets=new Transform[13];
            var shader=Resources.Load<Shader>("PixelPackersMission/EngineFlame");
            if(!shader){Debug.LogError("EngineFlame shader missing. Import the mission update shader.");return;}
            var mat=new Material(shader);
            for(int i=0;i<jets.Length;i++)
            {
                var g=new GameObject("Engine flame "+i,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(transform,false);
                float angle=i*Mathf.PI*2/12;
                g.transform.localPosition=i==12?Vector3.zero:new Vector3(Mathf.Cos(angle)*1.35f,0,Mathf.Sin(angle)*1.35f);
                var v=new Vector3[48];var colors=new Color[48];var triangles=new int[216];
                for(int r=0;r<4;r++)for(int s=0;s<12;s++)
                {
                    float a=s*Mathf.PI*2/12;float radius=new[]{.19f,.28f,.23f,.015f}[r];
                    v[r*12+s]=new Vector3(Mathf.Cos(a)*radius,-new[]{0f,.9f,3.4f,7.8f}[r],Mathf.Sin(a)*radius);
                    colors[r*12+s]=new[]{new Color(3,3,2,.95f),new Color(2,1.1f,.35f,.85f),new Color(1.8f,.45f,.07f,.6f),new Color(.8f,.1f,.02f,0)}[r];
                }
                int n=0;
                for(int r=0;r<3;r++)for(int s=0;s<12;s++)
                {int a=r*12+s,b=r*12+(s+1)%12;foreach(int t in new[]{a,b,a+12,b,b+12,a+12})triangles[n++]=t;}
                var mesh=new Mesh{name="Engine jet core"};mesh.vertices=v;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();
                g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=mat;jets[i]=g.transform;
            }
            var prefab=Resources.Load<GameObject>("PixelPackersMission/LaunchFlame");
            if(prefab)
            {
                var flame=Instantiate(prefab,transform);flame.transform.localPosition=Vector3.zero;
                flame.transform.localRotation=Quaternion.Euler(180,0,0);flame.transform.localScale=new Vector3(2.6f,4,2.6f);
            }
            var light=new GameObject("Engine light").AddComponent<Light>();light.transform.SetParent(transform,false);
            light.transform.localPosition=Vector3.down*1.2f;light.color=new Color(1,.55f,.2f);light.range=20;light.intensity=5;
        }
        void Update()
        {
            if(jets==null)return;
            for(int i=0;i<jets.Length;i++)if(jets[i])jets[i].localScale=new Vector3(1, .93f+.12f*Mathf.PerlinNoise(i*3.7f,Time.time*17),1);
        }
    }
}
