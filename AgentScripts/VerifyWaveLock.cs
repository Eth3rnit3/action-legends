using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Paname;
// Locked waves: the screen holds until the last enemy falls, enemies that walked in stay in view,
// GO appears once the wave is cleared, "lock": false keeps the old free run, and every mission
// can still be finished by a bot that has to clear each wave.
public static class VerifyWaveLock
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags).Invoke(o,args);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    static float EnemyX(object e)=>((Vector2)e.GetType().GetField("position").GetValue(e)).x;
    const float HalfWidth=5.8f*16f/9f;
    public static object Main()
    {
        var checks=new List<string>();var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        var players=(PanameGame.Player[])Get(g,"players");var enemies=(IList)Get(g,"enemies");var commands=new PlayerCommand[4];
        for(int i=0;i<4;i++)g.slots[i].joined=i==0;
        g.ReturnToLobby();Call(g,"LoadMission",0);g.SetDifficulty(1);g.BeginRound();
        var mission=g.CurrentMission;
        Check(mission.waves[0].@lock,"Une vague sans champ lock est bloquée par défaut",checks);

        // Run right without shooting: the hero must be held at the edge of the locked screen.
        commands[0]=new PlayerCommand{move=1};
        bool locked=false,escaped=false,leftView=false;float lockX=0,cameraMoved=0;
        for(int t=0;t<60*20;t++)
        {
            players[0].invulnerable=1;g.TickSimulation(1f/60,commands);
            if(!locked&&(bool)Get(g,"screenLocked")){locked=true;lockX=(float)Get(g,"lockX");}
            if(!locked)continue;
            float cameraX=(float)Get(g,"cameraX");cameraMoved=Mathf.Max(cameraMoved,Mathf.Abs(cameraX-lockX));
            if(players[0].position.x>lockX+HalfWidth-1+.001f)escaped=true;
            foreach(var e in enemies)if(EnemyX(e)<lockX-HalfWidth-2.5f||EnemyX(e)>lockX+HalfWidth+6)leftView=true;
        }
        Check(locked,"L'écran se bloque à l'arrivée de la première vague",checks);
        Check(!escaped&&players[0].position.x>lockX+HalfWidth-1.1f,"Le héros reste coincé au bord droit de l'écran bloqué",checks);
        Check(cameraMoved<.01f,"La caméra ne bouge plus pendant le blocage",checks);
        Check(g.Wave==0&&enemies.Count>0,"La vague n'est pas finie tant que des ennemis restent debout",checks);
        Check(!leftView,"Les ennemis entrent par les bords et restent près de l'écran",checks);
        Check((float)Get(g,"goFromX")<0,"Pas de flèche GO pendant le combat",checks);

        // A hero who falls during the lock comes back inside the locked screen and can move again.
        int reservesBefore=g.Reserves;
        players[0].invulnerable=0;Call(g,"Damage",players[0],99);
        for(int t=0;t<60*4&&players[0].hp<=0;t++)g.TickSimulation(1f/60,commands);
        float back=players[0].position.x;
        for(int t=0;t<60*2;t++){players[0].invulnerable=1;g.TickSimulation(1f/60,commands);}
        Check(players[0].hp>0&&g.Reserves==reservesBefore-1&&back>=lockX-HalfWidth&&back<=lockX+HalfWidth,"Un héros tombé revient dans l'écran bloqué (x "+back.ToString("0.0")+")",checks);
        Check(players[0].position.x>lockX+HalfWidth-1.1f,"Le héros revenu peut de nouveau avancer jusqu'au bord",checks);

        // Clear the wave: GO appears and the camera follows again.
        for(int t=0;t<60*30&&g.Wave==0;t++)
        {
            players[0].invulnerable=1;
            for(int e=enemies.Count-1;e>=0;e--)Call(g,"HitEnemy",e,999,1,false);
            g.TickSimulation(1f/60,commands);
        }
        Check(g.Wave==1&&!(bool)Get(g,"screenLocked"),"La vague se termine à la chute du dernier ennemi",checks);
        Check(Mathf.Abs((float)Get(g,"goFromX")-lockX)<.01f,"La flèche GO s'affiche une fois la vague vaincue",checks);
        for(int t=0;t<60*2;t++){players[0].invulnerable=1;g.TickSimulation(1f/60,commands);}
        Check((float)Get(g,"cameraX")>lockX+5,"Le défilement reprend après la vague",checks);

        // "lock": false keeps the free run: the wave ends once all of it has appeared.
        mission.waves[0].@lock=false;
        try
        {
            g.BeginRound();bool everLocked=false;
            for(int t=0;t<60*12&&g.Wave==0;t++){players[0].invulnerable=1;g.TickSimulation(1f/60,commands);everLocked|=(bool)Get(g,"screenLocked");}
            Check(!everLocked&&g.Wave==1,"Une vague \"lock\": false laisse courir l'équipe",checks);
        }
        finally{mission.waves[0].@lock=true;}

        // A bot that must clear every wave: aims at the nearest enemy, jumps now and then.
        var times=new List<string>();
        for(int m=0;m<g.MissionCount;m++)foreach(int heroes in new[]{1,4})
        {
            for(int i=0;i<4;i++){g.slots[i].joined=i<heroes;g.slots[i].hero=i;}
            g.ReturnToLobby();Call(g,"LoadMission",m);g.SetDifficulty(1);g.BeginRound();
            int lockedTicks=0,t=0;
            for(;t<60*60*20&&g.phase==PanameGame.Phase.Playing;t++)
            {
                for(int i=0;i<heroes;i++)
                {
                    var p=players[i];if(p==null)continue;p.invulnerable=1;
                    float nearest=float.MaxValue,dir=1;
                    foreach(var e in enemies){float d=EnemyX(e)-p.position.x;if(Mathf.Abs(d)<Mathf.Abs(nearest)){nearest=d;dir=Mathf.Sign(d);}}
                    // Walk to the nearest enemy, back off when it is closer than the barrel, otherwise creep toward it to face it.
                    float move=enemies.Count==0?1:Mathf.Abs(nearest)>5?dir:Mathf.Abs(nearest)<1.6f&&t%40<25?-dir:dir*.15f;
                    // Under the boss, stand still and shoot straight up.
                    if(g.BossActive&&enemies.Count==0){float d=((Vector2)Get(g,"bossPosition")).x-p.position.x;move=Mathf.Abs(d)>.8f?Mathf.Sign(d):0;}
                    commands[i]=new PlayerCommand{move=move,fire=true,special=true,aimUp=g.BossActive&&move==0,jump=t%53==i*7};
                }
                g.TickSimulation(1f/60,commands);if((bool)Get(g,"screenLocked"))lockedTicks++;
            }
            if(g.phase!=PanameGame.Phase.Won)
            {
                var state=new List<string>();foreach(var e in enemies)state.Add(e.GetType().GetProperty("Role").GetValue(e)+"@"+EnemyX(e).ToString("0.0")+" hp"+e.GetType().GetField("hp").GetValue(e));
                throw new Exception("ÉCHEC : mission non terminée : "+g.CurrentMission.title+", "+heroes+" joueur(s), vague "+g.Wave+", phase "+g.phase+", reste "+Get(g,"pending")+", lockX "+Get(g,"lockX")+", héros "+players[0].position+" face "+players[0].facing+", ennemis "+string.Join(" ",state));
            }
            times.Add(g.CurrentMission.title+" ×"+heroes+" : "+(t/60)+" s dont "+(lockedTicks/60)+" s bloqué");
        }
        Check(true,"Chaque mission se termine en battant chaque vague bloquée ("+string.Join(" ; ",times)+")",checks);
        for(int i=0;i<4;i++)g.slots[i].joined=i==0;
        g.ReturnToLobby();
        return string.Join("\n",checks);
    }
}
