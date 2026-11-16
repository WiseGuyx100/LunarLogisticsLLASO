using UnityEngine;
namespace PixelPackersMission
{
 public sealed class AutomaticSlidingDoor : MonoBehaviour
 {
  public Transform leftPanel,rightPanel,sensorTarget;
  public Vector3 leftTravel=new Vector3(-1.1f,0,0),rightTravel=new Vector3(1.1f,0,0);
  public float activationRadius=3f,openSeconds=.75f,closeDelay=1.5f;
  public Vector3 sensorCenter=new Vector3(0,1.2f,0),sensorHalfSize=new Vector3(1.2f,1.2f,.8f);
  readonly Collider[] overlaps=new Collider[64];
  Vector3 leftClosed,rightClosed;float amount,lastPresence=-999,nextSense,nextCamera;
  bool initialized;Camera view;
  public float OpenAmount=>amount;
  void Start(){Initialize();}
  void Initialize(){if(initialized||!leftPanel||!rightPanel)return;leftClosed=leftPanel.localPosition;rightClosed=rightPanel.localPosition;initialized=true;}
  void Update()
  {
   Initialize();if(!initialized)return;
   if(Time.time>=nextSense)
   {
    nextSense=Time.time+.05f;
    if(!view||Time.time>=nextCamera){view=Camera.main;nextCamera=Time.time+1;}
    var target=sensorTarget?sensorTarget:(view?view.transform:null);
    var center=transform.TransformPoint(sensorCenter);
    bool occupied=target&&(target.position-center).sqrMagnitude<activationRadius*activationRadius;
    int count=Physics.OverlapBoxNonAlloc(center,Vector3.Scale(sensorHalfSize,transform.lossyScale),overlaps,transform.rotation,~0,QueryTriggerInteraction.Ignore);
    if(count==overlaps.Length)occupied=true;
    for(int i=0;i<count&&!occupied;i++){var c=overlaps[i];if(!c.transform.IsChildOf(transform)&&(c is CharacterController||c.attachedRigidbody))occupied=true;}
    if(occupied)lastPresence=Time.time;
   }
   float next=Mathf.MoveTowards(amount,Time.time-lastPresence<closeDelay?1:0,Time.deltaTime/Mathf.Max(.1f,openSeconds));
   if(next==amount)return;amount=next;float t=Mathf.SmoothStep(0,1,amount);
   leftPanel.localPosition=leftClosed+leftTravel*t;rightPanel.localPosition=rightClosed+rightTravel*t;
  }
 }
}
