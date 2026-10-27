using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Paname;
// Records scripted runs through the replay hooks, sends them through JSON like the page does, and
// replays them in a hidden verifier: the same run must match, a tampered one must not.
public static class VerifyReplays
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags).Invoke(o,args);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    // Missions by their place among the three this test was written for (Paris 0, Marseille 1, Tokyo 2): Saint-Remy now comes
    // before Tokyo and has its own VerifySaintRemy. A test mission appended after the catalogue keeps its index.
    static readonly string[] Three={"panique-a-paname","mistral-gagnant","tokyo-neon"};
    static int Old(PanameGame game,int m){var all=(MissionDefinition[])game.GetType().GetField("missions",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);return m<Three.Length?Array.FindIndex(all,x=>x.id==Three[m]):m;}
    static ReplayRecord Play(PanameGame g,int mission,int difficulty,int[] heroes,int leaveAt)
    {
        g.ReturnToLobby();Call(g,"LoadMission",Old(g,mission));g.SetDifficulty(difficulty);
        for(int i=0;i<4;i++){g.slots[i].joined=heroes[i]>=0;if(heroes[i]>=0)g.slots[i].hero=heroes[i];}
        g.BeginRound();
        var moves=new int[4];var flags=new int[4];var commands=new PlayerCommand[4];
        for(int t=0;t<60*60*8&&g.phase==PanameGame.Phase.Playing;t++)
        {
            int left=0;
            if(t==leaveAt){left=2;Call(g,"DropPlayer",1);}
            for(int i=0;i<4;i++)
            {
                moves[i]=(t/240+i)%5==4?-50:(t/90)%3==2?0:100;
                flags[i]=1|((t/120)%4==3?2:0)|(t%47==i*5?4:0)|(t%420==100?8:0);
                commands[i]=new PlayerCommand{move=moves[i]/100f,fire=true,aimUp=(flags[i]&2)!=0,jump=(flags[i]&4)!=0,special=(flags[i]&8)!=0};
            }
            Call(g,"RecordTick",left,moves,flags);g.TickSimulation(1f/60,commands);Call(g,"EndReplayTick");
        }
        return g.LastReplay;
    }
    static bool Verify(PanameGame verifier,ReplayRecord record)
    {
        if(!verifier.BeginVerify(record))return false;
        bool? result=null;while(result==null)result=verifier.StepVerify(1e6f);return result.Value;
    }
    static ReplayRecord Copy(ReplayRecord r)=>JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(r));
    public static object Main()
    {
        var checks=new List<string>();var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        var missions=(MissionDefinition[])g.GetType().GetField("missions",Flags).GetValue(g);
        bool anyUnlock=false;
        var verifier=PanameGame.CreateVerifier(missions);
        var sizes=new List<string>();
        foreach(var run in new[]{(m:2,d:1,h:new[]{0,-1,-1,-1},leave:-1),(m:0,d:0,h:new[]{1,2,-1,-1},leave:900),(m:1,d:3,h:new[]{3,0,1,2},leave:-1)})
        {
            var record=Play(g,run.m,run.d,run.h,run.leave);
            string label=missions[Old(g,run.m)].id+" / "+PanameGame.DifficultyNames[run.d]+" / "+(run.h.Length-Array.FindAll(run.h,x=>x<0).Length)+" J";
            Check(record!=null&&record.ticks>600&&record.score>0,"Partie enregistrée : "+label+" ("+record?.ticks+" pas, "+record?.score+" pts, "+(record!=null&&record.won?"victoire":"défaite")+")",checks);
            sizes.Add(label+" : "+record.runs.Length+" caractères");
            // The recorded run may itself unlock the next mission; verifying it must not.
            foreach(var m in missions)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
            Check(Verify(verifier,Copy(record)),"Le replay se rejoue à l'identique : "+label,checks);
            var score=Copy(record);score.score+=100;Check(!Verify(verifier,score),"Un score gonflé est refusé : "+label,checks);
            var runs=Copy(record);int star=runs.runs.IndexOf('*',runs.runs.Length/2);int cut=runs.runs.LastIndexOf(';',star)+1;
            runs.runs=runs.runs.Substring(0,cut)+"30*100.9,"+runs.runs.Substring(cut);
            Check(!Verify(verifier,runs),"Des entrées modifiées sont refusées : "+label,checks);
            foreach(var m in missions)anyUnlock|=PlayerPrefs.GetInt("action-legends.unlocked."+m.id,0)!=0;
        }
        Check(!anyUnlock,"Vérifier un replay ne débloque aucune mission",checks);
        Check(g.MissionIndex==1&&g.CurrentMission.id==missions[1].id,"La partie affichée garde sa mission pendant les vérifications",checks);
        g.ReturnToLobby();Call(g,"LoadMission",0);
        return new{passed=checks,count=checks.Count,sizes};
    }
}
