using System.Collections.Generic;
using UnityEngine;

namespace VelvetLoto {
 public class CellHit : MonoBehaviour { public int seat,slot; }
 public sealed class BarrelView:MonoBehaviour {public TextMesh question,answer;public int value=-1;}
 public sealed class SalonWorld {
  public Camera camera;public Transform bag;public Transform[] tickets=new Transform[4];
  public Transform[,] cells=new Transform[4,27]; public TextMesh[,] numbers=new TextMesh[4,27];
  public readonly List<GameObject> placed=new List<GameObject>();
  readonly GameObject[,] placedByCell=new GameObject[4,27];readonly Stack<GameObject> barrelPool=new Stack<GameObject>();
  readonly Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>();Transform staticRoot;public int CreatedBarrels{get;private set;}
  public Material wood,brass,velvet,paper,ink,red,felt;Material printedInk;Font serif;public Transform root;
  Dictionary<string,Material> materials=new Dictionary<string,Material>();
  Material Mat(string name,Color color,float metallic=0,float smooth=.3f,string texture=null){var m=new Material(Shader.Find("Standard")){name=name,color=color,enableInstancing=true};m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);if(texture!=null)m.mainTexture=Resources.Load<Texture2D>("Textures/"+texture);return m;}
  public void Create(){
   root=new GameObject("The Velvet Salon").transform;serif=Resources.Load<Font>("Fonts/Numbers");
   staticRoot=new GameObject("Batched architecture").transform;staticRoot.SetParent(root,false);
   serif.RequestCharactersInTexture("0123456789?МАТЕМАТИЧЕСКОЕЛОТБИ№•РХНЫЙКУбилетклуб",80,FontStyle.Normal);printedInk=new Material(Resources.Load<Material>("PrintedInk"));printedInk.mainTexture=serif.material.mainTexture;Font.textureRebuilt+=RefreshFontTexture;
   wood=Mat("Walnut",Color.white,0,.38f,"Walnut");brass=Mat("Brass",new Color(.62f,.39f,.14f),.75f,.55f);
   velvet=new Material(Resources.Load<Material>("Velvet"));velvet.enableInstancing=true;velvet.color=new Color(.32f,.045f,.075f);velvet.mainTexture=Resources.Load<Texture2D>("Textures/Velvet");
   paper=Mat("Aged cotton stock",Color.white,0,.06f,"Paper");ink=Mat("Letterpress ink",new Color(.13f,.12f,.10f));red=Mat("Carmine pencil",new Color(.66f,.08f,.075f));felt=Mat("Forest baize",new Color(.025f,.145f,.085f),0,.04f);
   materials["Walnut"]=wood;materials["Brass"]=brass;materials["Velvet"]=velvet;materials["Thread"]=Mat("Golden thread",new Color(.59f,.38f,.16f));materials["Felt"]=felt;materials["BarrelWood"]=Mat("Oiled beech",Color.white,0,.38f,"BarrelWood");
   var chairVelvet=new Material(velvet);chairVelvet.color=new Color(.035f,.15f,.12f);materials["ChairVelvet"]=chairVelvet;
   Model("Table",Vector3.zero,Quaternion.identity,Vector3.one);
   bag=Model("Sack",new Vector3(0,.92f,.10f),Quaternion.Euler(0,-18,0),Vector3.one*.85f).transform;
   for(int s=0;s<4;s++){
    float a=s*90;Vector3 pos=Quaternion.Euler(0,a,0)*new Vector3(0,0,-2.52f);
    Model("Chair",pos,ChairRotation(s),Vector3.one);
    var t=new GameObject("Ticket "+(s+1)).transform;t.SetParent(root);t.rotation=Quaternion.Euler(0,a,0);t.position=Quaternion.Euler(0,a,0)*new Vector3(0,.923f,-1.19f);tickets[s]=t;
    BuildTicket(s);
   }
   BuildRoom();
   var cam=new GameObject("First person · visible cursor");camera=cam.AddComponent<Camera>();cam.AddComponent<AudioListener>();camera.fieldOfView=52;camera.nearClipPlane=.025f;camera.farClipPlane=20;camera.allowHDR=false;camera.renderingPath=RenderingPath.Forward;camera.backgroundColor=new Color(.025f,.04f,.035f);camera.clearFlags=CameraClearFlags.SolidColor;
   camera.transform.position=new Vector3(0,1.88f,-2.40f);camera.transform.rotation=Quaternion.Euler(30,0,0);
   var handFill=new GameObject("Soft token bounce").AddComponent<Light>();handFill.transform.SetParent(camera.transform,false);handFill.transform.localPosition=new Vector3(-.2f,.2f,.1f);handFill.type=LightType.Point;handFill.range=1.7f;handFill.intensity=.55f;handFill.renderMode=LightRenderMode.ForceVertex;handFill.color=new Color(1,.90f,.77f);
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.22f,.25f,.23f);RenderSettings.ambientEquatorColor=new Color(.14f,.13f,.105f);RenderSettings.ambientGroundColor=new Color(.055f,.04f,.025f);
   RenderSettings.fog=true;RenderSettings.fogColor=new Color(.035f,.055f,.048f);RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.025f;
   QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=2;QualitySettings.pixelLightCount=1;QualitySettings.shadowDistance=7;QualitySettings.shadowCascades=0;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.Medium;
   // Batch immutable architecture and printed ticket rules. The sack and tokens stay movable.
   var batch=new List<GameObject>();foreach(var r in staticRoot.GetComponentsInChildren<MeshRenderer>())batch.Add(r.gameObject);
   foreach(var ticket in tickets)foreach(var r in ticket.GetComponentsInChildren<MeshRenderer>())if(!r.GetComponent<TextMesh>())batch.Add(r.gameObject);
   StaticBatchingUtility.Combine(batch.ToArray(),root.gameObject);
  }
  public GameObject Model(string name,Vector3 pos,Quaternion rot,Vector3 scale){if(!prefabs.TryGetValue(name,out var prefab)){prefab=Resources.Load<GameObject>("Models/"+name);prefabs[name]=prefab;}var o=Object.Instantiate(prefab,pos,rot,name=="Table"||name=="Chair"?staticRoot:root);o.name=name;o.transform.localScale=scale;foreach(var r in o.GetComponentsInChildren<Renderer>()){var ms=r.sharedMaterials;for(int i=0;i<ms.Length;i++){string n=ms[i].name.Replace(" (Instance)","");if(materials.TryGetValue(n,out var m))ms[i]=m;}r.sharedMaterials=ms;}return o;}
  public GameObject Box(string name,Vector3 pos,Vector3 scale,Material mat,Transform parent=null){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent??staticRoot,false);o.transform.localPosition=pos;o.transform.localScale=scale;var r=o.GetComponent<Renderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;Object.Destroy(o.GetComponent<Collider>());return o;}
  void RefreshFontTexture(Font f){if(f==serif&&printedInk)printedInk.mainTexture=serif.material.mainTexture;}
  TextMesh Text(string value,Vector3 pos,float size,Color color,Transform parent,Font font=null){var o=new GameObject(value);o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localRotation=Quaternion.Euler(90,0,0);var t=o.AddComponent<TextMesh>();t.font=font??serif;t.GetComponent<Renderer>().sharedMaterial=printedInk;t.text=value;t.characterSize=size/8f;t.fontSize=80;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;return t;}
  void BuildTicket(int seat){var t=tickets[seat];Box("Rounded aged ticket",Vector3.zero,new Vector3(1.37f,.006f,.67f),paper,t);
   Box("Burgundy letterpress header",new Vector3(0,.004f,.244f),new Vector3(1.30f,.001f,.123f),red,t);
   Text("М А Т Е М А Т И Ч Е С К О Е   Л О Т О",new Vector3(0,.008f,.25f),.037f,new Color(.94f,.85f,.65f),t);
   Text("БИЛЕТ № 00"+(seat+1)+"     •     БАРХАТНЫЙ КЛУБ",new Vector3(0,.008f,-.29f),.026f,new Color(.38f,.28f,.18f),t);
   for(int c=0;c<=9;c++)Box("Printed rule",new Vector3((c-4.5f)*.142f,.006f,-.045f),new Vector3(.0017f,.001f,.438f),ink,t);
   for(int r=0;r<=3;r++)Box("Printed rule",new Vector3(0,.006f,.174f-r*.146f),new Vector3(1.278f,.001f,.0017f),ink,t);
   for(int i=0;i<27;i++){
    int r=i/9,c=i%9;var cell=new GameObject("Cell "+i);cell.transform.SetParent(t,false);cell.transform.localPosition=new Vector3((c-4)*.142f,.009f,.101f-r*.146f);cells[seat,i]=cell.transform;
    var hit=cell.AddComponent<CellHit>();hit.seat=seat;hit.slot=i;var col=cell.AddComponent<BoxCollider>();col.size=new Vector3(.136f,.012f,.139f);
    numbers[seat,i]=Text("",Vector3.zero,.103f,new Color(.075f,.055f,.035f),cell.transform);
   }
  }
  public void UpdateTickets(MatchState state){for(int s=0;s<4;s++)for(int i=0;i<27;i++){var p=state.seats[s];string value=p.active&&p.cells[i]>0?p.cells[i].ToString():"";if(numbers[s,i].text!=value)numbers[s,i].text=value;}}
  public GameObject Barrel(int value,bool unknown,Vector3 pos,float scale=.065f){GameObject o;BarrelView view;
   if(barrelPool.Count>0){o=barrelPool.Pop();view=o.GetComponent<BarrelView>();o.SetActive(true);}else{
    o=Model("Barrel",pos,Quaternion.identity,Vector3.one*scale);view=o.AddComponent<BarrelView>();CreatedBarrels++;
    view.question=Text("?",new Vector3(0,.523f,0),.96f,new Color(.23f,.12f,.055f),o.transform);view.question.name="Question face";
    view.answer=Text("",new Vector3(0,-.523f,0),.73f,new Color(.20f,.10f,.04f),o.transform);view.answer.transform.localRotation=Quaternion.Euler(-90,0,0);view.answer.name="Answer face";
   }
   o.transform.position=pos;o.transform.localScale=Vector3.one*scale;o.transform.rotation=unknown?Quaternion.identity:Quaternion.Euler(180,0,0);view.question.gameObject.SetActive(unknown);view.answer.gameObject.SetActive(!unknown);if(view.value!=value){view.value=value;view.answer.text=value>0?value.ToString():"";}return o;
  }
  public void RevealBarrel(GameObject o,int value){var view=o.GetComponent<BarrelView>();view.value=value;view.answer.text=value.ToString();view.answer.gameObject.SetActive(true);}
  public void ReleaseBarrel(GameObject o){if(!o)return;o.SetActive(false);barrelPool.Push(o);}
  public Quaternion BarrelOnTicketRotation(int seat)=>tickets[seat].rotation*Quaternion.Euler(180,0,0);
  public static Quaternion ChairRotation(int seat)=>Quaternion.Euler(0,seat*90,0);
  public void ClearPlaced(){for(int s=0;s<4;s++)for(int i=0;i<27;i++)if(placedByCell[s,i]){ReleaseBarrel(placedByCell[s,i]);placedByCell[s,i]=null;}placed.Clear();}
  public void RestoreCovered(MatchState state,int exceptSeat=-1,int exceptSlot=-1){
   for(int s=0;s<4;s++)for(int i=0;i<27;i++){
    bool wanted=state.seats[s].covered[i]&&!(s==exceptSeat&&i==exceptSlot);var existing=placedByCell[s,i];
    if(wanted&&!existing){var o=Barrel(state.seats[s].cells[i],false,cells[s,i].position+Vector3.up*.039f,.061f);o.transform.rotation=BarrelOnTicketRotation(s);placedByCell[s,i]=o;placed.Add(o);}
    else if(!wanted&&existing){placed.Remove(existing);ReleaseBarrel(existing);placedByCell[s,i]=null;}
   }
  }
  void BuildRoom(){
   var panel=Mat("Petrol wainscot",new Color(.035f,.085f,.075f),0,.24f);var wall=Mat("Deep green plaster",new Color(.058f,.108f,.09f),0,.1f);var dark=Mat("Ebony",new Color(.022f,.026f,.023f));
   Box("Floor",new Vector3(0,-.04f,0),new Vector3(14,.08f,14),wood);
   for(int k=-14;k<=14;k++)Box("Floorboard seam",new Vector3(k*.48f,.002f,0),new Vector3(.005f,.002f,14),dark);
   var rug=GameObject.CreatePrimitive(PrimitiveType.Cylinder);rug.name="Circular wool rug";rug.transform.SetParent(root);rug.transform.position=new Vector3(0,.009f,0);rug.transform.localScale=new Vector3(6.4f,.007f,6.4f);rug.GetComponent<Renderer>().sharedMaterial=Mat("Rug",new Color(.105f,.035f,.033f));Object.Destroy(rug.GetComponent<Collider>());
   for(int i=0;i<4;i++){
    var side=new GameObject("Paneled wall").transform;side.SetParent(root);side.rotation=Quaternion.Euler(0,i*90,0);
    Box("Plaster",new Vector3(0,2.5f,5.8f),new Vector3(12,5,.14f),wall,side);Box("Wainscot",new Vector3(0,.8f,5.69f),new Vector3(12,1.6f,.08f),panel,side);
    foreach(float y in new[]{.12f,1.5f,1.6f})Box("Chair rail",new Vector3(0,y,5.60f),new Vector3(12,.035f,.055f),brass,side);
    for(int j=-5;j<=5;j++)Box("Panel stile",new Vector3(j, .8f,5.59f),new Vector3(.032f,1.4f,.045f),wood,side);
    // Tall architectural frames keep the distant room quietly readable.
    for(int j=-1;j<=1;j++){
     Box("Recessed art panel",new Vector3(j*2.6f,2.9f,5.68f),new Vector3(1.5f,1.9f,.06f),dark,side);
     foreach(float x in new[]{-.78f,.78f})Box("Frame",new Vector3(j*2.6f+x,2.9f,5.61f),new Vector3(.025f,2,.04f),brass,side);
     foreach(float y in new[]{1.9f,3.9f})Box("Frame",new Vector3(j*2.6f,y,5.61f),new Vector3(1.58f,.025f,.04f),brass,side);
    }
   }
   var key=new GameObject("Warm pool of lamplight").AddComponent<Light>();key.transform.SetParent(root);key.type=LightType.Spot;key.transform.position=new Vector3(0,4.8f,-.3f);key.transform.rotation=Quaternion.Euler(90,0,0);key.spotAngle=94;key.range=10;key.intensity=1.55f;key.color=new Color(1,.89f,.74f);key.shadows=LightShadows.Soft;key.shadowBias=.025f;
   var fill=new GameObject("Window bounce").AddComponent<Light>();fill.transform.SetParent(root);fill.type=LightType.Point;fill.renderMode=LightRenderMode.ForceVertex;fill.transform.position=new Vector3(-3,2.8f,-2);fill.range=9;fill.intensity=1.3f;fill.color=new Color(.53f,.69f,.72f);
   var rim=new GameObject("Amber room fill").AddComponent<Light>();rim.transform.SetParent(root);rim.type=LightType.Point;rim.renderMode=LightRenderMode.ForceVertex;rim.transform.position=new Vector3(2,2.7f,3);rim.range=9;rim.intensity=1.2f;rim.color=new Color(1,.65f,.34f);
  }
 }
}
