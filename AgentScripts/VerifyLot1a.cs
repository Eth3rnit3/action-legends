using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Paname;
// Lot 1a, the Metal Slug feel: crouching lets chest-high bullets fly over, down in the air shoots straight down,
// grenades fly in an arc from a limited stock that bonuses refill, the fire button stabs at arm's length, the new
// inputs survive replays and online frames, and a bot that uses all of it wins every mission. Captures show the new poses.
public static class VerifyLot1a
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

    static PanameGame g;static PanameGame.Player[] players;static IList enemies,shots,thrown;
    static readonly PlayerCommand[] commands=new PlayerCommand[4];
    // Missions by their place among the three this test was written for (Paris 0, Marseille 1, Tokyo 2): Saint-Remy now comes
    // before Tokyo and has its own VerifySaintRemy. A test mission appended after the catalogue keeps its index.
    static readonly string[] Three={"panique-a-paname","mistral-gagnant","tokyo-neon"};
    static int Old(PanameGame game,int m){var all=(MissionDefinition[])game.GetType().GetField("missions",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);return m<Three.Length?Array.FindIndex(all,x=>x.id==Three[m]):m;}
    static void Start(int mission,int difficulty,int heroes,int first=0)
    {
        g.ReturnToLobby();Call(g,"LoadMission",Old(g,mission));g.SetDifficulty(difficulty);
        for(int i=0;i<4;i++){g.slots[i].joined=i<heroes;g.slots[i].hero=(first+i)%4;}
        g.BeginRound();players=(PanameGame.Player[])Get(g,"players");enemies=(IList)Get(g,"enemies");shots=(IList)Get(g,"shots");thrown=(IList)Get(g,"thrown");
        for(int i=0;i<4;i++)commands[i]=default;
    }
    static void Tick(int n=1,bool invulnerable=true){for(int t=0;t<n;t++){if(invulnerable)foreach(var p in players)if(p!=null)p.invulnerable=1;g.TickSimulation(1f/60,commands);}}
    static void ClearEnemies(){foreach(var e in enemies)((Transform)Field(e,"root")).gameObject.SetActive(false);enemies.Clear();}
    static object Spawn(float x,string id,int hp=0,bool frozen=false)
    {
        int kind=Array.IndexOf(g.CurrentMission.enemies,id);if(kind<0)throw new Exception("ennemi absent de la mission : "+id);
        Call(g,"SpawnEnemy",x,kind);var e=enemies[enemies.Count-1];
        if(hp>0)SetField(e,"hp",hp);if(frozen){SetField(e,"stagger",999f);SetField(e,"attackTimer",999f);}
        return e;
    }
    static GameRules Rules=>g.CurrentMission.ruleSet;
    static int GrenadeBonus{get{for(int i=0;i<Rules.bonuses.Length;i++)if(Rules.bonuses[i].effect=="grenades")return i;return -1;}}

    public static object Main(string captures)
    {
        var checks=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        float scale=Time.timeScale;Time.timeScale=0;
        try
        {
            Crouch(checks,captures);ShootDown(checks);Grenades(checks,captures);Knife(checks);Inputs(checks);Bots(checks);Poses(checks,captures);
        }
        finally
        {
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=i;}
            foreach(var m in (MissionDefinition[])Get(g,"missions"))PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
            g.ReturnToLobby();Call(g,"LoadMission",0);g.SetDifficulty(1);g.ReturnToLobby();Time.timeScale=scale;
        }
        return string.Join("\n",checks);
    }

    // A pistol gunman fires at chest height: over a crouching hero, into a standing one.
    static void Crouch(List<string> checks,string captures)
    {
        Start(2,1,1);var p=players[0];var hero=p.Definition;ClearEnemies();
        Spawn(p.position.x+7,"gunman-pistol");
        commands[0]=new PlayerCommand{down=true};int fired=0,passed=0;bool captured=false;float crouchSpeed=0;
        var seen=new HashSet<object>();
        for(int t=0;t<60*5;t++)
        {
            p.invulnerable=0;float x=p.position.x;Tick(1,false);
            foreach(var s in shots){if(!(bool)s.GetType().GetProperty("Hostile").GetValue(s))continue;if(seen.Add(s))fired++;if(Pos(s).x<p.position.x-.6f)passed++;
                if(!captured&&Pos(s).x>p.position.x-.2f&&Pos(s).x<p.position.x+.5f){Capture(captures+"/lot1a-crouch-bullet.png");captured=true;}}
        }
        Check(p.crouching&&p.animState==hero.crouching,"Bas au sol : le héros s'accroupit (pose crouch)",checks);
        Check(fired>0&&passed>0&&p.hp==PlayerHealth(),"Les balles du tireur passent au-dessus du héros accroupi ("+fired+" tirées, PV "+p.hp+")",checks);
        commands[0]=new PlayerCommand{down=true,move=1};float before=p.position.x;Tick(30);crouchSpeed=(p.position.x-before)*2;
        Check(Mathf.Abs(crouchSpeed-hero.crouch.speed)<.05f&&p.animState==hero.crouchWalk,"Accroupi, il avance lentement ("+crouchSpeed.ToString("0.00")+" u/s au lieu de "+hero.speed+")",checks);
        p.position=new Vector2(p.position.x-3,0);commands[0]=default;int hp=p.hp;
        for(int t=0;t<60*5&&p.hp==hp;t++){p.invulnerable=0;Tick(1,false);}
        Check(p.hp<hp,"Debout, le même tireur le touche",checks);
    }
    static int PlayerHealth()=>new[]{6,5,4,3}[(int)g.SelectedDifficulty];

    // Down and fire in the air: shots go straight down and kill an enemy below.
    static void ShootDown(List<string> checks)
    {
        Start(0,1,1);var p=players[0];ClearEnemies();var e=Spawn(p.position.x,"looter-rusher",0,true);
        commands[0]=new PlayerCommand{jump=true};Tick(1);commands[0]=new PlayerCommand{down=true,fire=true};
        bool down=false,pose=false,stabbed=false;
        for(int t=0;t<50&&enemies.Contains(e);t++)
        {
            Tick(1);foreach(var s in shots){var v=(Vector2)Field(s,"velocity");if(v.y<-30&&Mathf.Abs(v.x)<.01f)down=true;}
            pose|=p.animState==p.Definition.jumpDown&&p.aimDown;stabbed|=p.knifePose>0;
        }
        Check(down&&pose,"Bas + tir en l'air : tir vertical vers le bas depuis la pose jumpDown",checks);
        Check(!enemies.Contains(e)&&!stabbed,"Le tir vers le bas tue un ennemi en dessous, sans couteau",checks);
        // Up still shoots up, and diagonally while moving.
        Start(0,1,1);p=players[0];ClearEnemies();commands[0]=new PlayerCommand{aimUp=true,fire=true};Tick(2);
        bool up=false;foreach(var s in shots){var v=(Vector2)Field(s,"velocity");up|=v.x==0&&v.y>30;}
        commands[0]=new PlayerCommand{aimUp=true,fire=true,move=1};Tick(12);bool diagonal=false;foreach(var s in shots){var v=(Vector2)Field(s,"velocity");diagonal|=v.x>20&&v.y>20;}
        Check(up&&diagonal&&p.animState==p.Definition.aimDiagonal,"Le tir vers le haut et en diagonale restent, avec leurs poses",checks);
    }

    static void Grenades(List<string> checks,string captures)
    {
        Start(0,1,1);var p=players[0];ClearEnemies();var rules=Rules;var gr=rules.grenade;int d=(int)g.SelectedDifficulty;
        Check(p.grenades==rules.grenades.start[d],"Le héros commence avec le stock de grenades de la difficulté ("+p.grenades+")",checks);
        commands[0]=new PlayerCommand{grenade=true};Tick(1);commands[0]=default;Tick(1);
        Check(thrown.Count==1&&p.grenades==rules.grenades.start[d]-1&&p.animState==p.Definition.throwing,"L (ou B) lance une grenade : le stock baisse, pose de lancer",checks);
        var n=thrown[0];Vector2 start=Pos(n);float apex=start.y,last=start.x;bool forward=true,bounced=false;Vector2 end=start;bool captured=false;
        for(int t=0;t<60*4&&thrown.Count>0;t++)
        {
            Tick(1);if(thrown.Count==0)break;var at=Pos(n);apex=Mathf.Max(apex,at.y);forward&=at.x>=last;last=at.x;end=at;bounced|=(int)Field(n,"bounces")>0;
            if(!captured&&at.y<apex-.3f){Capture(captures+"/lot1a-grenade.png");captured=true;}
        }
        Check(apex>start.y+1&&forward&&bounced&&end.x>start.x+4,"Trajectoire en cloche vers l'avant, un rebond, explosion à "+(end.x-start.x).ToString("0.0")+" u",checks);
        // The same throw lands among enemies: the two near ones take the blast, the far one does not.
        float land=end.x;var a=Spawn(land-.3f,"looter-shield",100,true);var b=Spawn(land+1.4f,"looter-shield",100,true);var c=Spawn(land+9,"looter-shield",100,true);
        for(int t=0;t<60;t++)Tick(1);
        commands[0]=new PlayerCommand{grenade=true};Tick(1);commands[0]=default;
        for(int t=0;t<60*4&&thrown.Count>0;t++)Tick(1);
        Check(Hp(a)==100-gr.damage&&Hp(b)==100-gr.damage&&Hp(c)==100,"L'explosion blesse les ennemis dans son rayon ("+Hp(a)+", "+Hp(b)+") et épargne les autres ("+Hp(c)+")",checks);
        p.grenades=0;commands[0]=new PlayerCommand{grenade=true};Tick(1);commands[0]=default;Tick(30);
        Check(thrown.Count==0&&p.grenades==0&&p.throwPose==0,"Sans grenade, le bouton ne lance rien",checks);
        int bonus=GrenadeBonus;g.GiveBonus(0,bonus);
        Check(bonus>=0&&p.grenades==rules.bonuses[bonus].amount[d],"Le bonus grenades recharge le stock (+"+p.grenades+")",checks);
        p.grenades=rules.grenades.max[d]-1;g.GiveBonus(0,bonus);
        Check(p.grenades==rules.grenades.max[d],"Le stock ne dépasse pas le maximum de la difficulté ("+p.grenades+")",checks);
        p.grenades=0;Call(g,"SpawnPickup",p.position,bonus);Tick(3);
        Check(p.grenades==rules.bonuses[bonus].amount[d],"Une caisse de grenades ramassée recharge aussi le stock",checks);
        // A mission naming other rules plays by them: here three grenades, crates that only give grenades.
        var custom=ContentLibrary.AddRules(@"{""id"": ""test-rules"", ""extends"": ""default"", ""grenades"": {""start"": [3, 3, 3, 3], ""max"": [5, 5, 5, 5]}, ""supplies"": [4]}");
        var mission=g.CurrentMission;var shipped=mission.ruleSet;mission.ruleSet=custom;
        try{g.BeginRound();p=players[0];Check(p.grenades==3&&custom.grenade.damage==gr.damage&&custom.supplies.Length==1,"Une mission qui nomme d'autres règles (extends default) en change le stock sans réécrire le reste",checks);}
        finally{mission.ruleSet=shipped;}
        // The boss: a walker takes the blast, a helicopter's missiles caught in it blow up.
        Start(2,1,1);p=players[0];p.position=new Vector2(g.CurrentMission.exitX-18,0);Tick(2);
        int health=g.BossHealth;Call(g,"Explode",(Vector2)Get(g,"bossPosition"));
        Check(g.BossActive&&health-g.BossHealth==gr.bossDamage,"L'explosion blesse le boss (-"+(health-g.BossHealth)+")",checks);
        Start(0,1,1);p=players[0];p.position=new Vector2(g.CurrentMission.exitX-18,0);var bombs=(IList)Get(g,"bossBombs");object bomb=null;
        for(int t=0;t<60*20&&bomb==null;t++){Tick(1);foreach(var x in bombs)if((bool)Field(x,"launched")){bomb=x;break;}}
        int count=bombs.Count;if(bomb!=null)Call(g,"Explode",Pos(bomb));
        Check(bomb!=null&&!bombs.Contains(bomb)&&bombs.Count<count,"Elle détruit les missiles du boss pris dans son rayon",checks);
    }

    static void Knife(List<string> checks)
    {
        Start(0,1,1);var p=players[0];ClearEnemies();var k=p.Definition.knife;Tick(10);
        var e=Spawn(p.position.x+k.reach*.7f,"looter-shield",100,true);int before=shots.Count;
        commands[0]=new PlayerCommand{fire=true};Tick(1);
        Check(shots.Count==before&&Hp(e)==100-k.damage&&p.knifePose>0,"Au contact, le tir devient un coup de couteau : pas de projectile, "+k.damage+" dégâts",checks);
        Tick(1);Check(p.animState==p.Definition.stab,"Le couteau a sa pose",checks);
        commands[0]=default;Tick(30);SetField(e,"position",new Vector2(p.position.x+5,0));int hp=Hp(e);
        commands[0]=new PlayerCommand{fire=true};Tick(1);commands[0]=default;
        bool shot=shots.Count>0;Tick(20);
        Check(shot&&hp-Hp(e)==p.Definition.Weapon.damage,"À distance, le même bouton tire normalement",checks);
    }

    // The new bits through the replay record and the online messages.
    static void Inputs(List<string> checks)
    {
        var command=(PlayerCommand)Call(g,"Command",0,48);
        Check(command.down&&command.grenade&&!command.fire,"Les bits 16 (bas) et 32 (grenade) se décodent",checks);
        Start(0,1,1);Set(g,"localGrenades",3);
        string body=(string)Call(g,"GuestInput",new PlayerCommand{move=.5f,fire=true,down=true});
        var type=typeof(PanameGame).GetNestedType("NetInput",BindingFlags.NonPublic);var args=new object[]{body,Activator.CreateInstance(type)};
        typeof(PanameGame).GetMethod("ParseInput",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
        Check((int)type.GetField("flags").GetValue(args[1])==17&&(int)type.GetField("grenades").GetValue(args[1])==3&&(int)type.GetField("move").GetValue(args[1])==50,"Le message d'entrée en ligne porte bas et le compteur de grenades ("+body+")",checks);
        var frame=Activator.CreateInstance(typeof(PanameGame).GetNestedType("NetFrame",BindingFlags.NonPublic));((int[])frame.GetType().GetField("flags").GetValue(frame))[0]=48;
        var p=players[0];for(int i=0;i<4;i++)p.invulnerable=1;Call(g,"RunFrame",frame);
        Check(p.crouching&&thrown.Count==1,"Une image en ligne avec ces bits accroupit le héros et lance une grenade",checks);
        // A real run that crouches, throws and stabs, recorded and replayed in the hidden verifier.
        var missions=(MissionDefinition[])Get(g,"missions");var verifier=PanameGame.CreateVerifier(missions);var results=new List<string>();var total=new int[3];
        foreach(var run in new[]{(m:2,d:3,h:2),(m:0,d:3,h:1)})
        {
            Start(run.m,run.d,run.h);var moves=new int[4];var flags=new int[4];var use=new int[3];
            for(int t=0;t<60*60*10&&g.phase==PanameGame.Phase.Playing;t++)
            {
                for(int i=0;i<4;i++){moves[i]=0;flags[i]=0;var q=players[i];if(q==null)continue;var c=Bot(q,i,t,use);moves[i]=Mathf.RoundToInt(c.move*10)*10;flags[i]=(c.fire?1:0)|(c.aimUp?2:0)|(c.jump?4:0)|(c.special?8:0)|(c.down?16:0)|(c.grenade?32:0);commands[i]=(PlayerCommand)Call(g,"Command",moves[i],flags[i]);}
                Call(g,"RecordTick",0,moves,flags);g.TickSimulation(1f/60,commands);Call(g,"EndReplayTick");
            }
            var record=g.LastReplay;foreach(var m in missions)PlayerPrefs.DeleteKey("action-legends.unlocked."+m.id);
            bool ok=record!=null&&verifier.BeginVerify(JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(record)));bool? result=null;while(ok&&result==null)result=verifier.StepVerify(1e6f);
            Check(ok&&result==true,"Une partie avec accroupi ("+use[0]+" pas), grenades ("+use[1]+") et couteau ("+use[2]+") se rejoue à l'identique : "+missions[Old(g,run.m)].id+" ("+(record!=null&&record.won?"victoire":"défaite")+", "+record?.ticks+" pas)",checks);
            for(int k=0;k<3;k++)total[k]+=use[k];
        }
        Check(total[0]>0&&total[1]>0&&total[2]>0,"Ces parties rejouées se servent des trois nouveautés",checks);
    }

    // Walks to the nearest enemy, crouches while a gunman aims, stabs up close, throws grenades into groups,
    // shoots down when an enemy is under it, and stands under the boss shooting up. use: crouch ticks, grenades, stabs.
    static PlayerCommand Bot(PanameGame.Player p,int i,int t,int[] use)
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
        // Now and then it also crouches out in the open, to cover the crouch walk where no gunman aims.
        if(p.position.y<.01f&&Mathf.Abs(nearest)>2&&t%240<30)down=true;
        if(g.BossActive&&enemies.Count==0){float d=((Vector2)Get(g,"bossPosition")).x-p.position.x;move=Mathf.Abs(d)>.8f?Mathf.Sign(d):0;up=move==0&&t%90<60;grenade=t%40==i*7&&Mathf.Abs(d)<8;down=false;}
        if(p.crouching)use[0]++;if(grenade&&p.grenades>0&&p.grenadeTimer<=0&&p.hp>0)use[1]++;if(p.knifePose>0&&Mathf.Approximately(p.knifePose,p.Definition.knife.pose))use[2]++;
        return new PlayerCommand{move=move,fire=true,aimUp=up,down=down,jump=jump,grenade=grenade,special=t%97==i*11};
    }

    static void Bots(List<string> checks)
    {
        var times=new List<string>();int stabs=0;
        foreach(var run in new[]{(m:0,h:1),(m:1,h:1),(m:2,h:1),(m:2,h:4)})
        {
            Start(run.m,1,run.h);var use=new int[3];int t=0;
            for(;t<60*60*20&&g.phase==PanameGame.Phase.Playing;t++){for(int i=0;i<4;i++)commands[i]=players[i]==null?default:Bot(players[i],i,t,use);Tick(1);}
            stabs+=use[2];
            Check(g.phase==PanameGame.Phase.Won&&use[0]>0&&use[1]>0,"Un bot qui s'accroupit, lance des grenades et joue du couteau gagne "+g.CurrentMission.title+" ×"+run.h+" ("+(t/60)+" s ; accroupi "+use[0]/60+" s, "+use[1]+" grenades, "+use[2]+" coups de couteau)",checks);
        }
        Check(stabs>0,"Le bot a joué du couteau au fil de ces missions ("+stabs+" coups)",checks);
    }

    // Every new pose of the four heroes in the street, with a mark on the barrel tip or the throwing hand.
    static void Poses(List<string> checks,string captures)
    {
        Start(0,1,4);ClearEnemies();var marks=new List<GameObject>();int shotsTaken=0;
        string[] names={"idle","run","crouch","crouchWalk","crouchShoot","aimUp","aimDiagonal","jump","jumpDown","knife","throw"};
        foreach(var name in names)
        {
            foreach(var m in marks)UnityEngine.Object.DestroyImmediate(m);marks.Clear();
            for(int i=0;i<4;i++)
            {
                var p=players[i];var set=p.Definition.animations;int state=set.Find(name);if(state<0)throw new Exception("état absent : "+name);
                p.position=new Vector2(2.2f+i*4.2f,name.StartsWith("jump")?1.4f:0);p.facing=1;p.moving=false;p.walk=0;p.animState=state;p.animTime=name=="knife"?.1f:name=="throw"?.2f:0;p.invulnerable=0;p.flash=0;
                Call(g,"SyncPlayer",i);
                bool hand=name=="throw";Vector2 at=hand?p.position+p.Definition.hand:(Vector2)Call(g,"MuzzlePoint",p);
                if(name=="knife")continue;
                var mark=new GameObject("Repère");var r=mark.AddComponent<SpriteRenderer>();r.sprite=PanameArt.Square;r.color=Color.magenta;r.sortingOrder=200;mark.transform.position=at;mark.transform.localScale=Vector3.one*.12f;marks.Add(mark);
            }
            Set(g,"cameraX",8.5f);Capture(captures+"/lot1a-pose-"+name+".png");shotsTaken++;
        }
        foreach(var m in marks)UnityEngine.Object.DestroyImmediate(m);
        Check(shotsTaken==names.Length,"Captures des poses des quatre héros (lot1a-pose-*.png), repère magenta sur la bouche du canon ou la main",checks);
    }

    static void Capture(string path)
    {
        Set(g,"shake",0f);g.gameCamera.aspect=16f/9;var x=(float)Get(g,"cameraX");
        PanameArt.UpdatePanorama(x,g.gameCamera.orthographicSize*g.gameCamera.aspect,g.CurrentMission.cameraMin,g.CurrentMission.cameraMax);g.gameCamera.transform.position=new Vector3(x,3.6f,-10);
        var rt=new RenderTexture(1280,720,24);g.gameCamera.targetTexture=rt;g.gameCamera.Render();
        RenderTexture.active=rt;var shot=new Texture2D(1280,720,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,1280,720),0,0);shot.Apply();
        g.gameCamera.targetTexture=null;RenderTexture.active=null;File.WriteAllBytes(path,shot.EncodeToPNG());
    }
}
