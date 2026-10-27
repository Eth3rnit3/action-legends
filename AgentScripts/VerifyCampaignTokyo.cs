using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Paname;
// Plays the campaign rules and the Tokyo mission in the running game: locks and codes, armed
// enemies, the mecha's three attacks, and chaining a won mission into the next one.
public static class VerifyCampaignTokyo
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags).Invoke(o,args);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    static void Capture(PanameGame g,string path)
    {
        Call(g,"UpdateCamera",0f);
        var rt=new RenderTexture(1280,720,24);g.gameCamera.targetTexture=rt;g.gameCamera.aspect=16f/9;g.gameCamera.Render();
        RenderTexture.active=rt;var shot=new Texture2D(1280,720,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,1280,720),0,0);shot.Apply();
        g.gameCamera.targetTexture=null;RenderTexture.active=null;File.WriteAllBytes(path,shot.EncodeToPNG());
    }
    public static object Main(string captures)
    {
        var checks=new List<string>();var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        var missions=(MissionDefinition[])Get(g,"missions");
        foreach(var m in missions)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
        var players=(PanameGame.Player[])Get(g,"players");var commands=new PlayerCommand[4];
        var shots=(IList)Get(g,"shots");var enemies=(IList)Get(g,"enemies");var bombs=(IList)Get(g,"bossBombs");var waves=(IList)Get(g,"shockwaves");
        Check(g.MissionCount==3&&missions[2].id=="tokyo-neon","Le catalogue contient Paris, Marseille et Tokyo",checks);
        Check(missions[0].enemies.Length==3&&missions[0].enemies[2]=="looter-shield"&&missions[0].roster[2].animations.sheet.texture=="Art/enemies","Paris garde ses pilleurs",checks);
        Check(missions[2].enemies.Length==3&&missions[2].roster[0].Armed&&missions[2].roster[1].Armed&&missions[2].roster[2].Armed,"Les trois ennemis de Tokyo sont armés",checks);
        g.ReturnToLobby();Call(g,"LoadMission",0);
        Check(!g.IsUnlocked(1)&&!g.IsUnlocked(2)&&g.UnlockedCount==1,"Seule la première mission est ouverte au départ",checks);
        g.SelectMission(1);Check(g.MissionIndex==0,"Les flèches sautent les missions verrouillées",checks);
        Check(!g.EnterMissionCode("NOPE")&&g.MissionIndex==0,"Un code inconnu est refusé",checks);
        Check(g.EnterMissionCode("sakura")&&g.MissionIndex==2&&g.IsUnlocked(2),"Le code SAKURA ouvre et sélectionne Tokyo",checks);
        g.SelectMission(1);Check(g.MissionIndex==0,"Les flèches passent de Tokyo à Paris sans s'arrêter sur Marseille",checks);
        g.SelectMission(-1);Check(g.MissionIndex==2,"Tokyo reste accessible avec les flèches",checks);

        for(int i=0;i<4;i++)g.slots[i].joined=i==0;
        g.SetDifficulty(1);
        // Each gunman alone in front of a hero standing still must land its shots.
        for(int kind=0;kind<3;kind++)
        {
            g.BeginRound();var hero=players[0];hero.invulnerable=0;int full=hero.hp,taken=0;
            Call(g,"SpawnEnemy",hero.position.x+3.5f,kind);
            for(int n=0;n<60*4;n++){g.TickSimulation(1f/60,commands);if(hero.hp<full){taken+=full-hero.hp;hero.hp=full;}}
            Check(taken>0,"Le "+missions[2].enemies[kind]+" touche un héros immobile ("+taken+" touches en 4 s)",checks);
        }
        // A hero that only runs right cannot clear locked waves: this run looks at the gunmen, VerifyWaveLock at the locks.
        foreach(var wave in missions[2].waves)wave.@lock=false;
        g.BeginRound();var p=players[0];int max=p.hp;
        int hits=0,hostile=0,aiming=0;bool captured=false;var seenRoles=new HashSet<string>();
        for(int n=0;n<60*240&&!g.BossActive&&g.phase==PanameGame.Phase.Playing;n++)
        {
            commands[0]=new PlayerCommand{move=1,fire=true,jump=n%50==0};
            g.TickSimulation(1f/60,commands);
            if(p.hp<max){hits+=max-p.hp;p.hp=max;}
            foreach(var s in shots){var look=s.GetType().GetProperty("Hostile").GetValue(s);if((bool)look)hostile++;}
            foreach(var e in enemies){var role=(string)e.GetType().GetProperty("Role").GetValue(e);seenRoles.Add(role);if((float)e.GetType().GetField("windup").GetValue(e)>0)aiming++;}
            if(!captured&&g.Wave>=2&&enemies.Count>=3&&hostile>0){Capture(g,captures+"/tokyo-gunmen.png");captured=true;}
        }
        Check(seenRoles.Count==3,"Pistolet, mitraillette et fusil à pompe apparaissent",checks);
        Check(aiming>0,"Les tireurs visent avant de tirer",checks);
        Check(hostile>0&&hits>0,"Les ennemis tirent des balles qui touchent le joueur ("+hits+" touches)",checks);
        Check(g.BossActive&&g.Wave==missions[2].waves.Length,"Le boss arrive après les neuf vagues",checks);
        foreach(var wave in missions[2].waves)wave.@lock=true;

        var renderer=(SpriteRenderer)Get(g,"bossRenderer");
        Check(renderer&&renderer.sprite&&renderer.sprite.texture==Resources.Load<Texture2D>("Art/tokyo/mecha-shogun"),"Le boss est le Mécha Shogun dessiné",checks);
        var frames=new HashSet<int>();int maxWaves=0,maxBombs=0,bullets=0;int bossHits=0;
        bool shotWave=false,shotBombs=false,shotGun=false;
        for(int n=0;n<60*30&&g.phase==PanameGame.Phase.Playing;n++)
        {
            commands[0]=new PlayerCommand{move=n/90%2==0?-.4f:.4f,fire=n%3==0,aimUp=n%120<60};
            g.TickSimulation(1f/60,commands);if(p.hp<max){bossHits+=max-p.hp;p.hp=max;}
            frames.Add((int)Get(g,"bossCell"));maxWaves=Mathf.Max(maxWaves,waves.Count);maxBombs=Mathf.Max(maxBombs,bombs.Count);
            foreach(var s in shots)if((bool)s.GetType().GetProperty("Hostile").GetValue(s))bullets++;
            if(!shotWave&&waves.Count>0){Capture(g,captures+"/mecha-stomp.png");shotWave=true;}
            if(!shotBombs&&bombs.Count>0&&n%20==0){Capture(g,captures+"/mecha-missiles.png");shotBombs=true;}
            if(!shotGun&&(int)Get(g,"bossCell")==4&&bullets>20){Capture(g,captures+"/mecha-gatling.png");shotGun=true;}
        }
        Check(frames.Contains(1)&&frames.Contains(2)&&frames.Contains(3)&&frames.Contains(4)&&frames.Contains(5),"Le mécha marche, piétine, mitraille et lance ses missiles ("+string.Join(",",frames)+")",checks);
        Check(maxWaves==2&&maxBombs>=3&&bullets>0,"Ondes de choc, missiles et rafales sont lancés",checks);
        int health=g.BossHealth;Check(health<(int)Get(g,"bossMaxHealth"),"Les tirs des héros blessent le mécha",checks);
        commands[0]=default;
        p.position=new Vector2(p.position.x,2);p.vertical=0;var before=p.hp;
        Call(g,"DamageBoss",health);
        for(int n=0;n<60*4&&g.phase==PanameGame.Phase.Playing;n++)g.TickSimulation(1f/60,commands);
        Check(g.phase==PanameGame.Phase.Won&&g.NextMissionDefinition==null,"Abattre le mécha termine la dernière mission",checks);

        Call(g,"LoadMission",0);g.BeginRound();Call(g,"WinMission");int score=g.Score+12345;
        g.GetType().GetProperty("Score").SetValue(g,score);
        Check(g.IsUnlocked(1)&&g.NextMissionDefinition.code=="PASTIS","Gagner Paris débloque Marseille et révèle PASTIS",checks);
        g.NextMission();
        Check(g.MissionIndex==1&&g.phase==PanameGame.Phase.Playing&&g.Score==score&&g.Wave==0,"MISSION SUIVANTE enchaîne Marseille en gardant le score",checks);
        g.ReturnToLobby();Call(g,"LoadMission",0);
        foreach(var m in missions)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
        return new{passed=checks,count=checks.Count,tokyoHits=hits,bossHits};
    }
}
