using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using Paname;
// Mission 4, Avignon ("Sur le Pont d'Avignon"), built from data only: the mission loads, its six panoramas join and cover
// the course (the Rhône and the Pont Saint-Bénézet, the ramparts, rue des Teinturiers, place des Carmes, place de l'Horloge,
// the Palais des Papes), its street scenery stands, the festival troupe fights (the mime's punch, the actor's tomatoes, the
// whip at reach), the bull holds the screen and charges, the Antipope plays every move (crosier shots, announced bells, troops,
// the great peal) and falls at the end of the campaign, a game is replayed exactly, and bots win the mission in the five
// difficulties, alone and four together. VerifySaintRemy covers the campaign's unlocks and codes.
public static class VerifyAvignon
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
    const string Id="sur-le-pont-d-avignon";

    static PanameGame g;static PanameGame.Player[] players;static IList enemies,shots,hostages,pickups,arrivals,carriers,bombs,thrown;
    static readonly PlayerCommand[] commands=new PlayerCommand[4];
    static MissionDefinition[] original;static int av,tokyo;
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
            Catalog(checks);Map(checks,captures);Enemies(checks,captures);Bull(checks,captures);Pope(checks,captures);
            // AVIGNON_QUICK=1 skips the long runs while looking at the captures.
            if(Environment.GetEnvironmentVariable("AVIGNON_QUICK")!="1"){Replay(checks);Bots(checks);}
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
        var ids=Array.ConvertAll(original,m=>m.id);av=Array.IndexOf(ids,Id);tokyo=Array.IndexOf(ids,"tokyo-neon");
        Check(av==3&&tokyo==4&&!original[av].hidden&&original[tokyo].hidden,"Avignon est la mission 4 de la campagne, avant Tokyo devenue secrète ("+string.Join(", ",ids)+")",checks);
        var m=original[av];
        Check(m.code=="PAPALINE"&&m.title=="Sur le Pont d'Avignon"&&m.location.StartsWith("Avignon"),"Avignon a son code PAPALINE et son titre « Sur le Pont d'Avignon »",checks);
        Check(m.roster.Length==3&&m.roster[0].id=="mime"&&m.roster[1].id=="comedien"&&m.roster[2].id=="arlesien-fouet"&&m.boss.definition.id=="antipape"&&
              Array.Exists(m.encounters,c=>c.HasBoss&&c.boss.definition.id=="taureau")&&m.music=="Audio/Music/pont-avignon-arcade"&&m.bossMusic=="Audio/Music/pont-avignon-boss"&&
              m.music.Length>0&&Resources.Load<AudioClip>(m.music)&&Resources.Load<AudioClip>(m.bossMusic),
              "Le mime, le comédien et l'Arlésien au fouet, le taureau en mini-boss, l'Antipape en boss final, « Sur le pont d'Avignon » en arcade (parcours et boss)",checks);
        Check(m.waves.Length==9&&m.encounters.Length>=14&&m.map.hostages.Length>=8&&Array.Exists(m.map.hostages,h=>h.id=="affichiste")&&Array.TrueForAll(m.map.hostages,h=>h.id=="affichiste"||h.id=="civilian")&&m.map.decor.Length>=30,
              "Neuf vagues, "+m.encounters.Length+" rencontres, "+m.map.hostages.Length+" otages (l'affichiste du festival), "+m.map.decor.Length+" accessoires posés dans la rue",checks);
        g.ReturnToLobby();Call(g,"LoadMission",av);var logo=(Texture2D)Get(g,"missionLogo");
        Check(m.logo=="Art/avignon/mission-logo"&&logo&&logo.width>2*logo.height,"Avignon affiche son logo dans la carte de mission du menu ("+(logo?logo.width+"×"+logo.height:"pas d'image")+")",checks);
        Call(g,"LoadMission",0);
    }

    static Transform Route()=>(Transform)typeof(PanameArt).GetField("routeRoot",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
    // The six panoramas side by side, covering the whole course at both ends; the street scenery stands where the mission puts it.
    static void Map(List<string> checks,string captures)
    {
        Start(av,1,1);var m=g.CurrentMission;float half=g.gameCamera.orthographicSize*16f/9;
        Check(PanameArt.RouteSegments==6&&Mathf.Abs(PanameArt.RouteWidth-(6*2172f/724*13-5*1.5f))<1,"Les six panoramas (le Rhône et le pont, les remparts, rue des Teinturiers, place des Carmes, place de l'Horloge, Palais des Papes) se raccordent en une fresque de "+PanameArt.RouteWidth.ToString("0.0")+" u",checks);
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
            seen.Add(x.ToString("0")+" : "+m.map.backgrounds[piece].Substring("Art/avignon/".Length));
        }
        Check(seen[0].EndsWith("rhone")&&seen[seen.Count-1].EndsWith("palais"),"Le parcours part du Rhône et du pont Saint-Bénézet et le boss se bat devant le Palais des Papes ("+string.Join(", ",seen)+")",checks);
        int decor=0;foreach(Transform t in (Transform)Get(g,"world"))if(t.name.StartsWith("Décor posé"))decor++;
        Check(decor==m.map.decor.Length,"Les "+decor+" accessoires (terrasses, chevalets, fontaine, tonneaux, oliviers, murets) sont posés dans la rue",checks);
        Quiet();var p=players[0];
        foreach(var x in new[]{20f,62f,128f,150f,214f,300f,352f})
        {
            Stand(p,x-4);Set(g,"cameraX",x);Capture(captures+"/avignon-decor-"+x.ToString("000")+".png");
        }
        // The joins between pieces, each centred on screen.
        for(int i=1;i<route.childCount;i++)
        {
            float join=route.GetChild(i).localPosition.x;
            // The screen centre shows the panorama at half + 1 + scroll (x - cameraMin): solved for the join.
            float scroll=Mathf.Clamp((PanameArt.RouteWidth*route.localScale.x-2*half-2)/Mathf.Max(1,m.cameraMax-m.cameraMin),0,.5f);
            float x=m.cameraMin+(join*route.localScale.x-half-1)/scroll;x=Mathf.Clamp(x,m.cameraMin,m.cameraMax);
            Stand(p,x-4);Set(g,"cameraX",x);Capture(captures+"/avignon-join-"+i+".png");
        }
        Check(true,"Captures : les décors (avignon-decor-*.png) et chaque raccord de panorama au milieu de l'écran (avignon-join-*.png)",checks);
    }

    // Each of the troupe on the street: off guard, startled, strikes a hero standing still (the whip from two units away), and falls.
    static void Enemies(List<string> checks,string captures)
    {
        var lines=new List<string>();
        foreach(var id in new[]{"mime","comedien","arlesien-fouet"})
        {
            Start(av,1,1);var p=players[0];Quiet();Stand(p,40);var type=ContentLibrary.Enemy(id);
            var e=Place(id,40+type.keepDistance+.2f,false,-1);SetField(e,"mode",0);
            int taken=0,full=p.hp,tomatoes=0;float minGap=99;
            for(int t=0;t<60*4;t++){p.invulnerable=0;g.TickSimulation(1f/60,commands);minGap=Mathf.Min(minGap,Mathf.Abs(Pos(e).x-p.position.x));if(p.hp<full){taken+=full-p.hp;p.hp=full;}foreach(var s in shots)if(Field(s,"drawn")!=null)tomatoes++;}
            bool states=type.unaware!=type.idle&&type.surprised!=type.idle&&type.flee!=type.run&&type.hurt>=0&&Array.TrueForAll(type.deaths,d=>d>=0)&&Array.IndexOf(type.deaths,type.death)>=0;
            bool sound=id!="arlesien-fouet"||type.melee.Clip!=null;bool thrown=id!="comedien"||tomatoes>0;
            Check(taken>0&&states&&sound&&thrown&&(id!="arlesien-fouet"||minGap>1.5f),type.id+" frappe un héros immobile ("+taken+" touches en 4 s, au plus près à "+minGap.ToString("0.0")+" u"+(id=="arlesien-fouet"?", claquement du fouet":id=="comedien"?", tomates dessinées":"")+") et a ses poses inattentif, surpris, touché, fuite et ses quatre morts",checks);
            lines.Add(id);
        }
        // Their poses side by side on the place.
        foreach(var id in new[]{"mime","comedien","arlesien-fouet"})
        {
            Start(av,1,1);var p=players[0];Quiet();Stand(p,22);p.position=new Vector2(15.5f,0);Call(g,"SyncPlayer",0);
            var type=ContentLibrary.Enemy(id);var set=type.animations;
            var poses=new[]{("idle",0f),("attack",0f),("unaware",0f),("surprised",0f),("flee",.2f),("death",.5f),("deathExplosion",.1f),("deathFire",1f)};
            for(int i=0;i<poses.Length;i++)
            {
                var e=Place(id,17.5f+i*2.15f,false,-1);int state=set.Find(poses[i].Item1);
                SetField(e,"animState",state);SetField(e,"animTime",poses[i].Item2);SetField(e,"walk",poses[i].Item2*10);Call(g,"ShowEnemy",e,state,0f);
            }
            Set(g,"cameraX",25.5f);Capture(captures+"/avignon-"+id+".png");
        }
        // The local hostage.
        var h=ContentLibrary.Hostage("affichiste");
        Check(h.animations.sheet.texture=="Art/hostages/affichiste"&&h.name=="AFFICHISTE"&&h.rewards.Length>0,"L'otage local : l'affichiste du festival (planche Art/hostages/affichiste, récompenses de l'otage générique)",checks);
    }

    // The bull: holds the screen, waves wait, it charges (a jump clears it), rears and stomps, and its defeat lets the team go on.
    static void Bull(List<string> checks,string captures)
    {
        Start(av,1,1);var p=players[0];var mission=g.CurrentMission;int index=Array.FindIndex(mission.encounters,c=>c.HasBoss);var enc=mission.encounters[index];
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
            if(charging&&!shotCharge&&t%10==0){Capture(captures+"/avignon-taureau-charge.png");shotCharge=true;}
            if(a.kind==2&&phase==3&&!shotRear){Capture(captures+"/avignon-taureau-cabre.png");shotRear=true;}
            was=charging;
        }
        bool held=Mathf.Abs((float)Get(g,"cameraX")-arena)<.01f&&g.Wave==next&&g.BossHealth>0;
        Check(spawned&&held&&grounds>0&&hitsOnGround==grounds&&airs>0&&hitsInAir==0&&kinds.Contains(2)&&cells.Contains(3)&&cells.Contains(4)&&cells.Contains(5)&&cells.Contains(6),
              "Le taureau (x "+enc.atX+") bloque l'écran, la vague "+(next+1)+" attend ; il gratte le sol puis charge (le héros resté dans la rue est touché, celui qui saute passe), se cabre et piétine (cases "+string.Join(",",cells)+")"+(spawned&&held&&grounds>0&&hitsOnGround==grounds&&airs>0&&hitsInAir==0&&kinds.Contains(2)?"":" [apparu "+spawned+", tenu "+held+", au sol "+hitsOnGround+"/"+grounds+", en l'air "+hitsInAir+"/"+airs+", attaques "+string.Join(",",kinds)+"]"),checks);
        Call(g,"DamageBoss",100000);int wreck=Drawn(boss);int t2=0;while(g.MidBossActive&&t2<600){Tick(1);t2++;}
        int t3=0;commands[0]=new PlayerCommand{move=1};while(p.position.x<arena+HalfWidth+1&&t3<600){Tick(1);t3++;}commands[0]=default;
        Check(wreck==7&&!g.MidBossActive&&p.position.x>arena+HalfWidth&&g.phase==PanameGame.Phase.Playing,"Abattu, le taureau se couche (case 7) et libère l'écran : l'équipe repart"+(wreck==7&&!g.MidBossActive&&p.position.x>arena+HalfWidth?"":" [case "+wreck+", actif "+g.MidBossActive+", x "+p.position.x.ToString("0.0")+"/"+(arena+HalfWidth).ToString("0.0")+", "+g.phase+", pv "+p.hp+"]"),checks);
    }

    // The Antipope at the end of the course: every move, each one hurting a hero who stays in its way, then his fall.
    static void Pope(List<string> checks,string captures)
    {
        Start(av,1,1);var p=players[0];var mission=g.CurrentMission;Quiet();
        Stand(p,mission.exitX-18.5f);commands[0]=new PlayerCommand{move=1};Tick(20);commands[0]=default;
        Check(g.BossActive&&!g.MidBossActive&&(Get(g,"bossRenderer") as SpriteRenderer).sprite.texture==Resources.Load<Texture2D>("Art/avignon/antipape"),"L'Antipape attend devant le Palais des Papes, dessiné depuis sa planche",checks);
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
            if(bombs.Count<count&&impactAt<0)impactAt=t+8;if(t==impactAt)Capture(captures+"/avignon-impact.png");
            int drawn=0;foreach(var s in shots)if(Field(s,"drawn")!=null)drawn++;scrolls=Mathf.Max(scrolls,drawn);
            int marks=0;foreach(var b in bombs){var r=(SpriteRenderer)Field(b,"warning");if(r&&r.gameObject.name=="Zone annoncée")marks++;}zones=Mathf.Max(zones,marks);
            if(bombs.Count<before)impacts+=before-bombs.Count;before=bombs.Count;
            foreach(var e in enemies)if((int)Field(e,"group")==-2)troops++;
            if(marks>=3&&!shotZones&&(float)Field(bombs[0],"age")>.5f){Capture(captures+"/avignon-cloches.png");shotZones=true;}
            if(drawn>=3&&!shotScrolls){Capture(captures+"/avignon-crosse.png");shotScrolls=true;}
            if(troops>60&&!shotTroops){Capture(captures+"/avignon-antipape-renforts.png");shotTroops=true;}
        }
        Check(seen.Count==boss.attacks.Length&&cells.Contains(3)&&cells.Contains(4)&&cells.Contains(5),"L'Antipape joue ses "+boss.attacks.Length+" attaques à tour de rôle (cases "+string.Join(",",cells)+")",checks);
        Check(scrolls>=2&&scrollHits>0,"Sa crosse lance des boules de feu dessinées (jusqu'à "+scrolls+" en vol) qui touchent le héros ("+scrollHits+" fois)",checks);
        Check(zones>=3&&impacts>=3&&zoneHits>0,"Ses cloches s'annoncent par des cercles au sol (jusqu'à "+zones+") puis les frappent ("+impacts+" impacts, le héros resté dans un cercle est touché "+zoneHits+" fois)",checks);
        Check(troops>0,"Il appelle la troupe du Palais à la rescousse",checks);
        int health=g.BossHealth;
        // Shot at with the hero's rifle: it takes damage.
        Quiet();var target=(Vector2)Get(g,"bossPosition");p.position=new Vector2(target.x-6,0);p.facing=1;commands[0]=new PlayerCommand{fire=true};
        for(int t=0;t<120;t++){p.invulnerable=1;g.TickSimulation(1f/60,commands);}commands[0]=default;
        Check(g.BossHealth<health,"Les tirs des héros le blessent ("+health+" -> "+g.BossHealth+")",checks);
        Call(g,"DamageBoss",g.BossHealth);int wreck=Drawn(boss);
        for(int t=0;t<60*6&&g.phase==PanameGame.Phase.Playing;t++)Tick(1);
        Check(g.phase==PanameGame.Phase.Won&&wreck==7&&g.NextMissionDefinition==null,"Vaincu, il s'effondre sur sa crosse (case 7) : la mission est gagnée et la campagne terminée",checks);
    }

    // The cell the boss is drawn with.
    static int Drawn(BossDefinition boss){var r=(SpriteRenderer)Get(g,"bossRenderer");for(int i=0;i<8;i++)if(r.sprite==boss.animations.Sprite(i))return i;return -1;}
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
        var record=Record(av,0,2,60*60*15);foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
        var verifier=PanameGame.CreateVerifier(original);bool ok=record!=null&&verifier.BeginVerify(JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record)));bool? result=null;
        while(ok&&result==null)result=verifier.StepVerify(1e6f);UnityEngine.Object.DestroyImmediate(verifier.gameObject);
        Check(ok&&result==true,"Une partie d'Avignon à deux (taureau, tomates, crosse, cloches) se rejoue à l'identique ("+(record!=null&&record.won?"victoire":"défaite")+", "+(record!=null?record.ticks:0)+" pas)",checks);
    }
    // Bots alone and four together, invulnerable from DÉTENTE to LÉGENDE, vulnerable in SLUG with continues.
    static void Bots(List<string> checks)
    {
        var lines=new List<string>();
        foreach(int heroes in new[]{1,4})for(int d=0;d<PanameGame.DifficultyCount;d++)
        {
            bool slug=d==Slug;Start(av,d,heroes);int t=0;
            for(;t<60*60*30&&g.phase==PanameGame.Phase.Playing;t++){for(int i=0;i<4;i++)commands[i]=players[i]!=null?Bot(players[i],i,t):default;Tick(1,!slug);}
            if(g.phase!=PanameGame.Phase.Won)throw new Exception("ÉCHEC : bot d'Avignon en "+PanameGame.DifficultyNames[d]+" à "+heroes+" ("+g.phase+", "+t/60+" s, vague "+g.Wave+")");
            lines.Add(PanameGame.DifficultyNames[d]+" ×"+heroes+" "+t/60+" s"+(slug?" ("+g.Continues+" continues)":""));
        }
        foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
        Check(lines.Count==2*PanameGame.DifficultyCount,"Un bot gagne Avignon dans les cinq difficultés, seul et à quatre (invulnérable de DÉTENTE à LÉGENDE, vulnérable en SLUG avec continues) : "+string.Join(", ",lines),checks);
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
