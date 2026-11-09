using UnityEngine;
namespace VelvetLoto {
 // Recorded graphite stroke; the remaining close-up foley is synthesized once.
 public sealed class SalonAudio {
  AudioSource source,pencilSource;AudioClip rustle,pencil,wood,whoosh,chime;System.Random rng=new System.Random(73);float nextPencilAt;bool pencilReleasing;
  public SalonAudio(GameObject owner){source=owner.AddComponent<AudioSource>();source.spatialBlend=0;source.volume=.65f;pencilSource=owner.AddComponent<AudioSource>();pencilSource.spatialBlend=0;pencilSource.playOnAwake=false;rustle=Clip("Cloth and wooden rummage",.65f,0);pencil=Resources.Load<AudioClip>("Audio/PencilStroke");wood=Clip("Beech on paper",.22f,2);whoosh=Clip("Gentle turn",.28f,3);chime=Clip("Closing chord",1.3f,4);}
  public bool TryBeginPencil(){if(!pencil||Time.unscaledTime<nextPencilAt)return false;pencilSource.clip=pencil;pencilSource.pitch=1;pencilSource.volume=0;pencilReleasing=false;pencilSource.Play();nextPencilAt=Time.unscaledTime+.45f;PencilPlayCount++;return true;}
  public void Tick(float dt){if(!pencilSource.isPlaying)return;pencilSource.volume=Mathf.MoveTowards(pencilSource.volume,pencilReleasing?0:.32f,dt*(.32f/(pencilReleasing?.025f:.008f)));if(pencilReleasing&&pencilSource.volume<=0)pencilSource.Stop();}
  public void StopPencil(){pencilReleasing=true;}
  public bool PencilPlaying=>pencilSource.isPlaying;
  public int PencilPlayCount{get;private set;}
  AudioClip Clip(string name,float duration,int type){int sr=44100,n=(int)(sr*duration);float[] data=new float[n];float low=0;
   for(int i=0;i<n;i++){float t=(float)i/sr,u=t/duration,noise=(float)(rng.NextDouble()*2-1);low=Mathf.Lerp(low,noise,.15f);float v=0;
    if(type==0)v=(low*.23f+noise*.045f)*Mathf.Pow(Mathf.Sin(u*Mathf.PI),.6f)*( .5f+.5f*Mathf.Sin(t*71)*Mathf.Sin(t*27))+Mathf.Sin(t*1800)*Mathf.Exp(-t*25)*.06f;
    if(type==1)v=(noise-low)*.10f*Mathf.Sin(u*Mathf.PI)*(.6f+.4f*Mathf.Sin(t*163));
    if(type==2)v=(Mathf.Sin(t*1280)*.4f+Mathf.Sin(t*2137)*.2f+low*.17f)*Mathf.Exp(-t*30);
    if(type==3)v=low*.16f*Mathf.Sin(u*Mathf.PI);
    if(type==4)v=(Mathf.Sin(t*2*Mathf.PI*523.25f)+Mathf.Sin(t*2*Mathf.PI*659.25f)*.6f+Mathf.Sin(t*2*Mathf.PI*783.99f)*.4f)*.12f*Mathf.Exp(-t*3)*Mathf.Min(1,t*70);
    data[i]=v;
   }var c=AudioClip.Create(name,n,1,sr,false);c.SetData(data,0);return c;
  }
  public void Play(string sound){source.pitch=Random.Range(.94f,1.06f);source.PlayOneShot(sound=="rustle"?rustle:sound=="pencil"?pencil:sound=="wood"?wood:sound=="win"?chime:whoosh);}
  public void Mute(bool value){source.mute=value;pencilSource.mute=value;}
 }
}
