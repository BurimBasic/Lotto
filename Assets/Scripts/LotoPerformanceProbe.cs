using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.Profiling;

namespace VelvetLoto {
 public sealed class LotoPerformanceProbe:MonoBehaviour {
  public LotoGame game;
  IEnumerator Start(){
   QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
   yield return new WaitForSecondsRealtime(2);game.Training();yield return new WaitForSecondsRealtime(3);
   using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))
   using(var tris=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count")){
    foreach(bool crowded in new[]{false,true}){
     game.BenchmarkRefresh(crowded);yield return new WaitForSecondsRealtime(2);
     int created=game.BenchmarkCreated;var times=new List<float>();long totalDraws=0,totalTris=0;int count=0;float until=Time.realtimeSinceStartup+10,nextRefresh=Time.realtimeSinceStartup+.25f;
     while(Time.realtimeSinceStartup<until){yield return null;times.Add(Time.unscaledDeltaTime*1000);totalDraws+=draws.Valid?draws.LastValue:0;totalTris+=tris.Valid?tris.LastValue:0;count++;if(crowded&&Time.realtimeSinceStartup>=nextRefresh){game.BenchmarkRefresh(true);nextRefresh=Time.realtimeSinceStartup+.25f;}}
     times.Sort();float mean=times.Average();Debug.Log($"LOTO_PERF {(crowded?"crowded56_network4Hz":"fresh")} frames={count} fps={1000/mean:F1} mean_ms={mean:F2} p95_ms={times[(int)((times.Count-1)*.95f)]:F2} max_ms={times[times.Count-1]:F2} draws={totalDraws/Math.Max(1,count)} tris={totalTris/Math.Max(1,count)} resolution={Screen.width}x{Screen.height} gpu={SystemInfo.graphicsDeviceName}");if(crowded)Debug.Log($"LOTO_POOL_CHECK created_during_repeated_snapshots={game.BenchmarkCreated-created}");
    }
   }Application.Quit();
  }
 }
}
