using System;
using UnityEngine;
namespace Paname
{
    // The gun on its pivot (anchor of the body's images): drawn by sprite, size units tall, turning on the point pivot (a fraction
    // of the sprite's width from its left, the barrels pointing right) at turnRate degrees per second towards the angle the pilot
    // aims at: 0 ahead, up, diagonal (up while moving), down on the ground and downAir in the air. Shots leave length units
    // along the barrel from the pivot; firing shows the firing sprite when it has one.
    [Serializable] public sealed class VehicleTurret
    {
        public string anchor="turret";public SpriteCell sprite=new SpriteCell(),firing=new SpriteCell();
        public float size=.5f,pivot=.3f,length=1.4f,turnRate=360,up=90,diagonal=45,down=-25,downAir=-90;
    }
    // The fixed cannon (the grenade button): weapon (Content/weapons), shells per difficulty, fired from its anchor at angle
    // degrees above the way the vehicle faces, or along the gun when aimed; the body holds its fire state for pose seconds.
    [Serializable] public sealed class VehicleCannon
    {
        public string weapon,anchor="cannon";public int[] shells;public float angle,pose=.25f;public bool aimed;
        [NonSerialized] public WeaponDefinition Weapon;
    }
    // The hero shown in the seat: his own image of state (else his idle one), scale times his size, his feet at the anchor of the
    // body's image, drawn behind the body so only his head and chest show above the cockpit.
    [Serializable] public sealed class VehiclePilot {public string state="crouch",anchor="seat";public float scale=1;}
    // Getting in and out. board: "touch" (walking into it) or "jump" (landing on it from above). A hero who gets out with down + jump
    // leaves with exitJump; one thrown out when it breaks flies up at ejectJump; both are invulnerable for their time after.
    [Serializable] public sealed class VehicleBoarding {public string board="touch";public float exitJump=10,exitInvulnerable=.6f,ejectJump=15,ejectInvulnerable=2;}
    // What the vehicle does to what it drives into: every interval seconds, damage to the enemies inside its box while it moves,
    // propDamage to a standing prop that walls it; stomp to the enemies under it when it lands from a fall; props with "crush"
    // break under it at once. wound: how those enemies fall (bullet, explosion, fire, knife).
    [Serializable] public sealed class VehicleRam {public int damage,propDamage,stomp;public float interval=.3f;public string wound="explosion";[NonSerialized] public int woundKind;}
    // At zero health it blinks for time seconds (its wreck state) with nobody in it, then blows up: enemies and props within radius
    // take damage, the boss bossDamage, heroes heroDamage; its debris fly and its destroyed state stays (or nothing).
    [Serializable] public sealed class VehicleWreck {public float time=1.4f,radius=3;public int damage=60,bossDamage,heroDamage;}
    // A vehicle of the library (Content/vehicles/<id>.json), parked on the map (map.vehicles) for any hero to drive (lot 3b): its own
    // health per difficulty (the pilot takes no damage while he is in it), speed, jump and gravity, a gun that turns with the aim
    // (the fire button, an unlimited weapon of Content/weapons) and a cannon with its shells (the grenade button). Down + jump gets
    // out. box: the box it is hit in and walls with (width units wide, centred, height tall). States: idle, run and jump are
    // required; fire falls back to idle, damaged to idle, damagedRun to damaged or run, wreck to damaged; destroyed is optional.
    [Serializable] public sealed class VehicleDefinition
    {
        public string id,extends,name,gun,sound,boardSound,exitSound,ejectSound,breakSound,voice,announce;
        public int[] health;
        public float speed=6,jump=11,gravity=29,damagedAt=.4f,invulnerable=.12f,strideRate=1;
        public PropBox box=new PropBox();
        public VehicleTurret turret=new VehicleTurret();
        public VehicleCannon cannon=new VehicleCannon();
        public VehiclePilot pilot=new VehiclePilot();
        public VehicleBoarding boarding=new VehicleBoarding();
        public VehicleRam ram=new VehicleRam();
        public VehicleWreck wreck=new VehicleWreck();
        public PropDebris debris=new PropDebris();
        // HUD: the vehicle's badge by its gauge, and the shell's by the count.
        public SpriteCell icon=new SpriteCell(),shellIcon=new SpriteCell();
        public AnimationSet animations=new AnimationSet();
        public static readonly string[] Required={"idle","run","jump"};
        public static readonly string[] Boards={"touch","jump"};
        [NonSerialized] public int idle,run,jumping,fire,damaged,damagedRun,wrecked,destroyed;
        [NonSerialized] public bool byJump;
        [NonSerialized] public WeaponDefinition Gun;
        [NonSerialized] AudioClip[] clips;
        AudioClip Load(int i,string path){clips??=new AudioClip[5];return string.IsNullOrEmpty(path)?null:clips[i]?clips[i]:(clips[i]=Resources.Load<AudioClip>(path));}
        public AudioClip EngineClip=>Load(0,sound);
        public AudioClip BoardClip=>Load(1,boardSound);
        public AudioClip ExitClip=>Load(2,exitSound);
        public AudioClip EjectClip=>Load(3,ejectSound);
        public AudioClip BreakClip=>Load(4,breakSound);
        public float Half=>box.width*.5f;
        static WeaponDefinition Weapon(string id,string label)
        {
            var w=ContentLibrary.FindWeapon(id);if(w==null)throw new ArgumentException(label+" : arme inconnue "+id+" (Content/weapons/<id>.json)");
            // A ray follows the hero's own barrel, not the vehicle's.
            if(w.projectile.Beam)throw new ArgumentException(label+" : une arme de véhicule ne tire pas de rayon (beam)");
            return w;
        }
        public void Validate()
        {
            string label="Véhicule "+id;
            Content.Text(name,24,label+".name");
            Content.PerDifficulty(health,1,2000,label+".health");Content.Range(damagedAt,0,1,label+".damagedAt");Content.Range(invulnerable,0,2,label+".invulnerable");
            Content.Range(speed,1,20,label+".speed");Content.Range(jump,0,30,label+".jump");Content.Range(gravity,5,80,label+".gravity");Content.Range(strideRate,.05f,20,label+".strideRate");
            Content.Range(box.width,.8f,8,label+".box.width");Content.Range(box.height,.6f,6,label+".box.height");
            Gun=Weapon(gun,label+".gun");if(!Gun.Unlimited)throw new ArgumentException(label+".gun : une arme sans fin de munitions (ammo à 0)");
            var c=cannon;if(c==null)throw new ArgumentException(label+" : cannon requis");
            c.Weapon=Weapon(c.weapon,label+".cannon.weapon");Content.PerDifficulty(c.shells,0,99,label+".cannon.shells");
            Content.Range(c.angle,-90,90,label+".cannon.angle");Content.Range(c.pose,0,2,label+".cannon.pose");
            var t=turret;t.sprite.Validate(label+".turret.sprite");if(!string.IsNullOrEmpty(t.firing?.texture))t.firing.Validate(label+".turret.firing");
            Content.Range(t.size,.1f,4,label+".turret.size");Content.Range(t.pivot,0,1,label+".turret.pivot");Content.Range(t.length,0,6,label+".turret.length");Content.Range(t.turnRate,30,3600,label+".turret.turnRate");
            foreach(var (angle,n) in new[]{(t.up,"up"),(t.diagonal,"diagonal"),(t.down,"down"),(t.downAir,"downAir")})Content.Range(angle,-90,90,label+".turret."+n);
            Content.Range(pilot.scale,.2f,2,label+".pilot.scale");
            var b=boarding;int board=Array.IndexOf(Boards,b.board);if(board<0)throw new ArgumentException(label+".boarding.board : touch ou jump");byJump=board==1;
            Content.Range(b.exitJump,0,30,label+".boarding.exitJump");Content.Range(b.ejectJump,0,30,label+".boarding.ejectJump");Content.Range(b.exitInvulnerable,0,5,label+".boarding.exitInvulnerable");Content.Range(b.ejectInvulnerable,0,5,label+".boarding.ejectInvulnerable");
            var r=ram;if(r.damage<0||r.damage>500||r.propDamage<0||r.propDamage>500||r.stomp<0||r.stomp>500)throw new ArgumentException(label+" : ram.damage, ram.propDamage et ram.stomp de 0 à 500");
            Content.Range(r.interval,.05f,5,label+".ram.interval");r.woundKind=Wounds.Parse(string.IsNullOrEmpty(r.wound)?"explosion":r.wound,label+".ram.wound");
            var w=wreck;Content.Range(w.time,0,6,label+".wreck.time");Content.Range(w.radius,0,10,label+".wreck.radius");
            if(w.damage<0||w.damage>500||w.bossDamage<0||w.bossDamage>1000||w.heroDamage<0||w.heroDamage>10)throw new ArgumentException(label+" : wreck.damage de 0 à 500, bossDamage de 0 à 1000, heroDamage de 0 à 10");
            debris.Validate(label+".debris");
            if(!string.IsNullOrEmpty(icon?.texture))icon.Validate(label+".icon");if(!string.IsNullOrEmpty(shellIcon?.texture))shellIcon.Validate(label+".shellIcon");
            foreach(var s in new[]{sound,boardSound,exitSound,ejectSound,breakSound,voice})if(!string.IsNullOrEmpty(s)){Content.Path(s,"Audio/",label+" : son");if(s!=voice)Content.Require<AudioClip>(s,label+" : son");}
            if(!string.IsNullOrEmpty(announce))Content.Text(announce,30,label+".announce");
            animations.Validate(label,Required);
            var a=animations;idle=a.Find("idle");run=a.Find("run");jumping=a.Find("jump");fire=a.Find("fire",idle);damaged=a.Find("damaged",idle);
            damagedRun=a.Find("damagedRun",a.Find("damaged")>=0?damaged:run);wrecked=a.Find("wreck",damaged);destroyed=a.Find("destroyed");
            // The seat, the gun's pivot and the cannon's mouth are read from the image shown, as the heroes' barrels are.
            foreach(var state in new[]{idle,run,jumping,fire,damaged,damagedRun})foreach(int cell in a.states[state].cells)
                foreach(var anchor in new[]{pilot.anchor,t.anchor,c.anchor})if(!a.HasAnchor(anchor,cell))throw new ArgumentException(label+" : ancrage "+anchor+" manquant pour l'image "+cell);
        }
    }
}
