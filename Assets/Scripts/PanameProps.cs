using System.Collections.Generic;
using UnityEngine;
namespace Paname
{
    // Destructible scenery placed by the mission (map.props, Content/props): the heroes' shots, blasts, knife and charges wear it
    // down through its intact, damaged and destroyed images; while it stands it may wall the way and carry those who stand on it
    // (PanameTerrain.cs) and shelter enemies from the heroes' fire, or the heroes from theirs. Broken, it may explode, scatter
    // debris, drop a reward and give points. A prop named by a wave is that wave's objective: the wave ends when it falls.
    public sealed partial class PanameGame
    {
        sealed class Breakable
        {
            public MapProp place;public PropDefinition type;public Vector2 position;public int hp,maxHp,lastShot,animState=-1;public float flash,animTime,soundAt;
            public Transform root;public SpriteRenderer renderer;
            public float Left=>position.x-type.box.width*.5f;public float Right=>position.x+type.box.width*.5f;public float Top=>position.y+type.box.height;
            public Vector2 Centre=>position+Vector2.up*(type.box.height*.5f);
        }
        readonly List<Breakable> breakables=new List<Breakable>();
        // Per prop type, the credits of its weighted rewards table (same round robin as the hostages').
        readonly Dictionary<PropDefinition,int[]> propCredit=new Dictionary<PropDefinition,int[]>();
        public int PropsStanding{get{int n=0;foreach(var b in breakables)if(b.hp>0)n++;return n;}}
        public int PropCount=>breakables.Count;
        void ResetProps(){breakables.Clear();propCredit.Clear();}
        void PlaceProps()
        {
            foreach(var place in CurrentMission.map.props??new MapProp[0])
            {
                var t=place.definition;var b=new Breakable{place=place,type=t,position=new Vector2(place.x,place.y),hp=t.health[DifficultyIndex]};b.maxHp=b.hp;
                b.root=new GameObject("Décor destructible • "+t.name).transform;b.root.SetParent(actors,false);b.root.position=b.position;b.root.localScale=new Vector3(place.flip?-1:1,1,1);
                b.renderer=b.root.gameObject.AddComponent<SpriteRenderer>();b.renderer.sortingOrder=t.animations.layer;if(ArcadeEffects.ActorMaterial)b.renderer.sharedMaterial=ArcadeEffects.ActorMaterial;
                breakables.Add(b);DrawBreakable(b,0);
            }
        }
        // Only what the team sees is hurt, and an objective only once its wave is under way.
        bool PropHittable(Breakable b)=>b.hp>0&&Mathf.Abs(b.position.x-cameraX)<SimHalfWidth+b.type.box.width*.5f&&(b.place.wave<0||Wave>b.place.wave||Wave==b.place.wave&&waveLive);
        static bool Clip(float p,float q,ref float t0,ref float t1)
        {
            if(p==0)return q>=0;float r=q/p;
            if(p<0){if(r>t1)return false;if(r>t0)t0=r;}else{if(r<t0)return false;if(r<t1)t1=r;}
            return true;
        }
        // Whether the segment a-b crosses the box (Liang-Barsky).
        static bool SegmentHitsBox(Vector2 a,Vector2 b,float left,float right,float bottom,float top)
        {
            float t0=0,t1=1;Vector2 d=b-a;
            return Clip(-d.x,a.x-left,ref t0,ref t1)&&Clip(d.x,right-a.x,ref t0,ref t1)&&Clip(-d.y,a.y-bottom,ref t0,ref t1)&&Clip(d.y,top-a.y,ref t0,ref t1);
        }
        // A hero's projectile meets the nearest prop on its way: true when it stops there (an explosive one bursts on it).
        bool ShotHitsProp(Shot s,Vector2 before,int direction)
        {
            Breakable hit=null;float r=Reach(s),best=float.MaxValue;
            foreach(var b in breakables)
            {
                if(b.lastShot==s.serial||!PropHittable(b)||!SegmentHitsBox(before,s.position,b.Left-r,b.Right+r,b.position.y-r,b.Top+r))continue;
                float d=Mathf.Abs(b.position.x-before.x);if(d<best){best=d;hit=b;}
            }
            if(hit==null)return false;
            if(s.blast>0){Blast(s.position,s.blast,s.blastDamage,s.blastBoss,direction,s.blastAnywhere);return true;}
            hit.lastShot=s.serial;HitProp(hit,s.damage,direction,s.weapon!=null&&s.weapon.wound==Wounds.Explosion);
            return s.pierce==0;
        }
        // Enemy shots stop on cover that still stands.
        bool CoverStops(Vector2 a,Vector2 b)
        {
            foreach(var p in breakables)if(p.hp>0&&p.type.cover&&SegmentHitsBox(a,b,p.Left,p.Right,p.position.y,p.Top))return true;
            return false;
        }
        void BeamHitsProps(Vector2 from,Vector2 to,int damage,int direction)
        {
            foreach(var b in breakables)if(PropHittable(b)&&SegmentHitsBox(from,to,b.Left,b.Right,b.position.y,b.Top))HitProp(b,damage,direction);
        }
        // Explosions reach the props whose box comes within their radius.
        void BlastProps(Vector2 at,float radius,int damage,int direction)
        {
            for(int i=0;i<breakables.Count;i++)
            {
                var b=breakables[i];if(!PropHittable(b))continue;
                float dx=at.x-Mathf.Clamp(at.x,b.Left,b.Right),dy=at.y-Mathf.Clamp(at.y,b.position.y,b.Top);
                if(dx*dx+dy*dy<radius*radius)HitProp(b,damage,direction,true);
            }
        }
        // The prop a grenade touches (within its radius), or null.
        Breakable PropAt(Vector2 at,float radius)
        {
            foreach(var b in breakables)if(b.hp>0&&b.type.blockKind>0&&at.x>b.Left-radius&&at.x<b.Right+radius&&at.y>b.position.y-radius&&at.y<b.Top+radius)return b;
            return null;
        }
        // The standing prop whose top an enemy is placed on.
        Breakable PropUnder(Vector2 at)
        {
            foreach(var b in breakables)if(b.hp>0&&at.x>=b.Left&&at.x<=b.Right&&(Mathf.Abs(b.Top-at.y)<.05f||b.type.platform>0&&Mathf.Abs(b.position.y+b.type.platform-at.y)<.05f))return b;
            return null;
        }
        // The nearest prop at arm's length in front of a hero on his feet.
        Breakable KnifeProp(Player p)
        {
            var k=p.Definition.knife;if(p.position.y-p.floor>k.height)return null;
            Breakable best=null;float nearest=float.MaxValue,chest=p.position.y+1;
            foreach(var b in breakables)
            {
                if(!PropHittable(b)||chest<b.position.y||chest>b.Top+.4f)continue;
                float near=p.facing>0?b.Left-p.position.x:p.position.x-b.Right;
                if(near>=-b.type.box.width&&near<=k.reach&&near<nearest){nearest=near;best=b;}
            }
            return best;
        }
        void HitProp(Breakable b,int damage,int direction,bool explosive=false)
        {
            if(b.hp<=0)return;
            b.hp-=damage;b.flash=.07f;shake=Mathf.Max(shake,explosive?.25f:.04f);
            if(Time.unscaledTime>=b.soundAt){if(b.type.HitClip)Sound(b.type.HitClip);else Sound(impactClips);b.soundAt=Time.unscaledTime+.09f;}
            if(b.hp<=0)Break(b,direction);else DrawBreakable(b,0);
        }
        void Break(Breakable b,int direction)
        {
            var t=b.type;b.hp=0;Score+=t.score;Sound(t.BreakClip);shake=Mathf.Max(shake,.22f);
            effects.Play(3,b.Centre,Mathf.Max(1.2f,t.box.width*.7f),.55f,1,Vector2.up*.6f);
            if(t.debris.Drawn)for(int i=0;i<t.debris.count;i++)
            {
                // Spread fan by fan around the hit side: drawn only.
                float side=(i%2==0?1:-1)*(.35f+.65f*((i*37)%10)/10f),up=.55f+.45f*((i*53)%10)/10f;
                effects.Piece(t.debris.Piece(i),b.Centre+new Vector2(side*t.box.width*.3f,(i%3-1)*t.box.height*.2f),new Vector2(side*t.debris.speed+direction*1.2f,up*t.debris.speed*1.4f),(i%2==0?1:-1)*(260+i*45),t.debris.size,1.4f+.1f*(i%4),b.position.y);
            }
            var x=t.explosion;
            if(x.radius>0)
            {
                // A barrel blows up: enemies, heroes too close, and the props around it, which may go up in turn.
                var at=b.Centre;effects.Play(2,at,x.radius*1.25f,.55f);effects.Play(0,at+Vector2.up*.3f,x.radius*.9f,.4f,direction,Vector2.up*1.5f,90);Sound(explosionClips);shake=Mathf.Max(shake,.4f);
                for(int k=enemies.Count-1;k>=0;k--){var e=enemies[k];if(Shootable(e)&&Vector2.Distance(e.position+Vector2.up,at)<x.radius+e.type.hitRadius)HitEnemy(k,x.damage,e.position.x>=at.x?1:-1,Wounds.Explosion);}
                if(x.heroDamage>0)foreach(var p in players)if(p!=null&&p.hp>0&&Vector2.Distance(HitCenter(p),at)<x.radius+HitRadius(p))Damage(p,x.heroDamage);
                BlastFreesHostages(at,x.radius);BlastProps(at,x.radius,x.damage,direction);
            }
            GivePropReward(b);DrawBreakable(b,0);
        }
        // The reward of the placement, else one drawn from the prop's table; none when it has neither.
        void GivePropReward(Breakable b)
        {
            int kind=-1;WeaponDefinition gun=null;
            if(b.place.bonus>=0){kind=b.place.bonus;gun=b.place.gun;}
            else if(b.type.rewards!=null&&b.type.rewards.Length>0)
            {
                var table=b.type.rewards;if(!propCredit.TryGetValue(b.type,out var credit))propCredit[b.type]=credit=new int[table.Length];
                int total=0,best=0;for(int i=0;i<table.Length;i++){credit[i]+=table[i].weight;total+=table[i].weight;if(credit[i]>credit[best])best=i;}
                credit[best]-=total;var w=table[best];kind=HostageDefinition.BonusIndex(Rules,w.bonus,w.weapon);if(!string.IsNullOrEmpty(w.weapon))gun=ContentLibrary.FindWeapon(w.weapon);
            }
            if(kind>=0)DropPickup(new Vector2(b.position.x,b.position.y+.1f),kind,gun);
        }
        bool HasObjective(int wave){foreach(var b in breakables)if(b.place.wave==wave)return true;return false;}
        bool ObjectiveStands(int wave){foreach(var b in breakables)if(b.place.wave==wave&&b.hp>0)return true;return false;}
        void TickProps(float dt)
        {
            for(int i=0;i<breakables.Count;i++){var b=breakables[i];if(b.flash>0){b.flash=Mathf.Max(0,b.flash-dt);DrawBreakable(b,dt);}else if(b.renderer&&b.renderer.enabled&&b.type.animations.states[b.animState<0?0:b.animState].fps>0)DrawBreakable(b,dt);}
        }
        // Intact, damaged below damagedAt of its health, then destroyed (or gone when it has no such image).
        void DrawBreakable(Breakable b,float dt)
        {
            if(!b.renderer)return;var t=b.type;var set=t.animations;
            int state=b.hp<=0?t.destroyed:b.hp<=b.maxHp*t.damagedAt?t.damaged:t.intact;
            b.renderer.enabled=state>=0;if(state<0)return;
            if(state!=b.animState){b.animState=state;b.animTime=0;}else b.animTime+=dt;
            b.renderer.sprite=set.Sprite(set.Cell(state,b.animTime,0));ArcadeEffects.HitFlash(b.renderer,b.flash>0?1:0,flashProperties);
        }
    }
}
