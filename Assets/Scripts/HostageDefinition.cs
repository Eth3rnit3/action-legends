using System;
using UnityEngine;
namespace Paname
{
    // What a freed hostage hands over: a bonus of the mission's rules by id (a weapon bonus that names no weapon draws
    // one from the mission's table), or a crate holding a library weapon; weight: its share in the hostage's table.
    [Serializable] public sealed class HostageReward {public string bonus,weapon;public int weight=1;}
    [Serializable] public sealed class HostageHitbox {public float y=.7f,radius=.6f;}
    // After the thanks and the gift, the hostage runs at speed toward direction (-1 left, 1 right, 0 away from the hero who freed him).
    [Serializable] public sealed class HostageRun {public float speed=7;public int direction=-1;}
    // A tied-up prisoner placed by a mission (map.hostages): heroes free him by shooting his ropes (hits shots) or by
    // touching him, as release says (shot, touch or both). Freed, he plays thanks for thanks seconds, then give for give
    // seconds (the reward drops at giveAt), then runs off the screen. Resources/Content/hostages/<id>.json.
    [Serializable] public sealed class HostageDefinition
    {
        public string id,extends,name,release="both",sound,voice;
        public int hits=1;
        public float touchRadius=.9f,thanks=1.2f,give=.8f,giveAt=.45f;
        public HostageHitbox hitbox=new HostageHitbox();
        public HostageRun run=new HostageRun();
        public HostageReward[] rewards;
        public AnimationSet animations=new AnimationSet();
        public static readonly string[] Releases={"shot","touch","both"};
        public static readonly string[] Required={"tied","thanks","give","run"};
        [NonSerialized] public bool byShot,byTouch;
        [NonSerialized] public int tied,thanking,giving,running;
        [NonSerialized] public AudioClip clip;
        public void Validate()
        {
            string label="Otage "+id;
            Content.Text(name,24,label+".name");
            int r=Array.IndexOf(Releases,release);if(r<0)throw new ArgumentException(label+" : release doit être shot, touch ou both");byShot=r!=1;byTouch=r!=0;
            if(hits<1||hits>50)throw new ArgumentException(label+" : hits entre 1 et 50");
            Content.Range(touchRadius,.2f,5,label+".touchRadius");Content.Range(thanks,0,10,label+".thanks");Content.Range(give,0,10,label+".give");Content.Range(giveAt,0,give,label+".giveAt");
            Content.Range(hitbox.y,0,5,label+".hitbox.y");Content.Range(hitbox.radius,.1f,3,label+".hitbox.radius");
            Content.Range(run.speed,.5f,30,label+".run.speed");if(run.direction<-1||run.direction>1)throw new ArgumentException(label+" : run.direction -1, 0 ou 1");
            if(rewards==null||rewards.Length<1||rewards.Length>16)throw new ArgumentException(label+" : une à seize récompenses");
            foreach(var w in rewards)
            {
                if(w==null||w.weight<1||w.weight>100)throw new ArgumentException(label+" : weight des récompenses entre 1 et 100");
                CheckReward(w.bonus,w.weapon,label+".rewards");
            }
            if(!string.IsNullOrEmpty(sound)){Content.Path(sound,"Audio/",label+".sound");Content.Require<AudioClip>(sound,label+" : son de libération");}
            if(!string.IsNullOrEmpty(voice))Content.Path(voice,"Audio/",label+".voice");
            animations.Validate(label,Required);
            var a=animations;tied=a.Find("tied");thanking=a.Find("thanks");giving=a.Find("give");running=a.Find("run");
        }
        // A reward names a bonus or a weapon with ammunition; whether the bonus exists depends on the mission's rules (MissionDefinition.Validate).
        public static void CheckReward(string bonus,string weapon,string label)
        {
            if(string.IsNullOrEmpty(bonus)&&string.IsNullOrEmpty(weapon))throw new ArgumentException(label+" : bonus ou weapon requis");
            if(string.IsNullOrEmpty(weapon))return;
            var gun=ContentLibrary.FindWeapon(weapon);if(gun==null)throw new ArgumentException(label+" : arme inconnue "+weapon);
            if(gun.Unlimited)throw new ArgumentException(label+" : "+weapon+" n'a pas de munitions à donner");
        }
        // The bonus index of a reward in the rules: the named bonus, or the first weapon bonus for a weapon; -1 if absent.
        public static int BonusIndex(GameRules rules,string bonus,string weapon)
        {
            for(int i=0;i<rules.bonuses.Length;i++)
            {
                var b=rules.bonuses[i];
                if(!string.IsNullOrEmpty(bonus)?b.id==bonus:b.kind==0)return !string.IsNullOrEmpty(weapon)&&b.kind!=0?-1:i;
            }
            return -1;
        }
    }
}
