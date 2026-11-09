using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VelvetLoto {
 public sealed class LotoGame : MonoBehaviour {
  SalonWorld world;SalonUI ui;SalonAudio sound;LotoRules rules;MatchState state;Problem[] bank;
  LanTransport net;readonly Dictionary<int,int> peerSeats=new Dictionary<int,int>();
  bool training,busy,paused;int localSeat,pendingSlot=-1,lastRound;GameObject held;LineRenderer circle;
  CellHit hovered;float circleTime,elapsed,broadcastTimer,hudTimer;float[] botTimers=new float[4];float pitch=31,yaw;int drawnPoints,feedbackSlot=-1;bool pencilAttempted;const float PencilDuration=.38f,PencilStartDelay=.10f;readonly Vector3[] pencilPoints=new Vector3[97];
  string networkError="";bool smoke,benchmark;int smokeStep;float smokeTime;string captureDir;
  void Start(){
   Application.runInBackground=true;Application.targetFrameRate=60;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   bank=JsonUtility.FromJson<ProblemBank>(Resources.Load<TextAsset>("Expressions").text).problems;
   world=new SalonWorld();world.Create();ui=new SalonUI();ui.Create(this);sound=new SalonAudio(gameObject);
   var ring=new GameObject("Animated red pencil circle");circle=ring.AddComponent<LineRenderer>();circle.useWorldSpace=true;circle.widthMultiplier=.0022f;circle.widthCurve=new AnimationCurve(new Keyframe(0,.65f),new Keyframe(.18f,1.12f),new Keyframe(.43f,.82f),new Keyframe(.72f,1.08f),new Keyframe(1,.62f));circle.material=Resources.Load<Material>("Pencil");circle.startColor=circle.endColor=new Color(.68f,.025f,.025f,.94f);circle.numCapVertices=3;circle.enabled=false;
   string[] args=Environment.GetCommandLineArgs();smoke=args.Contains("-lotoSmoke");int ci=Array.IndexOf(args,"-captureDir");captureDir=ci>=0?args[ci+1]:Application.persistentDataPath;
   if(!args.Contains("-screen-width")&&Screen.width>1280)Screen.SetResolution(1280,720,FullScreenMode.Windowed);
   if(smoke){var test=gameObject.AddComponent<LotoSmoke>();test.game=this;test.directory=captureDir;}
   benchmark=args.Contains("-lotoBenchmark");if(benchmark){var probe=gameObject.AddComponent<LotoPerformanceProbe>();probe.game=this;}
   if(args.Contains("-lotoNetHost")||args.Contains("-lotoNetClient")){var probe=gameObject.AddComponent<LotoNetProbe>();probe.game=this;probe.host=args.Contains("-lotoNetHost");}
  }
  public void Training(){ResetSession();training=true;rules=new LotoRules(Environment.TickCount,bank);rules.Join(0,ui.playerName.text);rules.Join(1,"Александра");rules.Join(2,"Михаил");rules.Join(3,"София");localSeat=0;StartMatch();}
  public void Host(){ResetSession();rules=new LotoRules(Environment.TickCount,bank);rules.Join(0,ui.playerName.text);localSeat=0;try{net=new LanTransport();net.Host();}catch(Exception e){networkError="Не удалось открыть стол: "+e.Message;net?.Dispose();net=null;}
   ui.menu.SetActive(false);ui.lobby.SetActive(true);ui.address.gameObject.SetActive(false);ui.lobby.transform.Find("Подключиться").gameObject.SetActive(false);ui.start.gameObject.SetActive(net!=null);RefreshLobby();
  }
  public void Join(string address){if(net!=null)net.Dispose();net=new LanTransport();net.Connect(address.Trim());ui.roomStatus.text="Подключаемся к столу…";ui.start.gameObject.SetActive(false);}
  public void StartMatch(){if(rules==null)return;if(!training&&rules.state.seats.Count(p=>p.active)<2){ui.roomStatus.text="Для сетевой партии нужны хотя бы два игрока.\nЖдём подключения.";return;}
   rules.Start(ui.difficulty.value);for(int i=1;i<4;i++)botTimers[i]=UnityEngine.Random.Range(5f,12f);state=rules.state;BeginView();Broadcast();
  }
  void BeginView(){ui.menu.SetActive(false);ui.lobby.SetActive(false);ui.hud.SetActive(true);ui.end.SetActive(false);ui.pause.SetActive(false);paused=false;lastRound=state.seats[localSeat].round;world.UpdateTickets(state);world.RestoreCovered(state);StartCoroutine(Draw());}
  public void Back(){ResetSession();ui.menu.SetActive(true);ui.lobby.SetActive(false);ui.hud.SetActive(false);ui.end.SetActive(false);ui.pause.SetActive(false);ui.lobby.transform.Find("Подключиться").gameObject.SetActive(true);}
  void ResetSession(){StopAllCoroutines();net?.Dispose();net=null;rules=null;state=null;peerSeats.Clear();training=false;busy=false;paused=false;elapsed=0;pendingSlot=-1;lastRound=0;networkError="";if(held)world.ReleaseBarrel(held);held=null;world.ClearPlaced();hovered=null;circle.enabled=false;sound.StopPencil();}
  public void TogglePause(){if(state==null||!state.started)return;paused=!paused;ui.pause.SetActive(paused);hovered=null;circle.enabled=false;sound.StopPencil();}
  public void SetSound(bool on){sound.Mute(!on);}
  void Update(){
   sound.Tick(Time.unscaledDeltaTime);
   PumpNetwork();
   bool playing=state!=null&&state.started;
   if(Input.GetKeyDown(KeyCode.Escape)){if(playing)TogglePause();else if(ui.lobby.activeSelf)Back();}
   if(!paused){
    float mx=Input.mousePosition.x/Mathf.Max(1,Screen.width),my=Input.mousePosition.y/Mathf.Max(1,Screen.height);
    float targetPitch=playing?(smoke||benchmark?43:Mathf.Lerp(59,18,Mathf.SmoothStep(0,1,my))):27;
    float targetYaw=playing?(smoke||benchmark?0:(mx-.5f)*17): -10;
    pitch=Mathf.Lerp(pitch,targetPitch,1-Mathf.Exp(-Time.deltaTime*4));yaw=Mathf.Lerp(yaw,targetYaw,1-Mathf.Exp(-Time.deltaTime*3));
    var seatRotation=Quaternion.Euler(0,localSeat*90,0);world.camera.transform.position=seatRotation*new Vector3(0,1.88f,-2.40f);world.camera.transform.rotation=seatRotation*Quaternion.Euler(pitch,yaw,0);
   }
   if(!playing)return;
   if(rules!=null&&state.winner<0&&(!training||!paused)){rules.state.elapsed+=Time.deltaTime;if(training&&!benchmark)Bots();}
   if(net!=null&&net.IsHost){broadcastTimer+=Time.deltaTime;if(broadcastTimer>.25f){broadcastTimer=0;Broadcast();}}
   hudTimer-=Time.unscaledDeltaTime;if(hudTimer<=0){UpdateHUD();hudTimer=.12f;}
   if(state.winner>=0){if(!busy&&!ui.end.activeSelf)ShowWinner();return;}
   if(!paused&&!busy){Hover();if(Input.GetKeyDown(KeyCode.Space))Discard();if(Input.GetMouseButtonDown(0)&&hovered!=null&&!EventSystem.current.IsPointerOverGameObject())Submit(hovered.slot);}
   else {circle.enabled=false;sound.StopPencil();}
   if(held&&!busy){held.transform.position=HoldPosition()+world.camera.transform.up*(Mathf.Sin(Time.time*1.6f)*.004f);held.transform.rotation=HoldRotation()*Quaternion.Euler(0,Mathf.Sin(Time.time*.8f)*2,0);}
   if(hovered!=null&&circle.enabled&&circleTime<1){circleTime=Mathf.Min(1,circleTime+Time.deltaTime/PencilDuration);int count=Mathf.Max(2,1+Mathf.FloorToInt(circleTime*96));if(count!=drawnPoints){circle.positionCount=count;for(int i=0;i<count;i++)circle.SetPosition(i,pencilPoints[i]);drawnPoints=count;}if(!pencilAttempted&&circleTime*PencilDuration>=PencilStartDelay){pencilAttempted=true;sound.TryBeginPencil();}}
  }
  void Hover(){CellHit hit=null;if(smoke){if(feedbackSlot>=0)hit=world.cells[localSeat,feedbackSlot].GetComponent<CellHit>();}else if(!EventSystem.current.IsPointerOverGameObject()){var ray=world.camera.ScreenPointToRay(Input.mousePosition);if(Physics.Raycast(ray,out var rh,10)){var c=rh.collider.GetComponent<CellHit>();if(c&&c.seat==localSeat&&state.seats[localSeat].cells[c.slot]>0&&!state.seats[localSeat].covered[c.slot])hit=c;}}
   if(hit!=hovered){hovered=hit;circleTime=0;drawnPoints=0;pencilAttempted=false;sound.StopPencil();if(hit){for(int i=0;i<97;i++){float a=-Mathf.PI*.7f+i/96f*Mathf.PI*2.04f;float pressure=.0012f*Mathf.Sin(a*7)+.0006f*Mathf.Sin(a*13);pencilPoints[i]=hit.transform.TransformPoint(new Vector3(Mathf.Cos(a)*(.058f+pressure),.006f,Mathf.Sin(a)*(.057f+pressure)));}circle.positionCount=0;}}circle.enabled=hit!=null;
  }
  public void Discard(){if(!busy&&!paused&&state!=null&&state.started&&state.winner<0)Submit(-1);}
  void Submit(int slot){if(busy)return;pendingSlot=slot;busy=true;circle.enabled=false;
   if(rules!=null){int old=state.seats[localSeat].round;if(rules.Act(localSeat,old,slot)){ObserveRound(old);Broadcast();}else busy=false;}
   else if(net!=null){net.Send(0,JsonUtility.ToJson(new Wire{type="act",seat=localSeat,round=state.seats[localSeat].round,slot=slot}));StartCoroutine(ActionTimeout(state.seats[localSeat].round));}
  }
  IEnumerator ActionTimeout(int round){yield return new WaitForSecondsRealtime(6);if(state!=null&&state.seats[localSeat].round==round&&busy){busy=false;ui.notice.text="Нет ответа от ведущего. Проверьте соединение.";}}
  void ObserveRound(int oldRound){var p=state.seats[localSeat];if(p.round!=oldRound||state.winner>=0){bool accepted=pendingSlot>=0&&p.covered[pendingSlot];int slot=pendingSlot;pendingSlot=-1;lastRound=p.round;StartCoroutine(Resolve(slot,accepted));}}
  Vector3 HoldPosition(){return world.camera.transform.TransformPoint(new Vector3(.24f,-.04f,.61f));}
  Quaternion HoldRotation(){return world.camera.transform.rotation*Quaternion.Euler(-90,0,-5);}
  IEnumerator Draw(){busy=true;hovered=null;circle.enabled=false;ui.problem.text="· · ·";ui.subtitle.text="НОВЫЙ БОЧОНОК";sound.Play("rustle");
   Vector3 bagBase=world.bag.localPosition;Quaternion bagRot=world.bag.localRotation;
   float t=0;while(t<.58f){t+=Time.deltaTime;world.bag.localRotation=bagRot*Quaternion.Euler(Mathf.Sin(t*37)*1.6f,0,Mathf.Sin(t*29)*2.1f);world.bag.localPosition=bagBase+new Vector3(Mathf.Sin(t*41)*.004f,0,0);yield return null;}world.bag.localRotation=bagRot;world.bag.localPosition=bagBase;
   held=world.Barrel(0,true,world.bag.position+Vector3.up*.5f,.067f);Vector3 origin=held.transform.position;t=0;sound.Play("turn");
   while(t<1){t+=Time.deltaTime/.43f;float u=Mathf.SmoothStep(0,1,t);held.transform.position=Vector3.Lerp(origin,HoldPosition(),u)+Vector3.up*Mathf.Sin(u*Mathf.PI)*.17f;held.transform.rotation=Quaternion.Slerp(Quaternion.identity,HoldRotation(),u);yield return null;}
   ui.problem.text=state.seats[localSeat].expression;ui.subtitle.text="ВЫЧИСЛИТЕ ЗНАЧЕНИЕ БОЧОНКА";busy=false;
  }
  IEnumerator Resolve(int slot,bool accepted){busy=true;hovered=null;circle.enabled=false;world.UpdateTickets(state);world.RestoreCovered(state,accepted?localSeat:-1,accepted?slot:-1);
   if(held){Vector3 from=held.transform.position;Quaternion rotation=held.transform.rotation;Vector3 to=accepted?world.cells[localSeat,slot].position+Vector3.up*.039f:world.bag.position+new Vector3(.65f,.5f,0);
    if(accepted){world.RevealBarrel(held,state.seats[localSeat].cells[slot]);sound.Play("turn");}else sound.Play("rustle");
    float t=0;while(t<1){t+=Time.deltaTime/(accepted?.64f:.40f);float u=Mathf.SmoothStep(0,1,t);held.transform.position=Vector3.Lerp(from,to,u)+Vector3.up*Mathf.Sin(u*Mathf.PI)*.2f;held.transform.rotation=accepted?Quaternion.Slerp(rotation,world.BarrelOnTicketRotation(localSeat),u):rotation*Quaternion.Euler(0,t*150,t*110);held.transform.localScale=Vector3.one*Mathf.Lerp(.067f,accepted?.061f:0,u);yield return null;}
    if(accepted)sound.Play("wood");world.ReleaseBarrel(held);held=null;
   }
   world.RestoreCovered(state);ui.notice.text=state.seats[localSeat].notice;yield return new WaitForSeconds(.24f);
   if(state.winner>=0){busy=false;ShowWinner();}else yield return Draw();
  }
  void Bots(){for(int s=1;s<4;s++){botTimers[s]-=Time.deltaTime;if(botTimers[s]>0)continue;var p=state.seats[s];int slot=Array.FindIndex(p.cells,v=>v==p.answer);if(slot>=0&&p.covered[slot])slot=-1;if(UnityEngine.Random.value<.08f)slot=-1;rules.Act(s,p.round,slot);botTimers[s]=UnityEngine.Random.Range(5.5f,13f)+(state.difficulty==3?4:0);world.RestoreCovered(state);}}
  void UpdateHUD(){var p=state.seats[localSeat];ui.progress.text=$"{p.solved} / 15   ·   {TimeSpan.FromSeconds(state.elapsed):mm\\:ss}";for(int s=0;s<4;s++)ui.seatLabels[s].text=state.seats[s].active?$"{(s==localSeat?"Вы":state.seats[s].name)}   {state.seats[s].solved} / 15":"Свободное место";if(!busy)ui.notice.text=string.IsNullOrEmpty(networkError)?(hovered!=null?"Поставить бочонок · "+p.cells[hovered.slot]:p.notice):networkError;}
  void ShowWinner(){ui.end.SetActive(true);ui.pause.SetActive(false);var w=state.seats[state.winner];ui.results.text=(state.winner==localSeat?"Вы закрыли билет!":w.name+" закрывает билет!")+"\n\n"+string.Join("\n",state.seats.Where(p=>p.active).OrderByDescending(p=>p.solved).Select(p=>$"{p.name}   ·   {p.solved}/15   ·   ошибок: {p.mistakes}"));sound.Play("win");}
  void RefreshLobby(){if(rules==null)return;string ips=string.Join("  /  ",Dns.GetHostAddresses(Dns.GetHostName()).Where(ip=>ip.AddressFamily==AddressFamily.InterNetwork).Select(ip=>ip.ToString()));ui.roomStatus.text=string.IsNullOrEmpty(networkError)?"IP ведущего: "+ips+"\n\n"+string.Join("\n",rules.state.seats.Select((p,i)=>$"{i+1}.  {p.name}")):networkError;}
  void Broadcast(){if(net!=null&&net.IsHost&&rules!=null)net.Broadcast(JsonUtility.ToJson(new Wire{type="state",state=rules.state}));}
  void PumpNetwork(){if(net==null)return;while(net.inbox.TryDequeue(out var msg)){
    if(msg.line.StartsWith("ERROR:")){networkError=msg.line.Substring(6);ui.roomStatus.text=networkError;continue;}
    if(msg.line=="CONNECTED"){if(!net.IsHost)net.Send(0,JsonUtility.ToJson(new Wire{type="join",name=ui.playerName.text}));continue;}
    if(msg.line=="DISCONNECTED"){
     if(net.IsHost&&peerSeats.TryGetValue(msg.peer,out int left)){if(!rules.state.started){rules.state.seats[left]=new SeatState();peerSeats.Remove(msg.peer);RefreshLobby();}else rules.state.seats[left].name+=" (вышел)";Broadcast();}
     else if(!net.IsHost){networkError="Ведущий отключился. Вернитесь в меню через Esc.";ui.roomStatus.text=networkError;busy=false;}
     continue;
    }
    Wire wire;try{wire=JsonUtility.FromJson<Wire>(msg.line);}catch{continue;}if(wire==null)continue;
    if(net.IsHost){
     if(wire.type=="join"){
      if(peerSeats.ContainsKey(msg.peer))continue;int seat=Array.FindIndex(rules.state.seats,p=>!p.active);
      if(rules.state.started||seat<0){net.Send(msg.peer,JsonUtility.ToJson(new Wire{type="error",name="Стол занят или партия уже началась."}));continue;}
      peerSeats[msg.peer]=seat;rules.Join(seat,wire.name);net.Send(msg.peer,JsonUtility.ToJson(new Wire{type="welcome",seat=seat}));RefreshLobby();Broadcast();
     }else if(wire.type=="act"&&peerSeats.TryGetValue(msg.peer,out int seat)){if(rules.Act(seat,wire.round,wire.slot)){world.UpdateTickets(rules.state);world.RestoreCovered(rules.state);Broadcast();}}
    }else{
     if(wire.type=="welcome"){localSeat=wire.seat;ui.address.gameObject.SetActive(false);ui.lobby.transform.Find("Подключиться").gameObject.SetActive(false);}
     if(wire.type=="error"){networkError=wire.name;ui.roomStatus.text=wire.name;}
     if(wire.type=="state"&&wire.state!=null){bool beginning=state==null||!state.started;int old=state==null?0:state.seats[localSeat].round;state=wire.state;
      if(state.started){if(beginning)BeginView();else{world.UpdateTickets(state);if(state.seats[localSeat].round!=old||state.winner==localSeat&&pendingSlot>=0)ObserveRound(old);else if(!busy)world.RestoreCovered(state);}}
      else ui.roomStatus.text="За столом:\n"+string.Join("\n",state.seats.Select(p=>p.name))+"\n\nОжидаем начала партии.";
     }
    }
   }}
  public void SmokeMove(){var p=state.seats[localSeat];int value=bank.First(q=>q.expression==p.expression).value;Submit(Array.FindIndex(p.cells,v=>v==value));}
  public int SmokeSolved(){return state.seats[0].solved;}
  public MatchState ProbeState=>state??rules?.state;
  public Camera GetSalonCamera()=>world.camera;
  public bool ProbeReady=>!busy;
  public void ProbeJoin(){ui.ShowJoin();Join("127.0.0.1");}
  public void BenchmarkRefresh(bool crowded){if(crowded){for(int s=0;s<4;s++){int filled=0;for(int i=0;i<27;i++)state.seats[s].covered[i]=state.seats[s].cells[i]>0&&filled++<14;state.seats[s].solved=14;}}world.RestoreCovered(state);}
  public int BenchmarkCreated=>world.CreatedBarrels;
  public void ProbeStartCircle(){feedbackSlot=Array.FindIndex(state.seats[localSeat].cells,n=>n>0);}
  public void ProbeSwitchCircle(int index){feedbackSlot=Enumerable.Range(0,27).Where(i=>state.seats[localSeat].cells[i]>0).ElementAt(index%15);}
  public void ProbeStopCircle(){feedbackSlot=-1;hovered=null;circle.enabled=false;sound.StopPencil();}
  public int ProbeCirclePoints=>circle.positionCount;
  public bool ProbePencilPlaying=>sound.PencilPlaying;
  public int ProbePencilCount=>sound.PencilPlayCount;
  public bool ProbeBarrelDirections(){for(int s=0;s<4;s++){var o=world.Barrel(6,false,Vector3.zero);o.transform.rotation=world.BarrelOnTicketRotation(s);var answer=o.GetComponent<BarrelView>().answer.transform;bool good=Vector3.Dot(answer.up,world.tickets[s].forward)>.999f&&Vector3.Dot(answer.TransformDirection(Vector3.back),Vector3.up)>.999f;world.ReleaseBarrel(o);if(!good)return false;}return true;}
  void OnApplicationQuit(){net?.Dispose();}
 }
 public sealed class LotoSmoke : MonoBehaviour {
  public LotoGame game;public string directory;
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();var cam=game.GetSalonCamera();var rt=new RenderTexture(1600,900,24);rt.antiAliasing=4;var previous=RenderTexture.active;var previousTarget=cam.targetTexture;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var t=new Texture2D(1600,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,name+".png"),t.EncodeToPNG());cam.targetTexture=previousTarget;RenderTexture.active=previous;rt.Release();Destroy(rt);Destroy(t);}
  IEnumerator Start(){
   System.IO.Directory.CreateDirectory(directory);yield return new WaitForSeconds(2);yield return Capture("00-menu");yield return new WaitForSeconds(1);game.Training();yield return new WaitForSeconds(3);yield return Capture("01-salon");
   game.ProbeStartCircle();yield return new WaitForSeconds(.18f);int partial=game.ProbeCirclePoints;bool playing=game.ProbePencilPlaying;yield return Capture("03-pencil-partial");yield return new WaitForSeconds(.4f);int full=game.ProbeCirclePoints;bool stopped=!game.ProbePencilPlaying;yield return Capture("04-pencil-complete");
   if(partial<=2||partial>=full||full!=97||!playing||!stopped)Debug.LogError($"LOTO_FEEDBACK_FAIL partial={partial} full={full} playing={playing} stopped={stopped}");else Debug.Log($"LOTO_FEEDBACK_PASS partial={partial} full={full} short recorded pencil");
   game.ProbeStopCircle();yield return new WaitForSeconds(.5f);int before=game.ProbePencilCount;for(int i=0;i<10;i++){game.ProbeSwitchCircle(i);yield return new WaitForSeconds(.035f);}game.ProbeStopCircle();int rapid=game.ProbePencilCount-before;
   yield return new WaitForSeconds(.05f);game.ProbeStartCircle();yield return new WaitForSeconds(.14f);int stable=game.ProbePencilCount-before;game.ProbeStopCircle();yield return new WaitForSeconds(.04f);game.ProbeStartCircle();yield return new WaitForSeconds(.14f);int cooled=game.ProbePencilCount-before;game.ProbeStopCircle();
   if(rapid!=0||stable!=1||cooled!=1)Debug.LogError($"LOTO_SOUND_FAIL rapid={rapid} stable={stable} after_reentry={cooled}");else Debug.Log("LOTO_SOUND_PASS rapid switches silent, stable hover plays once, cooldown prevents retrigger");
   Debug.Log(game.ProbeBarrelDirections()?"LOTO_ORIENTATION_PASS: number faces upright toward all four ticket owners":"LOTO_ORIENTATION_FAIL");game.SmokeMove();yield return new WaitForSeconds(2);yield return Capture("02-placed");Debug.Log("LOTO_SMOKE_SUCCESS: placed="+game.SmokeSolved());yield return new WaitForSeconds(1);Application.Quit();
  }
 }
 public sealed class LotoNetProbe : MonoBehaviour {
  public LotoGame game;public bool host;
  IEnumerator Start(){yield return new WaitForSecondsRealtime(1);if(host)game.Host();else game.ProbeJoin();float until=Time.realtimeSinceStartup+40;
   if(host){while(Time.realtimeSinceStartup<until&&(game.ProbeState==null||game.ProbeState.seats.Count(p=>p.active)<4))yield return null;if(game.ProbeState.seats.Count(p=>p.active)!=4){Debug.LogError("LOTO_E2E_FAIL: clients did not join");Application.Quit(2);yield break;}game.StartMatch();}
   while(Time.realtimeSinceStartup<until&&(game.ProbeState==null||!game.ProbeState.started||!game.ProbeReady))yield return null;
   if(game.ProbeState==null||!game.ProbeState.started){Debug.LogError("LOTO_E2E_FAIL: match did not start");Application.Quit(3);yield break;}
   game.SmokeMove();while(Time.realtimeSinceStartup<until&&!game.ProbeState.seats.Where(p=>p.active).All(p=>p.solved==1))yield return null;
   bool good=game.ProbeState.seats.Count(p=>p.active)==4&&game.ProbeState.seats.All(p=>p.solved==1);
   if(!host)good&=game.ProbeState.seats.All(p=>p.answer==0);
   Debug.Log(good?"LOTO_E2E_PASS: four players, four authoritative placements, synchronized cards, hidden answers":"LOTO_E2E_FAIL: divergent state");yield return new WaitForSecondsRealtime(host?4:1);Application.Quit(good?0:4);
  }
 }
}
