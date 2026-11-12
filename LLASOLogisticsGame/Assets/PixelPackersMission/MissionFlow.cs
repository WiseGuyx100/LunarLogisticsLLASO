using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PixelPackersMission
{
    internal sealed class MissionFlow : MonoBehaviour
    {
        const string Moon="Moon Test";
        static MissionFlow flow;
        GameObject rocket, player, moonActor;
        Camera shot, playCamera;
        AudioSource audioTrack;
        Vector3 pad, basePosition, launchConsole;
        float fade;
        bool busy, skip, hasLaunch;
        Material metal, dark, bright;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(flow)return;
            flow=new GameObject("Pixel Packers mission").AddComponent<MissionFlow>();
            DontDestroyOnLoad(flow.gameObject);
            SceneManager.sceneLoaded+=flow.Loaded;
            flow.Loaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);
        }

        void OnDestroy()
        {
            if(flow==this){SceneManager.sceneLoaded-=Loaded;flow=null;}
        }

        void Loaded(Scene scene,LoadSceneMode mode)
        {
            hasLaunch=false;
            if(scene.name==Moon){HabitatUpgrade.Apply();return;}
            if(!busy&&(scene.name=="GameTest"||scene.name=="GameTest_PixelPackers"))
                StartCoroutine(PlaceLaunch(scene));
        }

        IEnumerator PlaceLaunch(Scene scene)
        {
            for(int i=0;i<180&&!Camera.main;i++)yield return null;
            if(!scene.isLoaded||!Camera.main)yield break;
            playCamera=Camera.main;
            var terrain=Terrain.activeTerrain;
            var direction=Vector3.ProjectOnPlane(playCamera.transform.forward,Vector3.up).normalized;
            if(direction.sqrMagnitude<.1f)direction=Vector3.forward;
            pad=FindLaunchPad(playCamera,terrain,direction);
            basePosition=pad;
            metal=Surface("Launch stainless steel",new Color(.53f,.58f,.63f),.32f);
            dark=Surface("Launch heat shielding",new Color(.09f,.11f,.14f),.23f);
            bright=Surface("Launch pad marking",new Color(.89f,.84f,.6f),.0f);
            var dock=new GameObject("Earth launch site");SceneManager.MoveGameObjectToScene(dock,scene);
            Piece(dock.transform,"Launch apron",PrimitiveType.Cylinder,pad+Vector3.down*.14f,new Vector3(21,.12f,21),dark);
            for(int i=-2;i<=2;i++)Piece(dock.transform,"Pad approach stripe",PrimitiveType.Cube,pad+new Vector3(i*2,.014f,-7),new Vector3(.68f,.025f,1.8f),bright,false);
            var source=Resources.Load<GameObject>("PixelPackersMission/SuperHeavy");
            rocket=new GameObject("Earth launch vehicle");rocket.transform.SetParent(dock.transform,false);rocket.transform.position=pad;
            if(source)
            {
                var model=Instantiate(source,rocket.transform);
                model.transform.localPosition=Vector3.zero;
                var rs=model.GetComponentsInChildren<Renderer>();
                Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                float k=28f/Mathf.Max(.01f,b.size.y);
                model.transform.localScale*=k;
                model.transform.localPosition+=(pad-new Vector3(b.center.x,b.min.y,b.center.z))*k;
                foreach(var r in rs)r.sharedMaterials=r.sharedMaterials.Select(m=>m&&m.name.ToLowerInvariant().Contains("raptor")?dark:metal).ToArray();
            }
            else Piece(rocket.transform,"Booster body",PrimitiveType.Cylinder,pad+Vector3.up*14,new Vector3(4.6f,14,4.6f),metal);
            Piece(rocket.transform,"Upper stage body",PrimitiveType.Cylinder,pad+Vector3.up*36,new Vector3(4.45f,8,4.45f),metal,false);
            Piece(rocket.transform,"Upper stage nose",PrimitiveType.Sphere,pad+Vector3.up*44,new Vector3(4.44f,11,4.44f),metal,false);
            Piece(rocket.transform,"Stage join",PrimitiveType.Cylinder,pad+Vector3.up*28,new Vector3(4.55f,.35f,4.55f),dark,false);
            var hit=rocket.AddComponent<BoxCollider>();hit.size=new Vector3(6,49,6);hit.center=Vector3.up*24.5f;
            // Put the interaction on the player's side of any warehouse fence.
            var approach=Vector3.ProjectOnPlane(pad-playCamera.transform.position,Vector3.up).normalized;
            launchConsole=playCamera.transform.position+approach*2.2f;
            launchConsole.y=Ground(launchConsole,terrain,playCamera.transform.position.y-1.6f);
            Piece(dock.transform,"Launch control pedestal",PrimitiveType.Cube,launchConsole+Vector3.up*.50f,new Vector3(.32f,1,.32f),dark,false);
            Piece(dock.transform,"Launch control button",PrimitiveType.Cube,launchConsole+Vector3.up*1.03f,new Vector3(.40f,.10f,.31f),bright,false);
            hasLaunch=true;
        }

        void Update()
        {
            var k=Keyboard.current;var mouse=Mouse.current;
            if(busy){if(k!=null&&k.escapeKey.wasPressedThisFrame)skip=true;return;}
            if(!hasLaunch||!rocket||!Application.CanStreamedLevelBeLoaded(Moon))return;
            if(k!=null&&k.lKey.wasPressedThisFrame){StartCoroutine(Fly());return;}
            if(k!=null&&k.eKey.wasPressedThisFrame&&NearLaunchConsole()){StartCoroutine(Fly());return;}
            if(!CanStart())return;
            if((k!=null&&k.eKey.wasPressedThisFrame)||(mouse!=null&&mouse.leftButton.wasPressedThisFrame))
                StartCoroutine(Fly());
        }

        bool NearLaunchConsole()=>Camera.main&&Vector3.Distance(Camera.main.transform.position,launchConsole+Vector3.up)<3.1f;
        bool CanStart()
        {
            var c=Camera.main;if(!c||Vector3.Distance(c.transform.position,pad)>38)return false;
            var direction=Cursor.lockState==CursorLockMode.Locked||Mouse.current==null?c.transform.forward:
                c.ScreenPointToRay(Mouse.current.position.ReadValue()).direction;
            return Physics.Raycast(c.transform.position,direction,out var h,65)&&h.transform.IsChildOf(rocket.transform);
        }

        IEnumerator Fly()
        {
            busy=true;skip=false;hasLaunch=false;
            playCamera=Camera.main;bool vr=playCamera&&playCamera.stereoEnabled;
            if(vr){fade=1;yield return SceneManager.LoadSceneAsync(Moon);fade=0;busy=false;yield break;}
            shot=new GameObject("Mission cinematic camera").AddComponent<Camera>();
            shot.gameObject.AddComponent<AudioListener>();DontDestroyOnLoad(shot.gameObject);
            shot.fieldOfView=52;shot.farClipPlane=3000;shot.depth=20;shot.tag="Untagged";
            audioTrack=shot.gameObject.AddComponent<AudioSource>();
            var clip=Resources.Load<AudioClip>("PixelPackersMission/Launch");
            if(clip){audioTrack.clip=clip;audioTrack.volume=.66f;audioTrack.Play();}
            SuspendPlayer();
            var plume=Particles(rocket.transform,"Engine exhaust",new Color(1,.41f,.08f),true);
            var smoke=Particles(rocket.transform,"Pad smoke",new Color(.64f,.63f,.61f),false); smoke.transform.SetParent(rocket.transform.parent,true);
            plume.Play();smoke.Play();
            var openingEye=pad+new Vector3(0,1.65f,-9);
            for(int i=0;i<8;i++)
            {
                var eye=pad+Quaternion.Euler(0,i*45,0)*new Vector3(0,0,-9);
                eye.y=pad.y+1.65f;
                if(Physics.CheckSphere(eye,.32f,~0,QueryTriggerInteraction.Ignore))continue;
                if(Physics.Linecast(eye,pad+Vector3.up*3,out var hit,~0,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(rocket.transform))continue;
                openingEye=eye;break;
            }
            for(float t=0;t<10.5f&&!skip;t+=Time.unscaledDeltaTime)
            {
                float lift=Mathf.Pow(Mathf.Max(0,t-1.4f),2)*1.45f;
                rocket.transform.position=basePosition+Vector3.up*lift;
                if(t<4.2f)
                {
                    // A stationary person on the pad looks up as the engines ignite and lift away.
                    shot.fieldOfView=70;
                    shot.transform.position=openingEye;
                    shot.transform.LookAt(rocket.transform.position+Vector3.up*Mathf.Lerp(5.7f,2,Mathf.Clamp01((t-1.4f)/2.8f)));
                }
                else
                {
                    shot.fieldOfView=52;
                    var offset=t<7.25f?new Vector3(-24,10,-33):new Vector3(32,14,-44);
                    shot.transform.position=rocket.transform.position+offset;
                    shot.transform.LookAt(rocket.transform.position+Vector3.up*22);
                }
                if(t>4.0f&&smoke.isEmitting)smoke.Stop();
                fade=Mathf.Clamp01((t-9.45f)/1.05f);
                yield return null;
            }
            fade=1;audioTrack.Stop();
            yield return SceneManager.LoadSceneAsync(Moon);
            var moonCamera=Camera.main;
            if(moonCamera){playCamera=moonCamera;SuspendPlayer();}
            var landing=GameObject.Find("Landing pad");
            var existing=landing?landing.transform.Find("Apollo Lunar Module"):null;
            var foot=new Vector3(-66,.15f,-37);
            if(existing)moonActor=existing.gameObject;
            else
            {
                moonActor=new GameObject("Lunar landing vehicle");
                Piece(moonActor.transform,"Lander body",PrimitiveType.Capsule,Vector3.up*1.7f,new Vector3(3,1.9f,3),Surface("Lander hull",Color.gray,.2f),false);
            }
            foot=existing?existing.position:foot;
            var dust=Particles(moonActor.transform,"Lunar touchdown dust",new Color(.56f,.53f,.47f),false);
            dust.transform.SetParent(null,true);dust.transform.position=foot;
            bool touched=false; var descent=Particles(moonActor.transform,"Descent exhaust",new Color(.55f,.69f,1),true);
            descent.Play();
            for(float t=0;t<7.4f&&!skip;t+=Time.unscaledDeltaTime)
            {
                float u=Mathf.Clamp01(t/6.1f);
                moonActor.transform.position=foot+Vector3.up*(26*(1-u)*(1-u));
                shot.transform.position=foot+Vector3.Lerp(new Vector3(22,9,-24),new Vector3(14,4.8f,-18),t/7.4f);
                shot.transform.LookAt(moonActor.transform.position+Vector3.up*2.5f);
                if(u>.76f&&!dust.isPlaying)dust.Play();
                if(u>=1&&!touched){touched=true;descent.Stop();PlayCue("Touchdown",.55f);}
                fade=1-Mathf.Clamp01(t/.7f);
                yield return null;
            }
            moonActor.transform.position=foot;descent.Stop();dust.Stop();Destroy(dust.gameObject,4);
            var exit=foot+new Vector3(5.3f,0,0);
            var ground=Ground(exit,Terrain.activeTerrain,foot.y);
            exit.y=Mathf.Max(foot.y,ground);
            var facing=Quaternion.LookRotation(Vector3.right);
            bool stepped=false;
            for(float t=0;t<2.6f&&!skip;t+=Time.unscaledDeltaTime)
            {
                float u=Mathf.SmoothStep(0,1,t/2.6f);
                shot.transform.SetPositionAndRotation(Vector3.Lerp(foot+new Vector3(2.0f,2.95f,0),exit+Vector3.up*1.65f,u),facing*Quaternion.Euler(8*Mathf.Sin(u*Mathf.PI),0,0)); if(t>1.55f&&!stepped){stepped=true;PlayCue("Bootstep",.48f);}
                yield return null;
            }
            if(player)
            {
                player.transform.SetPositionAndRotation(exit,facing);
                player.SetActive(true);
                if(playCamera)playCamera.transform.rotation=facing;
            }
            Destroy(shot.gameObject);shot=null;player=null;fade=0;busy=false;
        }

        void PlayCue(string name,float level){var clip=Resources.Load<AudioClip>("PixelPackersMission/"+name);if(clip&&audioTrack)audioTrack.PlayOneShot(clip,level);}
        void SuspendPlayer()
        {
            if(!playCamera)return;
            var c=playCamera.GetComponentInParent<CharacterController>();
            player=c?c.gameObject:playCamera.transform.root.gameObject;
            player.SetActive(false);
        }

        void OnGUI()
        {
            if(fade>0)
            {
                GUI.depth=-1000;var old=GUI.color;GUI.color=new Color(0,0,0,fade);
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
            }
            if(busy)GUI.Label(new Rect(18,Screen.height-38,240,27),"Esc  ·  Skip cinematic");
            else if(hasLaunch&&rocket)
            {
                bool ready=Application.CanStreamedLevelBeLoaded(Moon);
                var camera=Camera.main;
                if(camera)
                {
                    var mark=camera.WorldToScreenPoint(pad+Vector3.up*4);
                    if(mark.z>0&&mark.x>60&&mark.x<Screen.width-60&&mark.y>0&&mark.y<Screen.height)
                        GUI.Box(new Rect(mark.x-60,Screen.height-mark.y-15,120,30),"LAUNCH PAD");
                }
                var button=new Rect(Screen.width-256,18,238,38);
                if(!ready)GUI.Box(button,"Enable Moon Test in Build Profiles");
                else if(Cursor.lockState==CursorLockMode.Locked)GUI.Box(button,"L  |  Launch to Moon");
                else if(GUI.Button(button,"Launch to Moon [L]")&&!busy)StartCoroutine(Fly());
                if(NearLaunchConsole()||CanStart())GUI.Box(new Rect(Screen.width/2-140,Screen.height-92,280,34),NearLaunchConsole()?"E  |  Use launch controls":"Click or press E to launch");
            }
        }

        static Vector3 FindLaunchPad(Camera camera,Terrain terrain,Vector3 forward)
        {
            var eye=camera.transform.position;
            Vector3 best=eye+forward*26;
            best.y=Ground(best,terrain,eye.y-1.6f);
            float bestScore=float.NegativeInfinity;
            var controller=camera.GetComponentInParent<CharacterController>();
            foreach(float distance in new[]{22f,30f,40f,54f})
            foreach(float yaw in new[]{0f,22f,-22f,42f,-42f,70f,-70f,105f,-105f,180f})
            {
                var point=eye+(Quaternion.Euler(0,yaw,0)*forward)*distance;
                point.y=Ground(point,terrain,eye.y-1.6f);
                if(Mathf.Abs(point.y-(eye.y-1.6f))>2.8f)continue;
                bool blocked=Physics.CheckBox(point+Vector3.up*3,new Vector3(4,2.65f,4),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
                if(blocked)continue;
                var visiblePoint=point+Vector3.up*5;
                bool visible=!Physics.Linecast(eye,visiblePoint,out var hit,~0,QueryTriggerInteraction.Ignore)
                    || (controller&&hit.transform.IsChildOf(controller.transform));
                float score=(visible?100:0)-Mathf.Abs(yaw)*.45f-Mathf.Abs(distance-26)*.5f;
                if(score>bestScore){bestScore=score;best=point;}
            }
            return best;
        }

        static float Ground(Vector3 p,Terrain terrain,float fallback)
        {
            if(Physics.Raycast(p+Vector3.up*110,Vector3.down,out var hit,230,~0,QueryTriggerInteraction.Ignore))return hit.point.y;
            if(terrain)
            {
                var q=p-terrain.transform.position;var s=terrain.terrainData.size;
                if(q.x>=0&&q.z>=0&&q.x<=s.x&&q.z<=s.z)return terrain.SampleHeight(p)+terrain.transform.position.y;
            }
            return fallback;
        }

        internal static Material Surface(string name,Color c,float metallic)
        {
            var shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
            var mat=new Material(shader){name=name,color=c};mat.SetFloat("_Metallic",metallic);return mat;
        }
        internal static GameObject Piece(Transform p,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material,bool coll=true)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(p,false);
            g.transform.position=position;g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=material;
            if(!coll)g.GetComponent<Collider>().enabled=false;
            return g;
        }
        static ParticleSystem Particles(Transform parent,string name,Color color,bool jet)
        {
            var obj=new GameObject(name);obj.SetActive(false);obj.transform.SetParent(parent,false);
            obj.transform.localRotation=Quaternion.Euler(90,0,0);
            if(!jet)obj.transform.localPosition=Vector3.up*.22f;
            var p=obj.AddComponent<ParticleSystem>();var main=p.main;
            main.playOnAwake=false;main.loop=true;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startLifetime=jet?.8f:2.9f;main.startSpeed=jet?13:4.5f;main.startSize=jet?1.12f:2.2f;
            main.startColor=color;main.maxParticles=800;
            var em=p.emission;em.rateOverTime=jet?190:62;
            var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=jet?12:80;shape.radius=jet?1.18f:1.8f;
            if(!jet)obj.transform.localRotation=Quaternion.identity;
            var grow=p.sizeOverLifetime;grow.enabled=true;grow.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.45f),new Keyframe(1,jet?2:3.8f)));
            var tint=p.colorOverLifetime;tint.enabled=true;var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(color,0),new GradientColorKey(jet?new Color(.9f,.18f,.025f):color,1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.15f),new GradientAlphaKey(0,1)});
            tint.color=gradient;
            var sh=Shader.Find("Universal Render Pipeline/Particles/Unlit")??Shader.Find("Particles/Standard Unlit");
            var mat=new Material(sh);mat.SetFloat("_Surface",1);mat.SetFloat("_ZWrite",0);
            mat.SetInt("_SrcBlend",5);mat.SetInt("_DstBlend",10);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;
            var tex=new Texture2D(32,32);var pixels=new Color[1024];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){float a=Mathf.Clamp01(1-Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f);pixels[y*32+x]=new Color(1,1,1,a*a);}
            tex.SetPixels(pixels);tex.Apply();mat.mainTexture=tex;
            p.GetComponent<ParticleSystemRenderer>().sharedMaterial=mat;obj.SetActive(true);return p;
        }
    }
}





