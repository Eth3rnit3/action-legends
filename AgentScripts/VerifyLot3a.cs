using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using Paname;
// Lot 3a, relief and destructible props. In a mission written only as JSON (with a prop of its own): heroes land and stay on a
// platform, drop through it with down + jump, are walled by a block and climb onto it, follow a slope up and down, lose health
// and come back on the edge of a lethal pit, wade in a shallow one until they jump out; enemies leap onto low relief, fall onto
// it, follow a hero up and down a ledge, and a gunman placed on a car falls when it breaks; bonuses and hostages rest on the
// relief; grenades and shots stop at walls, enemy fire at cover. Props wear down through their images, give points and a
// reward, wall the heroes until they break, explode and chain, and a barricade is the objective that ends its wave. Replays
// and online frames are exact; a bot that jumps onto platforms and breaks obstacles wins the four missions in every difficulty,
// alone, at four and with a new hero. Captures of each mission's relief and props, standing and broken.
public static class VerifyLot3a
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

    static PanameGame g;static PanameGame.Player[] players;static IList enemies,shots,thrown,hostages,pickups,arrivals,carriers,breakables;
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
        Set(g,"screenLocked",false);Set(g,"encounterLock",false);Set(g,"waveLive",false);
    }
    static float SolidAt(float x)=>(float)Call(g,"SolidAt",x,1);
    static float FloorAt(float x,float y)=>(float)Call(g,"FloorAt",x,y,1,float.MaxValue);
    static MapTerrain PitAt(float x)=>Call(g,"PitAt",x) as MapTerrain;
    // The hero stands at x on whatever is there, the camera on him.
    static void Stand(PanameGame.Player p,float x){p.position=new Vector2(x,SolidAt(x));p.vertical=0;p.dropFrom=float.MaxValue;Set(g,"cameraX",x+2.5f);Tick(2);}
    static void Walk(float move,int ticks,bool jump=false,bool down=false,bool fire=false,bool invulnerable=true)
    {
        for(int t=0;t<ticks;t++){commands[0]=new PlayerCommand{move=move,jump=jump&&t==0,down=down,fire=fire};Tick(1,invulnerable);}
        commands[0]=default;
    }
    static object Place(string enemy,float x,float y=0)
    {
        var s=new EncounterSpawn{enemy=enemy,entry="none",x=x,y=y,definition=ContentLibrary.Enemy(enemy),kind=-1};return Call(g,"Arrive",s,0,null);
    }
    static object Prop(float x){foreach(var b in breakables)if(Mathf.Abs(Pos(b).x-x)<.01f)return b;throw new Exception("pas de décor à "+x);}
    static int Hp(object b)=>(int)Field(b,"hp");
    static void Hit(object b,int damage)=>Call(g,"HitProp",b,damage,1,false);

    // ---------- A mission written only as JSON, with two props of its own ----------
    const string TestCrate=@"{""id"": ""test-caisse"", ""name"": ""CAISSE D'ESSAI"", ""blocks"": ""heroes"", ""health"": [12, 12, 12, 12], ""damagedAt"": 0.5, ""score"": 150,
      ""box"": {""width"": 1.2, ""height"": 1.25}, ""rewards"": [{""bonus"": ""grenades""}], ""sound"": ""Audio/Sfx/bois-coup"", ""breakSound"": ""Audio/Sfx/bois-casse"",
      ""debris"": {""texture"": ""Art/props/debris"", ""columns"": 4, ""rows"": 2, ""cells"": [0, 1], ""count"": 5},
      ""animations"": {""sheet"": {""texture"": ""Art/props/paris"", ""columns"": 3, ""rows"": 5, ""cellHeight"": 1.55, ""pivot"": {""x"": 0.5, ""y"": 0.025}}, ""layer"": 24,
        ""states"": [{""name"": ""intact"", ""cells"": [9]}, {""name"": ""damaged"", ""cells"": [10]}, {""name"": ""destroyed"", ""cells"": [11]}]}}";
    const string TestBarrel=@"{""id"": ""test-baril"", ""name"": ""BARIL D'ESSAI"", ""blocks"": ""none"", ""health"": [5, 5, 5, 5], ""score"": 300,
      ""box"": {""width"": 0.7, ""height"": 1.1}, ""explosion"": {""radius"": 2.6, ""damage"": 60, ""heroDamage"": 1}, ""breakSound"": ""Audio/Sfx/metal-casse"",
      ""animations"": {""sheet"": {""texture"": ""Art/props/paris"", ""columns"": 3, ""rows"": 5, ""cellHeight"": 1.46, ""pivot"": {""x"": 0.5, ""y"": 0.025}}, ""layer"": 24,
        ""states"": [{""name"": ""intact"", ""cells"": [12]}, {""name"": ""destroyed"", ""cells"": [14]}]}}";
    const string Follower=@"{""id"": ""test-grimpeur"", ""extends"": ""looter-rusher"", ""terrain"": {""climb"": 2, ""leap"": 11, ""follow"": true}}";
    static string Cell(int i)=>@"{""texture"": ""Art/props/paris-terrain"", ""columns"": 3, ""rows"": 1, ""cell"": "+i+"}";
    static string Mission=>@"{""schemaVersion"": 2, ""id"": ""test-relief"", ""order"": 900, ""title"": ""Relief de test"", ""location"": ""Banc d'essai"", ""intro"": ""ESSAI !"",
      ""exitLabel"": ""FIN"", ""winTitle"": ""ESSAI RÉUSSI"", ""music"": ""Audio/Music/marseillaise-arcade"", ""bossMusic"": ""Audio/Music/marseillaise-boss"",
      ""map"": {""backgrounds"": [""Art/route-01"", ""Art/route-02""], ""ground"": ""Art/paris"", ""horizon"": 0.1, ""groundBand"": 0.12, ""groundTop"": -0.9, ""groundFade"": 0.25,
        ""terrain"": [{""type"": ""platform"", ""x"": 20, ""width"": 5, ""height"": 1.8, ""sprite"": "+Cell(0)+@"},
          {""type"": ""block"", ""x"": 30, ""width"": 3, ""height"": 1.2, ""sprite"": "+Cell(2)+@"},
          {""type"": ""slope"", ""x"": 40, ""width"": 4, ""height"": 0, ""toHeight"": 1.5, ""sprite"": "+Cell(1)+@"},
          {""type"": ""block"", ""x"": 44, ""width"": 4, ""height"": 1.5, ""sprite"": "+Cell(2)+@"},
          {""type"": ""slope"", ""x"": 48, ""width"": 4, ""height"": 1.5, ""toHeight"": 0, ""sprite"": "+Cell(1)+@", ""flip"": true},
          {""type"": ""pit"", ""x"": 60, ""width"": 3, ""height"": 4, ""lethal"": true, ""damage"": 2},
          {""type"": ""pit"", ""x"": 70, ""width"": 3, ""height"": 0.8}],
        ""props"": [{""id"": ""test-caisse"", ""x"": 90}, {""id"": ""test-baril"", ""x"": 100}, {""id"": ""test-baril"", ""x"": 101.6}, {""id"": ""voiture-parisienne"", ""x"": 108},
          {""id"": ""barricade-vauban"", ""x"": 121, ""wave"": 0}],
        ""hostages"": [{""id"": ""civilian"", ""x"": 22, ""y"": 1.8}]},
      ""enemies"": [""looter-rusher""], ""length"": 160, ""exitX"": 157, ""cameraMin"": 7, ""cameraMax"": 154, ""rules"": ""pow"",
      ""waves"": [{""atX"": 120, ""baseEnemies"": 2, ""enemiesPerPlayer"": 0, ""maxAlive"": 4, ""spawnInterval"": 0.5, ""enemyPattern"": [0]}],
      ""boss"": {""enabled"": false}}";
    static int TestMission()
    {
        ContentLibrary.AddProp(TestCrate);ContentLibrary.AddProp(TestBarrel);ContentLibrary.AddEnemy(Follower);
        var mission=MissionDefinition.Parse(Mission);var extended=new List<MissionDefinition>(original){mission};missionsField.SetValue(g,extended.ToArray());
        PlayerPrefs.SetInt("action-legends.unlocked.test-relief",1);return original.Length;
    }

    public static object Main(string captures)
    {
        var checks=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        float scale=Time.timeScale;Time.timeScale=0;
        missionsField=typeof(PanameGame).GetField("missions",Flags);original=(MissionDefinition[])missionsField.GetValue(g);
        var only=Environment.GetEnvironmentVariable("LOT3A_ONLY");bool Part(string name)=>string.IsNullOrEmpty(only)||Array.IndexOf(only.Split(','),name)>=0;
        try
        {
            if(Part("Library"))Library(checks);
            int test=TestMission();
            if(Part("Heroes"))Heroes(checks,test);
            if(Part("Enemies"))Enemies(checks,test);
            if(Part("Things"))Things(checks,test);
            if(Part("Props"))Props(checks,test);
            if(Part("Objective"))Objective(checks,test);
            if(Part("JsonOnly"))JsonOnly(checks,test);
            missionsField.SetValue(g,original);Set(g,"missionIndex",0);ContentLibrary.Reload();
            if(Part("Replays"))Replays(checks);
            if(Part("Bots"))Bots(checks);
            if(Part("Captures"))Captures(checks,captures);
        }
        finally
        {
            missionsField.SetValue(g,original);Set(g,"missionIndex",0);ContentLibrary.Reload();
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;}
            foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);PlayerPrefs.DeleteKey("action-legends.unlocked.test-relief");
            g.ReturnToLobby();Call(g,"LoadMission",0);g.SetDifficulty(1);g.ReturnToLobby();Time.timeScale=scale;
        }
        return string.Join("\n",checks);
    }

    static void Library(List<string> checks)
    {
        var lines=new List<string>();
        foreach(var id in Four)
        {
            var m=original[M(id)];int drawn=0,objectives=0,platforms=0,slopes=0;
            foreach(var t in m.map.terrain){if(t.Drawn)drawn++;if(t.kind==MapTerrain.Platform)platforms++;if(t.kind==MapTerrain.Slope)slopes++;}
            foreach(var p in m.map.props)if(p.wave>=0)objectives++;
            Check(drawn==m.map.terrain.Length&&platforms>=1&&slopes>=2&&objectives>=1&&m.map.props.Length>=8,id+" : relief dessiné et décor destructible",new List<string>());
            lines.Add(id+" "+m.map.terrain.Length+" reliefs ("+platforms+" plateformes, "+slopes+" pentes), "+m.map.props.Length+" décors dont "+objectives+" objectif");
        }
        int props=0;foreach(var id in new[]{"barricade-vauban","voiture-parisienne","etal-fleuriste","caisses-paris","baril-rouge","barricade-du-port","triporteur","etal-poissonnier","caisses-savon","bidon-bleu",
            "barricade-de-foin","charrette","etal-provencal","bottes-de-foin","tonneau-de-poudre","barricade-travaux","fourgonnette-kei","yatai","distributeur","bidon-chimique"}){var d=ContentLibrary.Prop(id);if(d.intact>=0&&d.damaged!=d.intact&&d.destroyed>=0)props++;}
        Check(props==20,"Vingt décors destructibles (cinq par ville, intacts, abîmés et détruits) et chaque mission les pose avec son relief : "+string.Join(" ; ",lines),checks);
    }

    static void Heroes(List<string> checks,int test)
    {
        Start(test,1,1);var p=players[0];Quiet();
        // Platform: jump up from below and stay; down + jump drops through.
        Stand(p,22);float before=p.position.y;Walk(0,1,jump:true);Walk(0,70);float on=p.position.y;int pose=p.animState;
        Walk(0,60);bool stays=Mathf.Abs(p.position.y-1.8f)<.01f;Walk(0,1,jump:true,down:true);Walk(0,50);float dropped=p.position.y;
        Check(before==0&&Mathf.Abs(on-1.8f)<.01f&&stays&&pose==p.Definition.idle&&dropped==0,"Un héros saute sur une plateforme par-dessous et y reste debout (y 1.8, pose d'attente), puis bas + saut le fait redescendre à travers ("+dropped+")",checks);
        // Block: walls the way, jumped onto, walked off.
        Stand(p,27);Walk(1,90);float wall=p.position.x;Walk(1,1,jump:true);Walk(1,10);Walk(0,40);float top=p.position.y;Walk(1,25);Walk(0,30);float off=p.position.y;
        Check(wall>29.5f&&wall<30&&Mathf.Abs(top-1.2f)<.01f&&off==0&&p.position.x>33,"Un bloc arrête le héros (x "+wall.ToString("0.00")+" devant son bord à 30), il saute dessus (y "+top.ToString("0.00")+") et retombe dans la rue de l'autre côté (y "+off+", x "+p.position.x.ToString("0.0")+")",checks);
        // Slope up, terrace, slope down.
        Stand(p,37);float mid=-1,high=-1;for(int t=0;t<160&&p.position.x<55;t++){Walk(1,1);if(mid<0&&p.position.x>=42)mid=p.position.y-(p.position.x-40)/4*1.5f;if(p.position.x>45&&p.position.x<47)high=p.position.y;}
        Check(Mathf.Abs(mid)<.05f&&Mathf.Abs(high-1.5f)<.01f&&p.position.y==0&&p.animState!=p.Definition.jumping,"Le héros monte une pente en suivant sa hauteur (écart "+mid.ToString("0.000")+"), traverse la terrasse à 1.5 et redescend sans sauter",checks);
        // Lethal pit: hurt and back on its edge.
        Stand(p,57);int hp=p.hp;p.invulnerable=0;Walk(1,100,invulnerable:false);int lost=hp-p.hp;float back=p.position.x;
        Check(lost>0&&back<60&&p.position.y==0,"Un trou mortel coûte "+lost+" PV au héros tombé dedans et le remet sur son bord (x "+back.ToString("0.0")+")",checks);
        // Shallow pit: the walls hold him until he jumps out.
        Stand(p,67);Walk(1,90);float wade=p.position.y,stuck=p.position.x;Walk(1,1,jump:true);Walk(1,40);
        Check(Mathf.Abs(wade+.8f)<.01f&&stuck<73&&p.position.x>73.5f&&p.position.y==0,"Dans un trou peu profond il patauge (y "+wade+"), bloqué contre la paroi (x "+stuck.ToString("0.0")+"), jusqu'à ce qu'il saute dehors",checks);
    }

    static void Enemies(List<string> checks,int test)
    {
        Start(test,1,1);var p=players[0];Quiet();Stand(p,26);
        // A rusher walks into the block, leaps onto it and over it.
        var e=Place("looter-rusher",35);float highest=0;bool crossed=false;
        for(int t=0;t<300&&enemies.Contains(e);t++){Tick(1);highest=Mathf.Max(highest,Pos(e).y);if(Pos(e).x<29.9f){crossed=true;break;}}
        Check(highest>1.15f&&crossed,"Un ennemi qui marche vers le héros saute sur un bloc (jusqu'à y "+highest.ToString("0.00")+") et le franchit",checks);
        Clear(enemies);
        // One dropped above the block lands on it; one walked off falls to the street.
        var f=Place("gunman-pistol",31.5f);SetField(f,"position",new Vector2(31.5f,4));Tick(90);float landed=Pos(f).y;
        Check(Mathf.Abs(landed-1.2f)<.01f,"Un ennemi qui tombe se pose sur le relief (y "+landed+")",checks);
        Clear(enemies);
        // A follower jumps up onto the hero's platform, then drops after him.
        Stand(p,22.5f);Walk(0,1,jump:true);Walk(0,60);var c=Place("test-grimpeur",28);float up=0;
        for(int t=0;t<300&&enemies.Contains(c);t++){Tick(1);up=Mathf.Max(up,Pos(c).y);if(Mathf.Abs(Pos(c).y-1.8f)<.01f&&(float)Field(c,"vertical")==0)break;}
        bool onLedge=Mathf.Abs(Pos(c).y-1.8f)<.01f;Walk(0,1,jump:true,down:true);Walk(-1,30);float low=9;for(int t=0;t<240;t++){Tick(1);low=Mathf.Min(low,Pos(c).y);if(low==0)break;}
        Check(onLedge&&low==0,"Un ennemi suiveur (terrain.follow) saute sur la plateforme du héros (y 1.8) puis en redescend derrière lui",checks);
        Clear(enemies);
        // A gunman placed on the car falls when it breaks.
        Stand(p,100);var car=Prop(108);float roof=Pos(car).y+((PropDefinition)Field(car,"type")).box.height;
        var s=new EncounterSpawn{enemy="gunman-pistol",entry="window",x=108,y=roof,fromX=108,fromY=roof,definition=ContentLibrary.Enemy("gunman-pistol"),kind=-1};var gunman=Call(g,"Arrive",s,0,null);
        Tick(80);bool perched=(bool)Field(gunman,"perched")&&Mathf.Abs(Pos(gunman).y-roof)<.01f;string before=" [perché "+Field(gunman,"perched")+" y "+Pos(gunman).y+" mode "+Field(gunman,"mode")+"]";SetField(gunman,"hp",9999);Hit(car,999);int carHp=Hp(car);Tick(80);
        Check(perched&&Pos(gunman).y==0&&!(bool)Field(gunman,"perched"),"Un tireur posté sur une voiture (y "+roof.ToString("0.00")+") y reste, puis tombe dans la rue quand elle explose (sans en mourir ici)"+(perched?"":before),checks);
        Clear(enemies);
    }

    static void Things(List<string> checks,int test)
    {
        Start(test,1,1);var p=players[0];Stand(p,15);
        // The hostage tied on the platform stays there; freed, he runs off it and comes down.
        object h=null;foreach(var o in hostages)if(Mathf.Abs(Pos(o).x-22)<.1f)h=o;Tick(30);float tied=Pos(h).y;
        Quiet();var place=new MapHostage{id="civilian",x=22,y=1.8f,definition=ContentLibrary.Hostage("civilian")};Call(g,"SpawnHostage",place);h=hostages[hostages.Count-1];
        Call(g,"FreeHostage",h,p);float lowest=9;for(int t=0;t<240&&hostages.Contains(h);t++){Tick(1);lowest=Mathf.Min(lowest,Pos(h).y);}
        Check(Mathf.Abs(tied-1.8f)<.01f&&lowest==0,"Un otage attaché sur une plateforme y reste (y 1.8) ; libéré, il en descend en fuyant",checks);
        // A bonus dropped on the block rests on its top.
        Clear(pickups);Call(g,"SpawnPickup",new Vector2(31.5f,0),1);float bonus=Pos(pickups[0]).y;
        Check(Mathf.Abs(bonus-1.85f)<.01f,"Un bonus posé sur un bloc repose sur son dessus (y "+bonus+")",checks);Clear(pickups);
        // Grenades never go into the relief and go off at a wall.
        // One sent low and level into the block's side goes off on it.
        Stand(p,26);p.facing=1;p.grenades=5;commands[0]=new PlayerCommand{grenade=true};Tick(1);commands[0]=default;float far=0;bool inside=false;
        SetField(thrown[0],"position",new Vector2(28,.5f));SetField(thrown[0],"velocity",new Vector2(9,1));
        for(int t=0;t<240&&thrown.Count>0;t++){Tick(1);foreach(var n in thrown){var at=Pos(n);far=Mathf.Max(far,at.x);if(at.y<SolidAt(at.x)-.01f)inside=true;}}
        Stand(p,27);p.facing=1;p.grenades=5;Set(g,"cameraX",33f);commands[0]=new PlayerCommand{grenade=true};Tick(1);commands[0]=default;
        for(int t=0;t<240&&thrown.Count>0;t++){Tick(1);foreach(var n in thrown){var at=Pos(n);if(at.y<SolidAt(at.x)-.01f)inside=true;}}
        Stand(p,42);p.facing=1;commands[0]=new PlayerCommand{grenade=true};Tick(1);commands[0]=default;
        for(int t=0;t<240&&thrown.Count>0;t++){Tick(1);foreach(var n in thrown){var at=Pos(n);if(at.y<SolidAt(at.x)-.01f)inside=true;}}
        Check(far<30.3f&&!inside,"Une grenade envoyée contre la paroi d'un bloc y explose (x "+far.ToString("0.00")+"), et aucune grenade lancée par-dessus ou sur la pente ne traverse le relief",checks);
        // Shots stop at a wall; enemy shots stop at cover.
        Quiet();Stand(p,27.5f);p.facing=1;float shot=0;for(int t=0;t<40;t++){commands[0]=new PlayerCommand{fire=true,down=true};Tick(1);foreach(var o in shots)if(!(bool)Field(o,"hostile"))shot=Mathf.Max(shot,Pos(o).x);}
        commands[0]=default;
        Start(test,1,1);p=players[0];Quiet();Stand(p,104);p.facing=1;var gun=Place("gunman-smg",113);int hp=p.hp;float nearest=999;int fired=0;
        for(int t=0;t<360;t++){p.invulnerable=0;g.TickSimulation(1f/60,commands);foreach(var o in shots)if((bool)Field(o,"hostile")){fired++;nearest=Mathf.Min(nearest,Pos(o).x);}}
        Check(shot<30.3f&&fired>0&&nearest>109&&p.hp==hp,"Les tirs du héros s'arrêtent sur la paroi d'un bloc (x "+shot.ToString("0.00")+"), ceux d'un tireur sur la voiture qui couvre le héros (aucun au-delà de x "+nearest.ToString("0.0")+", PV intacts)",checks);
    }

    static void Props(List<string> checks,int test)
    {
        Start(test,1,1);var p=players[0];Quiet();Stand(p,86);
        var crate=Prop(90);var type=(PropDefinition)Field(crate,"type");Walk(1,90);float wall=p.position.x;
        int full=Hp(crate),score=g.Score,bonuses=pickups.Count;Hit(crate,7);Tick(1);int damaged=(int)Field(crate,"animState");Hit(crate,5);int dropped=pickups.Count;Tick(1);int broken=(int)Field(crate,"animState");
        Walk(1,60);bool passed=p.position.x>91;
        Check(type.name=="CAISSE D'ESSAI"&&wall<89.41f&&wall>88.9f&&damaged==type.damaged&&broken==type.destroyed&&g.Score-score==type.score&&dropped==bonuses+1&&passed,
            "Une caisse écrite en JSON bloque le héros (x "+wall.ToString("0.00")+"), passe à l'image abîmée sous la moitié de ses "+full+" PV puis détruite, rapporte "+type.score+" points et une récompense, et laisse passer une fois détruite"+(passed&&g.Score-score==type.score&&damaged==type.damaged&&broken==type.destroyed&&dropped==bonuses+1?"":" [états "+damaged+"/"+type.damaged+" "+broken+"/"+type.destroyed+" score "+(g.Score-score)+" bonus "+(dropped-bonuses)+" x "+p.position.x+"]"),checks);
        // Shot down by the hero's own fire.
        Start(test,1,1);p=players[0];Quiet();Stand(p,84);p.facing=1;var again=Prop(90);for(int t=0;t<180&&Hp(again)>0;t++){commands[0]=new PlayerCommand{fire=true};Tick(1);}commands[0]=default;
        Check(Hp(again)<=0,"Les tirs du héros détruisent la caisse devant lui",checks);
        // A barrel blows up, sets the next one off, kills the enemy beside it and hurts the hero too close.
        Clear(pickups);Stand(p,98.6f);var e=Place("looter-shield",101);var b1=Prop(100);var b2=Prop(101.6f);int hp=p.hp;p.invulnerable=0;
        Hit(b1,99);bool chained=Hp(b2)<=0;bool killed=!enemies.Contains(e);Tick(1,false);
        Check(chained&&killed&&p.hp<hp,"Un baril qui explose fait sauter le baril voisin, abat l'ennemi à côté et blesse le héros trop près ("+(hp-p.hp)+" PV)",checks);
    }

    static void Objective(List<string> checks,int test)
    {
        Start(test,1,1);var p=players[0];Clear(hostages);Stand(p,110.5f);var barricade=Prop(121);int full=Hp(barricade);
        Hit(barricade,40);bool early=Hp(barricade)==full;
        Walk(1,120);bool locked=(bool)Get(g,"screenLocked")&&g.Wave==0;float blocked=p.position.x;
        for(int t=0;t<900&&(int)Get(g,"pending")+enemies.Count>0;t++){foreach(var e in enemies)if((int)Field(e,"mode")==0)SetField(e,"hp",0);for(int k=enemies.Count-1;k>=0;k--)if((int)Field(enemies[k],"hp")<=0)Call(g,"HitEnemy",k,1,1,0);Tick(1);}
        Tick(120);bool holds=g.Wave==0&&(bool)Get(g,"screenLocked");var sign=(Transform)Field(barricade,"sign");bool signed=sign&&sign.gameObject.activeSelf;
        Hit(barricade,9999);Tick(2);
        Check(early&&locked&&blocked<119&&holds&&signed&&g.Wave==1&&!(bool)Get(g,"screenLocked"),"Une barricade objectif ne s'abîme pas avant sa vague, bloque le héros (x "+blocked.ToString("0.0")+"), retient la vague bloquée (avec son signal) une fois ses ennemis tombés, et sa destruction finit la vague",checks);
        // Destroyed while its enemies still fight: the wave ends at once.
        Start(test,1,1);p=players[0];Clear(hostages);Stand(p,110.5f);Walk(1,120);var other=Prop(121);Tick(60);bool fighting=enemies.Count+(int)Get(g,"pending")>0;Hit(other,9999);Tick(2);
        Check(fighting&&g.Wave==1,"Détruite pendant que ses ennemis arrivent, la barricade finit sa vague sur-le-champ",checks);
    }

    static void JsonOnly(List<string> checks,int test)
    {
        Start(test,1,1);int t=0;float stood=0;
        for(;t<60*60*4&&g.phase==PanameGame.Phase.Playing;t++){commands[0]=Bot(players[0],0,t);Tick(1);stood=Mathf.Max(stood,players[0].floor);}
        Check(g.phase==PanameGame.Phase.Won,"Le bot finit la mission écrite en JSON (plateforme, bloc, pentes, deux trous, caisse et barils propres, voiture, barricade objectif) en "+t/60+" s, debout jusqu'à y "+stood.ToString("0.0"),checks);
    }

    // ---------- Replays and online ----------
    static ReplayRecord Record(int mission,int difficulty,int heroes,int hero,int limit,int giveUp,out List<string> frames)
    {
        Start(mission,difficulty,heroes,hero);var moves=new int[4];var flags=new int[4];var text=new StringBuilder();frames=new List<string>();
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
        int fernandel=ContentLibrary.HeroIndex("fernandel");
        foreach(var (id,d,h,hero,giveUp) in new[]{("panique-a-paname",1,2,0,0),("mistral-gagnant",0,1,1,0),("prophetie-en-provence",2,1,fernandel,0),("tokyo-neon",3,4,2,0),("mistral-gagnant",Slug,1,3,2)})
        {
            var run=Record(M(id),d,h,hero,60*60*15,giveUp,out var frames);int broken=0;foreach(var b in breakables)if(Hp(b)<=0)broken++;
            foreach(var mm in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+mm.id);
            Check(Verify(run)&&Online(run,frames),"Une partie sur "+id+" ("+PanameGame.DifficultyNames[d]+", "+h+" joueur(s), "+PanameGame.Heroes[hero].shortName+", "+broken+" décors détruits) se rejoue à l'identique en replay et en trames en ligne : "+(run.won?"victoire":"défaite")+", "+run.ticks+" pas"+(run.continues>0?", "+run.continues+" continues":""),checks);
        }
    }

    // ---------- The bot ----------
    // VerifyLot3Heroes' bot (VerifyLot2a's, which also shoots up at perched enemies), told about the relief: it measures heights
    // from its floor, jumps a wall or a prop it cannot shoot over, jumps lethal pits, climbs out of shallow ones, hops onto the
    // ledges ahead now and then and up to enemies standing higher, and drops through a ledge to reach those below.
    static PlayerCommand Bot(PanameGame.Player p,int i,int t)
    {
        float nearest=float.MaxValue,dir=1,height=0;int ahead=0;bool aimed=false,perched=false;
        foreach(var e in enemies)
        {
            float d=Pos(e).x-p.position.x;if(Mathf.Abs(d)<Mathf.Abs(nearest)){nearest=d;dir=Mathf.Sign(d);height=Pos(e).y-p.floor;perched=(bool)Field(e,"perched");}
            if(d*p.facing>1.5f&&d*p.facing<9)ahead++;
            // Crouching only dodges a gunman standing level with the hero; one up a slope would be missed.
            if(Mathf.Abs(d)>1.6f&&Mathf.Abs(d)<13&&((float)Field(e,"windup")>0||(int)Field(e,"burst")>0)&&Pos(e).y-p.floor<.3f)aimed=true;
        }
        float lift=p.position.y-p.floor;bool ground=lift<.01f;
        float move=enemies.Count==0?1:Mathf.Abs(nearest)>5?dir:Mathf.Abs(nearest)<1?dir*.15f:dir*.6f;
        bool up=false,down=aimed&&ground,jump=t%71==i*9&&!aimed,grenade=ahead>=2&&t%50==i*5;
        if(lift>.4f&&Mathf.Abs(nearest)<.8f)down=true;
        if(ground&&Mathf.Abs(nearest)>2&&t%240<30)down=true;
        // An enemy on a ledge or in a window: up onto the relief beside it when there is one, else stand under it and shoot up.
        if((perched&&height>.5f||height>1.2f)&&Mathf.Abs(nearest)<8){move=Mathf.Abs(nearest)>.4f?dir:0;up=true;down=false;if(ground&&height<2.2f&&Mathf.Abs(nearest)<2.5f&&t%25==i*3)jump=true;}
        // One below while the hero stands on a ledge: drop through it.
        if(height<-.8f&&Mathf.Abs(nearest)<4&&ground&&(bool)Call(g,"OnLedge",p.position.x,p.position.y,1)&&t%20==i*2){down=true;jump=true;}
        if(!g.BossActive&&ground&&Mathf.Abs(move)>.1f)
        {
            float way=Mathf.Sign(move),front=p.position.x+way*.8f;
            // A wall of the relief, or a prop that walls him and that his shots fly over (a low crate): jump it.
            if(SolidAt(front)>p.position.y+.36f&&(SolidAt(front)<p.position.y+1.1f||t%40==i*4))jump=true;
            var pit=PitAt(p.position.x+way*1.2f);if(pit!=null&&pit.lethal&&PitAt(p.position.x)==null){jump=true;down=false;}
            // A ledge just ahead (a platform, an awning): hop onto it now and then.
            if(t%90==i*7&&FloorAt(front,p.position.y+2.1f)>p.position.y+.6f){jump=true;down=false;}
        }
        if(p.position.y<-.3f&&ground){jump=true;down=false;}
        if(g.BossActive&&enemies.Count==0){float d=((Vector2)Get(g,"bossPosition")).x-p.position.x;move=Mathf.Abs(d)>.8f?Mathf.Sign(d):0;up=move==0&&t%90<60;grenade=t%40==i*7&&Mathf.Abs(d)<8;down=false;}
        if(g.ContinueOffered)jump=t%71==i*9;
        return new PlayerCommand{move=move,fire=true,aimUp=up,down=down,jump=jump,grenade=grenade&&!g.ContinueOffered,special=t%97==i*11};
    }
    static string Run(int mission,int difficulty,int heroes,int hero,out bool won)
    {
        bool slug=difficulty==Slug;Start(mission,difficulty,heroes,hero);int t=0,ledge=0;float stood=0;
        for(;t<60*60*30&&g.phase==PanameGame.Phase.Playing;t++)
        {
            for(int i=0;i<4;i++)commands[i]=players[i]!=null?Bot(players[i],i,t):default;Tick(1,!slug);
            foreach(var p in players)if(p!=null&&p.hp>0){stood=Mathf.Max(stood,p.floor);if(p.floor>.5f&&(bool)Call(g,"OnLedge",p.position.x,p.position.y,1))ledge++;}
        }
        won=g.phase==PanameGame.Phase.Won;int broken=0;foreach(var b in breakables)if(Hp(b)<=0)broken++;
        return PanameGame.DifficultyNames[difficulty]+" "+g.CurrentMission.id.Split('-')[0]+(heroes>1?" ×"+heroes:"")+" "+t/60+" s ("+broken+"/"+breakables.Count+" décors"+(ledge>0?", "+(ledge/60f).ToString("0")+" s sur plateforme":"")+(slug?", "+g.Continues+" continues":"")+")";
    }
    static void Bots(List<string> checks)
    {
        var lines=new List<string>();
        foreach(var id in Four)for(int d=0;d<PanameGame.DifficultyCount;d++)
        {
            var line=Run(M(id),d,1,0,out bool won);if(!won)throw new Exception("ÉCHEC : bot "+line);lines.Add(line);
        }
        Check(true,"Un bot gagne les quatre missions dans les cinq difficultés (invulnérable, vulnérable en SLUG avec continues) : "+string.Join(", ",lines),checks);
        lines.Clear();
        foreach(var id in Four){var line=Run(M(id),1,4,0,out bool won);if(!won)throw new Exception("ÉCHEC : bot "+line);lines.Add(line);}
        Check(true,"Quatre bots gagnent les quatre missions ensemble (ARCADE) : "+string.Join(", ",lines),checks);
        lines.Clear();int brigitte=ContentLibrary.HeroIndex("brigitte-bardot");
        foreach(var id in Four){var line=Run(M(id),1,1,brigitte,out bool won);if(!won)throw new Exception("ÉCHEC : bot (Brigitte) "+line);lines.Add(line);}
        Check(true,"Un bot avec un nouveau héros (Brigitte Bardot) gagne les quatre missions (ARCADE) : "+string.Join(", ",lines),checks);
    }

    // ---------- Captures ----------
    // Each mission's relief and props: standing, then broken.
    static void Captures(List<string> checks,string captures)
    {
        int taken=0;
        foreach(var (id,x,hero) in new[]{("panique-a-paname",62f,64f),("panique-a-paname",286f,288f),("mistral-gagnant",120f,116f),("mistral-gagnant",176f,176f),
                                       ("prophetie-en-provence",137f,133f),("prophetie-en-provence",319f,320f),("tokyo-neon",116f,112f),("tokyo-neon",287f,288f)})
        {
            Start(M(id),1,1);var p=players[0];Quiet();Set(g,"cameraX",x);p.position=new Vector2(hero,FloorAt(hero,9));p.vertical=0;Tick(2);Set(g,"cameraX",x);
            string name=captures+"/lot3a-"+id.Split('-')[0]+"-"+(int)x;Capture(name+"-intact.png");
            foreach(var b in breakables)if(Mathf.Abs(Pos(b).x-x)<13){Set(g,"waveLive",true);typeof(PanameGame).GetProperty("Wave").SetValue(g,Math.Max(0,((MapProp)Field(b,"place")).wave));Hit(b,9999);}
            Quiet();Set(g,"cameraX",x);Tick(50);Set(g,"cameraX",x);Capture(name+"-detruit.png");taken+=2;
        }
        Check(taken==16,"Captures du relief et des décors de chaque mission, intacts puis détruits (lot3a-<mission>-<x>-intact.png, -detruit.png)",checks);
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
