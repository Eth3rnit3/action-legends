using System;
using UnityEngine;
namespace Paname
{
    // The box a prop is hit in, and the obstacle it makes while it stands: width units wide, centred on its x, height tall from its foot.
    [Serializable] public sealed class PropBox {public float width=1,height=1;}
    // A blast when the prop breaks (a barrel): enemies within radius take damage, heroes heroDamage, other props break in turn.
    [Serializable] public sealed class PropExplosion {public float radius;public int damage,heroDamage;}
    // Pieces flung when it breaks, drawn only: count cells of a sheet (cells, else the whole grid), size units tall, thrown at speed.
    [Serializable] public sealed class PropDebris
    {
        public string texture;public int columns=1,rows=1,count;public int[] cells;public float size=.45f,speed=5;
        [NonSerialized] Sprite[] sprites;
        public bool Drawn=>!string.IsNullOrEmpty(texture)&&count>0;
        public Sprite Piece(int i)
        {
            var t=Resources.Load<Texture2D>(texture);if(!t)return null;
            sprites??=new Sprite[columns*rows];int c=cells!=null&&cells.Length>0?cells[i%cells.Length]:i%(columns*rows);
            if(sprites[c])return sprites[c];float w=t.width/(float)columns,h=t.height/(float)rows;
            return sprites[c]=Sprite.Create(t,new Rect(c%columns*w,t.height-(c/columns+1)*h,w,h),new Vector2(.5f,.5f),h,0,SpriteMeshType.FullRect);
        }
        public void Validate(string label)
        {
            if(string.IsNullOrEmpty(texture)&&count==0)return;
            Content.Path(texture,"Art/",label+".texture");Content.Require<Texture2D>(texture,label);
            if(columns<1||columns>32||rows<1||rows>32||count<1||count>24)throw new ArgumentException(label+" : columns et rows de 1 à 32, count de 1 à 24");
            if(cells!=null)foreach(int c in cells)if(c<0||c>=columns*rows)throw new ArgumentException(label+".cells : case hors de la grille");
            Content.Range(size,.05f,3,label+".size");Content.Range(speed,0,30,label+".speed");
        }
    }
    // Scenery the heroes can break (Content/props): a car, a stall, a barrel, a barricade. It takes health hits (per difficulty) from
    // the heroes' shots, blasts, knife and specials, shows its "damaged" state below damagedAt of its health and its "destroyed" state
    // (or nothing) once broken. While it stands it may block the way (blocks: heroes, all or none; its top is then a floor) and stop
    // enemy shots (cover), and offer a ledge platform units high. wall raises the obstacle above the drawing (an objective
    // barricade nobody jumps over or stands on), else it is the box's height; (a stall's awning) that is jumped onto from below. Broken, it may
    // explode, fling debris, drop a reward drawn from rewards (bonuses of the mission's rules or weapons) and give score points.
    [Serializable] public sealed class PropDefinition
    {
        public string id,extends,name,blocks="heroes",sound,breakSound;
        public int[] health;
        public float damagedAt=.5f,platform,platformWidth,wall;
        public int score;
        public bool cover;
        // A vehicle that drives into it breaks it at once (a stall, crates); otherwise a prop that walls the way stops it.
        public bool crush;
        public PropBox box=new PropBox();
        public PropExplosion explosion=new PropExplosion();
        public PropDebris debris=new PropDebris();
        public HostageReward[] rewards;
        public AnimationSet animations=new AnimationSet();
        public static readonly string[] Blocks={"none","heroes","all"};
        public static readonly string[] Required={"intact"};
        [NonSerialized] public int blockKind,intact,damaged,destroyed;
        [NonSerialized] AudioClip hitClip,breakClip;
        public AudioClip HitClip=>string.IsNullOrEmpty(sound)?null:hitClip?hitClip:(hitClip=Resources.Load<AudioClip>(sound));
        public AudioClip BreakClip=>string.IsNullOrEmpty(breakSound)?null:breakClip?breakClip:(breakClip=Resources.Load<AudioClip>(breakSound));
        public float LedgeWidth=>platformWidth>0?platformWidth:box.width;
        public float Obstacle=>wall>0?wall:box.height;
        public void Validate()
        {
            string label="Décor destructible "+id;
            Content.Text(name,24,label+".name");
            blockKind=Array.IndexOf(Blocks,blocks);if(blockKind<0)throw new ArgumentException(label+" : blocks vaut none, heroes ou all");
            Content.PerDifficulty(health,1,5000,label+".health");Content.Range(damagedAt,0,1,label+".damagedAt");
            Content.Range(box.width,.3f,12,label+".box.width");Content.Range(box.height,.2f,6,label+".box.height");
            // A ledge stays within reach of a jump from the ground or from the prop's own top.
            Content.Range(platform,0,4,label+".platform");Content.Range(platformWidth,0,12,label+".platformWidth");
            Content.Range(wall,0,8,label+".wall");if(wall>0&&(wall<box.height||blockKind==0))throw new ArgumentException(label+" : wall (hauteur de l'obstacle) d'un décor qui bloque, au moins box.height");
            if(blockKind>0&&platform>0&&platform<Obstacle)throw new ArgumentException(label+" : platform au-dessus du haut de la boîte (box.height) d'un décor qui bloque");
            if(score<0||score>10000)throw new ArgumentException(label+" : score de 0 à 10000");
            var x=explosion;Content.Range(x.radius,0,8,label+".explosion.radius");
            if(x.radius>0&&(x.damage<0||x.damage>500||x.heroDamage<0||x.heroDamage>10))throw new ArgumentException(label+" : explosion.damage de 0 à 500, explosion.heroDamage de 0 à 10");
            debris.Validate(label+".debris");
            if(rewards!=null){if(rewards.Length>16)throw new ArgumentException(label+" : seize récompenses au plus");foreach(var w in rewards){if(w==null||w.weight<1||w.weight>100)throw new ArgumentException(label+" : weight des récompenses entre 1 et 100");HostageDefinition.CheckReward(w.bonus,w.weapon,label+".rewards");}}
            foreach(var s in new[]{sound,breakSound})if(!string.IsNullOrEmpty(s)){Content.Path(s,"Audio/",label+" : son");Content.Require<AudioClip>(s,label+" : son");}
            animations.Validate(label,Required);
            var a=animations;intact=a.Find("intact");damaged=a.Find("damaged",intact);destroyed=a.Find("destroyed");
        }
    }
}
