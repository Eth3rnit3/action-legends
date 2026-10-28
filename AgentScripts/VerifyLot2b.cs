using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using Paname;
// Lot 2b, staged encounters and enemies with a life of their own. Every entry (walk, door, window, parachute, drop, truck,
// already there) brings its enemy to its place in view; an enemy off guard does not shoot before its surprise; panic and
// return; each wound plays its death; an enemy grenade hurts and can be dodged; each mission's mid-boss holds the screen,
// then lets it go; an encounter and a mid-boss written only in JSON work; an enemy without the new states still works;
// replays and online frames are exact; bots win the three missions in every difficulty, Slug included.
public static class VerifyLot2b
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static void Set(object o,string name,object v)=>o.GetType().GetField(name,Flags).SetValue(o,v);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags|BindingFlags.Static).Invoke(o,args);
    static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
    static void SetField(object o,string name,object v)=>o.GetType().GetField(name).SetValue(o,v);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    static Vector2 Pos(object o)=>(Vector2)Field(o,"position");
    static int Mode(object e)=>(int)Field(e,"mode");
    const int Active=0,Entering=1,Unaware=2,Surprised=3,Fleeing=4,Slug=4;
    const float HalfWidth=5.8f*16f/9f;

    static PanameGame g;static PanameGame.Player[] players;static IList enemies,shots,thrown,hostages,pickups,arrivals,carriers;
    static readonly PlayerCommand[] commands=new PlayerCommand[4];
    static MissionDefinition[] original;static FieldInfo missionsField;
    // Missions by their place among the three this test was written for (Paris 0, Marseille 1, Tokyo 2): Saint-Remy now comes
    // before Tokyo and has its own VerifySaintRemy. A test mission appended after the catalogue keeps its index.
    static readonly string[] Three={"panique-a-paname","mistral-gagnant","tokyo-neon"};
    static int Old(PanameGame game,int m){var all=(MissionDefinition[])game.GetType().GetField("missions",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);return m<Three.Length?Array.FindIndex(all,x=>x.id==Three[m]):m;}
    static void Start(int mission,int difficulty,int heroes,int first=0)
    {
        g.ReturnToLobby();Call(g,"LoadMission",Old(g,mission));g.SetDifficulty(difficulty);
        for(int i=0;i<4;i++){g.slots[i].joined=i<heroes;g.slots[i].hero=(first+i)%4;}
        g.BeginRound();players=(PanameGame.Player[])Get(g,"players");enemies=(IList)Get(g,"enemies");shots=(IList)Get(g,"shots");thrown=(IList)Get(g,"thrown");
        hostages=(IList)Get(g,"hostages");pickups=(IList)Get(g,"pickups");arrivals=(IList)Get(g,"arrivals");carriers=(IList)Get(g,"carriers");
        for(int i=0;i<4;i++)commands[i]=default;
    }
    static void Tick(int n=1,bool invulnerable=true){for(int t=0;t<n;t++){if(invulnerable)foreach(var p in players)if(p!=null&&p.hp>0)p.invulnerable=1;g.TickSimulation(1f/60,commands);}}
    static void Clear(IList list){foreach(var o in list){var r=o.GetType().GetField("root")?.GetValue(o) as Transform;if(r)r.gameObject.SetActive(false);}list.Clear();}
    // Nothing else on the street: no wave, no encounter, no hostage, no bonus.
    static void Quiet()
    {
        Clear(enemies);Clear(hostages);Clear(pickups);arrivals.Clear();Clear(carriers);Clear(thrown);
        foreach(var shot in shots){SetField(shot,"active",false);((Transform)Field(shot,"root")).gameObject.SetActive(false);}shots.Clear();
        typeof(PanameGame).GetProperty("Wave").SetValue(g,g.CurrentMission.waves.Length);
        var started=(bool[])Get(g,"encounterStarted");for(int i=0;i<started.Length;i++)started[i]=true;
        Set(g,"screenLocked",false);Set(g,"encounterLock",false);
    }
    // The hero stands at x, the camera on him.
    static void Stand(PanameGame.Player p,float x){p.position=new Vector2(x,0);Set(g,"cameraX",x+2.5f);Tick(2);}
    static object Spawn(string enemy,string entry,float x,float y=0,float fromX=0,float fromY=0,bool unaware=false,int facing=0,string opening=null,int group=0)
    {
        var s=new EncounterSpawn{enemy=enemy,entry=entry,x=x,y=y,fromX=fromX,fromY=fromY,unaware=unaware,facing=facing,definition=ContentLibrary.Enemy(enemy),kind=-1};
        string open=string.IsNullOrEmpty(opening)?entry:opening;if(entry=="door"||entry=="window")s.look=g.CurrentMission.ruleSet.stage.Opening(open);
        return Call(g,"Arrive",s,group,null);
    }
    static object Place(string enemy,float x,bool unaware=false,int facing=-1)=>Spawn(enemy,"none",x,0,0,0,unaware,facing);
    static int HostileShots(){int n=0;foreach(var s in shots)if((bool)Field(s,"hostile"))n++;return n;}
    static int HostileGrenades(){int n=0;foreach(var o in thrown)if((bool)Field(o,"hostile"))n++;return n;}

    public static object Main(string captures)
    {
        var checks=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        float scale=Time.timeScale;Time.timeScale=0;missionsField=typeof(PanameGame).GetField("missions",Flags);original=(MissionDefinition[])missionsField.GetValue(g);
        try
        {
            Library(checks);Entries(checks,captures);Awareness(checks);Panic(checks);Deaths(checks);Grenades(checks);
            MidBosses(checks,captures);JsonOnly(checks);Plain(checks);Replays(checks);Bots(checks);Captures(checks,captures);
        }
        finally
        {
            ContentLibrary.Reload();missionsField.SetValue(g,original);typeof(PanameGame).GetField("missionIndex",Flags).SetValue(g,0);
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;}
            foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);PlayerPrefs.DeleteKey("action-legends.unlocked.test-encounters");
            g.phase=PanameGame.Phase.Lobby;Call(g,"LoadMission",0);g.SetDifficulty(1);g.ReturnToLobby();Time.timeScale=scale;
        }
        return string.Join("\n",checks);
    }

    static void Library(List<string> checks)
    {
        string[] ids={"looter-rusher","looter-thrower","looter-shield","gunman-pistol","gunman-smg","gunman-shotgun"};bool states=true;var missing=new List<string>();
        foreach(var id in ids)
        {
            var e=ContentLibrary.Enemy(id);var a=e.animations;
            foreach(var s in new[]{"unaware","surprised","flee","death","deathExplosion","deathFire","deathKnife"})if(a.Find(s)<0){states=false;missing.Add(id+"."+s);}
            // Lot 3c appends rows of in-between images to the most common ones; the first sixteen cells stay.
            states&=a.sheet.texture=="Art/enemies/"+id&&a.CellCount>=16&&a.sheet.columns==4&&e.cries!=null;
        }
        Check(states,"Les six ennemis ont leur planche (Art/enemies/<id>.png, 16 cases au moins : les 4 anciennes à l'identique + inattentif, surpris, fuite, morts par balle, explosion, feu, couteau) et un cri"+(missing.Count>0?" ; manque "+string.Join(",",missing):""),checks);
        var lines=new List<string>();bool staged=true;
        foreach(var m in original)
        {
            var kinds=new HashSet<string>();int placed=0,bosses=0,horde=0;
            foreach(var c in m.encounters){if(c.HasBoss)bosses++;foreach(var s in c.spawns){kinds.Add(s.entry);placed++;}}
            foreach(var w in m.waves)horde+=w.baseEnemies+w.enemiesPerPlayer;
            staged&=bosses==1&&kinds.Count>=5&&placed>=25;
            lines.Add(m.id+" : "+m.waves.Length+" vagues ("+horde+" ennemis de horde en solo, avant "+(m.id.StartsWith("panique")?"12 vagues / 251":m.id.StartsWith("mistral")?"10 vagues / 235":"9 vagues / 175")+"), "+placed+" ennemis placés, entrées "+string.Join("/",kinds)+", mini-boss "+Array.Find(m.encounters,c=>c.HasBoss).boss.id);
        }
        Check(staged,"Chaque mission met en scène ses ennemis (au moins cinq sortes d'entrées, un mini-boss) : "+string.Join(" ; ",lines),checks);
        // The voices the earlier lots left pending, now recorded and wired in the data, and lot 2b's cries.
        var voices=new List<string>();foreach(var w in new[]{"heavy-machinegun","shotgun","flamethrower","rocket-launcher","laser"})voices.Add(ContentLibrary.Weapon(w).voice);
        voices.Add(Array.Find(ContentLibrary.Rules("pow").bonuses,b=>b.id=="grenades").voice);voices.Add(ContentLibrary.Hostage("baker").voice);
        voices.Add(ContentLibrary.Enemy("gunman-smg").cries[0]);voices.Add(ContentLibrary.Rules("pow").stage.alertVoice);voices.Add(ContentLibrary.Rules("pow").stage.panicVoice);voices.Add("Audio/Voices/miniboss-alert");
        bool takes=true;foreach(var v in voices)foreach(var t in new[]{"a","b","c"})takes&=!string.IsNullOrEmpty(v)&&Resources.Load<AudioClip>(v+"-"+t)!=null;
        Check(takes,"Voix en trois prises branchées dans les données : "+string.Join(", ",voices.ConvertAll(v=>v.Replace("Audio/Voices/",""))),checks);
        Check(ContentLibrary.Carrier("rusty-van").door.y>0&&ContentLibrary.Boss("armored-van").walker&&!ContentLibrary.Boss("speedboat").walker&&!ContentLibrary.Boss("combat-drone").walker,"Bibliothèque : deux véhicules de dépôt (Content/carriers) et trois mini-boss (fourgon blindé au sol, hors-bord et drone volants)",checks);
    }

    // Each entry: where the enemy appears, how long the team sees it come, and that it ends where the mission placed it.
    static void Entries(List<string> checks,string captures)
    {
        Start(0,1,1);var p=players[0];Quiet();Stand(p,40);float centre=(float)Get(g,"cameraX");
        var cases=new[]{("walk","looter-rusher",45f,0f,0f,0f),("door","looter-thrower",47f,0f,48f,0f),("window","looter-thrower",44f,2.6f,44f,2.6f),("leap","looter-rusher",43f,0f,41f,2.6f),
            ("parachute","looter-rusher",46f,0f,0f,0f),("drop","looter-rusher",42f,0f,0f,0f)};
        var lines=new List<string>();bool all=true;bool shotDoor=false,shotChute=false;
        foreach(var (name,enemy,x,y,fx,fy) in cases)
        {
            Clear(enemies);var e=Spawn(enemy,name=="leap"?"window":name,x,y,fx,fy);var start=Pos(e);float fade=(float)Field(e,"fade");
            bool chute=false,prop=((IList)Get(g,"props")).Count>0;int seen=0,t=0;float step=0;var last=start;
            while(Mode(e)==Entering&&t<600)
            {
                Tick(1);t++;var at=Pos(e);step=Mathf.Max(step,Mathf.Abs(at.x-last.x));last=at;
                if(Mathf.Abs(at.x-centre)<HalfWidth&&at.y<9.4f&&(float)Field(e,"fade")>.3f)seen++;chute|=Field(e,"chute") as Transform;
                if(name=="door"&&!shotDoor&&t==32){Capture(captures+"/lot2b-entry-door.png");shotDoor=true;}
                if(name=="parachute"&&!shotChute&&t==60){Capture(captures+"/lot2b-entry-parachute.png");shotChute=true;}
            }
            var end=Pos(e);bool placed=Mathf.Abs(end.x-x)<.01f&&Mathf.Abs(end.y-y)<.01f&&Mode(e)!=Entering;
            bool visible=name=="walk"?Mathf.Abs(start.x-centre)>HalfWidth&&seen>=30:name=="door"||name=="window"||name=="leap"?fade==0&&prop&&seen>=15:start.y>7&&seen>=30;
            if(name=="parachute")visible&=chute;
            bool ok=placed&&visible&&step<.4f&&(name!="window"||(bool)Field(e,"perched"));all&=ok;
            lines.Add(name+" "+(t/60f).ToString("0.0")+" s"+(ok?"":" ÉCHEC départ "+start+" fin "+end+" vu "+seen+" pas "+step));
        }
        Check(all,"Chaque entrée amène l'ennemi à sa place, vu de l'équipe, sans saut de position : "+string.Join(", ",lines)+" (porte et fenêtre : apparition en fondu dans l'ouverture des règles ; fenêtre : il y reste, perché)",checks);
        // The truck: drives in from the right edge, stops, lets them out one by one, drives off.
        Clear(enemies);var mission=g.CurrentMission;var saved=mission.encounters;
        var c=new MissionEncounter{atX=0,wave=-1,spawns=new[]{Truck(46),Truck(47),Truck(45)},truck=new EncounterTruck{id="rusty-van",side="right",x=46,definition=ContentLibrary.Carrier("rusty-van")}};
        mission.encounters=new[]{c};Set(g,"encounterStarted",new bool[1]);Call(g,"StartEncounter",0);
        var truck=carriers[0];float x0=(float)Field(truck,"x");int ticks=0,stops=0,out1=-1;bool shotTruck=false;
        while(carriers.Count>0&&ticks<1200){Tick(1);ticks++;if(carriers.Count>0&&(int)Field(truck,"state")==1){stops++;if(!shotTruck&&enemies.Count>=2){Capture(captures+"/lot2b-entry-truck.png");shotTruck=true;}}if(out1<0&&enemies.Count>0)out1=ticks;}
        int arrived=0;foreach(var e in enemies)if(Mode(e)!=Entering&&Mathf.Abs(Pos(e).y)<.01f)arrived++;
        mission.encounters=saved;Set(g,"encounterStarted",new bool[saved.Length]);
        Check(x0>centre+HalfWidth&&stops>0&&enemies.Count==3&&arrived==3&&carriers.Count==0,"Le camion arrive du bord droit (x "+x0.ToString("0")+"), s'arrête, dépose ses trois passagers un par un ("+(out1/60f).ToString("0.0")+" s après son départ), puis repart hors de l'écran",checks);
        // Already there: an enemy off guard placed beyond the screen, ahead of the team.
        Quiet();Stand(p,60);var mission0=g.CurrentMission;int camp=Array.FindIndex(mission0.encounters,k=>k.wave<0&&!k.HasBoss&&Array.Exists(k.spawns,s=>s.entry=="none"));
        var campEnc=mission0.encounters[camp];var started=(bool[])Get(g,"encounterStarted");started[camp]=false;Stand(p,campEnc.atX+.1f);Tick(2);
        bool ahead=true;float right=(float)Get(g,"cameraX")+HalfWidth;foreach(var e in enemies)ahead&=Pos(e).x>right&&Mode(e)==Unaware;
        Check(enemies.Count==campEnc.spawns.Length&&ahead,"Une rencontre « déjà là » pose ses "+enemies.Count+" ennemis inattentifs hors de l'écran, devant l'équipe (x > "+right.ToString("0")+")",checks);
        // A camp off guard ahead of a held screen does not keep a wave from ending; an enemy off guard inside the screen does.
        Set(g,"lockX",campEnc.atX-3);bool free=!(bool)Call(g,"EnemiesHold");var inside=Place("looter-rusher",campEnc.atX,true,1);bool heldIn=(bool)Call(g,"EnemiesHold");
        Check(free&&heldIn,"Un camp inattentif devant l'écran bloqué d'une vague ne la retient pas ; un ennemi inattentif dans l'écran, si",checks);
        // A wave waits for its staged enemies: it is not over until they have come and fallen.
        Start(0,1,1);p=players[0];Clear(hostages);Stand(p,8);int waited=0,staged=0;
        while(g.Wave==0&&waited<60*40)
        {
            foreach(var e in enemies)if((int)Field(e,"group")>=0)staged++;
            if(waited>60*6)for(int k=enemies.Count-1;k>=0;k--)if(Mode(enemies[k])!=Entering)Call(g,"HitEnemy",k,999,1,0);
            Tick(1);waited++;
        }
        Check(staged>0&&g.Wave==1&&waited>60*6,"La vague 1 de Paris mêle sa horde et ses ennemis placés (porte, bords) et attend qu'ils soient tombés ("+(waited/60f).ToString("0.0")+" s)",checks);
    }
    static EncounterSpawn Truck(float x)=>new EncounterSpawn{enemy="looter-rusher",entry="truck",x=x,definition=ContentLibrary.Enemy("looter-rusher"),kind=-1};

    // Off guard: no shot before the surprise, the "!" sign, the alarm spreading, a hit that wakes it up.
    static void Awareness(List<string> checks)
    {
        Start(2,1,1);var p=players[0];Quiet();Stand(p,40);
        // A gunman within range, facing away, beyond hearing: it does not notice and does not shoot.
        var gun=Place("gunman-pistol",45,true,1);int before=HostileShots();Tick(180);
        bool quiet=Mode(gun)==Unaware&&HostileShots()==before;
        // The hero steps in front of it: surprised for its surprise time, still without a shot, then it fires.
        Stand(p,50);int t=0,sign=0;while(Mode(gun)==Unaware&&t<60){Tick(1);t++;}float surprise=0;
        while(Mode(gun)==Surprised&&surprise<3){if(Field(gun,"sign") is Transform s&&s.gameObject.activeSelf)sign++;Tick(1);surprise+=1/60f;}
        bool silent=HostileShots()==before;int t2=0;while(HostileShots()==before&&t2<300){Tick(1);t2++;}
        var a=ContentLibrary.Enemy("gunman-pistol").awareness;
        Check(quiet&&t<5&&Mathf.Abs(surprise-a.surprise)<.05f&&sign>10&&silent&&HostileShots()>before,"Un tireur inattentif dans sa portée mais de dos ne tire pas (3 s) ; vu de face il sursaute "+surprise.ToString("0.00")+" s avec le « ! » sans tirer, puis tire "+(t2/60f).ToString("0.00")+" s plus tard",checks);
        // The alarm spreads to an ally nearby; a hit wakes one up.
        Quiet();Stand(p,30);var a1=Place("gunman-smg",45,true,1);var a2=Place("gunman-smg",48,true,1);var lone=Place("gunman-smg",60,true,1);
        Call(g,"Surprise",a1);bool chain=Mode(a2)==Surprised&&Mode(lone)==Unaware;
        int index=enemies.IndexOf(lone);Call(g,"HitEnemy",index,1,1,0);
        Check(chain&&Mode(lone)==Surprised,"L'alerte gagne l'allié proche (3 u) mais pas le lointain (15 u) ; une balle réveille l'inattentif",checks);
    }

    // Panic: low health (pistol), or allies fallen (rusher); it runs away, then comes back.
    static void Panic(List<string> checks)
    {
        Start(2,1,1);var p=players[0];Quiet();Stand(p,40);
        var e=Place("gunman-pistol",46);Tick(2);int hp=(int)Field(e,"hp"),max=(int)Field(e,"maxHp");
        Call(g,"HitEnemy",enemies.IndexOf(e),Mathf.CeilToInt(max*.65f),1,0);bool fled=Mode(e)==Fleeing;float x=Pos(e).x;Tick(30);bool away=Pos(e).x>x+.8f;
        int t=0;while(Mode(e)==Fleeing&&t<600){Tick(1);t++;}var f=ContentLibrary.Enemy("gunman-pistol").panic;
        Check(fled&&away&&Mode(e)==Active&&Mathf.Abs(t/60f+.5f-f.time)<.1f,"Sous "+(f.health*100)+" % de vie, un tireur s'enfuit loin du héros (+"+(Pos(e).x-x).ToString("0.0")+" u) pendant "+f.time+" s, puis revient au combat",checks);
        Quiet();Stand(p,40);var r=Place("looter-rusher",47);var d1=Place("looter-rusher",48);var d2=Place("looter-rusher",49);Tick(2);
        Call(g,"HitEnemy",enemies.IndexOf(d1),999,1,0);bool still=Mode(r)==Active;Call(g,"HitEnemy",enemies.IndexOf(d2),999,1,0);
        Check(still&&Mode(r)==Fleeing,"Un pillard s'enfuit quand deux de ses alliés tombent près de lui (panic.allies 2), pas au premier",checks);
        // In a locked screen it stays in view and always comes back, so the wave can end.
        Quiet();Stand(p,40);Set(g,"lockX",42.5f);Set(g,"screenLocked",true);var l=Place("gunman-pistol",50);Tick(2);
        Call(g,"HitEnemy",enemies.IndexOf(l),Mathf.CeilToInt((int)Field(l,"maxHp")*.65f),-1,0);
        bool fledLocked=Mode(l)==Fleeing;float far=Pos(l).x;Tick(180);far=Mathf.Max(far,Pos(l).x);
        Check(fledLocked&&far<=42.5f+HalfWidth-.59f&&Mode(l)==Active,"Dans un écran bloqué, le fuyard reste dans l'écran (x "+far.ToString("0.0")+" au plus, bord "+(42.5f+HalfWidth).ToString("0.0")+") et revient (mode "+Mode(l)+")",checks);Set(g,"screenLocked",false);
    }

    // Each wound plays its death state; the real tools give the right wound.
    static void Deaths(List<string> checks)
    {
        Start(0,1,1);var p=players[0];Quiet();Stand(p,40);var lines=new List<string>();bool all=true;
        string[] expect={"death","deathExplosion","deathFire","deathKnife"};
        for(int w=0;w<4;w++){Place("looter-shield",46);Call(g,"HitEnemy",0,999,1,w);bool ok=g.LastFall==expect[w]&&g.LastWound==w;all&=ok;lines.Add(Wounds.Names[w]+" > "+g.LastFall);Tick(1);}
        Check(all,"Chaque type de dégât joue sa mort : "+string.Join(", ",lines),checks);
        // The rifle, a grenade, the flame thrower and the knife.
        var tools=new List<string>();bool real=true;
        Quiet();SetField(Place("looter-rusher",48),"hp",1);p.facing=1;p.fireTimer=0;commands[0]=new PlayerCommand{fire=true};int t=0;while(enemies.Count>0&&t<200){Tick(1);t++;}commands[0]=default;real&=g.LastFall=="death";tools.Add("fusil > "+g.LastFall);
        Quiet();var target=Place("looter-thrower",45.5f);SetField(target,"hp",2);Tick(1);Call(g,"Explode",Pos(target)+Vector2.up*.5f);real&=g.LastFall=="deathExplosion";tools.Add("explosion de grenade > "+g.LastFall);
        Quiet();g.GiveWeapon(0,"flamethrower");SetField(Place("looter-rusher",45),"hp",2);commands[0]=new PlayerCommand{fire=true};t=0;while(enemies.Count>0&&t<200){Tick(1);t++;}commands[0]=default;real&=g.LastFall=="deathFire";tools.Add("lance-flammes > "+g.LastFall);
        Quiet();Place("looter-rusher",40.6f);var e=enemies[0];SetField(e,"hp",1);p.fireTimer=0;commands[0]=new PlayerCommand{fire=true};Tick(2);commands[0]=default;real&=g.LastFall=="deathKnife";tools.Add("couteau > "+g.LastFall);
        Check(real,"Les armes donnent leur blessure : "+string.Join(", ",tools)+" (damageType par défaut des armes : flamme = feu, explosion = explosion)",checks);
    }

    // An enemy grenade: lobbed at the hero, it hurts where it lands, and a hero who moves off is spared.
    static void Grenades(List<string> checks)
    {
        Start(2,1,1);var p=players[0];Quiet();Stand(p,40);
        var brute=Place("gunman-shotgun",48);SetField(brute,"grenadeTimer",0f);SetField(brute,"attackTimer",99f);int t=0;while(HostileGrenades()==0&&t<300){Tick(1,false);p.invulnerable=0;t++;}
        int hp=p.hp;bool thrownAtHero=HostileGrenades()>0;int t2=0;while(HostileGrenades()>0&&t2<300){Tick(1,false);t2++;}
        bool hurt=p.hp<hp;
        Quiet();p.hp=Mathf.Max(p.hp,3);Stand(p,40);p.invulnerable=0;
        brute=Place("gunman-shotgun",48);SetField(brute,"grenadeTimer",0f);SetField(brute,"attackTimer",99f);t=0;while(HostileGrenades()==0&&t<300){Tick(1,false);t++;}
        hp=p.hp;t2=0;commands[0]=new PlayerCommand{move=-1};while(HostileGrenades()>0&&t2<300){Tick(1,false);t2++;}commands[0]=default;
        Check(thrownAtHero&&hurt&&HostileGrenades()==0&&p.hp==hp,"Le colosse au fusil lance une grenade en cloche (grenade.range 5 à 11 u) : elle blesse le héros resté sur place, pas celui qui recule ("+(t2/60f).ToString("0.0")+" s de vol)",checks);
    }

    // Each mission's mid-boss: it holds the screen, the waves wait, its defeat lets the team go on and its troops run away.
    static void MidBosses(List<string> checks,string captures)
    {
        var lines=new List<string>();
        for(int m=0;m<3;m++)
        {
            Start(m,1,1);var p=players[0];var mission=g.CurrentMission;int index=Array.FindIndex(mission.encounters,c=>c.HasBoss);var enc=mission.encounters[index];
            // Straight to it: the waves and encounters before it are behind the team.
            Clear(enemies);Clear(hostages);int next=Array.FindIndex(mission.waves,w=>w.atX>enc.atX);typeof(PanameGame).GetProperty("Wave").SetValue(g,next);
            var started=(bool[])Get(g,"encounterStarted");for(int i=0;i<started.Length;i++)started[i]=i<index||mission.encounters[i].wave>=0&&mission.encounters[i].wave<next;
            Stand(p,enc.atX-.5f);commands[0]=new PlayerCommand{move=1};Tick(30);commands[0]=default;
            bool spawned=g.MidBossActive;float arena=(float)Get(g,"bossArenaX");
            // It fights the hero standing still for 12 s, then the hero tries to walk out of the arena.
            int t=0,troops=0;float bx=((Vector2)Get(g,"bossPosition")).x,minX=bx,maxX=bx;
            while(t<60*14){if(t==60*12)commands[0]=new PlayerCommand{move=1};Tick(1);t++;var bp=(Vector2)Get(g,"bossPosition");minX=Mathf.Min(minX,bp.x);maxX=Mathf.Max(maxX,bp.x);foreach(var e in enemies)if((int)Field(e,"group")==-2)troops++;if(t==300)Capture(captures+"/lot2b-miniboss-"+mission.id+".png");}
            bool held=p.position.x<=arena+HalfWidth-.99f&&Mathf.Abs((float)Get(g,"cameraX")-arena)<.01f&&g.Wave==next&&g.BossHealth>0;
            var boss=enc.boss.definition;bool hasTroops=Array.Exists(boss.attacks,a=>a.type=="troops");
            Call(g,"DamageBoss",100000);int t2=0;while(g.MidBossActive&&t2<600){Tick(1);t2++;}
            bool routed=true;foreach(var e in enemies)if((int)Field(e,"group")==-2)routed&=Mode(e)==Fleeing&&(bool)Field(e,"routed");
            int t3=0;while(p.position.x<arena+HalfWidth+1&&t3<600){Tick(1);t3++;}
            bool released=!g.MidBossActive&&!g.BossActive&&p.position.x>arena+HalfWidth&&g.phase==PanameGame.Phase.Playing&&(float)Get(g,"goFromX")>0;
            commands[0]=default;
            Check(spawned&&held&&released&&routed&&(!hasTroops||troops>0),boss.name+" ("+mission.id+", x "+enc.atX+") : il bloque l'écran (14 s, le héros ne peut pas en sortir), la vague "+(next+1)+" attend, il se déplace de "+minX.ToString("0.0")+" à "+maxX.ToString("0.0")+(hasTroops?", lâche des renforts":"")+" ; détruit, il libère le défilement (GO) et ses renforts s'enfuient",checks);
            lines.Add(boss.id);
        }
        // The armoured van's charge: a hero on the street in its path is hit, one jumping over it is not.
        Start(0,1,1);var q=players[0];var paris=g.CurrentMission;int bi=Array.FindIndex(paris.encounters,c=>c.HasBoss);Clear(enemies);Clear(hostages);
        typeof(PanameGame).GetProperty("Wave").SetValue(g,Array.FindIndex(paris.waves,w=>w.atX>paris.encounters[bi].atX));
        var st=(bool[])Get(g,"encounterStarted");for(int i=0;i<bi;i++)st[i]=true;Stand(q,paris.encounters[bi].atX+.1f);Tick(2);
        // One charge with the hero standing in its way, the next with him in the air over it.
        int grounds=0,airs=0,hitsOnGround=0,hitsInAir=0,charges=0,k=0;bool was=false;int hpAt=0;
        while(k<60*60&&(grounds<1||airs<1))
        {
            int attack=(int)Get(g,"bossAttack"),phase=(int)Get(g,"bossPhase");bool charging=g.MidBossActive&&ContentLibrary.Boss("armored-van").attacks[attack].kind==3&&phase==2;
            if(charging&&!was){charges++;q.invulnerable=0;q.hp=Mathf.Max(q.hp,3);hpAt=q.hp;}
            bool air=charges%2==0;
            if(charging){var bp=(Vector2)Get(g,"bossPosition");q.position=new Vector2(bp.x+(int)Get(g,"bossCharge")*1.2f,air?1.9f:0);q.vertical=0;}else q.invulnerable=1;
            g.TickSimulation(1f/60,commands);
            if(was&&!charging){if(air){airs++;if(q.hp<hpAt)hitsInAir++;}else{grounds++;if(q.hp<hpAt)hitsOnGround++;}}
            was=charging;k++;
        }
        bool hitOnGround=grounds>0&&hitsOnGround==grounds,clearedInAir=airs>0&&hitsInAir==0;
        Check(hitOnGround&&clearedInAir,"La charge du fourgon blindé touche le héros resté dans la rue, pas celui qui saute par-dessus (clearance 1,5 u)",checks);
    }

    // A mission written only as JSON: its encounters (every entry, a truck, a camp off guard) and its mid-boss, a library boss re-tuned by extends.
    const string TestVan=@"{""id"": ""test-van"", ""extends"": ""black-minivan"", ""speed"": 14, ""interval"": 0.3}";
    const string TestTank=@"{""id"": ""test-tank"", ""extends"": ""armored-van"", ""name"": ""CHAR DE TEST"", ""health"": [60, 80, 100, 120], ""score"": 2500,
      ""walk"": {""speed"": [3, 3, 3, 3], ""entry"": -2}, ""attacks"": [{""type"": ""troops"", ""announce"": ""TEST : RENFORTS"", ""state"": ""doors"", ""enemies"": [""gunman-pistol""], ""count"": [2, 2, 2, 2],
        ""interval"": [0.4, 0.4, 0.4, 0.4], ""duration"": 1, ""door"": ""door"", ""cooldown"": 1}]}";
    const string Mission=@"{""schemaVersion"": 2, ""id"": ""test-encounters"", ""order"": 900, ""title"": ""Rencontres de test"", ""location"": ""Banc d'essai"", ""intro"": ""ESSAI !"",
      ""exitLabel"": ""FIN"", ""winTitle"": ""ESSAI RÉUSSI"", ""music"": ""Audio/Music/marseillaise-arcade"", ""bossMusic"": ""Audio/Music/marseillaise-boss"",
      ""map"": {""backgrounds"": [""Art/route-01"", ""Art/route-02""], ""ground"": ""Art/paris""}, ""enemies"": [""looter-rusher"", ""gunman-pistol""],
      ""length"": 160, ""exitX"": 157, ""cameraMin"": 7, ""cameraMax"": 154, ""rules"": ""pow"",
      ""waves"": [{""atX"": 30, ""baseEnemies"": 0, ""enemiesPerPlayer"": 0, ""maxAlive"": 10, ""spawnInterval"": 0.5, ""enemyPattern"": [0]},
                 {""atX"": 120, ""baseEnemies"": 1, ""enemiesPerPlayer"": 0, ""maxAlive"": 4, ""spawnInterval"": 0.5, ""enemyPattern"": [1]}],
      ""encounters"": [
        {""wave"": 0, ""spawns"": [{""enemy"": ""looter-rusher"", ""entry"": ""walk"", ""x"": 28, ""side"": ""right""}, {""enemy"": ""gunman-pistol"", ""entry"": ""door"", ""x"": 26, ""fromX"": 27, ""opening"": ""shutter""},
          {""enemy"": ""gunman-smg"", ""entry"": ""window"", ""x"": 22, ""y"": 2.6, ""fromX"": 22, ""fromY"": 2.6, ""delay"": 0.5}, {""enemy"": ""looter-rusher"", ""entry"": ""parachute"", ""x"": 24, ""delay"": 1},
          {""enemy"": ""looter-rusher"", ""entry"": ""drop"", ""x"": 20, ""delay"": 1.5}, {""enemy"": ""gunman-pistol"", ""entry"": ""truck"", ""x"": 25}, {""enemy"": ""gunman-pistol"", ""entry"": ""truck"", ""x"": 27}],
         ""truck"": {""id"": ""test-van"", ""side"": ""right"", ""x"": 26}},
        {""atX"": 50, ""spawns"": [{""enemy"": ""gunman-pistol"", ""entry"": ""none"", ""x"": 66, ""unaware"": true, ""facing"": 1}, {""enemy"": ""looter-rusher"", ""entry"": ""none"", ""x"": 68, ""unaware"": true, ""facing"": -1}]},
        {""atX"": 80, ""boss"": {""id"": ""test-tank"", ""name"": ""CHAR D'ESSAI""}}],
      ""boss"": {""enabled"": false}}";
    static void JsonOnly(List<string> checks)
    {
        ContentLibrary.AddCarrier(TestVan);var tank=ContentLibrary.AddBoss(TestTank);var mission=MissionDefinition.Parse(Mission);
        var extended=new List<MissionDefinition>(original){mission};missionsField.SetValue(g,extended.ToArray());PlayerPrefs.SetInt("action-legends.unlocked.test-encounters",1);
        Start(original.Length,1,1);var p=players[0];int t=0;var entries=new HashSet<string>();bool truckSeen=false,boss=false,bossDown=false,unawareSeen=false;
        for(;t<60*60*5&&g.phase==PanameGame.Phase.Playing;t++)
        {
            commands[0]=Bot(p,0,t);Tick(1);truckSeen|=carriers.Count>0;
            foreach(var e in enemies){int entry=(int)Field(e,"entry");if(Mode(e)==Entering||entry==0)entries.Add(entry<EncounterSpawn.Entries.Length?EncounterSpawn.Entries[entry]:"hop");unawareSeen|=Mode(e)==Unaware;}
            if(g.MidBossActive)boss=true;if(boss&&!g.MidBossActive)bossDown=true;
        }
        Check(g.phase==PanameGame.Phase.Won&&entries.Count>=7&&truckSeen&&unawareSeen&&boss&&bossDown&&tank.name=="CHAR DE TEST","Une mission écrite seulement en JSON (rencontres de toutes les entrées, un véhicule test-van qui étend black-minivan, un camp inattentif, un mini-boss test-tank qui étend le fourgon blindé, sans boss final) se joue jusqu'à la victoire ("+(t/60)+" s ; entrées vues : "+string.Join(", ",entries)+")"+(g.phase==PanameGame.Phase.Won&&truckSeen&&unawareSeen&&boss&&bossDown?"":" ["+g.phase+" camion "+truckSeen+" camp "+unawareSeen+" boss "+boss+"/"+bossDown+" x "+p.position.x.ToString("0")+" vague "+g.Wave+" "+g.CurrentMission.id+" "+tank.name+"]"),checks);
        missionsField.SetValue(g,original);typeof(PanameGame).GetField("missionIndex",Flags).SetValue(g,0);ContentLibrary.Reload();
    }

    // An enemy with only idle, run and attack, placed off guard: every new state falls back on an old one.
    const string PlainEnemy=@"{""id"": ""test-plain"", ""extends"": ""looter-thrower"", ""panic"": {""health"": 0.5, ""time"": 1}, ""cries"": [],
      ""animations"": {""sheet"": {""texture"": ""Art/enemies/looter-thrower"", ""columns"": 4, ""rows"": 4}, ""states"": [{""name"": ""idle"", ""cells"": [4]}, {""name"": ""run"", ""cells"": [5, 6], ""perStride"": 0.7}, {""name"": ""attack"", ""cells"": [7]}],
        ""anchors"": [{""name"": ""hand"", ""cell"": 7, ""x"": -0.62, ""y"": 1.9}]}}";
    static void Plain(List<string> checks)
    {
        var type=ContentLibrary.AddEnemy(PlainEnemy);
        Start(0,1,1);var p=players[0];Quiet();Stand(p,40);
        var e=(object)null;var s=new EncounterSpawn{enemy="test-plain",entry="parachute",x=46,definition=type,kind=-1};e=Call(g,"Arrive",s,0,null);
        int t=0;while(Mode(e)==Entering&&t<600){Tick(1);t++;}bool landed=Mode(e)==Active;
        Call(g,"HitEnemy",enemies.IndexOf(e),Mathf.CeilToInt((int)Field(e,"maxHp")*.6f),1,0);bool fled=Mode(e)==Fleeing;Tick(90);
        var u=Place("test-plain",44,true,1);Call(g,"Surprise",u);Tick(60);
        Call(g,"HitEnemy",enemies.IndexOf(u),999,1,1);string fall=g.LastFall;
        bool states=type.unaware==type.idle&&type.surprised==type.idle&&type.flee==type.run&&type.deaths[1]<0;
        Check(landed&&fled&&fall=="tumble"&&states&&Mode(e)==Active,"Un ennemi sans les nouveaux états (idle, run, attack seulement) arrive en parachute, fuit, revient, sursaute (avec idle et le « ! ») et tombe sans état de mort (« tumble »)",checks);
        ContentLibrary.Reload();
    }

    // A recorded run, its flags as the replay keeps them, replayed in the hidden verifier and through online frames (VerifyLot2a).
    static ReplayRecord Record(int mission,int difficulty,int heroes,int limit,int giveUp,out List<string> frames)
    {
        Start(mission,difficulty,heroes);var moves=new int[4];var flags=new int[4];var text=new StringBuilder();frames=new List<string>();
        for(int t=0;t<limit&&g.phase==PanameGame.Phase.Playing;t++)
        {
            for(int i=0;i<4;i++){moves[i]=0;flags[i]=0;var q=players[i];if(q==null)continue;var c=Bot(q,i,t);if(g.ContinueOffered&&g.Continues>=giveUp)c=new PlayerCommand{grenade=t%30==0};moves[i]=Mathf.RoundToInt(c.move*10)*10;flags[i]=(c.fire?1:0)|(c.aimUp?2:0)|(c.jump?4:0)|(c.special?8:0)|(c.down?16:0)|(c.grenade?32:0);commands[i]=(PlayerCommand)Call(g,"Command",moves[i],flags[i]);}
            text.Clear().Append(0);for(int i=0;i<4;i++)text.Append(' ').Append(moves[i]).Append(' ').Append(flags[i]);frames.Add(text.ToString());
            Call(g,"RecordTick",0,moves,flags);g.TickSimulation(1f/60,commands);Call(g,"EndReplayTick");
        }
        return g.LastReplay;
    }
    static bool Verify(ReplayRecord record)
    {
        var verifier=PanameGame.CreateVerifier(original);bool ok=record!=null&&verifier.BeginVerify(JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record)));bool? result=null;
        while(ok&&result==null)result=verifier.StepVerify(1e6f);UnityEngine.Object.DestroyImmediate(verifier.gameObject);return ok&&result==true;
    }
    static bool Online(ReplayRecord record,List<string> frames)
    {
        var guest=PanameGame.CreateVerifier(original);guest.BeginVerify(JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record)));
        var queue=Get(guest,"netFrames");
        for(int i=0;i<frames.Count;i+=10)
        {
            Call(guest,"ParseFrames",string.Join("/",frames.GetRange(i,Mathf.Min(10,frames.Count-i))));
            while((int)queue.GetType().GetProperty("Count").GetValue(queue)>0)Call(guest,"RunFrame",queue.GetType().GetMethod("Dequeue").Invoke(queue,null));
        }
        bool ok=guest.Score==record.score&&(int)Call(guest,"StateHash")==record.hash&&guest.Continues==record.continues&&(guest.phase==PanameGame.Phase.Won)==record.won&&guest.phase!=PanameGame.Phase.Playing;
        UnityEngine.Object.DestroyImmediate(guest.gameObject);return ok;
    }
    static void Replays(List<string> checks)
    {
        var lines=new List<string>();
        foreach(var (m,d,h) in new[]{(0,1,2),(1,0,1),(2,3,1)})
        {
            var run=Record(m,d,h,60*60*15,0,out var frames);foreach(var mm in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+mm.id);
            Check(Verify(run)&&Online(run,frames),"Une partie sur "+original[Old(g,m)].id+" ("+PanameGame.DifficultyNames[d]+", "+h+" joueur(s), rencontres, mini-boss) se rejoue à l'identique en replay et en trames en ligne : "+(run.won?"victoire":"défaite")+", "+run.ticks+" pas",checks);
        }
        var slug=Record(0,Slug,1,60*60*20,3,out var slugFrames);foreach(var mm in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+mm.id);
        Check(slug.continues>=1&&Verify(slug)&&Online(slug,slugFrames),"Une partie en SLUG sur Paris avec "+slug.continues+" continues se rejoue à l'identique ("+(slug.won?"victoire":"défaite")+", "+slug.ticks+" pas)",checks);
    }

    // VerifyLot2a's bot: walks to the nearest enemy, crouches, stabs, throws grenades, stands under the boss; its jumps answer CONTINUE?.
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
    static void Bots(List<string> checks)
    {
        var lines=new List<string>();
        for(int d=0;d<PanameGame.DifficultyCount;d++)for(int m=0;m<3;m++)
        {
            bool slug=d==Slug;Start(m,d,1);int t=0;for(;t<60*60*30&&g.phase==PanameGame.Phase.Playing;t++){commands[0]=Bot(players[0],0,t);Tick(1,!slug);}
            if(g.phase!=PanameGame.Phase.Won)throw new Exception("ÉCHEC : bot de "+g.CurrentMission.id+" en "+PanameGame.DifficultyNames[d]+" ("+g.phase+", "+t/60+" s)");
            lines.Add(PanameGame.DifficultyNames[d]+" "+g.CurrentMission.id.Split('-')[0]+" "+t/60+" s"+(slug?" ("+g.Continues+" continues)":""));
        }
        Check(lines.Count==3*PanameGame.DifficultyCount,"Un bot gagne les trois missions dans chaque difficulté (invulnérable de DÉTENTE à LÉGENDE, vulnérable en SLUG avec continues) : "+string.Join(", ",lines),checks);
    }

    // The new animations of each enemy side by side, on its mission's street.
    static void Captures(List<string> checks,string captures)
    {
        int taken=0;
        foreach(var (m,ids) in new[]{(0,new[]{"looter-rusher","looter-thrower","looter-shield"}),(2,new[]{"gunman-pistol","gunman-smg","gunman-shotgun"})})
            foreach(var id in ids)
            {
                Start(m,1,1);var p=players[0];Quiet();Stand(p,30);p.position=new Vector2(23.5f,0);Call(g,"SyncPlayer",0);
                var type=ContentLibrary.Enemy(id);var set=type.animations;
                var poses=new[]{("unaware",0f),("surprised",0f),("flee",.2f),(type.throwing!=type.attack?"throw":"hurt",0f),("death",.5f),("deathExplosion",.1f),("deathFire",0f),("deathFire",1f)};
                for(int i=0;i<poses.Length;i++)
                {
                    var e=Place(id,25.5f+i*2.15f,false,-1);int state=set.Find(poses[i].Item1);
                    SetField(e,"animState",state);SetField(e,"animTime",poses[i].Item2);SetField(e,"walk",poses[i].Item2*10);Call(g,"ShowEnemy",e,state,0f);
                    if(poses[i].Item1=="surprised"){SetField(e,"mode",Surprised);SetField(e,"modeTime",.3f);Call(g,"ShowEnemy",e,state,0f);}
                }
                Set(g,"cameraX",33.5f);Capture(captures+"/lot2b-"+id+".png");taken++;
            }
        Check(taken==6,"Captures : chaque ennemi inattentif, surpris (« ! »), en fuite, au lancer ou touché, puis ses morts par balle, explosion et feu (lot2b-<ennemi>.png), les entrées (lot2b-entry-*.png) et les mini-boss (lot2b-miniboss-*.png)",checks);
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
