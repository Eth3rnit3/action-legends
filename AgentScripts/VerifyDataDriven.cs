using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Paname;
// Levels are data: a mission written only as JSON strings (a new enemy with its own animations, an existing boss
// re-parameterised, a map made of existing scenery) is played by a bot to victory; the numbers of the shipped heroes,
// enemies and bosses are those the engine had in code before they moved to Resources/Content; bot runs recorded on
// that engine end on the same tick, score and state hash; and no mission name is written in Assets/Scripts.
public static class VerifyDataDriven
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static void Set(object o,string name,object v)=>o.GetType().GetField(name,Flags).SetValue(o,v);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags).Invoke(o,args);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label+(lastFails.Length>0?" ["+lastFails+"]":""));checks.Add(label);lastFails="";}
    static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
    static readonly List<string> fails=new List<string>();static string where="";
    static void Expect(bool ok,[System.Runtime.CompilerServices.CallerLineNumber]int line=0){if(!ok)fails.Add(where+"@"+line);}
    static bool Clean(){if(fails.Count==0)return true;Debug.LogError("VerifyDataDriven : "+string.Join(", ",fails));lastFails=string.Join(", ",fails);return false;}
    static string lastFails="";
    // The old formulas, rounded to single precision after each operation as the browser build computes them.
    static float F(double x)=>(float)x;
    static float EnemyX(object e)=>((Vector2)Field(e,"position")).x;

    const string Sniper=@"{
      ""id"": ""test-sniper"", ""behavior"": ""gunman"",
      ""health"": 5, ""speed"": 4.2, ""keepDistance"": 5, ""hitRadius"": 0.55, ""knockback"": 2, ""firstAttack"": 0.5, ""attackPose"": 0.3, ""strideRate"": 9, ""bonusScore"": 25,
      ""gun"": {""range"": 10, ""aimTime"": 0.3, ""cooldown"": 1.2, ""burst"": 2, ""burstInterval"": 0.15, ""pellets"": 2, ""spread"": -2, ""spreadStep"": 4,
               ""speed"": [10, 11, 12, 13], ""life"": 1, ""pose"": 0.2, ""damage"": 1, ""projectile"": ""bullet"", ""sound"": ""Audio/Sfx/tir-D-pistolet-sec""},
      ""animations"": {
        ""sheet"": {""texture"": ""Art/heroes"", ""columns"": 4, ""rows"": 4, ""cropLeft"": 0.065, ""cropRight"": 0.025, ""pivot"": {""x"": 0.4, ""y"": 0.045}, ""cellHeight"": 2.5},
        ""states"": [
          {""name"": ""idle"", ""cells"": [12, 13], ""fps"": 4},
          {""name"": ""run"", ""cells"": [13, 14, 15], ""perStride"": 0.5},
          {""name"": ""aim"", ""cells"": [15]},
          {""name"": ""attack"", ""cells"": [12]},
          {""name"": ""hurt"", ""cells"": [14]},
          {""name"": ""death"", ""cells"": [15, 12], ""fps"": 8, ""loop"": false}],
        ""anchors"": [{""name"": ""muzzle"", ""cell"": 12, ""x"": 1.33, ""y"": 1.54}]}}";
    const string Gunship=@"{
      ""id"": ""test-gunship"", ""extends"": ""le-direct"", ""name"": ""HÉLICO D'ESSAI"", ""intro"": ""ESSAI : HÉLICO EN APPROCHE !"",
      ""health"": [400, 600, 800, 1000], ""flight"": {""speed"": [2, 2, 2, 2], ""altitude"": 4},
      ""attacks"": [
        {""type"": ""bombs"", ""announce"": ""ESSAI : UNE BOMBE !"", ""count"": [1, 1, 1, 1], ""rageExtra"": 0, ""spacing"": 2, ""delay"": [0.4, 0.4, 0.4, 0.4], ""stagger"": 0.1,
         ""flight"": [0.9, 0.9, 0.9, 0.9], ""arc"": 0.5, ""blastRadius"": 1.2, ""blastHeight"": 1, ""damage"": 1, ""marker"": 2.4, ""rack"": ""rack"", ""cooldown"": 0.8},
        {""type"": ""gun"", ""announce"": ""ESSAI : RAFALE !"", ""aimTime"": [0.5, 0.5, 0.5, 0.5], ""duration"": 1, ""interval"": [0.2, 0.2, 0.2, 0.2], ""speed"": [12, 12, 12, 12],
         ""sweep"": 1, ""sweepRate"": 3, ""aimHeight"": 0.9, ""sightWidth"": 3, ""muzzles"": [""gun-b""], ""sound"": ""Audio/Sfx/tir-C-blaster-cps1"", ""damage"": 1, ""cooldown"": 0.7},
        {""type"": ""bombs"", ""announce"": ""ESSAI : DEUX BOMBES !"", ""count"": [2, 2, 2, 2], ""rageExtra"": 1, ""spacing"": 3, ""delay"": [0.3, 0.3, 0.3, 0.3], ""stagger"": 0.2,
         ""flight"": [0.8, 0.8, 0.8, 0.8], ""arc"": 1, ""blastRadius"": 1.2, ""blastHeight"": 1, ""damage"": 1, ""marker"": 2.4, ""rack"": ""rack"", ""cooldown"": 1}]}";
    const string Mission=@"{
      ""schemaVersion"": 2, ""order"": 90, ""id"": ""test-data-driven"", ""title"": ""Mission en données"", ""location"": ""Banc d'essai"",
      ""intro"": ""ESSAI : TOUT VIENT DU JSON !"", ""exitLabel"": ""LA SORTIE"", ""winTitle"": ""ESSAI RÉUSSI."",
      ""music"": ""Audio/Music/sakura-arcade"", ""bossMusic"": ""Audio/Music/coupo-santo-boss"",
      ""map"": {""backgrounds"": [""Art/tokyo/shibuya"", ""Art/marseille/panier"", ""Art/canal-panorama""], ""ground"": ""Art/marseille/sol"",
               ""terrain"": [], ""props"": [], ""hostages"": [], ""vehicles"": []},
      ""enemies"": [""test-sniper"", ""looter-rusher"", ""gunman-pistol""],
      ""extraEnemy"": {""enemy"": 0, ""every"": 3, ""fromDifficulty"": 0},
      ""length"": 120, ""exitX"": 117, ""cameraMin"": 7, ""cameraMax"": 114,
      ""waves"": [
        {""atX"": 14, ""baseEnemies"": 6, ""enemiesPerPlayer"": 2, ""maxAlive"": 6, ""spawnInterval"": 0.4, ""enemyPattern"": [0, 1, 0, 2]},
        {""atX"": 50, ""baseEnemies"": 8, ""enemiesPerPlayer"": 2, ""maxAlive"": 8, ""spawnInterval"": 0.3, ""enemyPattern"": [1, 0, 2]},
        {""atX"": 86, ""baseEnemies"": 8, ""enemiesPerPlayer"": 2, ""maxAlive"": 8, ""spawnInterval"": 0.3, ""enemyPattern"": [0, 0, 1], ""lock"": false}],
      ""encounters"": [],
      ""boss"": {""enabled"": true, ""id"": ""test-gunship"", ""health"": 0.5}}";

    // Numbers the engine held in code before this content library, per enemy id (health, speed, distance kept,
    // range, aim time, cooldown, hit radius, knockback, kill bonus) and the gunmen's barrels.
    static readonly Dictionary<string,float[]> OldEnemies=new Dictionary<string,float[]>{
        {"looter-rusher",new[]{2,5.8f,.6f,0,0,0,.5f,3.5f,0}},{"looter-thrower",new[]{3,3.6f,5,10,0,1.65f,.5f,3.5f,0}},{"looter-shield",new[]{6,3.3f,.6f,0,0,0,.68f,1.2f,50}},
        {"gunman-pistol",new[]{3,4.6f,6,11,.42f,1.7f,.5f,3.5f,0}},{"gunman-smg",new[]{4,3.8f,7.5f,12,.5f,2.5f,.5f,3.5f,0}},{"gunman-shotgun",new[]{7,3,3.2f,6,.55f,2.3f,.68f,1.2f,50}}};
    static readonly Dictionary<string,Vector2> OldMuzzles=new Dictionary<string,Vector2>{{"gunman-pistol",new Vector2(1.33f,1.43f)},{"gunman-smg",new Vector2(1.18f,1.39f)},{"gunman-shotgun",new Vector2(1.52f,1.61f)}};
    static readonly string[] OldHeroNames={"CHUCK NORRIS","ARNOLD","JEAN-CLAUDE","SYLVESTER"};
    static readonly float[] OldSpeed={10.5f,9.8f,11.5f,10.7f},OldRate={.095f,.065f,.085f,.10f},OldJump={11,11,12.8f,11},OldShake={.025f,.07f,.025f,.025f};
    static readonly int[] OldDamage={3,5,3,4};
    static readonly float[] HealthFactors={1,1.8f,2.5f,3.2f},SpeedFactors={1,1.12f,1.25f,1.4f};
    // Bot runs (mission, difficulty, heroes, invulnerable): ticks, score, state hash. Recorded on the engine before the
    // library, then again in lot 1a, which changes gameplay on purpose: the new hero sheets move every barrel tip (shots
    // leave from another point), the knife replaces a shot when the bot creeps within reach, the bonus cycles now hold
    // grenade crates, and the state hash includes grenades. A lot that changes gameplay on purpose records them again and says so.
    static readonly (int m,int d,int[] h,bool inv,int ticks,int score,int hash)[] Legacy={
        (0,0,new[]{0,1,2,3},false,7076,189500,-1071455707),(1,1,new[]{2,3,-1,-1},false,2682,27100,-1461635582),(2,0,new[]{3,2,1,0},false,7156,140650,59450490),
        (2,3,new[]{1,-1,-1,-1},false,569,1100,-865923552),(0,2,new[]{0,-1,-1,-1},false,724,2000,280097480),(1,3,new[]{1,0,3,2},false,1029,23000,-682329150),
        (2,1,new[]{0,1,-1,-1},false,5613,42050,1423870097),(0,1,new[]{2,-1,3,-1},false,2532,25700,517487189),(1,0,new[]{0,1,2,3},true,5993,178950,1411155241)};
    static readonly string[] ProperNouns={"Paris","Parisien","Macron","Bastille","Seine","Eiffel","Haussmann","Marseille","Marseillaise","Mistral","Pastis","Canebière","Tokyo","Shogun","Sakura","Shibuya","Akihabara","Chuck","Norris","Arnold","Jean-Claude","JCVD","Sylvester","macron-helicopter","mecha-shogun","news-helicopter","route-0"};

    public static object Main(string captures)
    {
        var checks=new List<string>();var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        var missionsField=typeof(PanameGame).GetField("missions",Flags);var original=(MissionDefinition[])missionsField.GetValue(g);
        float scale=Time.timeScale;Time.timeScale=0;
        try
        {
            OldNumbers(g,original,checks);
            NewMission(g,original,missionsField,checks,captures);
            LegacyRuns(g,checks);
            var words=new Regex("(?<![A-Za-z0-9-])("+string.Join("|",Array.ConvertAll(ProperNouns,Regex.Escape))+")(?![A-Za-z])",RegexOptions.IgnoreCase);
            var hits=new List<string>();
            foreach(var file in Directory.GetFiles("Assets/Scripts","*.cs",SearchOption.AllDirectories))
            {var lines=File.ReadAllLines(file);for(int i=0;i<lines.Length;i++)foreach(Match m in words.Matches(lines[i]))hits.Add(Path.GetFileName(file)+":"+(i+1)+" "+m.Value);}
            Check(hits.Count==0,"Aucun nom propre à une mission dans Assets/Scripts"+(hits.Count>0?" : "+string.Join(", ",hits):""),checks);
            return string.Join("\n",checks);
        }
        finally
        {
            ContentLibrary.Reload();missionsField.SetValue(g,original);typeof(PanameGame).GetField("missionIndex",Flags).SetValue(g,0);
            PlayerPrefs.DeleteKey("action-legends.unlocked.test-data-driven");foreach(var m in original)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;}g.phase=PanameGame.Phase.Lobby;Call(g,"LoadMission",0);g.SetDifficulty(1);g.ReturnToLobby();Time.timeScale=scale;
        }
    }

    static void OldNumbers(PanameGame g,MissionDefinition[] missions,List<string> checks)
    {
        // Heroes: same order, names, speeds, fire rates, damage, jump, recoil, specials and barrel tips per frame.
        var heroes=ContentLibrary.Heroes;fails.Clear();Expect(heroes.Length==4);
        for(int i=0;i<heroes.Length&&i<4;i++)
        {
            var h=heroes[i];where=h.id;var s=h.special;
            Expect(h.name==OldHeroNames[i]&&h.speed==OldSpeed[i]&&h.fireRate==OldRate[i]&&h.damage==OldDamage[i]&&h.jump==OldJump[i]&&h.recoilShake==OldShake[i]&&h.strideRate==13&&h.bob==.045f);
            Expect(s.cooldown==5&&s.cooldownPerDifficulty==1&&h.blast==(i==0||i==2));
            if(h.blast)Expect(s.radius==(i==0?4:3.2f)&&s.damage==8&&s.bossDamage==24&&s.bossReach==1&&s.invulnerable==1&&s.leap==(i==2?12.8f:0));
            else Expect(s.shots==7&&s.speed==(i==1?26:19)&&s.spread==(i==1?.8f:2)&&s.damage==(i==1?4:5)&&s.explosive);
        }
        // Their barrel tips moved with the lot 1a sheets (one sheet per hero, Art/heroes/<id>.png): VerifyLot1a checks those.
        Check(Clean(),"Les quatre héros gardent noms, vitesses, cadences, dégâts, sauts et spéciaux",checks);
        // Enemies: the old role tables, per-difficulty projectile speeds computed with the old formulas, bit for bit.
        fails.Clear();
        foreach(var pair in OldEnemies)
        {
            var e=ContentLibrary.Enemy(pair.Key);var v=pair.Value;where=pair.Key;
            float range=e.kind==EnemyBehavior.Thrower?e.@throw.range:e.Armed?e.gun.range:0,aim=e.Armed?e.gun.aimTime:0,cooldown=e.kind==EnemyBehavior.Thrower?e.@throw.cooldown:e.Armed?e.gun.cooldown:0;
            Expect(e.health==v[0]&&e.speed==v[1]&&e.keepDistance==v[2]&&range==v[3]&&aim==v[4]&&cooldown==v[5]&&e.hitRadius==v[6]&&e.knockback==v[7]&&e.bonusScore==(int)v[8]&&e.armored==(v[8]>0));
            Expect(e.firstAttack==(e.Armed?1:.7f)&&e.attackPose==.3f&&e.strideRate==11);
            if(!e.Armed)Expect(e.melee.reach==.9f&&e.melee.height==1.35f&&e.melee.cooldown==1&&e.melee.damage==1);
            if(e.Armed)Expect(e.muzzle==OldMuzzles[pair.Key]&&e.gun.burst==(pair.Key=="gunman-smg"?3:1)&&e.gun.burstInterval==.11f&&e.gun.pose==.25f&&e.gun.pellets==(pair.Key=="gunman-shotgun"?3:1));
            for(int d=0;d<4;d++)
            {
                if(pair.Key=="looter-thrower")Expect(e.@throw.speed[d]==8+d*2);
                if(pair.Key=="gunman-pistol")Expect(e.gun.speed[d]==F(11+F(d*1.5f)));
                if(pair.Key=="gunman-smg")Expect(e.gun.speed[d]==F(12+F(d*1.4f)));
                if(pair.Key=="gunman-shotgun")Expect(e.gun.speed[d]==13+d&&e.gun.life==.45f&&e.gun.spread==3&&e.gun.spreadStep==5);
            }
        }
        Expect(ContentLibrary.Enemy("looter-thrower").hand==new Vector2(-.62f,1.9f));
        Check(Clean(),"Les six ennemis gardent PV, vitesses, distances, portées, visées, cadences, recul et vitesses de tir",checks);
        // Bosses: health, pace and every timer of the helicopter and mecha, against the old formulas.
        fails.Clear();
        foreach(var id in new[]{"macron-helicopter","le-direct","mecha-shogun"})
        {
            var b=ContentLibrary.Boss(id);bool mecha=id=="mecha-shogun";where=id;
            Expect(b.health[0]==300&&b.health[1]==560&&b.health[2]==800&&b.health[3]==1100&&b.healthPerExtraPlayer==.65f&&b.ragePace==.72f&&b.rageDelay==.35f&&b.firstAttack==(mecha?1.2f:.8f));
            for(int d=0;d<4;d++)
            {
                var gun=b.attacks[0];var bombs=b.FirstBombs;
                Expect(gun.aimTime[d]==(mecha?Mathf.Max(.45f,F(.75f-F(d*.09f))):Mathf.Max(.4f,F(.72f-F(d*.09f))))&&gun.interval[d]==F(.14f-F(d*.022f))&&gun.speed[d]==(d==0?13:F(16+F(d*1.8f)))&&gun.duration==(mecha?1.6f:1.7f));
                Expect(bombs.count[d]==2+d&&bombs.delay[d]==Mathf.Max(.3f,F(.55f-F(d*.04f)))&&bombs.flight[d]==F(.95f-F(d*.08f))&&bombs.arc==(mecha?4.5f:.3f));
                if(mecha)Expect(b.walk.speed[d]==F(2.1f+F(d*.3f))&&b.attacks[1].speed[d]==8.5f+d);else Expect(b.flight.speed[d]==F(1.2f+F(d*.16f)));
            }
        }
        Check(Clean(),"L'hélicoptère, sa variante et le mécha gardent leurs PV et tous leurs délais, bit pour bit",checks);
        // In play: health at spawn and walking speed per difficulty, hero travel per tick, damage of a shot.
        var commands=new PlayerCommand[4];var enemies=(IList)Get(g,"enemies");var players=(PanameGame.Player[])Get(g,"players");fails.Clear();
        for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;}
        for(int m=0;m<missions.Length;m++)for(int d=0;d<4;d++)
        {
            g.ReturnToLobby();Call(g,"LoadMission",m);g.SetDifficulty(d);g.BeginRound();
            for(int k=0;k<missions[m].roster.Length;k++)
            {
                var old=OldEnemies[missions[m].enemies[k]];where=missions[m].enemies[k]+" d"+d;
                enemies.Clear();Call(g,"SpawnEnemy",40f,k);var e=enemies[0];
                Expect((int)Field(e,"hp")==Mathf.CeilToInt(old[0]*HealthFactors[d]));
                players[0].invulnerable=10;float before=EnemyX(e);g.TickSimulation(1f/60,commands);
                Expect(Mathf.Abs(EnemyX(e)-(before-old[1]*SpeedFactors[d]/60))<1e-4f);
                ((Transform)Field(e,"root")).gameObject.SetActive(false);enemies.Clear();
            }
        }
        Check(Clean(),"En jeu, chaque ennemi des trois missions naît avec ses anciens PV et marche à son ancienne vitesse dans les quatre difficultés",checks);
        fails.Clear();
        for(int hero=0;hero<4;hero++)
        {
            g.slots[0].hero=hero;where="hero "+hero;g.ReturnToLobby();Call(g,"LoadMission",0);g.SetDifficulty(1);g.BeginRound();var p=players[0];
            float x=p.position.x;commands[0]=new PlayerCommand{move=1};g.TickSimulation(1f/60,commands);
            Expect(Mathf.Abs(p.position.x-x-OldSpeed[hero]/60)<1e-4f);
            commands[0]=default;for(int t=0;t<30;t++)g.TickSimulation(1f/60,commands);
            enemies.Clear();Call(g,"SpawnEnemy",p.position.x+4,2);var e=enemies[0];int hp=(int)Field(e,"hp");
            e.GetType().GetField("attackTimer").SetValue(e,99f);commands[0]=new PlayerCommand{fire=true};g.TickSimulation(1f/60,commands);commands[0]=default;
            for(int t=0;t<20&&(int)Field(e,"hp")==hp;t++)g.TickSimulation(1f/60,commands);
            Expect(hp-(int)Field(e,"hp")==OldDamage[hero]);
        }
        Check(Clean(),"En jeu, chaque héros court à son ancienne vitesse et son tir retire ses anciens dégâts",checks);
        fails.Clear();
        for(int m=0;m<missions.Length;m++)
        {
            g.slots[0].hero=0;g.ReturnToLobby();Call(g,"LoadMission",m);g.SetDifficulty(1);g.BeginRound();
            players[0].position=new Vector2(missions[m].exitX-18,0);players[0].invulnerable=100;Call(g,"UpdateCamera",1f/60);g.TickSimulation(1f/60,commands);
            where=missions[m].id;Expect(g.BossActive&&(int)Get(g,"bossMaxHealth")==Mathf.RoundToInt(560*missions[m].boss.health));
        }
        Check(Clean()&&missions[1].boss.name=="LE DIRECT"&&missions[2].boss.definition.walker,"Chaque boss garde ses PV de mission (×1, ×1.2, ×1.3) et son comportement",checks);
    }

    static void NewMission(PanameGame g,MissionDefinition[] original,FieldInfo missionsField,List<string> checks,string captures)
    {
        var sniper=ContentLibrary.AddEnemy(Sniper);var gunship=ContentLibrary.AddBoss(Gunship);var mission=MissionDefinition.Parse(Mission);
        Check(sniper.kind==EnemyBehavior.Gunman&&sniper.animations.states.Length==6&&sniper.muzzle==new Vector2(1.33f,1.54f),"Un nouvel ennemi tireur est défini en JSON avec six états d'animation sur la planche des héros",checks);
        Check(gunship.walker==false&&gunship.attacks.Length==3&&gunship.animations.sheet.texture=="Art/marseille/news-helicopter"&&gunship.rotor.width==6.5f&&gunship.flight.sweep==5.2f,"Un boss existant est re-paramétré par extends (PV, vol, trois attaques) en gardant le reste",checks);
        Check(mission.roster[1]==ContentLibrary.Enemy("looter-rusher")&&mission.roster[2]==ContentLibrary.Enemy("gunman-pistol"),"La mission réutilise des ennemis d'autres missions",checks);
        missionsField.SetValue(g,new[]{original[0],original[1],original[2],mission});
        PlayerPrefs.SetInt("action-legends.unlocked.test-data-driven",1);
        for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;}
        g.phase=PanameGame.Phase.Lobby;Call(g,"LoadMission",3);g.SetDifficulty(1);
        Check(g.CurrentMission.id=="test-data-driven"&&PanameArt.RouteSegments==3,"La carte assemble trois décors existants de trois missions",checks);
        g.BeginRound();
        var players=(PanameGame.Player[])Get(g,"players");var enemies=(IList)Get(g,"enemies");var commands=new PlayerCommand[4];
        var cells=new HashSet<int>();var set=sniper.animations;int sniperHp=-1,shotsFromSniper=0;var attacks=new List<int>();int lastPhase=0;bool captured=false;
        var shots=(IList)Get(g,"shots");
        int t=0;
        for(;t<60*60*6&&g.phase==PanameGame.Phase.Playing;t++)
        {
            var p=players[0];p.invulnerable=1;
            float nearest=float.MaxValue,dir=1;
            foreach(var e in enemies){float d=EnemyX(e)-p.position.x;if(Mathf.Abs(d)<Mathf.Abs(nearest)){nearest=d;dir=Mathf.Sign(d);}}
            float move=enemies.Count==0?1:Mathf.Abs(nearest)>5?dir:Mathf.Abs(nearest)<1.6f&&t%40<25?-dir:dir*.15f;
            if(g.BossActive&&enemies.Count==0){float d=((Vector2)Get(g,"bossPosition")).x-p.position.x;move=Mathf.Abs(d)>.8f?Mathf.Sign(d):0;}
            commands[0]=new PlayerCommand{move=move,fire=true,special=true,aimUp=g.BossActive&&move==0,jump=t%53==0};
            int before=shots.Count;
            g.TickSimulation(1f/60,commands);
            foreach(var e in enemies)
            {
                if(Field(e,"type")!=sniper)continue;
                if(sniperHp<0)sniperHp=(int)Field(e,"hp");
                var sprite=((SpriteRenderer)Field(e,"illustrated")).sprite;for(int c=0;c<16;c++)if(set.Sprite(c)==sprite)cells.Add(c);
            }
            if(g.BossActive)
            {
                int phase=(int)Get(g,"bossPhase"),attack=(int)Get(g,"bossAttack");
                if((int)Get(g,"bossVolley")>attacks.Count)attacks.Add(attack);lastPhase=phase;
                if(!captured&&((IList)Get(g,"bossBombs")).Count>0){Capture(g,captures+"/data-driven-boss.png");captured=true;}
            }
            else if(t==60*12)Capture(g,captures+"/data-driven-map.png");
        }
        Check(sniperHp==Mathf.CeilToInt(5*1.8f),"Le nouvel ennemi naît avec les PV de son JSON (5 × 1,8 en Arcade)",checks);
        Check(cells.Contains(12)&&cells.Contains(13)&&cells.Contains(14)&&cells.Contains(15),"Le nouvel ennemi joue ses propres états : attente sur deux images, course sur trois, visée, tir, choc ("+string.Join(",",cells)+")",checks);
        Check(attacks.Count>=4&&attacks[0]==0&&attacks[1]==1&&attacks[2]==2&&attacks[3]==0,"Le boss re-paramétré enchaîne ses trois attaques dans l'ordre du JSON ("+string.Join(",",attacks)+")",checks);
        Check(g.phase==PanameGame.Phase.Won,"Un bot finit la mission écrite en JSON jusqu'à la victoire ("+(t/60)+" s)",checks);
        Check((int)Get(g,"bossMaxHealth")==Mathf.RoundToInt(600*.5f),"Le boss re-paramétré a les PV de son JSON et de la mission (600 × 0,5)",checks);
    }

    static void LegacyRuns(PanameGame g,List<string> checks)
    {
        var players=(PanameGame.Player[])Get(g,"players");var enemies=(IList)Get(g,"enemies");var results=new List<string>();fails.Clear();
        foreach(var run in Legacy)
        {
            g.ReturnToLobby();Call(g,"LoadMission",run.m);g.SetDifficulty(run.d);
            for(int i=0;i<4;i++){g.slots[i].joined=run.h[i]>=0;if(run.h[i]>=0)g.slots[i].hero=run.h[i];}
            g.BeginRound();var commands=new PlayerCommand[4];int ticks=0;
            for(int t=0;t<60*60*12&&g.phase==PanameGame.Phase.Playing;t++)
            {
                if(run.inv)foreach(var p in players)if(p!=null)p.invulnerable=1;
                for(int i=0;i<4;i++)
                {
                    var p=players[i];commands[i]=default;if(p==null)continue;
                    float nearest=float.MaxValue,dir=1;
                    foreach(var e in enemies){float d=EnemyX(e)-p.position.x;if(Mathf.Abs(d)<Mathf.Abs(nearest)){nearest=d;dir=Mathf.Sign(d);}}
                    int move=enemies.Count==0?100:Mathf.Abs(nearest)>5?(int)(dir*100):Mathf.Abs(nearest)<1.6f&&t%40<25?(int)(-dir*100):(int)(dir*20);bool up=false;
                    if(g.BossActive&&enemies.Count==0){float d=((Vector2)Get(g,"bossPosition")).x-p.position.x;move=Mathf.Abs(d)>.8f?(int)(Mathf.Sign(d)*100):0;up=move==0&&t%90<60;}
                    int flags=1|(up?2:0)|(t%53==i*7?4:0)|(t%97==i*11?8:0);
                    commands[i]=new PlayerCommand{move=move/100f,fire=true,aimUp=up,jump=(flags&4)!=0,special=(flags&8)!=0};
                }
                g.TickSimulation(1f/60,commands);ticks++;
            }
            int hash=(int)Call(g,"StateHash");bool ok=ticks==run.ticks&&g.Score==run.score&&hash==run.hash;if(!ok)fails.Add("run");
            results.Add(g.CurrentMission.id+" d"+run.d+" ×"+Array.FindAll(run.h,x=>x>=0).Length+(ok?" =":" ≠ "+ticks+"/"+g.Score+"/"+hash));
        }
        Check(Clean(),"Neuf parties de bot enregistrées (lot 1a) finissent au même pas, au même score et au même état ("+string.Join(" ; ",results)+")",checks);
    }

    static void Capture(PanameGame g,string path)
    {
        Set(g,"shake",0f);g.gameCamera.aspect=16f/9;Call(g,"UpdateCamera",0f);
        var rt=new RenderTexture(1280,720,24);g.gameCamera.targetTexture=rt;g.gameCamera.aspect=16f/9;g.gameCamera.Render();
        RenderTexture.active=rt;var shot=new Texture2D(1280,720,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,1280,720),0,0);shot.Apply();
        g.gameCamera.targetTexture=null;RenderTexture.active=null;File.WriteAllBytes(path,shot.EncodeToPNG());
    }
}
