using System.Collections.Generic;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    // Heroes' specials, generic kinds parameterised by each hero's JSON (HeroSpecial): blast, volley, summon (an ally that
    // charges across the screen), dash (the hero charges himself) and salvo (shots in several directions at once).
    public sealed partial class PanameGame
    {
        // A charge under way: a summoned ally running across the screen, or the hero himself while he dashes (follows).
        // Each strikes an enemy once and the boss once.
        sealed class Rush
        {
            public Player owner;public HeroSpecial spec;public Vector2 position;public int facing,animState=-1;public float delay,age,walk,trail;
            public bool started,follows,bossHit;public Transform root;public SpriteRenderer renderer;
            public readonly List<Enemy> struck=new List<Enemy>();public readonly List<Breakable> smashed=new List<Breakable>();
        }
        readonly List<Rush> rushes=new List<Rush>();
        public int RushCount=>rushes.Count;
        void Special(Player p)
        {
            var s=p.Definition.special;p.specialTimer=s.cooldown+DifficultyIndex*s.cooldownPerDifficulty;
            // The two original specials keep their blast of sound and shake; the others play their own sound.
            // A salvo's sound comes with each burst.
            if(s.kind<=HeroSpecial.Volley){shake=.35f;Sound(explosionClips);}else{shake=Mathf.Max(shake,.12f);if(s.kind!=HeroSpecial.Salvo)Sound(s.Clip);}
            if(!string.IsNullOrEmpty(s.voice)){if(!bonusTakes.TryGetValue(s.voice,out var takes))bonusTakes[s.voice]=takes=TakesAt(s.voice);Speak(takes);}
            p.specialPose=s.Busy;
            if(s.kind==HeroSpecial.Blast)
            {
                effects.Play(1,p.position+new Vector2(p.facing*.7f,1),2.8f,.25f,p.facing);
                effects.Play(3,p.position+Vector2.up*.3f,1.8f,.4f);
                for(int e=enemies.Count-1;e>=0;e--)if(Vector2.Distance(p.position,enemies[e].position)<s.radius)HitEnemy(e,s.damage,p.facing,Wounds.Explosion);
                BlastFreesHostages(p.position+Vector2.up,s.radius);BlastProps(p.position+Vector2.up,s.radius,s.damage,p.facing);
                if(BossActive&&Vector2.Distance(p.position+Vector2.up,bossPosition)<s.radius+s.bossReach)DamageBoss(s.bossDamage);
                p.invulnerable=s.invulnerable;if(s.leap>0)p.vertical=s.leap;
            }
            else if(s.kind==HeroSpecial.Volley)
            {
                Vector2 aim=AimDirection(p),side=new Vector2(-aim.y,aim.x),muzzle=MuzzlePoint(p);
                for(int j=0;j<s.shots;j++)Shoot(ShotLook.Tracer,muzzle,aim*s.speed+side*(j-(s.shots-1)*.5f)*s.spread,s.damage,s.explosive,true);
                effects.Muzzle(muzzle,aim,true);
            }
            else if(s.kind==HeroSpecial.Summon)rushes.Add(new Rush{owner=p,spec=s,facing=p.facing,delay=s.delay});
            else if(s.kind==HeroSpecial.Dash)
            {
                p.dash=s.time;p.invulnerable=Mathf.Max(p.invulnerable,s.time+s.invulnerable);
                rushes.Add(new Rush{owner=p,spec=s,facing=p.facing,follows=true,started=true,position=HitCenter(p)});
            }
            else{p.salvo=s.bursts;p.salvoTimer=0;}
        }
        // The first burst leaves at the press, then one every interval while the pose lasts.
        void TickSalvo(Player p,float dt)
        {
            p.salvoTimer-=dt;if(p.salvoTimer>0)return;
            // The interval is added, not set, so that bursts keep their cadence on the 60 Hz steps.
            var h=p.Definition;var s=h.special;p.salvo--;p.salvoTimer+=s.interval;
            // Shots leave from the special image's anchors (fixed, like the pose), else from the middle of the body.
            int cell=h.specialPose>=0?h.animations.states[h.specialPose].cells[0]:-1;
            foreach(var d in s.directions)
            {
                var local=cell>=0&&!string.IsNullOrEmpty(d.anchor)&&h.animations.HasAnchor(d.anchor,cell)?h.animations.Points(d.anchor)[cell]:new Vector2(0,h.hitbox.y);
                Vector2 from=p.position+new Vector2(local.x*p.facing,local.y),aim=(Vector2)(Quaternion.Euler(0,0,d.angle)*Vector2.right);aim.x*=p.facing;
                for(int j=0;j<s.shots;j++)
                {
                    float angle=(j-(s.shots-1)*.5f)*s.spread*p.facing;
                    Shoot(ShotLook.Tracer,from,(Vector2)(Quaternion.Euler(0,0,angle)*aim)*s.speed,s.damage,s.explosive,true);
                }
                effects.Muzzle(from,aim,s.explosive);
            }
            shake=Mathf.Max(shake,.08f);Sound(s.Clip);
        }
        void TickRushes(float dt)
        {
            for(int i=rushes.Count-1;i>=0;i--)
            {
                var r=rushes[i];var s=r.spec;var owner=r.owner;
                if(r.follows)
                {
                    // The charge ends with the dash, or with the hero (fallen, or gone from an online game).
                    if(owner.dash<=0||owner.hp<=0||System.Array.IndexOf(players,owner)<0){rushes.RemoveAt(i);continue;}
                    r.position=HitCenter(owner);r.age+=dt;Strike(r,s.radius,1.2f);Trail(r,dt);continue;
                }
                var a=s.ally;
                if(!r.started)
                {
                    r.delay-=dt;if(r.delay>0)continue;
                    // From the screen's edge behind the hero, or from just behind him, at the moment it is called.
                    float x=a.from=="edge"?cameraX-r.facing*(SimHalfWidth+a.margin):owner.position.x-r.facing*a.margin;
                    r.started=true;r.position=new Vector2(x,a.y);
                    r.root=Actor(actors,a.animations,a.animations.states[a.run].cells[0],Coral,false);r.root.name="Allié • "+owner.Definition.name;
                    r.renderer=r.root.Find("Illustration").GetComponent<SpriteRenderer>();Sound(a.Clip);
                }
                float step=a.speed*dt;r.position.x+=r.facing*step;r.walk+=step;r.age+=dt;
                Strike(r,a.reach,a.height*.5f,a.height*.5f);Trail(r,dt);
                r.root.position=r.position;r.root.localScale=new Vector3(r.facing,1,1);
                r.renderer.sprite=a.animations.Sprite(a.animations.Cell(a.run,r.age,r.walk));
                float edge=cameraX+r.facing*(SimHalfWidth+a.margin+1);
                if(r.age>=a.life||(r.position.x-edge)*r.facing>0){r.root.gameObject.SetActive(false);Destroy(r.root.gameObject);rushes.RemoveAt(i);}
            }
        }
        // Every enemy whose middle is within reach ahead or behind and half a height above or below, each once; the boss once; hostages freed.
        void Strike(Rush r,float reach,float half,float rise=0)
        {
            var s=r.spec;Vector2 centre=r.position+Vector2.up*rise;
            for(int e=enemies.Count-1;e>=0;e--)
            {
                var enemy=enemies[e];if(r.struck.Contains(enemy)||!Shootable(enemy))continue;Vector2 at=enemy.position+Vector2.up;
                if(Mathf.Abs(at.x-centre.x)>=reach+enemy.type.hitRadius||Mathf.Abs(at.y-centre.y)>=half+enemy.type.hitRadius)continue;
                r.struck.Add(enemy);if(s.impact.Drawn)effects.Flipbook(s.impact.All,at,s.impact.size,s.impact.Duration);
                HitEnemy(e,s.damage,r.facing,s.woundKind);
            }
            if(!r.bossHit&&s.bossDamage>0&&BossActive&&BossWithin(centre,Mathf.Max(reach,half))){r.bossHit=true;DamageBoss(s.bossDamage);if(s.impact.Drawn)effects.Flipbook(s.impact.All,centre,s.impact.size,s.impact.Duration);}
            BlastFreesHostages(centre,reach);
            // Props in its way, each once.
            foreach(var b in breakables)if(b.hp>0&&!r.smashed.Contains(b)&&PropHittable(b)&&Mathf.Abs(b.position.x-centre.x)<reach+b.type.box.width*.5f&&centre.y+half>b.position.y&&centre.y-half<b.position.y+b.type.box.height){r.smashed.Add(b);HitProp(b,s.damage,r.facing,s.woundKind==Wounds.Explosion);}
        }
        // Drawn only: puffs of the trail left behind the charge.
        void Trail(Rush r,float dt)
        {
            var t=r.spec.trail;if(!t.Drawn)return;r.trail-=dt;if(r.trail>0)return;r.trail=r.spec.trailEvery;
            effects.Flipbook(t.All,r.position+new Vector2(-r.facing*.8f,t.y),t.size,t.Duration);
        }
    }
}
