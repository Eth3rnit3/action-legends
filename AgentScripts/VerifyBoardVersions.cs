using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Paname;
// Each game version has its own leaderboard (0.9.0): a score published by an older version is neither replayed
// nor shown, and never marked as a cheat; it does not spend the 25 replay checks either, so the current
// version's scores behind a crowd of older, higher ones still appear. scores.js drops them at fetch time too.
public static class VerifyBoardVersions
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags).Invoke(o,args);
    static object Get(object o,string field)=>o.GetType().GetField(field,Flags).GetValue(o);
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,Flags).SetValue(o,value);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    // The scripted run of VerifyReplays, one hero: short and deterministic.
    static ReplayRecord Play(PanameGame g,int mission,int difficulty)
    {
        g.ReturnToLobby();Call(g,"LoadMission",mission);g.SetDifficulty(difficulty);
        for(int i=0;i<4;i++){g.slots[i].joined=i==0;if(i==0)g.slots[i].hero=0;}
        g.BeginRound();
        var moves=new int[4];var flags=new int[4];var commands=new PlayerCommand[4];
        for(int t=0;t<60*60*8&&g.phase==PanameGame.Phase.Playing;t++)
        {
            moves[0]=(t/240)%5==4?-50:(t/90)%3==2?0:100;flags[0]=1|((t/120)%4==3?2:0)|(t%47==0?4:0)|(t%420==100?8:0);
            commands[0]=new PlayerCommand{move=moves[0]/100f,fire=true,aimUp=(flags[0]&2)!=0,jump=(flags[0]&4)!=0,special=(flags[0]&8)!=0};
            Call(g,"RecordTick",0,moves,flags);g.TickSimulation(1f/60,commands);Call(g,"EndReplayTick");
        }
        return g.LastReplay;
    }
    static string Entry(string id,int score,ReplayRecord replay)=>"{\"id\":\""+id+"\",\"name\":\""+id+"\",\"players\":1,\"score\":"+score+",\"time\":60,\"won\":false,\"mine\":false,\"verdict\":\"\",\"replay\":"+JsonUtility.ToJson(replay)+"}";
    public static object Main()
    {
        var checks=new List<string>();var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        Check(!string.IsNullOrEmpty(Application.version)&&Application.version!="0.8.0","Version du jeu : "+Application.version,checks);
        var record=Play(g,0,0);
        Check(record!=null&&record.version==Application.version&&record.ticks>600,"Partie enregistrée en "+record?.version+" ("+record?.ticks+" pas, "+record?.score+" pts)",checks);
        g.ReturnToLobby();Call(g,"LoadMission",0);
        // The same inputs labelled 0.8.0 would even replay here: the board is chosen by version, not by luck.
        var old=JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record));old.version="0.8.0";
        var legacy=JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record));legacy.version=null;legacy.heroIds=null;
        var entries=new List<string>();
        for(int i=0;i<30;i++)entries.Add(Entry("old"+i,900000-i,old));
        entries.Add(Entry("legacy",800000,legacy));
        entries.Add(Entry("current",record.score,record));
        string key="action-legends/"+g.CurrentMission.id+"/0";
        Set(g,"boardOpen",true);Set(g,"boardBoard",key);Set(g,"boardChecks",0);
        g.OnScoreEvent("{\"type\":\"board\",\"board\":\""+key+"\",\"message\":\"\",\"entries\":["+string.Join(",",entries)+"]}");
        var board=(System.Collections.IList)Get(g,"board");
        Check(board.Count==32,"Les "+board.Count+" scores reçus sont lus",checks);
        string Verdict(int i){var e=board[i];return (string)e.GetType().GetField("verdict").GetValue(e);}
        string Id(object e)=>(string)e.GetType().GetField("id").GetValue(e);
        var verifying=Get(g,"verifyingEntry");
        Check(verifying!=null&&Id(verifying)=="current","Le score de cette version est rejoué d'abord, derrière 30 scores 0.8.0 plus hauts",checks);
        bool allOld=true;for(int i=0;i<31;i++)allOld&=Verdict(i)=="old";
        Check(allOld,"Les scores 0.8.0 et sans version sont classés « autre version », pas « ko »",checks);
        Check((int)Get(g,"boardChecks")==0,"Ils ne consomment aucune des 25 vérifications",checks);
        for(int n=0;n<100000&&Get(g,"verifyingEntry")!=null;n++)Call(g,"TickVerifier");
        Check(Verdict(31)=="ok","Le score de cette version se rejoue et s'affiche (verdict ok)",checks);
        Check((int)Get(g,"boardChecks")==1,"Une seule vérification dépensée",checks);
        Set(g,"boardOpen",false);g.ReturnToLobby();Call(g,"LoadMission",0);
        return string.Join("\n",checks);
    }
}
