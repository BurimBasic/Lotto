using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace VelvetLoto {
 public sealed class SalonUI {
  public Canvas canvas;public GameObject menu,lobby,hud,end,pause;
  public Text problem,subtitle,notice,progress,roomStatus,results;public InputField playerName,address;public Button start;
  public Dropdown difficulty;public Text[] seatLabels=new Text[4]; public bool sound=true;
  Font font,serif;Color cream=new Color(.92f,.86f,.71f),muted=new Color(.59f,.66f,.61f),gold=new Color(.76f,.59f,.32f);LotoGame game;
  public void Create(LotoGame game){this.game=game;font=Resources.Load<Font>("Fonts/Interface");serif=Resources.Load<Font>("Fonts/Numbers");
   var c=new GameObject("Salon interface");canvas=c.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=game.GetSalonCamera();canvas.planeDistance=.12f;c.AddComponent<GraphicRaycaster>();var scaler=c.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
   new GameObject("Events",typeof(EventSystem),typeof(StandaloneInputModule));
   menu=Panel("Main menu",canvas.transform,new Color(.025f,.055f,.045f,.94f),new Vector2(630,760),new Vector2(-430,0));
   Label(menu.transform,"Б А Р Х А Т Н Ы Й   К Л У Б",20,new Vector2(520,40),new Vector2(0,296),gold);
   Label(menu.transform,"Лото\nв уме",82,new Vector2(520,210),new Vector2(0,170),cream,serif);
   Label(menu.transform,"Тёплый свет. Быстрый счёт.\nОдин стол — четыре соперника.",23,new Vector2(520,90),new Vector2(0,20),muted);
   playerName=Input(menu.transform,"Ваше имя",new Vector2(0,-63),"Игрок");
   difficulty=Drop(menu.transform,new Vector2(0,-133));
   Button(menu.transform,"Тренировка с соперниками",new Vector2(0,-215),()=>game.Training(),true);
   Button(menu.transform,"Создать стол по сети",new Vector2(0,-281),()=>game.Host());
   Button(menu.transform,"Присоединиться",new Vector2(0,-347),()=>ShowJoin());
   var tagline=Label(canvas.transform,"01 — 90\n450 ВЫРАЖЕНИЙ · 5 СТУПЕНЕЙ СЛОЖНОСТИ",20,new Vector2(650,70),new Vector2(350,-353),cream);tagline.alignment=TextAnchor.MiddleCenter;
   // Menu-only captions are children of the menu for reliable visibility.
   tagline.transform.SetParent(menu.transform,true);tagline.rectTransform.anchoredPosition=new Vector2(790,-325);
   lobby=Panel("Network room",canvas.transform,new Color(.025f,.05f,.043f,.97f),new Vector2(650,680),Vector2.zero);lobby.SetActive(false);
   Label(lobby.transform,"ВАШ СТОЛ",21,new Vector2(560,40),new Vector2(0,277),gold);
   roomStatus=Label(lobby.transform,"",22,new Vector2(560,150),new Vector2(0,164),cream);
   address=Input(lobby.transform,"IP адрес ведущего",new Vector2(0,42),"192.168.1.");
   Button(lobby.transform,"Подключиться",new Vector2(0,-33),()=>game.Join(address.text));
   start=Button(lobby.transform,"Начать партию",new Vector2(0,-123),()=>game.StartMatch(),true);
   Label(lobby.transform,"Один компьютер на игрока. До четырёх мест.\nВедущий запускает партию, когда все за столом.",18,new Vector2(560,70),new Vector2(0,-217),muted);
   Button(lobby.transform,"Вернуться",new Vector2(0,-285),()=>game.Back());
   hud=Panel("Playing",canvas.transform,Color.clear,new Vector2(1600,900),Vector2.zero);hud.SetActive(false);hud.GetComponent<Image>().raycastTarget=false;
   Label(hud.transform,"ЛОТО В УМЕ",19,new Vector2(300,35),new Vector2(-585,399),gold);
   progress=Label(hud.transform,"",18,new Vector2(420,40),new Vector2(525,399),cream);
   var q=Panel("Expression card",hud.transform,new Color(.027f,.055f,.047f,.96f),new Vector2(600,144),new Vector2(0,322));
   subtitle=Label(q.transform,"ВЫЧИСЛИТЕ ЗНАЧЕНИЕ БОЧОНКА",15,new Vector2(560,28),new Vector2(0,43),gold);
   problem=Label(q.transform,"",43,new Vector2(565,74),new Vector2(0,-10),cream,serif);
   notice=Label(hud.transform,"",21,new Vector2(920,45),new Vector2(0,-301),cream);
   Label(hud.transform,"Мышь ↑↓ — осмотреть стол     •     ЛКМ — поставить     •     Esc — пауза",18,new Vector2(1100,40),new Vector2(-90,-403),muted);
   Button(hud.transform,"Не подходит  [Пробел]",new Vector2(555,-347),()=>game.Discard(),false,new Vector2(335,54));
   var scores=Panel("Players",hud.transform,new Color(.025f,.055f,.045f,.92f),new Vector2(310,179),new Vector2(-570,287));scores.GetComponent<Image>().raycastTarget=false;
   for(int i=0;i<4;i++)seatLabels[i]=Label(scores.transform,"",17,new Vector2(300,34),new Vector2(0,59-i*39),muted);
   end=Panel("Winner",canvas.transform,new Color(.025f,.05f,.043f,.98f),new Vector2(710,560),Vector2.zero);end.SetActive(false);
   Label(end.transform,"ПАРТИЯ ЗАВЕРШЕНА",19,new Vector2(620,40),new Vector2(0,211),gold);
   results=Label(end.transform,"",30,new Vector2(620,290),new Vector2(0,25),cream,serif);
   Button(end.transform,"К новому столу",new Vector2(0,-194),()=>game.Back(),true);
   pause=Panel("Pause",canvas.transform,new Color(.025f,.05f,.043f,.98f),new Vector2(650,660),Vector2.zero);pause.SetActive(false);
   Label(pause.transform,"ПРАВИЛА СТОЛА",24,new Vector2(560,50),new Vector2(0,267),gold);
   Label(pause.transform,"Решите пример сверху. Опустите взгляд\nи нажмите нужное число в билете.\n\nЧисла нет? Пробел уберёт бочонок.\nПри ошибке он тоже уйдёт. Пропущенные\nчисла вернутся в следующем круге мешка.\n\nПервый, закрывший все 15 чисел, побеждает.\nВ сети партия продолжается во время паузы.",21,new Vector2(570,330),new Vector2(0,54),cream);
   Button(pause.transform,"Продолжить",new Vector2(0,-160),()=>game.TogglePause(),true);
   Button(pause.transform,"Звук: вкл / выкл",new Vector2(0,-226),()=>{sound=!sound;game.SetSound(sound);});
   Button(pause.transform,"Выйти к меню",new Vector2(0,-292),()=>game.Back());
  }
  public void ShowJoin(){menu.SetActive(false);lobby.SetActive(true);address.gameObject.SetActive(true);start.gameObject.SetActive(false);roomStatus.text="Укажите локальный IP компьютера ведущего.\nОба компьютера должны быть в одной сети.";}
  public GameObject Panel(string name,Transform parent,Color color,Vector2 size,Vector2 pos){var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;r.sizeDelta=size;r.anchoredPosition=pos;o.GetComponent<Image>().color=color;return o;}
  public Text Label(Transform parent,string value,int size,Vector2 box,Vector2 pos,Color color,Font use=null){var o=new GameObject(value,typeof(RectTransform),typeof(Text));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;r.sizeDelta=box;r.anchoredPosition=pos;var t=o.GetComponent<Text>();t.text=value;t.font=use??font;t.fontSize=size;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.supportRichText=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
  public Button Button(Transform parent,string value,Vector2 pos,Action action,bool primary=false,Vector2? size=null){var o=Panel(value,parent,primary?gold:new Color(.12f,.19f,.16f),size??new Vector2(520,54),pos);var b=o.AddComponent<Button>();b.onClick.AddListener(()=>action());var colors=b.colors;colors.highlightedColor=new Color(1,.9f,.72f);colors.pressedColor=new Color(.7f,.62f,.46f);b.colors=colors;Label(o.transform,value,21,size??new Vector2(510,50),Vector2.zero,primary?new Color(.07f,.10f,.08f):cream);return b;}
  InputField Input(Transform parent,string hint,Vector2 pos,string value){var o=Panel(hint,parent,new Color(.07f,.12f,.10f),new Vector2(520,54),pos);var input=o.AddComponent<InputField>();var txt=Label(o.transform,"",21,new Vector2(490,50),Vector2.zero,cream);txt.alignment=TextAnchor.MiddleLeft;input.textComponent=txt;var ph=Label(o.transform,hint,21,new Vector2(490,50),Vector2.zero,muted);ph.alignment=TextAnchor.MiddleLeft;input.placeholder=ph;input.characterLimit=40;input.text=value;return input;}
  Dropdown Drop(Transform parent,Vector2 pos){
   var o=Panel("Difficulty",parent,new Color(.07f,.12f,.10f),new Vector2(520,54),pos);var d=o.AddComponent<Dropdown>();d.captionText=Label(o.transform,"",21,new Vector2(490,50),Vector2.zero,cream);
   var template=Panel("Template",o.transform,new Color(.07f,.12f,.10f),new Vector2(520,220),new Vector2(0,-138));
   var sr=template.AddComponent<ScrollRect>();sr.horizontal=false;var viewport=Panel("Viewport",template.transform,Color.white,new Vector2(520,220),Vector2.zero);viewport.AddComponent<Mask>().showMaskGraphic=false;
   var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(viewport.transform,false);var cr=(RectTransform)content.transform;cr.anchorMin=new Vector2(0,1);cr.anchorMax=new Vector2(1,1);cr.pivot=new Vector2(.5f,1);cr.sizeDelta=new Vector2(0,220);sr.viewport=(RectTransform)viewport.transform;sr.content=cr;
   var item=Panel("Item",content.transform,new Color(.12f,.20f,.16f),new Vector2(520,50),new Vector2(0,-25));var ir=(RectTransform)item.transform;ir.anchorMin=new Vector2(.5f,1);ir.anchorMax=new Vector2(.5f,1);var toggle=item.AddComponent<Toggle>();toggle.targetGraphic=item.GetComponent<Image>();toggle.graphic=Panel("Check",item.transform,gold,new Vector2(5,32),new Vector2(-245,0)).GetComponent<Image>();d.itemText=Label(item.transform,"Choice",21,new Vector2(460,48),Vector2.zero,cream);d.template=(RectTransform)template.transform;d.options.Clear();foreach(string s in new[]{"Сложность: смешанная  ▾","Лёгкая · ступени 1–2","Средняя · ступени 2–4","Сложная · ступени 4–5"})d.options.Add(new Dropdown.OptionData(s));d.RefreshShownValue();template.SetActive(false);return d;
  }
 }
}
