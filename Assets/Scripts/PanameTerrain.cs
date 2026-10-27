using System.Collections.Generic;
using UnityEngine;
namespace Paname
{
    // The relief of the street (mission map.terrain) and the obstacles of the props that still stand (PanameProps.cs): solid ground
    // (blocks, slopes, the bottom of pits, a car) walls the way and is stood on; thin ledges (platforms, a stall's awning) are jumped
    // onto from below and dropped through with down + jump. Everything that moves asks here where its floor is: heroes, enemies,
    // hostages, bonuses, grenades and shots. A mission without relief or props keeps the flat street of the earlier lots exactly.
    public sealed partial class PanameGame
    {
        // Who a solid stops: heroes, enemies, and the terrain's own (which stops everything, shots included).
        const int ForHeroes=1,ForEnemies=2,ForTerrain=4,ForAll=7;
        // A step this high is walked up without a jump; walking down a slope or a step this high keeps the feet on it.
        const float StepUp=.36f;
        // Below this depth in a lethal pit, a hero is lost to it.
        const float PitDepth=1.2f;
        sealed class Ground {public float left,right,height,toHeight;public bool slope;public int blocks=ForAll;public Breakable prop;public bool Standing=>prop==null||prop.hp>0;}
        readonly List<Ground> solids=new List<Ground>(),ledges=new List<Ground>();
        readonly List<MapTerrain> pits=new List<MapTerrain>();
        // False on a flat street: every query then answers what the code of the earlier lots did.
        bool relief;
        public bool HasRelief=>relief;
        void BuildTerrain()
        {
            solids.Clear();ledges.Clear();pits.Clear();
            foreach(var t in CurrentMission.map.terrain??new MapTerrain[0])
            {
                if(t.kind==MapTerrain.Platform)ledges.Add(new Ground{left=t.x,right=t.Right,height=t.height,toHeight=t.height});
                else if(t.kind==MapTerrain.Pit)pits.Add(t);
                else solids.Add(new Ground{left=t.x,right=t.Right,height=t.height,toHeight=t.kind==MapTerrain.Slope?t.toHeight:t.height,slope=t.kind==MapTerrain.Slope});
            }
            foreach(var b in breakables)
            {
                var d=b.type;float half=d.box.width*.5f,top=b.position.y+d.box.height;
                // A prop that stops the heroes only is a floor for the enemies that walk through it, once they are above it.
                if(d.blockKind>0){solids.Add(new Ground{left=b.position.x-half,right=b.position.x+half,height=top,toHeight=top,blocks=d.blockKind==1?ForHeroes:ForHeroes|ForEnemies,prop=b});if(d.blockKind==1)ledges.Add(new Ground{left=b.position.x-half,right=b.position.x+half,height=top,toHeight=top,prop=b});}
                if(d.platform>0){float w=d.LedgeWidth*.5f;ledges.Add(new Ground{left=b.position.x-w,right=b.position.x+w,height=b.position.y+d.platform,prop=b});}
            }
            relief=solids.Count+ledges.Count+pits.Count>0;
        }
        static float TopAt(Ground s,float x)=>s.slope?Mathf.Lerp(s.height,s.toHeight,(x-s.left)/(s.right-s.left)):s.height;
        MapTerrain PitAt(float x){foreach(var p in pits)if(x>p.x&&x<p.Right)return p;return null;}
        // The top of the solid ground at x for who: the street, a pit's bottom, a block, a slope, a standing prop that stops who.
        float SolidAt(float x,int who)
        {
            if(!relief)return 0;
            var pit=PitAt(x);float y=pit!=null?-pit.height:0;
            foreach(var s in solids)if((s.blocks&who)!=0&&x>=s.left&&x<=s.right&&s.Standing){float t=TopAt(s,x);if(t>y)y=t;}
            return y;
        }
        // The floor under feet at (x, y): the solid ground, or the highest ledge at or below the feet; ledges at ignore or above
        // are passed through (a hero dropping off one).
        float FloorAt(float x,float y,int who,float ignore=float.MaxValue)
        {
            if(!relief)return 0;
            float f=SolidAt(x,who);
            foreach(var l in ledges)if(x>=l.left&&x<=l.right&&l.Standing&&l.height>f&&l.height<=y+.001f&&l.height<ignore-.001f)f=l.height;
            return f;
        }
        bool OnLedge(float x,float y,int who)=>relief&&FloorAt(x,y,who)>SolidAt(x,who)+.001f;
        // Where a walk of dx from (x, y) ends: at the first wall higher than a step, else at x + dx.
        float Advance(float x,float y,float dx,int who)
        {
            if(!relief||dx==0)return x+dx;
            float to=x+dx,limit=y+StepUp;
            if(SolidAt(to,who)<=limit)return to;
            bool found=false;float stop=to;
            foreach(var s in solids)
            {
                if((s.blocks&who)==0||!s.Standing)continue;
                float edge=dx>0?s.left:s.right;
                if((dx>0?edge>x-.0001f&&edge<=to:edge<x+.0001f&&edge>=to)&&TopAt(s,edge)>limit&&(!found||(dx>0?edge<stop:edge>stop))){stop=edge;found=true;}
            }
            // The far wall of a pit, seen from its bottom.
            foreach(var p in pits)
            {
                float edge=dx>0?p.Right:p.x;
                if((dx>0?edge>x&&edge<=to:edge<x&&edge>=to)&&0>limit&&(!found||(dx>0?edge<stop:edge>stop))){stop=edge;found=true;}
            }
            if(!found)return x;
            return dx>0?Mathf.Max(x,stop-.002f):Mathf.Min(x,stop+.002f);
        }
        // Gravity and landing: true when the feet are on a floor. stick: it stood at the start of the step, so it follows the floor
        // down a slope or a step instead of falling off it.
        bool Settle(ref Vector2 at,ref float vertical,float gravity,float dt,int who,float ignore,bool stick)
        {
            float floor=FloorAt(at.x,at.y,who,ignore);
            if(vertical<=0&&(at.y<=floor||stick&&at.y-floor<=StepUp)){at.y=floor;vertical=0;return true;}
            vertical-=gravity*dt;float before=at.y;at.y+=vertical*dt;
            if(vertical<=0){float f=FloorAt(at.x,before,who,ignore);if(at.y<=f){at.y=f;vertical=0;return true;}}
            return false;
        }
        // A hero in a lethal pit: hurt, and back on the edge he came from.
        void PitFall(Player p)
        {
            var pit=PitAt(p.position.x);if(pit==null||!pit.lethal||p.position.y>-PitDepth)return;
            float x=p.facing>0?pit.x-.7f:pit.Right+.7f;
            p.position=new Vector2(x,SolidAt(x,ForHeroes));p.vertical=0;p.dropFrom=float.MaxValue;
            effects.Play(3,new Vector2(x,.3f),1.2f,.45f);
            if(pit.damage>0)Damage(p,pit.damage);
            if(p.hp>0)p.invulnerable=Mathf.Max(p.invulnerable,1.2f);
        }
        // An enemy's walk was stopped by a wall: it leaps onto it when it is low enough.
        void Leap(Enemy e)
        {
            if(e.vertical>0||e.position.y>FloorAt(e.position.x,e.position.y,ForEnemies,e.dropFrom)+.001f)return;
            float top=SolidAt(e.position.x+e.facing*.3f,ForEnemies);
            if(top-e.position.y<=e.type.terrain.climb)e.vertical=e.type.terrain.leap;
        }
        // Enemies fall off ledges and follow slopes; one placed up high (perched) stays there until the prop under it breaks.
        // True when the enemy was lost in a lethal pit (and removed).
        bool EnemyGround(Enemy e,int index,float dt,bool stood)
        {
            if(!relief)return false;
            if(e.perched){if(e.standOn==null||e.standOn.hp>0)return false;e.perched=false;e.standOn=null;stood=false;}
            var t=e.type.terrain;
            // A follower jumps up to its target's ledge, or drops off its own towards it.
            if(t.follow&&e.target!=null&&e.vertical<=0&&Mathf.Abs(e.target.position.x-e.position.x)<1.6f)
            {
                float rise=e.target.floor-e.position.y;
                if(rise>.6f&&rise<=t.climb+.3f&&stood)e.vertical=t.leap;
                else if(rise<-.6f&&stood&&OnLedge(e.position.x,e.position.y,ForEnemies))e.dropFrom=e.position.y;
            }
            if(Settle(ref e.position,ref e.vertical,26,dt,ForEnemies,e.dropFrom,stood&&e.vertical<=0)&&e.position.y<e.dropFrom-.01f)e.dropFrom=float.MaxValue;
            var pit=PitAt(e.position.x);
            if(pit==null||!pit.lethal||e.position.y>-PitDepth)return false;
            HitEnemy(index,e.hp+999,e.facing,Wounds.Explosion);return true;
        }
        bool EnemyStands(Enemy e)=>!relief||e.perched||e.vertical<=0&&e.position.y<=FloorAt(e.position.x,e.position.y,ForEnemies,e.dropFrom)+.001f;
        // Where a shot stops in the ground: the street, or the relief (props are hit, not walls, for shots).
        float ShotFloor(float x)=>relief?SolidAt(x,ForTerrain):0;
    }
}
