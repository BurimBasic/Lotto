using System;
using System.Collections.Generic;
using System.Linq;

namespace VelvetLoto {
 [Serializable] public class Problem { public int value, level; public string expression; }
 [Serializable] public class ProblemBank { public Problem[] problems; }
 [Serializable] public class SeatState {
  public string name="Свободно", expression=""; public int[] cells=new int[27]; public bool[] covered=new bool[27];
  public int round, solved, mistakes; public bool active; public string notice="";
  [NonSerialized] public int answer; [NonSerialized] public Queue<int> bag;
 }
 [Serializable] public class MatchState { public SeatState[] seats=new SeatState[4]; public bool started; public int winner=-1, difficulty; public float elapsed; }
 public class LotoRules {
  public readonly MatchState state=new MatchState(); readonly Random rng; readonly Problem[] bank;
  public LotoRules(int seed,Problem[] bank) { rng=new Random(seed);this.bank=bank; for(int i=0;i<4;i++)state.seats[i]=new SeatState(); }
  public void Join(int seat,string name) { if(state.started) return; state.seats[seat].active=true;state.seats[seat].name=string.IsNullOrWhiteSpace(name)?"Игрок "+(seat+1):name.Substring(0,Math.Min(18,name.Length)); }
  public void Start(int difficulty) { state.difficulty=difficulty;state.started=true;state.winner=-1;state.elapsed=0; foreach(var p in state.seats) if(p.active) { p.cells=Ticket();p.covered=new bool[27];p.solved=p.mistakes=p.round=0;p.bag=null;Draw(p); } }
  public int[] Ticket() {
   bool[,] mask;int[] counts;
   do { mask=new bool[3,9];counts=new int[9];for(int r=0;r<3;r++){var cols=Enumerable.Range(0,9).OrderBy(_=>rng.Next()).Take(5);foreach(int c in cols){mask[r,c]=true;counts[c]++;}} }while(counts.Any(n=>n==0));
   int[] cells=new int[27];
   for(int c=0;c<9;c++){int lo=c==0?1:c*10,hi=c==8?90:c*10+9;var nums=Enumerable.Range(lo,hi-lo+1).OrderBy(_=>rng.Next()).Take(counts[c]).OrderBy(n=>n).ToArray();int k=0;for(int r=0;r<3;r++)if(mask[r,c])cells[r*9+c]=nums[k++];}
   return cells;
  }
  void Draw(SeatState p) {
   if(p.bag==null||p.bag.Count==0) {
    // Each private cycle contains every value exactly once. A discarded required value always returns.
    var values=Enumerable.Range(1,90).OrderBy(_=>rng.Next()).ToList();
    // Scatter required values through the cycle, then start with one to make the first move useful.
    if(p.round==0){int first=p.cells.Where(n=>n>0).OrderBy(_=>rng.Next()).First();values.Remove(first);values.Insert(0,first);}
    p.bag=new Queue<int>(values);
   }
   p.answer=p.bag.Dequeue();p.round++;
   int level=state.difficulty==0?rng.Next(5):state.difficulty==1?rng.Next(2):state.difficulty==2?1+rng.Next(3):3+rng.Next(2);
   var choices=bank.Where(q=>q.value==p.answer&&q.level==level+1).ToArray();
   if(choices.Length==0)throw new InvalidOperationException("Missing expression for "+p.answer+" / "+level);
   p.expression=choices[rng.Next(choices.Length)].expression;
  }
  public bool Act(int seat,int round,int slot) {
   if(!state.started||state.winner>=0||seat<0||seat>3)return false;
   var p=state.seats[seat];if(!p.active||p.round!=round)return false;
   bool correct=slot>=0&&slot<27&&!p.covered[slot]&&p.cells[slot]==p.answer;
   if(correct){p.covered[slot]=true;p.solved++;p.notice="Верно · "+p.answer;if(p.solved==15){state.winner=seat;return true;}}
   else {bool needed=Enumerable.Range(0,27).Any(i=>!p.covered[i]&&p.cells[i]==p.answer); if(slot>=0||needed){p.mistakes++;p.notice="Бочонок ушёл · число вернётся";}else p.notice="Следующий бочонок";}
   Draw(p);return true;
  }
 }
}
