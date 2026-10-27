using System;
using UnityEngine;
namespace Paname
{
    // What hurt an enemy decides how it falls: shot, blown up, burnt or stabbed (Content/rules deaths, enemy death states).
    public static class Wounds
    {
        public const int Bullet=0,Explosion=1,Fire=2,Knife=3;
        public static readonly string[] Names={"bullet","explosion","fire","knife"};
        // The animation state each kind plays, falling back to "death".
        public static readonly string[] States={"death","deathExplosion","deathFire","deathKnife"};
        public static int Parse(string name,string label){int i=Array.IndexOf(Names,name);if(i<0)throw new ArgumentException(label+" : bullet, explosion, fire ou knife");return i;}
    }
    // An enemy placed by an encounter may start off guard ("unaware": its relax state): it notices a hero in front of it within
    // sight, or any hero within hearing, or a hit, or an ally within alarm that noticed; then it is surprised for surprise seconds
    // (its surprised state and the rules' "!" sign) before it attacks.
    [Serializable] public sealed class EnemyAwareness {public float sight=8,hearing=2.5f,surprise=.6f,alarm=5;}
    // Panic: below health (a fraction of its health; 0 never) or once allies (0 never) of its side have fallen within radius,
    // the enemy runs away from the heroes for time seconds at speed times its own speed, once; then it comes back if returns.
    [Serializable] public sealed class EnemyPanic {public float health,radius=6,time=2,speed=1.3f;public int allies;public bool returns=true;}
    // How an enemy gets about the relief (lot 3a): it leaps onto a step, a block or a prop up to climb units higher than its feet
    // (upward speed leap); with follow it also jumps up onto a ledge where its target stands, and down from one, instead of
    // waiting below.
    [Serializable] public sealed class EnemyTerrain
    {
        public float climb=1.7f,leap=10;public bool follow;
        public void Validate(string label){Content.Range(climb,0,4,label+".climb");Content.Range(leap,1,30,label+".leap");}
    }
    // A lobbed grenade (the rules' grenade physics and look): thrown at a hero between minRange and range, every cooldown seconds,
    // first after first seconds; throwY is the upward speed, the forward speed is chosen to land on the hero (at most maxX).
    // It goes off at its bounces-th landing, on touching a hero or when fuse runs out, and hurts heroes within radius.
    [Serializable] public sealed class EnemyGrenade
    {
        public float range,minRange=3,cooldown=4,first=1.5f,throwY=8,maxX=9,radius=1.6f,fuse=2.5f,pose=.4f;
        public int damage=1,bounces;
        public bool Enabled=>range>0;
        public void Validate(string label)
        {
            if(!Enabled)return;
            Content.Range(range,1,30,label+".range");Content.Range(minRange,0,range,label+".minRange");Content.Range(cooldown,.3f,30,label+".cooldown");Content.Range(first,0,30,label+".first");
            Content.Range(throwY,1,30,label+".throwY");Content.Range(maxX,1,30,label+".maxX");Content.Range(radius,.3f,6,label+".radius");Content.Range(fuse,.2f,10,label+".fuse");Content.Range(pose,0,2,label+".pose");
            if(damage<1||damage>10||bounces<0||bounces>3)throw new ArgumentException(label+" : damage de 1 à 10, bounces de 0 à 3");
        }
    }
    // How a defeated enemy leaves the screen for one kind of wound: launched at launch (x away from the hit), spinning, under gravity;
    // lie: it lands and stays on the street until the end; burn: it flickers with fire and smokes. Purely visual.
    [Serializable] public sealed class DeathMotion
    {
        public string damage;public Vector2 launch;public float spin,time=1,gravity=18;public bool lie,burn;
        [NonSerialized] public int kind;
        public void Validate(string label)
        {
            kind=Wounds.Parse(damage,label+".damage");
            if(!(Mathf.Abs(launch.x)<=20&&launch.y>=-5&&launch.y<=30))throw new ArgumentException(label+" : launch.x de -20 à 20, launch.y de -5 à 30");
            Content.Range(spin,-1440,1440,label+".spin");Content.Range(time,.1f,5,label+".time");Content.Range(gravity,0,100,label+".gravity");
        }
    }
    // A doorway, window or hatch drawn where an encounter's enemy comes out, height world units tall, open for time seconds.
    [Serializable] public sealed class OpeningLook {public string id;public SpriteCell sprite=new SpriteCell();public float height=2.6f,time=.7f;}
    [Serializable] public sealed class ParachuteLook
    {
        public SpriteCell open=new SpriteCell(),folding=new SpriteCell(),landed=new SpriteCell();
        public float size=2.4f,speed=2.4f,sway=.3f,height=2.1f;
    }
    // The staging of encounters: the surprise sign, parachutes and openings.
    [Serializable] public sealed class StageRules
    {
        // alertVoice, panicVoice: voice lines (takes) an enemy may shout when it spots the team or runs away.
        public SpriteCell alert=new SpriteCell();public float alertSize=.9f;public string alertSound,alertVoice,panicVoice;
        public ParachuteLook parachute=new ParachuteLook();
        public OpeningLook[] openings;
        [NonSerialized] public AudioClip alertClip;
        public OpeningLook Opening(string id){if(openings!=null)foreach(var o in openings)if(o.id==id)return o;return null;}
        public void Validate(string label)
        {
            if(!string.IsNullOrEmpty(alert.texture))alert.Validate(label+".alert");Content.Range(alertSize,.1f,5,label+".alertSize");
            if(!string.IsNullOrEmpty(alertSound)){Content.Path(alertSound,"Audio/",label+".alertSound");Content.Require<AudioClip>(alertSound,label+" : son de surprise");}
            foreach(var v in new[]{alertVoice,panicVoice})if(!string.IsNullOrEmpty(v)){Content.Path(v,"Audio/",label+" : voix");if(!Resources.Load<AudioClip>(v+"-a"))throw new ArgumentException(label+" : voix manquante "+v);}
            var p=parachute;foreach(var (cell,name) in new[]{(p.open,"open"),(p.folding,"folding"),(p.landed,"landed")})if(!string.IsNullOrEmpty(cell.texture))cell.Validate(label+".parachute."+name);
            Content.Range(p.size,.2f,10,label+".parachute.size");Content.Range(p.speed,.3f,20,label+".parachute.speed");Content.Range(p.sway,0,2,label+".parachute.sway");Content.Range(p.height,0,6,label+".parachute.height");
            if(openings!=null)
            {
                if(openings.Length>16)throw new ArgumentException(label+".openings : seize au plus");
                for(int i=0;i<openings.Length;i++)
                {
                    var o=openings[i];string ol=label+".openings["+i+"]";
                    if(o==null||string.IsNullOrWhiteSpace(o.id)||Array.FindIndex(openings,x=>x!=null&&x.id==o.id)!=i)throw new ArgumentException(ol+" : id unique requis");
                    o.sprite.Validate(ol+".sprite");Content.Range(o.height,.2f,10,ol+".height");Content.Range(o.time,0,5,ol+".time");
                }
            }
        }
    }
    // A vehicle that brings an encounter's enemies (Content/carriers): it drives in from a side at speed, slows down over brake,
    // stops, lets its passengers out of its "door" anchor every interval seconds, waits, then drives off. States: drive, open.
    // Its art faces right. It is scenery: nothing hits it.
    [Serializable] public sealed class CarrierDefinition
    {
        public string id,extends,sound;
        public float speed=9,brake=4,interval=.45f,wait=.8f,length=4;
        public AnimationSet animations=new AnimationSet();
        [NonSerialized] public int drive,open;[NonSerialized] public Vector2 door;[NonSerialized] public AudioClip clip;
        public void Validate()
        {
            string label="Véhicule "+id;
            Content.Range(speed,1,40,label+".speed");Content.Range(brake,0,20,label+".brake");Content.Range(interval,.05f,5,label+".interval");Content.Range(wait,0,10,label+".wait");Content.Range(length,.5f,20,label+".length");
            if(!string.IsNullOrEmpty(sound)){Content.Path(sound,"Audio/",label+".sound");Content.Require<AudioClip>(sound,label+" : son");}
            animations.Validate(label,"drive","open");drive=animations.Find("drive");open=animations.Find("open");
            if(!animations.HasAnchor("door",animations.states[open].cells[0]))throw new ArgumentException(label+" : ancrage door requis sur l'image open");
            door=animations.Points("door")[animations.states[open].cells[0]];
        }
    }
}
