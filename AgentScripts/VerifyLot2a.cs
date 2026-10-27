using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using Paname;
// Lot 2a, hostages and the Slug mode. Hostages of Content/hostages are freed by a shot or a touch as their data says, play
// tied, thanks, give and run, drop their reward (a weapon, grenades, points) and run off the screen, never before giving it
// in a locked wave; each hero counts his own until he falls; the end-of-mission tally pays them; a hostage written only as
// JSON works. Slug mode: one hit kills, lives per hero, a respawn on the spot, invulnerable and with the hero's own weapon,
// CONTINUE? when the team is out, continuing keeps the mission, refusing or waiting loses it. Replays and online frames with
// a continue are exact; bots win the three missions in every difficulty, and one finishes a mission in Slug mode.
public static class VerifyLot2a
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static void Set(object o,string name,object v)=>o.GetType().GetField(name,Flags).SetValue(o,v);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags|BindingFlags.Static).Invoke(o,args);
    static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
    static void SetField(object o,string name,object v)=>o.GetType().GetField(name).SetValue(o,v);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    static Vector2 Pos(object o)=>(Vector2)Field(o,"position");
    static int State(object h)=>(int)Field(h,"state");

    static PanameGame g;static PanameGame.Player[] players;static IList enemies,shots,pickups,hostages;
    static readonly PlayerCommand[] commands=new PlayerCommand[4];
    const int Slug=4;
    static void Start(int mission,int difficulty,int heroes,int first=0)
    {
        g.ReturnToLobby();Call(g,"LoadMission",mission);g.SetDifficulty(difficulty);
        for(int i=0;i<4;i++){g.slots[i].joined=i<heroes;g.slots[i].hero=(first+i)%4;}
        g.BeginRound();players=(PanameGame.Player[])Get(g,"players");enemies=(IList)Get(g,"enemies");shots=(IList)Get(g,"shots");pickups=(IList)Get(g,"pickups");hostages=(IList)Get(g,"hostages");
        for(int i=0;i<4;i++)commands[i]=default;
    }
    static void Tick(int n=1,bool invulnerable=true){for(int t=0;t<n;t++){if(invulnerable)foreach(var p in players)if(p!=null&&p.hp>0)p.invulnerable=1;g.TickSimulation(1f/60,commands);}}
    static void ClearEnemies(){foreach(var e in enemies)((Transform)Field(e,"root")).gameObject.SetActive(false);enemies.Clear();}
    static void ClearHostages(){foreach(var h in hostages)((Transform)Field(h,"root")).gameObject.SetActive(false);hostages.Clear();}
    static void ClearPickups(){foreach(var k in pickups)((Transform)Field(k,"root")).gameObject.SetActive(false);pickups.Clear();}
    static void NoWaves()=>typeof(PanameGame).GetProperty("Wave").SetValue(g,g.CurrentMission.waves.Length);
    static void Quiet(){ClearEnemies();ClearHostages();ClearPickups();NoWaves();}
    static object Hostage(string id,float x,string reward=null,string weapon=null)
    {
        var place=new MapHostage{id=id,x=x,reward=reward,weapon=weapon};place.definition=ContentLibrary.Hostage(id);
        place.bonus=reward!=null||weapon!=null?HostageDefinition.BonusIndex(g.CurrentMission.ruleSet,reward,weapon):-1;place.gun=weapon!=null?ContentLibrary.FindWeapon(weapon):null;
        Call(g,"SpawnHostage",place);return hostages[hostages.Count-1];
    }
    // Ticks until the hostage reaches a state (or leaves), at most n ticks; returns the ticks spent.
    static int Until(object h,int state,int n=600){int t=0;while(t<n&&hostages.Contains(h)&&State(h)!=state){Tick(1);t++;}return t;}
    static void Walk(PanameGame.Player p,float x){for(int t=0;t<60*6&&Mathf.Abs(p.position.x-x)>.15f;t++){commands[0]=new PlayerCommand{move=Mathf.Clamp((x-p.position.x)*2,-1,1)};Tick(1);}commands[0]=default;}
    // Touches a hostage, then steps back so that his reward does not land in the hero's hands at once.
    static void Touch(PanameGame.Player p,object h){float x=Pos(h).x;Walk(p,x);Walk(p,x-3);}
    static void Fire(int settle=10){commands[0]=new PlayerCommand{fire=true};Tick(1);commands[0]=default;Tick(settle);}

    public static object Main(string captures)
    {
        var checks=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        float scale=Time.timeScale;Time.timeScale=0;
        var missions=(MissionDefinition[])Get(g,"missions");
        try
        {
            Library(checks,missions);Release(checks);Rewards(checks);JsonOnly(checks);Counter(checks);WaveHostage(checks);Tally(checks);
            SlugRules(checks);Continues(checks);Replays(checks,missions);Bots(checks);Captures(checks,captures);
        }
        finally
        {
            ContentLibrary.Reload();
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;}
            foreach(var m in missions)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
            g.ReturnToLobby();Call(g,"LoadMission",0);g.SetDifficulty(1);g.ReturnToLobby();Time.timeScale=scale;
        }
        return string.Join("\n",checks);
    }

    static void Library(List<string> checks,MissionDefinition[] missions)
    {
        var ids=new[]{"civilian","baker","boules-player","office-worker"};bool sheets=true;
        foreach(var id in ids){var h=ContentLibrary.Hostage(id);sheets&=h.animations.Texture!=null&&h.animations.CellCount==12&&h.rewards.Length==4&&h.byShot&&h.byTouch&&h.clip==null;}
        Check(sheets,"Quatre otages dans Content/hostages (un générique, une variante par ville par extends), planches de 12 images, libérables par tir et par contact",checks);
        var lines=new List<string>();bool placed=true;
        foreach(var m in missions)
        {
            var list=m.map.hostages;int waves=Array.FindAll(list,h=>h.wave>=0).Length;placed&=list.Length>=6&&list.Length<=10&&waves>=2&&m.ruleSet.id=="pow";
            float first=list[0].x,last=list[list.Length-1].x;placed&=first<m.length*.15f&&last>m.length*.8f;
            lines.Add(m.id+" "+list.Length+" otages dont "+waves+" dans des vagues, de x "+first+" à "+last);
        }
        Check(placed,"Chaque mission place 6 à 10 otages répartis sur tout le parcours, dont certains dans des vagues bloquées ("+string.Join(" ; ",lines)+")",checks);
        var pow=ContentLibrary.Rules("pow");var def=ContentLibrary.Rules("default");bool fewer=true;for(int d=0;d<4;d++)fewer&=pow.supplySpacing[d]>def.supplySpacing[d]*1.8f;
        Check(fewer&&pow.bonuses.Length==def.bonuses.Length,"Les missions jouent les règles pow : moitié moins de caisses sur le parcours que default ("+string.Join("/",pow.supplySpacing)+" u entre deux caisses au lieu de "+string.Join("/",def.supplySpacing)+")",checks);
    }

    // A shot frees a hostage, who plays his four states, drops his reward, then runs off the screen.
    static void Release(List<string> checks)
    {
        Start(0,1,1);var p=players[0];Quiet();Tick(5);float hx=p.position.x+6;
        var h=Hostage("civilian",hx,"grenades");var type=(HostageDefinition)Field(h,"type");var set=type.animations;
        var seen=new Dictionary<int,HashSet<Sprite>>();for(int s=0;s<4;s++)seen[s]=new HashSet<Sprite>();
        var renderer=(SpriteRenderer)Field(h,"illustrated");
        Tick(40);seen[0].Add(renderer.sprite);
        p.fireTimer=0;commands[0]=new PlayerCommand{fire=true};Tick(1);commands[0]=default;int waited=0;while(State(h)==0&&waited<40){Tick(1);waited++;}
        Check(State(h)==1&&Field(h,"rescuer")==p&&p.rescued==1&&g.HostagesFreed==1,"Un tir sur ses liens libère l'otage ; le héros qui a tiré le compte (×"+p.rescued+")",checks);
        int before=pickups.Count;float bornAt=0;int ticks=0;var order=new List<int>{0};
        while(hostages.Contains(h)&&ticks<60*8){int s=State(h);if(order[order.Count-1]!=s)order.Add(s);seen[s].Add(renderer.sprite);if(pickups.Count>before&&bornAt==0)bornAt=ticks/60f;Tick(1);ticks++;}
        bool states=string.Join(",",order)=="0,1,2,3";
        foreach(var (state,name) in new[]{(0,"tied"),(1,"thanks"),(2,"give"),(3,"run")})
        {var cells=new HashSet<Sprite>();foreach(int c in set.states[set.Find(name)].cells)cells.Add(set.Sprite(c));states&=seen[state].Count>0&&cells.IsSupersetOf(seen[state]);if(state>0)states&=seen[state].Count>=2;}
        Check(states,"Il joue attaché, remerciement (salut), lâcher de l'objet puis fuite, chaque état avec les images de son JSON ("+string.Join(" > ",order)+")",checks);
        var crate=pickups.Count>before?pickups[pickups.Count-1]:null;
        Check(crate!=null&&Mathf.Abs(Pos(crate).x-(hx-.9f))<.01f&&Mathf.Abs(bornAt-(type.thanks+type.giveAt))<.05f,"Il lâche sa récompense devant lui, vers le héros, "+bornAt.ToString("0.00")+" s après sa libération (thanks + giveAt)",checks);
        Check(!hostages.Contains(h)&&ticks<60*5,"Puis il s'enfuit vers la gauche et disparaît hors de l'écran ("+(ticks/60f).ToString("0.0")+" s)",checks);
        p.grenades=0;Walk(p,Pos(crate).x);Tick(2);
        Check(p.grenades==ContentLibrary.Rules("pow").bonuses[4].amount[1],"Sa récompense « grenades » se ramasse : "+p.grenades+" grenades",checks);
        // A hostage beyond the screen cannot be freed, even by a laser that reaches him.
        Quiet();var far=Hostage("civilian",p.position.x+24);g.GiveWeapon(0,"laser");p.fireTimer=0;Fire(20);
        Check(State(far)==0,"Un otage hors de l'écran ne se libère pas, même au laser (portée "+ContentLibrary.Weapon("laser").projectile.range+" u)",checks);
        // A grenade's explosion cuts the ropes too; touching him is enough.
        Quiet();var near=Hostage("civilian",p.position.x+6);commands[0]=new PlayerCommand{grenade=true};Tick(1);commands[0]=default;int t2=0;while(State(near)==0&&t2<120){Tick(1);t2++;}
        Check(State(near)==1,"L'explosion d'une grenade libère un otage ("+(t2/60f).ToString("0.00")+" s après le lancer)",checks);
        Quiet();var touched=Hostage("civilian",p.position.x+3);Walk(p,p.position.x+3);
        Check(State(touched)>0&&Field(touched,"rescuer")==p,"Un otage se libère aussi au contact du héros",checks);
    }

    static void Rewards(List<string> checks)
    {
        // A weapon named by the placement: a crate holding it, the same path as GiveWeapon (Arm).
        Start(1,1,1);var p=players[0];Quiet();Tick(5);var h=Hostage("boules-player",p.position.x+3,null,"rocket-launcher");Touch(p,h);
        Until(h,3);Tick(2);var crate=pickups[pickups.Count-1];
        bool shown=((Transform)Field(crate,"root")).GetComponent<SpriteRenderer>().sprite==ContentLibrary.Weapon("rocket-launcher").crate.Sprite;
        Walk(p,Pos(crate).x);Tick(2);
        Check(shown&&p.weapon.id=="rocket-launcher"&&p.ammo==p.weapon.ammo[1],"Une récompense d'arme (map.hostages weapon) tombe en caisse de cette arme ; le héros la ramasse avec "+p.ammo+" munitions",checks);
        Quiet();int score=g.Score;h=Hostage("boules-player",p.position.x+3,"score");Touch(p,h);Until(h,3);Tick(2);crate=pickups[pickups.Count-1];Walk(p,Pos(crate).x);Tick(2);
        Check(g.Score-score==1500,"Une récompense « score » rapporte ses points (+"+(g.Score-score)+")",checks);
        // Without a reward of its own, the hostage's weighted table: weapon 2, grenades 2, health 1, score 1, spread out.
        Quiet();var drawn=new List<string>();var rules=g.CurrentMission.ruleSet;
        for(int i=0;i<12;i++){var q=Hostage("civilian",p.position.x+4);Call(g,"GiveHostageReward",q);drawn.Add(rules.bonuses[(int)Field(pickups[pickups.Count-1],"kind")].id[0].ToString());ClearPickups();}
        string seq=string.Join("",drawn);int w=seq.Split('w').Length-1,gr=seq.Split('g').Length-1,he=seq.Split('h').Length-1,sc=seq.Split('s').Length-1;
        Check(w==4&&gr==4&&he==2&&sc==2&&!seq.Contains("ww")&&!seq.Contains("gg"),"Sans récompense imposée, la table pondérée de l'otage tourne de façon déterministe ("+seq+")",checks);
    }

    // Hostages written only as JSON here: one freed by touch only, with another sheet; a tough one that takes three shots.
    const string TouchOnly=@"{""id"": ""test-touch"", ""extends"": ""civilian"", ""name"": ""TOUCHE"", ""release"": ""touch"", ""thanks"": 0.5, ""give"": 0.4, ""giveAt"": 0.2,
      ""run"": {""speed"": 12, ""direction"": 1}, ""rewards"": [{""bonus"": ""score"", ""weight"": 1}],
      ""animations"": {""sheet"": {""texture"": ""Art/hostages/office-worker""}, ""states"": [{""name"": ""tied"", ""cells"": [1]}, {""name"": ""thanks"", ""cells"": [3], ""fps"": 2},
        {""name"": ""give"", ""cells"": [7]}, {""name"": ""run"", ""cells"": [8, 10], ""fps"": 8}]}}";
    const string Tough=@"{""id"": ""test-tough"", ""extends"": ""civilian"", ""name"": ""COSTAUD"", ""release"": ""shot"", ""hits"": 3}";
    static void JsonOnly(List<string> checks)
    {
        var touch=ContentLibrary.AddHostage(TouchOnly);var tough=ContentLibrary.AddHostage(Tough);
        Start(2,1,1);var p=players[0];Quiet();Tick(5);var h=Hostage("test-touch",p.position.x+5);p.fireTimer=0;
        commands[0]=new PlayerCommand{fire=true};Tick(40);commands[0]=default;bool shotIgnored=State(h)==0;
        float hx=Pos(h).x;Touch(p,h);bool freed=State(h)>0;int score=g.Score;
        Until(h,3);bool cell=((SpriteRenderer)Field(h,"illustrated")).sprite==touch.animations.Sprite(8)||((SpriteRenderer)Field(h,"illustrated")).sprite==touch.animations.Sprite(10);
        Tick(10);bool right=Pos(h).x>hx+1;var crate=pickups[pickups.Count-1];Walk(p,Pos(crate).x);Tick(2);
        Check(shotIgnored&&freed&&cell&&right&&g.Score-score==1500,"Un otage écrit seulement en JSON (test-touch : contact seul, autre planche, quatre états, fuite à droite, récompense score) fonctionne sans code",checks);
        Quiet();var t=Hostage("test-tough",p.position.x+5);int shotsTaken=0;
        for(int i=0;i<6&&State(t)==0;i++){p.fireTimer=0;Fire(20);shotsTaken++;}
        Walk(p,p.position.x+5);
        Check(shotsTaken==3&&(int)Field(t,"hits")==3,"Un otage à trois liens (hits 3, tir seul) ne se libère qu'au troisième tir, pas au contact",checks);
    }

    // Each hero counts his own hostages and loses them when he falls.
    static void Counter(List<string> checks)
    {
        Start(0,1,2);var a=players[0];var b=players[1];Quiet();Tick(5);
        var h1=Hostage("baker",a.position.x+.2f);Tick(2);var h2=Hostage("baker",b.position.x+.2f);Tick(2);var h3=Hostage("baker",b.position.x+.4f);Tick(2);
        Check(a.rescued+b.rescued==3&&g.HostagesFreed==3,"Les otages touchés sont comptés par joueur (J1 ×"+a.rescued+", J2 ×"+b.rescued+")",checks);
        int keep=b.rescued;a.invulnerable=0;Call(g,"Damage",a,99);
        Check(a.hp<=0&&a.rescued==0&&b.rescued==keep,"À la mort, le joueur perd son compteur d'otages ; l'autre garde le sien",checks);
        int before=(int)Call(g,"StateHash");b.rescued++;Check((int)Call(g,"StateHash")!=before,"Le compteur d'otages entre dans l'empreinte d'état comparée en ligne",checks);
    }

    // A wave's hostage appears with its wave, inside the locked screen, and gives his reward before running off.
    static void WaveHostage(List<string> checks)
    {
        Start(0,1,1);var p=players[0];var m=g.CurrentMission;var place=Array.Find(m.map.hostages,x=>x.wave==1);
        int start=hostages.Count,loose=Array.FindAll(m.map.hostages,x=>x.wave<0).Length;
        ClearEnemies();typeof(PanameGame).GetProperty("Wave").SetValue(g,1);p.position=new Vector2(m.waves[1].atX-8.5f,0);Tick(2);
        bool locked=(bool)Get(g,"screenLocked");float lockX=(float)Get(g,"lockX");object h=null;foreach(var o in hostages)if(Field(o,"place")==place)h=o;
        Check(start==loose&&h!=null&&locked&&Mathf.Abs(place.x-lockX)<10,"L'otage de la vague 2 n'apparaît qu'avec elle, dans l'écran bloqué ("+start+" otages au départ, celui-ci à x "+place.x+", écran centré en "+lockX.ToString("0.0")+")",checks);
        Call(g,"FreeHostage",h,p);bool gaveFirst=true;int t=0;
        while(hostages.Contains(h)&&t<60*8){Tick(1);t++;if(!hostages.Contains(h))break;if(!(bool)Field(h,"given")&&Mathf.Abs(Pos(h).x-lockX)>10.3f)gaveFirst=false;}
        var crate=pickups.Count>0?pickups[pickups.Count-1]:null;
        Check(gaveFirst&&crate!=null&&Mathf.Abs(Pos(crate).x-lockX)<10&&(bool)Get(g,"screenLocked"),"Dans la vague bloquée, il donne sa récompense dans l'écran avant de fuir hors de l'écran",checks);
    }

    static void Tally(List<string> checks)
    {
        Start(0,1,2);Quiet();players[0].rescued=3;players[1].rescued=1;typeof(PanameGame).GetProperty("SimulationTime").SetValue(g,200f);int score=g.Score;
        var rules=g.CurrentMission.ruleSet;int each=rules.tally.hostage[1],second=rules.tally.perSecond[1];float par=g.CurrentMission.parTime;
        Call(g,"WinMission");int expected=4*each+Mathf.FloorToInt(par-200)*second;
        Check(g.phase==PanameGame.Phase.Won&&g.Score-score==expected&&g.TallyHostages[0]==3&&g.TallyHostages[1]==1&&g.TallyHostagePoints==4*each&&g.TallyTime==(int)(par-200),
            "Le décompte de fin de mission paie 4 otages × "+each+" et "+(int)(par-200)+" s sous le temps de référence × "+second+" : +"+(g.Score-score)+" points",checks);
        g.NextMission();Check(g.phase==PanameGame.Phase.Playing&&g.MissionIndex==1&&g.Score==score+expected,"Le décompte ne bloque pas la mission suivante, qui garde le score",checks);
    }

    static void SlugRules(List<string> checks)
    {
        var slug=PanameGame.Difficulties[Slug];
        Check(PanameGame.DifficultyCount==5&&slug.id=="slug"&&slug.name=="SLUG"&&slug.health==1&&slug.livesPerPlayer&&slug.continues.enabled&&slug.column==1,"Le mode SLUG est une cinquième difficulté de Content/difficulties (valeurs d'Arcade, 1 PV, vies par joueur, continues)",checks);
        Start(0,Slug,1);var p=players[0];Quiet();p.position=new Vector2(20,0);Tick(130,false);g.GiveWeapon(0,"laser");p.rescued=2;
        Check(p.hp==1&&p.lives==2,"En SLUG, le héros a "+p.hp+" PV et "+p.lives+" vies de réserve",checks);
        float x=p.position.x;p.invulnerable=0;Call(g,"Damage",p,1);
        Check(p.hp<=0&&p.rescued==0,"Un seul coup le tue (et il perd ses otages)",checks);
        int t=0;while(p.hp<=0&&t<600){Tick(1,false);t++;}
        bool blink=false;for(int i=0;i<30;i++){Tick(1,false);foreach(var r in p.renderers)if(r.color.a<1)blink=true;}
        Check(p.hp==1&&p.lives==1&&Mathf.Abs(p.position.x-x)<.01f&&p.invulnerable>0&&blink&&p.weapon==p.Definition.Weapon&&Mathf.Abs(t/60f-slug.respawn.delay)<.05f,
            "Il perd une vie et réapparaît sur place "+(t/60f).ToString("0.0")+" s plus tard, invulnérable et clignotant, avec son arme de base",checks);
        // In the other difficulties, the team still shares its lives.
        Start(0,1,2);Check(g.Reserves==4&&players[0].lives==0&&g.GetPlayerHealth(0)==5,"Les autres difficultés gardent les renforts partagés par l'équipe (Arcade ×2 : "+g.Reserves+")",checks);
    }

    static void DieOut(PanameGame.Player p){p.lives=0;p.invulnerable=0;Call(g,"Damage",p,1);int t=0;while(!g.ContinueOffered&&g.phase==PanameGame.Phase.Playing&&t<600){Tick(1,false);t++;}}
    static void Continues(List<string> checks)
    {
        Start(0,Slug,1);var p=players[0];Quiet();p.position=new Vector2(30,0);Tick(5,false);int score=g.Score,wave=g.Wave;float x=p.position.x;
        DieOut(p);float clock=g.SimulationTime;
        Check(g.ContinueOffered&&g.phase==PanameGame.Phase.Playing&&Mathf.Abs(g.ContinueSeconds-10)<.02f,"Sans vie, l'équipe voit CONTINUE ? et son compte à rebours de 10 s (la mission reste en cours)",checks);
        commands[0]=new PlayerCommand{jump=true,fire=true};Tick(1,false);commands[0]=new PlayerCommand{fire=true};Tick(30,false);commands[0]=default;
        Check(g.ContinueOffered&&g.SimulationTime==clock,"Pendant le délai de grâce, un saut ou un tir déjà enfoncé ne continue pas ; la mission est figée",checks);
        Tick(40,false);commands[0]=new PlayerCommand{jump=true};Tick(1,false);commands[0]=default;
        Check(!g.ContinueOffered&&g.Continues==1&&p.hp==1&&p.lives==2&&Mathf.Abs(p.position.x-x)<.01f&&g.Wave==wave&&g.Score==score&&g.phase==PanameGame.Phase.Playing,
            "Continuer (saut) reprend la mission où elle en était : héros sur place avec 2 vies, même vague, même score, 1 continue compté",checks);
        DieOut(p);Tick(70,false);commands[0]=new PlayerCommand{grenade=true};Tick(1,false);commands[0]=default;
        Check(g.phase==PanameGame.Phase.Lost&&g.Continues==1,"Refuser (grenade) mène à la défaite",checks);
        Start(0,Slug,1);p=players[0];Quiet();Tick(5,false);DieOut(p);int t=0;while(g.phase==PanameGame.Phase.Playing&&t<60*12){Tick(1,false);t++;}
        Check(g.phase==PanameGame.Phase.Lost&&Mathf.Abs(t/60f-10)<.1f,"Laisser filer le compte à rebours mène à la défaite ("+(t/60f).ToString("0.0")+" s)",checks);
        // Two heroes: one out of lives waits while the other plays; CONTINUE? comes when both are out, and brings both back.
        Start(0,Slug,2);var a=players[0];var b=players[1];Quiet();Tick(5,false);a.lives=0;a.invulnerable=0;Call(g,"Damage",a,1);Tick(120,false);
        bool waiting=!g.ContinueOffered&&a.hp<=0&&b.hp>0;DieOut(b);bool offered=g.ContinueOffered;Tick(70,false);commands[1]=new PlayerCommand{jump=true};Tick(1,false);commands[1]=default;
        Check(waiting&&offered&&a.hp==1&&b.hp==1&&a.lives==2&&b.lives==2,"À deux, le joueur sans vie attend ; CONTINUE ? vient quand toute l'équipe est à court, et n'importe quel joueur relance les deux",checks);
    }

    // A recorded run, its flags as the replay keeps them, replayed in the hidden verifier and through online frames.
    // giveUp: after that many continues, the bot answers CONTINUE? with a grenade, so the run ends.
    static ReplayRecord Record(int mission,int difficulty,int heroes,int limit,int giveUp,out List<string> frames)
    {
        Start(mission,difficulty,heroes);var moves=new int[4];var flags=new int[4];var text=new StringBuilder();frames=new List<string>();
        for(int t=0;t<limit&&g.phase==PanameGame.Phase.Playing;t++)
        {
            for(int i=0;i<4;i++){moves[i]=0;flags[i]=0;var q=players[i];if(q==null)continue;var c=Bot(q,i,t);if(g.ContinueOffered&&g.Continues>=giveUp)c=new PlayerCommand{grenade=t%30==0};moves[i]=Mathf.RoundToInt(c.move*10)*10;flags[i]=(c.fire?1:0)|(c.aimUp?2:0)|(c.jump?4:0)|(c.special?8:0)|(c.down?16:0)|(c.grenade?32:0);commands[i]=(PlayerCommand)Call(g,"Command",moves[i],flags[i]);}
            // The host's online message for this tick: departures, then move and flags of the four slots.
            text.Clear().Append(0);for(int i=0;i<4;i++)text.Append(' ').Append(moves[i]).Append(' ').Append(flags[i]);frames.Add(text.ToString());
            Call(g,"RecordTick",0,moves,flags);g.TickSimulation(1f/60,commands);Call(g,"EndReplayTick");
        }
        return g.LastReplay;
    }
    static bool Verify(MissionDefinition[] missions,ReplayRecord record)
    {
        var verifier=PanameGame.CreateVerifier(missions);bool ok=record!=null&&verifier.BeginVerify(JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record)));bool? result=null;
        while(ok&&result==null)result=verifier.StepVerify(1e6f);UnityEngine.Object.DestroyImmediate(verifier.gameObject);return ok&&result==true;
    }
    // The same ticks sent as the host's "F" messages, split in packets of ten, parsed and run by another game.
    static bool Online(MissionDefinition[] missions,ReplayRecord record,List<string> frames)
    {
        var guest=PanameGame.CreateVerifier(missions);guest.BeginVerify(JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record)));
        var queue=Get(guest,"netFrames");
        for(int i=0;i<frames.Count;i+=10)
        {
            Call(guest,"ParseFrames",string.Join("/",frames.GetRange(i,Mathf.Min(10,frames.Count-i))));
            while((int)queue.GetType().GetProperty("Count").GetValue(queue)>0)Call(guest,"RunFrame",queue.GetType().GetMethod("Dequeue").Invoke(queue,null));
        }
        bool ok=guest.Score==record.score&&(int)Call(guest,"StateHash")==record.hash&&guest.Continues==record.continues&&(guest.phase==PanameGame.Phase.Won)==record.won&&guest.phase!=PanameGame.Phase.Playing;
        UnityEngine.Object.DestroyImmediate(guest.gameObject);return ok;
    }
    static void Replays(List<string> checks,MissionDefinition[] missions)
    {
        var hostage=Record(0,0,2,60*60*8,0,out var frames);foreach(var m in missions)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
        Check(Verify(missions,hostage)&&Online(missions,hostage,frames)&&g.HostagesFreed>=4,"Une partie à deux qui libère "+g.HostagesFreed+" otages se rejoue à l'identique (replay et trames en ligne) : "+(hostage.won?"victoire":"défaite")+", "+hostage.ticks+" pas",checks);
        // Slug, not invulnerable: the bot dies, continues by jumping, and the run replays with its continues.
        var slug=Record(2,Slug,1,60*60*20,2,out frames);foreach(var m in missions)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
        bool replayed=Verify(missions,slug),online=Online(missions,slug,frames);
        Check(slug.continues>=1&&slug.difficulty==Slug&&replayed&&online,"Une partie en SLUG avec "+slug.continues+" continues se rejoue à l'identique, en replay comme en trames en ligne ("+(slug.won?"victoire":"défaite")+", "+slug.ticks+" pas)",checks);
        var forged=JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(slug));forged.continues=0;
        Check(!Verify(missions,forged),"Un replay qui cache ses continues est refusé",checks);
        Set(g,"publish",Enum.ToObject(typeof(PanameGame).GetNestedType("Publish",BindingFlags.NonPublic),0));Call(g,"OnReplayFinished",slug);
        Check(Get(g,"publish").ToString()=="Continued","Une partie continuée n'est pas publiable au classement (seules les parties sans continue sont classées)",checks);
    }

    // VerifyLot1b's bot (walks to the nearest enemy, crouches, stabs, throws grenades, shoots down, stands under the boss).
    // It fires all the time, so it frees the hostages in its way, and its jumps answer CONTINUE?.
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
        for(int d=0;d<4;d++)for(int m=0;m<3;m++)
        {
            Start(m,d,1);int t=0;for(;t<60*60*20&&g.phase==PanameGame.Phase.Playing;t++){commands[0]=Bot(players[0],0,t);Tick(1);}
            if(g.phase!=PanameGame.Phase.Won)throw new Exception("ÉCHEC : bot de "+g.CurrentMission.id+" en "+PanameGame.DifficultyNames[d]);
            lines.Add(PanameGame.DifficultyNames[d]+" "+g.CurrentMission.id.Split('-')[0]+" "+t/60+" s "+g.HostagesFreed+" otages");
        }
        Check(lines.Count==12,"Un bot invulnérable gagne les trois missions dans les quatre difficultés ("+string.Join(", ",lines)+")",checks);
        // Slug, vulnerable: one hit kills, the bot continues as often as it needs.
        Start(2,Slug,1);int s=0;for(;s<60*60*30&&g.phase==PanameGame.Phase.Playing;s++){commands[0]=Bot(players[0],0,s);Tick(1,false);}
        Check(g.phase==PanameGame.Phase.Won,"Un bot sans invulnérabilité termine "+g.CurrentMission.title+" en SLUG en "+s/60+" s avec "+g.Continues+" continues",checks);
    }

    // In the street: each mission's hostage tied, thanking, giving and running, with a hero.
    static void Captures(List<string> checks,string captures)
    {
        int taken=0;
        foreach(var (m,id) in new[]{(0,"baker"),(1,"boules-player"),(2,"office-worker"),(0,"civilian")})
        {
            Start(m,1,1);var p=players[0];Quiet();p.position=new Vector2(14,0);Tick(3);
            var tied=Hostage(id,17.5f);var thanks=Hostage(id,20.5f);var give=Hostage(id,23.5f);var run=Hostage(id,26.5f);
            Pose(thanks,1,.5f);Pose(give,2,.6f);Call(g,"GiveHostageReward",give);Pose(run,3,.2f);Tick(1);
            Set(g,"cameraX",p.position.x+7);Capture(captures+"/lot2a-"+id+".png");taken++;
        }
        Check(taken==4,"Captures : chaque otage attaché, qui salue, qui donne et qui s'enfuit (lot2a-<otage>.png)",checks);
    }
    // A hostage shown in a state at a time of its animation.
    static void Pose(object h,int state,float time)
    {
        SetField(h,"state",state);SetField(h,"time",-999f);SetField(h,"facing",state==3?-1:-1);Call(g,"ShowHostage",h,0f);SetField(h,"animTime",time);Call(g,"ShowHostage",h,0f);
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
