using System.Collections.Generic;
using UnityEngine;
namespace PixelPackersMission
{
 public sealed class LocalLightBudget : MonoBehaviour
 {
  sealed class Entry { public Light light;public bool enabled;public LightShadows shadows;public bool duplicate; }
  readonly List<Entry> entries=new List<Entry>();
  readonly List<Entry> shadowCandidates=new List<Entry>();
  Camera view;float nextUpdate,nextScan;
  public int activeLights,duplicateLights;
  void OnEnable(){nextScan=0;}
  void Scan()
  {
   Restore();entries.Clear();duplicateLights=0;
   foreach(var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
   {
    if(l.type==LightType.Directional||!l.enabled||l.lightmapBakeType==LightmapBakeType.Baked)continue;
    var e=new Entry{light=l,enabled=l.enabled,shadows=l.shadows};
    if(l.name=="Warehouse panel illumination")foreach(var other in entries)
     if(!other.duplicate&&other.light&&other.light.name==l.name&&(other.light.transform.position-l.transform.position).sqrMagnitude<.16f&&Vector3.Dot(other.light.transform.forward,l.transform.forward)>.98f){e.duplicate=true;duplicateLights++;break;}
    entries.Add(e);
   }
  }
  void Update()
  {
   if(Time.unscaledTime>=nextScan){Scan();nextScan=Time.unscaledTime+5;}
   if(Time.unscaledTime<nextUpdate)return;nextUpdate=Time.unscaledTime+.2f;
   if(!view||!view.isActiveAndEnabled)view=Camera.main;if(!view)return;
   var p=view.transform.position;shadowCandidates.Clear();activeLights=0;
   foreach(var e in entries)
   {
    var l=e.light;if(!l)continue;float distance=(l.transform.position-p).sqrMagnitude;
    bool near=distance<(l.range+22)*(l.range+22);
    l.enabled=e.enabled&&near&&!e.duplicate;
    if(!l.enabled)continue;activeLights++;
    if(e.shadows!=LightShadows.None)shadowCandidates.Add(e);
   }
   shadowCandidates.Sort((a,b)=>(a.light.transform.position-p).sqrMagnitude.CompareTo((b.light.transform.position-p).sqrMagnitude));
   int faces=0;
   foreach(var e in shadowCandidates){int cost=e.light.type==LightType.Point?6:1;bool keep=faces+cost<=8;if(keep)faces+=cost;e.light.shadows=keep?e.shadows:LightShadows.None;}
  }
  void Restore(){foreach(var e in entries)if(e.light){e.light.enabled=e.enabled;e.light.shadows=e.shadows;}}
  void OnDisable(){Restore();}
 }
}
