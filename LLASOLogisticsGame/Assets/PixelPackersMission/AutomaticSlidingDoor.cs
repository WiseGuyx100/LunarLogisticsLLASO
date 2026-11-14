using UnityEngine;

namespace PixelPackersMission
{
    // Assign the two movable panels from a downloaded door model in the Inspector.
    public sealed class AutomaticSlidingDoor : MonoBehaviour
    {
        public Transform leftPanel,rightPanel;
        public Vector3 leftTravel=new Vector3(-1.1f,0,0),rightTravel=new Vector3(1.1f,0,0);
        public float activationRadius=3f,openSeconds=.75f,closeDelay=1.5f;
        Vector3 leftClosed,rightClosed;float amount,lastPresence=-999;
        void Awake()
        {
            if(!leftPanel||!rightPanel){Debug.LogWarning("Assign both sliding door panels.",this);enabled=false;return;}
            leftClosed=leftPanel.localPosition;rightClosed=rightPanel.localPosition;
        }
        void Update()
        {
            var camera=Camera.main;
            bool occupied=camera&&Vector3.Distance(camera.transform.position,transform.position)<activationRadius;
            // Keep the doorway open for cargo or a character occupying its threshold.
            foreach(var c in Physics.OverlapBox(transform.position+Vector3.up, new Vector3(1.3f,1.1f,.6f),transform.rotation,~0,QueryTriggerInteraction.Ignore))
                if(!c.transform.IsChildOf(transform)&&(c is CharacterController||c.attachedRigidbody))occupied=true;
            if(occupied)lastPresence=Time.time;
            float target=occupied||Time.time-lastPresence<closeDelay?1:0;
            amount=Mathf.MoveTowards(amount,target,Time.deltaTime/Mathf.Max(.1f,openSeconds));
            float smooth=Mathf.SmoothStep(0,1,amount);
            leftPanel.localPosition=leftClosed+leftTravel*smooth;rightPanel.localPosition=rightClosed+rightTravel*smooth;
        }
    }
}
