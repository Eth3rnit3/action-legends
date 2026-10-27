using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using Paname;
// Mission 4, Saint-Remy-de-Provence ("Prophétie en Provence"), built from data only: the mission loads, its six panoramas
// join and cover the course, its street scenery stands, the Arlésiens fight (the whip at reach), the bull holds the screen
// and charges, Nostradamus plays every move (scrolls, announced circles, troops, the great prophecy) and falls, the campaign
// runs Paris, Marseille, Saint-Remy, Tokyo with its unlocks and codes (a Tokyo unlocked before stays unlocked), a game is
// replayed exactly, and bots win the mission in the five difficulties, alone and four together.
public static class VerifySaintRemy
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static void Set(object o,string name,object v)=>o.GetType().GetField(name,Flags).SetValue(o,v);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags|BindingFlags.Static).Invoke(o,args);
    static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
    static void SetField(object o,string name,object v)=>o.GetType().GetField(name).SetValue(o,v);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    static Vector2 Pos(object o)=>(Vector2)Field(o,"position");
    const int Slug=4,Fleeing=4,Unaware=2;
    const float HalfWidth=5.8f*16f/9f;
    const string Id="prophetie-en-provence";

    static PanameGame g;static PanameGame.Player[] players;static IList enemies,shots,hostages,pickups,arrivals,carriers,bombs,thrown;
    static readonly PlayerCommand[] commands=new PlayerCommand[4];
    static MissionDefinition[] original;static int sr,tokyo;
    static void Start(int mission,int difficulty,int heroes)
    {
        g.ReturnToLobby();Call(g,"LoadMission",mission);g.SetDifficulty(difficulty);
        for(int i=0;i<4;i++){g.slots[i].joined=i<heroes;g.slots[i].hero=i%4;}
        g.BeginRound();players=(PanameGame.Player[])Get(g,"players");enemies=(IList)Get(g,"enemies");shots=(IList)Get(g,"shots");thrown=(IList)Get(g,"thrown");
        hostages=(IList)Get(g,"hostages");pickups=(IList)Get(g,"pickups");arrivals=(IList)Get(g,"arrivals");carriers=(IList)Get(g,"carriers");bombs=(IList)Get(g,"bossBombs");
        for(int i=0;i<4;i++)commands[i]=default;
    }
    static void Tick(int n=1,bool invulnerable=true){for(int t=0;t<n;t++){if(invulnerable)foreach(var p in players)if(p!=null&&p.hp>0)p.invulnerable=1;g.TickSimulation(1f/60,commands);}}
    static void Clear(IList list){foreach(var o in list){var r=o.GetType().GetField("root")?.GetValue(o) as Transform;if(r)r.gameObject.SetActive(false);}list.Clear();}
    static void Quiet()
    {
        Clear(enemies);Clear(hostages);Clear(pickups);arrivals.Clear();Clear(carriers);Clear(thrown);
        foreach(var shot in shots){SetField(shot,"active",false);((Transform)Field(shot,"root")).gameObject.SetActive(false);}shots.Clear();
        typeof(PanameGame).GetProperty("Wave").SetValue(g,g.CurrentMission.waves.Length);
        var started=(bool[])Get(g,"encounterStarted");for(int i=0;i<started.Length;i++)started[i]=true;
        Set(g,"screenLocked",false);Set(g,"encounterLock",false);
    }
    static void Stand(PanameGame.Player p,float x){p.position=new Vector2(x,0);Set(g,"cameraX",x+2.5f);Tick(2);}
    static object Place(string enemy,float x,bool unaware=false,int facing=-1)
    {
        var s=new EncounterSpawn{enemy=enemy,entry="none",x=x,unaware=unaware,facing=facing,definition=ContentLibrary.Enemy(enemy),kind=-1};
        return Call(g,"Arrive",s,0,null);
    }

    public static object Main(string captures)
    {
        var checks=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        float scale=Time.timeScale;Time.timeScale=0;original=(MissionDefinition[])Get(g,"missions");
        foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
        try
        {
            Catalog(checks);Map(checks,captures);Enemies(checks,captures);Bull(checks,captures);Prophet(checks,captures);Campaign(checks);
            // SAINT_REMY_QUICK=1 skips the long runs while looking at the captures.
            if(Environment.GetEnvironmentVariable("SAINT_REMY_QUICK")!="1"){Replay(checks);Bots(checks);}
        }
        finally
        {
            Set(g,"missions",original);Set(g,"missionIndex",0);
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;}
            foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
            g.phase=PanameGame.Phase.Lobby;Call(g,"LoadMission",0);g.SetDifficulty(1);g.ReturnToLobby();Time.timeScale=scale;
        }
        return string.Join("\n",checks);
    }

    static void Catalog(List<string> checks)
    {
        var ids=Array.ConvertAll(original,m=>m.id);sr=Array.IndexOf(ids,Id);tokyo=Array.IndexOf(ids,"tokyo-neon");
        Check(string.Join(",",ids)=="panique-a-paname,mistral-gagnant,"+Id+",tokyo-neon","La campagne compte quatre missions dans l'ordre Paris, Marseille, Saint-Rémy, Tokyo ("+string.Join(", ",ids)+")",checks);
        var m=original[sr];
        Check(m.code=="FARANDOLE"&&original[tokyo].code=="SAKURA"&&original[1].code=="PASTIS"&&m.title=="Prophétie en Provence"&&m.location.StartsWith("Saint-Rémy-de-Provence"),"Saint-Rémy a son code FARANDOLE ; PASTIS et SAKURA restent valables",checks);
        Check(m.roster.Length==3&&m.roster[0].id=="arlesien"&&m.roster[1].id=="arlesienne"&&m.roster[2].id=="arlesien-fouet"&&m.boss.definition.id=="nostradamus"&&
              Array.Exists(m.encounters,c=>c.HasBoss&&c.boss.definition.id=="taureau")&&m.music=="Audio/Music/saint-remy/farandole-8bit"&&m.bossMusic=="Audio/Music/saint-remy/farandole-8bit-boss",
              "Arlésien, Arlésienne et Arlésien au fouet, le taureau en mini-boss, Nostradamus en boss final, la Farandole 8 bits (parcours et boss)",checks);
        Check(m.waves.Length==9&&m.encounters.Length>=14&&m.map.hostages.Length>=8&&Array.TrueForAll(m.map.hostages,h=>h.id=="peintre"||h.id=="civilian")&&m.map.decor.Length>=30,
              "Neuf vagues, "+m.encounters.Length+" rencontres, "+m.map.hostages.Length+" otages (le peintre local), "+m.map.decor.Length+" accessoires posés dans la rue",checks);
    }

    static Transform Route()=>(Transform)typeof(PanameArt).GetField("routeRoot",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
    // The six panoramas side by side, covering the whole course at both ends; the street scenery stands where the mission puts it.
    static void Map(List<string> checks,string captures)
    {
        Start(sr,1,1);var m=g.CurrentMission;float half=g.gameCamera.orthographicSize*16f/9;
        Check(PanameArt.RouteSegments==6&&Mathf.Abs(PanameArt.RouteWidth-(6*2172f/724*13-5*1.5f))<1,"Les six panoramas (place de la République, ruelles et place Favier, sortie de ville, Antiques, Glanum, Alpilles) se raccordent en une fresque de "+PanameArt.RouteWidth.ToString("0.0")+" u",checks);
        var route=Route();float coverLeft=0,coverRight=0;
        foreach(var at in new[]{m.cameraMin,m.cameraMax})
        {
            PanameArt.UpdatePanorama(at,half,m.cameraMin,m.cameraMax);float left=route.position.x,right=left+PanameArt.RouteWidth*route.localScale.x;
            if(at==m.cameraMin)coverLeft=at-half-left;else coverRight=right-(at+half);
        }
        Check(coverLeft>=0&&coverRight>=0,"La fresque couvre l'écran du départ à la sortie (marges "+coverLeft.ToString("0.0")+" et "+coverRight.ToString("0.0")+" u)",checks);
        // Which piece is in the middle of the screen at a few points of the course.
        var seen=new List<string>();
        foreach(var x in new[]{m.cameraMin,60f,140f,210f,300f,m.exitX-18+HalfWidth*.5f})
        {
            PanameArt.UpdatePanorama(x,half,m.cameraMin,m.cameraMax);float local=(x-route.position.x)/route.localScale.x;
            int piece=0;float edge=0;for(int i=0;i<route.childCount;i++){var c=route.GetChild(i);if(c.localPosition.x<=local){piece=i;edge=c.localPosition.x;}}
            seen.Add(x.ToString("0")+" : "+m.map.backgrounds[piece].Substring("Art/saint-remy/".Length));
        }
        Check(seen[0].EndsWith("centre-ville")&&seen[seen.Count-1].EndsWith("alpilles"),"Le parcours part de la place de la République et le boss se bat devant les Alpilles ("+string.Join(", ",seen)+")",checks);
        int decor=0;foreach(Transform t in (Transform)Get(g,"world"))if(t.name.StartsWith("Décor posé"))decor++;
        Check(decor==m.map.decor.Length,"Les "+decor+" accessoires (terrasses, fontaine, tonneaux, oliviers, colonnes, murets) sont posés dans la rue",checks);
        Quiet();var p=players[0];
        foreach(var x in new[]{20f,62f,128f,150f,214f,300f,352f})
        {
            Stand(p,x-4);Set(g,"cameraX",x);Capture(captures+"/saint-remy-decor-"+x.ToString("000")+".png");
        }
        // The joins between pieces, each centred on screen.
        for(int i=1;i<route.childCount;i++)
        {
            float join=route.GetChild(i).localPosition.x;
            // The screen centre shows the panorama at half + 1 + scroll (x - cameraMin): solved for the join.
            float scroll=Mathf.Clamp((PanameArt.RouteWidth*route.localScale.x-2*half-2)/Mathf.Max(1,m.cameraMax-m.cameraMin),0,.5f);
            float x=m.cameraMin+(join*route.localScale.x-half-1)/scroll;x=Mathf.Clamp(x,m.cameraMin,m.cameraMax);
            Stand(p,x-4);Set(g,"cameraX",x);Capture(captures+"/saint-remy-join-"+i+".png");
        }
        Check(true,"Captures : les décors (saint-remy-decor-*.png) et chaque raccord de panorama au milieu de l'écran (saint-remy-join-*.png)",checks);
    }

    // Each Arlésien on the street: off guard, startled, strikes a hero standing still (the whip from two units away), and falls.
    static void Enemies(List<string> checks,string captures)
    {
        var lines=new List<string>();
        foreach(var id in new[]{"arlesien","arlesienne","arlesien-fouet"})
        {
            Start(sr,1,1);var p=players[0];Quiet();Stand(p,40);var type=ContentLibrary.Enemy(id);
            var e=Place(id,40+type.keepDistance+.2f,false,-1);SetField(e,"mode",0);
            int taken=0,full=p.hp;float minGap=99;
            for(int t=0;t<60*4;t++){p.invulnerable=0;g.TickSimulation(1f/60,commands);minGap=Mathf.Min(minGap,Mathf.Abs(Pos(e).x-p.position.x));if(p.hp<full){taken+=full-p.hp;p.hp=full;}}
            bool states=type.unaware!=type.idle&&type.surprised!=type.idle&&type.flee!=type.run&&type.hurt>=0&&Array.TrueForAll(type.deaths,d=>d>=0)&&Array.IndexOf(type.deaths,type.death)>=0;
            bool sound=id!="arlesien-fouet"||type.melee.Clip!=null;
            Check(taken>0&&states&&sound&&(id!="arlesien-fouet"||minGap>1.5f),type.id+" frappe un héros immobile ("+taken+" touches en 4 s, au plus près à "+minGap.ToString("0.0")+" u"+(id=="arlesien-fouet"?", claquement du fouet":"")+") et a ses poses inattentif, surpris, touché, fuite et ses quatre morts",checks);
            lines.Add(id);
        }
        // Their poses side by side on the place.
        foreach(var id in new[]{"arlesien","arlesienne","arlesien-fouet"})
        {
            Start(sr,1,1);var p=players[0];Quiet();Stand(p,22);p.position=new Vector2(15.5f,0);Call(g,"SyncPlayer",0);
            var type=ContentLibrary.Enemy(id);var set=type.animations;
            var poses=new[]{("idle",0f),("attack",0f),("unaware",0f),("surprised",0f),("flee",.2f),("death",.5f),("deathExplosion",.1f),("deathFire",1f)};
            for(int i=0;i<poses.Length;i++)
            {
                var e=Place(id,17.5f+i*2.15f,false,-1);int state=set.Find(poses[i].Item1);
                SetField(e,"animState",state);SetField(e,"animTime",poses[i].Item2);SetField(e,"walk",poses[i].Item2*10);Call(g,"ShowEnemy",e,state,0f);
            }
            Set(g,"cameraX",25.5f);Capture(captures+"/saint-remy-"+id+".png");
        }
        // The local hostage, tied, then thanking the hero.
        Start(sr,1,1);var q=players[0];Quiet();Stand(q,32);
        var h=ContentLibrary.Hostage("peintre");
        Check(h.animations.sheet.texture=="Art/hostages/peintre"&&h.name=="PEINTRE"&&h.rewards.Length>0,"L'otage local : le peintre de Provence (planche Art/hostages/peintre, récompenses de l'otage générique)",checks);
    }

    // The bull: holds the screen, waves wait, it charges (a jump clears it), rears and stomps, and its defeat lets the team go on.
    static void Bull(List<string> checks,string captures)
    {
        Start(sr,1,1);var p=players[0];var mission=g.CurrentMission;int index=Array.FindIndex(mission.encounters,c=>c.HasBoss);var enc=mission.encounters[index];
        Clear(enemies);Clear(hostages);int next=Array.FindIndex(mission.waves,w=>w.atX>enc.atX);typeof(PanameGame).GetProperty("Wave").SetValue(g,next);
        var started=(bool[])Get(g,"encounterStarted");for(int i=0;i<started.Length;i++)started[i]=i<index||mission.encounters[i].wave>=0&&mission.encounters[i].wave<next;
        Stand(p,enc.atX-.5f);commands[0]=new PlayerCommand{move=1};Tick(30);commands[0]=default;
        bool spawned=g.MidBossActive;float arena=(float)Get(g,"bossArenaX");var boss=enc.boss.definition;
        var cells=new HashSet<int>();var kinds=new HashSet<int>();bool shotCharge=false,shotRear=false;
        int grounds=0,airs=0,hitsOnGround=0,hitsInAir=0,charges=0,hpAt=0;bool was=false,struck=false;
        for(int t=0;t<60*40&&(grounds<1||airs<1||!kinds.Contains(2)||!shotCharge||!shotRear);t++)
        {
            int attack=(int)Get(g,"bossAttack"),phase=(int)Get(g,"bossPhase");var a=boss.attacks[attack];bool charging=a.kind==3&&phase==2;
            if(phase!=0)kinds.Add(a.kind);
            if(charging&&!was){charges++;p.invulnerable=0;p.hp=Mathf.Max(p.hp,3);hpAt=p.hp;struck=false;}
            bool air=charges%2==0;
            // In its way until it hits him once (a fallen hero would come back with full health).
            if(charging&&!struck){var bp=(Vector2)Get(g,"bossPosition");p.position=new Vector2(bp.x+(int)Get(g,"bossCharge")*1.2f,air?1.9f:0);p.vertical=0;}else if(!charging)p.invulnerable=1;
            g.TickSimulation(1f/60,commands);cells.Add((int)Get(g,"bossCell"));
            if(charging&&p.hp<hpAt){struck=true;p.invulnerable=1;}
            if(was&&!charging){if(air){airs++;if(struck)hitsInAir++;}else{grounds++;if(struck)hitsOnGround++;}}
            if(charging&&!shotCharge&&t%10==0){Capture(captures+"/saint-remy-taureau-charge.png");shotCharge=true;}
            if(a.kind==2&&phase==3&&!shotRear){Capture(captures+"/saint-remy-taureau-cabre.png");shotRear=true;}
            was=charging;
        }
        bool held=Mathf.Abs((float)Get(g,"cameraX")-arena)<.01f&&g.Wave==next&&g.BossHealth>0;
        Check(spawned&&held&&grounds>0&&hitsOnGround==grounds&&airs>0&&hitsInAir==0&&kinds.Contains(2)&&cells.Contains(3)&&cells.Contains(4)&&cells.Contains(5)&&cells.Contains(6),
              "Le taureau (x "+enc.atX+") bloque l'écran, la vague "+(next+1)+" attend ; il gratte le sol puis charge (le héros resté dans la rue est touché, celui qui saute passe), se cabre et piétine (cases "+string.Join(",",cells)+")"+(spawned&&held&&grounds>0&&hitsOnGround==grounds&&airs>0&&hitsInAir==0&&kinds.Contains(2)?"":" [apparu "+spawned+", tenu "+held+", au sol "+hitsOnGround+"/"+grounds+", en l'air "+hitsInAir+"/"+airs+", attaques "+string.Join(",",kinds)+"]"),checks);
        Call(g,"DamageBoss",100000);int wreck=Drawn(boss);int t2=0;while(g.MidBossActive&&t2<600){Tick(1);t2++;}
        int t3=0;commands[0]=new PlayerCommand{move=1};while(p.position.x<arena+HalfWidth+1&&t3<600){Tick(1);t3++;}commands[0]=default;
        Check(wreck==7&&!g.MidBossActive&&p.position.x>arena+HalfWidth&&g.phase==PanameGame.Phase.Playing,"Abattu, le taureau se couche (case 7) et libère l'écran : l'équipe repart"+(wreck==7&&!g.MidBossActive&&p.position.x>arena+HalfWidth?"":" [case "+wreck+", actif "+g.MidBossActive+", x "+p.position.x.ToString("0.0")+"/"+(arena+HalfWidth).ToString("0.0")+", "+g.phase+", pv "+p.hp+"]"),checks);
    }

    // Nostradamus at the end of the course: every move, each one hurting a hero who stays in its way, then his fall.
    static void Prophet(List<string> checks,string captures)
    {
        Start(sr,1,1);var p=players[0];var mission=g.CurrentMission;Quiet();
        Stand(p,mission.exitX-18.5f);commands[0]=new PlayerCommand{move=1};Tick(20);commands[0]=default;
        Check(g.BossActive&&!g.MidBossActive&&(Get(g,"bossRenderer") as SpriteRenderer).sprite.texture==Resources.Load<Texture2D>("Art/saint-remy/nostradamus"),"Nostradamus arrive à la sortie de la ville, dessiné depuis sa planche",checks);
        var boss=mission.boss.definition;var seen=new HashSet<int>();var cells=new HashSet<int>();int scrolls=0,zones=0,impacts=0,troops=0,zoneHits=0,scrollHits=0;
        bool shotZones=false,shotScrolls=false,shotTroops=false;int before=0,impactAt=-1;
        for(int t=0;t<60*60&&(seen.Count<boss.attacks.Length||!shotZones||!shotScrolls||!shotTroops||zoneHits==0||scrollHits==0);t++)
        {
            int attack=(int)Get(g,"bossAttack"),phase=(int)Get(g,"bossPhase");if(phase!=0)seen.Add(attack);var a=boss.attacks[attack];
            // The hero stands under the first announced circle, else in front of the boss, else he walks back.
            p.invulnerable=0;p.hp=Mathf.Max(p.hp,3);int hp=p.hp;
            if(bombs.Count>0){var marker=(Transform)Field(bombs[0],"marker");p.position=new Vector2(marker.position.x,0);}
            else p.position=new Vector2((float)Get(g,"bossArenaX")-4,0);
            // What may hit him this tick: a scroll close to him, a circle that strikes.
            bool scrollNear=false;foreach(var s in shots)if(Field(s,"drawn")!=null&&Vector2.Distance(Pos(s),p.position+Vector2.up)<1.6f)scrollNear=true;
            int count=bombs.Count;
            g.TickSimulation(1f/60,commands);cells.Add((int)Get(g,"bossCell"));
            if(p.hp<hp){if(bombs.Count<count)zoneHits++;else if(scrollNear)scrollHits++;}
            if(bombs.Count<count&&impactAt<0)impactAt=t+8;if(t==impactAt)Capture(captures+"/saint-remy-impact.png");
            int drawn=0;foreach(var s in shots)if(Field(s,"drawn")!=null)drawn++;scrolls=Mathf.Max(scrolls,drawn);
            int marks=0;foreach(var b in bombs){var r=(SpriteRenderer)Field(b,"warning");if(r&&r.gameObject.name=="Zone annoncée")marks++;}zones=Mathf.Max(zones,marks);
            if(bombs.Count<before)impacts+=before-bombs.Count;before=bombs.Count;
            foreach(var e in enemies)if((int)Field(e,"group")==-2)troops++;
            if(marks>=3&&!shotZones&&(float)Field(bombs[0],"age")>.5f){Capture(captures+"/saint-remy-prophetie.png");shotZones=true;}
            if(drawn>=3&&!shotScrolls){Capture(captures+"/saint-remy-parchemins.png");shotScrolls=true;}
            if(troops>60&&!shotTroops){Capture(captures+"/saint-remy-nostradamus-renforts.png");shotTroops=true;}
        }
        Check(seen.Count==boss.attacks.Length&&cells.Contains(3)&&cells.Contains(4)&&cells.Contains(5),"Nostradamus joue ses "+boss.attacks.Length+" attaques à tour de rôle (cases "+string.Join(",",cells)+")",checks);
        Check(scrolls>=2&&scrollHits>0,"Il lance des parchemins dessinés (jusqu'à "+scrolls+" en vol) qui touchent le héros ("+scrollHits+" fois)",checks);
        Check(zones>=3&&impacts>=3&&zoneHits>0,"Ses prophéties annoncent des cercles au sol (jusqu'à "+zones+") puis les frappent ("+impacts+" impacts, le héros resté dans un cercle est touché "+zoneHits+" fois)",checks);
        Check(troops>0,"Il appelle les Arlésiens à la rescousse",checks);
        int health=g.BossHealth;
        // Shot at with the hero's rifle: it takes damage.
        Quiet();var target=(Vector2)Get(g,"bossPosition");p.position=new Vector2(target.x-6,0);p.facing=1;commands[0]=new PlayerCommand{fire=true};
        for(int t=0;t<120;t++){p.invulnerable=1;g.TickSimulation(1f/60,commands);}commands[0]=default;
        Check(g.BossHealth<health,"Les tirs des héros le blessent ("+health+" -> "+g.BossHealth+")",checks);
        Call(g,"DamageBoss",g.BossHealth);int wreck=Drawn(boss);
        for(int t=0;t<60*6&&g.phase==PanameGame.Phase.Playing;t++)Tick(1);
        Check(g.phase==PanameGame.Phase.Won&&wreck==7&&g.NextMissionDefinition!=null&&g.NextMissionDefinition.id=="tokyo-neon","Vaincu, il s'assoit sur son grimoire (case 7) : la mission est gagnée et Tokyo vient ensuite",checks);
    }

    // The cell the boss is drawn with.
    static int Drawn(BossDefinition boss){var r=(SpriteRenderer)Get(g,"bossRenderer");for(int i=0;i<8;i++)if(r.sprite==boss.animations.Sprite(i))return i;return -1;}
    static void Campaign(List<string> checks)
    {
        foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
        g.ReturnToLobby();Call(g,"LoadMission",0);
        Check(g.UnlockedCount==1&&!g.IsUnlocked(sr)&&!g.IsUnlocked(tokyo),"Au départ seul Paris est ouvert",checks);
        Call(g,"LoadMission",1);g.BeginRound();Call(g,"WinMission");
        Check(g.IsUnlocked(sr)&&!g.IsUnlocked(tokyo)&&g.NextMissionDefinition.id==Id&&g.NextMissionDefinition.code=="FARANDOLE","Gagner Marseille débloque Saint-Rémy (pas Tokyo) et révèle FARANDOLE",checks);
        int score=g.Score;g.NextMission();
        Check(g.MissionIndex==sr&&g.phase==PanameGame.Phase.Playing&&g.Score==score&&g.Wave==0,"MISSION SUIVANTE enchaîne Marseille sur Saint-Rémy en gardant le score",checks);
        Call(g,"WinMission");
        Check(g.IsUnlocked(tokyo)&&g.NextMissionDefinition.id=="tokyo-neon"&&g.NextMissionDefinition.code=="SAKURA","Gagner Saint-Rémy débloque Tokyo et révèle SAKURA",checks);
        g.NextMission();Check(g.MissionIndex==tokyo&&g.phase==PanameGame.Phase.Playing,"Saint-Rémy enchaîne sur Tokyo",checks);
        g.ReturnToLobby();
        // A browser that had unlocked Tokyo before Saint-Remy existed: Tokyo stays open, Saint-Remy waits for Marseille or its code.
        foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
        PlayerPrefs.SetInt("action-legends.unlocked.tokyo-neon",1);Call(g,"LoadMission",0);
        Check(g.IsUnlocked(tokyo)&&!g.IsUnlocked(sr)&&g.UnlockedCount==2,"Un Tokyo déjà débloqué le reste (Saint-Rémy reste à débloquer)",checks);
        g.SelectMission(1);Check(g.MissionIndex==tokyo,"Les flèches passent de Paris à Tokyo sans s'arrêter sur les missions fermées",checks);
        Check(g.EnterMissionCode("farandole")&&g.MissionIndex==sr&&g.IsUnlocked(sr),"Le code FARANDOLE ouvre et sélectionne Saint-Rémy",checks);
        Check(g.EnterMissionCode("SAKURA")&&g.MissionIndex==tokyo,"Le code SAKURA sélectionne toujours Tokyo, à sa nouvelle place",checks);
        Call(g,"LoadMission",0);foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
    }

    // VerifyLot2b's bot.
    static PlayerCommand Bot(PanameGame.Player p,int i,int t)
    {
        float nearest=float.MaxValue,dir=1;int ahead=0;bool aimed=false;
        foreach(var e in enemies)
        {
            float d=Pos(e).x-p.position.x;if(Mathf.Abs(d)<Mathf.Abs(nearest)){nearest=d;dir=Mathf.Sign(d);}
            if(d*p.facing>1.5f&&d*p.facing<9)ahead++;
            if(Mathf.Abs(d)>1.6f&&Mathf.Abs(d)<13&&((float)Field(e,"windup")>0||(int)Field(e,"burst")>0))aimed=true;
        }
        float move=enemies.Count==0?1:Mathf.Abs(nearest)>5?dir:Mathf.Abs(nearest)<1?dir*.15f:dir*.6f;
        bool up=false,down=aimed&&p.position.y<.01f,jump=t%71==i*9&&!aimed,grenade=ahead>=2&&t%50==i*5;
        if(p.position.y>.4f&&Mathf.Abs(nearest)<.8f)down=true;
        if(p.position.y<.01f&&Mathf.Abs(nearest)>2&&t%240<30)down=true;
        if(g.BossActive&&enemies.Count==0){float d=((Vector2)Get(g,"bossPosition")).x-p.position.x;move=Mathf.Abs(d)>.8f?Mathf.Sign(d):0;up=move==0&&t%90<60;grenade=t%40==i*7&&Mathf.Abs(d)<8;down=false;}
        if(g.ContinueOffered)jump=t%71==i*9;
        return new PlayerCommand{move=move,fire=true,aimUp=up,down=down,jump=jump,grenade=grenade&&!g.ContinueOffered,special=t%97==i*11};
    }
    static ReplayRecord Record(int mission,int difficulty,int heroes,int limit)
    {
        Start(mission,difficulty,heroes);var moves=new int[4];var flags=new int[4];
        for(int t=0;t<limit&&g.phase==PanameGame.Phase.Playing;t++)
        {
            for(int i=0;i<4;i++){moves[i]=0;flags[i]=0;var q=players[i];if(q==null)continue;var c=Bot(q,i,t);moves[i]=Mathf.RoundToInt(c.move*10)*10;flags[i]=(c.fire?1:0)|(c.aimUp?2:0)|(c.jump?4:0)|(c.special?8:0)|(c.down?16:0)|(c.grenade?32:0);commands[i]=(PlayerCommand)Call(g,"Command",moves[i],flags[i]);}
            Call(g,"RecordTick",0,moves,flags);g.TickSimulation(1f/60,commands);Call(g,"EndReplayTick");
        }
        return g.LastReplay;
    }
    static void Replay(List<string> checks)
    {
        var record=Record(sr,0,2,60*60*15);foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
        var verifier=PanameGame.CreateVerifier(original);bool ok=record!=null&&verifier.BeginVerify(JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record)));bool? result=null;
        while(ok&&result==null)result=verifier.StepVerify(1e6f);UnityEngine.Object.DestroyImmediate(verifier.gameObject);
        Check(ok&&result==true,"Une partie de Saint-Rémy à deux (taureau, parchemins, cercles de prophétie) se rejoue à l'identique ("+(record!=null&&record.won?"victoire":"défaite")+", "+(record!=null?record.ticks:0)+" pas)",checks);
    }
    // Bots alone and four together, invulnerable from DÉTENTE to LÉGENDE, vulnerable in SLUG with continues.
    static void Bots(List<string> checks)
    {
        var lines=new List<string>();
        foreach(int heroes in new[]{1,4})for(int d=0;d<PanameGame.DifficultyCount;d++)
        {
            bool slug=d==Slug;Start(sr,d,heroes);int t=0;
            for(;t<60*60*30&&g.phase==PanameGame.Phase.Playing;t++){for(int i=0;i<4;i++)commands[i]=players[i]!=null?Bot(players[i],i,t):default;Tick(1,!slug);}
            if(g.phase!=PanameGame.Phase.Won)throw new Exception("ÉCHEC : bot de Saint-Rémy en "+PanameGame.DifficultyNames[d]+" à "+heroes+" ("+g.phase+", "+t/60+" s, vague "+g.Wave+")");
            lines.Add(PanameGame.DifficultyNames[d]+" ×"+heroes+" "+t/60+" s"+(slug?" ("+g.Continues+" continues)":""));
        }
        foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
        Check(lines.Count==2*PanameGame.DifficultyCount,"Un bot gagne Saint-Rémy dans les cinq difficultés, seul et à quatre (invulnérable de DÉTENTE à LÉGENDE, vulnérable en SLUG avec continues) : "+string.Join(", ",lines),checks);
    }
    static void Capture(string path)
    {
        for(int i=0;i<4;i++)if(players[i]!=null&&players[i].hp>0){players[i].invulnerable=0;Call(g,"SyncPlayer",i);}
        Set(g,"shake",0f);g.gameCamera.aspect=16f/9;var x=(float)Get(g,"cameraX");
        PanameArt.UpdatePanorama(x,g.gameCamera.orthographicSize*g.gameCamera.aspect,g.CurrentMission.cameraMin,g.CurrentMission.cameraMax);g.gameCamera.transform.position=new Vector3(x,3.6f,-10);
        var rt=new RenderTexture(1280,720,24);g.gameCamera.targetTexture=rt;g.gameCamera.Render();
        RenderTexture.active=rt;var shot=new Texture2D(1280,720,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,1280,720),0,0);shot.Apply();
        g.gameCamera.targetTexture=null;RenderTexture.active=null;File.WriteAllBytes(path,shot.EncodeToPNG());
    }
}
