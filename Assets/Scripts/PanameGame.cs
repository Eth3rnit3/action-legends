using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Paname.PanameArt;

namespace Paname
{
    public sealed partial class PanameGame : MonoBehaviour
    {
        // Heroes come from Resources/Content/heroes; replays and online play store their position in this list.
        public static HeroDefinition[] Heroes=>ContentLibrary.Heroes;
        public static int HeroCount=>Heroes.Length;
        public static readonly Color[] PlayerColors={C("F5BD56"),C("59CCBA"),C("F98A72"),C("A7AFF5")};
        public readonly LocalSlot[] slots={new LocalSlot{joined=true},new LocalSlot{hero=1},new LocalSlot{hero=2},new LocalSlot{hero=3}};
        public enum Phase { Lobby, Playing, Paused, Won, Lost }
        public Phase phase=Phase.Lobby;
        MissionDefinition[] missions;
        int missionIndex;
        public MissionDefinition CurrentMission=>missions[missionIndex];
        public int MissionCount=>missions?.Length??0;
        public int MissionIndex=>missionIndex;
        public void PauseFromBrowser(){if(!Online&&phase==Phase.Playing)phase=Phase.Paused;}
        // The lobby arrows only stop on missions this browser has unlocked.
        public void SelectMission(int direction)
        {
            if(HostOnly(()=>SelectMission(direction))||phase!=Phase.Lobby||missions==null)return;
            int index=missionIndex;
            for(int i=0;i<missions.Length;i++){index=Mod(index+direction,missions.Length);if(IsUnlocked(index))break;}
            if(index!=missionIndex)LoadMission(index);
        }
        // Also used outside the lobby, when a won mission chains into the next one.
        void LoadMission(int index)
        {
            missionIndex=Mod(index,missions.Length);
            if(world){world.gameObject.SetActive(false);Destroy(world.gameObject);}
            world=new GameObject("Décor de mission").transform;world.SetParent(transform,false);Street(world,CurrentMission.map,CurrentMission.length);
            cameraX=CurrentMission.cameraMin;LoadMissionMusic();BuildLobbyActors();
            missionLogo=string.IsNullOrEmpty(CurrentMission.logo)?null:Resources.Load<Texture2D>(CurrentMission.logo);
        }
        public int Score {get;private set;}
        public int Wave {get;private set;}
        public int Reserves {get;private set;}
        public int EnemyCount => enemies.Count;
        public int JoinedCount {get{int n=0;foreach(var s in slots)if(s.joined)n++;return n;}}
        public float SimulationTime {get;private set;}
        public Camera gameCamera;
        readonly Player[] players=new Player[4];
        readonly List<Enemy> enemies=new List<Enemy>();
        readonly List<Shot> shots=new List<Shot>();
        readonly List<Shot> shotPool=new List<Shot>();
        // Numbers every shot fired, as pooled shots are reused: a hostage counts each shot through his ropes once.
        int shotSerial;
        Transform projectileRoot;
        Transform actors,world;
        ArcadeEffects effects;
        MaterialPropertyBlock flashProperties;
        public int ActiveEffects => effects?.ActiveCount ?? 0;
        public int EffectPoolCapacity => effects?.Capacity ?? 0;
        Font font;
        float spawnTimer,announcementTimer,shake;
        int pending;
        bool waveLive,screenLocked;
        // lockX: where the camera holds during a locked wave; goFromX: where it stood when the last one was cleared, for the GO arrow.
        float lockX,goFromX=-1;
        string announcement="";
        float cameraX=7;
        AudioSource audioOut;
        readonly PlayerCommand[] frameInputs=new PlayerCommand[4];
        public sealed class Player
        {
            public Vector2 position; public float vertical,fireTimer,specialTimer,invulnerable,downTimer,walk,recoil,flash,dust;
            public int ammo,shots,animState=-1,grenades;
            // rescued: hostages freed since this hero last fell.
            public int rescued,lives; public float boost,boostMax=1,animTime,grenadeTimer,knifePose,throwPose,kick;
            // The special's pose, a dash under way and the bursts of a salvo still to fire (PanameSpecials.cs).
            public float specialPose,dash,salvoTimer; public int salvo;
            // The relief under his feet (PanameTerrain.cs), and the ledge he drops through with down + jump until he is below it.
            public float floor,dropFrom=float.MaxValue;
            // The weapon in hand (Content/weapons), with ammo rounds left unless it is unlimited; shots counts what it has fired.
            public WeaponDefinition weapon;
            public int hp=6,hero,facing=1; public bool moving,aimUp,aimDown,crouching; public Transform root; public SpriteRenderer illustrated; public SpriteRenderer[] renderers;
            public HeroDefinition Definition=>Heroes[hero];
        }
        sealed class Enemy
        {
            public Vector2 position; public int hp,kind,facing=-1,burst,animState=-1; public float attackTimer,attackPose,walk,flash,stagger,knockback,windup,burstTimer,animTime;
            public EnemyDefinition type; public string Role=>type.id;
            public Transform root; public SpriteRenderer illustrated;
            // Lot 2b (PanameEnemyLife.cs, PanameEncounters.cs): what it is doing (EnemyActive...), the encounter it came with
            // (-1 none, -2 the boss's troops), how it enters and where it goes, and its panic.
            public int mode,group=-1,entry,stage,maxHp,rest,fright; public float modeTime,grenadeTimer,throwPose,vertical,fade=1; public bool fled,routed,perched,unaware;
            public Vector2 goal; public Player target; public Transform sign,chute;
            // Lot 3a: the ledge it drops through, and the prop it was placed on (it falls when that breaks).
            public float dropFrom=float.MaxValue; public Breakable standOn;
        }
        // Sprite: a heroes' weapon projectile drawn from its weapon's sheet.
        enum ShotLook { Tracer, Molotov, Bullet, Sprite }
        sealed class Shot
        {
            public Vector2 position,velocity,heading; public float life=1.8f,age,blast,length; public int damage,pierce,blastDamage,blastBoss,serial; public ShotLook look; public bool hostile,active,blastAnywhere; public Transform root;
            // Heroes' weapons: the weapon that fired it and its owner (a beam follows the barrel); struck: enemies it has passed through.
            public WeaponDefinition weapon; public Player owner; public SpriteRenderer sprite,halo;
            // A boss's drawn projectile (Sprite look, hostile): its frames play as it flies.
            public BossSprite drawn;
            public readonly List<Enemy> struck=new List<Enemy>();
            public bool Hostile=>hostile;
        }

        void Awake()
        {
            Application.targetFrameRate=60;
            flashProperties=new MaterialPropertyBlock();
            missions=LevelCatalog.LoadAll();cameraX=CurrentMission.cameraMin;
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(!gameCamera) {var go=new GameObject("Main Camera");gameCamera=go.AddComponent<Camera>();go.tag="MainCamera";}
            gameCamera.orthographic=true; gameCamera.orthographicSize=ViewHalfHeight; gameCamera.backgroundColor=C("C6DFD9"); gameCamera.transform.position=new Vector3(cameraX,3.6f,-10);
            world=new GameObject("Décor de mission").transform; world.SetParent(transform,false); Street(world,CurrentMission.map,CurrentMission.length);
            actors=new GameObject("Acteurs et effets").transform;actors.SetParent(transform,false);
            audioOut=gameObject.AddComponent<AudioSource>();audioOut.volume=.2f;
            projectileRoot=new GameObject("Projectiles • pool").transform;projectileRoot.SetParent(transform,false);
            effects=new ArcadeEffects(transform);
            foreach(var hero in Heroes){hero.animations.Prewarm();if(hero.special.kind==HeroSpecial.Summon)hero.special.ally.animations.Prewarm();}
            foreach(var mission in missions){foreach(var enemy in mission.roster)enemy.animations.Prewarm();foreach(var prop in mission.map.props)prop.definition.animations.Prewarm();}
            for(int row=0;row<4;row++)for(int frame=0;frame<6;frame++)ArcadeEffects.Frame(row,frame);
            LoadSoundtrack();BuildLobbyActors();
        }
        void Sound(AudioClip clip){if(audioOut&&clip)audioOut.PlayOneShot(clip);}
        void ResetActors()
        {
            for(int i=actors.childCount-1;i>=0;i--){var child=actors.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
            foreach(var shot in shots){shot.active=false;shot.root.gameObject.SetActive(false);}
            Array.Clear(players,0,4);enemies.Clear();shots.Clear();thrown.Clear();rushes.Clear();effects?.Clear();ResetBonuses();ResetBoss();ResetHostages();ResetEncounters();ResetProps();
        }
        void BuildLobbyActors()
        {
            ResetActors();PlaceProps();BuildTerrain();
            for(int i=0;i<4;i++)if(slots[i].joined)SpawnPlayer(i,3+i*1.6f);
        }
        void SpawnPlayer(int i,float x)
        {
            var p=new Player{position=new Vector2(x,SolidAt(x,ForHeroes)),hero=Mod(slots[i].hero,HeroCount),hp=MaxPlayerHealth,invulnerable=2,lives=Level.livesPerPlayer?Level.lives:0,grenades=Rules.grenades.start[DifficultyIndex]};var hero=p.Definition;p.weapon=hero.Weapon;
            p.root=Actor(actors,hero.animations,hero.animations.states[hero.idle].cells[0],PlayerColors[i],true);p.root.name="J"+(i+1)+" • "+hero.name;
            p.illustrated=p.root.Find("Illustration").GetComponent<SpriteRenderer>();
            p.renderers=p.root.GetComponentsInChildren<SpriteRenderer>();players[i]=p;SyncPlayer(i);
        }
        public void BeginRound()
        {
            if(OnlineRound())return;
            ResetActors();PlaceProps();BuildTerrain();Score=carriedScore;carriedScore=0;Wave=0;Reserves=StartingReserves;pending=0;waveLive=false;screenLocked=false;goFromX=-1;spawnTimer=0;SimulationTime=0;cameraX=CurrentMission.cameraMin;shake=0;
            for(int i=0;i<4;i++)if(slots[i].joined)SpawnPlayer(i,2+i*1.3f);
            PlaceHostages();ResetContinue();StartReplay();
            phase=Phase.Playing;Announce(CurrentMission.intro);Sound(startClip);if(musicOut){musicOut.Stop();musicOut.clip=musicClip;}
        }
        public void ReturnToLobby(){if(OnlineReturn())return;phase=Phase.Lobby;cameraX=CurrentMission.cameraMin;BuildLobbyActors();}
        public void SelectHero(int playerSlot,int hero)
        {
            if(OnlineHero(hero)||phase!=Phase.Lobby||playerSlot<0||playerSlot>=4||!slots[playerSlot].joined)return;
            slots[playerSlot].hero=Mathf.Clamp(hero,0,HeroCount-1);BuildLobbyActors();Sound(selectClip);
        }
        void Update()
        {
            TickVerifier();
            if(OnlineUpdate())return;
            if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if(phase==Phase.Playing)phase=Phase.Paused;else if(phase==Phase.Paused)phase=Phase.Playing;
                else if(phase==Phase.Won||phase==Phase.Lost)ReturnToLobby();
            }
            CheckControllers();
            if(phase==Phase.Lobby)
            {
                var k=Keyboard.current;
                if(k!=null&&!LobbyKeysBlocked){if(k.qKey.wasPressedThisFrame||k.aKey.wasPressedThisFrame)ChangeHero(0,-1);if(k.eKey.wasPressedThisFrame||k.dKey.wasPressedThisFrame)ChangeHero(0,1);if(k.leftArrowKey.wasPressedThisFrame)ChangeHero(0,-1);if(k.rightArrowKey.wasPressedThisFrame)ChangeHero(0,1);if(k.enterKey.wasPressedThisFrame)BeginRound();}
                for(int i=0;i<4;i++)if(slots[i].joined&&slots[i].Connected)
                {
                    if(i==0&&slots[i].pad.leftShoulder.wasPressedThisFrame)SelectMission(-1);
                    if(i==0&&slots[i].pad.rightShoulder.wasPressedThisFrame)SelectMission(1);
                    if(slots[i].pad.dpad.left.wasPressedThisFrame)ChangeHero(i,-1);
                    if(slots[i].pad.dpad.right.wasPressedThisFrame)ChangeHero(i,1);
                    if(slots[i].pad.startButton.wasPressedThisFrame){BeginRound();break;}
                }
            }
            else if(phase==Phase.Won&&NextMissionDefinition!=null&&ContinuePressed())NextMission();
            else if(phase==Phase.Playing)LocalStep();
            UpdateCamera(Time.unscaledDeltaTime);UpdateSoundtrack(Time.unscaledDeltaTime);
        }
        void CheckControllers()
        {
            if(phase==Phase.Lobby)
            {
                for(int i=1;i<4;i++)if(slots[i].joined&&!slots[i].Connected)
                {
                    slots[i].joined=false;slots[i].pad=null;
                    if(players[i]!=null){players[i].root.gameObject.SetActive(false);Destroy(players[i].root.gameObject);players[i]=null;}
                }
            }
            foreach(var pad in Gamepad.all)
            {
                bool assigned=false;for(int i=0;i<4;i++)if(slots[i].pad==pad){assigned=true;break;}
                if(assigned)continue;
                if(pad.buttonSouth.wasPressedThisFrame && (phase==Phase.Lobby||phase==Phase.Playing))
                {
                    int index=-1;
                    for(int i=0;i<4;i++)if(slots[i].pad!=null&&!slots[i].Connected){index=i;break;}
                    if(index<0)for(int i=1;i<4;i++)if(!slots[i].joined){index=i;break;}
                    if(index<0&&slots[0].pad==null)index=0;
                    if(index<0)continue;
                    slots[index].joined=true;slots[index].pad=pad;Sound(coinClip);
                    if(players[index]==null){if(phase==Phase.Lobby)BuildLobbyActors();else{SpawnPlayer(index,LeaderX()-1);ReplayBroken("UN JOUEUR A REJOINT EN COURS DE MISSION");}}
                }
            }
            for(int i=0;i<4;i++)if(slots[i].Connected && slots[i].pad.startButton.wasPressedThisFrame)
            {
                if(phase==Phase.Playing)phase=Phase.Paused;else if(phase==Phase.Paused)phase=Phase.Playing;
            }
        }
        void ChangeHero(int slot,int direction)
        {
            slots[slot].hero=Mod(slots[slot].hero+direction,HeroCount);BuildLobbyActors();
        }
        public Vector2 GetPlayerPosition(int slot)=>players[slot]?.position??Vector2.zero;
        public int GetPlayerHealth(int slot)=>players[slot]?.hp??0;
        // The simulation sees a fixed 16:9 view so every browser computes the same game, whatever its window size.
        const float ViewHalfHeight=5.8f,SimHalfWidth=ViewHalfHeight*16f/9f;
        public void TickSimulation(float dt,PlayerCommand[] commands)
        {
            if(phase!=Phase.Playing)return;
            // CONTINUE? holds the whole mission until the team answers or the countdown ends.
            if(continueClock>0){TickContinue(dt,commands);return;}
            SimulationTime+=dt;announcementTimer=Mathf.Max(0,announcementTimer-dt);shake=Mathf.Max(0,shake-dt*2);
            cameraX=Mathf.Max(cameraX,CameraTarget());
            float rear=Mathf.Max(.5f,cameraX-SimHalfWidth+.7f);
            for(int i=0;i<4;i++)
            {
                var p=players[i];if(p==null)continue;
                p.invulnerable=Mathf.Max(0,p.invulnerable-dt);p.specialTimer=Mathf.Max(0,p.specialTimer-dt);p.fireTimer-=dt;p.recoil=Mathf.Max(0,p.recoil-dt);p.flash=Mathf.Max(0,p.flash-dt);p.dust-=dt;p.boost=Mathf.Max(0,p.boost-dt);
                p.grenadeTimer-=dt;p.knifePose=Mathf.Max(0,p.knifePose-dt);p.throwPose=Mathf.Max(0,p.throwPose-dt);p.specialPose=Mathf.Max(0,p.specialPose-dt);
                if(p.hp<=0)
                {
                    p.downTimer-=dt;
                    // Back from the team's lives, or from his own when every hero has his.
                    if(p.downTimer<=0&&(Level.livesPerPlayer?p.lives>0:Reserves>0)){if(Level.livesPerPlayer)p.lives--;else Reserves--;Revive(p,rear,Level.respawn.inPlace);}
                    else {p.root.gameObject.SetActive(false);continue;}
                }
                PlayerCommand input=i<commands.Length?commands[i]:default;
                // A dash carries the hero forward whatever the stick says.
                float move=p.dash>0?0:Mathf.Clamp(input.move,-1,1);p.moving=Mathf.Abs(move)>.1f;if(Mathf.Abs(move)>.1f)p.facing=move>0?1:-1;
                var hero=p.Definition;bool grounded=p.position.y<=FloorAt(p.position.x,p.position.y,ForHeroes,p.dropFrom)+.001f;
                // Down crouches on the ground (slower, lower) and aims straight down in the air; up wins over it.
                p.crouching=input.down&&!input.aimUp&&grounded&&!input.jump;
                // Walls of the relief and of standing props stop every way the hero moves (PanameTerrain.cs).
                if(p.crouching){p.position.x=Advance(p.position.x,p.position.y,move*hero.crouch.speed*(p.boost>0?1.35f:1)*dt,ForHeroes);p.walk+=Mathf.Abs(move)*dt*hero.strideRate*(hero.crouch.speed/hero.speed);}
                else{p.position.x=Advance(p.position.x,p.position.y,move*hero.speed*(p.boost>0?1.35f:1)*dt,ForHeroes);p.walk+=Mathf.Abs(move)*dt*hero.strideRate;}
                // A heavy weapon's recoil pushes the hero back for a moment.
                if(p.kick!=0){p.position.x=Advance(p.position.x,p.position.y,p.kick*dt,ForHeroes);p.kick=Mathf.Abs(p.kick)<.05f?0:Mathf.Lerp(p.kick,0,Mathf.Min(1,dt*14));}
                if(p.dash>0){float step=Mathf.Min(p.dash,dt);p.dash-=step;p.position.x=Advance(p.position.x,p.position.y,p.facing*hero.special.speed*step,ForHeroes);}
                p.position.x=Mathf.Clamp(p.position.x,rear,BossActive?bossArenaX+ArenaHalfWidth-1:screenLocked?lockX+SimHalfWidth-1:CurrentMission.length);p.aimUp=input.aimUp;
                // Down + jump on a ledge drops through it; on solid ground it is a jump.
                bool drop=input.jump&&input.down&&grounded&&OnLedge(p.position.x,p.position.y,ForHeroes);
                if(drop)p.dropFrom=p.position.y;else if(input.jump&&grounded)p.vertical=hero.jump;
                if(Settle(ref p.position,ref p.vertical,29,dt,ForHeroes,p.dropFrom,grounded&&!drop&&p.vertical<=0)&&p.position.y<p.dropFrom-.01f)p.dropFrom=float.MaxValue;
                if(relief)PitFall(p);
                p.floor=FloorAt(p.position.x,p.position.y,ForHeroes,p.dropFrom);
                p.aimDown=input.down&&!input.aimUp&&p.position.y-p.floor>.1f;
                AnimateHero(p,dt);
                // An enemy at arm's length in front takes the knife instead of a bullet; the knife and the throw poses do not shoot.
                // Neither during the special's pose, whose image has no barrel.
                bool free=p.specialPose<=0;
                if(input.fire&&free&&p.fireTimer<=0&&p.knifePose<=0&&p.throwPose<=0){int victim=KnifeTarget(p);var prop=victim<0&&breakables.Count>0?KnifeProp(p):null;if(victim>=0)Stab(p,victim);else if(prop!=null)StabProp(p,prop);else FirePlayer(p);}
                if(input.grenade&&free&&p.grenadeTimer<=0&&p.grenades>0)ThrowGrenade(p);
                if(input.special&&free&&p.specialTimer<=0&&p.knifePose<=0&&p.throwPose<=0)Special(p);
                if(p.salvo>0)TickSalvo(p,dt);
                if(p.moving&&p.position.y-p.floor<.03f&&p.dust<=0){effects.Play(3,p.position+new Vector2(-p.facing*.28f,.1f),.42f,.3f,1,new Vector2(-p.facing*.18f,.2f));p.dust=.19f;}
                SyncPlayer(i);
            }
            TickRushes(dt);TickBoss(dt);if(!bossStarted)UpdateWaves(dt);TickEncounters(dt);UpdateEnemies(dt);UpdateShots(dt);TickGrenades(dt);effects.Tick(dt);TickBonuses(dt);TickHostages(dt);TickProps(dt);
            bool alive=false;foreach(var p in players)if(p!=null&&p.hp>0)alive=true;
            if(!alive&&!SpareLives)OutOfLives();
        }
        // The hero's state follows the simulation, and so does the gun drawn in it: shots leave from the cell being shown.
        static int HeroState(Player p)
        {
            var h=p.Definition;bool airborne=p.position.y-p.floor>.1f;
            if(p.specialPose>0&&h.specialPose>=0)return h.specialPose;
            if(p.knifePose>0)return h.stab;if(p.throwPose>0)return h.throwing;
            if(p.aimDown)return h.jumpDown;if(p.aimUp)return p.moving?h.aimDiagonal:h.aimUp;if(airborne)return h.jumping;
            if(p.crouching)return p.moving?h.crouchWalk:p.recoil>0?h.crouchShoot:h.crouching;
            return p.moving?h.run:h.idle;
        }
        static void AnimateHero(Player p,float dt){int state=HeroState(p);if(state!=p.animState){p.animState=state;p.animTime=0;}else p.animTime+=dt;}
        static int HeroCell(Player p)=>p.Definition.animations.Cell(p.animState<0?HeroState(p):p.animState,p.animTime,p.walk);
        static float StrideBob(Player p)=>p.moving&&p.position.y-p.floor<.1f?Mathf.Abs(Mathf.Sin(p.walk))*p.Definition.bob:0;
        // Where shots leave the gun drawn in the current frame, so tracers and flashes start at the barrel.
        static Vector2 MuzzlePoint(Player p)
        {
            var tip=p.Definition.muzzles[HeroCell(p)];
            return p.position+new Vector2(tip.x*p.facing,tip.y+StrideBob(p));
        }
        void SyncPlayer(int i)
        {
            var p=players[i];if(p==null||!p.root)return;
            p.root.position=p.position;p.root.localScale=new Vector3(p.facing,1,1);
            p.illustrated.sprite=p.Definition.animations.Sprite(HeroCell(p));
            p.illustrated.transform.localPosition=new Vector3(0,StrideBob(p),0);
            ArcadeEffects.HitFlash(p.illustrated,p.flash>0?1:0,flashProperties);
            float alpha=p.invulnerable>0&&Mathf.Sin(SimulationTime*35)>0?.45f:1;
            foreach(var r in p.renderers){var c=r.color;c.a=alpha;r.color=c;}
        }
        // The boss arena and locked waves hold the camera; otherwise it follows the leading hero.
        float CameraTarget()=>bossStarted?bossArenaX:screenLocked?lockX:Mathf.Clamp(LeaderX()+2.5f,CurrentMission.cameraMin,CurrentMission.cameraMax);
        float LeaderX(){float x=2;foreach(var p in players)if(p!=null&&p.hp>0)x=Mathf.Max(x,p.position.x);return x;}
        void Announce(string text){announcement=text;announcementTimer=2.5f;}
        void UpdateWaves(float dt)
        {
            if(Wave>=CurrentMission.waves.Length)return;
            var wave=CurrentMission.waves[Wave];
            float gate=wave.atX;
            if(!waveLive&&LeaderX()>gate-9){waveLive=true;pending=Mathf.CeilToInt((wave.baseEnemies+JoinedCount*wave.enemiesPerPlayer)*CountFactor);spawnTimer=.05f;if(wave.@lock){lockX=cameraX=Mathf.Max(cameraX,CameraTarget());screenLocked=true;}PlaceWaveHostages(Wave);Announce("VAGUE "+(Wave+1)+"  /  "+CurrentMission.waves.Length);StartWaveEncounters(Wave,false);}
            if(!waveLive)return;
            spawnTimer-=dt;
            if(pending>0&&spawnTimer<=0&&enemies.Count<Mathf.Min(42,Mathf.CeilToInt(wave.maxAlive*CountFactor)))
            {
                // A locked wave enters from the edges of the screen; a free one appears around the leading hero.
                float x=Mathf.Clamp(screenLocked?lockX+(pending%5==0?-SimHalfWidth-1.5f:SimHalfWidth+1.5f+(pending%3)*1.3f):LeaderX()+(pending%5==0?-6.5f:9+(pending%3)*1.3f),.5f,CurrentMission.length);
                var extra=CurrentMission.extraEnemy;
                SpawnEnemy(x,extra.enemy>=0&&DifficultyIndex>=extra.fromDifficulty&&pending%extra.every==0?extra.enemy:wave.enemyPattern[pending%wave.enemyPattern.Length]);pending--;spawnTimer=wave.spawnInterval*AttackFactor;
            }
            // A free wave is over once it has all appeared; a locked one when its last enemy falls; either waits for its staged enemies.
            // A wave with an objective (a barricade) ends when that falls, whatever is left of it.
            if(HasObjective(Wave)?!ObjectiveStands(Wave):pending==0&&!WaveAwaits(Wave)&&(!screenLocked||!EnemiesHold()))
            {
                pending=0;Wave++;waveLive=false;if(screenLocked&&!EncounterHolds()){screenLocked=false;goFromX=cameraX;}StartWaveEncounters(Wave-1,true);var reward=Level.waveReward;if(reward.every>0&&Wave%reward.every==0){Reserves+=reward.lives;foreach(var p in players)if(p!=null&&p.hp>0)p.hp=Mathf.Min(MaxPlayerHealth,p.hp+reward.health);}
                Announce(Wave==CurrentMission.waves.Length?"PASSAGE LIBRE  »  "+CurrentMission.exitLabel+" !":"CONTINUEZ !  »");Score+=500;
            }
        }
        void SpawnEnemy(float x,int kind)=>PlaceEnemy(CurrentMission.roster[kind],new Vector2(x,0),kind);
        // Any library enemy at a point; the horde's appear in a puff of dust, staged ones make their own entrance.
        Enemy PlaceEnemy(EnemyDefinition type,Vector2 at,int kind=-1,bool dust=true)
        {
            var e=new Enemy{position=at,kind=kind,type=type,hp=Mathf.CeilToInt(type.health*HealthFactor),attackTimer=type.firstAttack,grenadeTimer=type.grenade.first};e.maxHp=e.hp;
            e.root=Actor(actors,type.animations,type.animations.states[type.idle].cells[0],Coral,false);e.illustrated=e.root.Find("Illustration").GetComponent<SpriteRenderer>();e.root.position=e.position;enemies.Add(e);
            if(dust)effects.Play(3,e.position+Vector2.up*.12f,.6f,.3f);
            return e;
        }
        void UpdateEnemies(float dt)
        {
            for(int enemyIndex=enemies.Count-1;enemyIndex>=0;enemyIndex--)
            {
                var e=enemies[enemyIndex];
                // Left behind, or routed out of sight once nothing holds the screen.
                if(e.position.x<cameraX-SimHalfWidth-5||e.routed&&!screenLocked&&!BossActive&&Mathf.Abs(e.position.x-cameraX)>SimHalfWidth+2.5f){e.root.gameObject.SetActive(false);Destroy(e.root.gameObject);enemies.RemoveAt(enemyIndex);continue;}
                Player target=null;float best=float.MaxValue;
                foreach(var p in players)if(p!=null&&p.hp>0){float distance=Mathf.Abs(p.position.x-e.position.x);if(distance<best){best=distance;target=p;}}
                if(target==null)continue;
                e.target=target;e.flash=Mathf.Max(0,e.flash-dt);e.stagger=Mathf.Max(0,e.stagger-dt);
                float lastX=e.position.x;
                bool stood=EnemyStands(e);
                e.position.x=Mathf.Clamp(Advance(e.position.x,e.position.y,e.knockback*dt,ForEnemies),.2f,CurrentMission.length+1);e.knockback=Mathf.Lerp(e.knockback,0,Mathf.Min(1,dt*14));
                // Entering, off guard, surprised or in a panic: no attack this tick.
                if(e.mode!=EnemyActive){int shown=TickEnemyMode(e,target,lastX,dt);if(e.mode!=EnemyEntering&&EnemyGround(e,enemyIndex,dt,stood))continue;ShowEnemy(e,shown,dt);continue;}
                var type=e.type;bool shooting=e.windup>0||e.burst>0;
                // A gunman keeps the direction it aimed in until its shots are fired, so players can get behind it.
                if(!shooting)e.facing=target.position.x>=e.position.x?1:-1;
                e.attackTimer-=dt;e.attackPose=Mathf.Max(0,e.attackPose-dt);e.throwPose=Mathf.Max(0,e.throwPose-dt);e.grenadeTimer-=dt;
                // An enemy holds its strike, throw or aim pose briefly instead of running through it; one on a ledge holds it.
                // Up on the relief above its target, it walks on and off the edge rather than wait over his head.
                bool walking=!e.perched&&(best>type.keepDistance||relief&&e.position.y-target.floor>.6f)&&e.stagger<=0&&e.attackPose<=0&&!shooting;
                if(walking){float want=e.facing*type.speed*SpeedFactor*dt,from=e.position.x;e.position.x=Advance(from,e.position.y,want,ForEnemies);if(relief&&e.position.x!=from+want)Leap(e);e.walk+=dt*type.strideRate;}
                // While the screen is locked, an enemy that has walked in stays in view, so none is left to hunt off screen.
                if(screenLocked&&Mathf.Abs(lastX-lockX)<=SimHalfWidth-.6f)e.position.x=Mathf.Clamp(e.position.x,lockX-SimHalfWidth+.6f,lockX+SimHalfWidth-.6f);
                if(type.Armed&&e.stagger<=0)TickGunman(e,best,dt);
                var t=type.@throw;
                if(type.kind==EnemyBehavior.Thrower&&best<t.range&&e.attackTimer<=0&&e.stagger<=0)
                {
                    var hand=e.position+new Vector2(e.facing*type.hand.x,type.hand.y);
                    // A bottle is aimed at the hero's chest, or lower at a crouching hero.
                    Shoot(Look(t.projectile),hand,(target.position+Vector2.up*Mathf.Min(t.aimHeight,HitCenter(target).y-target.position.y)-hand).normalized*t.speed[DifficultyIndex],t.damage);
                    e.attackTimer=t.cooldown*AttackFactor;e.attackPose=type.attackPose;
                }
                var m=type.melee;
                if(!type.Armed&&!e.perched&&best<m.reach&&target.position.y-e.position.y<m.height&&target.position.y-e.position.y>-.8f&&e.attackTimer<=0&&e.stagger<=0){Damage(target,m.damage);Sound(m.Clip);e.attackTimer=m.cooldown*AttackFactor;e.attackPose=type.attackPose;}
                // A lobbed grenade, between the shots: it lands where the hero stood.
                var g=type.grenade;
                if(g.Enabled&&e.grenadeTimer<=0&&e.stagger<=0&&e.windup<=0&&e.burst<=0&&e.attackPose<=0&&best>=g.minRange&&best<=g.range){LobGrenade(e,target);e.grenadeTimer=g.cooldown*AttackFactor;e.attackPose=e.throwPose=g.pose;}
                if(EnemyGround(e,enemyIndex,dt,stood))continue;
                // Aim, then throw and attack poses win over a hit reaction, then the run and idle cycles.
                ShowEnemy(e,e.windup>0?type.aim:e.throwPose>0?type.throwing:e.attackPose>0||e.burst>0?type.attack:e.stagger>0&&type.hurt>=0?type.hurt:walking?type.run:type.idle,dt);
            }
        }
        void ShowEnemy(Enemy e,int state,float dt)
        {
            e.root.position=e.position;e.root.localScale=new Vector3(e.facing,1,1);var set=e.type.animations;
            if(state!=e.animState){e.animState=state;e.animTime=0;}else e.animTime+=dt;
            e.illustrated.sprite=set.Sprite(set.Cell(state,e.animTime,e.walk));ArcadeEffects.HitFlash(e.illustrated,e.flash>0?1:0,flashProperties);e.illustrated.transform.localRotation=Quaternion.Euler(0,0,e.stagger>0?-7:0);
            ShowEnemyStaging(e);
        }
        // Gunmen aim, freeze in their shooting frame, then fire level at chest height: jumping dodges it.
        void TickGunman(Enemy e,float best,float dt)
        {
            if(e.windup>0)
            {
                e.windup-=dt;
                if(e.windup<=0){e.burst=e.type.gun.burst;e.burstTimer=0;}
                return;
            }
            if(e.burst>0)
            {
                e.burstTimer-=dt;if(e.burstTimer>0)return;
                var g=e.type.gun;Vector2 muzzle=e.position+new Vector2(e.type.muzzle.x*e.facing,e.type.muzzle.y),aim=new Vector2(e.facing,0);float speed=g.speed[DifficultyIndex];
                // From a ledge or a window it fires down at the hero's chest instead of level.
                // So does one below or above a hero standing on the relief.
                if(e.target!=null&&(e.perched||relief&&Mathf.Abs(e.target.floor-e.position.y)>.6f)){var down=HitCenter(e.target)-muzzle;if(down.x*e.facing>.3f)aim=down.normalized;}
                // Several pellets fan out from spread degrees by spreadStep; a positive spread fans downward, so a gun held
                // above chest height still reaches a standing hero.
                if(g.pellets>1)for(int j=0;j<g.pellets;j++){var pellet=Shoot(Look(g.projectile),muzzle,(Vector2)(Quaternion.Euler(0,0,-(g.spread+j*g.spreadStep)*e.facing)*aim)*speed,g.damage);if(pellet!=null)pellet.life=g.life;}
                else{var bullet=Shoot(Look(g.projectile),muzzle,aim*speed,g.damage);if(bullet!=null)bullet.life=g.life;}
                shake=Mathf.Max(shake,g.shake);
                effects.Muzzle(muzzle,aim,g.heavyFlash);Sound(g.clip?g.clip:(g.clip=Resources.Load<AudioClip>(g.sound)));
                e.burst--;e.burstTimer=g.burstInterval;e.attackPose=g.pose;
                if(e.burst==0)e.attackTimer=g.cooldown*AttackFactor;
                return;
            }
            if(best<e.type.gun.range&&e.attackTimer<=0)e.windup=e.type.gun.aimTime*Level.gunmanAim;
        }
        void Damage(Player p,int amount)
        {
            if(bossDefeated||p.invulnerable>0||p.hp<=0)return;p.hp-=amount*Level.damageTaken;p.invulnerable=Level.hurtInvulnerable;p.flash=.075f;shake=.15f;effects.Impact(p.position+Vector2.up,-p.facing);Sound(playerHitClip);
            // A hero who falls loses the weapon in hand and comes back with the hero's own.
            if(p.hp<=0){p.downTimer=Level.respawn.delay;p.rescued=0;p.root.gameObject.SetActive(false);Disarm(p);p.dash=0;p.salvo=0;p.specialPose=0;}
        }
        static ShotLook Look(string look)=>look=="molotov"?ShotLook.Molotov:look=="tracer"?ShotLook.Tracer:ShotLook.Bullet;
        static readonly Color TracerHalo=new Color(1,.45f,.08f,.65f);
        // Projectiles only fly and collide; callers add the matching muzzle flash. friendly: fired by a hero.
        // heavy: an explosive special (blast of 2.5 around the enemy it hits); weapons set their own projectile after (Load).
        Shot Shoot(ShotLook look,Vector2 pos,Vector2 velocity,int damage,bool heavy=false,bool friendly=false)
        {
            // Bounded by the shots in flight, so the game never depends on what earlier rounds left in the pool.
            if(shots.Count>=240)return null;
            Shot s=null;
            foreach(var cached in shotPool)if(!cached.active&&cached.look==look){s=cached;break;}
            if(s==null)
            {
                s=new Shot{look=look};
                var g=new GameObject(look==ShotLook.Tracer?"Traceur":look==ShotLook.Molotov?"Cocktail Molotov":look==ShotLook.Sprite?"Projectile d'arme":"Balle de mitrailleuse");g.transform.SetParent(projectileRoot,false);s.root=g.transform;
                if(look==ShotLook.Molotov){Shape(s.root,"Bouteille",0,0,.18f,.23f,Coral,60,true);Box(s.root,"Mèche",0,.13f,.025f,.07f,Gold,61);}
                else if(look==ShotLook.Bullet){Shape(s.root,"Traînée rouge",-.08f,0,.42f,.09f,new Color(1,.3f,.15f,.7f),59,true);Shape(s.root,"Cœur brûlant",0,0,.22f,.05f,C("FFE2B8"),61,true);}
                else if(look==ShotLook.Sprite){s.sprite=g.AddComponent<SpriteRenderer>();s.sprite.sortingOrder=60;}
                else{s.halo=Shape(s.root,"Halo ambré",-.1f,0,.48f,.1f,TracerHalo,59,true).GetComponent<SpriteRenderer>();Shape(s.root,"Cœur blanc",0,0,.28f,.045f,C("FFF5CC"),61,true);}
                shotPool.Add(s);
            }
            s.active=true;s.serial=++shotSerial;s.root.gameObject.SetActive(true);s.position=pos;s.velocity=velocity;s.damage=damage;s.life=1.25f;s.age=0;s.hostile=!friendly;s.root.position=pos;
            s.weapon=null;s.drawn=null;s.owner=null;s.pierce=0;s.struck.Clear();s.blast=heavy&&friendly?2.5f:0;s.blastDamage=damage;s.blastBoss=0;s.blastAnywhere=false;if(s.halo)s.halo.color=TracerHalo;
            s.root.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg);s.root.localScale=Vector3.one*(heavy||look==ShotLook.Molotov?1.5f:1);
            shots.Add(s);return s;
        }
        // Swept segment collision avoids fast bullets passing through actors at low frame rates.
        static float SegmentDistance(Vector2 a,Vector2 b,Vector2 p)
        {Vector2 v=b-a;float t=v.sqrMagnitude>.0001f?Mathf.Clamp01(Vector2.Dot(p-a,v)/v.sqrMagnitude):0;return Vector2.Distance(p,a+t*v);}
        void UpdateShots(float dt)
        {
            for(int i=shots.Count-1;i>=0;i--)
            {
                var s=shots[i];s.age+=dt;if(s.weapon!=null)Steer(s,dt);
                var before=s.position;s.position+=s.velocity*dt;s.life-=dt;bool hit=false;int direction=s.velocity.x>=0?1:-1;
                if(s.Hostile)
                {
                    // Cover (a car, a barricade) stops enemy shots on their way.
                    if(breakables.Count>0&&CoverStops(before,s.position))hit=true;
                    else foreach(var p in players)if(p!=null&&p.hp>0&&SegmentDistance(before,s.position,HitCenter(p))<HitRadius(p)){Damage(p,s.damage);hit=true;break;}
                }
                // A beam struck everything when it was fired; it only lingers on screen.
                else if(s.weapon!=null&&s.weapon.projectile.Beam){}
                else if(ShotFreesHostage(s,before))hit=true;
                else if(HitBossOrBomb(before,s.position,s.damage)){hit=true;if(s.blast>0&&s.blastAnywhere)Blast(s.position,s.blast,s.blastDamage,0,direction,true);}
                else if(breakables.Count>0&&ShotHitsProp(s,before,direction))hit=true;
                else
                {
                    // A piercing projectile passes through enemies, each of them once; an explosive one bursts on the first.
                    float reach=Reach(s);
                    for(int e=0;e<enemies.Count;e++)
                    {
                        var enemy=enemies[e];
                        if(s.struck.Count>0&&s.struck.Contains(enemy)||!Shootable(enemy)||SegmentDistance(before,s.position,enemy.position+Vector2.up)>=enemy.type.hitRadius+reach)continue;
                        if(s.blast>0){Blast(enemy.position+Vector2.up,s.blast,s.blastDamage,s.blastBoss,direction,s.blastAnywhere);hit=true;break;}
                        int count=enemies.Count;HitEnemy(e,s.damage,direction,s.weapon?.wound??Wounds.Bullet);s.struck.Add(enemy);if(enemies.Count<count)e--;
                        if(s.pierce>=0&&s.struck.Count>s.pierce){hit=true;break;}
                    }
                }
                // A weapon's explosive goes off on the ground and at the end of its flight too.
                float ground=ShotFloor(s.position.x);
                bool grounded=s.weapon==null||!s.weapon.projectile.Beam?s.position.y<ground+(s.blastAnywhere&&s.blast>0?0:-.3f):false;
                if(!hit&&s.blastAnywhere&&s.blast>0&&(grounded||s.life<=0)){Blast(new Vector2(s.position.x,Mathf.Max(s.position.y,ground+.25f)),s.blast,s.blastDamage,s.blastBoss,direction,true);hit=true;}
                if(hit||s.life<=0||grounded){s.active=false;s.root.gameObject.SetActive(false);shots.RemoveAt(i);}else{s.root.position=s.position;if(s.weapon!=null)DrawWeaponShot(s,dt);else if(s.drawn!=null)s.sprite.sprite=s.drawn.Frame(s.age);}
            }
        }
        // wound: Wounds.Bullet, Explosion, Fire or Knife; it decides how the enemy falls.
        void HitEnemy(int index,int damage,int direction=1,int wound=Wounds.Bullet)
        {
            var e=enemies[index];e.hp-=damage;e.flash=.065f;e.stagger=.11f;e.knockback=direction*e.type.knockback;bool explosive=wound==Wounds.Explosion;
            effects.Impact(e.position+Vector2.up*1.1f,direction,e.type.armored,explosive);
            ArcadeEffects.HitFlash(e.illustrated,1,flashProperties);
            Sound(explosive?explosionClips:impactClips);shake=Mathf.Max(shake,explosive?.3f:.055f);
            if(e.hp>0){Hurt(e);return;}
            RecordKill(e.position);Score+=e.type.bonusScore;
            effects.Play(3,e.position+Vector2.up*.65f,1.5f,.5f,1,new Vector2(direction*.5f,.6f));
            if(e.type.armored)effects.Play(1,e.position+Vector2.up*.8f,1.5f,.22f);
            Fall(e,direction,wound);
            e.root.gameObject.SetActive(false);Destroy(e.root.gameObject);enemies.RemoveAt(index);Fallen(e);
        }
        // Rendering only: viewX is where the camera is drawn. When a locked wave lifts with the hero at the
        // right edge, cameraX jumps up to 12 units in one step; the view pans there instead of popping.
        float viewX=7,goSeen=-1,goSince=-10;
        void UpdateCamera(float dt)
        {
            float target=phase==Phase.Lobby?CurrentMission.cameraMin:CameraTarget();
            if(phase!=Phase.Lobby)target=Mathf.Max(cameraX,target);
            cameraX=phase==Phase.Lobby?Mathf.Lerp(cameraX,target,1-Mathf.Exp(-dt*5)):target;
            // A camera that went back (new round, new mission, lobby) or far ahead is followed at once.
            viewX=phase==Phase.Lobby||dt<=0||cameraX<viewX||cameraX>viewX+40?cameraX:Mathf.MoveTowards(viewX,cameraX,25*dt);
            if(goFromX!=goSeen){goSeen=goFromX;goSince=Time.unscaledTime;}
            UpdatePanorama(viewX,gameCamera.orthographicSize*gameCamera.aspect,CurrentMission.cameraMin,CurrentMission.cameraMax);
            gameCamera.transform.position=new Vector3(viewX+(phase==Phase.Playing?Mathf.Sin(Time.unscaledTime*70)*shake*.15f:0),3.6f,-10);
        }
        void OnGUI()
        {
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);float ox=(Screen.width-1280*scale)*.5f,oy=(Screen.height-720*scale)*.5f;
            GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,Vector3.one*scale);
            if(DrawOnlineBlocker())return;
            if(phase==Phase.Lobby){DrawLobby();return;}
            DrawHUD();DrawBossHUD();DrawOnlineBanner();DrawContinue();
            if(phase==Phase.Paused)Overlay("ON SOUFFLE UN PEU.","PAUSE",()=>phase=Phase.Playing,"REPRENDRE");
            if(phase==Phase.Won&&DrawTally()){}
            else if(phase==Phase.Won)Overlay(CurrentMission.winTitle,"TEMPS "+Clock(SimulationTime)+"  ·  "+Score.ToString("N0")+" POINTS"+(HostagesFreed>0?"  ·  "+HostagesFreed+" OTAGES LIBÉRÉS":"")+ContinueNote,NextMissionDefinition!=null?NextMission:(Action)ReturnToLobby,NextMissionDefinition!=null?"MISSION SUIVANTE  »":"CHOISIR LES HÉROS");
            if(phase==Phase.Lost)Overlay("ÇA SE JOUE À UN POIL.","PLUS DE RENFORTS  ·  "+Score.ToString("N0")+" POINTS"+ContinueNote,BeginRound,"REJOUER");
            DrawPublishRow();
        }
        readonly Dictionary<ulong,GUIStyle> labelStyles=new Dictionary<ulong,GUIStyle>();
        GUIStyle Style(int size,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft)
        {
            Color32 c=color;uint rgba=(uint)(c.r|(c.g<<8)|(c.b<<16)|(c.a<<24));ulong key=((ulong)rgba<<32)|(uint)(size|(bold?1<<12:0)|((int)align<<16));
            if(!labelStyles.TryGetValue(key,out var style)){style=new GUIStyle(GUI.skin.label){font=font,fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,alignment=align,normal={textColor=color},wordWrap=true};labelStyles.Add(key,style);}return style;
        }
        void Label(float x,float y,float w,float h,string text,int size,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft){CheckGlyphs(text);GUI.Label(new Rect(x,y,w,h),text,Style(size,color,bold,align));}
        void Panel(float x,float y,float w,float h,Color color){GUI.color=color;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
        bool Button(float x,float y,float w,float h,string text,Color color,bool dark=false)
        {
            CheckGlyphs(text);var rect=new Rect(x,y,w,h);bool hover=rect.Contains(Event.current.mousePosition);
            Panel(x+2,y+4,w,h,new Color(0,0,0,.45f));
            Panel(x,y,w,h,hover?Color.Lerp(color,Cream,.18f):color);
            Panel(x+1,y+1,w-2,2,new Color(1,1,1,.24f));Panel(x+1,y+h-2,w-2,2,new Color(0,0,0,.3f));
            var style=Style(h<30?11:17,dark?Ink:Cream,true,TextAnchor.MiddleCenter);
            bool clicked=GUI.Button(rect,text,new GUIStyle(style){normal={background=null,textColor=dark?Ink:Cream},hover={textColor=dark?Ink:Gold},active={textColor=Coral},padding=new RectOffset(6,6,3,3)});
            if(clicked)Sound(selectClip);return clicked;
        }
        // The part of the cell named by portraitCell that portraitFrame frames.
        void Portrait(float x,float y,int hero,float size=1)
        {
            var h=Heroes[hero];var set=h.animations;var art=set.Texture;if(!art)return;
            int columns=set.sheet.columns,rows=set.sheet.rows,cell=h.portraitCell;var f=h.portraitFrame;
            // A square of the cell: size of its width across, as many pixels up.
            GUI.DrawTextureWithTexCoords(new Rect(x-11*size,y-13*size,190*size,190*size),art,new Rect((cell%columns+f.x)/columns,(rows-1-cell/columns+f.y)/rows,f.size/columns,f.size*h.PortraitAspect/rows));
        }
        void DrawHUD()
        {
            for(int i=0;i<4;i++)
            {
                var p=players[i];if(!slots[i].joined||p==null)continue;
                float x=22+i*314;
                Panel(x,18,264,80,new Color(.035f,.09f,.14f,.76f));Panel(x,18,3,80,PlayerColors[i]);
                Label(x+12,22,166,22,PlayerTag(i)+"  "+p.Definition.shortName,13,PlayerColors[i],true);
                Label(x+180,23,77,20,p.specialTimer>0?"SPÉCIAL "+Mathf.CeilToInt(p.specialTimer):"SPÉCIAL",11,p.specialTimer<=0?Gold:C("7C929A"),true,TextAnchor.MiddleRight);
                for(int h=0;h<MaxPlayerHealth;h++)Panel(x+13+h*17,56,11,6,h<p.hp?PlayerColors[i]:C("344A55"));
                // The weapon's badge, then its ammunition, or its name when it never runs out.
                var w=p.weapon;if(p.hp>0&&w.HasIcon&&w.icon.Texture)GUI.DrawTextureWithTexCoords(new Rect(x+170,44,26,26),w.icon.Texture,w.icon.TexCoords);
                string ammo=w.Unlimited?w.name:(w.HasIcon?"":w.letter+"  ")+p.ammo;
                Label(x+112,45,141,25,p.hp<=0?"×":ammo,14,w.Unlimited?Cream:Gold,true,TextAnchor.MiddleRight);
                if(p.boost>0)Panel(x+3,77,261*Mathf.Clamp01(p.boost/p.boostMax),3,Gold);
                var icon=Rules.grenade.icon;if(icon.Texture)GUI.DrawTextureWithTexCoords(new Rect(x+10,61,17,17),icon.Texture,icon.TexCoords);
                Label(x+28,60,60,19,"×"+p.grenades,12,p.grenades>0?Cream:C("7C929A"),true);
                if(slots[i].pad!=null&&!slots[i].Connected)Label(x+112,22,24,22,"!",16,Coral,true);
                DrawPlayerTallies(i,x);
            }
            Label(22,104,230,28,Score.ToString("00000000"),20,Gold,true);
            Label(520,104,240,28,Clock(SimulationTime),17,Cream,true,TextAnchor.MiddleCenter);
            DrawGo();
            if(!Level.livesPerPlayer){Panel(1206,110,7,7,Cream);Panel(1203,119,13,8,Cream);Label(1223,103,48,29,"×"+Reserves,18,Cream,true);}
            if(JoinedCount>1)for(int i=0;i<4;i++)
            {
                var p=players[i];if(p==null||p.hp<=0)continue;
                var v=gameCamera.WorldToScreenPoint(p.position+Vector2.up*2.25f);
                float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f),ox=(Screen.width-1280*scale)*.5f,oy=(Screen.height-720*scale)*.5f;
                if(Online)Label((v.x-ox)/scale-60,(Screen.height-v.y-oy)/scale-12,120,23,PlayerTag(i),13,PlayerColors[i],true,TextAnchor.MiddleCenter);
                else Label((v.x-ox)/scale-14,(Screen.height-v.y-oy)/scale-9,28,18,(i+1).ToString(),12,PlayerColors[i],true,TextAnchor.MiddleCenter);
            }
        }
        // After a locked wave, a blinking GO points the way until the team has moved on.
        void DrawGo()
        {
            // Shown until the camera has gone 8 units on, and in any case for 2.5 s: the camera can jump past
            // those 8 units the moment the wave lifts when the hero waits at the right edge.
            bool shown=cameraX<goFromX+8||Time.unscaledTime<goSince+2.5f;
            if(phase!=Phase.Playing||goFromX<0||screenLocked||bossStarted||!shown||Mathf.Repeat(Time.unscaledTime,.7f)>.45f)return;
            var go=Rules.go;var t=go.Texture;if(!t)return;
            float w=t.width/(float)go.columns,h=t.height/(float)go.rows,height=Mathf.Min(150,250*h/w);
            GUI.DrawTextureWithTexCoords(new Rect(1262-height*w/h,289-height*.5f,height*w/h,height),t,go.TexCoords);
        }
        void Overlay(string title,string subtitle,Action action,string button)
        {
            Panel(-2000,-2000,5280,4720,new Color(.06f,.12f,.16f,.88f));
            Label(90,223,1100,76,title,45,Cream,true,TextAnchor.MiddleCenter);Label(90,310,1100,45,subtitle,20,Gold,true,TextAnchor.MiddleCenter);
            if(Online&&!IsHost)Label(440,393,400,58,"L'HÔTE CHOISIT LA SUITE...",18,Gold,true,TextAnchor.MiddleCenter);
            else if(Button(440,393,400,58,button,Coral,true))action();
            if(phase==Phase.Won&&NextMissionDefinition!=null)
            {
                var next=NextMissionDefinition;
                Label(90,355,1100,30,"PROCHAINE MISSION : "+next.title.ToUpperInvariant()+(string.IsNullOrEmpty(next.code)?"":"  ·  CODE "+next.code),16,Cream,true,TextAnchor.MiddleCenter);
                if((!Online||IsHost)&&Button(440,467,400,46,"RETOUR À LA SÉLECTION",C("34515C")))ReturnToLobby();
            }
            else if(phase==Phase.Won&&MissionCount>1)Label(90,355,1100,30,"CAMPAGNE TERMINÉE : BRAVO, LES LÉGENDES !",16,Cream,true,TextAnchor.MiddleCenter);
            if(Online&&IsHost&&phase==Phase.Lost&&Button(440,467,400,46,"RETOUR À LA SÉLECTION",C("34515C")))ReturnToLobby();
            if(phase==Phase.Paused)
            {
                if(Button(440,467,400,46,"RETOUR À LA SÉLECTION",C("34515C")))ReturnToLobby();
                Label(160,528,960,92,"CLAVIER   Q / A, D : bouger   ·   W / Z / HAUT : viser haut   ·   S / BAS : accroupi, tir bas en saut   ·   ESPACE : sauter\nJ : tirer, couteau au contact   ·   L : grenade   ·   K : spécial   ·   ÉCHAP : reprendre\nMANETTE   stick ou croix directionnelle : bouger, viser, s'accroupir   ·   A / CROIX : sauter\nX / CARRÉ / RT : tirer   ·   B / ROND : grenade   ·   Y / TRIANGLE / RB : spécial   ·   START : reprendre",14,Cream,false,TextAnchor.MiddleCenter);
            }
        }
    }
}
