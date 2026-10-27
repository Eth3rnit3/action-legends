using System.Collections.Generic;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    // The mission's boss, read from its library entry: a flyer follows its flight path and attacks on the
    // move, a walker walks toward the heroes and stops for each attack (PanameBossAttacks.cs).
    public sealed partial class PanameGame
    {
        sealed class BossBomb
        {
            public Vector2 position,origin;public float age,delay,flight;public bool launched;public Transform root,marker;public SpriteRenderer warning;public BossAttack attack;
        }
        readonly List<BossBomb> bossBombs=new List<BossBomb>();
        Transform bossRoot,bossRotor,bossSight;
        SpriteRenderer bossRenderer;
        Vector2 bossPosition,bossAim;
        // bossAttack: index of the current move in the boss's list; bossPhase: what it is doing in it.
        int bossHealth,bossMaxHealth,bossAttack,bossPhase,bossVolley,bossBarrel,bossCell,bossState=-1;
        float bossTime,bossTimer,bossShotTimer,bossFlash,bossArenaX,bossDeathClock,bossExplosionTimer,bossRageTime,bossStateTime;
        const int BossIdle=0,BossAim=1,BossFire=2,BossWindup=3,BossRecover=4;
        bool bossStarted,bossDefeated,bossEnraged;
        // The boss being fought: the mission's at the end, or an encounter's mid-mission boss (midBoss), whose defeat lets the team move on.
        MissionBoss bossEntry;bool midBoss;
        MissionBoss BossEntry=>bossEntry??CurrentMission.boss;
        BossDefinition Boss=>BossEntry.definition;
        public bool MidBossActive=>midBoss&&bossStarted;
        float BossAttackPace=>bossEnraged?Boss.ragePace:1;
        public bool BossActive=>bossStarted&&!bossDefeated;
        public bool BossDefeated=>bossDefeated;
        public int BossHealth=>bossHealth;
        float ArenaHalfWidth=>SimHalfWidth;
        void ResetBoss()
        {
            bossBombs.Clear();ResetShockwaves();bossRoot=null;bossRotor=null;bossSight=null;bossRenderer=null;
            bossStarted=false;bossDefeated=false;bossEnraged=false;bossHealth=0;bossTime=0;bossDeathClock=0;bossBarrel=0;bossPhase=BossIdle;bossState=-1;bossStateTime=0;
            bossEntry=null;midBoss=false;bossTroops=0;bossTroop=0;
        }
        void SpawnBoss()=>SpawnBoss(null);
        // mid: an encounter's boss; the street keeps its enemies and the waves wait for its defeat.
        void SpawnBoss(MissionBoss mid)
        {
            bossEntry=mid;midBoss=mid!=null;bossStarted=true;screenLocked=false;encounterLock=false;bossArenaX=cameraX;
            var boss=Boss;var set=boss.animations;
            bossMaxHealth=Mathf.RoundToInt(boss.health[DifficultyIndex]*(1+boss.healthPerExtraPlayer*(JoinedCount-1))*BossEntry.health);
            bossHealth=bossMaxHealth;bossTimer=boss.firstAttack;bossAttack=0;bossPhase=BossIdle;bossVolley=0;bossTime=0;bossTroop=0;
            if(!midBoss){foreach(var e in enemies){e.root.gameObject.SetActive(false);Destroy(e.root.gameObject);}enemies.Clear();pending=0;Wave=CurrentMission.waves.Length;arrivals.Clear();}
            bossRoot=new GameObject("Boss • "+BossEntry.name).transform;bossRoot.SetParent(actors,false);
            bossSight=Shape(actors,"Visée de la mitrailleuse",0,.06f,1,.055f,Coral,42);
            bossSight.gameObject.SetActive(false);
            Announce(BossEntry.intro);Speak(midBoss?miniBossVoice:bossVoice);shake=.25f;
            bossCell=set.states[boss.idle].cells[0];
            bossRenderer=bossRoot.gameObject.AddComponent<SpriteRenderer>();bossRenderer.sprite=set.Sprite(bossCell);bossRenderer.sortingOrder=set.layer;
            if(boss.flash&&ArcadeEffects.ActorMaterial)bossRenderer.sharedMaterial=ArcadeEffects.ActorMaterial;
            if(boss.shadow.x>0)Shape(bossRoot,"Ombre du boss",0,.04f,boss.shadow.x,boss.shadow.y,new Color(.04f,.06f,.12f,.35f),10,true);
            var r=boss.rotor;
            if(!boss.walker&&r.width>0)
            {
                bossRotor=Shape(bossRoot,"Rotor animé",r.x,r.y,r.width,r.height,C("B8C8C9"),set.layer+1);
                Shape(bossRoot,"Rotor ombre",r.x,r.y-.07f,r.width-.9f,r.height-.015f,new Color(.08f,.14f,.19f,.6f),set.layer+1);
            }
            // A walker comes in from the right edge of the arena.
            if(boss.walker){bossPosition=new Vector2(bossArenaX+ArenaHalfWidth-boss.walk.entry,0);bossRoot.position=bossPosition;}
        }
        Player BossTarget()
        {
            Player target=null;float distance=float.MaxValue;
            foreach(var p in players)if(p!=null&&p.hp>0)
            {
                float d=Mathf.Abs(p.position.x-bossPosition.x);if(d<distance){distance=d;target=p;}
            }
            return target;
        }
        // A point drawn on the boss, mirrored with it: its art faces left.
        Vector2 BossPoint(string anchor){var local=Boss.animations.Points(anchor)[bossCell];return bossPosition+new Vector2(local.x*(bossRoot?bossRoot.localScale.x:1),local.y);}
        void TickBoss(float dt)
        {
            if(!bossStarted)
            {
                if(!CurrentMission.boss.enabled)
                {
                    // Without a boss the mission ends once every wave is cleared and the exit is reached.
                    if(phase==Phase.Playing&&Wave>=CurrentMission.waves.Length&&LeaderX()>=CurrentMission.exitX)WinMission();
                    return;
                }
                if(LeaderX()>=CurrentMission.exitX-18)SpawnBoss();
                if(!bossStarted)return;
            }
            var boss=Boss;
            if(bossDefeated)
            {
                bossDeathClock-=dt;bossExplosionTimer-=dt;
                if(bossRoot&&bossRoot.gameObject.activeSelf)
                {
                    // It falls and turns while explosions burst around its core.
                    var d=boss.death;
                    bossRoot.position+=(Vector3)(Vector2.down*dt*d.fall);bossRoot.Rotate(0,0,dt*(d.spinWithFacing?d.spin*bossRoot.localScale.x:d.spin));
                    Vector2 core=(Vector2)bossRoot.position+d.core;
                    if(bossExplosionTimer<=0){effects.Play(1,core+new Vector2(Mathf.Sin(bossDeathClock*25)*d.spreadX,Mathf.Cos(bossDeathClock*19)*d.spreadY),2.2f,.4f);bossExplosionTimer=.16f;shake=.25f;}
                    if(bossDeathClock<.45f)bossRoot.gameObject.SetActive(false);
                }
                if(bossDeathClock<=0){if(midBoss)EndMidBoss();else WinMission();}
                return;
            }
            bossTime+=dt;bossFlash=Mathf.Max(0,bossFlash-dt);
            if(boss.walker){TickWalker(dt);return;}
            var f=boss.flight;
            float movementTime=bossTime*f.speed[DifficultyIndex];
            // The phase stays continuous when the boss enters its faster second phase.
            if(bossEnraged)movementTime+=bossRageTime*f.rageCatchUp;
            if(bossEnraged)bossRageTime+=dt;
            bossPosition=new Vector2(bossArenaX+Mathf.Sin(movementTime)*Mathf.Min(f.sweep,ArenaHalfWidth-f.sweepMargin),f.altitude+Mathf.Sin(movementTime*f.bobRate)*f.bob);
            bossRoot.position=bossPosition;
            var target=BossTarget();if(target==null)return;
            bossRoot.localScale=new Vector3(target.position.x<bossPosition.x?1:-1,1,1);
            if(bossRotor)bossRotor.localScale=new Vector3(boss.rotor.width*(.25f+.75f*Mathf.Abs(Mathf.Sin(bossTime*boss.rotor.speed))),boss.rotor.height,1);
            bossTimer-=dt;
            TickBossAttack(dt,target);ShowBoss(dt);
            if(boss.stomps)TickShockwaves(dt);TickBossBombs(dt);
            if(bossHealth<bossMaxHealth*.5f&&bossTime%1<dt)effects.Play(3,BossPoint("smoke"),boss.smokeSize,.7f,1,Vector2.up*.6f);
        }
        void SetBossState(string name)=>SetBossState(string.IsNullOrEmpty(name)?Boss.idle:Boss.animations.Find(name,Boss.idle));
        void SetBossState(int state){if(state!=bossState){bossState=state;bossStateTime=0;}}
        void ShowBoss(float dt)
        {
            var set=Boss.animations;if(bossState<0)bossState=Boss.idle;
            bossCell=set.Cell(bossState,bossStateTime,Boss.walker?bossStride/Boss.walk.stepLength:bossTime);bossStateTime+=dt;
            if(bossRenderer){bossRenderer.sprite=set.Sprite(bossCell);ArcadeEffects.HitFlash(bossRenderer,bossFlash>0?1:0,flashProperties);}
        }
        BossAttack BombAttack=>bossAttack<Boss.attacks.Length&&Boss.attacks[bossAttack].kind==1?Boss.attacks[bossAttack]:Boss.FirstBombs;
        Vector2 BossBombRack()=>BossPoint(BombAttack.rack);
        void DropBossBomb(float x){if(BombAttack!=null)LaunchBossBomb(x,BombAttack);}
        void LaunchBossBomb(float x,BossAttack attack)
        {
            var root=new GameObject("Bombe du boss").transform;root.SetParent(actors,false);
            var renderer=root.gameObject.AddComponent<SpriteRenderer>();renderer.sprite=Boss.MissileSprite();renderer.sortingOrder=Boss.missile.layer;
            root.gameObject.SetActive(false);
            var marker=Shape(actors,"Danger au sol",x,.05f,attack.marker,.1f,Coral,43);
            // Keep the impact mark at x, then visibly release each missile from the rack.
            var rack=BossPoint(attack.rack);
            bossBombs.Add(new BossBomb{root=root,marker=marker,warning=marker.GetComponent<SpriteRenderer>(),position=rack,origin=rack,attack=attack,delay=attack.delay[DifficultyIndex]*BossAttackPace+bossBombs.Count*attack.stagger});
        }
        void RemoveBossBomb(int index,bool explosion)
        {
            var bomb=bossBombs[index];
            effects.Play(explosion?1:3,bomb.position,explosion?2.8f:1.1f,explosion?.35f:.25f);
            if(explosion)
            {
                Sound(explosionClips);shake=.25f;var a=bomb.attack;
                foreach(var p in players)if(p!=null&&p.hp>0&&Mathf.Abs(p.position.x-bomb.position.x)<a.blastRadius&&p.position.y<a.blastHeight)Damage(p,a.damage);
            }
            bomb.root.gameObject.SetActive(false);bomb.marker.gameObject.SetActive(false);Destroy(bomb.root.gameObject);Destroy(bomb.marker.gameObject);bossBombs.RemoveAt(index);
        }
        void TickBossBombs(float dt)
        {
            for(int i=bossBombs.Count-1;i>=0;i--)
            {
                var bomb=bossBombs[i];bomb.age+=dt;
                if(bomb.warning)bomb.warning.color=new Color(1,.3f,.15f,.35f+.65f*Mathf.Abs(Mathf.Sin(bomb.age*14)));
                if(bomb.age<bomb.delay)continue;
                if(!bomb.launched){bomb.launched=true;bomb.origin=BossPoint(bomb.attack.rack);bomb.flight=0;bomb.root.gameObject.SetActive(true);}
                bomb.flight+=dt;
                float t=Mathf.Clamp01(bomb.flight/bomb.attack.flight[DifficultyIndex]);
                Vector2 before=bomb.position,end=new Vector2(bomb.marker.position.x,.25f);
                // arc lifts the missile on its way: low for a drop, high for missiles that climb out of their pods.
                bomb.position=Vector2.Lerp(bomb.origin,end,t)+Vector2.up*(Mathf.Sin(t*Mathf.PI)*bomb.attack.arc);
                Vector2 heading=bomb.position-before;
                if(heading.sqrMagnitude>.000001f)bomb.root.rotation=Quaternion.Euler(0,0,Mathf.Atan2(heading.y,heading.x)*Mathf.Rad2Deg+180);
                bomb.root.position=bomb.position;
                if(t>=1)RemoveBossBomb(i,true);
            }
        }
        bool HitBossOrBomb(Vector2 from,Vector2 to,int damage)
        {
            if(!BossActive)return false;
            var m=Boss.missile;
            for(int i=bossBombs.Count-1;i>=0;i--)
            {
                var bomb=bossBombs[i];if(!bomb.launched)continue;
                Vector2 axis=bomb.root.TransformVector(Vector2.left).normalized*m.hitLength;
                if(SegmentDistance(from,to,bomb.position)<m.hitRadius||SegmentDistance(from,to,bomb.position+axis)<m.tipRadius||SegmentDistance(from,to,bomb.position-axis)<m.tipRadius){RemoveBossBomb(i,false);return true;}
            }
            // A row of circles along the drawn body, mirrored with the boss.
            var h=Boss.hitbox;float facing=bossRoot.localScale.x;
            for(int i=0;i<h.count;i++)if(SegmentDistance(from,to,bossPosition+new Vector2((h.x+i*h.stepX)*facing,h.y+i*h.stepY))<h.radius)
            {DamageBoss(damage);effects.Impact(to,1,true);return true;}
            return false;
        }
        // Whether a circle touches the boss's body.
        bool BossWithin(Vector2 at,float radius)
        {
            if(!BossActive||!bossRoot)return false;var h=Boss.hitbox;float facing=bossRoot.localScale.x;
            for(int i=0;i<h.count;i++)if(Vector2.Distance(at,bossPosition+new Vector2((h.x+i*h.stepX)*facing,h.y+i*h.stepY))<radius+h.radius)return true;
            return false;
        }
        void DamageBoss(int amount)
        {
            if(!BossActive)return;
            bossHealth=Mathf.Max(0,bossHealth-amount);bossFlash=.065f;
            if(!bossEnraged&&bossHealth>0&&bossHealth<=bossMaxHealth/2){bossEnraged=true;bossRageTime=0;bossTimer=Mathf.Min(bossTimer,Boss.rageDelay);Announce(BossEntry.rage);}
            if(bossHealth>0)return;
            bossDefeated=true;bossDeathClock=2.5f;bossExplosionTimer=0;Score+=Boss.score;
            // A boss with a "wreck" state shows it as it goes down.
            int wreck=Boss.animations.Find("wreck");if(wreck>=0&&bossRenderer){bossCell=Boss.animations.states[wreck].cells[0];bossRenderer.sprite=Boss.animations.Sprite(bossCell);}
            if(bossSight)bossSight.gameObject.SetActive(false);
            for(int i=bossBombs.Count-1;i>=0;i--)RemoveBossBomb(i,false);
            ResetShockwaves();
            foreach(var shot in shots)if(shot.Hostile)shot.life=0;
            Announce(BossEntry.defeat);Sound(bossDownClip);
        }
        // A mid-mission boss is gone: the street scrolls again, its troops run away.
        void EndMidBoss()
        {
            if(bossRoot)Destroy(bossRoot.gameObject);if(bossSight)Destroy(bossSight.gameObject);
            ResetBoss();goFromX=cameraX;Announce("CONTINUEZ !  »");
            foreach(var e in enemies)if(e.group==-2)Flee(e,true);
        }
        void DrawBossHUD()
        {
            if(!BossActive)return;
            Label(370,622,510,20,BossEntry.name,12,Gold,true);
            if(bossEnraged)Label(887,622,24,20,"!",16,Coral,true,TextAnchor.MiddleRight);
            Panel(370,646,540,10,new Color(.035f,.09f,.14f,.85f));
            Panel(372,648,536*(bossHealth/(float)bossMaxHealth),6,Coral);
        }
    }
}
