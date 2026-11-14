using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
internal static class LightingAuditR7
{
 [MenuItem("Tools/Pixel Packers/Inspect Current Lighting (R7)")]
 static void Audit(){var s=new StringBuilder();s.AppendLine("Play="+Application.isPlaying);foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None)){var r=mf.GetComponent<Renderer>();if(!r||!mf.sharedMesh)continue;var path=AssetDatabase.GetAssetPath(mf.sharedMesh);string full=mf.name;for(var t=mf.transform.parent;t;t=t.parent)full=t.name+"/"+full;if(!new[]{"Warehouse","Office","CeilingLight","Fac_","Launch","DeskLamp"}.Any(x=>full.Contains(x)))continue;s.AppendLine(full+" | "+path+" | enabled="+r.enabled+" | world="+r.bounds+" | local="+mf.transform.localPosition+" scale="+mf.transform.lossyScale);s.AppendLine("  mats="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name+":"+m.GetTexture("_BaseMap"):"null")));}
 foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))s.AppendLine("LIGHT "+l.name+" pos="+l.transform.position+" dir="+l.transform.forward+" intensity="+l.intensity+" enabled="+l.enabled);
 var button=Resources.Load<GameObject>("PixelPackersMission/LaunchButtonR6");s.AppendLine("RESOURCE button="+button+" path="+AssetDatabase.GetAssetPath(button));File.WriteAllText(@"C:\Users\kadia\Documents\Codex\2026-10-13\new-chat\work\r7_audit.txt",s.ToString());Debug.Log("R7 lighting audit written");}
 [MenuItem("Tools/Pixel Packers/Review R7/Office %&F9")]
 static void Office(){View(new Vector3(59.5f,1.8f,10),new Vector3(59.5f,1.3f,14.8f));}
 [MenuItem("Tools/Pixel Packers/Review R7/Cargo warehouse %&F10")]
 static void Cargo(){View(new Vector3(24,2,-8),new Vector3(24,3.6f,-20));}
 [MenuItem("Tools/Pixel Packers/Review R7/Facility ceiling %&F11")]
 static void Ceiling(){View(new Vector3(63.67f,6,-4),new Vector3(63.67f,8.8f,-.36f));}
 [MenuItem("Tools/Pixel Packers/Review R7/Launch control %&F12")]
 static void Launch(){var t=GameObject.Find("Rocket launch controls R7").transform;View(t.position+new Vector3(0,1.6f,-2),t.position+Vector3.up);}
 static void View(Vector3 from,Vector3 target){var v=SceneView.lastActiveSceneView;v.sceneLighting=true;v.drawGizmos=false;v.LookAtDirect(target,Quaternion.LookRotation(target-from),Vector3.Distance(from,target)*.45f);v.Repaint();}
}

