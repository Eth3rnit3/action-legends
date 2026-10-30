using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using Paname;
// Lot 3b, drivable vehicles. In a mission written only as JSON, with a vehicle of its own: a hero gets in by walking into it,
// drives it at its speed, jumps, fires its gun ahead, up, diagonally and down (on the ground and in the air), fires the cannon
// until its shells run out, takes the hits in its place (the pilot's health does not move), gets out with down + jump and does
// not get straight back in, is thrown out when it breaks, which blinks then blows up the enemies around; an empty vehicle is
// taken by another player. It climbs slopes and blocks, is walled by a high one, lands on a platform, crushes a crate, rams a
// car and an objective barricade down, stays in a locked wave's screen, and fights the boss (its cannon hurts the boss, the
// boss's moves hurt the vehicle). Replays and online frames are exact; bots win the four missions in every difficulty, alone
// and at four, driving each mission's vehicle. Captures of each vehicle with several heroes in the seat.
public static class VerifyLot3b
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static void Set(object o,string name,object v)=>o.GetType().GetField(name,Flags).SetValue(o,v);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags|BindingFlags.Static).Invoke(o,args);
    static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
    static void SetField(object o,string name,object v)=>o.GetType().GetField(name).SetValue(o,v);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    static Vector2 Pos(object o)=>(Vector2)Field(o,"position");
    const int Slug=4;

    static PanameGame g;static PanameGame.Player[] players;static IList enemies,shots,thrown,hostages,pickups,arrivals,carriers,breakables;static List<PanameGame.Vehicle> vehicles;
    static readonly PlayerCommand[] commands=new PlayerCommand[4];
    static MissionDefinition[] original;static FieldInfo missionsField;
    static int M(string id)=>Array.FindIndex((MissionDefinition[])missionsField.GetValue(g),x=>x.id==id);
    static readonly string[] Four={"panique-a-paname","mistral-gagnant","prophetie-en-provence","tokyo-neon"};
    static void Start(int mission,int difficulty,int heroes,int hero=0)
    {
        g.ReturnToLobby();Call(g,"LoadMission",mission);g.SetDifficulty(difficulty);
        for(int i=0;i<4;i++){g.slots[i].joined=i<heroes;g.slots[i].hero=(hero+i)%PanameGame.HeroCount;}
        g.BeginRound();players=(PanameGame.Player[])Get(g,"players");enemies=(IList)Get(g,"enemies");shots=(IList)Get(g,"shots");thrown=(IList)Get(g,"thrown");
        hostages=(IList)Get(g,"hostages");pickups=(IList)Get(g,"pickups");arrivals=(IList)Get(g,"arrivals");carriers=(IList)Get(g,"carriers");breakables=(IList)Get(g,"breakables");
        vehicles=(List<PanameGame.Vehicle>)Get(g,"vehicles");
        for(int i=0;i<4;i++)commands[i]=default;
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
    static float FloorAt(float x,float y)=>(float)Call(g,"FloorAt",x,y,1,float.MaxValue);
    static void Command(int i,float move=0,bool jump=false,bool down=false,bool fire=false,bool up=false,bool grenade=false)=>commands[i]=new PlayerCommand{move=move,jump=jump,down=down,fire=fire,aimUp=up,grenade=grenade};
    static void Hold(int ticks,float move=0,bool jump=false,bool down=false,bool fire=false,bool up=false,bool grenade=false,int who=0,bool invulnerable=true)
    {
        for(int t=0;t<ticks;t++){Command(who,move,jump&&t==0,down,fire,up,grenade&&t==0);Tick(1,invulnerable);}
        commands[who]=default;
    }
    static PanameGame.Vehicle At(float x){foreach(var v in vehicles)if(Mathf.Abs(v.place.x-x)<.01f)return v;throw new Exception("pas de véhicule à "+x);}
    static object Prop(float x){foreach(var b in breakables)if(Mathf.Abs(Pos(b).x-x)<.01f)return b;throw new Exception("pas de décor à "+x);}
    static int Hp(object b)=>(int)Field(b,"hp");
    // A hero next to a vehicle (on foot), the camera on him.
    static void Stand(PanameGame.Player p,float x){if(p.vehicle!=null)Call(g,"LeaveVehicle",p,false);p.position=new Vector2(x,SolidAt(x));p.vertical=0;p.dropFrom=float.MaxValue;Set(g,"cameraX",x+2.5f);Tick(2);}
    // The vehicle and its pilot moved to x.
    static void Drive(PanameGame.Vehicle v,float x){v.position=new Vector2(x,SolidAt(x));v.vertical=0;v.rider.position=v.position;Set(g,"cameraX",x+2.5f);Tick(2);}
    static object Place(string enemy,float x,float y=0)
    {
        var s=new EncounterSpawn{enemy=enemy,entry="none",x=x,y=y,definition=ContentLibrary.Enemy(enemy),kind=-1};return Call(g,"Arrive",s,0,null);
    }
    static Vector2 LastShot(out WeaponDefinition weapon){var s=shots[shots.Count-1];weapon=(WeaponDefinition)Field(s,"weapon");return ((Vector2)Field(s,"velocity")).normalized;}
    static float Angle(Vector2 d)=>Mathf.Atan2(d.y,Mathf.Abs(d.x))*Mathf.Rad2Deg;

    // ---------- A mission written only as JSON, with a vehicle of its own ----------
    const string TestVehicle=@"{""id"": ""test-char"", ""name"": ""CHAR D'ESSAI"", ""health"": [20, 20, 20, 20], ""speed"": 7, ""jump"": 12, ""gravity"": 30,
      ""box"": {""width"": 3, ""height"": 1.8}, ""gun"": ""mitrailleuse-de-bord"", ""cannon"": {""weapon"": ""obus-de-char"", ""shells"": [3, 3, 3, 3], ""pose"": 0.25},
      ""turret"": {""sprite"": {""texture"": ""Art/vehicles/taureau-mecanique"", ""columns"": 4, ""rows"": 3, ""cell"": 10}, ""size"": 2, ""pivot"": 0.2979, ""length"": 0.79, ""turnRate"": 600},
      ""pilot"": {""state"": ""idle"", ""scale"": 0.8}, ""ram"": {""damage"": 15, ""propDamage"": 12, ""stomp"": 50, ""interval"": 0.3},
      ""wreck"": {""time"": 1, ""radius"": 3.5, ""damage"": 200, ""bossDamage"": 30}, ""breakSound"": ""Audio/Sfx/metal-casse"",
      ""animations"": {""sheet"": {""texture"": ""Art/vehicles/taureau-mecanique"", ""columns"": 4, ""rows"": 3, ""cellHeight"": 3.265, ""pivot"": {""x"": 0.5, ""y"": 0.0365}}, ""layer"": 32,
        ""states"": [{""name"": ""idle"", ""cells"": [0]}, {""name"": ""run"", ""cells"": [1, 2, 3], ""perStride"": 1.5}, {""name"": ""jump"", ""cells"": [4]}],
        ""anchors"": [{""name"": ""seat"", ""x"": -0.05, ""y"": 0.78}, {""name"": ""turret"", ""x"": -0.39, ""y"": 1.79}, {""name"": ""cannon"", ""x"": 1.82, ""y"": 1.06}]}}";
    const string TestCrate=@"{""id"": ""test-caisse-3b"", ""extends"": ""caisses-paris"", ""crush"": true}";
    static string Cell(int i)=>@"{""texture"": ""Art/props/paris-terrain"", ""columns"": 3, ""rows"": 1, ""cell"": "+i+"}";
    static string Mission=>@"{""schemaVersion"": 2, ""id"": ""test-vehicule"", ""order"": 900, ""title"": ""Véhicule de test"", ""location"": ""Banc d'essai"", ""intro"": ""ESSAI !"",
      ""exitLabel"": ""FIN"", ""winTitle"": ""ESSAI RÉUSSI"", ""music"": ""Audio/Music/marseillaise-arcade"", ""bossMusic"": ""Audio/Music/marseillaise-boss"",
      ""map"": {""backgrounds"": [""Art/route-01"", ""Art/route-02""], ""ground"": ""Art/bastille-panorama"", ""horizon"": 0.1, ""groundBand"": 0.12, ""groundTop"": -0.9, ""groundFade"": 0.25,
        ""terrain"": [{""type"": ""slope"", ""x"": 40, ""width"": 3, ""height"": 0, ""toHeight"": 1.2, ""sprite"": "+Cell(1)+@"},
          {""type"": ""block"", ""x"": 43, ""width"": 5, ""height"": 1.2, ""sprite"": "+Cell(2)+@"},
          {""type"": ""slope"", ""x"": 48, ""width"": 3, ""height"": 1.2, ""toHeight"": 0, ""sprite"": "+Cell(1)+@", ""flip"": true},
          {""type"": ""block"", ""x"": 60, ""width"": 2, ""height"": 3, ""sprite"": "+Cell(2)+@"},
          {""type"": ""platform"", ""x"": 70, ""width"": 6, ""height"": 1.8, ""sprite"": "+Cell(0)+@"}],
        ""props"": [{""id"": ""test-caisse-3b"", ""x"": 85}, {""id"": ""voiture-parisienne"", ""x"": 95}, {""id"": ""barricade-vauban"", ""x"": 120, ""wave"": 0}],
        ""vehicles"": [{""id"": ""test-char"", ""x"": 20}, {""id"": ""deuche-blindee"", ""x"": 150}]},
      ""enemies"": [""looter-rusher""], ""length"": 200, ""exitX"": 197, ""cameraMin"": 7, ""cameraMax"": 194, ""rules"": ""pow"",
      ""waves"": [{""atX"": 122, ""baseEnemies"": 2, ""enemiesPerPlayer"": 0, ""maxAlive"": 4, ""spawnInterval"": 0.5, ""enemyPattern"": [0]}],
      ""boss"": {""enabled"": true, ""id"": ""armored-van"", ""health"": 1}}";
    static int TestMission()
    {
        ContentLibrary.AddVehicle(TestVehicle);ContentLibrary.AddProp(TestCrate);
        var mission=MissionDefinition.Parse(Mission);var extended=new List<MissionDefinition>(original){mission};missionsField.SetValue(g,extended.ToArray());
        PlayerPrefs.SetInt("action-legends.unlocked.test-vehicule",1);return original.Length;
    }

    public static object Main(string captures)
    {
        var checks=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        float scale=Time.timeScale;Time.timeScale=0;
        missionsField=typeof(PanameGame).GetField("missions",Flags);original=(MissionDefinition[])missionsField.GetValue(g);
        var only=Environment.GetEnvironmentVariable("LOT3B_ONLY");bool Part(string name)=>string.IsNullOrEmpty(only)||Array.IndexOf(only.Split(','),name)>=0;
        try
        {
            if(Part("Library"))Library(checks);
            int test=TestMission();
            if(Part("Drive"))DriveAndShoot(checks,test);
            if(Part("Hits"))Hits(checks,test);
            if(Part("Relief"))Relief(checks,test);
            if(Part("Props"))Props(checks,test);
            if(Part("Boss"))BossFight(checks,test);
            missionsField.SetValue(g,original);Set(g,"missionIndex",0);ContentLibrary.Reload();
            if(Part("Replays"))Replays(checks);
            if(Part("Bots"))Bots(checks);
            if(Part("Captures"))Captures(checks,captures);
        }
        finally
        {
            missionsField.SetValue(g,original);Set(g,"missionIndex",0);ContentLibrary.Reload();
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;}
            foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);PlayerPrefs.DeleteKey("action-legends.unlocked.test-vehicule");
            g.ReturnToLobby();Call(g,"LoadMission",0);g.SetDifficulty(1);g.ReturnToLobby();Time.timeScale=scale;
        }
        return string.Join("\n",checks);
    }

    static void Library(List<string> checks)
    {
        var lines=new List<string>();
        foreach(var id in Four)
        {
            Start(M(id),1,1);
            if(vehicles.Count<1)throw new Exception("ÉCHEC : pas de véhicule dans "+id);
            foreach(var v in vehicles)lines.Add(id.Split('-')[0]+" : "+v.type.name+" à x "+v.place.x+" ("+v.hp+" PV, "+v.shells+" obus)");
        }
        Check(true,"Chaque mission gare au moins un véhicule de la bibliothèque : "+string.Join(", ",lines),checks);
        bool Refused(string json){try{ContentLibrary.AddVehicle(json);return false;}catch(ArgumentException){return true;}}
        Check(Refused(TestVehicle.Replace(@"{""name"": ""cannon"", ""x"": 1.82, ""y"": 1.06}","{\"name\": \"other\", \"x\": 0, \"y\": 0}"))&&Refused(TestVehicle.Replace("mitrailleuse-de-bord","rocket-launcher"))
            &&Refused(TestVehicle.Replace("mitrailleuse-de-bord","laser"))&&Refused(TestVehicle.Replace(@"""shells"": [3, 3, 3, 3]",@"""shells"": [3, 3]")),
            "Un véhicule sans ancrage du canon, à mitrailleuse qui s'épuise ou à rayon, ou sans obus par difficulté, est refusé",checks);
        ContentLibrary.AddVehicle(TestVehicle);
        bool Rejected(string mission){try{MissionDefinition.Parse(mission);return false;}catch(ArgumentException){return true;}}
        Check(Rejected(Mission.Replace(@"{""id"": ""deuche-blindee"", ""x"": 150}",@"{""id"": ""deuche-blindee"", ""x"": 180}"))&&Rejected(Mission.Replace(@"""x"": 150}",@"""x"": 150}, {""id"": ""nothing"", ""x"": 30}")),
            "Une mission refuse un véhicule garé dans l'arène du boss ou inconnu",checks);
    }

    static void DriveAndShoot(List<string> checks,int test)
    {
        Start(test,1,1);Quiet();var p=players[0];var v=At(20);Stand(p,16.5f);
        Hold(30,1);
        Check(p.vehicle==v&&v.rider==p&&!p.root.gameObject.activeSelf&&p.position==v.position,"Un héros qui touche un véhicule vide y monte : sa position est celle du véhicule, son image est dans le siège",checks);
        float x0=v.position.x;Hold(60,1);float speed=v.position.x-x0;
        Check(Mathf.Abs(speed-7)<.2f&&v.facing==1,"Le véhicule roule à sa vitesse ("+speed.ToString("0.00")+" u/s, 7 en JSON)",checks);
        Hold(1,0,jump:true);float top=0;for(int t=0;t<90;t++){Tick();top=Mathf.Max(top,v.position.y);}
        Check(top>2&&v.position.y<.01f&&p.position==v.position,"Il saute (sommet à "+top.ToString("0.0")+" u) et retombe, le pilote avec lui",checks);
        // The gun turns with the aim: ahead, up, diagonal while moving, down on the ground, straight down in the air.
        var results=new List<string>();
        foreach(var (label,move,up,down,air,want) in new[]{("devant",0f,false,false,false,0f),("en haut",0f,true,false,false,90f),("en diagonale",.5f,true,false,false,45f),("en bas",0f,false,true,false,-25f),("en bas en l'air",0f,false,true,true,-90f)})
        {
            if(air){Hold(1,0,jump:true);Hold(4);}
            Hold(20,move,false,down,false,up);ClearShots();Command(0,move,false,down,true,up);Tick();commands[0]=default;
            var d=LastShot(out var w);float a=Angle(d);results.Add(label+" "+a.ToString("0"));
            if(w==null||w.id!="mitrailleuse-de-bord"||Mathf.Abs(a-want)>4)throw new Exception("ÉCHEC : tir "+label+" à "+a+"° ("+w?.id+")");
            Hold(90);
        }
        Check(true,"La mitrailleuse de bord suit la visée (degrés : "+string.Join(", ",results)+")",checks);
        Quiet();ClearShots();int shells=v.shells,fired=0;
        for(int i=0;i<6;i++){Hold(1,grenade:true);if(shots.Count>0&&((WeaponDefinition)Field(shots[shots.Count-1],"weapon")).id=="obus-de-char")fired++;ClearShots();Hold(40);}
        Check(shells==3&&fired==3&&v.shells==0,"Le canon tire ses obus (grenade) jusqu'au dernier : 3 tirés sur 6 appuis",checks);
        // The cannon's shell blows an enemy up.
        v.shells=1;Place("looter-rusher",v.position.x+6);var e=enemies[0];int hp=(int)Field(e,"hp");Hold(1,grenade:true);Hold(40);
        Check(enemies.Count==0||(int)Field(e,"hp")<hp,"Un obus touche un ennemi devant le véhicule et explose",checks);
        // Out with down + jump: up out of the cockpit, the vehicle stays; not back in before stepping off it.
        Quiet();float parked=v.position.x;Hold(1,0,jump:true,down:true);
        Check(p.vehicle==null&&v.rider==null&&p.root.gameObject.activeSelf&&p.vertical>0,"Bas + saut fait sortir le héros, qui saute hors du véhicule",checks);
        Hold(80);bool stayedOut=p.vehicle==null;Hold(50,-1);bool off=p.vehicle==null&&p.position.x<v.Left;Hold(60,1);
        Check(stayedOut&&off&&p.vehicle==v&&Mathf.Abs(v.position.x-parked)>.5f,"Il ne remonte pas tant qu'il ne l'a pas quitté, puis remonte en le touchant",checks);
    }

    static void Hits(List<string> checks,int test)
    {
        Start(test,1,2);Quiet();var p=players[0];var q=players[1];var v=At(20);Stand(q,13);Stand(p,16.5f);Hold(30,1);
        if(p.vehicle!=v)throw new Exception("ÉCHEC : J1 n'est pas monté");
        int hero=p.hp,before=v.hp;Call(g,"Damage",p,2);Tick(20);
        Check(p.hp==hero&&v.hp==before-2,"Un coup porté au pilote use la jauge du véhicule ("+before+" → "+v.hp+"), pas les PV du héros",checks);
        var look=Enum.ToObject(typeof(PanameGame).GetNestedType("ShotLook",BindingFlags.NonPublic),2);
        var mid=g.GetType().GetMethod("Shoot",Flags);
        before=v.hp;mid.Invoke(g,new object[]{look,v.position+new Vector2(5,1),new Vector2(-30,0),1,false,false});Tick(30);int low=v.hp;
        mid.Invoke(g,new object[]{look,v.position+new Vector2(5,2.6f),new Vector2(-30,0),1,false,false});Tick(30);
        Check(low==before-1&&v.hp==low&&p.hp==hero,"Une balle ennemie touche la boîte du véhicule ; une balle qui passe au-dessus ne touche personne",checks);
        // At zero health: the pilot is thrown out, the vehicle blinks, then blows up the enemies around.
        v.hp=1;v.invulnerable=0;Place("looter-rusher",v.position.x+2.5f);var e=enemies[0];Call(g,"Damage",p,1);
        bool thrown=p.vehicle==null&&p.vertical>10&&p.invulnerable>1&&v.Wrecked&&!v.exploded;
        Tick(30);bool waiting=!v.exploded&&enemies.Contains(e);Tick(40);
        Check(thrown&&waiting&&v.exploded&&!enemies.Contains(e)&&p.hp==hero,"À vie nulle le pilote est éjecté, le véhicule clignote puis explose et abat l'ennemi voisin ; le héros n'a rien perdu",checks);
        Stand(p,v.position.x-.5f);Hold(30,.2f);
        Check(p.vehicle==null,"Une épave ne se reprend pas",checks);
        // An empty vehicle is anyone's: J1 gets out of the second one and J2 takes it.
        var d=At(150);Stand(p,146.5f);Hold(40,1);bool j1=p.vehicle==d;Hold(1,0,jump:true,down:true,who:0);Hold(30);Hold(60,-1);
        q.position=new Vector2(156,0);q.facing=-1;Set(g,"cameraX",152f);Tick(2);Hold(40,-1,who:1);
        Check(j1&&p.vehicle==null&&q.vehicle==d&&d.rider==q,"Un véhicule laissé vide est repris par un autre joueur (J1 descend, J2 monte)",checks);
    }

    static void Relief(List<string> checks,int test)
    {
        Start(test,1,1);Quiet();var p=players[0];var v=At(20);Stand(p,16.5f);Hold(30,1);Drive(v,34);
        float midSlope=0,onBlock=0;for(int t=0;t<190;t++){Hold(1,1);if(Mathf.Abs(v.position.x-41.5f)<.1f)midSlope=v.position.y;if(Mathf.Abs(v.position.x-45.5f)<.1f)onBlock=v.position.y;}
        Check(Mathf.Abs(midSlope-.6f)<.15f&&Mathf.Abs(onBlock-1.2f)<.02f&&v.position.y<.01f&&v.position.x>52,"Il monte la pente (y "+midSlope.ToString("0.00")+" à mi-pente), roule sur le bloc (y "+onBlock.ToString("0.00")+") et redescend",checks);
        Drive(v,54);Hold(120,1);float wall=v.position.x;Hold(1,1,jump:true);Hold(80,1);
        Check(Mathf.Abs(wall+v.type.Half-60)<.05f&&v.position.x<=wall+.01f,"Un mur trop haut arrête l'avant du véhicule (x "+wall.ToString("0.00")+", mur à 60), même en sautant",checks);
        Drive(v,67);Hold(1,1,jump:true);Hold(40,1);Hold(40);
        Check(Mathf.Abs(v.position.y-1.8f)<.02f&&p.floor>1.7f,"Il saute sur une plateforme et s'y pose (y "+v.position.y.ToString("0.00")+")",checks);
    }

    static void Props(List<string> checks,int test)
    {
        Start(test,1,1);Quiet();var p=players[0];var v=At(20);Stand(p,16.5f);Hold(30,1);v.hp=v.maxHp=999;
        var crate=Prop(85);Drive(v,80);Hold(60,1);
        Check(Hp(crate)<=0&&v.position.x>87,"Il écrase la caisse sur sa route (décor « crush ») et passe",checks);
        var car=Prop(95);Drive(v,89);int carHp=Hp(car);Hold(30,1);float stop=v.position.x;Hold(30,1);int rammed=Hp(car);
        for(int t=0;t<600&&Hp(car)>0;t++)Hold(1,1);Hold(60,1);
        Check(stop<95-1.39f-v.type.Half+.05f&&rammed<carHp&&Hp(car)<=0&&v.position.x>97,"La voiture l'arrête ; il l'enfonce à force de la percuter ("+carHp+" PV), puis passe",checks);
        // The objective barricade of the locked wave: walled until it falls, the screen held.
        typeof(PanameGame).GetProperty("Wave").SetValue(g,0);Set(g,"waveLive",false);var bar=Prop(120);Drive(v,108);
        float most=0;bool held=false;
        for(int t=0;t<60*40&&Hp(bar)>0;t++){Hold(1,1,fire:t%3==0);most=Mathf.Max(most,v.position.x);if((bool)Get(g,"screenLocked"))held=true;}
        float lockX=(float)Get(g,"lockX");
        Check(held&&most<120-2.225f-v.type.Half+.05f&&Hp(bar)<=0,"La barricade-objectif bloque le véhicule (x max "+most.ToString("0.0")+") jusqu'à ce qu'il la détruise",checks);
        Set(g,"screenLocked",true);Set(g,"lockX",lockX);Hold(120,1);
        Check(v.position.x<=lockX+10.31f-v.type.Half+.01f,"Une vague bloquée garde le véhicule dans l'écran (x "+v.position.x.ToString("0.0")+", bord à "+(lockX+10.31f).ToString("0.0")+")",checks);
    }

    static void BossFight(List<string> checks,int test)
    {
        Start(test,1,1);Quiet();var p=players[0];var v=At(150);Stand(p,146.5f);Hold(40,1);v.hp=v.maxHp=999;v.shells=99;
        Drive(v,176);Hold(30,1);
        if(!g.BossActive)throw new Exception("ÉCHEC : le boss n'est pas venu");
        int boss=g.BossHealth,hero=p.hp,before=v.hp;
        for(int t=0;t<60*40&&g.BossActive&&v.hp==before;t++){Command(0);Tick();}
        int hit=v.hp;for(int t=0;t<60*3;t++){Command(0,0,false,false,true,false,t%30==0);Tick();}
        int hurt=g.BossHealth;
        Check(hurt<boss&&hit<before&&p.hp==hero,"Contre le boss : ses obus et sa mitrailleuse blessent le boss ("+boss+" → "+hurt+"), les coups du boss usent le véhicule ("+before+" → "+hit+"), pas le pilote",checks);
        for(int t=0;t<60*60&&g.phase==PanameGame.Phase.Playing;t++){Command(0,0,false,false,true,false,t%30==0);Tick();}
        Check(g.phase==PanameGame.Phase.Won,"Le véhicule vient à bout du boss : victoire",checks);
    }

    // ---------- Replays and online ----------
    static ReplayRecord Record(int mission,int difficulty,int heroes,int hero,int limit,int giveUp,out List<string> frames,out float ridden)
    {
        Start(mission,difficulty,heroes,hero);var moves=new int[4];var flags=new int[4];var text=new StringBuilder();frames=new List<string>();ridden=0;
        for(int t=0;t<limit&&g.phase==PanameGame.Phase.Playing;t++)
        {
            for(int i=0;i<4;i++){moves[i]=0;flags[i]=0;var q=players[i];if(q==null)continue;var c=Bot(q,i,t);if(g.ContinueOffered&&g.Continues>=giveUp)c=new PlayerCommand{grenade=t%30==0};moves[i]=Mathf.RoundToInt(c.move*10)*10;flags[i]=(c.fire?1:0)|(c.aimUp?2:0)|(c.jump?4:0)|(c.special?8:0)|(c.down?16:0)|(c.grenade?32:0);commands[i]=(PlayerCommand)Call(g,"Command",moves[i],flags[i]);}
            text.Clear().Append(0);for(int i=0;i<4;i++)text.Append(' ').Append(moves[i]).Append(' ').Append(flags[i]);frames.Add(text.ToString());
            Call(g,"RecordTick",0,moves,flags);g.TickSimulation(1f/60,commands);Call(g,"EndReplayTick");
            foreach(var p in players)if(p!=null&&p.vehicle!=null)ridden+=1f/60;
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
        int fernandel=ContentLibrary.HeroIndex("fernandel"),jeanne=ContentLibrary.HeroIndex("jeanne-d-arc");
        foreach(var (id,d,h,hero,giveUp) in new[]{("panique-a-paname",1,1,0,0),("mistral-gagnant",2,2,fernandel,0),("prophetie-en-provence",0,1,jeanne,0),("tokyo-neon",3,4,1,0),("panique-a-paname",Slug,1,2,2)})
        {
            var run=Record(M(id),d,h,hero,60*60*15,giveUp,out var frames,out float ridden);int wrecked=0;foreach(var v in vehicles)if(v.Wrecked)wrecked++;
            foreach(var mm in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+mm.id);
            if(ridden<=0)throw new Exception("ÉCHEC : personne n'a conduit dans "+id);
            Check(Verify(run)&&Online(run,frames),"Une partie sur "+id+" ("+PanameGame.DifficultyNames[d]+", "+h+" joueur(s), "+PanameGame.Heroes[hero].shortName+", "+ridden.ToString("0")+" s au volant"+(wrecked>0?", véhicule détruit":"")+") se rejoue à l'identique en replay et en trames en ligne : "+(run.won?"victoire":"défaite")+", "+run.ticks+" pas"+(run.continues>0?", "+run.continues+" continues":""),checks);
        }
    }

    // ---------- The bot ----------
    // VerifyLot3a's bot (which knows the relief), told about vehicles: on foot it walks into the one ahead; driving, it keeps
    // the trigger down, fires the cannon at groups ahead, never presses down + jump (that would get it out) and aims up at
    // enemies above.
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
            var pit=Call(g,"PitAt",p.position.x+way*1.2f) as MapTerrain;if(pit!=null&&pit.lethal&&Call(g,"PitAt",p.position.x)==null){jump=true;down=false;}
            if(t%90==i*7&&FloorAt(front,p.position.y+2.1f)>p.position.y+.6f){jump=true;down=false;}
        }
        // On foot, a parked vehicle within reach ahead is worth getting into.
        if(!driving&&!g.BossActive)foreach(var v in vehicles)if(v.rider==null&&!v.Wrecked&&v.position.x>p.position.x&&v.position.x-p.position.x<8){move=1;down=false;}
        if(p.position.y<-.3f&&ground){jump=true;down=false;}
        if(g.BossActive&&enemies.Count==0){float d=((Vector2)Get(g,"bossPosition")).x-p.position.x;move=Mathf.Abs(d)>(driving?3:.8f)?Mathf.Sign(d):0;up=move==0&&t%90<60;grenade=t%40==i*7&&Mathf.Abs(d)<8;down=false;}
        if(driving&&down&&jump)down=false;
        if(g.ContinueOffered)jump=t%71==i*9;
        return new PlayerCommand{move=move,fire=true,aimUp=up,down=down,jump=jump,grenade=grenade&&!g.ContinueOffered,special=t%97==i*11};
    }
    static string Run(int mission,int difficulty,int heroes,int hero,out bool won,out float ridden)
    {
        bool slug=difficulty==Slug;Start(mission,difficulty,heroes,hero);int t=0;ridden=0;
        for(;t<60*60*30&&g.phase==PanameGame.Phase.Playing;t++)
        {
            for(int i=0;i<4;i++)commands[i]=players[i]!=null?Bot(players[i],i,t):default;Tick(1,!slug);
            foreach(var p in players)if(p!=null&&p.vehicle!=null)ridden+=1f/60;
        }
        won=g.phase==PanameGame.Phase.Won;int wrecked=0;foreach(var v in vehicles)if(v.Wrecked)wrecked++;
        return PanameGame.DifficultyNames[difficulty]+" "+g.CurrentMission.id.Split('-')[0]+(heroes>1?" ×"+heroes:"")+" "+t/60+" s ("+ridden.ToString("0")+" s au volant"+(wrecked>0?", détruit":"")+(slug?", "+g.Continues+" continues":"")+")";
    }
    static void Bots(List<string> checks)
    {
        var lines=new List<string>();
        foreach(var id in Four)for(int d=0;d<PanameGame.DifficultyCount;d++)
        {
            var line=Run(M(id),d,1,0,out bool won,out float ridden);if(!won||ridden<=0)throw new Exception("ÉCHEC : bot "+line);lines.Add(line);
        }
        Check(true,"Un bot gagne les quatre missions dans les cinq difficultés en conduisant le véhicule de la mission (invulnérable à pied, vulnérable en SLUG avec continues) : "+string.Join(", ",lines),checks);
        lines.Clear();
        foreach(var id in Four){var line=Run(M(id),1,4,0,out bool won,out float ridden);if(!won||ridden<=0)throw new Exception("ÉCHEC : bot "+line);lines.Add(line);}
        Check(true,"Quatre bots gagnent les quatre missions ensemble (ARCADE), l'un d'eux au volant : "+string.Join(", ",lines),checks);
    }

    // ---------- Captures ----------
    // Each mission's vehicle: parked, then driven by three heroes in turn (ahead, up while rolling, down in a jump), damaged,
    // about to blow up and as a wreck.
    static void Captures(List<string> checks,string captures)
    {
        int taken=0;
        foreach(var id in Four)
        {
            Start(M(id),1,1);Quiet();var v=vehicles[0];var p=players[0];float x=v.place.x;string name=captures+"/lot3b-"+id.Split('-')[0];
            Set(g,"cameraX",x+1);Stand(p,x-6);Set(g,"cameraX",x+1);Tick(2);Capture(name+"-gare.png");taken++;
            for(int k=0;k<3;k++)
            {
                int hero=(k*3+Array.IndexOf(Four,id))%PanameGame.HeroCount;
                if(p.vehicle!=null)Call(g,"LeaveVehicle",p,false);v.left=null;p.hero=hero;p.weapon=p.Definition.Weapon;
                v.position=new Vector2(x,SolidAt(x));v.vertical=0;v.aim=0;p.position=new Vector2(x,v.position.y);Tick(1);
                if(p.vehicle!=v)throw new Exception("ÉCHEC : capture sans pilote");
                if(k==0){Hold(20,0,fire:true);}
                if(k==1){Hold(20,.6f,false,false,true,true);}
                if(k==2){Hold(1,0,jump:true);Hold(14,0,false,true,true);}
                v.position.x=x;p.position.x=x;Set(g,"cameraX",x+1);Call(g,"DrawVehicle",v,0f);Capture(name+"-"+PanameGame.Heroes[hero].id+".png");taken++;
                Quiet();Tick(60);
            }
            v.hp=Mathf.CeilToInt(v.maxHp*v.type.damagedAt*.8f);Hold(10,.5f);v.position.x=x;p.position.x=x;Set(g,"cameraX",x+1);Call(g,"DrawVehicle",v,0f);Capture(name+"-abime.png");taken++;
            v.invulnerable=0;v.hp=1;Call(g,"Damage",p,1);Tick(10);Set(g,"cameraX",x+1);Capture(name+"-epave.png");taken++;
            Tick(90);Quiet();Set(g,"cameraX",x+1);Tick(30);Capture(name+"-detruit.png");taken++;
        }
        Check(taken==28,"Captures de chaque véhicule garé, piloté par trois héros, abîmé, en flammes et détruit (lot3b-<mission>-*.png)",checks);
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
