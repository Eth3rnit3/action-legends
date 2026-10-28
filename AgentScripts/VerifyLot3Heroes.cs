using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Paname;
// Lot 3H, any number of heroes and three new ones. The menu has four cards, one per player, and each scrolls through every
// hero with the mouse (the card's arrows), the keyboard (J1) and a pad, also with twelve heroes; several players take the same.
// The new specials (summon: Fernandel whistles for Marguerite the cow; dash: Jeanne charges with her banner; salvo: Brigitte
// fires both revolvers both ways) hit, hurt each enemy once, the boss once, last their pose and wait for their cooldown; a hero
// written only as JSON with a new special plays it. The three heroes show every state on the street (captures). Replays and
// online frames with the new heroes are exact, and replays find their heroes by id. Bots win with each of the seven heroes,
// and every mission is won with a new hero.
public static class VerifyLot3Heroes
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

    static PanameGame g;static PanameGame.Player[] players;static IList enemies,shots,hostages,pickups,arrivals,carriers,thrown,rushes;
    static readonly PlayerCommand[] commands=new PlayerCommand[4];
    static int H(string id){int i=ContentLibrary.HeroIndex(id);if(i<0)throw new Exception("héros absent : "+id);return i;}
    static int M(string id){var all=(MissionDefinition[])Get(g,"missions");int i=Array.FindIndex(all,x=>x.id==id);if(i<0)throw new Exception("mission absente : "+id);return i;}
    static readonly string[] Missions={"panique-a-paname","mistral-gagnant","prophetie-en-provence","tokyo-neon"};
    static void Start(int mission,int difficulty,params int[] heroes)
    {
        g.ReturnToLobby();Call(g,"LoadMission",mission);g.SetDifficulty(difficulty);
        for(int i=0;i<4;i++){g.slots[i].joined=i<heroes.Length;g.slots[i].pad=null;if(i<heroes.Length)g.slots[i].hero=heroes[i];}
        g.BeginRound();players=(PanameGame.Player[])Get(g,"players");enemies=(IList)Get(g,"enemies");shots=(IList)Get(g,"shots");thrown=(IList)Get(g,"thrown");
        hostages=(IList)Get(g,"hostages");pickups=(IList)Get(g,"pickups");arrivals=(IList)Get(g,"arrivals");carriers=(IList)Get(g,"carriers");rushes=(IList)Get(g,"rushes");
        for(int i=0;i<4;i++)commands[i]=default;
    }
    static void Tick(int n=1,bool invulnerable=true){for(int t=0;t<n;t++){if(invulnerable)foreach(var p in players)if(p!=null&&p.hp>0)p.invulnerable=1;g.TickSimulation(1f/60,commands);}}
    static void Clear(IList list){foreach(var o in list){var r=o.GetType().GetField("root")?.GetValue(o) as Transform;if(r)r.gameObject.SetActive(false);}list.Clear();}
    static void Quiet()
    {
        Clear(enemies);Clear(hostages);Clear(pickups);arrivals.Clear();Clear(carriers);Clear(thrown);
        // Nor the props of lot 3a (a barrel among the dummies would blow up under the charge).
        Clear((IList)Get(g,"breakables"));Call(g,"BuildTerrain");
        foreach(var shot in shots){SetField(shot,"active",false);((Transform)Field(shot,"root")).gameObject.SetActive(false);}shots.Clear();
        typeof(PanameGame).GetProperty("Wave").SetValue(g,g.CurrentMission.waves.Length);
        var started=(bool[])Get(g,"encounterStarted");for(int i=0;i<started.Length;i++)started[i]=true;
        Set(g,"screenLocked",false);Set(g,"encounterLock",false);
    }
    static void Stand(PanameGame.Player p,float x){p.position=new Vector2(x,0);p.facing=1;Set(g,"cameraX",x+2.5f);Tick(2);}
    // A sturdy enemy that neither walks nor attacks: what the specials do to it can be counted.
    static object Dummy(float x,int hp=200)
    {
        var roster=g.CurrentMission.roster;int kind=Array.FindIndex(roster,e=>!e.Armed);if(kind<0)kind=0;
        Call(g,"SpawnEnemy",x,kind);var e=enemies[enemies.Count-1];SetField(e,"hp",hp);SetField(e,"stagger",9999f);SetField(e,"attackTimer",9999f);SetField(e,"grenadeTimer",9999f);
        return e;
    }

    public static object Main(string captures)
    {
        var checks=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        float scale=Time.timeScale;Time.timeScale=0;
        try
        {
            // LOT3_ONLY=Menu,Dash runs some parts only (while working on them).
            var only=Environment.GetEnvironmentVariable("LOT3_ONLY");bool Run(string part)=>string.IsNullOrEmpty(only)||Array.IndexOf(only.Split(','),part)>=0;
            Library(checks);if(Run("Menu"))Menu(checks);if(Run("Summon"))Summon(checks,captures);if(Run("Dash"))Dash(checks,captures);if(Run("Salvo"))Salvo(checks,captures);
            if(Run("JsonOnly"))JsonOnly(checks);if(Run("Bosses"))Bosses(checks);if(Run("Poses"))Poses(checks,captures);if(Run("Replays"))Replays(checks);if(Run("Bots"))Bots(checks);
            if(!string.IsNullOrEmpty(only))checks.Add("(parties : "+only+")");
        }
        finally
        {
            ContentLibrary.Reload();
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;g.slots[i].pad=null;}
            foreach(var m in (MissionDefinition[])Get(g,"missions"))PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
            g.ReturnToLobby();Call(g,"LoadMission",0);g.SetDifficulty(1);g.ReturnToLobby();Time.timeScale=scale;
        }
        return string.Join("\n",checks);
    }

    static void Library(List<string> checks)
    {
        var heroes=PanameGame.Heroes;var ids=Array.ConvertAll(heroes,h=>h.id);
        Check(heroes.Length==7&&string.Join(",",ids)=="chuck-norris,arnold,jean-claude,sylvester,fernandel,brigitte-bardot,jeanne-d-arc",
            "Sept héros dans Content/heroes, rangés par order : les quatre d'origine gardent leur place ("+string.Join(", ",ids)+")",checks);
        var f=heroes[H("fernandel")];var b=heroes[H("brigitte-bardot")];var j=heroes[H("jeanne-d-arc")];
        Check(f.special.kind==HeroSpecial.Summon&&j.special.kind==HeroSpecial.Dash&&b.special.kind==HeroSpecial.Salvo&&f.Weapon.id=="fusil-lebel"&&b.Weapon.id=="revolver"&&j.Weapon.id=="lame-de-lumiere",
            "Fernandel : fusil et invocation ; Brigitte : revolver et salve ; Jeanne : lame de lumière et charge",checks);
        foreach(var h in new[]{f,b,j})Check(h.specialPose>=0&&h.animations.CellCount==24,h.name+" : planche de 24 cases avec une pose « special »",checks);
    }

    // ---------- Menu ----------
    static void Menu(List<string> checks)
    {
        var card=typeof(PanameGame).GetMethod("PlayerCard",BindingFlags.NonPublic|BindingFlags.Static);bool layout=true;
        for(int i=0;i<4;i++)
        {
            var c=(Rect)card.Invoke(null,new object[]{i});Rect l=PanameGame.HeroArrow(i,-1),r=PanameGame.HeroArrow(i,1);
            layout&=c.Contains(l.min)&&c.Contains(l.max)&&c.Contains(r.min)&&c.Contains(r.max)&&!l.Overlaps(r)&&r.xMin-l.xMax>150;
            if(i>0)layout&=!c.Overlaps((Rect)card.Invoke(null,new object[]{i-1}));
        }
        Check(layout,"Menu : quatre cartes côte à côte (J1 à J4), deux flèches dans chacune",checks);
        // Five test heroes written as JSON: twelve heroes.
        for(int k=0;k<5;k++)ContentLibrary.AddHero("{\"id\":\"test-hero-"+k+"\",\"extends\":\""+(k%2==0?"fernandel":"jeanne-d-arc")+"\",\"order\":"+(10+k)+",\"name\":\"TEST "+k+"\"}");
        int n=PanameGame.HeroCount;Check(n==12,"Cinq héros de test ajoutés en JSON : douze héros",checks);
        g.ReturnToLobby();
        // The mouse: each card's arrows.
        bool mouse=true;
        for(int i=0;i<4;i++){g.slots[i].joined=true;g.slots[i].pad=null;g.slots[i].hero=i;}
        for(int i=0;i<4;i++)
        {
            var seen=new HashSet<int>();int start=g.slots[i].hero;
            for(int k=0;k<n;k++){Call(g,"MenuArrow",i,1);seen.Add(g.slots[i].hero);}
            mouse&=seen.Count==n&&g.slots[i].hero==start;
            Call(g,"MenuArrow",i,-1);mouse&=g.slots[i].hero==(start+n-1)%n;Call(g,"MenuArrow",i,1);
        }
        Check(mouse,"Souris : les flèches de chaque carte font défiler les douze héros pour chacun des quatre joueurs, dans les deux sens",checks);
        // Every player on the same hero, then a round with them.
        for(int i=0;i<4;i++)g.slots[i].hero=H("jeanne-d-arc");g.BeginRound();players=(PanameGame.Player[])Get(g,"players");
        bool same=true;for(int i=0;i<4;i++)same&=players[i]!=null&&players[i].Definition.id=="jeanne-d-arc";
        Check(same,"Quatre joueurs peuvent prendre le même héros",checks);
        g.ReturnToLobby();for(int i=1;i<4;i++)g.slots[i].joined=false;g.slots[0].hero=0;
        // The keyboard (J1) and pads, through the Input System and the game's own Update.
        // In batch mode the editor has no focused game view: let every device's input reach the game while this runs.
        var settings=InputSystem.settings;var playMode=settings.editorInputBehaviorInPlayMode;var background=settings.backgroundBehavior;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        var kb=InputSystem.AddDevice<Keyboard>();kb.MakeCurrent();var pads=new List<Gamepad>();var update=typeof(PanameGame).GetMethod("Update",Flags);
        try
        {
            void Press<T>(InputDevice device,T state,T released) where T:struct,IInputStateTypeInfo
            {
                InputSystem.QueueStateEvent(device,state);InputSystem.Update();update.Invoke(g,null);
                InputSystem.QueueStateEvent(device,released);InputSystem.Update();update.Invoke(g,null);
            }
            // The Input System starts telling presses apart the first time a key is asked about: one frame of warm-up.
            InputSystem.Update();update.Invoke(g,null);InputSystem.Update();update.Invoke(g,null);
            var seen=new HashSet<int>();
            for(int k=0;k<n;k++){Press(kb,new KeyboardState(Key.E),new KeyboardState());seen.Add(g.slots[0].hero);}
            string trace=seen.Count+" héros vus, J1 sur "+g.slots[0].hero;
            bool keys=seen.Count==n&&g.slots[0].hero==0;
            Press(kb,new KeyboardState(Key.Q),new KeyboardState());keys&=g.slots[0].hero==n-1;
            Press(kb,new KeyboardState(Key.RightArrow),new KeyboardState());keys&=g.slots[0].hero==0;
            Press(kb,new KeyboardState(Key.LeftArrow),new KeyboardState());keys&=g.slots[0].hero==n-1;
            Check(keys,"Clavier : E / D, Q / A et les flèches font défiler les douze héros de J1 ("+trace+")",checks);
            // Three pads join with A / cross, then scroll with the d-pad.
            for(int k=0;k<3;k++){var pad=InputSystem.AddDevice<Gamepad>();pads.Add(pad);Press(pad,new GamepadState().WithButton(GamepadButton.South),new GamepadState());}
            bool joined=g.slots[1].joined&&g.slots[2].joined&&g.slots[3].joined&&g.slots[1].pad==pads[0]&&g.slots[3].pad==pads[2];
            Check(joined,"Manettes virtuelles : A / croix fait rejoindre J2, J3 et J4 (leurs cartes « rejoindre » se remplissent)",checks);
            bool padScroll=true;
            for(int i=1;i<4;i++)
            {
                int start=g.slots[i].hero;var visited=new HashSet<int>();
                for(int k=0;k<n;k++){Press(pads[i-1],new GamepadState().WithButton(GamepadButton.DpadRight),new GamepadState());visited.Add(g.slots[i].hero);}
                padScroll&=visited.Count==n&&g.slots[i].hero==start;
                Press(pads[i-1],new GamepadState().WithButton(GamepadButton.DpadLeft),new GamepadState());padScroll&=g.slots[i].hero==(start+n-1)%n;
                // Only that player's hero moved.
                padScroll&=g.slots[0].hero==n-1;
            }
            Check(padScroll,"Manettes : la croix gauche / droite fait défiler les douze héros sur la carte de son joueur seulement",checks);
        }
        finally
        {
            foreach(var pad in pads)InputSystem.RemoveDevice(pad);InputSystem.RemoveDevice(kb);settings.editorInputBehaviorInPlayMode=playMode;settings.backgroundBehavior=background;
            for(int i=0;i<4;i++){g.slots[i].pad=null;g.slots[i].joined=i==0;g.slots[i].hero=i;}
            ContentLibrary.Reload();g.ReturnToLobby();
        }
    }

    // ---------- Specials ----------
    static float Expected(HeroSpecial s)=>s.cooldown+(int)typeof(PanameGame).GetProperty("DifficultyIndex",Flags).GetValue(g)*s.cooldownPerDifficulty;
    static void Press(int slot){commands[slot]=new PlayerCommand{special=true};Tick(1);commands[slot]=default;}

    static void Summon(List<string> checks,string captures)
    {
        Start(M("panique-a-paname"),1,H("fernandel"));Quiet();var p=players[0];Stand(p,40);var s=p.Definition.special;
        var foes=new List<object>{Dummy(43),Dummy(46),Dummy(49),Dummy(36)};var start=foes.ConvertAll(Hp);
        Press(0);float timer=p.specialTimer;bool posed=false,captured=false;int appearedAt=-1,t=1;float firstX=0;var hits=new int[foes.Count];
        Check(Mathf.Abs(timer-Expected(s))<1e-4f&&rushes.Count==1,"Fernandel siffle : une invocation en attente, recharge de "+Expected(s)+" s (cooldown + difficulté)",checks);
        Press(0);Check(rushes.Count==1,"Pendant la recharge, la spéciale ne repart pas",checks);t++;
        var last=foes.ConvertAll(Hp);
        for(;t<60*6&&(rushes.Count>0);t++)
        {
            posed|=p.animState==p.Definition.specialPose;
            var r=rushes.Count>0?rushes[0]:null;
            if(r!=null&&appearedAt<0&&(bool)Field(r,"started")){appearedAt=t;firstX=Pos(r).x;}
            if(r!=null&&!captured&&(bool)Field(r,"started")&&Pos(r).x>p.position.x-1){Set(g,"cameraX",p.position.x+3);Capture(captures+"/lot3h-marguerite.png");captured=true;}
            Tick(1);
            for(int i=0;i<foes.Count;i++){int hp=Hp(foes[i]);if(hp<last[i])hits[i]++;last[i]=hp;}
        }
        float camera=(float)Get(g,"cameraX");
        Check(posed&&p.specialPose<=0&&p.animState==p.Definition.idle,"Pose « spécial » (il siffle) pendant "+s.pose+" s, puis retour à l'attente",checks);
        Check(appearedAt>=Mathf.FloorToInt(s.delay*60)-1&&appearedAt<=Mathf.CeilToInt(s.delay*60)+2&&firstX<camera-11,"Marguerite arrive du bord de l'écran derrière lui après "+s.delay+" s (x "+firstX.ToString("0.0")+", caméra "+camera.ToString("0.0")+")",checks);
        bool once=true;for(int i=0;i<foes.Count;i++)once&=hits[i]==1&&start[i]-Hp(foes[i])==s.damage;
        Check(once&&rushes.Count==0,"Elle piétine chacun des quatre ennemis une seule fois ("+s.damage+" PV), devant comme derrière lui, puis quitte l'écran",checks);
        // Weak enemies fall.
        Quiet();p.specialTimer=0;var weak=new List<object>{Dummy(44,5),Dummy(47,5)};Press(0);Tick(60*4);
        Check(!enemies.Contains(weak[0])&&!enemies.Contains(weak[1])&&g.Score>0,"Les ennemis faibles qu'elle touche tombent",checks);
        // The cooldown runs out, then the special is there again.
        Tick(Mathf.CeilToInt((Expected(s)-4)*60)+2);Press(0);Check(rushes.Count==1,"Après la recharge, Fernandel siffle de nouveau",checks);
    }
    static void Dash(List<string> checks,string captures)
    {
        Start(M("panique-a-paname"),1,H("jeanne-d-arc"));Quiet();var p=players[0];Stand(p,40);var s=p.Definition.special;
        var foes=new List<object>{Dummy(42),Dummy(45),Dummy(48),Dummy(60)};var start=foes.ConvertAll(Hp);float x0=p.position.x;
        commands[0]=new PlayerCommand{special=true};Tick(1,false);commands[0]=new PlayerCommand{move=-1};
        Check(p.invulnerable>=s.time+s.invulnerable-.02f&&rushes.Count==1&&Mathf.Abs(p.specialTimer-Expected(s))<1e-4f,"Jeanne charge : invulnérable "+(s.time+s.invulnerable)+" s, recharge de "+Expected(s)+" s",checks);
        bool posed=true,captured=false;int ticks=1;var hits=new int[foes.Count];var last=foes.ConvertAll(Hp);
        while(p.dash>0&&ticks<200)
        {
            posed&=p.animState==p.Definition.specialPose||ticks==1;
            if(!captured&&p.position.x>43){Set(g,"cameraX",p.position.x+1);Capture(captures+"/lot3h-charge.png");captured=true;}
            Tick(1,false);ticks++;for(int i=0;i<foes.Count;i++){int hp=Hp(foes[i]);if(hp<last[i])hits[i]++;last[i]=hp;}
        }
        commands[0]=default;float moved=p.position.x-x0;
        Check(Mathf.Abs(moved-s.speed*s.time)<.35f&&p.facing==1,"Elle fonce de "+moved.ToString("0.00")+" u en "+s.time+" s ("+s.speed+" u/s) malgré le stick tiré en arrière",checks);
        Check(hits[0]==1&&hits[1]==1&&hits[2]==1&&hits[3]==0&&start[0]-Hp(foes[0])==s.damage,"Elle renverse une fois chacun des trois ennemis sur son chemin ("+s.damage+" PV), pas celui hors d'atteinte",checks);
        Check(posed&&rushes.Count==0,"Pose « spécial » pendant la charge, puis la charge s'arrête",checks);
        Tick(Mathf.CeilToInt(s.pose*60)+2);Check(p.specialPose<=0&&p.animState==p.Definition.idle,"Puis elle reprend sa garde",checks);
    }
    static void Salvo(List<string> checks,string captures)
    {
        Start(M("panique-a-paname"),1,H("brigitte-bardot"));Quiet();var p=players[0];Stand(p,40);var s=p.Definition.special;
        var right=Dummy(45);var left=Dummy(35);int hr=Hp(right),hl=Hp(left);var serials=new HashSet<int>();int ahead=0,behind=0,lastTick=0;bool captured=false,posed=true;
        Press(0);
        for(int t=1;t<90;t++)
        {
            foreach(var shot in shots){if(!(bool)shot.GetType().GetProperty("Hostile").GetValue(shot)&&serials.Add((int)Field(shot,"serial"))){if(((Vector2)Field(shot,"velocity")).x>0)ahead++;else behind++;lastTick=t;}}
            if(t>=2&&t<s.pose*60-1)posed&=p.animState==p.Definition.specialPose;
            if(!captured&&t==10){Set(g,"cameraX",p.position.x);Capture(captures+"/lot3h-feu-croise.png");captured=true;}
            Tick(1);
        }
        int expected=s.bursts*s.shots;
        Check(ahead==expected&&behind==expected,"Brigitte tire "+s.bursts+" salves de "+s.shots+" balles des deux côtés à la fois ("+ahead+" devant, "+behind+" derrière)",checks);
        Check(lastTick>=Mathf.FloorToInt((s.bursts-1)*s.interval*60)&&lastTick<=Mathf.CeilToInt((s.bursts-1)*s.interval*60)+2,"Une salve toutes les "+s.interval+" s (dernière au pas "+lastTick+")",checks);
        Check(Hp(right)<hr&&Hp(left)<hl&&posed,"Les ennemis devant et derrière elle sont touchés, pendant la pose « spécial » aux deux revolvers (PV "+hr+" > "+Hp(right)+", "+hl+" > "+Hp(left)+", pose "+posed+")",checks);
        Check(Mathf.Abs(p.specialTimer-(Expected(s)-89f/60))<.02f,"Recharge de "+Expected(s)+" s",checks);
    }
    // Heroes written only as JSON, with new specials: a salvo in four directions from the body, and a summon from behind.
    static void JsonOnly(List<string> checks)
    {
        try
        {
            var star=ContentLibrary.AddHero("{\"id\":\"test-quatre-vents\",\"extends\":\"chuck-norris\",\"order\":20,\"name\":\"QUATRE VENTS\",\"special\":{\"type\":\"salvo\",\"cooldown\":3,\"damage\":6,\"shots\":1,\"speed\":25,\"bursts\":2,\"interval\":0.1,\"directions\":[{\"angle\":0},{\"angle\":90},{\"angle\":180},{\"angle\":270}]}}");
            Start(M("panique-a-paname"),1,H("test-quatre-vents"));Quiet();var p=players[0];Stand(p,40);var behind=Dummy(36);int hp=Hp(behind);
            var dirs=new HashSet<string>();Press(0);for(int t=0;t<60;t++){foreach(var shot in shots){var v=(Vector2)Field(shot,"velocity");dirs.Add(Mathf.Round(v.x/25)+","+Mathf.Round(v.y/25));}Tick(1);}
            Check(star.specialPose<0&&dirs.Count==4&&Hp(behind)<hp,"Un héros écrit en JSON seulement (planche de Chuck, sans pose « spécial ») tire une salve en quatre directions depuis son torse",checks);
            ContentLibrary.AddHero("{\"id\":\"test-ruee\",\"extends\":\"jean-claude\",\"order\":21,\"name\":\"RUEE\",\"special\":{\"type\":\"summon\",\"cooldown\":4,\"damage\":30,\"delay\":0,\"ally\":{\"from\":\"behind\",\"speed\":22,\"reach\":1.2,\"height\":2,\"margin\":1,\"life\":2,\"animations\":{\"sheet\":{\"texture\":\"Art/heroes/marguerite\",\"columns\":2,\"rows\":2,\"pivot\":{\"x\":0.5,\"y\":0.04},\"cellHeight\":2.5},\"states\":[{\"name\":\"run\",\"cells\":[3,2,1,0],\"perStride\":2}]}}}}");
            Start(M("panique-a-paname"),1,H("test-ruee"));Quiet();p=players[0];Stand(p,40);var foe=Dummy(44,25);Press(0);
            float from=rushes.Count>0?Pos(rushes[0]).x:0;bool fast=rushes.Count==1&&Mathf.Abs(from-(40-1+22f/60))<.05f;Tick(30);
            Check(fast&&!enemies.Contains(foe),"Un autre héros JSON invoque son allié juste derrière lui (from behind), plus rapide, qui abat l'ennemi",checks);
            Tick(120);Check(rushes.Count==0,"L'allié disparaît au bout de sa durée de vie ("+2+" s)",checks);
        }
        finally{ContentLibrary.Reload();}
    }
    // A walking boss: the cow and the charge each hurt it once by their bossDamage.
    static void Bosses(List<string> checks)
    {
        foreach(var id in new[]{"fernandel","jeanne-d-arc"})
        {
            Start(M("prophetie-en-provence"),1,H(id));var p=players[0];Quiet();Stand(p,g.CurrentMission.exitX-18.5f);commands[0]=new PlayerCommand{move=1};Tick(20);commands[0]=default;
            if(!g.BossActive)throw new Exception("ÉCHEC : boss absent");
            // The boss arrives at the arena's right edge, past where heroes may go: it is brought 4 units in front of the hero.
            var boss=(Vector2)Get(g,"bossPosition");p.position=new Vector2(boss.x-9,0);p.facing=1;Set(g,"bossPosition",new Vector2(boss.x-5,0));Tick(1);int before=g.BossHealth,drops=0,last=before;
            var s=p.Definition.special;float x0=p.position.x;Press(0);int count=rushes.Count;float x1=p.position.x;Tick(10);string where="héros "+x0.ToString("0.0")+" > "+x1.ToString("0.0")+" > "+p.position.x.ToString("0.0")+", boss "+boss.x.ToString("0.0")+" > "+((Vector2)Get(g,"bossPosition")).ToString()+", charges "+count;
            for(int t=0;t<60*5;t++){Tick(1);if(g.BossHealth<last)drops++;last=g.BossHealth;}
            Check(drops==1&&before-g.BossHealth==s.bossDamage,p.Definition.name+" : sa spéciale touche le boss une seule fois ("+s.bossDamage+" PV ; "+drops+" coups, "+(before-g.BossHealth)+" PV, "+where+")",checks);
        }
    }

    // ---------- Poses ----------
    // Every state of the three new heroes on the street, Chuck Norris beside them for the scale, a mark on the barrel tip.
    static void Poses(List<string> checks,string captures)
    {
        Start(M("panique-a-paname"),1,0,H("fernandel"),H("brigitte-bardot"),H("jeanne-d-arc"));Quiet();var marks=new List<GameObject>();int taken=0;
        string[] names={"idle","run","jump","jumpDown","aimUp","aimDiagonal","crouch","crouchWalk","crouchShoot","knife","throw","special","hurt","down","victory"};
        foreach(var name in names)
        {
            foreach(var m in marks)UnityEngine.Object.DestroyImmediate(m);marks.Clear();
            for(int i=0;i<4;i++)
            {
                var p=players[i];var set=p.Definition.animations;int state=set.Find(name);if(state<0)state=set.Find("idle");
                p.position=new Vector2(36+i*4.4f,name.StartsWith("jump")?1.4f:0);p.facing=1;p.moving=false;p.walk=name.Contains("un")||name.Contains("Walk")?1.3f:0;p.animState=state;p.animTime=name=="knife"?.1f:name=="throw"?.2f:0;p.invulnerable=0;p.flash=0;
                Call(g,"SyncPlayer",i);
                int cell=(int)Call(g,"HeroCell",p);bool hand=name=="throw";
                if(!hand&&!set.HasAnchor("muzzle",cell))continue;
                Vector2 at=hand?p.position+p.Definition.hand:(Vector2)Call(g,"MuzzlePoint",p);
                var mark=new GameObject("Repère");var r=mark.AddComponent<SpriteRenderer>();r.sprite=PanameArt.Square;r.color=Color.magenta;r.sortingOrder=200;mark.transform.position=at;mark.transform.localScale=Vector3.one*.12f;marks.Add(mark);
            }
            Set(g,"cameraX",42.5f);Capture(captures+"/lot3h-pose-"+name+".png");taken++;
        }
        foreach(var m in marks)UnityEngine.Object.DestroyImmediate(m);
        // The run cycles, image by image.
        for(int k=0;k<6;k++)
        {
            for(int i=0;i<4;i++){var p=players[i];p.position=new Vector2(36+i*4.4f,0);p.moving=true;p.animState=p.Definition.run;p.walk=(k+.2f)/p.Definition.animations.states[p.Definition.run].perStride;Call(g,"SyncPlayer",i);}
            Set(g,"cameraX",42.5f);Capture(captures+"/lot3h-run-"+k+".png");taken++;
        }
        Check(taken==names.Length+6,"Captures de tous les états des trois nouveaux héros à côté de Chuck Norris (lot3h-pose-*.png, lot3h-run-*.png), repère magenta au bout de l'arme",checks);
    }

    // ---------- Replays and online ----------
    // VerifyLot2a's bot, which also shoots up at perched enemies.
    static PlayerCommand Bot(PanameGame.Player p,int i,int t)
    {
        float nearest=float.MaxValue,dir=1,height=0;int ahead=0;bool aimed=false;
        foreach(var e in enemies)
        {
            float d=Pos(e).x-p.position.x;if(Mathf.Abs(d)<Mathf.Abs(nearest)){nearest=d;dir=Mathf.Sign(d);height=Pos(e).y;}
            if(d*p.facing>1.5f&&d*p.facing<9)ahead++;
            if(Mathf.Abs(d)>1.6f&&Mathf.Abs(d)<13&&((float)Field(e,"windup")>0||(int)Field(e,"burst")>0))aimed=true;
        }
        float move=enemies.Count==0?1:Mathf.Abs(nearest)>5?dir:Mathf.Abs(nearest)<1?dir*.15f:dir*.6f;
        bool up=false,down=aimed&&p.position.y<.01f,jump=t%71==i*9&&!aimed,grenade=ahead>=2&&t%50==i*5;
        if(p.position.y>.4f&&Mathf.Abs(nearest)<.8f)down=true;
        if(p.position.y<.01f&&Mathf.Abs(nearest)>2&&t%240<30)down=true;
        // An enemy on a ledge or in a window (VerifyLot2a's bot only shoots level): stand under it and shoot up.
        if(height>.5f&&Mathf.Abs(nearest)<8){move=Mathf.Abs(nearest)>.4f?dir:0;up=true;down=false;}
        if(g.BossActive&&enemies.Count==0){float d=((Vector2)Get(g,"bossPosition")).x-p.position.x;move=Mathf.Abs(d)>.8f?Mathf.Sign(d):0;up=move==0&&t%90<60;grenade=t%40==i*7&&Mathf.Abs(d)<8;down=false;}
        if(g.ContinueOffered)jump=t%71==i*9;
        return new PlayerCommand{move=move,fire=true,aimUp=up,down=down,jump=jump,grenade=grenade&&!g.ContinueOffered,special=t%97==i*11};
    }
    static bool Verify(MissionDefinition[] missions,ReplayRecord record)
    {
        var verifier=PanameGame.CreateVerifier(missions);bool ok=record!=null&&verifier.BeginVerify(JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record)));bool? result=null;
        while(ok&&result==null)result=verifier.StepVerify(1e6f);UnityEngine.Object.DestroyImmediate(verifier.gameObject);return ok&&result==true;
    }
    static bool Online(MissionDefinition[] missions,ReplayRecord record,List<string> frames)
    {
        var guest=PanameGame.CreateVerifier(missions);guest.BeginVerify(JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record)));
        var queue=Get(guest,"netFrames");
        for(int i=0;i<frames.Count;i+=10)
        {
            Call(guest,"ParseFrames",string.Join("/",frames.GetRange(i,Mathf.Min(10,frames.Count-i))));
            while((int)queue.GetType().GetProperty("Count").GetValue(queue)>0)Call(guest,"RunFrame",queue.GetType().GetMethod("Dequeue").Invoke(queue,null));
        }
        bool ok=guest.Score==record.score&&(int)Call(guest,"StateHash")==record.hash&&(guest.phase==PanameGame.Phase.Won)==record.won&&guest.phase!=PanameGame.Phase.Playing;
        UnityEngine.Object.DestroyImmediate(guest.gameObject);return ok;
    }
    static void Replays(List<string> checks)
    {
        var missions=(MissionDefinition[])Get(g,"missions");
        // Mortal bots (the verifier replays inputs only): a mission with the three new heroes and Chuck, specials often.
        var record=RecordMortal(M("panique-a-paname"),0,60*60*12,out var frames,H("fernandel"),H("brigitte-bardot"),H("jeanne-d-arc"),0);
        foreach(var m in missions)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
        if(record==null)throw new Exception("ÉCHEC : la partie enregistrée n'a pas fini");
        Check(record.heroIds!=null&&string.Join(",",record.heroIds)=="fernandel,brigitte-bardot,jeanne-d-arc,chuck-norris","Le replay nomme ses héros (heroIds)",checks);
        bool replayed=Verify(missions,record),online=Online(missions,record,frames);
        Check(replayed&&online&&record.ticks>600,"Une partie à quatre avec les trois nouveaux héros (spéciales comprises) se rejoue à l'identique, en replay comme en trames en ligne ("+(record.won?"victoire":"défaite")+", "+record.ticks+" pas)",checks);
        var forged=JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record));forged.heroIds[1]="heros-inconnu";
        var verifier=PanameGame.CreateVerifier(missions);bool refused=!verifier.BeginVerify(forged);UnityEngine.Object.DestroyImmediate(verifier.gameObject);
        Check(refused,"Un replay qui nomme un héros inconnu est refusé",checks);
        var mixed=JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record));mixed.heroIds[0]="jeanne-d-arc";Check(!Verify(missions,mixed),"Un replay dont on change un héros ne passe plus",checks);
        try
        {
            // A hero added first in the list moves every position: the replay still finds its heroes by id.
            ContentLibrary.AddHero("{\"id\":\"aaa-test\",\"extends\":\"arnold\",\"order\":0,\"name\":\"EN TETE\"}");
            Check(H("aaa-test")==0&&H("fernandel")==5&&Verify(missions,record),"Avec un héros ajouté en tête de liste, le replay retrouve ses héros par leur id et passe toujours",checks);
            var old=JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record));old.heroIds=null;
            Check(!Verify(missions,old),"Un replay ancien (sans heroIds) se lit par position, comme avant : avec la liste décalée, il ne passe plus",checks);
        }
        finally{ContentLibrary.Reload();}
        // The lobby message carries the new heroes' positions to the guests.
        g.ReturnToLobby();for(int i=0;i<4;i++){g.slots[i].joined=true;g.slots[i].hero=6-i;}
        var json=(string)Call(g,"LobbyJson");var guest=PanameGame.CreateVerifier(missions);Call(guest,"ApplyLobby",json,false);
        bool lobby=true;for(int i=0;i<4;i++)lobby&=guest.slots[i].joined&&guest.slots[i].hero==6-i;UnityEngine.Object.DestroyImmediate(guest.gameObject);
        Check(lobby,"Le salon en ligne transmet le choix de chaque joueur parmi les sept héros",checks);
        for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;}
    }
    static ReplayRecord RecordMortal(int mission,int difficulty,int limit,out List<string> frames,params int[] heroes)
    {
        Start(mission,difficulty,heroes);var moves=new int[4];var flags=new int[4];var text=new StringBuilder();frames=new List<string>();
        for(int t=0;t<limit&&g.phase==PanameGame.Phase.Playing;t++)
        {
            for(int i=0;i<4;i++){moves[i]=0;flags[i]=0;var q=players[i];if(q==null)continue;var c=Bot(q,i,t);if(t%120<8)c.special=true;moves[i]=Mathf.RoundToInt(c.move*10)*10;flags[i]=(c.fire?1:0)|(c.aimUp?2:0)|(c.jump?4:0)|(c.special?8:0)|(c.down?16:0)|(c.grenade?32:0);commands[i]=(PlayerCommand)Call(g,"Command",moves[i],flags[i]);}
            text.Clear().Append(0);for(int i=0;i<4;i++)text.Append(' ').Append(moves[i]).Append(' ').Append(flags[i]);frames.Add(text.ToString());
            Call(g,"RecordTick",0,moves,flags);g.TickSimulation(1f/60,commands);Call(g,"EndReplayTick");
        }
        // A run cut short is ended like a lost one so that the record is closed.
        if(g.phase==PanameGame.Phase.Playing)return null;
        return g.LastReplay;
    }

    // ---------- Bots ----------
    static void Bots(List<string> checks)
    {
        var lines=new List<string>();var won=new HashSet<string>();var lost=new List<string>();
        bool Run(string hero,string mission)
        {
            Start(M(mission),1,H(hero));int t=0;for(;t<60*60*20&&g.phase==PanameGame.Phase.Playing;t++){commands[0]=Bot(players[0],0,t);Tick(1);}
            foreach(var m in (MissionDefinition[])Get(g,"missions"))PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
            if(g.phase!=PanameGame.Phase.Won){lost.Add(hero+" "+mission.Split('-')[0]+" (vague "+g.Wave+")");return false;}
            lines.Add(hero+" "+mission.Split('-')[0]+" "+t/60+" s");won.Add(mission);return true;
        }
        // The four original heroes: at least one mission each (the shared bot is not tuned for every hero and every map).
        foreach(var hero in new[]{"chuck-norris","arnold","jean-claude","sylvester"})
        {
            bool any=false;foreach(var mission in Missions)if(Run(hero,mission)){any=true;break;}
            if(!any)throw new Exception("ÉCHEC : aucun bot gagnant avec "+hero);
        }
        // Each new hero: every mission.
        foreach(var hero in new[]{"fernandel","brigitte-bardot","jeanne-d-arc"})foreach(var mission in Missions)if(!Run(hero,mission))throw new Exception("ÉCHEC : bot "+hero+" sur "+mission+" ("+string.Join(", ",lost)+")");
        Check(won.Count==4,"Un bot invulnérable gagne en ARCADE avec chacun des sept héros, et chaque nouveau héros gagne les quatre missions ("+string.Join(", ",lines)+(lost.Count>0?" ; perdues avant : "+string.Join(", ",lost):"")+")",checks);
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
