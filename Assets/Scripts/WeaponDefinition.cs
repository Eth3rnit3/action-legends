using System;
using UnityEngine;
namespace Paname
{
    // What a weapon fires. The engine knows four generic projectiles:
    // bullet: flies straight (pellets fan out by spread degrees, jitter scatters each shot by a fixed pattern);
    // flame: the same, drawn with its frames over its life, growing by growth while acceleration (negative) slows it;
    // rocket: a bullet that leaves smoke, speeds up by acceleration and turns towards the nearest enemy at homing degrees per second;
    // beam: an instant ray of range units that hits along its whole length, drawn for life seconds.
    // pierce: enemies a projectile passes through after the first (-1: all of them, each once); radius widens its hits.
    [Serializable] public sealed class WeaponProjectile
    {
        public string type="bullet",color;
        public int pellets=1,pierce,frames=1;
        public float speed=36,life=1.25f,spread,jitter,acceleration,homing,range,radius,size=1,growth;
        public SpriteCell sprite=new SpriteCell();
        public static readonly string[] Types={"bullet","flame","rocket","beam"};
        [NonSerialized] public int kind;
        [NonSerialized] public Color tint=Color.white;
        [NonSerialized] public bool drawn;
        public bool Beam=>kind==3;
    }
    // Hurts every enemy whose centre is within radius of where the projectile ends: on an enemy, the ground,
    // or at the end of its life. damage 0 uses the projectile's; bossDamage also reaches the boss within the radius.
    [Serializable] public sealed class WeaponExplosion {public float radius;public int damage,bossDamage;}
    // shake: camera shake per shot; push: speed in units per second the shot pushes the hero back, fading at once.
    [Serializable] public sealed class WeaponRecoil {public float shake,push;}
    // A weapon of the library, Resources/Content/weapons/<id>.json. Ammunition is per difficulty; a weapon without any
    // (every value 0) never runs out: heroes start with one ("weapon" of the hero) and get it back when they die or a
    // lettered weapon runs dry. flash: none, light or heavy muzzle flash.
    [Serializable] public sealed class WeaponDefinition
    {
        public string id,extends,letter,name,announce,voice,sound,flash="light";
        // The wound it gives (bullet, explosion, fire, knife), which decides how enemies fall; by default fire for a flame,
        // explosion when it explodes, bullet otherwise.
        public string damageType;
        [NonSerialized] public int wound;
        public int damage;
        public int[] ammo;
        public float fireRate;
        // What its projectiles and rays do to a boss, times their damage (rounded, at least 1 unless 0): a vehicle's gun
        // can sweep a street without melting the bosses. The explosion's own bossDamage is not scaled.
        public float bossFactor=1;
        public bool casings=true;
        public WeaponProjectile projectile=new WeaponProjectile();
        public WeaponExplosion explosion=new WeaponExplosion();
        public WeaponRecoil recoil=new WeaponRecoil();
        // icon: the badge drawn by the ammunition counter; crate: the crate a bonus of this weapon looks like.
        public SpriteCell icon=new SpriteCell(),crate=new SpriteCell();
        public static readonly string[] Flashes={"none","light","heavy"};
        [NonSerialized] public int hash,flashKind;
        [NonSerialized] public AudioClip clip;
        public bool Unlimited=>ammo==null||ammo.Length==0||ammo[0]==0;
        public AudioClip Clip=>clip?clip:(clip=Resources.Load<AudioClip>(sound));
        public bool HasIcon=>!string.IsNullOrEmpty(icon?.texture);
        public int BossDamage(int amount)=>bossFactor==1?amount:bossFactor<=0?0:Mathf.Max(1,Mathf.RoundToInt(amount*bossFactor));
        public void Validate()
        {
            string label="Arme "+id;
            Content.Text(name,24,label+".name");
            if(ammo==null||ammo.Length==0)ammo=new int[4];Content.PerDifficulty(ammo,0,9999,label+".ammo");
            bool none=Array.TrueForAll(ammo,a=>a==0),all=Array.TrueForAll(ammo,a=>a>0);
            if(!none&&!all)throw new ArgumentException(label+" : ammo vaut 0 partout (illimitée) ou au moins 1 partout");
            if(!Unlimited)
            {
                if(string.IsNullOrEmpty(letter)||letter.Length!=1||!(letter[0]>='A'&&letter[0]<='Z'))throw new ArgumentException(label+" : letter, une lettre majuscule, est requise pour une arme à munitions");
                Content.Text(announce,30,label+".announce");crate.Validate(label+".crate");
            }
            else if(!string.IsNullOrEmpty(letter)&&(letter.Length!=1||!(letter[0]>='A'&&letter[0]<='Z')))throw new ArgumentException(label+" : letter est une lettre majuscule");
            if(!string.IsNullOrEmpty(announce))Content.Text(announce,30,label+".announce");
            if(!string.IsNullOrEmpty(voice))Content.Path(voice,"Audio/",label+".voice");
            if(HasIcon)icon.Validate(label+".icon");
            if(!string.IsNullOrEmpty(crate?.texture))crate.Validate(label+".crate");
            Content.Range(fireRate,.02f,3,label+".fireRate");Content.Range(bossFactor,0,4,label+".bossFactor");
            if(damage<1||damage>200)throw new ArgumentException(label+" : damage entre 1 et 200");
            Content.Path(sound,"Audio/",label+".sound");Content.Require<AudioClip>(sound,label+" : son du tir");
            flashKind=Array.IndexOf(Flashes,flash);if(flashKind<0)throw new ArgumentException(label+" : flash doit être none, light ou heavy");
            var p=projectile;string pl=label+".projectile";
            p.kind=Array.IndexOf(WeaponProjectile.Types,p.type);if(p.kind<0)throw new ArgumentException(pl+".type doit être bullet, flame, rocket ou beam");
            if(p.pellets<1||p.pellets>9||p.pierce<-1||p.pierce>20)throw new ArgumentException(pl+" : pellets de 1 à 9, pierce de -1 à 20");
            Content.Range(p.life,.016f,5,pl+".life");Content.Range(p.spread,0,90,pl+".spread");Content.Range(p.jitter,0,30,pl+".jitter");
            Content.Range(p.radius,0,3,pl+".radius");Content.Range(p.size,.05f,5,pl+".size");Content.Range(p.growth,0,10,pl+".growth");
            if(p.Beam)Content.Range(p.range,1,40,pl+".range");
            else{Content.Range(p.speed,1,80,pl+".speed");Content.Range(p.acceleration,-200,200,pl+".acceleration");Content.Range(p.homing,0,1440,pl+".homing");}
            p.drawn=!string.IsNullOrEmpty(p.sprite?.texture);
            if(p.drawn){p.sprite.Validate(pl+".sprite");if(p.frames<1||p.frames>16||p.sprite.cell+p.frames>p.sprite.columns*p.sprite.rows)throw new ArgumentException(pl+" : frames de 1 à 16, dans la grille à partir de cell");}
            else if(p.Beam||p.kind==1)throw new ArgumentException(pl+" : un rayon ou une flamme se dessine avec sprite");
            p.tint=Color.white;
            if(!string.IsNullOrEmpty(p.color)&&(p.color.Length!=6||!ColorUtility.TryParseHtmlString("#"+p.color,out p.tint)))throw new ArgumentException(pl+".color : six chiffres hexadécimaux (RRGGBB)");
            var e=explosion;Content.Range(e.radius,0,10,label+".explosion.radius");
            if(e.damage<0||e.damage>500||e.bossDamage<0||e.bossDamage>1000)throw new ArgumentException(label+" : explosion.damage de 0 à 500, explosion.bossDamage de 0 à 1000");
            Content.Range(recoil.shake,0,1,label+".recoil.shake");Content.Range(recoil.push,0,20,label+".recoil.push");
            wound=string.IsNullOrEmpty(damageType)?(p.kind==1?Wounds.Fire:e.radius>0?Wounds.Explosion:Wounds.Bullet):Wounds.Parse(damageType,label+".damageType");
            // Mixed into the online state hash: the same on every browser, unlike string.GetHashCode.
            hash=-2128831035;foreach(char c in id)hash=unchecked((hash^c)*16777619);
        }
    }
}
