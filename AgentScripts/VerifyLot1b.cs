using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Paname;
// Lot 1b, lettered weapons in data: every weapon of Content/weapons is picked up from a crate and fires by its numbers
// (fire rate, damage, piercing, explosion, range, every aim), refills when picked up again, goes back to the hero's own
// weapon when empty and on death; a weapon written only as JSON here works without code; a mission picks its weapons
// and their proportions; replays stay exact; bots that use the weapons win the three missions; captures show each one firing.
public static class VerifyLot1b
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static void Set(object o,string name,object v)=>o.GetType().GetField(name,Flags).SetValue(o,v);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags|BindingFlags.Static).Invoke(o,args);
    static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
    static void SetField(object o,string name,object v)=>o.GetType().GetField(name).SetValue(o,v);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    static Vector2 Pos(object o)=>(Vector2)Field(o,"position");
    static int Hp(object e)=>(int)Field(e,"hp");

    static PanameGame g;static PanameGame.Player[] players;static IList enemies,shots,pickups;
    static readonly PlayerCommand[] commands=new PlayerCommand[4];
    static readonly string[] Lettered={"heavy-machinegun","shotgun","flamethrower","rocket-launcher","laser"};
    // Missions by their place among the three this test was written for (Paris 0, Marseille 1, Tokyo 2): Saint-Remy now comes
    // before Tokyo and has its own VerifySaintRemy. A test mission appended after the catalogue keeps its index.
    static readonly string[] Three={"panique-a-paname","mistral-gagnant","tokyo-neon"};
    static int Old(PanameGame game,int m){var all=(MissionDefinition[])game.GetType().GetField("missions",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);return m<Three.Length?Array.FindIndex(all,x=>x.id==Three[m]):m;}
    static void Start(int mission,int difficulty,int heroes,int first=0)
    {
        g.ReturnToLobby();Call(g,"LoadMission",Old(g,mission));g.SetDifficulty(difficulty);
        for(int i=0;i<4;i++){g.slots[i].joined=i<heroes;g.slots[i].hero=(first+i)%4;}
        g.BeginRound();players=(PanameGame.Player[])Get(g,"players");enemies=(IList)Get(g,"enemies");shots=(IList)Get(g,"shots");pickups=(IList)Get(g,"pickups");
        for(int i=0;i<4;i++)commands[i]=default;
    }
    static void Tick(int n=1,bool invulnerable=true){for(int t=0;t<n;t++){if(invulnerable)foreach(var p in players)if(p!=null)p.invulnerable=1;g.TickSimulation(1f/60,commands);}}
    static void ClearEnemies(){foreach(var e in enemies)((Transform)Field(e,"root")).gameObject.SetActive(false);enemies.Clear();}
    static void ClearShots(){while(shots.Count>0){Tick(1);}}
    static object Spawn(float x,string id,int hp=100)
    {
        int kind=Array.IndexOf(g.CurrentMission.enemies,id);if(kind<0)throw new Exception("ennemi absent de la mission : "+id);
        Call(g,"SpawnEnemy",x,kind);var e=enemies[enemies.Count-1];
        SetField(e,"hp",hp);SetField(e,"stagger",999f);SetField(e,"attackTimer",999f);
        return e;
    }
    static Vector2 Muzzle(PanameGame.Player p)=>(Vector2)Call(g,"MuzzlePoint",p);
    // No more waves: the street stays empty while a test walks or poses.
    static void NoWaves()=>typeof(PanameGame).GetProperty("Wave").SetValue(g,g.CurrentMission.waves.Length);
    // One press of fire, then the shots fly out.
    static void FireOnce(int settle=90){commands[0]=new PlayerCommand{fire=true};Tick(1);commands[0]=default;Tick(settle);}
    static WeaponChance[] Table(params (string id,int weight)[] lines)
    {
        var table=Array.ConvertAll(lines,l=>new WeaponChance{weapon=l.id,weight=l.weight});GameRules.Table(table,"test",true);return table;
    }
    // How far a projectile goes, integrated like the engine (speed clamped to 0..80).
    static float Reach(WeaponDefinition w)
    {
        var pr=w.projectile;if(pr.Beam)return pr.range;float v=pr.speed,d=0;
        for(float t=0;t<pr.life;t+=1f/60){v=Mathf.Clamp(v+pr.acceleration/60,0,80);d+=v/60;}
        return d;
    }

    public static object Main(string captures)
    {
        var checks=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        float scale=Time.timeScale;Time.timeScale=0;
        var missions=(MissionDefinition[])Get(g,"missions");var tables=Array.ConvertAll(missions,m=>m.weaponTable);var ruleSets=Array.ConvertAll(missions,m=>m.ruleSet);
        try
        {
            // Tests that pick a crate's weapon replace the mission's table; the shipped tables come back before the next one.
            void Restore(){for(int i=0;i<missions.Length;i++)missions[i].weaponTable=tables[i];}
            Library(checks);Weapons(checks);Restore();Lifecycle(checks);Aims(checks);JsonOnly(checks);Restore();MissionTables(checks,missions);Restore();
            Replays(checks,missions);Restore();Bots(checks,missions,tables);Restore();Captures(checks,captures);
        }
        finally
        {
            for(int i=0;i<missions.Length;i++){missions[i].weaponTable=tables[i];missions[i].ruleSet=ruleSets[i];}
            ContentLibrary.Reload();
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;}
            foreach(var m in missions)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
            g.ReturnToLobby();Call(g,"LoadMission",0);g.SetDifficulty(1);g.ReturnToLobby();Time.timeScale=scale;
        }
        return string.Join("\n",checks);
    }

    static void Library(List<string> checks)
    {
        var letters=new List<string>();
        foreach(var id in Lettered){var w=ContentLibrary.Weapon(id);letters.Add(w.letter);if(w.Unlimited||!w.crate.Sprite||!w.icon.Sprite||!w.Clip||w.ammo[0]<w.ammo[3])throw new Exception("arme incomplète : "+id);}
        Check(string.Join("",letters)=="HSFRL","Cinq armes à lettres dans Content/weapons (H, S, F, R, L), chacune avec caisse, badge, son et munitions décroissantes par difficulté",checks);
        bool bases=true;foreach(var h in ContentLibrary.Heroes)bases&=h.Weapon!=null&&h.Weapon.Unlimited&&h.Weapon.Clip;
        Check(bases,"Chaque héros a son arme de base illimitée en données ("+string.Join(", ",Array.ConvertAll(ContentLibrary.Heroes,h=>h.id+" : "+h.Weapon.id))+")",checks);
        string refused="";
        foreach(var bad in new[]{@"{""id"": ""test-bad"", ""extends"": ""shotgun"", ""projectile"": {""type"": ""plasma""}}",@"{""id"": ""test-bad"", ""extends"": ""shotgun"", ""letter"": """"}",@"{""id"": ""test-bad"", ""extends"": ""shotgun"", ""ammo"": [5, 0, 5, 5]}"})
            try{ContentLibrary.AddWeapon(bad);}catch(ArgumentException e){refused+=e.Message.Split(':')[0].Trim()+" ; ";}
        Check(refused.Split(';').Length==4,"Une arme invalide est refusée (type inconnu, lettre manquante, munitions incohérentes)",checks);
    }

    // Per weapon: picked up from a crate, then fire rate, damage, piercing or explosion, and range.
    static void Weapons(List<string> checks)
    {
        foreach(var id in Lettered)
        {
            var w=ContentLibrary.Weapon(id);var pr=w.projectile;
            Start(0,1,1);var p=players[0];ClearEnemies();Tick(10);
            // A crate drawn from a one-weapon table, under the hero's feet.
            g.CurrentMission.weaponTable=Table((id,1));Call(g,"SpawnPickup",p.position,0);var crate=pickups[pickups.Count-1];
            bool shown=((SpriteRenderer)((Transform)Field(crate,"root")).GetComponent<SpriteRenderer>()).sprite==w.crate.Sprite;Tick(2);
            Check(shown&&p.weapon==w&&p.ammo==w.ammo[1],w.letter+" : la caisse montre "+w.name+", le héros la ramasse avec "+p.ammo+" munitions (Arcade)",checks);
            // Fire rate: one second of held fire, nobody around.
            // The engine counts the timer down by whole ticks: the expected count follows the same steps.
            float timer=p.fireTimer;int expected=0;for(int t=0;t<60;t++){timer-=1f/60;if(timer<=0){expected++;timer=w.fireRate;}}
            int ammo=p.ammo;commands[0]=new PlayerCommand{fire=true};Tick(60);commands[0]=default;int fired=ammo-p.ammo;
            Check(fired==expected&&fired>=1,w.letter+" : cadence "+fired+" tirs par seconde, comme le veut fireRate "+w.fireRate+" compté en pas de 1/60 s",checks);
            ClearShots();
            // Three enemies in a row: piercing weapons go through all of them, the explosion reaches its radius.
            float x=Muzzle(p).x;var a=Spawn(x+2.8f,"looter-shield");var b=Spawn(x+4.3f,"looter-shield");var c=Spawn(x+5.8f,"looter-shield");
            p.fireTimer=0;FireOnce(80);int da=100-Hp(a),db=100-Hp(b),dc=100-Hp(c);string hits=da+"/"+db+"/"+dc;
            if(w.explosion.radius>0)
            {
                int boom=w.explosion.damage>0?w.explosion.damage:w.damage;
                Check(da==boom&&db==boom&&dc==0,w.letter+" : l'explosion ("+w.explosion.radius+" u, "+boom+" dégâts) blesse l'ennemi touché et son voisin, pas le troisième à 3 u ("+hits+")",checks);
            }
            else if(pr.pierce<0)Check(da>=w.damage&&db>=w.damage&&dc>=w.damage&&da%w.damage==0,w.letter+" : traverse les trois ennemis alignés, "+w.damage+" dégâts par projectile ("+hits+")",checks);
            else Check(da==w.damage&&db==0&&dc==0,w.letter+" : un tir touche le premier ennemi seul, "+w.damage+" dégâts ("+hits+")",checks);
            ClearEnemies();ClearShots();
            // Range: an enemy just beyond what the projectile covers is not touched, one well inside is.
            float reach=Reach(w);
            if(reach<20)
            {
                var near=Spawn(x+Mathf.Max(2.6f,reach*.6f),"looter-shield");var far=Spawn(x+reach+2,"looter-shield");p.fireTimer=0;FireOnce(80);
                Check(Hp(near)<100&&Hp(far)==100,w.letter+" : portée "+reach.ToString("0.0")+" u, l'ennemi au-delà n'est pas touché",checks);
                ClearEnemies();
            }
            else Check(reach>=20,w.letter+" : longue portée ("+reach.ToString("0")+" u, plus que l'écran)",checks);
        }
        // The rocket homes in: fired level, it climbs to an enemy above the line of fire.
        Start(0,1,1);var q=players[0];ClearEnemies();Tick(10);g.GiveWeapon(0,"rocket-launcher");q.fireTimer=0;
        var high=Spawn(Muzzle(q).x+7,"looter-shield");SetField(high,"position",new Vector2(Muzzle(q).x+7,1.6f));FireOnce(90);
        Check(Hp(high)<100,"R : la roquette à tête chercheuse rejoint un ennemi au-dessus de la ligne de tir",checks);
    }

    static void Lifecycle(List<string> checks)
    {
        Start(0,1,1);var p=players[0];ClearEnemies();Tick(10);var hero=p.Definition.Weapon;
        Check(p.weapon==hero&&p.ammo==0,"Le héros commence avec son arme de base, sans compteur ("+hero.name+")",checks);
        g.GiveWeapon(0,"heavy-machinegun");var h=p.weapon;commands[0]=new PlayerCommand{fire=true};Tick(30);commands[0]=default;int left=p.ammo;
        g.GiveWeapon(0,"heavy-machinegun");
        Check(left<h.ammo[1]&&p.ammo==h.ammo[1]&&p.weapon==h,"Ramasser la même arme recharge ses munitions ("+left+" puis "+p.ammo+")",checks);
        g.GiveWeapon(0,"laser");Check(p.weapon.id=="laser"&&p.ammo==p.weapon.ammo[1],"Une autre arme remplace la première, avec ses propres munitions",checks);
        p.ammo=1;p.fireTimer=0;FireOnce(5);
        Check(p.weapon==hero&&p.ammo==0,"À munitions nulles, retour à l'arme de base",checks);
        g.GiveWeapon(0,"shotgun");p.invulnerable=0;int reserves=g.Reserves;Call(g,"Damage",p,99);
        Check(p.hp<=0&&p.weapon==hero,"À la mort, l'arme à lettre est perdue",checks);
        for(int t=0;t<60*4&&p.hp<=0;t++)Tick(1);
        Check(p.hp>0&&p.weapon==hero&&g.Reserves==reserves-1,"Le héros revient en renfort avec son arme de base",checks);
        Check(!g.GiveWeapon(0,"arme-inconnue")&&!g.GiveWeapon(3,"laser"),"GiveWeapon refuse une arme inconnue ou un joueur absent",checks);
        // The weapon in hand is part of the state compared online.
        int before=(int)Call(g,"StateHash");g.GiveWeapon(0,"flamethrower");int after=(int)Call(g,"StateHash");
        Check(before!=after,"L'arme tenue entre dans l'empreinte d'état comparée en ligne",checks);
    }

    // Up, diagonal, crouched and down in the air work with every weapon.
    static void Aims(List<string> checks)
    {
        var report=new List<string>();
        foreach(var id in Lettered)
        {
            Start(0,1,1);var p=players[0];ClearEnemies();Tick(10);g.GiveWeapon(0,id);var w=p.weapon;string seen="";float standing=Muzzle(p).y;
            foreach(var aim in new[]{"haut","diagonale","accroupi","bas"})
            {
                ClearShots();p.fireTimer=0;var command=new PlayerCommand{fire=true,aimUp=aim=="haut"||aim=="diagonale",move=aim=="diagonale"?.3f:0,down=aim=="accroupi"||aim=="bas"};
                if(aim=="bas"){commands[0]=new PlayerCommand{jump=true};Tick(1);commands[0]=new PlayerCommand{down=true};Tick(8);}
                if(aim=="accroupi"){commands[0]=new PlayerCommand{down=true};Tick(3);}
                p.fireTimer=0;commands[0]=command;Tick(1);commands[0]=default;
                Vector2 d=Vector2.zero;float y=0;
                foreach(var s in shots){var v=(Vector2)Field(s,"velocity");if(v.sqrMagnitude<.01f)v=(Vector2)Field(s,"heading");d+=v.normalized;y=Pos(s).y;}
                d=d.normalized;bool ok=aim=="haut"?d.y>.97f:aim=="diagonale"?d.x>.55f&&d.y>.55f:aim=="bas"?d.y<-.97f:Mathf.Abs(d.y)<.3f&&p.crouching;
                if(aim=="accroupi")ok&=Muzzle(p).y<standing-.2f;
                if(!ok)throw new Exception("ÉCHEC : "+w.letter+" vers "+aim+" ("+d+")");seen+=aim+" ";
                Tick(40);
            }
            report.Add(w.letter);
        }
        Check(report.Count==5,"Chaque arme tire vers le haut, en diagonale, accroupi et vers le bas en saut ("+string.Join("",report)+")",checks);
    }

    // A weapon written only here as JSON, and a crate that gives it, play without any code.
    const string Zapper=@"{""id"": ""test-zapper"", ""letter"": ""Z"", ""name"": ""ZAPPEUR"", ""ammo"": [7, 7, 7, 7], ""fireRate"": 0.3, ""damage"": 7,
      ""projectile"": {""type"": ""bullet"", ""pellets"": 2, ""spread"": 2, ""speed"": 20, ""life"": 0.9, ""pierce"": 1, ""size"": 0.8, ""color"": ""88FFAA"",
                      ""sprite"": {""texture"": ""Art/weapon-fx"", ""columns"": 3, ""rows"": 2, ""cell"": 5}},
      ""recoil"": {""shake"": 0.1, ""push"": 3}, ""flash"": ""heavy"", ""casings"": false, ""sound"": ""Audio/Sfx/tir-B-laser-snes"", ""announce"": ""ZAPPEUR !"",
      ""icon"": {""texture"": ""Art/weapon-letters"", ""columns"": 3, ""rows"": 2, ""cell"": 5}, ""crate"": {""texture"": ""Art/bonus-icons"", ""columns"": 3, ""rows"": 2, ""cell"": 1}}";
    static void JsonOnly(List<string> checks)
    {
        var zap=ContentLibrary.AddWeapon(Zapper);
        var rules=ContentLibrary.AddRules(@"{""id"": ""test-zap-rules"", ""extends"": ""default"", ""bonuses"": [{""id"": ""zap"", ""effect"": ""weapon"", ""weapon"": ""test-zapper""}], ""supplies"": [0], ""drops"": [0]}");
        var mission=g.CurrentMission;var shipped=mission.ruleSet;
        try
        {
            Start(0,1,1);mission.ruleSet=rules;g.BeginRound();players=(PanameGame.Player[])Get(g,"players");var p=players[0];ClearEnemies();NoWaves();
            // The first crate of the route stands at firstSupply: walk to it.
            commands[0]=new PlayerCommand{move=1};for(int t=0;t<60*8&&p.weapon!=zap;t++)Tick(1);commands[0]=default;
            Check(p.weapon==zap&&p.ammo==7,"Une arme définie seulement en JSON ("+zap.id+") sort de la première caisse du parcours, 7 munitions",checks);
            Tick(20);float x=Muzzle(p).x;var a=Spawn(x+3,"looter-shield");var b=Spawn(x+4.5f,"looter-shield");var c=Spawn(x+6,"looter-shield");
            float before=p.position.x;p.fireTimer=0;commands[0]=new PlayerCommand{fire=true};Tick(1);commands[0]=default;Tick(3);float pushed=before-p.position.x;
            int count=0;foreach(var s in shots)if(Field(s,"weapon")==zap)count++;
            Tick(60);
            Check(count==2&&Hp(a)==100-14&&Hp(b)==100-14&&Hp(c)==100,"Elle tire deux projectiles qui percent un ennemi chacun : 7 + 7 sur les deux premiers, rien sur le troisième ("+Hp(a)+", "+Hp(b)+", "+Hp(c)+")",checks);
            Check(pushed>.02f,"Son recul repousse le héros ("+pushed.ToString("0.00")+" u)",checks);
            ClearEnemies();commands[0]=new PlayerCommand{fire=true};Tick(60*3);commands[0]=default;
            Check(p.weapon==p.Definition.Weapon,"Ses sept munitions tirées, retour à l'arme de base",checks);
        }
        finally{mission.ruleSet=shipped;}
    }

    static void MissionTables(List<string> checks,MissionDefinition[] missions)
    {
        // The shipped missions: Paris plays the rules' table, the other two their own.
        Check((missions[0].weapons==null||missions[0].weapons.Length==0)&&missions[0].weaponTable==missions[0].ruleSet.weapons&&missions[1].weaponTable[0].gun.id=="shotgun"&&missions[Old(g,2)].weaponTable[0].gun.id=="laser",
            "Chaque mission choisit ses armes : Paris garde la table des règles, Marseille et Tokyo ont la leur (mission.weapons)",checks);
        Start(0,1,1);g.CurrentMission.weaponTable=Table(("flamethrower",3),("laser",1));Call(g,"ResetWeaponDraws");var drawn=new List<string>();
        var p=players[0];
        for(int i=0;i<12;i++){Call(g,"SpawnPickup",new Vector2(p.position.x+20+i,0),0);drawn.Add(((WeaponDefinition)Field(pickups[pickups.Count-1],"gun")).letter);}
        int f=drawn.FindAll(l=>l=="F").Count;
        Check(f==9&&drawn.Count==12&&drawn[2]=="L","Ses caisses donnent ces armes dans ses proportions, réparties : 3 F pour 1 L ("+string.Join("",drawn)+")",checks);
        var text=Resources.Load<TextAsset>("Levels/"+missions[Old(g,2)].id).text;bool refused=false;
        try{MissionDefinition.Parse(text.Replace("\"weapon\": \"laser\"","\"weapon\": \"arme-inconnue\""));}catch(ArgumentException){refused=true;}
        Check(refused,"Une mission qui nomme une arme inconnue est refusée",checks);
    }

    // Recorded runs with weapons, replayed in the hidden verifier (the same simulation as online lockstep).
    static void Replays(List<string> checks,MissionDefinition[] missions)
    {
        var verifier=PanameGame.CreateVerifier(missions);
        var all=new HashSet<string>();
        // Marseille at DIFFICILE with three heroes: since lot 2b, three bots at LÉGENDE fall there before any crate.
        foreach(var run in new[]{(m:0,d:1,h:2),(m:2,d:0,h:1),(m:1,d:2,h:3)})
        {
            Start(run.m,run.d,run.h);var moves=new int[4];var flags=new int[4];var used=new Dictionary<string,int>();
            for(int t=0;t<60*60*10&&g.phase==PanameGame.Phase.Playing;t++)
            {
                for(int i=0;i<4;i++){moves[i]=0;flags[i]=0;var q=players[i];if(q==null)continue;var c=Bot(q,i,t);moves[i]=Mathf.RoundToInt(c.move*10)*10;flags[i]=(c.fire?1:0)|(c.aimUp?2:0)|(c.jump?4:0)|(c.special?8:0)|(c.down?16:0)|(c.grenade?32:0);commands[i]=(PlayerCommand)Call(g,"Command",moves[i],flags[i]);}
                var held=new WeaponDefinition[4];var fired=new int[4];for(int i=0;i<4;i++)if(players[i]!=null){held[i]=players[i].weapon;fired[i]=players[i].shots;}
                Call(g,"RecordTick",0,moves,flags);g.TickSimulation(1f/60,commands);Call(g,"EndReplayTick");
                for(int i=0;i<4;i++)if(players[i]!=null&&players[i].shots>fired[i]&&!held[i].Unlimited){used.TryGetValue(held[i].letter,out int n);used[held[i].letter]=n+1;}
            }
            var record=g.LastReplay;foreach(var m in missions)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
            bool ok=record!=null&&verifier.BeginVerify(JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record)));bool? result=null;while(ok&&result==null)result=verifier.StepVerify(1e6f);
            all.UnionWith(used.Keys);
            Check(ok&&result==true&&used.Count>=1,"Une partie avec "+used.Count+" armes à lettres ("+Describe(used)+") se rejoue à l'identique, même empreinte d'état : "+missions[Old(g,run.m)].id+" ×"+run.h+" ("+(record.won?"victoire":"défaite")+", "+record.ticks+" pas)",checks);
        }
        Check(all.Count>=3,"Ces parties rejouées couvrent "+all.Count+" armes différentes",checks);
    }
    static string Describe(Dictionary<string,int> used){var parts=new List<string>();foreach(var l in "HSFRLZ")if(used.TryGetValue(l.ToString(),out int n))parts.Add(l+" "+n);return string.Join(", ",parts);}

    // VerifyLot1a's bot: walks to the nearest enemy, crouches while a gunman aims, stabs up close, throws grenades into
    // groups, shoots down onto enemies below, and stands under the boss shooting up. It picks up the crates on its way.
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
        return new PlayerCommand{move=move,fire=true,aimUp=up,down=down,jump=jump,grenade=grenade,special=t%97==i*11};
    }

    static void Bots(List<string> checks,MissionDefinition[] missions,WeaponChance[][] tables)
    {
        foreach(var run in new[]{(m:0,h:1),(m:1,h:1),(m:2,h:1),(m:2,h:4)})
        {
            Start(run.m,1,run.h);var used=new Dictionary<string,int>();int t=0;
            for(;t<60*60*20&&g.phase==PanameGame.Phase.Playing;t++)
            {
                var held=new WeaponDefinition[4];var fired=new int[4];
                for(int i=0;i<4;i++){commands[i]=players[i]==null?default:Bot(players[i],i,t);if(players[i]!=null){held[i]=players[i].weapon;fired[i]=players[i].shots;}}
                Tick(1);
                for(int i=0;i<4;i++)if(players[i]!=null&&players[i].shots>fired[i]&&!held[i].Unlimited){used.TryGetValue(held[i].letter,out int n);used[held[i].letter]=n+1;}
            }
            Check(g.phase==PanameGame.Phase.Won&&used.Count>=2,"Un bot gagne "+g.CurrentMission.title+" ×"+run.h+" en "+(t/60)+" s avec les armes des caisses ("+Describe(used)+" tirs)",checks);
        }
        // Balance: the same bot, Paris solo, with and without weapon crates, by difficulty.
        var times=new List<string>();
        foreach(int d in new[]{0,1,3})foreach(bool armed in new[]{true,false})
        {
            Start(0,d,1);if(!armed)g.CurrentMission.weaponTable=new WeaponChance[0];int t=0;
            for(;t<60*60*20&&g.phase==PanameGame.Phase.Playing;t++){commands[0]=Bot(players[0],0,t);Tick(1);}
            g.CurrentMission.weaponTable=tables[0];
            if(g.phase!=PanameGame.Phase.Won)throw new Exception("ÉCHEC : bot de Paris en difficulté "+d);
            times.Add(PanameGame.DifficultyNames[d]+(armed?" avec armes ":" sans armes ")+t/60+" s");
        }
        Check(times.Count==6,"Équilibrage, bot invulnérable à Paris en solo : "+string.Join(", ",times),checks);
    }

    // In the street: each weapon firing at three enemies, the five crates, and some aims.
    static void Captures(List<string> checks,string captures)
    {
        var frames=new Dictionary<string,int>{{"heavy-machinegun",14},{"shotgun",5},{"flamethrower",24},{"rocket-launcher",7},{"laser",8}};int taken=0;
        foreach(var id in Lettered)
        {
            Start(0,1,1);var p=players[0];ClearEnemies();NoWaves();p.position=new Vector2(14,0);Tick(5);g.GiveWeapon(0,id);
            float x=Muzzle(p).x;Spawn(x+4.5f,"looter-shield",500);Spawn(x+6.5f,"looter-rusher",500);Spawn(x+8.5f,"looter-shield",500);
            p.fireTimer=0;commands[0]=new PlayerCommand{fire=true};Tick(frames[id]);
            Set(g,"cameraX",p.position.x+6);Capture(captures+"/lot1b-"+id+".png");taken++;commands[0]=default;
        }
        // Aims: the laser up, the flame thrower diagonally, the shotgun crouched, the heavy machine gun down from a jump.
        foreach(var (id,shot) in new[]{("laser","up"),("flamethrower","diagonal"),("shotgun","crouch"),("heavy-machinegun","down")})
        {
            Start(0,1,1);var p=players[0];ClearEnemies();NoWaves();p.position=new Vector2(14,0);Tick(5);g.GiveWeapon(0,id);p.fireTimer=0;
            if(shot=="down"){commands[0]=new PlayerCommand{jump=true};Tick(1);commands[0]=new PlayerCommand{down=true,fire=true};Tick(14);}
            else{commands[0]=new PlayerCommand{fire=true,aimUp=shot!="crouch",move=shot=="diagonal"?.2f:0,down=shot=="crouch"};Tick(shot=="crouch"?5:18);}
            Set(g,"cameraX",p.position.x+5);Capture(captures+"/lot1b-aim-"+shot+".png");taken++;commands[0]=default;
        }
        {
            Start(0,1,1);var p=players[0];ClearEnemies();p.position=new Vector2(8,0);Tick(2);
            for(int i=0;i<Lettered.Length;i++){g.CurrentMission.weaponTable=Table((Lettered[i],1));Call(g,"SpawnPickup",new Vector2(11+i*2.2f,0),0);}
            Tick(1);Set(g,"cameraX",14f);Capture(captures+"/lot1b-crates.png");taken++;
        }
        Check(taken==10,"Captures : chaque arme qui tire (lot1b-<arme>.png), quatre visées (lot1b-aim-*.png) et les cinq caisses (lot1b-crates.png)",checks);
    }

    static void Capture(string path)
    {
        // Heroes drawn solid, not in their blinking invulnerable phase.
        for(int i=0;i<4;i++)if(players[i]!=null&&players[i].hp>0){players[i].invulnerable=0;Call(g,"SyncPlayer",i);}
        Set(g,"shake",0f);g.gameCamera.aspect=16f/9;var x=(float)Get(g,"cameraX");
        PanameArt.UpdatePanorama(x,g.gameCamera.orthographicSize*g.gameCamera.aspect,g.CurrentMission.cameraMin,g.CurrentMission.cameraMax);g.gameCamera.transform.position=new Vector3(x,3.6f,-10);
        var rt=new RenderTexture(1280,720,24);g.gameCamera.targetTexture=rt;g.gameCamera.Render();
        RenderTexture.active=rt;var shot=new Texture2D(1280,720,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,1280,720),0,0);shot.Apply();
        g.gameCamera.targetTexture=null;RenderTexture.active=null;File.WriteAllBytes(path,shot.EncodeToPNG());
    }
}
