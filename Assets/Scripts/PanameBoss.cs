using System.Collections.Generic;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    public sealed partial class PanameGame
    {
        sealed class BossBomb
        {
            public Vector2 position;public float age;public Transform root,marker;public SpriteRenderer warning;
        }
        readonly List<BossBomb> bossBombs=new List<BossBomb>();
        Transform bossRoot,bossRotor,bossSight;
        SpriteRenderer bossRenderer;
        Vector2 bossPosition,bossAim;
        int bossHealth,bossMaxHealth,bossAttack,bossVolley;
        float bossTime,bossTimer,bossShotTimer,bossFlash,bossArenaX,bossDeathClock,bossExplosionTimer;
        bool bossStarted,bossDefeated;
        public bool BossActive=>bossStarted&&!bossDefeated;
        public bool BossDefeated=>bossDefeated;
        public int BossHealth=>bossHealth;
        float ArenaHalfWidth=>gameCamera.orthographicSize*gameCamera.aspect;
        void ResetBoss()
        {
            bossBombs.Clear();bossRoot=null;bossRotor=null;bossSight=null;bossRenderer=null;
            bossStarted=false;bossDefeated=false;bossHealth=0;bossTime=0;bossDeathClock=0;
        }
        void SpawnBoss()
        {
            bossStarted=true;bossArenaX=cameraX;
            bossMaxHealth=Mathf.RoundToInt((DifficultyIndex==0?300:DifficultyIndex==1?560:DifficultyIndex==2?800:1100)*(1+.65f*(JoinedCount-1)));
            bossHealth=bossMaxHealth;bossTimer=1.4f;bossAttack=0;bossVolley=0;bossTime=0;
            foreach(var e in enemies){e.root.gameObject.SetActive(false);Destroy(e.root.gameObject);}enemies.Clear();pending=0;Wave=CurrentMission.waves.Length;
            bossRoot=new GameObject("Boss • Macron en hélico").transform;bossRoot.SetParent(actors,false);
            var texture=Resources.Load<Texture2D>("Art/macron-helicopter");
            if(texture)
            {
                bossRenderer=bossRoot.gameObject.AddComponent<SpriteRenderer>();
                bossRenderer.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),texture.width/8.8f,0,SpriteMeshType.FullRect);bossRenderer.sortingOrder=35;
            }
            bossRotor=Shape(bossRoot,"Rotor animé",-1,1.35f,6.5f,.055f,C("B8C8C9"),36);
            Shape(bossRoot,"Rotor ombre",-1,1.28f,5.6f,.04f,new Color(.08f,.14f,.19f,.6f),36);
            bossSight=Shape(actors,"Visée de la mitrailleuse",0,.06f,1,.055f,Coral,42);
            bossSight.gameObject.SetActive(false);
            Announce("BOSS : MACRON EN HÉLICO !");Speak(bossVoice);shake=.25f;
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
        void TickBoss(float dt)
        {
            if(!bossStarted&&LeaderX()>=CurrentMission.exitX-18)SpawnBoss();
            if(!bossStarted)return;
            if(bossDefeated)
            {
                bossDeathClock-=dt;bossExplosionTimer-=dt;
                if(bossRoot&&bossRoot.gameObject.activeSelf)
                {
                    bossRoot.position+=(Vector3)(Vector2.down*dt*.8f);bossRoot.Rotate(0,0,dt*12);
                    if(bossExplosionTimer<=0){effects.Play(1,(Vector2)bossRoot.position+new Vector2(Mathf.Sin(bossDeathClock*25)*2,Mathf.Cos(bossDeathClock*19)*.6f),2.2f,.4f);bossExplosionTimer=.16f;shake=.25f;}
                    if(bossDeathClock<.45f)bossRoot.gameObject.SetActive(false);
                }
                if(bossDeathClock<=0){phase=Phase.Won;Announce("MISSION ACCOMPLIE !");Speak(winVoice);}
                return;
            }
            bossTime+=dt;bossFlash=Mathf.Max(0,bossFlash-dt);
            bossPosition=new Vector2(bossArenaX+Mathf.Sin(bossTime*.7f)*Mathf.Min(4.5f,ArenaHalfWidth-4),3.65f+Mathf.Sin(bossTime*1.4f)*.65f);
            bossRoot.position=bossPosition;
            var target=BossTarget();if(target==null)return;
            bossRoot.localScale=new Vector3(target.position.x<bossPosition.x?1:-1,1,1);
            bossRotor.localScale=new Vector3(6.5f*(.25f+.75f*Mathf.Abs(Mathf.Sin(bossTime*85))),.055f,1);
            if(bossRenderer)ArcadeEffects.HitFlash(bossRenderer,bossFlash>0?1:0,flashProperties);
            bossTimer-=dt;bossShotTimer-=dt;
            if(bossAttack==0&&bossTimer<=0)
            {
                bossAim=target.position+Vector2.up*.9f;bossVolley++;
                if(bossVolley%2==1)
                {
                    bossAttack=1;bossTimer=Mathf.Max(.55f,.95f-DifficultyIndex*.12f);
                    bossSight.position=new Vector2(bossAim.x,.07f);bossSight.localScale=new Vector3(1.4f,.065f,1);bossSight.gameObject.SetActive(true);
                    Announce("MITRAILLAGE : BOUGEZ !");
                }
                else
                {
                    int count=2+DifficultyIndex;
                    for(int i=0;i<count;i++)DropBossBomb(Mathf.Clamp(target.position.x+(i-(count-1)*.5f)*2.6f,bossArenaX-ArenaHalfWidth+1,bossArenaX+ArenaHalfWidth-1));
                    bossAttack=0;bossTimer=2.6f*AttackFactors[DifficultyIndex];Announce("BOMBES : ESQUIVEZ OU DÉTRUISEZ-LES !");
                }
            }
            else if(bossAttack==1&&bossTimer<=0){bossAttack=2;bossTimer=1.3f;bossShotTimer=0;bossSight.gameObject.SetActive(false);}
            else if(bossAttack==2)
            {
                if(bossShotTimer<=0)
                {
                    int facing=target.position.x<bossPosition.x?-1:1;
                    Vector2 muzzle=bossPosition+new Vector2(facing*3.2f,-.5f);
                    Vector2 aim=(bossAim-muzzle).normalized;
                    Shoot(muzzle,aim*(DifficultyIndex==0?10:13+DifficultyIndex),1,true,Coral);
                    effects.Muzzle(muzzle,facing,true);Sound(fireClip);bossShotTimer=.2f-DifficultyIndex*.035f;
                }
                if(bossTimer<=0){bossAttack=0;bossTimer=1.1f*AttackFactors[DifficultyIndex];}
            }
            TickBossBombs(dt);
            if(bossHealth<bossMaxHealth*.5f&&bossTime%1<dt)effects.Play(3,bossPosition+Vector2.right*1.5f,.75f,.7f,1,Vector2.up*.6f);
        }
        void DropBossBomb(float x)
        {
            var root=new GameObject("Bombe du boss").transform;root.SetParent(actors,false);
            Shape(root,"Corps",0,0,.36f,.52f,C("C39B45"),46,true);Box(root,"Ailette",0,.22f,.45f,.08f,Ink,47);
            root.gameObject.SetActive(false);
            var marker=Shape(actors,"Danger au sol",x,.05f,2.8f,.1f,Coral,43);
            bossBombs.Add(new BossBomb{root=root,marker=marker,warning=marker.GetComponent<SpriteRenderer>(),position=new Vector2(x,bossPosition.y+.4f)});
        }
        void RemoveBossBomb(int index,bool explosion)
        {
            var bomb=bossBombs[index];
            effects.Play(explosion?1:3,bomb.position,explosion?2.8f:1.1f,explosion?.35f:.25f);
            if(explosion)
            {
                Sound(specialClip);shake=.25f;
                foreach(var p in players)if(p!=null&&p.hp>0&&Mathf.Abs(p.position.x-bomb.position.x)<1.45f&&p.position.y<1)Damage(p,1);
            }
            bomb.root.gameObject.SetActive(false);bomb.marker.gameObject.SetActive(false);Destroy(bomb.root.gameObject);Destroy(bomb.marker.gameObject);bossBombs.RemoveAt(index);
        }
        void TickBossBombs(float dt)
        {
            for(int i=bossBombs.Count-1;i>=0;i--)
            {
                var bomb=bossBombs[i];bomb.age+=dt;
                if(bomb.warning)bomb.warning.color=new Color(1,.3f,.15f,.35f+.65f*Mathf.Abs(Mathf.Sin(bomb.age*14)));
                if(bomb.age<.75f)continue;
                bomb.root.gameObject.SetActive(true);bomb.position.y-=dt*(3.1f+DifficultyIndex*.65f);bomb.root.position=bomb.position;bomb.root.Rotate(0,0,dt*130);
                if(bomb.position.y<=.25f){bomb.position.y=.25f;RemoveBossBomb(i,true);}
            }
        }
        bool HitBossOrBomb(Vector2 from,Vector2 to,int damage)
        {
            if(!BossActive)return false;
            for(int i=bossBombs.Count-1;i>=0;i--)if(bossBombs[i].age>.75f&&SegmentDistance(from,to,bossBombs[i].position)<.4f){RemoveBossBomb(i,false);return true;}
            // A capsule around the fuselage excludes the rotor and most of the tail.
            float facing=bossRoot.localScale.x;
            for(int i=0;i<4;i++)if(SegmentDistance(from,to,bossPosition+new Vector2((-2+i*.85f)*facing,-.05f))<.95f)
            {DamageBoss(damage);effects.Impact(to,1,true);return true;}
            return false;
        }
        void DamageBoss(int amount)
        {
            if(!BossActive)return;
            bossHealth=Mathf.Max(0,bossHealth-amount);bossFlash=.065f;
            if(bossHealth>0)return;
            bossDefeated=true;bossDeathClock=2.5f;bossExplosionTimer=0;Score+=10000;
            if(bossSight)bossSight.gameObject.SetActive(false);
            for(int i=bossBombs.Count-1;i>=0;i--)RemoveBossBomb(i,false);
            foreach(var shot in shots)if(shot.hostile)shot.life=0;
            Announce("HÉLICO HORS SERVICE !");Sound(specialClip);
        }
        void DrawBossHUD()
        {
            if(!BossActive)return;
            Panel(290,585,700,70,new Color(.035f,.09f,.15f,.95f));
            Label(308,588,660,23,"BOSS  •  MACRON EN HÉLICO",17,Gold,true,TextAnchor.MiddleCenter);
            Panel(310,617,660,9,C("324752"));Panel(310,617,660*(bossHealth/(float)bossMaxHealth),9,Coral);
            Label(308,631,660,18,"W / Z / ↑ + J : VISER EN HAUT  •  SAUTEZ, ESQUIVEZ, DÉTRUISEZ LES BOMBES",11,Cream,true,TextAnchor.MiddleCenter);
        }
    }
}
