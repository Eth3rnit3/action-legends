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
        int bossStomps;
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
                bossPosition.x=Mathf.Clamp(bossPosition.x+step,bossArenaX-ArenaHalfWidth+w.margin,bossArenaX+ArenaHalfWidth-w.margin);bossStride+=Mathf.Abs(step);
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
                    Shoot(ShotLook.Bullet,muzzle,aim*a.speed[DifficultyIndex],a.damage);
                    effects.Muzzle(muzzle,aim,true);Sound(BossClip(a));bossShotTimer=a.interval[DifficultyIndex]*pace;
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
            else{bossPhase=BossWindup;bossTimer=a.windup*pace;bossStomps=bossEnraged?a.rageRepeat:a.repeat;}
            Announce(a.announce);
        }
        // The rest before the next move: rageCooldown, when given, replaces the faster pace of an enraged boss.
        void EndBossAttack(BossAttack a){bossPhase=BossIdle;bossTimer=(a.rageCooldown>=0&&bossEnraged?a.rageCooldown:a.cooldown)*AttackFactors[DifficultyIndex]*(a.rageCooldown>=0?1:BossAttackPace);}
        void DropBossBombs(BossAttack a,Player target)
        {
            int count=a.count[DifficultyIndex]+(bossEnraged?a.rageExtra:0);
            for(int i=0;i<count;i++)LaunchBossBomb(Mathf.Clamp(target.position.x+(i-(count-1)*.5f)*a.spacing,bossArenaX-ArenaHalfWidth+1,bossArenaX+ArenaHalfWidth-1),a);
        }
        AudioClip BossClip(BossAttack a)=>string.IsNullOrEmpty(a.sound)?null:a.clip?a.clip:(a.clip=Resources.Load<AudioClip>(a.sound));
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
