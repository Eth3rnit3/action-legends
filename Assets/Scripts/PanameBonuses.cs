using System.Collections.Generic;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    public sealed partial class PanameGame
    {
        // kind: the bonus's index in the rules' list of bonuses.
        sealed class Pickup {public int kind;public Vector2 position;public Transform root;public float age;}
        readonly List<Pickup> pickups=new List<Pickup>();
        readonly Dictionary<string,AudioClip[]> bonusTakes=new Dictionary<string,AudioClip[]>();
        int kills,nextSupply;float comboClock;int combo;
        public int BonusCount=>pickups.Count;
        public static string WeaponName(int kind)=>kind==1?"MITRAILLEUSE":kind==2?"TRIPLE TIR":kind==3?"ROQUETTES":"FUSIL";
        void ResetBonuses()
        {
            pickups.Clear();kills=0;combo=0;comboClock=0;nextSupply=0;
        }
        void SpawnPickup(Vector2 position,int kind)
        {
            if(pickups.Count>=20)return;
            var bonus=Rules.bonuses[kind];
            var root=new GameObject("Bonus • "+bonus.id).transform;root.SetParent(actors,false);position.y=.65f;root.position=position;
            var r=root.gameObject.AddComponent<SpriteRenderer>();r.sprite=bonus.icon.Sprite;r.sortingOrder=48;root.localScale=Vector3.one*bonus.scale;
            pickups.Add(new Pickup{root=root,position=position,kind=kind});
        }
        public void GiveBonus(int slot,int kind)
        {
            var p=players[slot];if(p==null||p.hp<=0||kind<0||kind>=Rules.bonuses.Length)return;
            var bonus=Rules.bonuses[kind];int amount=bonus.amount[DifficultyIndex];
            switch(bonus.kind)
            {
                case 0:p.weapon=bonus.weapon;p.ammo=amount;break;
                case 1:p.hp=Mathf.Min(MaxPlayerHealth,p.hp+amount);break;
                case 2:p.boost=p.boostMax=amount;break;
                case 3:Score+=amount;break;
                default:p.grenades=Mathf.Min(Rules.grenades.max[DifficultyIndex],p.grenades+amount);break;
            }
            Announce(bonus.announce);effects.Play(1,p.position+Vector2.up,.95f,.2f);Sound(pickupClip);
            if(!string.IsNullOrEmpty(bonus.voice)){if(!bonusTakes.TryGetValue(bonus.voice,out var takes))bonusTakes[bonus.voice]=takes=TakesAt(bonus.voice);Speak(takes);}
        }
        void TickBonuses(float dt)
        {
            if(bossStarted)return;
            comboClock-=dt;if(comboClock<=0)combo=0;
            while(nextSupply*SupplySpacing+FirstSupplyX<CurrentMission.length-5&&LeaderX()+13>=nextSupply*SupplySpacing+FirstSupplyX)
            {var cycle=Rules.supplies;SpawnPickup(new Vector2(nextSupply*SupplySpacing+FirstSupplyX,0),cycle[nextSupply%cycle.Length]);nextSupply++;}
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
            if(kills%KillsPerBonus==0){var cycle=Rules.drops;SpawnPickup(at,cycle[(kills/KillsPerBonus-1)%cycle.Length]);}
        }
        void FirePlayer(Player p)
        {
            var hero=p.Definition;int mode=p.weapon;float speed=mode==3?24:36;int damage=mode==3?10:mode==1?5:hero.damage;
            Vector2 aim=AimDirection(p),side=new Vector2(-aim.y,aim.x),muzzle=MuzzlePoint(p);
            if(mode==2)for(int k=-1;k<=1;k++)Shoot(ShotLook.Tracer,muzzle,aim*speed+side*k*3.3f,4);
            else Shoot(ShotLook.Tracer,muzzle,aim*speed,damage,mode==3);
            effects.Muzzle(muzzle,aim,mode==3);
            p.fireTimer=mode==1?.055f:mode==3?.19f:hero.fireRate;p.recoil=.045f;
            if(mode>0&&--p.ammo<=0){p.weapon=0;p.ammo=0;}
            effects.Casing(muzzle+new Vector2(-p.facing*.62f,.04f),p.facing);shake=Mathf.Max(shake,hero.recoilShake);Sound(FireClip(mode));
        }
        static Vector2 AimDirection(Player p)=>p.aimUp?(p.moving?new Vector2(p.facing,1).normalized:Vector2.up):p.aimDown?Vector2.down:new Vector2(p.facing,0);
        static string Clock(float time){int minutes=(int)time/60;return minutes.ToString("00")+":"+(time%60).ToString("00.0");}
    }
}
