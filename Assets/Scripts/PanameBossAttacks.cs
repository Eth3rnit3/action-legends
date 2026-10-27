using System;
using System.Collections.Generic;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    // The boss moves: each is taken in turn from the boss's attack list once its timer runs out.
    public sealed partial class PanameGame
    {
        sealed class Shockwave {public float x,direction,speed,reach,height;public int damage;public Transform root;}
        readonly List<Shockwave> shockwaves=new List<Shockwave>();
        int bossStomps,bossTroops,bossTroop,bossCharge;
        float bossStride,shockwaveDust;
        void ResetShockwaves()
        {
            foreach(var wave in shockwaves)if(wave.root)Destroy(wave.root.gameObject);
            shockwaves.Clear();bossStomps=0;bossStride=0;bossCell=0;
        }
        void TickWalker(float dt)
        {
            var target=BossTarget();if(target==null)return;
            bossTimer-=dt;
            TickBossAttack(dt,target);
            bossRoot.position=bossPosition;
            ShowBoss(dt);if(Boss.stomps)TickShockwaves(dt);TickBossBombs(dt);
            if(bossHealth<bossMaxHealth*.5f&&bossTime%1<dt)effects.Play(3,BossPoint("smoke"),Boss.smokeSize,.7f,1,Vector2.up*.6f);
        }
        // Between attacks a walker turns to the nearest hero and walks up to it; each footfall shakes the street.
        void WalkBoss(float dt,Player target)
        {
            var w=Boss.walk;float gap=target.position.x-bossPosition.x;
            bossRoot.localScale=new Vector3(gap<0?1:-1,1,1);
            if(Mathf.Abs(gap)>w.stopDistance)
            {
                float before=bossStride,step=Mathf.Sign(gap)*w.speed[DifficultyIndex]/BossAttackPace*dt;
                // One that starts beyond the edge (a negative entry) drives in; then it stays within the arena.
                float low=bossArenaX-ArenaHalfWidth+w.margin,high=bossArenaX+ArenaHalfWidth-w.margin;
                bossPosition.x=Mathf.Clamp(bossPosition.x+step,Mathf.Min(low,bossPosition.x),Mathf.Max(high,bossPosition.x));bossStride+=Mathf.Abs(step);
                if((int)(before/w.stepLength)!=(int)(bossStride/w.stepLength)){shake=Mathf.Max(shake,w.stepShake);effects.Play(3,BossPoint(w.foot),1.4f,.4f);}
                SetBossState(Boss.move);
            }
            else SetBossState(Boss.idle);
        }
        void TickBossAttack(float dt,Player target)
        {
            var boss=Boss;float pace=BossAttackPace;
            if(bossPhase==BossIdle)
            {
                if(boss.walker)WalkBoss(dt,target);else SetBossState(boss.idle);
                if(bossTimer<=0){bossVolley++;StartBossAttack((bossVolley-1)%boss.attacks.Length,target);}
                return;
            }
            var a=boss.attacks[bossAttack];
            if(a.kind==0)
            {
                if(bossPhase==BossAim)
                {
                    SetBossState(a.aimState);
                    if(bossTimer<=0){bossPhase=BossFire;bossTimer=a.duration;bossShotTimer=0;bossSight.gameObject.SetActive(false);}
                    return;
                }
                SetBossState(a.state);bossShotTimer-=dt;
                if(bossShotTimer<=0)
                {
                    // The sweep swings the aim either side of where the heroes stood.
                    Vector2 muzzle=BossPoint(a.muzzles[bossBarrel++%a.muzzles.Length]);
                    Vector2 sweep=bossAim+Vector2.right*(Mathf.Sin((a.duration-bossTimer)*a.sweepRate)*a.sweep);
                    Vector2 aim=(sweep-muzzle).normalized;
                    // A drawn projectile (a thrown scroll, a spell) flies like a bullet, without a muzzle flash.
                    var bullet=Shoot(a.projectile.Drawn?ShotLook.Sprite:ShotLook.Bullet,muzzle,aim*a.speed[DifficultyIndex],a.damage);
                    if(a.projectile.Drawn){if(bullet!=null){bullet.drawn=a.projectile;bullet.sprite.sprite=a.projectile.Frame(0);bullet.sprite.color=Color.white;bullet.root.localScale=Vector3.one*a.projectile.size;}}
                    else effects.Muzzle(muzzle,aim,true);
                    Sound(BossClip(a));bossShotTimer=a.interval[DifficultyIndex]*pace;
                }
                if(bossTimer<=0)EndBossAttack(a);
            }
            else if(a.kind==1)
            {
                SetBossState(a.state);bossShotTimer-=dt;
                if(bossShotTimer<=0&&bossShotTimer>-1)
                {
                    DropBossBombs(a,target);bossShotTimer=-2;Sound(BossClip(a));
                    if(a.smoke)effects.Play(3,BossPoint(a.rack),1.6f,.5f,1,Vector2.up*.8f);
                }
                if(bossTimer<=0)EndBossAttack(a);
            }
            else if(a.kind==3)TickCharge(a,pace,dt);
            else if(a.kind==4)TickTroops(a,dt,target);
            else if(bossPhase==BossWindup)
            {
                SetBossState(a.state);
                if(bossTimer<=0)
                {
                    // The slam sends one shockwave each way along the street; only heroes on the ground are hit.
                    Vector2 foot=BossPoint(a.foot);
                    for(int side=-1;side<=1;side+=2)
                    {
                        var root=Shape(actors,"Onde de choc",foot.x,.32f,.9f,.62f,new Color(1,.55f,.25f,.85f),44,true);
                        Shape(root,"Cœur de l'onde",0,-.1f,.6f,.5f,new Color(1,.92f,.7f,.9f),45,true);
                        shockwaves.Add(new Shockwave{x=foot.x,direction=side,root=root,speed=a.speed[DifficultyIndex],reach=a.waveReach,height=a.waveHeight,damage=a.damage});
                    }
                    effects.Play(1,foot+Vector2.up*.4f,2.6f,.35f);effects.Play(3,foot,2.4f,.5f);Sound(explosionClips);shake=a.shake;
                    bossPhase=BossRecover;bossTimer=a.recover*pace;SetBossState(a.recoverState);
                }
            }
            else
            {
                SetBossState(a.recoverState);
                if(bossTimer<=0){if(--bossStomps>0){bossPhase=BossWindup;bossTimer=a.repeatWindup*pace;}else EndBossAttack(a);}
            }
        }
        void StartBossAttack(int index,Player target)
        {
            var a=Boss.attacks[index];float pace=BossAttackPace;bossAttack=index;
            if(a.kind==0)
            {
                bossPhase=BossAim;bossTimer=a.aimTime[DifficultyIndex]*pace;bossAim=target.position+Vector2.up*a.aimHeight;
                bossSight.position=new Vector2(bossAim.x,.07f);bossSight.localScale=new Vector3(a.sightWidth,.065f,1);bossSight.gameObject.SetActive(true);
            }
            // Without a pose, the bombs fall at once and the boss carries on.
            else if(a.kind==1&&a.duration<=0){DropBossBombs(a,target);EndBossAttack(a);}
            else if(a.kind==1){bossPhase=BossFire;bossTimer=a.duration*pace;bossShotTimer=a.launch*pace;}
            else if(a.kind==3){bossPhase=BossWindup;bossTimer=a.windup*pace;bossCharge=target.position.x<bossPosition.x?-1:1;bossRoot.localScale=new Vector3(-bossCharge,1,1);}
            else if(a.kind==4){bossPhase=BossFire;bossTimer=a.duration*pace;bossShotTimer=0;bossTroops=a.count[DifficultyIndex];}
            else{bossPhase=BossWindup;bossTimer=a.windup*pace;bossStomps=bossEnraged?a.rageRepeat:a.repeat;}
            Announce(a.announce);Sound(AttackClip(a.startSound,ref a.startClip));
        }
        // The rest before the next move: rageCooldown, when given, replaces the faster pace of an enraged boss.
        void EndBossAttack(BossAttack a){bossPhase=BossIdle;bossTimer=(a.rageCooldown>=0&&bossEnraged?a.rageCooldown:a.cooldown)*AttackFactor*(a.rageCooldown>=0?1:BossAttackPace);}
        void DropBossBombs(BossAttack a,Player target)
        {
            int count=a.count[DifficultyIndex]+(bossEnraged?a.rageExtra:0);
            for(int i=0;i<count;i++)LaunchBossBomb(Mathf.Clamp(target.position.x+(i-(count-1)*.5f)*a.spacing,bossArenaX-ArenaHalfWidth+1,bossArenaX+ArenaHalfWidth-1),a);
        }
        // Revs up facing a hero, then rushes to the far side of the arena; heroes in its way on the ground are hit, a jump over it clears it.
        void TickCharge(BossAttack a,float pace,float dt)
        {
            if(bossPhase==BossWindup)
            {
                SetBossState(a.aimState);
                if(bossTimer<=0){bossPhase=BossFire;Sound(BossClip(a));}
                return;
            }
            if(bossPhase==BossFire)
            {
                SetBossState(a.state);float edge=bossArenaX+bossCharge*(ArenaHalfWidth-Boss.walk.margin),step=bossCharge*a.speed[DifficultyIndex]*dt;
                bossPosition.x+=step;bossStride+=Mathf.Abs(step);if((int)(bossStride/.8f)!=(int)((bossStride-Mathf.Abs(step))/.8f))effects.Play(3,bossPosition+new Vector2(-bossCharge*a.reach,.2f),1.2f,.4f);
                foreach(var p in players)if(p!=null&&p.hp>0&&p.position.y<a.clearance&&Mathf.Abs(p.position.x-bossPosition.x)<a.reach)Damage(p,a.damage);
                if((bossPosition.x-edge)*bossCharge>=0){bossPosition.x=edge;bossPhase=BossRecover;bossTimer=a.recover*pace;shake=Mathf.Max(shake,a.shake);}
                return;
            }
            SetBossState(a.recoverState);
            if(bossTimer<=0)EndBossAttack(a);
        }
        // Lets its troops out of its door one by one; they hop down and run a few steps towards the team.
        void TickTroops(BossAttack a,float dt,Player target)
        {
            SetBossState(a.state);bossShotTimer-=dt;
            if(bossTroops>0&&bossShotTimer<=0)
            {
                int alive=0;foreach(var e in enemies)if(e.group==-2)alive++;
                if(alive<a.maxAlive)
                {
                    var door=BossPoint(a.door);float side=target.position.x>=door.x?1:-1;
                    var s=new EncounterSpawn{definition=a.troops[bossTroop++%a.troops.Length],kind=-1,x=Mathf.Clamp(door.x+side*a.spread,bossArenaX-ArenaHalfWidth+1,bossArenaX+ArenaHalfWidth-1)};
                    Arrive(s,-2,door);
                }
                bossTroops--;bossShotTimer=a.interval[DifficultyIndex]*BossAttackPace;
            }
            if(bossTimer<=0)EndBossAttack(a);
        }
        AudioClip BossClip(BossAttack a)=>AttackClip(a.sound,ref a.clip);
        static AudioClip AttackClip(string path,ref AudioClip clip)=>string.IsNullOrEmpty(path)?null:clip?clip:(clip=Resources.Load<AudioClip>(path));
        void TickShockwaves(float dt)
        {
            shockwaveDust-=dt;bool dust=shockwaveDust<=0;if(dust)shockwaveDust=.06f;
            for(int i=shockwaves.Count-1;i>=0;i--)
            {
                var wave=shockwaves[i];wave.x+=wave.direction*wave.speed*dt;
                wave.root.position=new Vector3(wave.x,.32f,0);
                if(dust)effects.Play(3,new Vector2(wave.x,.15f),.8f,.3f,1,Vector2.up*.4f);
                foreach(var p in players)if(p!=null&&p.hp>0&&p.position.y<wave.height&&Mathf.Abs(p.position.x-wave.x)<wave.reach)Damage(p,wave.damage);
                if(Mathf.Abs(wave.x-bossArenaX)>ArenaHalfWidth+1){Destroy(wave.root.gameObject);shockwaves.RemoveAt(i);}
            }
        }
    }
}
