using System;
using UnityEngine;
namespace Paname
{
    // One cell of an image cut in a grid, numbered left to right then top to bottom: icons, the grenade, the GO sign.
    [Serializable] public sealed class SpriteCell
    {
        public string texture;
        public int columns=1,rows=1,cell;
        [NonSerialized] Texture2D loaded;
        [NonSerialized] Sprite sprite;
        public Texture2D Texture=>loaded?loaded:(loaded=Resources.Load<Texture2D>(texture));
        // The cell in texture coordinates, for OnGUI.
        public Rect TexCoords=>new Rect(cell%columns/(float)columns,(rows-1-cell/columns)/(float)rows,1f/columns,1f/rows);
        // A sprite one world unit tall, centred.
        public Sprite Sprite
        {
            get
            {
                if(sprite)return sprite;var t=Texture;if(!t)return null;float w=t.width/(float)columns,h=t.height/(float)rows;
                return sprite=UnityEngine.Sprite.Create(t,new Rect(cell%columns*w,t.height-(cell/columns+1)*h,w,h),new Vector2(.5f,.5f),h,0,SpriteMeshType.FullRect);
            }
        }
        public void Validate(string label)
        {
            Content.Path(texture,"Art/",label+".texture");Content.Require<Texture2D>(texture,label);
            if(columns<1||columns>32||rows<1||rows>32||cell<0||cell>=columns*rows)throw new ArgumentException(label+" : columns et rows entre 1 et 32, cell dans la grille");
        }
    }
    // Grenades each hero starts a life with, and the most a hero can carry, per difficulty.
    [Serializable] public sealed class GrenadeStock {public int[] start,max;public bool refillOnRespawn=true;}
    // A thrown grenade (drawn size world units tall, its body a quarter of that): launched from the hand anchor of the hero's throw pose, it falls under gravity, bounces
    // a number of times, and explodes on its last landing, on touching an enemy or the boss, or when its fuse ends.
    [Serializable] public sealed class GrenadeRules
    {
        public float throwX,throwY,gravity,bounce,friction,fuse,radius,size,cooldown,pose;
        public int bounces,damage,bossDamage;
        public bool destroysBombs;
        public string throwSound;
        public SpriteCell sprite=new SpriteCell(),icon=new SpriteCell();
        [NonSerialized] public AudioClip throwClip;
    }
    // A bonus a crate or a defeated enemy leaves. effect: weapon (weapon 1 to 3, amount = ammunition), health
    // (amount = points of health), turbo (amount = seconds), score (amount = points) or grenades (amount = grenades).
    [Serializable] public sealed class BonusDefinition
    {
        public string id,effect,announce,voice;
        public int weapon;
        public int[] amount;
        public float scale=1.25f;
        public SpriteCell icon=new SpriteCell();
        public static readonly string[] Effects={"weapon","health","turbo","score","grenades"};
        [NonSerialized] public int kind;
    }
    // Rules every mission plays by unless it names other ones: grenades, bonuses and the GO sign.
    // Resources/Content/rules/<id>.json, "default" unless the mission sets "rules".
    [Serializable] public sealed class GameRules
    {
        public string id,extends;
        public GrenadeStock grenades=new GrenadeStock();
        public GrenadeRules grenade=new GrenadeRules();
        public BonusDefinition[] bonuses;
        // Bonuses by index in bonuses: crates along the route take supplies in turn, defeated enemies leave drops in turn.
        public int[] supplies,drops;
        public SpriteCell go=new SpriteCell();
        public void Validate()
        {
            string label="Règles "+id;
            var s=grenades;Content.PerDifficulty(s.start,0,99,label+".grenades.start");Content.PerDifficulty(s.max,0,99,label+".grenades.max");
            for(int d=0;d<4;d++)if(s.start[d]>s.max[d])throw new ArgumentException(label+" : grenades.start ne dépasse pas grenades.max");
            var g=grenade;string gl=label+".grenade";
            Content.Range(g.throwX,0,30,gl+".throwX");Content.Range(g.throwY,-10,30,gl+".throwY");Content.Range(g.gravity,1,100,gl+".gravity");
            Content.Range(g.bounce,0,1,gl+".bounce");Content.Range(g.friction,0,1,gl+".friction");Content.Range(g.fuse,.1f,10,gl+".fuse");
            Content.Range(g.radius,.2f,10,gl+".radius");Content.Range(g.size,.05f,2,gl+".size");Content.Range(g.cooldown,0,5,gl+".cooldown");Content.Range(g.pose,0,2,gl+".pose");
            if(g.bounces<0||g.bounces>5||g.damage<1||g.damage>500||g.bossDamage<0||g.bossDamage>1000)throw new ArgumentException(label+" : grenade.bounces 0 à 5, grenade.damage 1 à 500, grenade.bossDamage 0 à 1000");
            Content.Path(g.throwSound,"Audio/",gl+".throwSound");Content.Require<AudioClip>(g.throwSound,label+" : son du lancer");
            g.sprite.Validate(gl+".sprite");g.icon.Validate(gl+".icon");
            if(bonuses==null||bonuses.Length<1||bonuses.Length>16)throw new ArgumentException(label+" : un à seize bonus");
            for(int i=0;i<bonuses.Length;i++)
            {
                var b=bonuses[i];string bl=label+".bonuses["+i+"]";
                if(b==null||string.IsNullOrWhiteSpace(b.id))throw new ArgumentException(bl+" : id requis");
                b.kind=Array.IndexOf(BonusDefinition.Effects,b.effect);if(b.kind<0)throw new ArgumentException(bl+" : effect doit être weapon, health, turbo, score ou grenades");
                if(b.kind==0&&(b.weapon<1||b.weapon>3))throw new ArgumentException(bl+" : weapon entre 1 et 3");
                Content.PerDifficulty(b.amount,1,100000,bl+".amount");Content.Text(b.announce,30,bl+".announce");Content.Range(b.scale,.2f,5,bl+".scale");
                if(!string.IsNullOrEmpty(b.voice))Content.Path(b.voice,"Audio/",bl+".voice");
                b.icon.Validate(bl+".icon");
            }
            Cycle(supplies,label+".supplies");Cycle(drops,label+".drops");
            go.Validate(label+".go");
        }
        void Cycle(int[] cycle,string label)
        {
            if(cycle==null||cycle.Length<1||cycle.Length>64)throw new ArgumentException(label+" : un à 64 bonus");
            foreach(int i in cycle)if(i<0||i>=bonuses.Length)throw new ArgumentException(label+" : indice de bonus hors de la liste");
        }
    }
}
