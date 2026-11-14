using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class OutpostTour : MonoBehaviour
{
    public string destination;
    public bool rover;
    public bool launch;
    public Transform movingPart;
    Camera view;
    bool driving, busy; float fade;
    Vector3 cameraOffset; float lookYaw,lookPitch;
    float speed, motorPhase, boost;
    public float cruiseSpeed=7.5f, boostSpeed=15f, boostBuildSeconds=4f, maxRockStep=1.0f;
    Vector3 roverCenter, roverHalf;
    static readonly RaycastHit[] groundHits=new RaycastHit[24];

    void MeasureRover()
    {
        var bounds=new Bounds(Vector3.zero,Vector3.zero);
        foreach(var mesh in GetComponentsInChildren<MeshFilter>())
        {
            if(!mesh.sharedMesh)continue;
            var b=mesh.sharedMesh.bounds;
            for(int i=0;i<8;i++)
            {
                var v=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                bounds.Encapsulate(transform.InverseTransformPoint(mesh.transform.TransformPoint(v)));
            }
        }
        roverCenter=new Vector3(bounds.center.x,Mathf.Max(.52f,bounds.center.y),bounds.center.z);
        roverHalf=new Vector3(Mathf.Max(.55f,bounds.extents.x*.94f),Mathf.Max(.22f,bounds.extents.y-.17f),Mathf.Max(.9f,bounds.extents.z*.96f));
        roverHalf.y=Mathf.Min(roverHalf.y,roverCenter.y-.22f);
    }

    bool Blocked(Vector3 position,Quaternion rotation)
    {
        foreach(var c in Physics.OverlapBox(position+rotation*roverCenter,roverHalf,rotation,~0,QueryTriggerInteraction.Ignore))
            if(!(c is TerrainCollider)&&!c.transform.IsChildOf(transform)&&!c.transform.IsChildOf(view.transform.parent))
                return true;
        return false;
    }

    float Ground(Vector3 position)
    {
        var origin=new Vector3(position.x,transform.position.y+maxRockStep+.6f,position.z);
        int count=Physics.RaycastNonAlloc(origin,Vector3.down,groundHits,4f+maxRockStep,~0,QueryTriggerInteraction.Ignore);
        float height=float.NegativeInfinity;
        for(int i=0;i<count;i++)
        {
            var hit=groundHits[i];
            if(hit.normal.y<.5f||hit.point.y>transform.position.y+maxRockStep||
               hit.transform.IsChildOf(transform)||hit.transform.IsChildOf(view.transform.parent))continue;
            height=Mathf.Max(height,hit.point.y);
        }
        return float.IsNegativeInfinity(height)?transform.position.y:height+.08f;
    }

    bool TryExit()
    {
        foreach(var dir in new[]{transform.right,-transform.right,-transform.forward})
        {
            var root=view.transform.parent;
            float distance=Mathf.Max(roverHalf.x,roverHalf.z)+.8f;
            bool blocked=false;
            foreach(var hit in Physics.RaycastAll(transform.position+Vector3.up,dir,distance,~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(root)) blocked=true;
            if(blocked)continue;
            var point=transform.position+dir*distance;
            point.y=Ground(point)+.03f;
            if(Physics.CheckCapsule(point+Vector3.up*.32f,point+Vector3.up*1.51f,.28f,~0,QueryTriggerInteraction.Ignore))continue;
            root.position=point;
            return true;
        }
        return false;
    }

    void Update()
    {
        var k=Keyboard.current;
        if(k==null || busy)return;
        if(!view)view=Camera.main;
        if(!view)return;
        if(driving)
        {
            float z=(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0);
            float x=(k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0);
            Physics.SyncTransforms();
            bool sprint=(k.leftShiftKey.isPressed||k.rightShiftKey.isPressed)&&z>0;
            boost=Mathf.MoveTowards(boost,sprint?1:0,Time.deltaTime/(sprint?Mathf.Max(.1f,boostBuildSeconds):1.3f));
            float target=z*Mathf.Lerp(cruiseSpeed,boostSpeed,boost);
            speed=Mathf.MoveTowards(speed,target,(z==0?7:3.2f)*Time.deltaTime);
            float dt=Mathf.Min(Time.deltaTime,.1f);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(speed)*dt/.09f));
            float bump=0;
            for(int i=0;i<steps;i++)
            {
                float step=dt/steps;
                var turn=transform.rotation*Quaternion.Euler(0,x*Mathf.Clamp(speed/2,-1,1)*57*step,0);
                var next=transform.position+turn*Vector3.forward*speed*step;
                next.y=Ground(next);
                // Sample the wheel footprint so the chassis rises before a low rock
                // reaches its collision box. Tall obstacles still stop the rover.
                foreach(float side in new[]{-.72f,.72f})
                foreach(float fore in new[]{-.75f,.75f})
                {
                    var probe=next+turn*new Vector3(side*roverHalf.x,0,fore*roverHalf.z);
                    next.y=Mathf.Max(next.y,Ground(probe));
                }
                if(Mathf.Abs(next.x)>=104||Mathf.Abs(next.z)>=100||Blocked(next,turn)){bump=Mathf.Abs(speed);speed=0;break;}
                transform.SetPositionAndRotation(next,turn);
            }
            motorPhase+=Time.deltaTime*(12+Mathf.Abs(speed)*3);
            float rumble=(.009f+.003f*Mathf.Abs(speed)+.003f*bump);
            var shake=view.stereoEnabled?Vector3.zero:new Vector3(Mathf.Sin(motorPhase*.72f)*rumble*.35f,Mathf.Sin(motorPhase)*rumble,0);
            view.transform.parent.position=transform.TransformPoint(cameraOffset+shake);
            view.transform.parent.rotation=transform.rotation;
            if(Mouse.current!=null&&Cursor.lockState==CursorLockMode.Locked)
            {
                var delta=Mouse.current.delta.ReadValue()*.11f;
                lookYaw=Mathf.Clamp(lookYaw+delta.x,-110,110);lookPitch=Mathf.Clamp(lookPitch-delta.y,-55,55);
                view.transform.localRotation=Quaternion.Euler(lookPitch,lookYaw,0);
            }
            if(k.eKey.wasPressedThisFrame)
            {
                var start=view.transform.parent.position;
                if(TryExit())
                {
                    var end=view.transform.parent.position;view.transform.parent.position=start;
                    StartCoroutine(Boarding(false,end));
                }
            }
        }
        else if(k.eKey.wasPressedThisFrame && Near())
        {
            if(rover){if(!view.transform.parent)return;MeasureRover();lookYaw=lookPitch=0;float top=transform.position.y;foreach(var r in GetComponentsInChildren<Renderer>())if(r.enabled)top=Mathf.Max(top,r.bounds.max.y);cameraOffset=new Vector3(0,top-transform.position.y+.30f-view.transform.localPosition.y,.25f);StartCoroutine(Boarding(true,transform.TransformPoint(cameraOffset)));}
            else if(!string.IsNullOrEmpty(destination))StartCoroutine(Travel());
        }
    }
    bool Near()=>view && Vector3.Distance(transform.position,view.transform.position)<4;
    IEnumerator Boarding(bool entering,Vector3 end)
    {
        busy=true;speed=0;boost=0;SetWalker(false);
        var root=view.transform.parent;var start=root.position;var rotation=root.rotation;
        var eyeRotation=view.transform.localRotation;
        var finish=entering?transform.rotation:root.rotation;
        float duration=view.stereoEnabled?0:1.1f;
        for(float t=0;t<duration;t+=Time.deltaTime)
        {
            float u=Mathf.SmoothStep(0,1,t/duration);
            float lean=Mathf.Sin(u*Mathf.PI);
            root.position=Vector3.Lerp(start,end,u)+Vector3.up*(entering?-.22f:.18f)*lean;
            root.rotation=Quaternion.Slerp(rotation,finish,u);
            view.transform.localRotation=Quaternion.Slerp(eyeRotation,Quaternion.identity,u)*Quaternion.Euler(12*lean,0,(entering?-5:5)*lean);
            yield return null;
        }
        root.SetPositionAndRotation(end,finish);view.transform.localRotation=Quaternion.identity;
        driving=entering;SetWalker(!entering);busy=false;
    }
    void SetWalker(bool enabled)
    {
        var root=view.transform.parent;
        if(!root)return;
        var walker=root.GetComponent<PixelPackersLunarBlank.HabitatWalker>();
        if(walker)walker.enabled=enabled;
        var controller=root.GetComponent<CharacterController>();
        if(controller)controller.enabled=enabled;
    }
    IEnumerator Travel()
    {
        busy=true;SetWalker(false);
        var r=movingPart?movingPart.localRotation:Quaternion.identity;
        if(launch)
        {
            view.transform.parent.position=transform.position-Vector3.forward*1.45f+Vector3.up*.08f;
            view.transform.parent.rotation=transform.rotation; if(Mouse.current!=null&&Cursor.lockState==CursorLockMode.Locked){var delta=Mouse.current.delta.ReadValue()*.11f;lookYaw=Mathf.Clamp(lookYaw+delta.x,-110,110);lookPitch=Mathf.Clamp(lookPitch-delta.y,-55,55);view.transform.localRotation=Quaternion.Euler(lookPitch,lookYaw,0);}
            view.transform.localRotation=Quaternion.identity;
        }
        for(float t=0;t<1.35f;t+=Time.deltaTime)
        {
            if(movingPart&&!launch)movingPart.localRotation=r*Quaternion.Euler(85*Mathf.Clamp01(t),0,0); fade=Mathf.Clamp01((t-.72f)*1.9f);
            if(launch && movingPart)movingPart.localPosition+=Vector3.down*(.07f+t*.25f)*Time.deltaTime; if(!launch&&t>.6f)view.transform.parent.position=Vector3.MoveTowards(view.transform.parent.position,transform.position+Vector3.up*.15f,2*Time.deltaTime);
            yield return null;
        }
        SceneManager.LoadSceneAsync(destination);
    }
    void OnGUI()
    {
        if(busy&&!rover)
        {
            GUI.color=new Color(0,0,0,fade); GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); GUI.color=Color.white;
            GUI.Label(new Rect(24,24,360,35),launch?"Transferring to lunar surface…":"Entering service level…");
        }
        else if(driving||Near())
        {
            var prompt=driving?"WASD drive | Hold Shift: accelerate | E exit":rover?"E board rover":launch?"E launch preview":"E use hatch";
            GUI.Box(new Rect((Screen.width-245)/2,Screen.height-85,245,34),prompt);
        }
    }
}





