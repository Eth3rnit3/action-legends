using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Paname.PanameArt;

namespace Paname
{
    public sealed partial class PanameGame : MonoBehaviour
    {
        public static readonly string[] HeroNames={"CHUCK NORRIS","ARNOLD","JEAN-CLAUDE","SYLVESTER"};
        public static readonly string[] HeroRoles={"LE RANGER","LE COLOSSE","LE VOLTIGEUR","LE COMMANDO"};
        public static readonly string[] HeroSpecials={"Coup circulaire","Rafale lourde","Double coup sauté","Salve explosive"};
        public static readonly Color[] PlayerColors={C("F5BD56"),C("59CCBA"),C("F98A72"),C("A7AFF5")};
        static readonly float[] Speed={10.5f,9.8f,11.5f,10.7f}, Rate={.095f,.065f,.085f,.10f};
        public readonly LocalSlot[] slots={new LocalSlot{joined=true},new LocalSlot{hero=1},new LocalSlot{hero=2},new LocalSlot{hero=3}};
        public enum Phase { Lobby, Playing, Paused, Won, Lost }
        public Phase phase=Phase.Lobby;
        MissionDefinition[] missions;
        int missionIndex;
        public MissionDefinition CurrentMission=>missions[missionIndex];
        public int MissionCount=>missions?.Length??0;
        public int MissionIndex=>missionIndex;
        public void PauseFromBrowser(){if(!Online&&phase==Phase.Playing)phase=Phase.Paused;}
        public void SelectMission(int direction)
        {
            if(HostOnly(()=>SelectMission(direction))||phase!=Phase.Lobby||missions==null)return;
            missionIndex=(missionIndex+direction+missions.Length)%missions.Length;
            if(world){world.gameObject.SetActive(false);Destroy(world.gameObject);}
            world=new GameObject("Décor de mission").transform;world.SetParent(transform,false);Street(world,CurrentMission.backgrounds,CurrentMission.length);
            cameraX=CurrentMission.cameraMin;BuildLobbyActors();
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
        Transform projectileRoot;
        readonly List<Spark> sparks=new List<Spark>();
        Transform actors,world;
        ArcadeEffects effects;
        MaterialPropertyBlock flashProperties;
        public int ActiveEffects => effects?.ActiveCount ?? 0;
        public int EffectPoolCapacity => effects?.Capacity ?? 0;
        Font font;
        float spawnTimer,announcementTimer,shake;
        int pending;
        bool waveLive;
        string announcement="";
        float cameraX=7;
        AudioSource audioOut;
        System.Random random=new System.Random(2026);
        readonly PlayerCommand[] frameInputs=new PlayerCommand[4];
        public sealed class Player
        {
            public Vector2 position; public float vertical,fireTimer,specialTimer,invulnerable,downTimer,walk,recoil,flash,dust;
            public int weapon,ammo; public float boost;
            public int hp=6,hero,facing=1; public bool moving,aimUp; public Transform root,leftLeg,rightLeg; public SpriteRenderer illustrated; public SpriteRenderer[] renderers;
        }
        sealed class Enemy
        {
            public Vector2 position; public int hp,kind,facing=-1; public float attackTimer,walk,flash,stagger,knockback;
            public Transform root,leftLeg,rightLeg; public SpriteRenderer illustrated;
        }
        sealed class Shot
        {
            public Vector2 position,velocity; public float life=1.8f; public int damage; public bool hostile,explosive,active; public Transform root;
        }
        sealed class Spark { public Transform root; public Vector2 velocity; public float life; }

        void Awake()
        {
            Application.targetFrameRate=60;
            flashProperties=new MaterialPropertyBlock();
            missions=LevelCatalog.LoadAll();cameraX=CurrentMission.cameraMin;
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(!gameCamera) {var go=new GameObject("Main Camera");gameCamera=go.AddComponent<Camera>();go.tag="MainCamera";}
            gameCamera.orthographic=true; gameCamera.orthographicSize=5.8f; gameCamera.backgroundColor=C("C6DFD9"); gameCamera.transform.position=new Vector3(cameraX,3.6f,-10);
            world=new GameObject("Décor de mission").transform; world.SetParent(transform,false); Street(world,CurrentMission.backgrounds,CurrentMission.length);
            actors=new GameObject("Acteurs et effets").transform;actors.SetParent(transform,false);
            audioOut=gameObject.AddComponent<AudioSource>();audioOut.volume=.2f;
            projectileRoot=new GameObject("Projectiles • pool").transform;projectileRoot.SetParent(transform,false);
            effects=new ArcadeEffects(transform);
            for(int row=0;row<4;row++)for(int frame=0;frame<4;frame++)Frame(false,row,frame);
            for(int row=0;row<3;row++)for(int frame=0;frame<4;frame++)Frame(true,row,frame);
            for(int row=0;row<4;row++)for(int frame=0;frame<6;frame++)ArcadeEffects.Frame(row,frame);
            LoadSoundtrack();BuildLobbyActors();
        }
        void Sound(AudioClip clip){if(audioOut&&clip)audioOut.PlayOneShot(clip);}
        void ResetActors()
        {
            for(int i=actors.childCount-1;i>=0;i--){var child=actors.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
            foreach(var shot in shots){shot.active=false;shot.root.gameObject.SetActive(false);}
            Array.Clear(players,0,4);enemies.Clear();shots.Clear();sparks.Clear();effects?.Clear();ResetBonuses();ResetBoss();
        }
        void BuildLobbyActors()
        {
            ResetActors();
            for(int i=0;i<4;i++)if(slots[i].joined)SpawnPlayer(i,3+i*1.6f);
        }
        void SpawnPlayer(int i,float x)
        {
            var p=new Player{position=new Vector2(x,0),hero=slots[i].hero,hp=MaxPlayerHealth,invulnerable=2};
            p.root=Actor(actors,p.hero,PlayerColors[i]);p.root.name="J"+(i+1)+" • "+HeroNames[p.hero];
            p.illustrated=p.root.Find("Illustration")?.GetComponent<SpriteRenderer>();
            p.leftLeg=p.root.Find("Jambe arrière");p.rightLeg=p.root.Find("Jambe avant");
            p.renderers=p.root.GetComponentsInChildren<SpriteRenderer>();players[i]=p;SyncPlayer(i);
        }
        public void BeginRound()
        {
            if(OnlineRound())return;
            ResetActors();random=new System.Random(2026);Score=0;Wave=0;Reserves=StartingReserves;pending=0;waveLive=false;spawnTimer=0;SimulationTime=0;cameraX=CurrentMission.cameraMin;shake=0;
            for(int i=0;i<4;i++)if(slots[i].joined)SpawnPlayer(i,2+i*1.3f);
            phase=Phase.Playing;Announce(CurrentMission.intro);Sound(startClip);if(musicOut){musicOut.Stop();musicOut.clip=musicClip;}
        }
        public void ReturnToLobby(){if(OnlineReturn())return;phase=Phase.Lobby;cameraX=CurrentMission.cameraMin;BuildLobbyActors();}
        public void SelectHero(int playerSlot,int hero)
        {
            if(OnlineHero(hero)||phase!=Phase.Lobby||playerSlot<0||playerSlot>=4||!slots[playerSlot].joined)return;
            slots[playerSlot].hero=Mathf.Clamp(hero,0,3);BuildLobbyActors();Sound(selectClip);
        }
        void Update()
        {
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
                if(k!=null){if(k.qKey.wasPressedThisFrame||k.aKey.wasPressedThisFrame)ChangeHero(0,-1);if(k.eKey.wasPressedThisFrame||k.dKey.wasPressedThisFrame)ChangeHero(0,1);if(k.leftArrowKey.wasPressedThisFrame)ChangeHero(0,-1);if(k.rightArrowKey.wasPressedThisFrame)ChangeHero(0,1);if(k.enterKey.wasPressedThisFrame)BeginRound();}
                for(int i=0;i<4;i++)if(slots[i].joined&&slots[i].Connected)
                {
                    if(i==0&&slots[i].pad.leftShoulder.wasPressedThisFrame)SelectMission(-1);
                    if(i==0&&slots[i].pad.rightShoulder.wasPressedThisFrame)SelectMission(1);
                    if(slots[i].pad.dpad.left.wasPressedThisFrame)ChangeHero(i,-1);
                    if(slots[i].pad.dpad.right.wasPressedThisFrame)ChangeHero(i,1);
                    if(slots[i].pad.startButton.wasPressedThisFrame){BeginRound();break;}
                }
            }
            else if(phase==Phase.Playing)
            {
                for(int i=0;i<4;i++)frameInputs[i]=slots[i].joined?LocalControls.Read(slots[i],i):default;
                // Bounded substeps keep collisions stable on slower machines.
                float remaining=Mathf.Min(Time.deltaTime,.25f);
                while(remaining>0){float dt=Mathf.Min(remaining,1f/60);TickSimulation(dt,frameInputs);for(int i=0;i<4;i++){frameInputs[i].jump=false;frameInputs[i].special=false;}remaining-=dt;}
            }
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
                    if(players[index]==null){if(phase==Phase.Lobby)BuildLobbyActors();else SpawnPlayer(index,LeaderX()-1);}
                }
            }
            for(int i=0;i<4;i++)if(slots[i].Connected && slots[i].pad.startButton.wasPressedThisFrame)
            {
                if(phase==Phase.Playing)phase=Phase.Paused;else if(phase==Phase.Paused)phase=Phase.Playing;
            }
        }
        void ChangeHero(int slot,int direction)
        {
            slots[slot].hero=(slots[slot].hero+direction+4)%4;BuildLobbyActors();
        }
        public Vector2 GetPlayerPosition(int slot)=>players[slot]?.position??Vector2.zero;
        public int GetPlayerHealth(int slot)=>players[slot]?.hp??0;
        // The simulation sees a fixed 16:9 view so every browser computes the same game, whatever its window size.
        const float SimHalfWidth=5.8f*16f/9f;
        public void TickSimulation(float dt,PlayerCommand[] commands)
        {
            if(phase!=Phase.Playing)return;
            SimulationTime+=dt;announcementTimer=Mathf.Max(0,announcementTimer-dt);shake=Mathf.Max(0,shake-dt*2);
            cameraX=Mathf.Max(cameraX,bossStarted?bossArenaX:Mathf.Clamp(LeaderX()+2.5f,CurrentMission.cameraMin,CurrentMission.cameraMax));
            float rear=Mathf.Max(.5f,cameraX-SimHalfWidth+.7f);
            for(int i=0;i<4;i++)
            {
                var p=players[i];if(p==null)continue;
                p.invulnerable=Mathf.Max(0,p.invulnerable-dt);p.specialTimer=Mathf.Max(0,p.specialTimer-dt);p.fireTimer-=dt;p.recoil=Mathf.Max(0,p.recoil-dt);p.flash=Mathf.Max(0,p.flash-dt);p.dust-=dt;p.boost=Mathf.Max(0,p.boost-dt);
                if(p.hp<=0)
                {
                    p.downTimer-=dt;
                    if(p.downTimer<=0&&Reserves>0){Reserves--;p.hp=MaxPlayerHealth;p.position=new Vector2(Mathf.Clamp(LeaderX()-1,1,CurrentMission.length-1),0);p.vertical=0;p.invulnerable=3;p.root.gameObject.SetActive(true);}
                    else {p.root.gameObject.SetActive(false);continue;}
                }
                PlayerCommand input=i<commands.Length?commands[i]:default;
                float move=Mathf.Clamp(input.move,-1,1);p.moving=Mathf.Abs(move)>.1f;if(Mathf.Abs(move)>.1f)p.facing=move>0?1:-1;
                p.position.x+=move*Speed[p.hero]*(p.boost>0?1.35f:1)*dt;p.walk+=Mathf.Abs(move)*dt*13;
                p.position.x=Mathf.Clamp(p.position.x,rear,BossActive?bossArenaX+ArenaHalfWidth-1:CurrentMission.length);p.aimUp=input.aimUp;
                if(input.jump&&p.position.y<=.001f)p.vertical=p.hero==2?12.8f:11;
                p.vertical-=29*dt;p.position.y+=p.vertical*dt;if(p.position.y<0){p.position.y=0;p.vertical=0;}
                if(input.fire&&p.fireTimer<=0)FirePlayer(p,i);
                if(input.special&&p.specialTimer<=0)Special(p,i);
                if(p.moving&&p.position.y<.03f&&p.dust<=0){effects.Play(3,p.position+new Vector2(-p.facing*.28f,.1f),.42f,.3f,1,new Vector2(-p.facing*.18f,.2f));p.dust=.19f;}
                SyncPlayer(i);
            }
            TickBoss(dt);if(!bossStarted)UpdateWaves(dt);UpdateEnemies(dt);UpdateShots(dt);UpdateSparks(dt);effects.Tick(dt);TickBonuses(dt);
            bool alive=false;foreach(var p in players)if(p!=null&&p.hp>0)alive=true;
            if(!alive&&Reserves==0){phase=Phase.Lost;Announce("ON REMET ÇA ?");}
        }
        void Special(Player p,int i)
        {
            p.specialTimer=5+DifficultyIndex;shake=.35f;Sound(explosionClips);
            if(p.hero==0||p.hero==2)
            {
                effects.Play(1,p.position+new Vector2(p.facing*.7f,1),2.8f,.25f,p.facing);
                effects.Play(3,p.position+Vector2.up*.3f,1.8f,.4f);
                float radius=p.hero==0?4:3.2f;
                for(int e=enemies.Count-1;e>=0;e--)if(Vector2.Distance(p.position,enemies[e].position)<radius)HitEnemy(e,8,p.facing);
                if(BossActive&&Vector2.Distance(p.position+Vector2.up,bossPosition)<radius+1)DamageBoss(24);
                p.invulnerable=1;if(p.hero==2)p.vertical=12.8f;
            }
            else
            {
                Vector2 aim=AimDirection(p),side=new Vector2(-aim.y,aim.x);
                for(int j=-3;j<=3;j++)Shoot(p.position+Vector2.up*1.2f+aim*.85f,aim*(p.hero==1?26:19)+side*j*(p.hero==1?.8f:2),p.hero==1?4:5,false,PlayerColors[i],true);
            }
        }
        void SyncPlayer(int i)
        {
            var p=players[i];if(p==null||!p.root)return;
            p.root.position=p.position;p.root.localScale=new Vector3(p.facing,1,1);
            if(p.illustrated)
            {
                int frame=p.position.y>.1f?3:p.moving?1+((int)(p.walk*.7f)%2):0;
                p.illustrated.sprite=Frame(false,p.hero,frame);
                p.illustrated.transform.localPosition=new Vector3(0,p.moving&&p.position.y<.1f?Mathf.Abs(Mathf.Sin(p.walk))*.045f:0,0);
                ArcadeEffects.HitFlash(p.illustrated,p.flash>0?1:0,flashProperties);
            }
            if(p.leftLeg)p.leftLeg.localRotation=Quaternion.Euler(0,0,Mathf.Sin(p.walk)*16);
            if(p.rightLeg)p.rightLeg.localRotation=Quaternion.Euler(0,0,-Mathf.Sin(p.walk)*16);
            float alpha=p.invulnerable>0&&Mathf.Sin(SimulationTime*35)>0?.45f:1;
            foreach(var r in p.renderers){var c=r.color;c.a=alpha;r.color=c;}
        }
        float LeaderX(){float x=2;foreach(var p in players)if(p!=null&&p.hp>0)x=Mathf.Max(x,p.position.x);return x;}
        float AverageX(){float x=0;int n=0;foreach(var p in players)if(p!=null&&p.hp>0){x+=p.position.x;n++;}return n==0?cameraX:x/n;}
        void Announce(string text){announcement=text;announcementTimer=2.5f;}
        void UpdateWaves(float dt)
        {
            if(Wave>=CurrentMission.waves.Length)return;
            var wave=CurrentMission.waves[Wave];
            float gate=wave.atX;
            if(!waveLive&&LeaderX()>gate-9){waveLive=true;pending=Mathf.CeilToInt((wave.baseEnemies+JoinedCount*wave.enemiesPerPlayer)*CountFactors[DifficultyIndex]);spawnTimer=.05f;Announce("VAGUE "+(Wave+1)+"  /  "+CurrentMission.waves.Length);}
            if(!waveLive)return;
            spawnTimer-=dt;
            if(pending>0&&spawnTimer<=0&&enemies.Count<Mathf.Min(42,Mathf.CeilToInt(wave.maxAlive*CountFactors[DifficultyIndex])))
            {
                float x=Mathf.Clamp(LeaderX()+(pending%5==0?-6.5f:9+(pending%3)*1.3f),.5f,CurrentMission.length);
                SpawnEnemy(x,DifficultyIndex>0&&pending%4==0?1:wave.enemyPattern[pending%wave.enemyPattern.Length]);pending--;spawnTimer=wave.spawnInterval*AttackFactors[DifficultyIndex];
            }
            if(pending==0)
            {
                Wave++;waveLive=false;if((DifficultyIndex==0&&Wave%3==0)||(DifficultyIndex==1&&Wave%4==0)){Reserves++;foreach(var p in players)if(p!=null&&p.hp>0)p.hp=Mathf.Min(MaxPlayerHealth,p.hp+(DifficultyIndex==0?2:1));}
                Announce(Wave==CurrentMission.waves.Length?"PASSAGE LIBRE  →  "+CurrentMission.exitLabel+" !":"CONTINUEZ !  →");Score+=500;
            }
        }
        void SpawnEnemy(float x,int kind)
        {
            var e=new Enemy{position=new Vector2(x,0),kind=kind,hp=Mathf.CeilToInt((kind==2?6:kind==1?3:2)*HealthFactors[DifficultyIndex]),attackTimer=.7f};
            e.root=Actor(actors,0,Coral,true,kind);e.illustrated=e.root.Find("Illustration")?.GetComponent<SpriteRenderer>();e.root.position=e.position;e.leftLeg=e.root.Find("Jambe arrière");e.rightLeg=e.root.Find("Jambe avant");enemies.Add(e);
            effects.Play(3,e.position+Vector2.up*.12f,.6f,.3f);
        }
        void UpdateEnemies(float dt)
        {
            for(int enemyIndex=enemies.Count-1;enemyIndex>=0;enemyIndex--)
            {
                var e=enemies[enemyIndex];
                if(e.position.x<cameraX-SimHalfWidth-5){e.root.gameObject.SetActive(false);Destroy(e.root.gameObject);enemies.RemoveAt(enemyIndex);continue;}
                Player target=null;float best=float.MaxValue;
                foreach(var p in players)if(p!=null&&p.hp>0){float distance=Mathf.Abs(p.position.x-e.position.x);if(distance<best){best=distance;target=p;}}
                if(target==null)continue;
                e.flash=Mathf.Max(0,e.flash-dt);e.stagger=Mathf.Max(0,e.stagger-dt);
                e.position.x=Mathf.Clamp(e.position.x+e.knockback*dt,.2f,CurrentMission.length+1);e.knockback=Mathf.Lerp(e.knockback,0,Mathf.Min(1,dt*14));
                e.facing=target.position.x>=e.position.x?1:-1;e.attackTimer-=dt;
                float desired=e.kind==1?5:.6f;
                if(best>desired&&e.stagger<=0){e.position.x+=e.facing*(e.kind==0?5.8f:e.kind==1?3.6f:3.3f)*SpeedFactors[DifficultyIndex]*dt;e.walk+=dt*11;}
                if(e.kind==1&&best<10&&e.attackTimer<=0&&e.stagger<=0)
                {
                    var aim=(target.position+Vector2.up-e.position-Vector2.up*1.17f).normalized;
                    Shoot(e.position+new Vector2(e.facing*.85f,1.17f),aim*(8+DifficultyIndex*2),1,true,Coral,true);e.attackTimer=1.65f*AttackFactors[DifficultyIndex];
                }
                if(best<.9f&&target.position.y<1.35f&&e.attackTimer<=0&&e.stagger<=0){Damage(target,1);e.attackTimer=AttackFactors[DifficultyIndex];}
                e.root.position=e.position;e.root.localScale=new Vector3(e.facing,1,1);
                if(e.illustrated){int frame=best>desired?1+((int)(e.walk*.7f)%2):e.attackTimer> .8f?3:0;e.illustrated.sprite=Frame(true,e.kind,frame);ArcadeEffects.HitFlash(e.illustrated,e.flash>0?1:0,flashProperties);e.illustrated.transform.localRotation=Quaternion.Euler(0,0,e.stagger>0?-7:0);}
                if(e.leftLeg)e.leftLeg.localRotation=Quaternion.Euler(0,0,Mathf.Sin(e.walk)*18);
                if(e.rightLeg)e.rightLeg.localRotation=Quaternion.Euler(0,0,-Mathf.Sin(e.walk)*18);
            }
        }
        void Damage(Player p,int amount)
        {
            if(bossDefeated||p.invulnerable>0||p.hp<=0)return;p.hp-=amount*(DifficultyIndex==3?2:1);p.invulnerable=DifficultyIndex==0?1.2f:DifficultyIndex==1?.75f:DifficultyIndex==2?.6f:.45f;p.flash=.075f;shake=.15f;effects.Impact(p.position+Vector2.up,-p.facing);Sound(playerHitClip);
            if(p.hp<=0){p.downTimer=2.5f;p.root.gameObject.SetActive(false);}
        }
        void Shoot(Vector2 pos,Vector2 velocity,int damage,bool hostile,Color color,bool heavy=false)
        {
            Shot s=null;
            foreach(var cached in shotPool)if(!cached.active&&cached.hostile==hostile){s=cached;break;}
            if(s==null)
            {
                if(shotPool.Count>=240)return;
                s=new Shot{hostile=hostile};
                var g=new GameObject(hostile?"Projectile ennemi":"Traceur");g.transform.SetParent(projectileRoot,false);s.root=g.transform;
                if(hostile){Shape(s.root,"Grenade",0,0,.18f,.23f,Coral,60,true);Box(s.root,"Mèche",0,.13f,.025f,.07f,Gold,61);}
                else{Shape(s.root,"Halo ambré",-.1f,0,.48f,.1f,new Color(1,.45f,.08f,.65f),59,true);Shape(s.root,"Cœur blanc",0,0,.28f,.045f,C("FFF5CC"),61,true);}
                shotPool.Add(s);
            }
            s.active=true;s.root.gameObject.SetActive(true);s.position=pos;s.velocity=velocity;s.damage=damage;s.life=1.25f;s.explosive=heavy&&!hostile;s.root.position=pos;
            s.root.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg);s.root.localScale=Vector3.one*(heavy?1.5f:1);
            if(!hostile)effects.Muzzle(pos,velocity.x>=0?1:-1,heavy);
            shots.Add(s);
        }
        // Swept segment collision avoids fast bullets passing through actors at low frame rates.
        static float SegmentDistance(Vector2 a,Vector2 b,Vector2 p)
        {Vector2 v=b-a;float t=v.sqrMagnitude>.0001f?Mathf.Clamp01(Vector2.Dot(p-a,v)/v.sqrMagnitude):0;return Vector2.Distance(p,a+t*v);}
        void UpdateShots(float dt)
        {
            for(int i=shots.Count-1;i>=0;i--)
            {
                var s=shots[i];var before=s.position;s.position+=s.velocity*dt;s.life-=dt;bool hit=false;
                if(s.hostile)
                {
                    foreach(var p in players)if(p!=null&&p.hp>0&&SegmentDistance(before,s.position,p.position+Vector2.up)<.53f){Damage(p,s.damage);hit=true;break;}
                }
                else if(HitBossOrBomb(before,s.position,s.damage))hit=true;
                else for(int e=0;e<enemies.Count;e++)if(SegmentDistance(before,s.position,enemies[e].position+Vector2.up)<(enemies[e].kind==2?.68f:.5f)){
                    if(s.explosive){Vector2 blast=enemies[e].position+Vector2.up;for(int k=enemies.Count-1;k>=0;k--)if(Vector2.Distance(enemies[k].position+Vector2.up,blast)<2.5f)HitEnemy(k,s.damage,s.velocity.x>=0?1:-1,true);}
                    else HitEnemy(e,s.damage,s.velocity.x>=0?1:-1,false);
                    hit=true;break;
                }
                if(hit||s.life<=0||s.position.y<-.3f){s.active=false;s.root.gameObject.SetActive(false);shots.RemoveAt(i);}else s.root.position=s.position;
            }
        }
        void HitEnemy(int index,int damage,int direction=1,bool explosive=false)
        {
            var e=enemies[index];e.hp-=damage;e.flash=.065f;e.stagger=.11f;e.knockback=direction*(e.kind==2?1.2f:3.5f);
            effects.Impact(e.position+Vector2.up*1.1f,direction,e.kind==2,explosive);
            if(e.illustrated)ArcadeEffects.HitFlash(e.illustrated,1,flashProperties);
            Sound(explosive?explosionClips:impactClips);shake=Mathf.Max(shake,explosive?.3f:.055f);
            if(e.hp<=0)
            {
                RecordKill(e.position);if(e.kind==2)Score+=50;
                effects.Play(3,e.position+Vector2.up*.65f,1.5f,.5f,1,new Vector2(direction*.5f,.6f));
                if(e.kind==2)effects.Play(1,e.position+Vector2.up*.8f,1.5f,.22f);
                if(e.illustrated)effects.Defeat(e.illustrated,direction);
                e.root.gameObject.SetActive(false);Destroy(e.root.gameObject);enemies.RemoveAt(index);
            }
        }
        void Burst(Vector2 pos,Color color,int count)
        {
            for(int i=0;i<count;i++)
            {
                if(sparks.Count>200)break;
                var s=new Spark{life=.18f+(float)random.NextDouble()*.24f,velocity=new Vector2(((float)random.NextDouble()-.5f)*7,(float)random.NextDouble()*6)};
                s.root=Shape(actors,"Étincelle",pos.x,pos.y,.08f,.08f,color,65);sparks.Add(s);
            }
        }
        void UpdateSparks(float dt)
        {
            for(int i=sparks.Count-1;i>=0;i--){var s=sparks[i];s.life-=dt;if(s.life<=0){s.root.gameObject.SetActive(false);Destroy(s.root.gameObject);sparks.RemoveAt(i);}else{s.velocity+=Vector2.down*18*dt;s.root.position+=(Vector3)s.velocity*dt;}}
        }
        void UpdateCamera(float dt)
        {
            float target=phase==Phase.Lobby?CurrentMission.cameraMin:Mathf.Clamp(LeaderX()+2.5f,CurrentMission.cameraMin,CurrentMission.cameraMax);
            if(bossStarted&&phase!=Phase.Lobby)target=bossArenaX;
            if(phase!=Phase.Lobby)target=Mathf.Max(cameraX,target);
            cameraX=phase==Phase.Lobby?Mathf.Lerp(cameraX,target,1-Mathf.Exp(-dt*5)):target;
            float size=5.8f;
            gameCamera.orthographicSize=Mathf.Lerp(gameCamera.orthographicSize,size,1-Mathf.Exp(-dt*5));
            UpdatePanorama(cameraX,gameCamera.orthographicSize*gameCamera.aspect,CurrentMission.cameraMin,CurrentMission.cameraMax);
            gameCamera.transform.position=new Vector3(cameraX+(phase==Phase.Playing?Mathf.Sin(Time.unscaledTime*70)*shake*.15f:0),3.6f,-10);
        }
        void OnGUI()
        {
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);float ox=(Screen.width-1280*scale)*.5f,oy=(Screen.height-720*scale)*.5f;
            GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,Vector3.one*scale);
            if(DrawOnlineBlocker())return;
            if(phase==Phase.Lobby){DrawLobby();return;}
            DrawHUD();DrawBossHUD();DrawOnlineBanner();
            if(phase==Phase.Paused)Overlay("ON SOUFFLE UN PEU.","PAUSE",()=>phase=Phase.Playing,"REPRENDRE");
            if(phase==Phase.Won)Overlay(CurrentMission.winTitle,"TEMPS "+Clock(SimulationTime)+"  •  "+Score.ToString("N0")+" POINTS",ReturnToLobby,"CHOISIR LES HÉROS");
            if(phase==Phase.Lost)Overlay("ÇA SE JOUE À UN POIL.","PLUS DE RENFORTS  •  "+Score.ToString("N0")+" POINTS",BeginRound,"REJOUER");
        }
        readonly Dictionary<ulong,GUIStyle> labelStyles=new Dictionary<ulong,GUIStyle>();
        GUIStyle Style(int size,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft)
        {
            Color32 c=color;uint rgba=(uint)(c.r|(c.g<<8)|(c.b<<16)|(c.a<<24));ulong key=((ulong)rgba<<32)|(uint)(size|(bold?1<<12:0)|((int)align<<16));
            if(!labelStyles.TryGetValue(key,out var style)){style=new GUIStyle(GUI.skin.label){font=font,fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,alignment=align,normal={textColor=color},wordWrap=true};labelStyles.Add(key,style);}return style;
        }
        void Label(float x,float y,float w,float h,string text,int size,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft){GUI.Label(new Rect(x,y,w,h),text,Style(size,color,bold,align));}
        void Panel(float x,float y,float w,float h,Color color){GUI.color=color;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
        bool Button(float x,float y,float w,float h,string text,Color color,bool dark=false)
        {
            var rect=new Rect(x,y,w,h);bool hover=rect.Contains(Event.current.mousePosition);
            Panel(x+2,y+4,w,h,new Color(0,0,0,.45f));
            Panel(x,y,w,h,hover?Color.Lerp(color,Cream,.18f):color);
            Panel(x+1,y+1,w-2,2,new Color(1,1,1,.24f));Panel(x+1,y+h-2,w-2,2,new Color(0,0,0,.3f));
            var style=Style(h<30?11:17,dark?Ink:Cream,true,TextAnchor.MiddleCenter);
            bool clicked=GUI.Button(rect,text,new GUIStyle(style){normal={background=null,textColor=dark?Ink:Cream},hover={textColor=dark?Ink:Gold},active={textColor=Coral},padding=new RectOffset(6,6,3,3)});
            if(clicked)Sound(selectClip);return clicked;
        }
        void Portrait(float x,float y,int hero,Color accent,float size=1)
        {
            var art=HeroesTexture;
            if(art)
            {
                GUI.DrawTextureWithTexCoords(new Rect(x-11*size,y-13*size,190*size,190*size),art,new Rect(0,(3-hero)*.25f,.25f,.25f));
                return;
            }
            float s=size;
            Panel(x+26*s,y+107*s,104*s,59*s,hero==2?Cream:hero==0?C("A57B49"):C("3E5954"));
            Panel(x+49*s,y+32*s,65*s,83*s,C("E3AB7A"));Panel(x+45*s,y+24*s,69*s,23*s,hero==0?C("935434"):C("3F3733"));
            Panel(x+49*s,y+45*s,11*s,40*s,hero==0?C("935434"):C("3F3733"));
            Panel(x+91*s,y+66*s,7*s,7*s,Ink);Panel(x+86*s,y+57*s,18*s,4*s,Ink);
            Panel(x+80*s,y+96*s,24*s,4*s,Ink);Panel(x+29*s,y+117*s,11*s,28*s,accent);
            if(hero==0)Panel(x+65*s,y+93*s,49*s,22*s,C("935434"));
            if(hero==1)Panel(x+65*s,y+62*s,49*s,15*s,Ink);
            if(hero>=2){Panel(x+45*s,y+44*s,69*s,10*s,hero==2?Teal:Coral);Panel(x+26*s,y+47*s,24*s,8*s,hero==2?Teal:Coral);}
        }
        static readonly string[] HeroShortNames={"CHUCK","ARNOLD","JCVD","SYLVESTER"};
        void DrawHUD()
        {
            for(int i=0;i<4;i++)
            {
                var p=players[i];if(!slots[i].joined||p==null)continue;
                float x=22+i*314;
                Panel(x,18,264,62,new Color(.035f,.09f,.14f,.76f));Panel(x,18,3,62,PlayerColors[i]);
                Label(x+12,22,194,22,PlayerTag(i)+"  "+HeroShortNames[p.hero],13,PlayerColors[i],true);
                Label(x+213,23,44,20,p.specialTimer>0?"★ "+Mathf.CeilToInt(p.specialTimer):"★",12,p.specialTimer<=0?Gold:C("7C929A"),true,TextAnchor.MiddleRight);
                for(int h=0;h<MaxPlayerHealth;h++)Panel(x+13+h*17,56,11,6,h<p.hp?PlayerColors[i]:C("344A55"));
                string ammo=p.weapon==0?"∞":(p.weapon==1?"MG":p.weapon==2?"3W":"R")+"  "+p.ammo;
                Label(x+130,45,123,25,p.hp<=0?"×":ammo,14,Cream,true,TextAnchor.MiddleRight);
                if(p.boost>0)Panel(x+3,77,261*Mathf.Clamp01(p.boost/(DifficultyIndex==0?10:8-DifficultyIndex)),3,Gold);
                if(slots[i].pad!=null&&!slots[i].Connected)Label(x+112,22,24,22,"!",16,Coral,true);
            }
            Label(22,88,230,28,Score.ToString("00000000"),20,Gold,true);
            Label(520,88,240,28,Clock(SimulationTime),17,Cream,true,TextAnchor.MiddleCenter);
            Panel(1206,94,7,7,Cream);Panel(1203,103,13,8,Cream);
            Label(1223,87,48,29,"×"+Reserves,18,Cream,true);
            if(JoinedCount>1)for(int i=0;i<4;i++)
            {
                var p=players[i];if(p==null||p.hp<=0)continue;
                var v=gameCamera.WorldToScreenPoint(p.position+Vector2.up*2.25f);
                float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f),ox=(Screen.width-1280*scale)*.5f,oy=(Screen.height-720*scale)*.5f;
                if(Online)Label((v.x-ox)/scale-60,(Screen.height-v.y-oy)/scale-12,120,23,PlayerTag(i),13,PlayerColors[i],true,TextAnchor.MiddleCenter);
                else Label((v.x-ox)/scale-14,(Screen.height-v.y-oy)/scale-9,28,18,(i+1).ToString(),12,PlayerColors[i],true,TextAnchor.MiddleCenter);
            }
        }
        void Overlay(string title,string subtitle,Action action,string button)
        {
            Panel(-2000,-2000,5280,4720,new Color(.06f,.12f,.16f,.88f));
            Label(90,223,1100,76,title,45,Cream,true,TextAnchor.MiddleCenter);Label(90,310,1100,45,subtitle,20,Gold,true,TextAnchor.MiddleCenter);
            if(Online&&!IsHost)Label(440,393,400,58,"L'HÔTE CHOISIT LA SUITE…",18,Gold,true,TextAnchor.MiddleCenter);
            else if(Button(440,393,400,58,button,Coral,true))action();
            if(Online&&IsHost&&phase==Phase.Lost&&Button(440,467,400,46,"RETOUR À LA SÉLECTION",C("34515C")))ReturnToLobby();
            if(phase==Phase.Paused)
            {
                if(Button(440,467,400,46,"RETOUR À LA SÉLECTION",C("34515C")))ReturnToLobby();
                Label(220,538,840,70,"Q / A, D : bouger   •   W / Z / ↑ : viser haut   •   ESPACE : sauter\nJ : tirer   •   K : spécial   •   ÉCHAP : reprendre\nMANETTE : stick   •   A / ✕   •   X / RT   •   Y / RB",14,Cream,false,TextAnchor.MiddleCenter);
                Label(220,622,840,24,DifficultyNames[DifficultyIndex]+"   •   "+Wave+" / "+CurrentMission.waves.Length+" ASSAUTS   •   "+Reserves+" RENFORTS",12,Gold,true,TextAnchor.MiddleCenter);
            }
        }
    }
}
