using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Paname;
// Lot 3c, a livelier game. The heroes strike their hurt, down and victory poses (drawn only: the gun still fires from the
// simulated image, and a game plays to the same hash with every pose and screen effect turned off); the enemies' richer
// cycles play (four-image run and panic, attack follow-through, three-image death); dust, jolts and casings stay within the
// effects' pool; the announcements' banner has its rules switch; Tokyo's bottom stair steps stand as relief; a dash held at
// the arena's edge still strikes a boss pinned there; and a vehicle against the mid-mission bosses: its boss damage (the
// weapons' bossFactor) against the rifle's, in duels and with VerifyLot3b's bot keeping its vehicle as long as it can.
public static class VerifyLot3c
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static void Set(object o,string name,object v)=>o.GetType().GetField(name,Flags).SetValue(o,v);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags|BindingFlags.Static).Invoke(o,args);
    static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
    static void SetField(object o,string name,object v)=>o.GetType().GetField(name).SetValue(o,v);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    static Vector2 Pos(object o)=>(Vector2)Field(o,"position");
    static PanameGame g;static PanameGame.Player[] players;static IList enemies,shots,thrown,hostages,pickups,arrivals,carriers,rushes;static List<PanameGame.Vehicle> vehicles;
    static readonly PlayerCommand[] commands=new PlayerCommand[4];
    static MissionDefinition[] original;static FieldInfo missionsField;
    static int M(string id)=>Array.FindIndex((MissionDefinition[])missionsField.GetValue(g),x=>x.id==id);
    static int H(string id)=>ContentLibrary.HeroIndex(id);
    static readonly string[] Four={"panique-a-paname","mistral-gagnant","prophetie-en-provence","tokyo-neon"};
    static void Start(int mission,int difficulty,int heroes,int hero=0)
    {
        g.ReturnToLobby();Call(g,"LoadMission",mission);g.SetDifficulty(difficulty);
        for(int i=0;i<4;i++){g.slots[i].joined=i<heroes;g.slots[i].hero=(hero+i)%PanameGame.HeroCount;}
        g.BeginRound();players=(PanameGame.Player[])Get(g,"players");enemies=(IList)Get(g,"enemies");shots=(IList)Get(g,"shots");thrown=(IList)Get(g,"thrown");
        hostages=(IList)Get(g,"hostages");pickups=(IList)Get(g,"pickups");arrivals=(IList)Get(g,"arrivals");carriers=(IList)Get(g,"carriers");rushes=(IList)Get(g,"rushes");
        vehicles=(List<PanameGame.Vehicle>)Get(g,"vehicles");for(int i=0;i<4;i++)commands[i]=default;
    }
    static void Tick(int n=1,bool invulnerable=true){for(int t=0;t<n;t++){if(invulnerable)foreach(var p in players)if(p!=null&&p.hp>0)p.invulnerable=1;g.TickSimulation(1f/60,commands);}}
    static void Clear(IList list){foreach(var o in list){var r=o.GetType().GetField("root")?.GetValue(o) as Transform;if(r)r.gameObject.SetActive(false);}list.Clear();}
    static void ClearShots(){foreach(var shot in shots){SetField(shot,"active",false);((Transform)Field(shot,"root")).gameObject.SetActive(false);}shots.Clear();}
    static void Quiet()
    {
        Clear(enemies);Clear(hostages);Clear(pickups);arrivals.Clear();Clear(carriers);Clear(thrown);ClearShots();
        typeof(PanameGame).GetProperty("Wave").SetValue(g,g.CurrentMission.waves.Length);
        var started=(bool[])Get(g,"encounterStarted");for(int i=0;i<started.Length;i++)started[i]=true;
        Set(g,"screenLocked",false);Set(g,"encounterLock",false);Set(g,"waveLive",false);
    }
    static float SolidAt(float x)=>(float)Call(g,"SolidAt",x,1);
    static void Stand(PanameGame.Player p,float x){if(p.vehicle!=null)Call(g,"LeaveVehicle",p,false);p.position=new Vector2(x,SolidAt(x));p.vertical=0;p.dropFrom=float.MaxValue;p.facing=1;Set(g,"cameraX",x+2.5f);Tick(2);}
    static object Place(string enemy,float x)
    {
        var s=new EncounterSpawn{enemy=enemy,entry="none",x=x,definition=ContentLibrary.Enemy(enemy),kind=-1};return Call(g,"Arrive",s,0,null);
    }
    static Sprite Shown(PanameGame.Player p)=>p.illustrated.sprite;
    static Sprite CellSprite(PanameGame.Player p,int state)=>p.Definition.animations.Sprite(p.Definition.animations.states[state].cells[0]);
    static MissionEncounter MidBoss(){foreach(var c in g.CurrentMission.encounters??new MissionEncounter[0])if(c.HasBoss)return c;return null;}
    // The mission's mid-boss, on the spot where it is met, the hero at the left of its arena.
    static void MeetMidBoss(MissionEncounter c)
    {
        Set(g,"cameraX",c.atX+2.5f);
        typeof(PanameGame).GetMethod("SpawnBoss",Flags,null,new[]{typeof(MissionBoss)},null).Invoke(g,new object[]{c.boss});
    }

    public static object Main(string captures)
    {
        var checks=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        float scale=Time.timeScale;Time.timeScale=0;
        missionsField=typeof(PanameGame).GetField("missions",Flags);original=(MissionDefinition[])missionsField.GetValue(g);
        var only=Environment.GetEnvironmentVariable("LOT3C_ONLY");bool Part(string name)=>string.IsNullOrEmpty(only)||Array.IndexOf(only.Split(','),name)>=0;
        try
        {
            if(Part("Library"))Library(checks);
            if(Part("Poses"))Poses(checks,captures);
            if(Part("Hash"))SameHash(checks);
            if(Part("Enemies"))Enemies(checks,captures);
            if(Part("Screen"))Screen(checks);
            if(Part("Tokyo"))Tokyo(checks,captures);
            if(Part("Dash"))Dash(checks);
            if(Part("Duels"))Duels(checks);
            if(Part("Bots"))Bots(checks);
        }
        finally
        {
            missionsField.SetValue(g,original);Set(g,"missionIndex",0);ContentLibrary.Reload();
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;}
            foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
            g.ReturnToLobby();Call(g,"LoadMission",0);g.SetDifficulty(1);g.ReturnToLobby();Time.timeScale=scale;
        }
        return string.Join("\n",checks);
    }

    // ---------- Data ----------
    const string Step=@"{""type"": ""block"", ""x"": 40, ""width"": 1, ""height"": 0.24}";
    static string Stairs(float drawX)=>@"{""type"": ""slope"", ""x"": 41, ""width"": 4, ""height"": 0.24, ""toHeight"": 1.2, ""sprite"": {""texture"": ""Art/props/tokyo-terrain"", ""columns"": 3, ""rows"": 1, ""cell"": 1}, ""drawX"": "+drawX.ToString(System.Globalization.CultureInfo.InvariantCulture)+@", ""drawWidth"": 5.8}";
    static string Mission(string terrain)=>@"{""schemaVersion"": 2, ""id"": ""test-3c"", ""order"": 900, ""title"": ""Essai"", ""location"": ""Banc d'essai"", ""intro"": ""ESSAI !"",
      ""exitLabel"": ""FIN"", ""winTitle"": ""ESSAI RÉUSSI"", ""music"": ""Audio/Music/marseillaise-arcade"", ""bossMusic"": ""Audio/Music/marseillaise-boss"",
      ""map"": {""backgrounds"": [""Art/route-01"", ""Art/route-02""], ""ground"": ""Art/paris"", ""terrain"": ["+terrain+@"]},
      ""enemies"": [""looter-rusher""], ""length"": 200, ""exitX"": 197, ""cameraMin"": 7, ""cameraMax"": 194,
      ""waves"": [{""atX"": 122, ""baseEnemies"": 2, ""enemiesPerPlayer"": 0, ""maxAlive"": 4, ""spawnInterval"": 0.5, ""enemyPattern"": [0]}],
      ""boss"": {""enabled"": true, ""id"": ""armored-van"", ""health"": 1}}";
    static void Library(List<string> checks)
    {
        var rules=ContentLibrary.Rules("default");var p=rules.poses;var s=rules.screen;
        Check(p.hurt>0&&p.victory>0&&p.lie>0&&p.fall.time>0&&s.shake>0&&s.dust>0&&s.push>0&&s.banner,"Règles par défaut : poses (blessure "+p.hurt+" s, chute "+p.fall.time+" s, à terre "+p.lie+" s, victoire "+p.victory+" s), secousse "+s.shake+" (verticale ×"+s.vertical+"), poussière "+s.dust+", sursaut "+s.push+" u, bandeau d'annonces activé",checks);
        bool RulesRefused(string json){try{ContentLibrary.AddRules(json);return false;}catch(ArgumentException){return true;}}
        Check(RulesRefused(@"{""id"": ""test-3c-a"", ""extends"": ""default"", ""poses"": {""hurt"": 5}}")&&RulesRefused(@"{""id"": ""test-3c-b"", ""extends"": ""default"", ""screen"": {""shake"": 3}}")
            &&RulesRefused(@"{""id"": ""test-3c-c"", ""extends"": ""default"", ""poses"": {""fall"": {""back"": 9}}}"),"Des poses ou des effets d'écran hors bornes sont refusés",checks);
        bool WeaponRefused(string json){try{ContentLibrary.AddWeapon(json);return false;}catch(ArgumentException){return true;}}
        var gun=ContentLibrary.Weapon("mitrailleuse-de-bord");var shell=ContentLibrary.Weapon("obus-de-char");
        Check(WeaponRefused(@"{""id"": ""test-3c-arme"", ""extends"": ""rifle"", ""bossFactor"": 5}")&&ContentLibrary.Weapon("rifle").bossFactor==1&&ContentLibrary.Weapon("rifle").BossDamage(3)==3,
            "bossFactor d'une arme : 1 par défaut, refusé au-delà de 4",checks);
        // Against a boss the vehicle's gun keeps a clear edge on the rifle (between 1.1 and 1.5 times its damage per second).
        var rifle=ContentLibrary.Weapon("rifle");float rifleDps=rifle.damage/rifle.fireRate,gunDps=gun.BossDamage(gun.damage)/gun.fireRate,shellDps=shell.BossDamage(shell.damage)/shell.fireRate;
        Check(gun.bossFactor<1&&shell.bossFactor<1&&gunDps>rifleDps*1.1f&&gunDps<rifleDps*1.5f,"Contre un boss : mitrailleuse de bord "+gun.BossDamage(gun.damage)+" par balle ("+gunDps.ToString("0")+" PV/s, "+(gun.damage/gun.fireRate).ToString("0")+" contre les ennemis), obus "+shell.BossDamage(shell.damage)+" ("+shellDps.ToString("0")+" PV/s au plus), fusil "+rifleDps.ToString("0")+" PV/s",checks);
        bool Parsed(string terrain){try{MissionDefinition.Parse(Mission(terrain));return true;}catch(ArgumentException){return false;}}
        Check(Parsed(Step+", "+Stairs(-1))&&!Parsed(Step+", "+Stairs(0)),"Une marche sans dessin est acceptée dans le dessin de son escalier, refusée hors de tout dessin",checks);
        var e=ContentLibrary.Enemy("looter-rusher");var a=e.animations;
        Check(a.sheet.rows==6&&a.states[e.run].cells.Length==4&&a.states[e.death].cells.Length==3&&a.states[e.attack].cells.Length==2,"Pillard : planche de 6 lignes, course en 4 images, mort en 3, attaque en 2",checks);
    }

    // ---------- Hero poses ----------
    static void Poses(List<string> checks,string captures)
    {
        Start(M("panique-a-paname"),1,2,0);Quiet();var p=players[0];var q=players[1];Stand(p,40);q.position=new Vector2(43,0);Tick(2);
        var h=p.Definition;var rules=g.CurrentMission.ruleSet.poses;
        // Hurt: the hurt image for a moment, while the simulated state and the gun's muzzle stay those of the standing hero.
        p.invulnerable=0;Call(g,"Damage",p,1);int simulated=p.animState;Tick(1,false);
        bool hurt=Shown(p)==CellSprite(p,h.hurtPose)&&p.animState==simulated;
        commands[0]=new PlayerCommand{fire=true};Tick(1,false);commands[0]=default;Vector2 muzzle=(Vector2)Call(g,"MuzzlePoint",p);
        var shot=shots[shots.Count-1];bool fired=Shown(p)==CellSprite(p,h.hurtPose)&&Vector2.Distance(Pos(shot)-(Vector2)Field(shot,"velocity")/60f,muzzle)<.02f;
        Capture(captures+"/lot3c-pose-blesse.png");
        p.invulnerable=1;Tick(Mathf.CeilToInt(rules.hurt*60)+2);bool after=Shown(p)!=CellSprite(p,h.hurtPose);
        Check(hurt&&fired&&after,"Coup reçu : l'image « hurt » pendant "+rules.hurt+" s, l'état simulé reste "+h.animations.states[simulated].name+" et le tir part de son canon",checks);
        // Down: knocked back, then lying until he gets up, blinking first; the other hero carries on.
        p.invulnerable=0;p.hp=1;Call(g,"Damage",p,1);Tick(Mathf.CeilToInt(rules.fall.time*60)+3);
        bool down=p.hp<=0&&p.root.gameObject.activeSelf&&Shown(p)==CellSprite(p,h.downPose)&&Mathf.Abs(p.root.position.y-p.floor)<.01f;
        Set(g,"cameraX",p.position.x+2);Capture(captures+"/lot3c-pose-a-terre.png");
        int waited=0;bool blinked=false;while(p.hp<=0&&waited<600){Tick(1);waited++;if(p.root.gameObject.activeSelf&&p.illustrated.color.a<.5f)blinked=true;}
        Check(down&&blinked&&p.hp>0&&Shown(p)!=CellSprite(p,h.downPose),"KO : renversé puis « down » à terre jusqu'au renfort ("+(waited/60f).ToString("0.0")+" s), en clignotant avant de se relever",checks);
        // Out of lives: he lies there for a while, then is gone.
        typeof(PanameGame).GetProperty("Reserves").SetValue(g,0);p.invulnerable=0;p.hp=1;Call(g,"Damage",p,1);Tick(30);bool lying=p.root.gameObject.activeSelf;
        Tick(Mathf.CeilToInt((rules.lie+rules.fall.time)*60)+5);
        Check(lying&&!p.root.gameObject.activeSelf,"Sans renfort, il reste "+rules.lie+" s à terre puis disparaît",checks);
        // Victory: after a boss falls, a hero standing still and holding his fire shows it; the one who moves does not.
        Start(M("panique-a-paname"),1,2,0);Quiet();p=players[0];q=players[1];Stand(p,200);q.position=new Vector2(202,0);var mid=MidBoss();MeetMidBoss(mid);Tick(2);
        Call(g,"DamageBoss",99999);commands[1]=new PlayerCommand{move=1};Tick(30);
        bool still=Shown(p)==CellSprite(p,h.victoryPose),moving=Shown(q)!=CellSprite(q,q.Definition.victoryPose);
        Set(g,"cameraX",p.position.x+2);Capture(captures+"/lot3c-pose-victoire.png");commands[1]=default;
        Tick(Mathf.CeilToInt(rules.victory*60)+10);bool over=Shown(p)!=CellSprite(p,h.victoryPose);
        Check(still&&moving&&over,"Boss abattu : « victory » pour le héros immobile ("+rules.victory+" s), pas pour celui qui court",checks);
        // At the end of the mission everyone keeps it.
        Start(M("panique-a-paname"),1,1,H("jeanne-d-arc"));Quiet();p=players[0];Stand(p,60);g.phase=PanameGame.Phase.Won;Call(g,"SyncPlayer",0);
        Check(Shown(p)==CellSprite(p,p.Definition.victoryPose),"Mission gagnée : pose de victoire",checks);
        g.phase=PanameGame.Phase.Playing;
        // A hero without these states keeps his images (and disappears when he falls, as before).
        var plain=ContentLibrary.AddHero(@"{""id"": ""test-3c-heros"", ""extends"": ""arnold"", ""order"": 950}");
        var states=new List<Paname.AnimationState>(plain.animations.states);states.RemoveAll(x=>x.name=="hurt"||x.name=="down"||x.name=="victory");plain.animations.states=states.ToArray();plain.Validate();
        Start(M("panique-a-paname"),1,1,H("test-3c-heros"));Quiet();p=players[0];Stand(p,40);p.invulnerable=0;p.hp=1;Call(g,"Damage",p,1);Tick(2);
        Check(plain.hurtPose<0&&plain.downPose<0&&!p.root.gameObject.activeSelf,"Un héros sans « hurt », « down », « victory » garde ses images et disparaît en tombant",checks);
        ContentLibrary.Reload();
    }
    // The same inputs give the same game whether the poses and screen effects are on or off.
    static void SameHash(List<string> checks)
    {
        var rules=original[0].ruleSet;var poses=JsonUtility.ToJson(rules.poses);var screen=JsonUtility.ToJson(rules.screen);
        int Play(){Start(0,1,2,0);int hp=0;for(int t=0;t<60*40&&g.phase==PanameGame.Phase.Playing;t++){for(int i=0;i<2;i++)commands[i]=new PlayerCommand{move=t%300<240?1:-.4f,fire=t%3!=0,jump=t%53==i*7,grenade=t%120==i*30,special=t%240==i*50};g.TickSimulation(1f/60,commands);hp+=players[0].hp;}return (int)Call(g,"StateHash")^hp*31^g.Score;}
        int on=Play();
        JsonUtility.FromJsonOverwrite(@"{""hurt"": 0, ""blink"": 0, ""lie"": 0, ""victory"": 0, ""fall"": {""time"": 0, ""back"": 0, ""height"": 0}}",rules.poses);
        JsonUtility.FromJsonOverwrite(@"{""shake"": 0, ""vertical"": 0, ""dust"": 0, ""push"": 0, ""hop"": 0, ""tilt"": 0, ""casings"": 0, ""banner"": false}",rules.screen);
        int off;try{off=Play();}finally{JsonUtility.FromJsonOverwrite(poses,rules.poses);JsonUtility.FromJsonOverwrite(screen,rules.screen);}
        Check(on==off,"40 s de jeu à deux donnent la même empreinte avec ou sans poses, secousse, poussière, sursauts et douilles (dessin seul)",checks);
    }

    // ---------- Enemies ----------
    static void Enemies(List<string> checks,string captures)
    {
        var lines=new List<string>();
        foreach(var (mission,id) in new[]{("panique-a-paname","looter-rusher"),("mistral-gagnant","looter-thrower"),("tokyo-neon","gunman-pistol"),("prophetie-en-provence","arlesien")})
        {
            Start(M(mission),1,1);Quiet();var p=players[0];Stand(p,40);var e=Place(id,54);var type=(EnemyDefinition)Field(e,"type");var set=type.animations;
            SetField(e,"mode",0);var seen=new HashSet<Sprite>();var illustrated=(SpriteRenderer)Field(e,"illustrated");
            for(int t=0;t<60;t++){Tick(1);if((int)Field(e,"animState")==type.run)seen.Add(illustrated.sprite);}
            int runs=seen.Count;
            Set(g,"cameraX",50f);Capture(captures+"/lot3c-"+id+"-course.png");
            int index=enemies.IndexOf(e);Call(g,"HitEnemy",index,999,1,Wounds.Bullet);Tick(9);Capture(captures+"/lot3c-"+id+"-mort.png");
            if(runs<4||g.LastFall!="death")throw new Exception("ÉCHEC : "+id+" : "+runs+" images de course, chute "+g.LastFall);
            lines.Add(id+" ("+runs+" images de course)");
        }
        Check(true,"Ennemis les plus présents : la course passe par ses quatre images, la mort par balle joue « death » (3 images) : "+string.Join(", ",lines),checks);
    }

    // ---------- Screen ----------
    static void Screen(List<string> checks)
    {
        // The frame loop still steps a local game (the victory poses are refreshed in Update, outside its chain of phases).
        Start(M("panique-a-paname"),1,1);float clock=g.SimulationTime;Set(g,"localClock",3.5f/60);typeof(PanameGame).GetMethod("Update",Flags).Invoke(g,null);
        Check(g.SimulationTime>clock+2.5f/60,"Update fait avancer une partie locale ("+Mathf.RoundToInt((g.SimulationTime-clock)*60)+" pas)",checks);
        Start(M("panique-a-paname"),1,1);Quiet();var p=players[0];Stand(p,40);Tick(40);int before=g.ActiveEffects;
        commands[0]=new PlayerCommand{jump=true};Tick(1);commands[0]=default;int landed=0;for(int t=0;t<80;t++){Tick(1);landed=Mathf.Max(landed,g.ActiveEffects);}
        var e=Place("looter-rusher",44);SetField(e,"mode",0);SetField(e,"hp",500);Tick(30);var art=((SpriteRenderer)Field(e,"illustrated")).transform;
        Call(g,"HitEnemy",enemies.IndexOf(e),1,1,Wounds.Bullet);Tick(1);float jolt=art.localPosition.magnitude;Tick(20);float rest=art.localPosition.magnitude;
        Clear(enemies);
        commands[0]=new PlayerCommand{fire=true};int peak=0;for(int t=0;t<240;t++){Tick(1);peak=Mathf.Max(peak,g.ActiveEffects);}commands[0]=default;
        Check(landed>before&&jolt>.02f&&rest<.001f&&peak<=g.EffectPoolCapacity&&g.EffectPoolCapacity<=128,"Poussière à l'atterrissage ("+before+" → "+landed+" effets), sursaut d'un ennemi touché ("+jolt.ToString("0.00")+" u, puis 0), tir continu : "+peak+" effets au plus (réserve de "+g.EffectPoolCapacity+")",checks);
    }

    // ---------- Tokyo ----------
    static void Tokyo(List<string> checks,string captures)
    {
        Start(M("tokyo-neon"),1,1);Quiet();var p=players[0];
        float left=SolidAt(278),right=SolidAt(295),street=SolidAt(276);
        Stand(p,276);for(int t=0;t<40;t++){commands[0]=new PlayerCommand{move=1};Tick(1);}commands[0]=default;
        Set(g,"cameraX",280f);Capture(captures+"/lot3c-tokyo-marche.png");
        Check(Mathf.Abs(left-.24f)<.01f&&Mathf.Abs(right-.24f)<.01f&&street==0&&p.position.x>279&&p.position.y>.24f,"Tokyo : les petites marches au pied des escaliers ont leur relief (0,24 u) ; le héros les monte sans sauter (x "+p.position.x.ToString("0.0")+", y "+p.position.y.ToString("0.00")+")",checks);
    }

    // ---------- Dash at the edge ----------
    static void Dash(List<string> checks)
    {
        var lines=new List<string>();
        foreach(var mission in new[]{"panique-a-paname","prophetie-en-provence"})
        {
            Start(M(mission),1,1,H("jeanne-d-arc"));Quiet();var p=players[0];var mid=MidBoss();Stand(p,mid.atX);MeetMidBoss(mid);Tick(1);
            float arena=(float)Get(g,"bossArenaX"),edge=arena+(float)typeof(PanameGame).GetProperty("ArenaHalfWidth",Flags).GetValue(g);
            // The boss as it comes in, past the edge the heroes stop at; the hero charges from 7 units away.
            Set(g,"bossPosition",new Vector2(edge+3,0));Set(g,"bossTimer",99f);p.position=new Vector2(edge-8,0);p.facing=1;p.specialTimer=0;Tick(1);
            var centre=(Vector2)Call(g,"HitCenter",p)+new Vector2(edge-1-p.position.x,0);bool missed=!(bool)Call(g,"BossWithin",centre,p.Definition.special.radius);
            int before=g.BossHealth;commands[0]=new PlayerCommand{special=true};Tick(1);commands[0]=default;
            for(int t=0;t<50;t++){Set(g,"bossPosition",new Vector2(edge+3,0));Tick(1);}
            if(!missed||g.BossHealth>=before)throw new Exception("ÉCHEC : charge au bord ("+mission+") : manquée d'avance "+missed+", PV "+before+" → "+g.BossHealth);
            lines.Add(Boss(mission)+" "+before+" → "+g.BossHealth);
        }
        Check(true,"Une charge (dash) arrêtée au bord de l'arène frappe encore devant elle le boss collé au bord, hors de portée du héros lui-même : "+string.Join(", ",lines),checks);
    }
    static string Boss(string mission)=>g.CurrentMission.encounters==null?mission:MidBoss()?.boss.id??mission;

    // ---------- Vehicles against the mid-bosses ----------
    // A duel against each mission's mid-boss: the hero invulnerable, on foot (rifle and grenades) or in the mission's vehicle
    // (its health kept up), stays near the boss, fires all along, aims up at a flyer, throws a grenade or fires a shell every
    // 40 steps. Seconds to bring it down, and the damage per second that makes.
    static float Duel(string mission,bool driving,out int health)
    {
        Start(M(mission),1,1);Quiet();var p=players[0];var mid=MidBoss();Stand(p,mid.atX);
        if(driving){var v=vehicles[0];v.position=new Vector2(mid.atX-1.5f,SolidAt(mid.atX-1.5f));v.rider=null;v.left=null;p.position=v.position;Tick(2);if(p.vehicle!=v)throw new Exception("ÉCHEC : pas monté");}
        MeetMidBoss(mid);Tick(1);health=g.BossHealth;int t=0;
        for(;t<60*120&&g.BossActive;t++)
        {
            if(p.vehicle!=null){p.vehicle.hp=p.vehicle.maxHp;}
            var boss=(Vector2)Get(g,"bossPosition");float dx=boss.x-p.position.x;bool high=boss.y-p.position.y>2.2f;
            float move=Mathf.Abs(dx)>(high?1.5f:4.5f)?Mathf.Sign(dx):Mathf.Sign(dx)*.15f;
            commands[0]=new PlayerCommand{move=move,fire=true,aimUp=high&&Mathf.Abs(dx)<3,grenade=t%40==0};Tick(1);
        }
        commands[0]=default;return t/60f;
    }
    static void Duels(List<string> checks)
    {
        var lines=new List<string>();bool crushed=false;
        foreach(var id in Four)
        {
            float foot=Duel(id,false,out int hp),car=Duel(id,true,out _);
            lines.Add(Boss(id)+" ("+hp+" PV) : à pied "+foot.ToString("0.0")+" s ("+(hp/foot).ToString("0")+" PV/s), au volant "+car.ToString("0.0")+" s ("+(hp/car).ToString("0")+" PV/s)");
            if(car<foot/1.6f)crushed=true;
        }
        if(crushed)throw new Exception("ÉCHEC : le véhicule écrase un mini-boss : "+string.Join(" ; ",lines));
        Check(true,"Duels contre les mini-boss (ARCADE, invulnérable) : "+string.Join(" ; ",lines),checks);
    }

    // VerifyLot3b's bot (on foot it walks into the vehicle ahead; driving, it never gets out): the mid-boss fight lasts how long,
    // and is it driving when the boss comes; and without any vehicle to compare.
    static PlayerCommand Bot(PanameGame.Player p,int i,int t)
    {
        float nearest=float.MaxValue,dir=1,height=0;int ahead=0;bool aimed=false,perched=false;
        foreach(var e in enemies)
        {
            float d=Pos(e).x-p.position.x;if(Mathf.Abs(d)<Mathf.Abs(nearest)){nearest=d;dir=Mathf.Sign(d);height=Pos(e).y-p.floor;perched=(bool)Field(e,"perched");}
            if(d*p.facing>1.5f&&d*p.facing<9)ahead++;
            if(Mathf.Abs(d)>1.6f&&Mathf.Abs(d)<13&&((float)Field(e,"windup")>0||(int)Field(e,"burst")>0)&&Pos(e).y-p.floor<.3f)aimed=true;
        }
        bool driving=p.vehicle!=null;
        float lift=p.position.y-p.floor;bool ground=lift<.01f;
        float move=enemies.Count==0?1:Mathf.Abs(nearest)>5?dir:Mathf.Abs(nearest)<1?dir*.15f:dir*.6f;
        bool up=false,down=aimed&&ground&&!driving,jump=t%71==i*9&&!aimed,grenade=ahead>=2&&t%50==i*5;
        if(lift>.4f&&Mathf.Abs(nearest)<.8f)down=true;
        if(ground&&Mathf.Abs(nearest)>2&&t%240<30&&!driving)down=true;
        if((perched&&height>.5f||height>1.2f)&&Mathf.Abs(nearest)<8){move=Mathf.Abs(nearest)>.4f?dir:0;up=true;down=false;if(ground&&height<2.2f&&Mathf.Abs(nearest)<2.5f&&t%25==i*3)jump=true;}
        if(!driving&&height<-.8f&&Mathf.Abs(nearest)<4&&ground&&(bool)Call(g,"OnLedge",p.position.x,p.position.y,1)&&t%20==i*2){down=true;jump=true;}
        if(!g.BossActive&&ground&&Mathf.Abs(move)>.1f)
        {
            float way=Mathf.Sign(move),front=p.position.x+way*(driving?p.vehicle.type.Half+.3f:.8f);
            if(SolidAt(front)>p.position.y+.36f&&(SolidAt(front)<p.position.y+1.1f||t%40==i*4))jump=true;
            if(t%90==i*7&&(float)Call(g,"FloorAt",front,p.position.y+2.1f,1,float.MaxValue)>p.position.y+.6f){jump=true;down=false;}
        }
        if(!driving&&!g.BossActive)foreach(var v in vehicles)if(v.rider==null&&!v.Wrecked&&v.position.x>p.position.x&&v.position.x-p.position.x<8){move=1;down=false;}
        if(p.position.y<-.3f&&ground){jump=true;down=false;}
        if(g.BossActive&&enemies.Count==0){float d=((Vector2)Get(g,"bossPosition")).x-p.position.x;move=Mathf.Abs(d)>(driving?3:.8f)?Mathf.Sign(d):0;up=move==0&&t%90<60;grenade=t%40==i*7&&Mathf.Abs(d)<8;down=false;}
        if(driving&&down&&jump)down=false;
        if(g.ContinueOffered)jump=t%71==i*9;
        return new PlayerCommand{move=move,fire=true,aimUp=up,down=down,jump=jump,grenade=grenade&&!g.ContinueOffered,special=t%97==i*11};
    }
    static string Run(string id,bool parked,out float fight,out bool drove)
    {
        var mission=original[M(id)];var kept=mission.map.vehicles;if(!parked)mission.map.vehicles=new MapVehicle[0];
        fight=0;drove=false;int t=0;
        try
        {
            Start(M(id),1,1);bool was=false;
            for(;t<60*60*30&&g.phase==PanameGame.Phase.Playing;t++)
            {
                commands[0]=Bot(players[0],0,t);Tick(1);
                bool mid=g.MidBossActive&&g.BossActive;if(mid&&!was)drove=players[0].vehicle!=null;if(mid)fight+=1f/60;was=mid;
            }
        }
        finally{mission.map.vehicles=kept;}
        if(g.phase!=PanameGame.Phase.Won)throw new Exception("ÉCHEC : bot "+id+(parked?" avec":" sans")+" véhicule");
        return id.Split('-')[0]+(parked?" avec véhicule":" sans véhicule")+" "+t/60+" s, mini-boss "+fight.ToString("0.0")+" s"+(parked?(drove?" au volant":" à pied"):"");
    }
    static void Bots(List<string> checks)
    {
        var lines=new List<string>();bool crushed=false;
        foreach(var id in Four)
        {
            lines.Add(Run(id,true,out float car,out bool drove));lines.Add(Run(id,false,out float foot,out _));
            if(drove&&car<foot*.6f)crushed=true;
        }
        if(crushed)throw new Exception("ÉCHEC : un mini-boss est expédié au volant : "+string.Join(" ; ",lines));
        Check(true,"Bot de VerifyLot3b (ARCADE, invulnérable à pied, garde son véhicule tant qu'il roule) : "+string.Join(" ; ",lines),checks);
    }

    // ---------- Captures ----------
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
