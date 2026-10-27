using System.Collections.Generic;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    public sealed partial class PanameGame
    {
        // kind: the bonus's index in the rules' list of bonuses; gun: the weapon a weapon crate gives, drawn when it appears.
        sealed class Pickup {public int kind;public WeaponDefinition gun;public Vector2 position;public Transform root;public float age;}
        readonly List<Pickup> pickups=new List<Pickup>();
        readonly Dictionary<string,AudioClip[]> bonusTakes=new Dictionary<string,AudioClip[]>();
        int kills,nextSupply;float comboClock;int combo;
        public int BonusCount=>pickups.Count;
        void ResetBonuses()
        {
            pickups.Clear();kills=0;combo=0;comboClock=0;nextSupply=0;ResetWeaponDraws();
        }
        void SpawnPickup(Vector2 position,int kind)=>DropPickup(position,kind,null);
        // preset: the weapon a crate holds when its giver chose it (a hostage's reward).
        void DropPickup(Vector2 position,int kind,WeaponDefinition preset)
        {
            if(pickups.Count>=20)return;
            var bonus=Rules.bonuses[kind];var gun=bonus.kind==0?preset??bonus.gun??DrawWeapon():null;
            var root=new GameObject("Bonus • "+(gun!=null?gun.id:bonus.id)).transform;root.SetParent(actors,false);position.y=.65f;root.position=position;
            // A weapon crate shows the weapon it holds unless the bonus draws its own icon.
            var icon=gun!=null&&string.IsNullOrEmpty(bonus.icon?.texture)?gun.crate:bonus.icon;
            var r=root.gameObject.AddComponent<SpriteRenderer>();r.sprite=icon.Sprite;r.sortingOrder=48;root.localScale=Vector3.one*bonus.scale;
            pickups.Add(new Pickup{root=root,position=position,kind=kind,gun=gun});
        }
        public void GiveBonus(int slot,int kind)=>Collect(slot,kind,null);
        // gun: the weapon of a crate already drawn; null draws one now for a weapon bonus that names none.
        void Collect(int slot,int kind,WeaponDefinition gun)
        {
            var p=players[slot];if(p==null||p.hp<=0||kind<0||kind>=Rules.bonuses.Length)return;
            var bonus=Rules.bonuses[kind];
            if(bonus.kind==0){Arm(p,gun??bonus.gun??DrawWeapon(),bonus);return;}
            int amount=bonus.amount[DifficultyIndex];
            switch(bonus.kind)
            {
                case 1:p.hp=Mathf.Min(MaxPlayerHealth,p.hp+amount);break;
                case 2:p.boost=p.boostMax=amount;break;
                case 3:Score+=amount;break;
                default:p.grenades=Mathf.Min(Rules.grenades.max[DifficultyIndex],p.grenades+amount);break;
            }
            Celebrate(p,bonus.announce,bonus.voice);
        }
        void Celebrate(Player p,string announce,string voice)
        {
            if(!string.IsNullOrEmpty(announce))Announce(announce);effects.Play(1,p.position+Vector2.up,.95f,.2f);Sound(pickupClip);
            if(!string.IsNullOrEmpty(voice)){if(!bonusTakes.TryGetValue(voice,out var takes))bonusTakes[voice]=takes=TakesAt(voice);Speak(takes);}
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
                    if(owner>=0)Collect(owner,item.kind,item.gun);
                    item.root.gameObject.SetActive(false);Destroy(item.root.gameObject);pickups.RemoveAt(i);
                }
            }
        }
        void RecordKill(Vector2 at)
        {
            kills++;combo++;comboClock=2;Score+=100*Mathf.Min(5,1+combo/8);
            if(kills%KillsPerBonus==0){var cycle=Rules.drops;SpawnPickup(at,cycle[(kills/KillsPerBonus-1)%cycle.Length]);}
        }
        static Vector2 AimDirection(Player p)=>p.aimUp?(p.moving?new Vector2(p.facing,1).normalized:Vector2.up):p.aimDown?Vector2.down:new Vector2(p.facing,0);
        static string Clock(float time){int minutes=(int)time/60;return minutes.ToString("00")+":"+(time%60).ToString("00.0");}
    }
}
