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
        [NonSerialized] Sprite[] following;
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
        // The cell offset places after this one, for frames that follow each other in the grid.
        public Sprite At(int offset)
        {
            if(offset<=0)return Sprite;var t=Texture;if(!t)return null;
            following??=new Sprite[columns*rows];int c=Mathf.Min(cell+offset,columns*rows-1);if(following[c])return following[c];
            float w=t.width/(float)columns,h=t.height/(float)rows;
            return following[c]=UnityEngine.Sprite.Create(t,new Rect(c%columns*w,t.height-(c/columns+1)*h,w,h),new Vector2(.5f,.5f),h,0,SpriteMeshType.FullRect);
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
    // A bonus a crate or a defeated enemy leaves. effect: weapon (weapon = a library weapon id, or empty to draw one from
    // the weapons table; its ammunition, crate, announce and voice come from the weapon), health (amount = points of health),
    // turbo (amount = seconds), score (amount = points) or grenades (amount = grenades).
    [Serializable] public sealed class BonusDefinition
    {
        public string id,effect,announce,voice,weapon;
        public int[] amount;
        public float scale=1.25f;
        public SpriteCell icon=new SpriteCell();
        public static readonly string[] Effects={"weapon","health","turbo","score","grenades"};
        [NonSerialized] public int kind;
        [NonSerialized] public WeaponDefinition gun;
        public bool Draws=>kind==0&&gun==null;
    }
    // The end-of-mission tally, per difficulty: points for each hostage a hero still counts, and for each second under the mission's parTime.
    [Serializable] public sealed class TallyRules {public int[] hostage,perSecond;public SpriteCell icon=new SpriteCell();}
    // One line of a weapons table: weapon crates that do not name their weapon give these, weight times in every sum of weights.
    [Serializable] public sealed class WeaponChance {public string weapon;public int weight=1;[NonSerialized] public WeaponDefinition gun;}
    // Rules every mission plays by unless it names other ones: grenades, bonuses, weapon crates and the GO sign.
    // Resources/Content/rules/<id>.json, "default" unless the mission sets "rules".
    [Serializable] public sealed class GameRules
    {
        public string id,extends;
        public GrenadeStock grenades=new GrenadeStock();
        public GrenadeRules grenade=new GrenadeRules();
        public BonusDefinition[] bonuses;
        // Bonuses by index in bonuses: crates along the route take supplies in turn, defeated enemies leave drops in turn.
        public int[] supplies,drops;
        // Per difficulty: where the first crate stands, the distance between crates, and the enemies defeated per drop.
        public float[] firstSupply,supplySpacing;
        public int[] killsPerDrop;
        // What weapon crates that name no weapon give, and in which proportions (a mission may replace it with its own "weapons").
        public WeaponChance[] weapons;
        public SpriteCell go=new SpriteCell();
        public TallyRules tally=new TallyRules();
        // How defeated enemies fall, one motion per kind of wound (a kind without one keeps the engine's short tumble),
        // and the staging of encounters (surprise sign, parachutes, doorways).
        public DeathMotion[] deaths;
        public StageRules stage=new StageRules();
        // The sign that blinks over a wave's objective (a barricade to break) while that wave holds the screen, size units tall.
        public SpriteCell objective=new SpriteCell();public float objectiveSize=1.1f;
        [NonSerialized] public DeathMotion[] motions;
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
                Content.Range(b.scale,.2f,5,bl+".scale");if(!string.IsNullOrEmpty(b.voice))Content.Path(b.voice,"Audio/",bl+".voice");
                if(b.kind==0)
                {
                    // The weapon brings its own ammunition; the crate, announce and voice default to the weapon's.
                    if(b.amount!=null&&b.amount.Length>0)throw new ArgumentException(bl+" : amount ne s'applique pas à une arme, ses munitions sont dans l'arme");
                    b.gun=null;if(!string.IsNullOrEmpty(b.weapon)){b.gun=ContentLibrary.FindWeapon(b.weapon);if(b.gun==null)throw new ArgumentException(bl+" : arme inconnue "+b.weapon);if(b.gun.Unlimited)throw new ArgumentException(bl+" : "+b.weapon+" n'a pas de munitions à donner");}
                    if(!string.IsNullOrEmpty(b.announce))Content.Text(b.announce,30,bl+".announce");
                    if(!string.IsNullOrEmpty(b.icon?.texture))b.icon.Validate(bl+".icon");
                }
                else
                {
                    if(!string.IsNullOrEmpty(b.weapon))throw new ArgumentException(bl+" : weapon ne sert qu'à effect weapon");
                    Content.PerDifficulty(b.amount,1,100000,bl+".amount");Content.Text(b.announce,30,bl+".announce");b.icon.Validate(bl+".icon");
                }
            }
            Cycle(supplies,label+".supplies");Cycle(drops,label+".drops");
            Content.PerDifficulty(firstSupply,5,500,label+".firstSupply");Content.PerDifficulty(supplySpacing,10,500,label+".supplySpacing");Content.PerDifficulty(killsPerDrop,1,500,label+".killsPerDrop");
            Table(weapons,label+".weapons",Array.Exists(bonuses,b=>b.Draws));
            go.Validate(label+".go");
            motions=new DeathMotion[Wounds.Names.Length];
            if(deaths!=null)for(int i=0;i<deaths.Length;i++){var d=deaths[i];if(d==null)throw new ArgumentException(label+".deaths : entrée vide");d.Validate(label+".deaths["+i+"]");if(motions[d.kind]!=null)throw new ArgumentException(label+".deaths : "+d.damage+" deux fois");motions[d.kind]=d;}
            stage.Validate(label+".stage");
            if(!string.IsNullOrEmpty(objective?.texture))objective.Validate(label+".objective");Content.Range(objectiveSize,.2f,5,label+".objectiveSize");
            Content.PerDifficulty(tally.hostage,0,100000,label+".tally.hostage");Content.PerDifficulty(tally.perSecond,0,10000,label+".tally.perSecond");tally.icon.Validate(label+".tally.icon");
        }
        // Also checks a mission's own table; required when some weapon crate draws from it.
        public static void Table(WeaponChance[] table,string label,bool required)
        {
            if(table==null||table.Length==0){if(required)throw new ArgumentException(label+" : au moins une arme, puisqu'une caisse d'arme ne nomme pas la sienne");return;}
            if(table.Length>16)throw new ArgumentException(label+" : seize armes au plus");
            foreach(var line in table)
            {
                if(line==null||line.weight<1||line.weight>100)throw new ArgumentException(label+" : weight entre 1 et 100");
                line.gun=ContentLibrary.FindWeapon(line.weapon);if(line.gun==null)throw new ArgumentException(label+" : arme inconnue "+line.weapon);
                if(line.gun.Unlimited)throw new ArgumentException(label+" : "+line.weapon+" n'a pas de munitions à donner");
            }
        }
        void Cycle(int[] cycle,string label)
        {
            if(cycle==null||cycle.Length<1||cycle.Length>64)throw new ArgumentException(label+" : un à 64 bonus");
            foreach(int i in cycle)if(i<0||i>=bonuses.Length)throw new ArgumentException(label+" : indice de bonus hors de la liste");
        }
    }
}
