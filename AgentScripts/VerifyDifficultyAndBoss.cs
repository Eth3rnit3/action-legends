using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Paname;
public static class VerifyDifficultyAndBoss
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Field(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags).Invoke(o,args);
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static object Main()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();float old=Time.timeScale;Time.timeScale=0;
        try
        {
            for(int i=0;i<4;i++)g.slots[i].joined=i==0;
            g.ReturnToLobby();g.SetDifficulty(0);g.BeginRound();Check(g.GetPlayerHealth(0)==6&&g.Reserves==3,"Détente : PV / renforts");
            Call(g,"SpawnEnemy",12f,0);var easyEnemy=((IList)Field(g,"enemies"))[0];int easyHealth=(int)easyEnemy.GetType().GetField("hp").GetValue(easyEnemy);
            g.ReturnToLobby();g.SetDifficulty(3);g.BeginRound();Check(g.GetPlayerHealth(0)==3&&g.Reserves==1,"Légende : PV / renforts");
            Call(g,"SpawnEnemy",12f,0);var hardEnemy=((IList)Field(g,"enemies"))[0];int hardHealth=(int)hardEnemy.GetType().GetField("hp").GetValue(hardEnemy);Check(hardHealth>easyHealth,"Résistance ennemie");
            var p=((PanameGame.Player[])Field(g,"players"))[0];p.invulnerable=0;Call(g,"Damage",p,1);Check(p.hp==1,"Dégâts Légende doublés");
            g.ReturnToLobby();g.SetDifficulty(1);g.BeginRound();p=((PanameGame.Player[])Field(g,"players"))[0];
            for(int i=0;i<21;i++)Call(g,"RecordKill",new Vector2(10,0));Check(g.BonusCount==0,"Bonus trop fréquent");Call(g,"RecordKill",new Vector2(10,0));Check(g.BonusCount==1,"Bonus 22 éliminations");
            p.position=new Vector2(g.CurrentMission.exitX-18,0);p.invulnerable=1000;Call(g,"UpdateCamera",1f/60);
            g.TickSimulation(1f/60,new PlayerCommand[4]);Check(g.BossActive&&g.phase==PanameGame.Phase.Playing,"Boss avant victoire");
            int health=g.BossHealth;var boss=(Vector2)Field(g,"bossPosition");
            p.position=new Vector2(boss.x,0);p.aimUp=true;p.moving=false;Call(g,"FirePlayer",p);
            for(int i=0;i<30;i++)Call(g,"UpdateShots",1f/60);Check(g.BossHealth<health,"Tir vertical ne touche pas le boss");
            Call(g,"DropBossBomb",p.position.x);Call(g,"TickBossBombs",.8f);Check(((IList)Field(g,"bossBombs")).Count==1,"Bombe non créée");
            var bombs=(IList)Field(g,"bossBombs");var bomb=bombs[0];Vector2 bombPosition=(Vector2)bomb.GetType().GetField("position").GetValue(bomb);
            Call(g,"HitBossOrBomb",bombPosition+Vector2.left,bombPosition+Vector2.right,3);Check(bombs.Count==0,"Bombe indestructible");
            for(int i=0;i<650;i++){g.TickSimulation(1f/60,new PlayerCommand[4]);Call(g,"UpdateCamera",1f/60);}
            Check((int)Field(g,"bossVolley")>=3,"Alternance des attaques du boss");
            Check(g.phase==PanameGame.Phase.Playing,"Victoire avant boss vaincu");
            Call(g,"DamageBoss",100000);g.TickSimulation(3,new PlayerCommand[4]);Check(g.BossDefeated&&g.phase==PanameGame.Phase.Won,"Victoire après boss");
            foreach(var line in new[]{"bonus-0","bonus-1","bonus-2","bonus-3","bonus-4","bonus-5","boss-alert","victory"})foreach(var take in new[]{"a","b","c"})Check(Resources.Load<AudioClip>("Audio/Voices/"+line+"-"+take),"Voix manquante : "+line+"-"+take);
            Check(Resources.Load<AudioClip>("Audio/Music/marseillaise-arcade").length>30&&Resources.Load<AudioClip>("Audio/Music/marseillaise-boss").length>30,"Musique manquante");
            foreach(var sfx in new[]{"tir-A-mitraille","tir-B-laser-snes","tir-C-blaster-cps1","tir-D-pistolet-sec","impact-A-metal","impact-B-tchak","impact-C-ping","explosion-A-grosse","explosion-B-courte","explosion-C-bombe-neo","bonus-ramasse","bonus-coin","joueur-touche","boss-explose","menu-select","menu-start","roquette-depart"})Check(Resources.Load<AudioClip>("Audio/Sfx/"+sfx),"Bruitage manquant : "+sfx);
            return new{difficulties=true,easyEnemyHealth=easyHealth,legendEnemyHealth=hardHealth,bonusCadence=true,upwardShotHitsBoss=true,bombsDestructible=true,bossAttacks=true,victoryAfterBoss=true,musicAndVoices=true};
        }
        finally{g.ReturnToLobby();g.SetDifficulty(1);Time.timeScale=old;}
    }
}
