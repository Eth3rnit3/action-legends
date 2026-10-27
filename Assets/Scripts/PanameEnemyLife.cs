using System.Collections.Generic;
using UnityEngine;
namespace Paname
{
    // Enemies with a life of their own (lot 2b): placed ones come in by a side, a door, a window, a parachute or a truck
    // (PanameEncounters.cs), may wait off guard until they spot the team, jump in surprise, panic and run, and fall in a
    // way that matches what hit them. Each state is optional in an enemy's JSON: without it, the nearest one plays.
    public sealed partial class PanameGame
    {
        const int EnemyActive=0,EnemyEntering=1,EnemyUnaware=2,EnemySurprised=3,EnemyFleeing=4;
        // Entry stages: coming out of an opening, falling or leaping down, walking to the place.
        const int StageEmerge=0,StageFall=1,StageWalk=2;
        // Entries as listed in EncounterSpawn.Entries, then the hop out of a carrier or a boss.
        const int EntryNone=0,EntryWalk=1,EntryDoor=2,EntryWindow=3,EntryParachute=4,EntryDrop=5,EntryTruck=6,EntryHop=7;
        // What the last defeated enemy played (a death state, or "tumble"), and its wound: read by the tests.
        public string LastFall {get;private set;}="";
        public int LastWound {get;private set;}=-1;
        // voiceGate: simulated time before the next shout; cryGate: real time before the next cry (sound only).
        float voiceGate,cryGate;
        StageRules Stage=>Rules.stage;
        // An enemy still making its entrance or off guard beyond the edge of the screen is not in play yet: shots fly past it.
        bool Shootable(Enemy e)=>e.mode!=EnemyUnaware&&e.mode!=EnemyEntering||Mathf.Abs(e.position.x-cameraX)<SimHalfWidth+.3f;
        // Returns the animation state to show.
        int TickEnemyMode(Enemy e,Player target,float lastX,float dt)
        {
            var type=e.type;
            if(e.mode==EnemyEntering)return TickEntry(e,dt);
            if(e.mode==EnemyUnaware)
            {
                // It sees a hero in front of it, or hears one close by.
                var a=type.awareness;
                foreach(var p in players)if(p!=null&&p.hp>0)
                {
                    float d=p.position.x-e.position.x;
                    if(Mathf.Abs(p.position.y-e.position.y)<5&&(Mathf.Abs(d)<=a.hearing||d*e.facing>0&&Mathf.Abs(d)<=a.sight)){Surprise(e);break;}
                }
                return e.mode==EnemyUnaware?type.unaware:type.surprised;
            }
            if(e.mode==EnemySurprised)
            {
                e.facing=target.position.x>=e.position.x?1:-1;e.modeTime-=dt;
                if(e.modeTime<=0){e.mode=EnemyActive;e.attackTimer=Mathf.Max(e.attackTimer,.25f);}
                return type.surprised;
            }
            // In a panic it runs away from the nearest hero; a screen that is held keeps it in view, and brings it back.
            var f=type.panic;int away=target.position.x>=e.position.x?-1:1;e.facing=away;
            if(!e.perched){e.position.x=Mathf.Clamp(Advance(e.position.x,e.position.y,away*type.speed*SpeedFactor*f.speed*dt,ForEnemies),.2f,CurrentMission.length+1);e.walk+=dt*type.strideRate*f.speed;}
            bool held=screenLocked||BossActive;float centre=BossActive?bossArenaX:lockX;
            // Compared with a little slack: one held on the edge must stay held despite rounding.
            if(held&&Mathf.Abs(lastX-centre)<=SimHalfWidth-.5f)e.position.x=Mathf.Clamp(e.position.x,centre-SimHalfWidth+.6f,centre+SimHalfWidth-.6f);
            e.modeTime-=dt;
            if(e.modeTime<=0)
            {
                if(held||!e.routed&&f.returns){e.mode=EnemyActive;e.routed=false;}
                else e.routed=true;
            }
            return type.flee;
        }
        void Surprise(Enemy e)
        {
            if(e.mode!=EnemyUnaware)return;
            e.mode=EnemySurprised;e.modeTime=e.type.awareness.surprise;e.unaware=false;
            Sound(Stage.alertClip?Stage.alertClip:(Stage.alertClip=string.IsNullOrEmpty(Stage.alertSound)?null:Resources.Load<AudioClip>(Stage.alertSound)));
            Shout(Stage.alertVoice);
            // The alarm spreads to the allies around it.
            foreach(var other in enemies)if(other.mode==EnemyUnaware&&Mathf.Abs(other.position.x-e.position.x)<=e.type.awareness.alarm)Surprise(other);
        }
        void Flee(Enemy e,bool routed=false)
        {
            if(e.fled&&!routed||e.perched)return;
            e.fled=true;e.mode=EnemyFleeing;e.modeTime=routed?6:e.type.panic.time;e.routed=routed;e.windup=0;e.burst=0;e.attackPose=0;e.unaware=false;
            Shout(Stage.panicVoice);
        }
        // A hit that does not kill: an enemy off guard is surprised, one low on health may panic.
        void Hurt(Enemy e)
        {
            if(e.mode==EnemyUnaware){Surprise(e);return;}
            var f=e.type.panic;
            if(f.health>0&&!e.fled&&e.mode==EnemyActive&&e.hp<=e.maxHp*f.health)Flee(e);
        }
        // After a defeat: allies nearby are alarmed, and those that fear losses count it.
        void Fallen(Enemy dead)
        {
            foreach(var e in enemies)
            {
                float d=Mathf.Abs(e.position.x-dead.position.x);
                if(e.mode==EnemyUnaware&&d<=e.type.awareness.alarm){Surprise(e);continue;}
                var f=e.type.panic;
                if(f.allies>0&&!e.fled&&e.mode==EnemyActive&&d<=f.radius&&++e.fright>=f.allies)Flee(e);
            }
        }
        // A short shout (a voice line's takes) at most every three seconds, so that a crowd does not drown the announcer.
        void Shout(string voice)
        {
            if(string.IsNullOrEmpty(voice)||SimulationTime<voiceGate)return;voiceGate=SimulationTime+3;
            if(!bonusTakes.TryGetValue(voice,out var takes))bonusTakes[voice]=takes=TakesAt(voice);
            var clip=Pick(takes);if(clip&&cryOut&&voiceEnabled)cryOut.PlayOneShot(clip);
        }
        // The defeated enemy leaves the screen the way its wound says (visual only), with a cry now and then.
        void Fall(Enemy e,int direction,int wound)
        {
            var type=e.type;var motion=Rules.motions[wound];int state=type.deaths[wound];
            var frames=state>=0?System.Array.ConvertAll(type.animations.states[state].cells,c=>type.animations.Sprite(c)):null;float fps=state>=0?type.animations.states[state].fps:0;
            if(motion==null)effects.Defeat(e.illustrated,direction,frames,fps);
            else effects.Fall(e.illustrated,direction,frames,fps,motion.launch,motion.spin,motion.time,motion.gravity,motion.lie,motion.burn);
            if(motion!=null&&motion.burn)for(int i=0;i<3;i++)effects.Play(0,e.position+new Vector2((i-1)*.35f,.7f+i*.3f),1.1f,.45f+i*.1f,1,Vector2.up*.9f,90);
            LastFall=state>=0?type.animations.states[state].name:"tumble";LastWound=wound;
            if(type.cries!=null&&type.cries.Length>0&&cryOut&&voiceEnabled&&Time.unscaledTime>=cryGate)
            {
                if(type.cryClips==null){var all=new List<AudioClip>();foreach(var c in type.cries){var one=Resources.Load<AudioClip>(c);if(one)all.Add(one);else all.AddRange(TakesAt(c));}type.cryClips=all.ToArray();}
                if(type.cryClips.Length>0){cryOut.PlayOneShot(type.cryClips[kills%type.cryClips.Length],.8f);cryGate=Time.unscaledTime+.8f;}
            }
        }
        // The "!" over a surprised enemy, the parachute over a falling one, the fade of one coming out of a doorway, its shadow on the street.
        void ShowEnemyStaging(Enemy e)
        {
            bool alert=e.mode==EnemySurprised&&!string.IsNullOrEmpty(Stage.alert.texture);
            if(alert&&!e.sign)
            {
                e.sign=new GameObject("Surprise !").transform;e.sign.SetParent(e.root,false);e.sign.localPosition=new Vector3(0,e.type.animations.sheet.cellHeight*.95f,0);e.sign.localScale=Vector3.one*Stage.alertSize;
                var r=e.sign.gameObject.AddComponent<SpriteRenderer>();r.sprite=Stage.alert.Sprite;r.sortingOrder=70;
            }
            if(e.sign){e.sign.gameObject.SetActive(alert);if(alert)e.sign.localScale=new Vector3(e.facing,1,1)*Stage.alertSize*(1+.15f*Mathf.Clamp01(e.modeTime*4));}
            var chute=Stage.parachute;bool falling=e.mode==EnemyEntering&&e.entry==EntryParachute&&e.stage==StageFall&&!string.IsNullOrEmpty(chute.open.texture);
            if(falling&&!e.chute)
            {
                e.chute=new GameObject("Parachute").transform;e.chute.SetParent(e.root,false);e.chute.localPosition=new Vector3(0,chute.height+chute.size*.5f,0);e.chute.localScale=Vector3.one*chute.size;
                var r=e.chute.gameObject.AddComponent<SpriteRenderer>();r.sprite=chute.open.Sprite;r.sortingOrder=29;
            }
            if(e.chute&&falling)e.chute.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.unscaledTime*2.3f+e.goal.x)*chute.sway*20);
            if(e.chute&&!falling){LeaveChute(e);}
            var c=e.illustrated.color;if(c.a!=e.fade){c.a=e.fade;e.illustrated.color=c;}
            var shadow=e.root.GetChild(0);bool grounded=e.position.y-FloorAt(e.position.x,e.position.y,ForEnemies,e.dropFrom)<.05f;if(shadow.gameObject.activeSelf!=grounded)shadow.gameObject.SetActive(grounded);
        }
        // On landing the parachute folds and lies on the street for a moment.
        void LeaveChute(Enemy e)
        {
            var chute=Stage.parachute;var at=(Vector2)e.chute.position;Destroy(e.chute.gameObject);e.chute=null;
            if(!string.IsNullOrEmpty(chute.folding.texture))ShowProp(chute.folding.Sprite,at+Vector2.down*chute.size*.3f,chute.size,.35f,0);
            if(!string.IsNullOrEmpty(chute.landed.texture))ShowProp(chute.landed.Sprite,new Vector2(at.x+e.facing*.9f,chute.size*.18f),chute.size*.8f,1.6f,.35f);
        }
        // A lobbed grenade: the forward speed is chosen to land on the hero, within the thrower's reach.
        void LobGrenade(Enemy e,Player target)
        {
            var g=Rules.grenade;var spec=e.type.grenade;var hand=e.type.grenadeHand;
            Vector2 at=e.position+new Vector2(hand.x*e.facing,hand.y);float floor=g.size*.25f,vy=spec.throwY;
            float flight=(vy+Mathf.Sqrt(vy*vy+2*g.gravity*Mathf.Max(0,at.y-floor)))/g.gravity;
            float vx=Mathf.Clamp((target.position.x-at.x)/flight,-spec.maxX,spec.maxX);
            var root=new GameObject("Grenade ennemie").transform;root.SetParent(actors,false);root.position=at;root.localScale=Vector3.one*g.size;
            var r=root.gameObject.AddComponent<SpriteRenderer>();r.sprite=g.sprite.Sprite;r.sortingOrder=62;
            thrown.Add(new Grenade{position=at,velocity=new Vector2(vx,vy),root=root,hostile=true,spec=spec});
            Sound(g.throwClip?g.throwClip:(g.throwClip=Resources.Load<AudioClip>(g.throwSound)));
        }
    }
}
