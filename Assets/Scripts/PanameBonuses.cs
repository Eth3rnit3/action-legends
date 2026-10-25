using System.Collections.Generic;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    public sealed partial class PanameGame
    {
        sealed class Pickup {public int kind;public Vector2 position;public Transform root;public float age;}
        readonly List<Pickup> pickups=new List<Pickup>();
        static Texture2D bonusTexture;static readonly Sprite[] bonusSprites=new Sprite[6];
        int kills,nextSupply;float comboClock;int combo;
        public int KillCount=>kills;
        public int BonusCount=>pickups.Count;
        public static string WeaponName(int kind)=>kind==1?"MITRAILLEUSE":kind==2?"TRIPLE TIR":kind==3?"ROQUETTES":"FUSIL";
        static Sprite BonusSprite(int kind)
        {
            if(bonusSprites[kind])return bonusSprites[kind];
            if(!bonusTexture)bonusTexture=Resources.Load<Texture2D>("Art/bonus-icons");if(!bonusTexture)return null;
            float w=bonusTexture.width/3f,h=bonusTexture.height/2f;
            return bonusSprites[kind]=Sprite.Create(bonusTexture,new Rect(kind%3*w,bonusTexture.height-(kind/3+1)*h,w,h),new Vector2(.5f,.5f),h,0,SpriteMeshType.FullRect);
        }
        void ResetBonuses()
        {
            pickups.Clear();kills=0;combo=0;comboClock=0;nextSupply=0;
        }
        void SpawnPickup(Vector2 position,int kind)
        {
            if(pickups.Count>=20)return;
            var root=new GameObject("Bonus • "+kind).transform;root.SetParent(actors,false);position.y=.65f;root.position=position;
            var sprite=BonusSprite(kind);
            if(sprite){var r=root.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=48;root.localScale=Vector3.one*1.25f;}
            else {Box(root,"Caisse bonus",0,0,.65f,.65f,kind==3?Coral:Gold,48);Text(root,kind==3?"+":"★",0,0,1.5f,Ink,50);}
            pickups.Add(new Pickup{root=root,position=position,kind=kind});
        }
        public void GiveBonus(int slot,int kind)
        {
            var p=players[slot];if(p==null||p.hp<=0)return;
            if(kind<3){p.weapon=kind+1;p.ammo=Mathf.RoundToInt((kind==0?120:kind==1?55:20)*(1-DifficultyIndex*.15f));Announce(WeaponName(p.weapon)+" !");}
            else if(kind==3){p.hp=Mathf.Min(MaxPlayerHealth,p.hp+(DifficultyIndex==0?3:2));Announce("SOINS !");}
            else if(kind==4){p.boost=DifficultyIndex==0?10:8-DifficultyIndex;Announce("TURBO !");}
            else {Score+=1500;Announce("BONUS +1500");}
            effects.Play(1,p.position+Vector2.up,.95f,.2f);Sound(pickupClip);Speak(bonusVoices[Mathf.Clamp(kind,0,5)]);
        }
        void TickBonuses(float dt)
        {
            if(bossStarted)return;
            comboClock-=dt;if(comboClock<=0)combo=0;
            while(nextSupply*SupplySpacing+FirstSupplyX<CurrentMission.length-5&&LeaderX()+13>=nextSupply*SupplySpacing+FirstSupplyX)
            {SpawnPickup(new Vector2(nextSupply*SupplySpacing+FirstSupplyX,0),nextSupply%6);nextSupply++;}
            for(int i=pickups.Count-1;i>=0;i--)
            {
                var item=pickups[i];item.age+=dt;item.root.position=item.position+Vector2.up*(Mathf.Sin(SimulationTime*5+i)*.11f);
                int owner=-1;for(int j=0;j<4;j++)if(players[j]!=null&&players[j].hp>0&&Vector2.Distance(players[j].position+Vector2.up*.65f,item.position)<1.05f){owner=j;break;}
                if(owner>=0||item.age>30)
                {
                    if(owner>=0)GiveBonus(owner,item.kind);
                    item.root.gameObject.SetActive(false);Destroy(item.root.gameObject);pickups.RemoveAt(i);
                }
            }
        }
        void RecordKill(Vector2 at)
        {
            kills++;combo++;comboClock=2;Score+=100*Mathf.Min(5,1+combo/8);
            if(kills%KillsPerBonus==0)SpawnPickup(at,(kills/KillsPerBonus-1)%6);
        }
        void FirePlayer(Player p,int slot)
        {
            int mode=p.weapon;float speed=mode==3?24:36;int damage=mode==3?10:mode==1?5:p.hero==1?5:p.hero==3?4:3;
            Vector2 aim=AimDirection(p),side=new Vector2(-aim.y,aim.x);
            Vector2 muzzle=p.position+Vector2.up*1.2f+aim*(p.aimUp?.65f:1.24f);
            if(mode==2)for(int k=-1;k<=1;k++)Shoot(muzzle,aim*speed+side*k*3.3f,4,false,PlayerColors[slot]);
            else Shoot(muzzle,aim*speed,damage,false,PlayerColors[slot],mode==3);
            p.fireTimer=mode==1?.055f:mode==3?.19f:Rate[p.hero];p.recoil=.045f;
            if(mode>0&&--p.ammo<=0){p.weapon=0;p.ammo=0;}
            effects.Casing(p.position+new Vector2(p.facing*.45f,1.25f),p.facing);shake=Mathf.Max(shake,p.hero==1?.07f:.025f);Sound(FireClip(mode));
        }
        static Vector2 AimDirection(Player p)=>p.aimUp?(p.moving?new Vector2(p.facing,1).normalized:Vector2.up):new Vector2(p.facing,0);
        static string Clock(float time){int minutes=(int)time/60;return minutes.ToString("00")+":"+(time%60).ToString("00.0");}
    }
}
