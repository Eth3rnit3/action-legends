using System;
using System.Reflection;
using UnityEngine;
using Paname;
public static class PreviewFourPlayers
{
    public static object Main()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        for(int i=0;i<4;i++){g.slots[i].joined=true;g.slots[i].hero=i;}
        g.BeginRound();Time.timeScale=0;
        var ps=(PanameGame.Player[])typeof(PanameGame).GetField("players",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(g);
        var commands=new PlayerCommand[4];
        for(int i=0;i<4;i++){ps[i].position=new Vector2(11.5f+i*2.7f,i==2?1.6f:0);ps[i].invulnerable=0;commands[i]=new PlayerCommand{move=1,fire=true};}
        var spawn=typeof(PanameGame).GetMethod("SpawnEnemy",BindingFlags.NonPublic|BindingFlags.Instance);
        for(int i=0;i<3;i++)spawn.Invoke(g,new object[]{24f+i*2.2f,i});
        g.TickSimulation(.001f,commands);
        return new {players=g.JoinedCount,enemies=g.EnemyCount,illustrated=true};
    }
}
