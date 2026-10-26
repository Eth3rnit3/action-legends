using System.Collections.Generic;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    // Ground boss selected with "kind": "mecha". Its sheet holds six square frames, three per row,
    // drawn facing left: idle, step A, step B, stomp wind-up, gatling, missile launch.
    public sealed partial class PanameGame
    {
        sealed class Shockwave {public float x,direction;public Transform root;}
        readonly List<Shockwave> shockwaves=new List<Shockwave>();
        readonly Sprite[] mechaFrames=new Sprite[6];
        int mechaFrame,mechaStomps;
        float mechaStride,mechaWaveDust;
        // Points of the drawn mecha from its feet, facing left, measured on Art/tokyo/mecha-shogun.png.
        static readonly Vector2 MechaGatling=new Vector2(-3.3f,3.53f),MechaPods=new Vector2(.5f,5.1f),MechaFoot=new Vector2(-1.9f,.2f);
        const float MechaCell=6.6f;
        enum MechaMove { Walk, AimGatling, Gatling, RaiseFoot, Stomp, Missiles }
        void ResetMecha()
        {
            foreach(var wave in shockwaves)if(wave.root)Destroy(wave.root.gameObject);
            shockwaves.Clear();mechaStomps=0;mechaStride=0;mechaFrame=0;
        }
        Vector2 MechaPoint(Vector2 local)=>bossPosition+new Vector2(local.x*(bossRoot?bossRoot.localScale.x:1),local.y);
        void SpawnMecha()
        {
            var sheet=Resources.Load<Texture2D>(CurrentMission.boss.sprite);
            if(sheet)
            {
                float w=sheet.width/3f,h=sheet.height/2f;
                for(int i=0;i<6;i++)mechaFrames[i]=Sprite.Create(sheet,new Rect(i%3*w,sheet.height-(i/3+1)*h,w,h),new Vector2(.5f,.025f),h/MechaCell,0,SpriteMeshType.FullRect);
                bossRenderer=bossRoot.gameObject.AddComponent<SpriteRenderer>();bossRenderer.sprite=mechaFrames[0];bossRenderer.sortingOrder=28;
                if(ArcadeEffects.ActorMaterial)bossRenderer.sharedMaterial=ArcadeEffects.ActorMaterial;
            }
            Shape(bossRoot,"Ombre du mécha",0,.04f,4.6f,.35f,new Color(.04f,.06f,.12f,.35f),10,true);
            // It walks in from the right edge of the arena.
            bossPosition=new Vector2(bossArenaX+ArenaHalfWidth-2.5f,0);bossRoot.position=bossPosition;
            bossAttack=(int)MechaMove.Walk;bossTimer=1.2f;
        }
        void TickMecha(float dt)
        {
            var target=BossTarget();if(target==null)return;
            var move=(MechaMove)bossAttack;bossTimer-=dt;
            float pace=BossAttackPace,left=bossArenaX-ArenaHalfWidth+2.6f,right=bossArenaX+ArenaHalfWidth-2.6f;
            if(move==MechaMove.Walk)
            {
                float gap=target.position.x-bossPosition.x;
                bossRoot.localScale=new Vector3(gap<0?1:-1,1,1);
                if(Mathf.Abs(gap)>4)
                {
                    float before=mechaStride,step=Mathf.Sign(gap)*(2.1f+DifficultyIndex*.3f)/pace*dt;
                    bossPosition.x=Mathf.Clamp(bossPosition.x+step,left,right);mechaStride+=Mathf.Abs(step);
                    // Each footfall shakes the street and kicks up dust.
                    if((int)(before/1.3f)!=(int)(mechaStride/1.3f)){shake=Mathf.Max(shake,.12f);effects.Play(3,MechaPoint(MechaFoot),1.4f,.4f);}
                    mechaFrame=1+(int)(mechaStride/1.3f)%2;
                }
                else mechaFrame=0;
                if(bossTimer<=0)
                {
                    bossVolley++;
                    if(bossVolley%3==1){bossAttack=(int)MechaMove.AimGatling;bossTimer=Mathf.Max(.45f,.75f-DifficultyIndex*.09f)*pace;bossAim=target.position+Vector2.up*.9f;bossSight.position=new Vector2(bossAim.x,.07f);bossSight.localScale=new Vector3(4.8f,.065f,1);bossSight.gameObject.SetActive(true);Announce("GATLING EN BALAYAGE : BOUGEZ !");}
                    else if(bossVolley%3==2){bossAttack=(int)MechaMove.RaiseFoot;bossTimer=.65f*pace;mechaStomps=bossEnraged?2:1;Announce("ONDE DE CHOC : SAUTEZ !");}
                    else{bossAttack=(int)MechaMove.Missiles;bossTimer=1.1f*pace;bossShotTimer=.3f*pace;Announce("MISSILES : ESQUIVEZ OU DÉTRUISEZ-LES !");}
                }
            }
            else if(move==MechaMove.AimGatling)
            {
                mechaFrame=4;
                if(bossTimer<=0){bossAttack=(int)MechaMove.Gatling;bossTimer=1.6f;bossShotTimer=0;bossSight.gameObject.SetActive(false);}
            }
            else if(move==MechaMove.Gatling)
            {
                mechaFrame=4;bossShotTimer-=dt;
                if(bossShotTimer<=0)
                {
                    Vector2 muzzle=MechaPoint(MechaGatling);
                    Vector2 sweep=bossAim+Vector2.right*(Mathf.Sin((1.6f-bossTimer)*4.2f)*1.8f);
                    Vector2 aim=(sweep-muzzle).normalized;
                    Shoot(ShotLook.Bullet,muzzle,aim*(DifficultyIndex==0?13:16+DifficultyIndex*1.8f),1);
                    effects.Muzzle(muzzle,aim,true);Sound(bossGunClip);bossShotTimer=(.14f-DifficultyIndex*.022f)*pace;
                }
                if(bossTimer<=0)EndMechaAttack();
            }
            else if(move==MechaMove.RaiseFoot)
            {
                mechaFrame=3;
                if(bossTimer<=0)
                {
                    // The slam sends one shockwave each way along the street.
                    Vector2 foot=MechaPoint(MechaFoot);
                    for(int side=-1;side<=1;side+=2)
                    {
                        var root=Shape(actors,"Onde de choc",foot.x,.32f,.9f,.62f,new Color(1,.55f,.25f,.85f),44,true);
                        Shape(root,"Cœur de l'onde",0,-.1f,.6f,.5f,new Color(1,.92f,.7f,.9f),45,true);
                        shockwaves.Add(new Shockwave{x=foot.x,direction=side,root=root});
                    }
                    effects.Play(1,foot+Vector2.up*.4f,2.6f,.35f);effects.Play(3,foot,2.4f,.5f);Sound(explosionClips);shake=.45f;
                    bossAttack=(int)MechaMove.Stomp;bossTimer=.45f*pace;mechaFrame=0;
                }
            }
            else if(move==MechaMove.Stomp)
            {
                mechaFrame=0;
                if(bossTimer<=0){if(--mechaStomps>0){bossAttack=(int)MechaMove.RaiseFoot;bossTimer=.4f*pace;}else EndMechaAttack();}
            }
            else if(move==MechaMove.Missiles)
            {
                mechaFrame=5;bossShotTimer-=dt;
                if(bossShotTimer<=0&&bossShotTimer>-1)
                {
                    int count=2+DifficultyIndex+(bossEnraged?1:0);
                    for(int i=0;i<count;i++)DropBossBomb(Mathf.Clamp(target.position.x+(i-(count-1)*.5f)*2.6f,bossArenaX-ArenaHalfWidth+1,bossArenaX+ArenaHalfWidth-1));
                    bossShotTimer=-2;Sound(rocketClip);effects.Play(3,MechaPoint(MechaPods),1.6f,.5f,1,Vector2.up*.8f);
                }
                if(bossTimer<=0)EndMechaAttack();
            }
            bossRoot.position=bossPosition;
            if(bossRenderer){if(mechaFrames[mechaFrame])bossRenderer.sprite=mechaFrames[mechaFrame];ArcadeEffects.HitFlash(bossRenderer,bossFlash>0?1:0,flashProperties);}
            TickShockwaves(dt);TickBossBombs(dt);
            if(bossHealth<bossMaxHealth*.5f&&bossTime%1<dt)effects.Play(3,MechaPoint(new Vector2(.6f,4.2f)),.9f,.7f,1,Vector2.up*.6f);
        }
        void EndMechaAttack(){bossAttack=(int)MechaMove.Walk;bossTimer=(bossEnraged?.9f:1.5f)*AttackFactors[DifficultyIndex];}
        void TickShockwaves(float dt)
        {
            mechaWaveDust-=dt;bool dust=mechaWaveDust<=0;if(dust)mechaWaveDust=.06f;
            for(int i=shockwaves.Count-1;i>=0;i--)
            {
                var wave=shockwaves[i];wave.x+=wave.direction*(8.5f+DifficultyIndex)*dt;
                wave.root.position=new Vector3(wave.x,.32f,0);
                if(dust)effects.Play(3,new Vector2(wave.x,.15f),.8f,.3f,1,Vector2.up*.4f);
                // Only players standing on the street are hit: a jump clears the wave.
                foreach(var p in players)if(p!=null&&p.hp>0&&p.position.y<.6f&&Mathf.Abs(p.position.x-wave.x)<.55f)Damage(p,1);
                if(Mathf.Abs(wave.x-bossArenaX)>ArenaHalfWidth+1){Destroy(wave.root.gameObject);shockwaves.RemoveAt(i);}
            }
        }
        bool HitMecha(Vector2 from,Vector2 to,int damage)
        {
            // A column from the knees to the helmet, a little forward of the feet like the drawn torso.
            for(int i=0;i<5;i++)if(SegmentDistance(from,to,MechaPoint(new Vector2(-.2f,.9f+i*1.05f)))<1.25f)
            {DamageBoss(damage);effects.Impact(to,1,true);return true;}
            return false;
        }
    }
}
