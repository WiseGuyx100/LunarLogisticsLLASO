using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelPackersMission
{
    // Installed on the actual moving drawer/hinge, or the chair's seat anchor.
    public sealed class OfficeInteraction : MonoBehaviour
    {
        public bool chair,hinged;
        public Vector3 travel;
        public float angle=95;
        public Transform seat;
        public Renderer targetRenderer;
        static OfficeInteraction activeSeat;
        static int usedFrame=-1;
        bool open,busy;
        Vector3 closed,standing;
        Quaternion closedRotation,standingRotation;
        Transform player;
        CharacterController controller;
        bool controllerWasEnabled;
        readonly List<Behaviour> paused=new List<Behaviour>();
        void Awake(){closed=transform.localPosition;closedRotation=transform.localRotation;}
        bool Aimed(Camera camera)
        {
            if(!camera||!targetRenderer)return false;
            var b=targetRenderer.bounds;b.Expand(.12f);
            if(!b.IntersectRay(new Ray(camera.transform.position,camera.transform.forward),out float d)||d>2.4f)return false;
            // Do not interact through a wall or another cabinet.
            if(Physics.Raycast(camera.transform.position,camera.transform.forward,out var hit,d+.02f,~0,QueryTriggerInteraction.Ignore))
                return hit.transform.IsChildOf(transform)||hit.collider.GetComponent<Renderer>()==targetRenderer;
            return true;
        }
        void Update()
        {
            var camera=Camera.main;var k=Keyboard.current;
            if(busy||!camera||k==null||!k.eKey.wasPressedThisFrame||usedFrame==Time.frameCount)return;
            if(activeSeat&&activeSeat!=this)return;
            if(activeSeat==this){usedFrame=Time.frameCount;StartCoroutine(Sit(camera,false));}
            else if(Aimed(camera)){usedFrame=Time.frameCount;StartCoroutine(chair?Sit(camera,true):Move());}
        }
        IEnumerator Move()
        {
            busy=true;var a=transform.localPosition;var r=transform.localRotation;open=!open;
            var b=closed+(open?travel:Vector3.zero);var q=closedRotation*Quaternion.Euler(0,open?angle:0,0);
            for(float t=0;t<.65f;t+=Time.deltaTime){float u=Mathf.SmoothStep(0,1,t/.65f);if(hinged)transform.localRotation=Quaternion.Slerp(r,q,u);else transform.localPosition=Vector3.Lerp(a,b,u);yield return null;}
            if(hinged)transform.localRotation=q;else transform.localPosition=b;busy=false;
        }
        IEnumerator Sit(Camera camera,bool entering)
        {
            busy=true;
            if(entering)
            {
                controller=camera.GetComponentInParent<CharacterController>();
                player=controller?controller.transform:camera.transform.parent;
                if(!player||!seat){busy=false;yield break;}
                standing=player.position;standingRotation=player.rotation;
                foreach(var b in player.GetComponentsInChildren<MonoBehaviour>())
                    if(b.enabled&&(b.GetType().Name=="FPSMovement"||b.GetType().Name=="HabitatWalker"||b.GetType().Name=="WarehouseMouseLook")){paused.Add(b);b.enabled=false;}
                controllerWasEnabled=controller&&controller.enabled;
                if(controllerWasEnabled)controller.enabled=false;
                activeSeat=this;
            }
            else if(controller)
            {
                // Refuse to stand into newly placed cargo. Ignore the player's own colliders.
                var center=standing+Vector3.up*controller.height*.5f;
                foreach(var c in Physics.OverlapCapsule(center+Vector3.up*(controller.height*.5f-controller.radius),center-Vector3.up*(controller.height*.5f-controller.radius),controller.radius*.95f,~0,QueryTriggerInteraction.Ignore))
                    if(!c.transform.IsChildOf(player)){busy=false;yield break;}
            }
            var start=player.position;var rotation=player.rotation;
            var endRotation=entering?Quaternion.Euler(0,seat.eulerAngles.y,0):standingRotation;
            var eyeOffset=Quaternion.Inverse(rotation)*(camera.transform.position-player.position);
            var end=entering?seat.position-endRotation*eyeOffset:standing;
            float duration=camera.stereoEnabled?0:.85f;
            for(float t=0;t<duration;t+=Time.deltaTime)
            {
                float u=Mathf.SmoothStep(0,1,t/duration);
                player.SetPositionAndRotation(Vector3.Lerp(start,end,u)+Vector3.down*(.07f*Mathf.Sin(u*Mathf.PI)),Quaternion.Slerp(rotation,endRotation,u));
                yield return null;
            }
            player.SetPositionAndRotation(end,endRotation);
            if(!entering){foreach(var b in paused)if(b)b.enabled=true;if(controller&&controllerWasEnabled)controller.enabled=true;paused.Clear();activeSeat=null;}
            busy=false;
        }
        void OnDisable(){if(activeSeat==this){foreach(var b in paused)if(b)b.enabled=true;if(controller&&controllerWasEnabled)controller.enabled=true;paused.Clear();activeSeat=null;}}
        void OnGUI(){if(activeSeat==this||(!activeSeat&&Aimed(Camera.main)))GUI.Box(new Rect(Screen.width/2-100,Screen.height-125,200,28),chair?(activeSeat==this?"E stand up":"E sit down"):(open?"E close":"E open"));}
    }
}
