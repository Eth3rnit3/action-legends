using System;
using System.Reflection;
using UnityEngine;
using Paname;
public static class VerifyFastPlaytest
{
    public static object Main()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();float old=Time.timeScale;Time.timeScale=0;
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        try
        {
            for(int i=0;i<4;i++)g.slots[i].joined=i==0;g.ReturnToLobby();g.SetDifficulty(0);g.SelectHero(0,3);
            if(g.JoinedCount!=1||g.slots[0].hero!=3)throw new Exception("Sélection clavier");
            // Measures the free run: locked waves would stop the hero at the first one.
            foreach(var wave in g.CurrentMission.waves)wave.@lock=false;
            g.slots[1].joined=true;g.BeginRound();var commands=new PlayerCommand[4];commands[0]=new PlayerCommand{move=1,fire=true};
            var camera=typeof(PanameGame).GetMethod("UpdateCamera",flags);float start=g.GetPlayerPosition(0).x,previous=start;
            for(int n=0;n<120;n++)
            {
                g.TickSimulation(1f/60,commands);camera.Invoke(g,new object[]{1f/60});float x=g.GetPlayerPosition(0).x;
                if(x<previous-.001f)throw new Exception("Retour arrière");previous=x;
            }
            float travel=g.GetPlayerPosition(0).x-start;
            if(Mathf.Abs(travel-21.4f)>.15f)throw new Exception("Vitesse irrégulière : "+travel);
            g.GiveBonus(0,0);var players=(PanameGame.Player[])typeof(PanameGame).GetField("players",flags).GetValue(g);
            if(players[0].weapon!=1||players[0].ammo!=120)throw new Exception("Mitrailleuse bonus");
            g.GiveBonus(0,4);if(players[0].boost<=0)throw new Exception("Bonus vitesse");
            return new{keyboardSelection=true,twoPlayersTravel=travel,enemies=g.EnemyCount,bonus=true,length=g.CurrentMission.length,waves=g.CurrentMission.waves.Length};
        }
        finally{foreach(var wave in g.CurrentMission.waves)wave.@lock=true;for(int i=1;i<4;i++)g.slots[i].joined=false;g.ReturnToLobby();Time.timeScale=old;}
    }
}
