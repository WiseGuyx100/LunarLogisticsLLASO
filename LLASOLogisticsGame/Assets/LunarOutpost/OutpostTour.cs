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
            if(Mathf.Abs(z)>0)transform.Rotate(0,x*z*65*Time.deltaTime,0);
            var next=transform.position+transform.forward*z*8*Time.deltaTime;
            if(Physics.Raycast(next+Vector3.up*5,Vector3.down,out var hit,12,1,QueryTriggerInteraction.Ignore) && hit.collider is TerrainCollider)next.y=hit.point.y+.08f;
            if(Mathf.Abs(next.x)<104&&Mathf.Abs(next.z)<100)transform.position=next;
            view.transform.parent.position=transform.TransformPoint(cameraOffset);
            view.transform.parent.rotation=transform.rotation; if(Mouse.current!=null&&Cursor.lockState==CursorLockMode.Locked){var delta=Mouse.current.delta.ReadValue()*.11f;lookYaw=Mathf.Clamp(lookYaw+delta.x,-110,110);lookPitch=Mathf.Clamp(lookPitch-delta.y,-55,55);view.transform.localRotation=Quaternion.Euler(lookPitch,lookYaw,0);}
            if(k.eKey.wasPressedThisFrame)
            {
                driving=false;
                view.transform.parent.position=transform.position+transform.right*2;
                SetWalker(true);
            }
        }
        else if(k.eKey.wasPressedThisFrame && Near())
        {
            if(rover){driving=true;lookYaw=lookPitch=0;float top=transform.position.y;foreach(var r in GetComponentsInChildren<Renderer>())if(r.enabled)top=Mathf.Max(top,r.bounds.max.y);cameraOffset=new Vector3(0,top-transform.position.y+.30f-view.transform.localPosition.y,.25f);view.transform.localRotation=Quaternion.identity;SetWalker(false);}
            else if(!string.IsNullOrEmpty(destination))StartCoroutine(Travel());
        }
    }
    bool Near()=>view && Vector3.Distance(transform.position,view.transform.position)<4;
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
        if(busy)
        {
            GUI.color=new Color(0,0,0,fade); GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); GUI.color=Color.white;
            GUI.Label(new Rect(24,24,360,35),launch?"Transferring to lunar surface…":"Entering service level…");
        }
        else if(driving||Near())
        {
            var prompt=driving?"WASD drive  |  E exit":rover?"E board rover":launch?"E launch preview":"E use hatch";
            GUI.Box(new Rect((Screen.width-245)/2,Screen.height-85,245,34),prompt);
        }
    }
}



