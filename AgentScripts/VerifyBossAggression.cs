using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Paname;
public static class VerifyBossAggression
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags).Invoke(o,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static object Main()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();float old=Time.timeScale;Time.timeScale=0;
        try
        {
            for(int i=0;i<4;i++)g.slots[i].joined=i==0;
            g.ReturnToLobby();g.SetDifficulty(1);g.BeginRound();
            var player=((PanameGame.Player[])Get(g,"players"))[0];player.position=new Vector2(g.CurrentMission.exitX-18,0);player.invulnerable=1000;
            Call(g,"UpdateCamera",1f/60);g.TickSimulation(1f/60,new PlayerCommand[4]);
            Vector2 rack=(Vector2)Call(g,"BossBombRack");Call(g,"DropBossBomb",rack.x-5);
            var bombs=(IList)Get(g,"bossBombs");var bomb=bombs[0];var kind=bomb.GetType();
            float delay=(float)kind.GetField("delay").GetValue(bomb);
            Call(g,"TickBossBombs",delay-.001f);Call(g,"TickBossBombs",.002f);
            var root=(Transform)kind.GetField("root").GetValue(bomb);var renderer=root.GetComponent<SpriteRenderer>();
            Check(root.gameObject.activeSelf&&Vector2.Distance(root.position,rack)<.08f,"La bombe ne part pas du support");
            Check(renderer&&renderer.sprite&&renderer.sprite.texture==Resources.Load<Texture2D>("Art/macron-helicopter"),"Munition différente du dessin de l'hélico");
            Check(root.childCount==0,"Ancienne bombe géométrique toujours présente");
            Call(g,"RemoveBossBomb",0,false);
            float peak=0;var commands=new PlayerCommand[4];float previous=((Vector2)Get(g,"bossPosition")).x;
            for(int i=0;i<600;i++){g.TickSimulation(1f/60,commands);float x=((Vector2)Get(g,"bossPosition")).x;peak=Mathf.Max(peak,Mathf.Abs(x-previous)*60);previous=x;}
            int attacks=(int)Get(g,"bossVolley");Check(peak>5,"Hélico trop lent");Check(attacks>=5,"Trop de répit entre les attaques");
            Call(g,"DamageBoss",g.BossHealth/2+1);Check((bool)Get(g,"bossEnraged"),"Furie non déclenchée");
            float ragePeak=0;
            for(int i=0;i<600;i++){g.TickSimulation(1f/60,commands);float x=((Vector2)Get(g,"bossPosition")).x;ragePeak=Mathf.Max(ragePeak,Mathf.Abs(x-previous)*60);previous=x;}
            Check(ragePeak>peak,"Furie ne rend pas le déplacement plus rapide");
            return new{sameMissileArtwork=true,releaseFromRack=true,attackSequencesInTenSeconds=attacks,peakSpeed=peak,enragedPeakSpeed=ragePeak};
        }
        finally{g.ReturnToLobby();g.SetDifficulty(1);Time.timeScale=old;}
    }
}
