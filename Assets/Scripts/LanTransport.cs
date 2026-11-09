using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace VelvetLoto {
 // Only the Unity main thread parses messages or changes match state.
 public sealed class LanTransport : IDisposable {
  public const int Port=28741;
  public struct Received { public int peer; public string line; }
  public readonly ConcurrentQueue<Received> inbox=new ConcurrentQueue<Received>();
  readonly Dictionary<int,TcpClient> peers=new Dictionary<int,TcpClient>(); readonly object gate=new object();
  TcpListener listener; volatile bool disposed; int nextPeer=1;public bool IsHost {get;private set;}
  public void Host(){IsHost=true;listener=new TcpListener(IPAddress.Any,Port);listener.Start();new Thread(Accept){IsBackground=true}.Start();}
  void Accept(){try{while(!disposed){var c=listener.AcceptTcpClient();int id=nextPeer++; lock(gate){peers[id]=c;}StartReader(id,c);inbox.Enqueue(new Received{peer=id,line="CONNECTED"});}}catch(Exception e){if(!disposed)inbox.Enqueue(new Received{peer=-1,line="ERROR:"+e.Message});}}
  public void Connect(string address){new Thread(()=>{try{var c=new TcpClient();c.Connect(address,Port);lock(gate){peers[0]=c;}StartReader(0,c);inbox.Enqueue(new Received{peer=0,line="CONNECTED"});}catch(Exception e){inbox.Enqueue(new Received{peer=-1,line="ERROR:"+e.Message});}}){IsBackground=true}.Start();}
  void StartReader(int id,TcpClient c){c.NoDelay=true;new Thread(()=>{try{using(var reader=new StreamReader(c.GetStream(),Encoding.UTF8,false,4096,true)){while(!disposed){var line=reader.ReadLine();if(line==null)break;if(line.Length>32000)break;inbox.Enqueue(new Received{peer=id,line=line});}}}catch{}finally{inbox.Enqueue(new Received{peer=id,line="DISCONNECTED"});}}){IsBackground=true}.Start();}
  public void Send(int peer,string line){lock(gate){if(!peers.TryGetValue(peer,out var c))return;try{byte[] b=Encoding.UTF8.GetBytes(line+"\n");c.GetStream().Write(b,0,b.Length);}catch{}}}
  public void Broadcast(string line){int[] ids;lock(gate){ids=new int[peers.Count];peers.Keys.CopyTo(ids,0);}foreach(int id in ids)Send(id,line);}
  public void Dispose(){disposed=true;listener?.Stop();lock(gate){foreach(var c in peers.Values)c.Close();peers.Clear();}}
 }
 [Serializable] public class Wire {public string type,name; public int seat,slot,round; public MatchState state;}
}
