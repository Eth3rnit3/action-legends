using System.Collections.Generic;
using UnityEngine;
namespace Paname
{
    // The hero's close-range tools: the knife the fire button turns into at arm's length, and grenades,
    // thrown in an arc from a limited stock (rules of the mission, Content/rules).
    public sealed partial class PanameGame
    {
        // hostile: lobbed by an enemy (spec): it only hurts heroes.
        sealed class Grenade {public Vector2 position,velocity;public float age;public int bounces;public Transform root;public bool hostile;public EnemyGrenade spec;}
        readonly List<Grenade> thrown=new List<Grenade>();
        GameRules Rules=>CurrentMission.ruleSet;
        // Where enemy shots hit a hero, lower while crouching.
        static Vector2 HitCenter(Player p)=>p.position+Vector2.up*(p.crouching?p.Definition.crouch.y:p.Definition.hitbox.y);
        static float HitRadius(Player p)=>p.crouching?p.Definition.crouch.radius:p.Definition.hitbox.radius;
        // The nearest enemy within the knife's reach in front of the hero, or -1.
        int KnifeTarget(Player p)
        {
            var k=p.Definition.knife;if(p.position.y>k.height)return -1;
            int best=-1;float nearest=float.MaxValue;
            for(int e=0;e<enemies.Count;e++){float d=(enemies[e].position.x-p.position.x)*p.facing;if(d>=-.25f&&d<=k.reach&&d<nearest&&Mathf.Abs(enemies[e].position.y-p.position.y)<1.2f){nearest=d;best=e;}}
            return best;
        }
        void Stab(Player p,int victim)
        {
            var k=p.Definition.knife;p.knifePose=k.pose;p.fireTimer=k.cooldown;shake=Mathf.Max(shake,.08f);
            Sound(k.clip?k.clip:(k.clip=Resources.Load<AudioClip>(k.sound)));HitEnemy(victim,k.damage,p.facing,Wounds.Knife);
        }
        void ThrowGrenade(Player p)
        {
            var g=Rules.grenade;var hero=p.Definition;p.grenades--;p.grenadeTimer=g.cooldown;p.throwPose=g.pose;p.knifePose=0;
            Vector2 at=p.position+new Vector2(hero.hand.x*p.facing,hero.hand.y);
            var root=new GameObject("Grenade").transform;root.SetParent(actors,false);root.position=at;root.localScale=Vector3.one*g.size;
            var r=root.gameObject.AddComponent<SpriteRenderer>();r.sprite=g.sprite.Sprite;r.sortingOrder=62;
            thrown.Add(new Grenade{position=at,velocity=new Vector2(g.throwX*p.facing,g.throwY),root=root});
            Sound(g.throwClip?g.throwClip:(g.throwClip=Resources.Load<AudioClip>(g.throwSound)));
        }
        void TickGrenades(float dt)
        {
            // The grenade fills about half of its drawn cell: its body is a quarter of size across.
            var g=Rules.grenade;float floor=g.size*.25f;
            for(int i=thrown.Count-1;i>=0;i--)
            {
                var n=thrown[i];var before=n.position;n.age+=dt;n.velocity.y-=g.gravity*dt;n.position+=n.velocity*dt;
                bool boom=n.age>=(n.hostile?n.spec.fuse:g.fuse);
                if(n.hostile){foreach(var p in players)if(!boom&&p!=null&&p.hp>0&&SegmentDistance(before,n.position,HitCenter(p))<HitRadius(p)+floor)boom=true;}
                else
                {
                    boom|=BossActive&&BossWithin(n.position,floor);
                    for(int e=0;e<enemies.Count&&!boom;e++)if(SegmentDistance(before,n.position,enemies[e].position+Vector2.up)<enemies[e].type.hitRadius+floor)boom=true;
                }
                // It bounces off the street a set number of times, then goes off where it lands.
                if(!boom&&n.position.y<=floor){if(n.bounces>=(n.hostile?n.spec.bounces:g.bounces))boom=true;else{n.position.y=floor;n.velocity=new Vector2(n.velocity.x*g.friction,-n.velocity.y*g.bounce);n.bounces++;}}
                if(boom&&n.hostile){ExplodeHostile(n.position,n.spec);n.root.gameObject.SetActive(false);Destroy(n.root.gameObject);thrown.RemoveAt(i);continue;}
                if(boom){Explode(n.position);n.root.gameObject.SetActive(false);Destroy(n.root.gameObject);thrown.RemoveAt(i);continue;}
                n.root.position=n.position;n.root.localRotation=Quaternion.Euler(0,0,-n.age*540*(n.velocity.x<0?-1:1));
            }
        }
        // An enemy's grenade hurts the heroes within its radius, nobody else.
        void ExplodeHostile(Vector2 at,EnemyGrenade spec)
        {
            effects.Play(2,at+Vector2.up*.5f,spec.radius*1.2f,.45f);effects.Play(3,at+Vector2.up*.3f,spec.radius*.8f,.6f,1,Vector2.up*.5f);
            Sound(explosionClips);shake=Mathf.Max(shake,.25f);
            foreach(var p in players)if(p!=null&&p.hp>0&&Vector2.Distance(HitCenter(p),at)<spec.radius+HitRadius(p))Damage(p,spec.damage);
        }
        // Hurts every enemy and the boss within the radius, and blows up the boss's missiles caught in it.
        void Explode(Vector2 at)
        {
            var g=Rules.grenade;
            effects.Play(2,at+Vector2.up*.6f,g.radius*1.15f,.5f);effects.Play(3,at+Vector2.up*.3f,g.radius*.8f,.6f,1,Vector2.up*.5f);
            Sound(explosionClips);shake=Mathf.Max(shake,.3f);
            for(int k=enemies.Count-1;k>=0;k--){var e=enemies[k];if(Vector2.Distance(e.position+Vector2.up,at)<g.radius+e.type.hitRadius)HitEnemy(k,g.damage,e.position.x>=at.x?1:-1,Wounds.Explosion);}
            if(BossActive&&BossWithin(at,g.radius))DamageBoss(g.bossDamage);
            BlastFreesHostages(at,g.radius);
            if(g.destroysBombs&&BossActive)for(int i=bossBombs.Count-1;i>=0;i--)if(bossBombs[i].launched&&Vector2.Distance(bossBombs[i].position,at)<g.radius)RemoveBossBomb(i,false);
        }
    }
}
