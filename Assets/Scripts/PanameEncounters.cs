using System.Collections.Generic;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    // Staged encounters (mission "encounters"): enemies placed by the mission that make an entrance where the team can see
    // them, alongside the waves' hordes or on their own, and the mid-mission bosses.
    public sealed partial class PanameGame
    {
        sealed class Arrival {public int encounter;public EncounterSpawn spawn;public float delay;}
        // A carrier on its way in (0), letting its passengers out (1), waiting (2), driving off (3).
        sealed class Carrier
        {
            public CarrierDefinition type;public int encounter,facing,state,animState=-1;public float x,goal,timer,animTime,stride;
            public readonly List<EncounterSpawn> passengers=new List<EncounterSpawn>();public Transform root;public SpriteRenderer renderer;
        }
        // Scenery shown for a moment: an opening, a folded parachute. Visual only.
        sealed class Prop {public Transform root;public SpriteRenderer renderer;public float age,life,fadeIn;}
        readonly List<Arrival> arrivals=new List<Arrival>();
        readonly List<Carrier> carriers=new List<Carrier>();
        readonly List<Prop> props=new List<Prop>();
        bool[] encounterStarted=new bool[0];
        // encounterLock: an encounter of its own (not a wave's) holds the screen.
        bool encounterLock;
        public int ArrivalCount=>arrivals.Count;
        public int CarrierCount=>carriers.Count;
        void ResetEncounters()
        {
            arrivals.Clear();carriers.Clear();foreach(var p in props)if(p.root)Destroy(p.root.gameObject);props.Clear();encounterLock=false;voiceGate=0;
            encounterStarted=new bool[CurrentMission.encounters?.Length??0];
        }
        void TickEncounters(float dt)
        {
            var list=CurrentMission.encounters;
            if(!bossStarted)for(int i=0;i<list.Length;i++)if(!encounterStarted[i]&&list[i].wave<0&&LeaderX()>=list[i].atX)StartEncounter(i);
            for(int i=0;i<arrivals.Count;i++){var a=arrivals[i];a.delay-=dt;if(a.delay<=0){arrivals.RemoveAt(i--);Arrive(a.spawn,a.encounter,null);}}
            for(int i=carriers.Count-1;i>=0;i--)if(TickCarrier(carriers[i],dt)){Destroy(carriers[i].root.gameObject);carriers.RemoveAt(i);}
            // An encounter that held the screen lets it go when its last enemy falls, unless a wave holds it too.
            if(encounterLock&&!EncounterHolds()){encounterLock=false;if(screenLocked&&!(waveLive&&CurrentMission.waves[Wave].@lock)){screenLocked=false;goFromX=cameraX;Announce("CONTINUEZ !  »");}}
            for(int i=props.Count-1;i>=0;i--)
            {
                var p=props[i];p.age+=dt;var c=p.renderer.color;c.a=p.age<p.fadeIn?p.age/p.fadeIn:Mathf.Clamp01((p.life-p.age)/.3f);p.renderer.color=c;
                if(p.age>=p.life){Destroy(p.root.gameObject);props.RemoveAt(i);}
            }
        }
        void StartEncounter(int i)
        {
            encounterStarted[i]=true;var c=CurrentMission.encounters[i];
            if(c.HasBoss){SpawnBoss(c.boss);return;}
            if(c.@lock&&!BossActive){if(!screenLocked){lockX=cameraX=Mathf.Max(cameraX,CameraTarget());screenLocked=true;}encounterLock=true;}
            if(!string.IsNullOrEmpty(c.announce))Announce(c.announce);
            Carrier truck=null;
            if(!string.IsNullOrEmpty(c.truck.id))
            {
                // From its side, beyond the edge of the screen, to its stop.
                var type=c.truck.definition;int side=c.truck.side=="left"?-1:1;
                truck=new Carrier{type=type,encounter=i,facing=-side,goal=c.truck.x,x=ViewCentre+side*(SimHalfWidth+type.length*.5f+1)};
                truck.root=new GameObject("Véhicule • "+type.id).transform;truck.root.SetParent(actors,false);
                truck.renderer=truck.root.gameObject.AddComponent<SpriteRenderer>();truck.renderer.sortingOrder=type.animations.layer;
                carriers.Add(truck);ShowCarrier(truck,0);
            }
            foreach(var s in c.spawns)
            {
                if(s.entry=="truck")truck.passengers.Add(s);
                else arrivals.Add(new Arrival{encounter=i,spawn=s,delay=s.delay});
            }
        }
        // A wave's encounters: those that come with it when it starts, or once it is cleared.
        void StartWaveEncounters(int wave,bool cleared)
        {
            var list=CurrentMission.encounters;
            for(int i=0;i<list.Length;i++)if(!encounterStarted[i]&&list[i].wave==wave&&list[i].OnClear==cleared)StartEncounter(i);
        }
        float ViewCentre=>BossActive?bossArenaX:screenLocked?lockX:cameraX;
        bool Queued(int encounter)
        {
            foreach(var a in arrivals)if(a.encounter==encounter)return true;
            foreach(var c in carriers)if(c.encounter==encounter&&(c.passengers.Count>0||c.state==0))return true;
            return false;
        }
        bool Alive(int encounter){foreach(var e in enemies)if(e.group==encounter)return true;return false;}
        // A locked wave ends when no enemy is left to fight: one still off guard beyond the held screen (a camp ahead) waits for the team.
        bool EnemiesHold(){foreach(var e in enemies)if(e.mode!=EnemyUnaware||Mathf.Abs(e.position.x-lockX)<SimHalfWidth)return true;return false;}
        // A wave waits for its staged enemies to have arrived.
        bool WaveAwaits(int wave)
        {
            var list=CurrentMission.encounters;
            for(int i=0;i<list.Length;i++)if(list[i].wave==wave&&!list[i].OnClear&&encounterStarted[i]&&Queued(i))return true;
            return false;
        }
        // A started encounter that holds the screen until its enemies have come and fallen.
        bool EncounterHolds()
        {
            var list=CurrentMission.encounters;
            for(int i=0;i<list.Length;i++)if(encounterStarted[i]&&list[i].@lock&&!list[i].HasBoss&&(Queued(i)||Alive(i)))return true;
            return false;
        }
        // The enemy of a spawn makes its entrance; from: where a carrier lets it out.
        Enemy Arrive(EncounterSpawn s,int encounter,Vector2? from)
        {
            int entry=from.HasValue?EntryHop:System.Array.IndexOf(EncounterSpawn.Entries,s.entry);var goal=new Vector2(s.x,s.y);
            Vector2 at=goal;int facing=s.facing!=0?s.facing:LeaderX()>=s.x?1:-1;
            if(entry==EntryWalk)
            {
                bool right=s.side=="right"||string.IsNullOrEmpty(s.side)&&s.x>=LeaderX();
                at=new Vector2(ViewCentre+(right?1:-1)*(SimHalfWidth+1.2f),0);
            }
            else if(entry==EntryDoor||entry==EntryWindow)at=new Vector2(s.fromX,s.fromY);
            // A parachutist comes down from the top of the screen, his canopy above it; a fall starts above it.
            else if(entry==EntryParachute)at=new Vector2(s.x,Mathf.Max(s.y+1,ViewHalfHeight+3.6f-Stage.parachute.height*.5f));
            else if(entry==EntryDrop)at=new Vector2(s.x,ViewHalfHeight+3.6f+1);
            else if(entry==EntryHop)at=from.Value;
            var e=PlaceEnemy(s.definition,at,s.kind,entry==EntryNone);e.group=encounter;e.entry=entry;e.goal=goal;e.unaware=s.unaware;e.facing=e.rest=facing;
            if(entry==EntryNone){FinishEntry(e);return e;}
            e.mode=EnemyEntering;
            if(entry==EntryDoor||entry==EntryWindow)
            {
                e.stage=StageEmerge;e.fade=0;e.modeTime=s.look?.time??.45f;e.vertical=e.modeTime;
                if(s.look!=null)ShowProp(s.look.sprite.Sprite,new Vector2(s.fromX,s.fromY+s.look.height*.5f),s.look.height,e.modeTime+1.1f,.2f,24);
                Sound(doorClip);
            }
            else if(entry==EntryWalk)e.stage=StageWalk;
            else{e.stage=StageFall;e.vertical=entry==EntryHop?4.5f:0;if(entry==EntryParachute)Sound(chuteClip);}
            return e;
        }
        int TickEntry(Enemy e,float dt)
        {
            var type=e.type;
            if(e.stage==StageEmerge)
            {
                e.modeTime-=dt;e.fade=Mathf.Clamp01(1-e.modeTime/Mathf.Max(.01f,e.vertical));
                if(e.modeTime>0)return type.unaware;
                e.fade=1;e.vertical=e.goal.y<e.position.y-.01f?3.5f:0;e.stage=e.goal.y<e.position.y-.01f?StageFall:StageWalk;
            }
            if(e.stage==StageFall)
            {
                // A parachute comes down at its own pace; the others fall, and drift towards their place.
                if(e.entry==EntryParachute)e.position.y-=Stage.parachute.speed*dt;
                else{e.vertical-=26*dt;e.position.y+=e.vertical*dt;}
                float drift=e.entry==EntryParachute?.8f:type.speed*SpeedFactor;
                e.position.x=Mathf.MoveTowards(e.position.x,e.goal.x,drift*dt);
                if(e.position.y>e.goal.y)return e.entry==EntryParachute?type.fall:type.surprised;
                e.position.y=e.goal.y;e.vertical=0;effects.Play(3,e.position+Vector2.up*.1f,.8f,.35f);e.stage=StageWalk;
            }
            // Walks to its place.
            float d=e.goal.x-e.position.x;
            if(Mathf.Abs(d)>.08f&&e.goal.y<.05f){e.facing=d>0?1:-1;e.position.x=Mathf.MoveTowards(e.position.x,e.goal.x,type.speed*SpeedFactor*dt);e.walk+=dt*type.strideRate;return type.run;}
            FinishEntry(e);return e.mode==EnemyUnaware?type.unaware:type.idle;
        }
        void FinishEntry(Enemy e)
        {
            e.position=e.goal;e.fade=1;e.perched=e.goal.y>.05f;
            e.mode=e.unaware?EnemyUnaware:EnemyActive;
            // Off guard, it looks the way its spawn says; otherwise towards the team.
            if(e.unaware)e.facing=e.rest;else{Player near=null;float best=float.MaxValue;foreach(var p in players)if(p!=null&&p.hp>0&&Mathf.Abs(p.position.x-e.position.x)<best){best=Mathf.Abs(p.position.x-e.position.x);near=p;}if(near!=null)e.facing=near.position.x>=e.position.x?1:-1;}
        }
        bool TickCarrier(Carrier c,float dt)
        {
            var type=c.type;float before=c.x;
            if(c.state==0)
            {
                // It slows down over its last brake units.
                float d=c.goal-c.x,speed=type.speed*Mathf.Clamp(Mathf.Abs(d)/Mathf.Max(.01f,type.brake),.15f,1);
                c.x=Mathf.MoveTowards(c.x,c.goal,speed*dt);
                if(Mathf.Abs(c.goal-c.x)<.01f){c.x=c.goal;c.state=1;c.timer=type.interval;Sound(type.clip?type.clip:(type.clip=string.IsNullOrEmpty(type.sound)?null:Resources.Load<AudioClip>(type.sound)));}
            }
            else if(c.state==1)
            {
                c.timer-=dt;
                if(c.timer<=0&&c.passengers.Count>0)
                {
                    var s=c.passengers[0];c.passengers.RemoveAt(0);
                    Arrive(s,c.encounter,new Vector2(c.x+type.door.x*c.facing,type.door.y));c.timer=type.interval+s.delay;
                }
                if(c.passengers.Count==0){c.state=2;c.timer=type.wait;}
            }
            else if(c.state==2){c.timer-=dt;if(c.timer<=0)c.state=3;}
            else
            {
                c.timer+=dt;c.x+=c.facing*type.speed*Mathf.Min(1,c.timer)*dt;
                if(Mathf.Abs(c.x-cameraX)>SimHalfWidth+type.length+2)return true;
            }
            c.stride+=Mathf.Abs(c.x-before);ShowCarrier(c,dt);return false;
        }
        void ShowCarrier(Carrier c,float dt)
        {
            var set=c.type.animations;int state=c.state==1||c.state==2?c.type.open:c.type.drive;
            if(state!=c.animState){c.animState=state;c.animTime=0;}else c.animTime+=dt;
            c.renderer.sprite=set.Sprite(set.Cell(state,c.animTime,c.stride));c.root.position=new Vector3(c.x,0,0);c.root.localScale=new Vector3(c.facing,1,1);
        }
        // Visual only: an opening or a folded parachute shown for life seconds.
        void ShowProp(Sprite sprite,Vector2 at,float height,float life,float fadeIn,int layer=28)
        {
            if(!sprite)return;
            var root=new GameObject("Décor de rencontre").transform;root.SetParent(actors,false);root.position=at;root.localScale=Vector3.one*height;
            var r=root.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=layer;r.color=new Color(1,1,1,fadeIn>0?0:1);
            props.Add(new Prop{root=root,renderer=r,life=life,fadeIn=fadeIn});
        }
    }
}
