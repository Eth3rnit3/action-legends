using System.Collections.Generic;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    public sealed partial class PanameGame
    {
        sealed class BossBomb
        {
            public Vector2 position,origin;public float age,delay,flight;public bool launched;public Transform root,marker;public SpriteRenderer warning;
        }
        readonly List<BossBomb> bossBombs=new List<BossBomb>();
        Transform bossRoot,bossRotor,bossSight;
        SpriteRenderer bossRenderer;
        Sprite bossBombSprite;
        Vector2 bossPosition,bossAim;
        int bossHealth,bossMaxHealth,bossAttack,bossVolley;
        float bossTime,bossTimer,bossShotTimer,bossFlash,bossArenaX,bossDeathClock,bossExplosionTimer;
        bool bossStarted,bossDefeated,bossEnraged;
        float BossAttackPace=>bossEnraged?.72f:1;
        public bool BossActive=>bossStarted&&!bossDefeated;
        public bool BossDefeated=>bossDefeated;
        public int BossHealth=>bossHealth;
        float ArenaHalfWidth=>SimHalfWidth;
        void ResetBoss()
        {
            bossBombs.Clear();bossRoot=null;bossRotor=null;bossSight=null;bossRenderer=null;
            bossStarted=false;bossDefeated=false;bossEnraged=false;bossHealth=0;bossTime=0;bossDeathClock=0;
        }
        void SpawnBoss()
        {
            bossStarted=true;bossArenaX=cameraX;
            bossMaxHealth=Mathf.RoundToInt((DifficultyIndex==0?300:DifficultyIndex==1?560:DifficultyIndex==2?800:1100)*(1+.65f*(JoinedCount-1)));
            bossHealth=bossMaxHealth;bossTimer=.8f;bossAttack=0;bossVolley=0;bossTime=0;
            foreach(var e in enemies){e.root.gameObject.SetActive(false);Destroy(e.root.gameObject);}enemies.Clear();pending=0;Wave=CurrentMission.waves.Length;
            bossRoot=new GameObject("Boss • Macron en hélico").transform;bossRoot.SetParent(actors,false);
            var texture=Resources.Load<Texture2D>("Art/macron-helicopter");
            if(texture)
            {
                bossRenderer=bossRoot.gameObject.AddComponent<SpriteRenderer>();
                bossRenderer.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),texture.width/8.8f,0,SpriteMeshType.FullRect);bossRenderer.sortingOrder=35;
                if(!bossBombSprite)bossBombSprite=CreateBossBombSprite(texture);
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
            float movementTime=bossTime*(1.2f+DifficultyIndex*.16f);
            // The phase stays continuous when the boss enters its faster second phase.
            if(bossEnraged)movementTime+=bossRageTime*.35f;
            if(bossEnraged)bossRageTime+=dt;
            bossPosition=new Vector2(bossArenaX+Mathf.Sin(movementTime)*Mathf.Min(5.2f,ArenaHalfWidth-4),3.65f+Mathf.Sin(movementTime*1.65f)*.75f);
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
                    bossAttack=1;bossTimer=Mathf.Max(.4f,.72f-DifficultyIndex*.09f)*BossAttackPace;
                    bossSight.position=new Vector2(bossAim.x,.07f);bossSight.localScale=new Vector3(4.8f,.065f,1);bossSight.gameObject.SetActive(true);
                    Announce("RAFALE EN BALAYAGE : BOUGEZ !");
                }
                else
                {
                    int count=2+DifficultyIndex+(bossEnraged?1:0);
                    for(int i=0;i<count;i++)DropBossBomb(Mathf.Clamp(target.position.x+(i-(count-1)*.5f)*2.6f,bossArenaX-ArenaHalfWidth+1,bossArenaX+ArenaHalfWidth-1));
                    bossAttack=0;bossTimer=1.35f*AttackFactors[DifficultyIndex]*BossAttackPace;Announce("BOMBES : ESQUIVEZ OU DÉTRUISEZ-LES !");
                }
            }
            else if(bossAttack==1&&bossTimer<=0){bossAttack=2;bossTimer=1.7f;bossShotTimer=0;bossSight.gameObject.SetActive(false);}
            else if(bossAttack==2)
            {
                if(bossShotTimer<=0)
                {
                    int facing=target.position.x<bossPosition.x?-1:1;
                    Vector2 muzzle=bossPosition+new Vector2(facing*3.2f,-.5f);
                    Vector2 sweep=bossAim+Vector2.right*(Mathf.Sin((1.7f-bossTimer)*4.2f)*1.8f);
                    Vector2 aim=(sweep-muzzle).normalized;
                    Shoot(muzzle,aim*(DifficultyIndex==0?13:16+DifficultyIndex*1.8f),1,true,Coral);
                    effects.Muzzle(muzzle,facing,true);Sound(fireClip);bossShotTimer=(.14f-DifficultyIndex*.022f)*BossAttackPace;
                }
                if(bossTimer<=0){bossAttack=0;bossTimer=.6f*AttackFactors[DifficultyIndex]*BossAttackPace;}
            }
            TickBossBombs(dt);
            if(bossHealth<bossMaxHealth*.5f&&bossTime%1<dt)effects.Play(3,bossPosition+Vector2.right*1.5f,.75f,.7f,1,Vector2.up*.6f);
        }
        float bossRageTime;
        static Sprite CreateBossBombSprite(Texture2D texture)
        {
            // Reuse the pixels of the upper missile on the helicopter. A native sprite
            // mesh follows its outline, excluding the surrounding rack and fuselage.
            var rect=new Rect(texture.width*.323f,texture.height*.291f,texture.width*.244f,texture.height*.115f);
            float ppu=texture.width/6.2f;
            var sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),ppu,0,SpriteMeshType.FullRect);
            Vector2[] outline={new Vector2(0,.48f),new Vector2(.08f,.75f),new Vector2(.19f,.89f),new Vector2(.76f,.89f),new Vector2(.88f,.99f),new Vector2(.99f,.99f),new Vector2(.99f,.72f),new Vector2(.93f,.49f),new Vector2(.99f,.26f),new Vector2(.99f,.03f),new Vector2(.88f,.03f),new Vector2(.77f,.12f),new Vector2(.18f,.1f),new Vector2(.07f,.28f)};
            var vertices=new Vector2[outline.Length+1];var triangles=new ushort[outline.Length*3];
            for(int i=0;i<outline.Length;i++)
            {
                vertices[i+1]=new Vector2((outline[i].x-.5f)*rect.width/ppu,(outline[i].y-.5f)*rect.height/ppu);
                triangles[i*3]=0;triangles[i*3+1]=(ushort)(i+1);triangles[i*3+2]=(ushort)((i+1)%outline.Length+1);
            }
            sprite.OverrideGeometry(vertices,triangles);return sprite;
        }
        Vector2 BossBombRack()=>bossPosition+new Vector2(-.5f*(bossRoot?bossRoot.localScale.x:1),-.68f);
        void DropBossBomb(float x)
        {
            var root=new GameObject("Bombe du boss").transform;root.SetParent(actors,false);
            var renderer=root.gameObject.AddComponent<SpriteRenderer>();renderer.sprite=bossBombSprite;renderer.sortingOrder=46;
            root.gameObject.SetActive(false);
            var marker=Shape(actors,"Danger au sol",x,.05f,2.8f,.1f,Coral,43);
            // Keep the impact mark at x, then visibly release each missile from the rack.
            bossBombs.Add(new BossBomb{root=root,marker=marker,warning=marker.GetComponent<SpriteRenderer>(),position=BossBombRack(),origin=BossBombRack(),delay=Mathf.Max(.3f,.55f-DifficultyIndex*.04f)*BossAttackPace+bossBombs.Count*.1f});
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
                if(bomb.age<bomb.delay)continue;
                if(!bomb.launched){bomb.launched=true;bomb.origin=BossBombRack();bomb.flight=0;bomb.root.gameObject.SetActive(true);}
                bomb.flight+=dt;
                float t=Mathf.Clamp01(bomb.flight/(.95f-DifficultyIndex*.08f));
                Vector2 before=bomb.position,end=new Vector2(bomb.marker.position.x,.25f);
                bomb.position=Vector2.Lerp(bomb.origin,end,t)+Vector2.up*(Mathf.Sin(t*Mathf.PI)*.3f);
                Vector2 heading=bomb.position-before;
                if(heading.sqrMagnitude>.000001f)bomb.root.rotation=Quaternion.Euler(0,0,Mathf.Atan2(heading.y,heading.x)*Mathf.Rad2Deg+180);
                bomb.root.position=bomb.position;
                if(t>=1)RemoveBossBomb(i,true);
            }
        }
        bool HitBossOrBomb(Vector2 from,Vector2 to,int damage)
        {
            if(!BossActive)return false;
            for(int i=bossBombs.Count-1;i>=0;i--)
            {
                var bomb=bossBombs[i];if(!bomb.launched)continue;
                Vector2 axis=bomb.root.TransformVector(Vector2.left).normalized*.5f;
                if(SegmentDistance(from,to,bomb.position)<.35f||SegmentDistance(from,to,bomb.position+axis)<.28f||SegmentDistance(from,to,bomb.position-axis)<.28f){RemoveBossBomb(i,false);return true;}
            }
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
            if(!bossEnraged&&bossHealth>0&&bossHealth<=bossMaxHealth/2){bossEnraged=true;bossRageTime=0;bossTimer=Mathf.Min(bossTimer,.35f);Announce("MACRON S'EMBALLE !");}
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
            Label(370,622,510,20,"MACRON",12,Gold,true);
            if(bossEnraged)Label(887,622,24,20,"!",16,Coral,true,TextAnchor.MiddleRight);
            Panel(370,646,540,10,new Color(.035f,.09f,.14f,.85f));
            Panel(372,648,536*(bossHealth/(float)bossMaxHealth),6,Coral);
        }
    }
}
