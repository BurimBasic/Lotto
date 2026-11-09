using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VelvetLoto;

public static class ProjectBuilder {
 [MenuItem("Velvet Loto/Build Windows game")]
 public static void Build(){
  PlayerSettings.companyName="Velvet Club";PlayerSettings.productName="Лото в уме";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;PlayerSettings.resizableWindow=true;
  PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);
  var shader=Shader.Find("VelvetLoto/Velvet");if(!shader)throw new Exception("Velvet shader is missing");
  var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Velvet.mat");if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,"Assets/Resources/Velvet.mat");}
  if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Pencil.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Sprites/Default")),"Assets/Resources/Pencil.mat");
  if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/PrintedInk.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("VelvetLoto/PrintedInk")),"Assets/Resources/PrintedInk.mat");
  foreach(string f in new[]{"Walnut","BarrelWood","Velvet","Paper"}){var ti=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Textures/"+f+".png");ti.wrapMode=TextureWrapMode.Repeat;ti.anisoLevel=8;ti.SaveAndReimport();}
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Velvet Loto · App",typeof(LotoGame));Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/Salon.unity");
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Salon.unity",true)};
  RunRulesChecks();RunNetworkCheck();RunChairDirectionChecks();AssetDatabase.SaveAssets();
  var args=Environment.GetCommandLineArgs();int oi=Array.IndexOf(args,"-lotoBuildOutput");string outPath=oi>=0?Path.GetFullPath(args[oi+1]):Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/Windows/VelvetLoto.exe"));Directory.CreateDirectory(Path.GetDirectoryName(outPath));
  var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,outPath,BuildTarget.StandaloneWindows64,BuildOptions.None);
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
  Debug.Log("LOTO_BUILD_SUCCESS "+outPath);
 }
 static void Check(bool value,string message){if(!value)throw new Exception("RULE TEST: "+message);}
 static void RunChairDirectionChecks(){
  for(int s=0;s<4;s++){
   Vector3 away=Quaternion.Euler(0,s*90,0)*Vector3.back;Vector3 center=away*2.52f;var chair=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Chair"),center,SalonWorld.ChairRotation(s));
   try{Vector3 back=Vector3.zero;int count=0;foreach(var mf in chair.GetComponentsInChildren<MeshFilter>())foreach(var vertex in mf.sharedMesh.vertices){var p=mf.transform.TransformPoint(vertex);if(p.y>.82f){back+=p;count++;}}Check(count>0,"chair back geometry exists");Check(Vector3.Dot(back/count-center,away)>.12f,"chair back must face away from table, seat "+s);}
   finally{UnityEngine.Object.DestroyImmediate(chair);}
  }Debug.Log("LOTO_CHAIR_DIRECTION_PASS: all four imported chair backs face away from table");
 }
 static void RunRulesChecks(){var bank=JsonUtility.FromJson<ProblemBank>(Resources.Load<TextAsset>("Expressions").text).problems;Check(bank.Length==450,"450 expressions");
  for(int n=1;n<=90;n++)for(int d=1;d<=5;d++)Check(bank.Count(p=>p.value==n&&p.level==d)==1,"expression coverage");
  for(int seed=0;seed<500;seed++){
   var rules=new LotoRules(seed,bank);var card=rules.Ticket();Check(card.Count(n=>n>0)==15,"15 numbers");Check(card.Where(n=>n>0).Distinct().Count()==15,"unique numbers");
   for(int r=0;r<3;r++)Check(card.Skip(r*9).Take(9).Count(n=>n>0)==5,"five per row");
   for(int c=0;c<9;c++){var col=Enumerable.Range(0,3).Select(r=>card[r*9+c]).Where(n=>n>0).ToArray();Check(col.Length>0,"nonempty column");Check(col.SequenceEqual(col.OrderBy(n=>n)),"ascending column");Check(col.All(n=>n>=(c==0?1:c*10)&&n<=(c==8?90:c*10+9)),"column range");}
  }
  for(int difficulty=0;difficulty<4;difficulty++){
   var r=new LotoRules(91+difficulty,bank);r.Join(0,"Tester");r.Join(1,"Other");r.Start(difficulty);var p=r.state.seats[0];int missed=p.answer,round=p.round;
   Check(!r.Act(0,round-1,0),"stale request rejected");int bad=Array.FindIndex(p.cells,n=>n>0&&n!=p.answer);r.Act(0,p.round,bad);Check(p.solved==0&&p.mistakes==1,"wrong placement removes barrel");bool returned=false;
   for(int i=0;i<180;i++){if(p.answer==missed)returned=true;r.Act(0,p.round,-1);}Check(returned,"missed value recurs");
   int moves=0;while(r.state.winner<0&&moves++<500){int slot=Array.FindIndex(p.cells,n=>n==p.answer);if(slot>=0&&p.covered[slot])slot=-1;r.Act(0,p.round,slot);}Check(r.state.winner==0&&p.solved==15,"ticket can always be completed");Check(!r.Act(0,p.round,0),"finished game immutable");
  }Debug.Log("LOTO_RULE_TESTS_PASS: 500 tickets, 4 difficulties, recurrence, stale commands, victory");
 }
 static void RunNetworkCheck(){using(var host=new LanTransport())using(var client=new LanTransport()){
   host.Host();client.Connect("127.0.0.1");bool connected=false;int peer=0;var until=DateTime.UtcNow.AddSeconds(6);
   while(DateTime.UtcNow<until&&!connected){while(host.inbox.TryDequeue(out var m))if(m.line=="CONNECTED"){connected=true;peer=m.peer;}Thread.Sleep(10);}Check(connected,"LAN connection");
   string payload="{\"type\":\"join\",\"name\":\"Тест кириллицы\"}";client.Send(0,payload);bool received=false;until=DateTime.UtcNow.AddSeconds(3);
   while(DateTime.UtcNow<until&&!received){while(host.inbox.TryDequeue(out var m))if(m.line==payload)received=true;Thread.Sleep(10);}Check(received,"UTF8 client to host");host.Send(peer,"welcome-test");received=false;until=DateTime.UtcNow.AddSeconds(3);
   while(DateTime.UtcNow<until&&!received){while(client.inbox.TryDequeue(out var m))if(m.line=="welcome-test")received=true;Thread.Sleep(10);}Check(received,"host to client");
  }Debug.Log("LOTO_NETWORK_TEST_PASS: real TCP loopback, bidirectional UTF8");}
}
