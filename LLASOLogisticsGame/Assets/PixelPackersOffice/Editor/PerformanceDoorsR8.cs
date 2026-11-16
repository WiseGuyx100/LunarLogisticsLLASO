using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using PixelPackersMission;
using Object=UnityEngine.Object;
internal static class PerformanceDoorsR8
{
 const string Folder="Assets/PixelPackersOffice/Facility/RepairAssets/";
 [MenuItem("Tools/Pixel Packers/Install Performance and Lunar Doors (R8)")]
 static void Install()
 {
  if(EditorApplication.isPlaying){EditorUtility.DisplayDialog("R8","Stop Play Mode first.","OK");return;}
  var active=SceneManager.GetActiveScene();int doors=0;
  foreach(var path in new[]{"Assets/Scenes/TestGame/GameTest.unity","Assets/LunarOutpost/Moon Test.unity","Assets/LunarOutpost/Lunar Service Level.unity"})
  {
   var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
   if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   try
   {
    SceneManager.SetActiveScene(scene);
    if(!scene.GetRootGameObjects().Any(g=>g.GetComponent<LocalLightBudget>()))new GameObject("Local lighting optimization R8",typeof(LocalLightBudget));
    if(scene.name=="Lunar Service Level")
    {
     var frames=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>()).Where(t=>t.name=="MagixBox DoorFrame").ToArray();
     if(frames.Length==0)throw new Exception("Service-level doorway frames not found.");
     foreach(var frame in frames)
     {
      var old=frame.Find("Automatic room doors R8");if(old){doors++;continue;}
      var root=new GameObject("Automatic room doors R8").transform;
      root.position=frame.position;root.rotation=frame.rotation;
      Build(root,1.78f,2.30f);
      root.SetParent(frame,true);doors++;
     }
    }
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }
   finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
  }
  SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();
  EditorUtility.DisplayDialog("R8 installed",doors+" automatic service-level doors saved. Moon habitat doors now open on approach. Local-light optimization installed in GameTest, Moon Test and Lunar Service Level. Rover collision buffers updated.","OK");
 }
 static Material Mat(string name,Color color,bool glass=false)
 {
  var p=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);
  if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}
  m.color=color;m.SetFloat("_Smoothness",glass?.65f:.35f);m.enableInstancing=true;
  if(glass){m.SetFloat("_Surface",1);m.SetFloat("_ZWrite",0);m.SetInt("_SrcBlend",5);m.SetInt("_DstBlend",10);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;}
  EditorUtility.SetDirty(m);return m;
 }
 static GameObject Part(Transform parent,string name,Vector3 p,Vector3 size,Material m)
 {
  var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;Object.DestroyImmediate(g.GetComponent<Collider>());return g;
 }
 static void Build(Transform root,float width,float height)
 {
  var metal=Mat("Door graphite R8",new Color(.16f,.20f,.24f));var glass=Mat("Door frosted glass R8",new Color(.43f,.65f,.73f,.45f),true);var stripe=Mat("Door visibility stripe R8",new Color(.75f,.85f,.87f));
  var control=root.gameObject.AddComponent<AutomaticSlidingDoor>();control.sensorHalfSize=new Vector3(width*.5f+.15f,height*.5f,.8f);control.sensorCenter=Vector3.up*(height*.5f);
  for(int i=0;i<2;i++)
  {
   float sign=i==0?-1:1;var leaf=new GameObject(i==0?"Left sliding panel":"Right sliding panel").transform;leaf.SetParent(root,false);leaf.localPosition=new Vector3(sign*width*.25f,height*.5f,0);
   var collider=leaf.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(width*.5f,height,.09f);
   var rb=leaf.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
   Part(leaf,"Frosted safety glass",Vector3.zero,new Vector3(width*.5f-.08f,height-.12f,.045f),glass).GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   foreach(float x in new[]{-width*.25f+.025f,width*.25f-.025f})Part(leaf,"Vertical frame",new Vector3(x,0,0),new Vector3(.05f,height,.09f),metal);
   foreach(float y in new[]{-height*.5f+.03f,height*.5f-.03f})Part(leaf,"Horizontal frame",new Vector3(0,y,0),new Vector3(width*.5f,.06f,.09f),metal);
   Part(leaf,"Safety visibility band",Vector3.zero,new Vector3(width*.5f-.06f,.055f,.055f),stripe);
   if(i==0)control.leftPanel=leaf;else control.rightPanel=leaf;
  }
  control.leftTravel=Vector3.left*(width*.5f+.06f);control.rightTravel=-control.leftTravel;
  Part(root,"Automatic sensor housing",new Vector3(0,height+.04f,-.07f),new Vector3(.28f,.07f,.13f),metal);
 }
 [MenuItem("Tools/Pixel Packers/Review Lunar Automatic Doors (R8)")]
 static void Review()
 {
  if(EditorApplication.isPlaying)return;
  var s=EditorSceneManager.OpenScene("Assets/LunarOutpost/Lunar Service Level.unity");
  var door=Object.FindObjectsByType<AutomaticSlidingDoor>(FindObjectsSortMode.None).First();
  Selection.activeGameObject=door.gameObject;var target=door.transform.position+Vector3.up*1.2f;
  var v=SceneView.lastActiveSceneView;v.sceneLighting=true;v.LookAtDirect(target,Quaternion.LookRotation(door.transform.forward),1.8f);v.Repaint();
 }
 [MenuItem("Tools/Pixel Packers/Test Automatic Door Safety (R8)")]
 static void TestDoors(){if(!EditorApplication.isPlaying)return;new GameObject("Door safety test R8",typeof(R8DoorCheck));}
 [MenuItem("Tools/Pixel Packers/Measure Current Scene Performance (R8)")]
 static void Measure(){if(!EditorApplication.isPlaying){EditorUtility.DisplayDialog("R8 test","Enter Play Mode, then run this command. Keep the camera still for 22 seconds.","OK");return;}new GameObject("R8 performance comparison",typeof(R8PerformanceCheck));}
}
public sealed class R8PerformanceCheck:MonoBehaviour
{
 System.Collections.IEnumerator Start()
 {
  var budget=FindFirstObjectByType<LocalLightBudget>();if(!budget)yield break;
  yield return new WaitForSecondsRealtime(2);budget.enabled=false;yield return new WaitForSecondsRealtime(2);
  var before=new System.Collections.Generic.List<float>();var after=new System.Collections.Generic.List<float>();
  float end=Time.realtimeSinceStartup+7;while(Time.realtimeSinceStartup<end){before.Add(Time.unscaledDeltaTime*1000);yield return null;}
  budget.enabled=true;yield return new WaitForSecondsRealtime(2);end=Time.realtimeSinceStartup+7;
  while(Time.realtimeSinceStartup<end){after.Add(Time.unscaledDeltaTime*1000);yield return null;}
  before.Sort();after.Sort();var report="Scene="+gameObject.scene.name+"\nBefore mean ms="+before.Average()+" p95="+before[(int)(before.Count*.95f)]+"\nAfter mean ms="+after.Average()+" p95="+after[(int)(after.Count*.95f)]+"\nActive local lights="+budget.activeLights+" duplicates suppressed="+budget.duplicateLights;
  File.WriteAllText(@"C:\Users\kadia\Documents\Codex\2026-10-13\new-chat\work\r8_performance.txt",report);Debug.Log(report);Destroy(gameObject);
 }
}

public sealed class R8DoorCheck:MonoBehaviour
{
 System.Collections.IEnumerator Start()
 {
  var door=FindFirstObjectByType<AutomaticSlidingDoor>();if(!door)yield break;
  var target=new GameObject("Temporary sensor test target");door.sensorTarget=target.transform;
  var center=door.transform.TransformPoint(door.sensorCenter);target.transform.position=center;
  yield return new WaitForSeconds(1.3f);bool opened=door.OpenAmount>.98f;
  target.transform.position=center+Vector3.up*50;
  var cargo=GameObject.CreatePrimitive(PrimitiveType.Cube);cargo.name="Temporary doorway safety probe";cargo.transform.position=center;cargo.transform.localScale=Vector3.one*.3f;cargo.AddComponent<Rigidbody>().isKinematic=true;
  yield return new WaitForSeconds(2.5f);bool held=door.OpenAmount>.98f;
  Destroy(cargo);yield return new WaitForSeconds(3);bool closed=door.OpenAmount<.02f;
  target.transform.position=center-door.transform.forward*1.2f;yield return new WaitForSeconds(1.3f);bool reverse=door.OpenAmount>.98f;
  door.sensorTarget=null;Destroy(target);
  string report="Door="+door.name+" open on approach="+opened+" holds for cargo="+held+" closes after clearance="+closed+" reverse approach="+reverse;
  File.WriteAllText(@"C:\Users\kadia\Documents\Codex\2026-10-13\new-chat\work\r8_door_test.txt",report);Debug.Log(report);Destroy(gameObject);
 }
}
