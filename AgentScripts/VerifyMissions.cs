using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Paname;
public static class VerifyMissions
{
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    public static object Main()
    {
        var checks=new List<string>();var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;var missionsField=typeof(PanameGame).GetField("missions",flags);var indexField=typeof(PanameGame).GetField("missionIndex",flags);
        var original=(MissionDefinition[])missionsField.GetValue(g);float scale=Time.timeScale;Time.timeScale=0;
        try
        {
            Check(UnityEditor.PlayerSettings.productName=="Action Legends","Le jeu s'appelle Action Legends",checks);
            Check(g.CurrentMission.id=="panique-a-paname"&&g.CurrentMission.title=="Panique à Paname","Panique à Paname est une mission du catalogue",checks);
            var added=JsonUtility.FromJson<MissionDefinition>(JsonUtility.ToJson(original[0]));added.id="test-mission";added.title="Mission de vérification";added.length=40;added.exitX=37;added.cameraMax=34;
            added.waves=new[]{new MissionWave{atX=12,baseEnemies=1,enemiesPerPlayer=0,maxAlive=1,spawnInterval=.2f,enemyPattern=new[]{0}}};
            // Paris's hostages, encounters, relief and props (lot 3a) and vehicle (lot 3b) stand beyond the shorter test mission.
            added.map.hostages=new MapHostage[0];added.encounters=new MissionEncounter[0];added.map.terrain=new MapTerrain[0];added.map.props=new MapProp[0];added.map.vehicles=new MapVehicle[0];added.Validate();
            missionsField.SetValue(g,new[]{original[0],added});g.phase=PanameGame.Phase.Lobby;PlayerPrefs.SetInt("action-legends.unlocked.test-mission",1);g.SelectMission(1);
            Check(g.MissionCount==2&&g.CurrentMission.id=="test-mission","Une mission supplémentaire devient sélectionnable",checks);
            for(int i=0;i<4;i++)g.slots[i].joined=i==0;g.BeginRound();var commands=new PlayerCommand[4];commands[0]=new PlayerCommand{move=1,fire=true};
            // The test mission ends with the boss like any other: shoot it down once it appears.
            var damageBoss=typeof(PanameGame).GetMethod("DamageBoss",flags);
            for(int n=0;n<1800&&g.phase==PanameGame.Phase.Playing;n++){if(g.BossActive)damageBoss.Invoke(g,new object[]{100000});g.TickSimulation(1f/60,commands);}
            Check(g.Wave==1&&g.phase==PanameGame.Phase.Won,"Une mission de longueur et de vagues différentes se termine",checks);
            g.BeginRound();g.PauseFromBrowser();Check(g.phase==PanameGame.Phase.Paused,"Quitter l'onglet peut mettre le jeu en pause",checks);
            bool rejected=false;try{added.waves[0].enemyPattern=new[]{99};added.Validate();}catch(ArgumentException){rejected=true;}
            Check(rejected,"Une mission invalide est refusée par le moteur",checks);
            return new{passed=checks,count=checks.Count};
        }
        finally
        {
            PlayerPrefs.DeleteKey("action-legends.unlocked.test-mission");missionsField.SetValue(g,original);indexField.SetValue(g,0);g.phase=PanameGame.Phase.Lobby;g.SelectMission(0);g.ReturnToLobby();Time.timeScale=scale;
        }
    }
}
