using System;
using UnityEngine;
namespace Paname
{
    // One move of a boss, chosen in turn from its list. "gun" aims, then sweeps bullets from its muzzles;
    // "bombs" drops missiles on the heroes from its rack, at once or after a launch pose; "stomp" raises
    // a foot and sends a shockwave each way; "charge" revs up (aimState) then rushes across the arena (state), hurting heroes
    // in its way below clearance (a jump clears it); "troops" lets enemies out of its door anchor, count of them, one every
    // interval. Values in arrays are per difficulty. startSound plays as the move begins (a roar, a warning); a gun's projectile
    // and a bombs move's warning marks and impacts can be drawn from a sheet instead of the engine's bullet, bar and explosion.
    [Serializable] public sealed class BossAttack
    {
        public string type,announce,state,aimState,recoverState,sound,rack,foot,door,startSound,impactSound;
        public BossSprite projectile=new BossSprite(),warning=new BossSprite(),impact=new BossSprite();
        public string[] muzzles,enemies;
        public float clearance,reach=1.5f,spread=3;
        public int maxAlive=4;
        [NonSerialized] public EnemyDefinition[] troops;
        public float[] aimTime,interval,speed,delay,flight;
        public int[] count;
        public float duration,launch,sweep,sweepRate,aimHeight,sightWidth,spacing,stagger,arc,blastRadius,blastHeight,marker,windup,repeatWindup,recover,waveReach,waveHeight,shake,cooldown,rageCooldown=-1;
        public int rageExtra,repeat,rageRepeat,damage;
        public bool smoke;
        public static readonly string[] Types={"gun","bombs","stomp","charge","troops"};
        [NonSerialized] public int kind;
        [NonSerialized] public AudioClip clip,startClip,impactClip;
    }
    // Art of a boss move cut from a sheet: frames cells from sprite.cell on, played at fps (looping, or once for an impact),
    // drawn size world units tall with its centre y above the street.
    [Serializable] public sealed class BossSprite
    {
        public SpriteCell sprite=new SpriteCell();
        public int frames=1;
        public float fps=12,size=1,y;
        [NonSerialized] Sprite[] all;
        public bool Drawn=>sprite!=null&&!string.IsNullOrEmpty(sprite.texture);
        public float Duration=>frames/fps;
        public Sprite Frame(float time)=>sprite.At(frames>1?(int)(time*fps)%frames:0);
        public Sprite[] All{get{if(all==null){all=new Sprite[frames];for(int i=0;i<frames;i++)all[i]=sprite.At(i);}return all;}}
        public void Validate(string label)
        {
            if(!Drawn)return;sprite.Validate(label+".sprite");
            if(frames<1||frames>16||sprite.cell+frames>sprite.columns*sprite.rows)throw new ArgumentException(label+" : frames de 1 à 16, dans la grille");
            Content.Range(fps,.5f,60,label+".fps");Content.Range(size,.1f,10,label+".size");Content.Range(y,-3,10,label+".y");all=null;
        }
    }
    // Missile art: a rectangle of an image in fractions (origin bottom-left, as in Unity), drawn size world units wide,
    // with an optional outline (fractions of the rectangle) that hides whatever surrounds it in the image.
    [Serializable] public sealed class BossMissile
    {
        public string texture;
        public float x,y,width,height,size,hitRadius,tipRadius,hitLength;
        public Vector2[] outline;
        public int layer=46;
        [NonSerialized] public Sprite sprite;
    }
    [Serializable] public sealed class BossRotor {public float x,y,width,height,speed;}
    // Flyer path: x sweeps sin(t) around the arena centre, y bobs around altitude; t runs at speed per difficulty.
    // Drawing only: the art rolls by roll degrees at rollRate, and a boat's wake loops at its waterline.
    [Serializable] public sealed class BossFlight {public float[] speed;public float rageCatchUp,sweep,sweepMargin,altitude,bob,bobRate,roll,rollRate;public BossSprite wake=new BossSprite();}
    // Walker: walks toward the nearest hero until stopDistance, one footfall every stepLength.
    [Serializable] public sealed class BossWalk {public float[] speed;public float stopDistance,margin,stepLength,stepShake,entry;public string foot;}
    // count circles of radius, from (x, y) by (stepX, stepY), facing the way the art is drawn.
    [Serializable] public sealed class BossHitbox {public float x,y,stepX,stepY,radius;public int count;}
    [Serializable] public sealed class BossDeath {public float fall,spin,spreadX,spreadY;public Vector2 core;public bool spinWithFacing;}
    // A boss: "flyer" follows a flight path and attacks on the move, "walker" walks and stops to attack.
    // Its art is drawn facing left; anchors and hitbox are mirrored when it turns.
    [Serializable] public sealed class BossDefinition
    {
        public string id,extends,behavior,name,intro,rage,defeat;
        public float[] health;
        public float healthPerExtraPlayer,ragePace,rageDelay,firstAttack,smokeSize;
        // Points for destroying it.
        public int score=10000;
        // flash: the art turns white when hit; shadow: width and height of a ground shadow (0 for none).
        public bool flash;
        public Vector2 shadow;
        public BossRotor rotor=new BossRotor();
        public BossFlight flight=new BossFlight();
        public BossWalk walk=new BossWalk();
        public BossHitbox hitbox=new BossHitbox();
        public BossDeath death=new BossDeath();
        public BossMissile missile=new BossMissile();
        public AnimationSet animations=new AnimationSet();
        public BossAttack[] attacks;
        public static readonly string[] Behaviors={"flyer","walker"};
        // stomps: the boss has a stomp move, so its shockwaves (and their dust) are ticked.
        [NonSerialized] public bool walker,stomps;
        [NonSerialized] public int idle,move;
        public void Validate()
        {
            string label="Boss "+id;
            int b=Array.IndexOf(Behaviors,behavior);if(b<0)throw new ArgumentException(label+" : behavior doit être flyer ou walker");walker=b==1;
            Content.Text(name,30,label+".name");Content.Text(intro,80,label+".intro");Content.Text(rage,80,label+".rage");Content.Text(defeat,80,label+".defeat");
            Content.PerDifficulty(health,10,100000,label+".health");Content.Range(healthPerExtraPlayer,0,5,label+".healthPerExtraPlayer");
            Content.Range(ragePace,.1f,2,label+".ragePace");Content.Range(rageDelay,0,10,label+".rageDelay");Content.Range(firstAttack,0,10,label+".firstAttack");Content.Range(smokeSize,0,5,label+".smokeSize");
            if(score<0||score>100000)throw new ArgumentException(label+" : score entre 0 et 100000");
            if(!(shadow.x>=0&&shadow.x<=20&&shadow.y>=0&&shadow.y<=5))throw new ArgumentException(label+" : shadow invalide");
            animations.Validate(label,walker?new[]{"idle","walk"}:new[]{"idle"});idle=animations.Find("idle");move=animations.Find("walk",idle);Anchor("smoke",label);
            if(walker){var w=walk;Content.PerDifficulty(w.speed,.1f,30,label+".walk.speed");Content.Range(w.stopDistance,0,20,label+".walk.stopDistance");Content.Range(w.margin,0,10,label+".walk.margin");Content.Range(w.stepLength,.1f,10,label+".walk.stepLength");Content.Range(w.stepShake,0,1,label+".walk.stepShake");Content.Range(w.entry,-8,10,label+".walk.entry");Anchor(w.foot,label+".walk");}
            else
            {
                var f=flight;Content.PerDifficulty(f.speed,.05f,20,label+".flight.speed");Content.Range(f.rageCatchUp,0,5,label+".flight.rageCatchUp");Content.Range(f.sweep,0,20,label+".flight.sweep");
                Content.Range(f.sweepMargin,0,20,label+".flight.sweepMargin");Content.Range(f.altitude,0,12,label+".flight.altitude");Content.Range(f.bob,0,5,label+".flight.bob");Content.Range(f.bobRate,0,20,label+".flight.bobRate");
                Content.Range(f.roll,0,20,label+".flight.roll");Content.Range(f.rollRate,0,20,label+".flight.rollRate");f.wake??=new BossSprite();f.wake.Validate(label+".flight.wake");
                Content.Range(rotor.width,0,20,label+".rotor.width");Content.Range(rotor.height,0,2,label+".rotor.height");Content.Range(rotor.speed,0,500,label+".rotor.speed");
            }
            var h=hitbox;if(h.count<1||h.count>12)throw new ArgumentException(label+" : hitbox.count entre 1 et 12");Content.Range(h.radius,.1f,10,label+".hitbox.radius");
            if(!(Mathf.Abs(h.x)<=20&&Mathf.Abs(h.y)<=20&&Mathf.Abs(h.stepX)<=10&&Mathf.Abs(h.stepY)<=10))throw new ArgumentException(label+" : hitbox hors limites");
            Content.Range(death.fall,0,10,label+".death.fall");Content.Range(death.spin,-360,360,label+".death.spin");Content.Range(death.spreadX,0,10,label+".death.spreadX");Content.Range(death.spreadY,0,10,label+".death.spreadY");
            if(attacks==null||attacks.Length<1||attacks.Length>12)throw new ArgumentException(label+" : une à douze attaques");
            bool bombs=false;
            foreach(var a in attacks)
            {
                if(a==null||(a.kind=Array.IndexOf(BossAttack.Types,a.type))<0)throw new ArgumentException(label+" : attaque gun, bombs, stomp, charge ou troops");
                string l=label+" : attaque "+a.type;Content.Text(a.announce,80,l+".announce");
                Content.Range(a.cooldown,0,20,l+".cooldown");if(a.rageCooldown!=-1)Content.Range(a.rageCooldown,0,20,l+".rageCooldown");
                foreach(var state in new[]{a.state,a.aimState,a.recoverState})if(!string.IsNullOrEmpty(state)&&animations.Find(state)<0)throw new ArgumentException(l+" : état "+state+" absent des animations");
                foreach(var (sound,field) in new[]{(a.sound,"sound"),(a.startSound,"startSound"),(a.impactSound,"impactSound")})
                    if(!string.IsNullOrEmpty(sound)){Content.Path(sound,"Audio/",l+"."+field);Content.Require<AudioClip>(sound,l+" : son "+field);}
                a.startClip=null;a.impactClip=null;
                a.projectile.Validate(l+".projectile");a.warning.Validate(l+".warning");a.impact.Validate(l+".impact");
                if(a.kind!=0&&a.projectile.Drawn)throw new ArgumentException(l+" : projectile ne sert qu'à une attaque gun");
                if(a.kind!=1&&(a.warning.Drawn||a.impact.Drawn||!string.IsNullOrEmpty(a.impactSound)))throw new ArgumentException(l+" : warning, impact et impactSound ne servent qu'à une attaque bombs");
                if(a.kind==0)
                {
                    Content.PerDifficulty(a.aimTime,0,10,l+".aimTime");Content.PerDifficulty(a.interval,.02f,5,l+".interval");Content.PerDifficulty(a.speed,1,60,l+".speed");
                    Content.Range(a.duration,.05f,20,l+".duration");Content.Range(a.sweep,0,20,l+".sweep");Content.Range(a.sweepRate,0,50,l+".sweepRate");Content.Range(a.aimHeight,0,5,l+".aimHeight");Content.Range(a.sightWidth,0,20,l+".sightWidth");
                    if(a.muzzles==null||a.muzzles.Length<1||a.muzzles.Length>8)throw new ArgumentException(l+" : un à huit muzzles");
                    foreach(var m in a.muzzles)Anchor(m,l);
                    if(a.damage<1||a.damage>10)throw new ArgumentException(l+" : damage entre 1 et 10");
                }
                else if(a.kind==1)
                {
                    bombs=true;Content.PerDifficulty(a.count,1,20,l+".count");Content.PerDifficulty(a.delay,0,10,l+".delay");Content.PerDifficulty(a.flight,.05f,10,l+".flight");
                    if(a.rageExtra<0||a.rageExtra>10)throw new ArgumentException(l+" : rageExtra entre 0 et 10");
                    Content.Range(a.duration,0,20,l+".duration");Content.Range(a.launch,0,20,l+".launch");Content.Range(a.spacing,0,10,l+".spacing");Content.Range(a.stagger,0,5,l+".stagger");Content.Range(a.arc,-20,20,l+".arc");
                    Content.Range(a.blastRadius,.1f,10,l+".blastRadius");Content.Range(a.blastHeight,0,10,l+".blastHeight");Content.Range(a.marker,0,20,l+".marker");Anchor(a.rack,l);
                    if(a.damage<1||a.damage>10)throw new ArgumentException(l+" : damage entre 1 et 10");
                }
                else if(a.kind==3)
                {
                    if(!walker)throw new ArgumentException(l+" : charge est une attaque d'un boss au sol (walker)");
                    Content.Range(a.windup,0,10,l+".windup");Content.Range(a.recover,0,10,l+".recover");Content.PerDifficulty(a.speed,1,60,l+".speed");
                    Content.Range(a.clearance,0,6,l+".clearance");Content.Range(a.reach,.2f,8,l+".reach");Content.Range(a.shake,0,1,l+".shake");
                    if(a.damage<1||a.damage>10)throw new ArgumentException(l+" : damage entre 1 et 10");
                }
                else if(a.kind==4)
                {
                    if(a.enemies==null||a.enemies.Length<1||a.enemies.Length>6)throw new ArgumentException(l+" : un à six enemies");
                    a.troops=Array.ConvertAll(a.enemies,ContentLibrary.FindEnemy);if(Array.IndexOf(a.troops,null)>=0)throw new ArgumentException(l+" : ennemi inconnu dans enemies");
                    Content.PerDifficulty(a.count,1,8,l+".count");Content.PerDifficulty(a.interval,.05f,5,l+".interval");Content.Range(a.duration,0,20,l+".duration");Content.Range(a.spread,0,10,l+".spread");
                    if(a.maxAlive<1||a.maxAlive>12)throw new ArgumentException(l+" : maxAlive entre 1 et 12");Anchor(a.door,l);
                }
                else
                {
                    Content.Range(a.windup,0,10,l+".windup");Content.Range(a.repeatWindup,0,10,l+".repeatWindup");Content.Range(a.recover,0,10,l+".recover");Content.PerDifficulty(a.speed,1,60,l+".speed");
                    Content.Range(a.waveReach,.05f,5,l+".waveReach");Content.Range(a.waveHeight,0,5,l+".waveHeight");Content.Range(a.shake,0,1,l+".shake");Anchor(a.foot,l);
                    if(a.repeat<1||a.repeat>6||a.rageRepeat<1||a.rageRepeat>6||a.damage<1||a.damage>10)throw new ArgumentException(l+" : repeat et rageRepeat entre 1 et 6, damage entre 1 et 10");
                }
            }
            stomps=Array.FindIndex(attacks,x=>x.kind==2)>=0;
            if(bombs)
            {
                var m=missile;Content.Path(m.texture,"Art/",label+".missile.texture");Content.Require<Texture2D>(m.texture,label+" : image du missile");
                if(!(m.x>=0&&m.y>=0&&m.width>0&&m.height>0&&m.x+m.width<=1.0001f&&m.y+m.height<=1.0001f))throw new ArgumentException(label+" : missile.x, y, width et height en fractions de l'image");
                Content.Range(m.size,.1f,10,label+".missile.size");Content.Range(m.hitRadius,.05f,3,label+".missile.hitRadius");Content.Range(m.tipRadius,0,3,label+".missile.tipRadius");Content.Range(m.hitLength,0,3,label+".missile.hitLength");
                if(m.outline!=null&&m.outline.Length>0){if(m.outline.Length<3||m.outline.Length>64)throw new ArgumentException(label+" : missile.outline de 3 à 64 points");foreach(var p in m.outline)if(!(p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1))throw new ArgumentException(label+" : missile.outline en fractions du rectangle");}
            }
        }
        void Anchor(string name,string label){if(string.IsNullOrEmpty(name)||animations.anchors==null||Array.FindIndex(animations.anchors,a=>a.name==name)<0)throw new ArgumentException(label+" : ancrage "+name+" absent des animations");}
        public BossAttack FirstBombs=>Array.Find(attacks,a=>a.kind==1);
        // Missile art cut from its image; the outline replaces the rectangle mesh.
        public Sprite MissileSprite()
        {
            var m=missile;if(m.sprite)return m.sprite;var texture=Resources.Load<Texture2D>(m.texture);if(!texture)return null;
            var rect=new Rect(texture.width*m.x,texture.height*m.y,texture.width*m.width,texture.height*m.height);float ppu=rect.width/m.size;
            var sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),ppu,0,SpriteMeshType.FullRect);
            if(m.outline!=null&&m.outline.Length>=3)
            {
                var vertices=new Vector2[m.outline.Length+1];var triangles=new ushort[m.outline.Length*3];
                for(int i=0;i<m.outline.Length;i++)
                {
                    vertices[i+1]=new Vector2((m.outline[i].x-.5f)*rect.width/ppu,(m.outline[i].y-.5f)*rect.height/ppu);
                    triangles[i*3]=0;triangles[i*3+1]=(ushort)(i+1);triangles[i*3+2]=(ushort)((i+1)%m.outline.Length+1);
                }
                sprite.OverrideGeometry(vertices,triangles);
            }
            return m.sprite=sprite;
        }
    }
}
