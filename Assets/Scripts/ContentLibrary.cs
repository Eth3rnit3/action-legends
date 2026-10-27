using System;
using System.Collections.Generic;
using UnityEngine;
namespace Paname
{
    // A grid of animation cells in one image, numbered left to right then top to bottom. Crops and
    // the pivot are fractions of a cell (pivot from its bottom-left); a cell measures cellHeight
    // world units in height, or cellWidth in width when cellHeight is 0.
    [Serializable] public sealed class SpriteSheet
    {
        public string texture;
        public int columns=1,rows=1;
        public float cropLeft,cropRight,cropTop,cropBottom,cellHeight,cellWidth;
        public Vector2 pivot=new Vector2(.5f,.5f);
    }
    // A named state plays its cells at fps (time spent in the state), or with perStride images per
    // unit of the actor's stride clock, which only runs while it moves: legs keep pace with the ground.
    [Serializable] public sealed class AnimationState {public string name;public int[] cells;public float fps,perStride;public bool loop=true;}
    // A point drawn on a cell, in world units from the pivot, facing the way the sheet is drawn; cell -1 is every cell.
    [Serializable] public sealed class AnimationAnchor {public string name;public int cell=-1;public float x,y;}
    [Serializable] public sealed class AnimationSet
    {
        public SpriteSheet sheet=new SpriteSheet();
        public AnimationState[] states;
        public AnimationAnchor[] anchors;
        public int layer=30;
        [NonSerialized] Texture2D loaded;
        [NonSerialized] Sprite[] sprites;
        [NonSerialized] Dictionary<string,Vector2[]> points;
        public Texture2D Texture=>loaded?loaded:(loaded=Resources.Load<Texture2D>(sheet.texture));
        public int CellCount=>sheet.columns*sheet.rows;
        public int Find(string name){if(states!=null)for(int i=0;i<states.Length;i++)if(states[i].name==name)return i;return -1;}
        public int Find(string name,int fallback){int i=Find(name);return i>=0?i:fallback;}
        public int Cell(int state,float time,float stride)
        {
            var s=states[state];int n=s.cells.Length,i=s.perStride>0?(int)(stride*s.perStride):s.fps>0?(int)(time*s.fps):0;
            return s.cells[s.loop?i%n:Mathf.Min(i,n-1)];
        }
        public Sprite Sprite(int cell)
        {
            var t=Texture;if(!t)return null;
            if(sprites==null||sprites.Length!=CellCount)sprites=new Sprite[CellCount];
            cell=Mathf.Clamp(cell,0,CellCount-1);if(sprites[cell])return sprites[cell];
            var s=sheet;float w=t.width/(float)s.columns,h=t.height/(float)s.rows;
            float left=w*s.cropLeft,right=w*s.cropRight,top=h*s.cropTop,bottom=h*s.cropBottom;
            return sprites[cell]=UnityEngine.Sprite.Create(t,new Rect(cell%s.columns*w+left,t.height-(cell/s.columns+1)*h+bottom,w-left-right,h-top-bottom),
                new Vector2((w*s.pivot.x-left)/(w-left-right),(h*s.pivot.y-bottom)/(h-top-bottom)),s.cellHeight>0?h/s.cellHeight:w/s.cellWidth,0,SpriteMeshType.FullRect);
        }
        public void Prewarm(){if(states!=null)foreach(var s in states)foreach(int cell in s.cells)Sprite(cell);}
        // One point per cell for an anchor name: the cell's own point, else the one for every cell.
        public Vector2[] Points(string name)
        {
            points??=new Dictionary<string,Vector2[]>();
            if(points.TryGetValue(name,out var cached))return cached;
            var result=new Vector2[CellCount];var set=new bool[CellCount];
            if(anchors!=null)foreach(var a in anchors)if(a.name==name&&a.cell<0)for(int c=0;c<result.Length;c++)if(!set[c])result[c]=new Vector2(a.x,a.y);
            if(anchors!=null)foreach(var a in anchors)if(a.name==name&&a.cell>=0&&a.cell<result.Length){result[a.cell]=new Vector2(a.x,a.y);set[a.cell]=true;}
            return points[name]=result;
        }
        public bool HasAnchor(string name,int cell){if(anchors!=null)foreach(var a in anchors)if(a.name==name&&(a.cell<0||a.cell==cell))return true;return false;}
        public void Validate(string label,params string[] required)
        {
            Content.Path(sheet?.texture,"Art/",label+".sheet.texture");Content.Require<Texture2D>(sheet.texture,label+" : planche");
            if(sheet.columns<1||sheet.columns>32||sheet.rows<1||sheet.rows>32)throw new ArgumentException(label+" : columns et rows entre 1 et 32");
            if(!(sheet.cropLeft>=0&&sheet.cropRight>=0&&sheet.cropTop>=0&&sheet.cropBottom>=0&&sheet.cropLeft+sheet.cropRight<.9f&&sheet.cropTop+sheet.cropBottom<.9f))throw new ArgumentException(label+" : recadrage invalide");
            if(!(sheet.pivot.x>=0&&sheet.pivot.x<=1&&sheet.pivot.y>=0&&sheet.pivot.y<=1))throw new ArgumentException(label+" : pivot entre 0 et 1");
            if(!(sheet.cellHeight>0&&sheet.cellHeight<=50||sheet.cellHeight==0&&sheet.cellWidth>0&&sheet.cellWidth<=50))throw new ArgumentException(label+" : cellHeight ou cellWidth entre 0 et 50");
            if(states==null||states.Length<1||states.Length>64)throw new ArgumentException(label+" : un à 64 états d'animation");
            for(int i=0;i<states.Length;i++)
            {
                var s=states[i];
                if(s==null||string.IsNullOrWhiteSpace(s.name)||s.name.Length>30||Find(s.name)!=i)throw new ArgumentException(label+" : chaque état a un nom unique");
                if(s.cells==null||s.cells.Length<1||s.cells.Length>64)throw new ArgumentException(label+" : l'état "+s.name+" a une à 64 images");
                foreach(int c in s.cells)if(c<0||c>=CellCount)throw new ArgumentException(label+" : l'état "+s.name+" utilise une image hors de la planche");
                if(!(s.fps>=0&&s.fps<=60&&s.perStride>=0&&s.perStride<=10))throw new ArgumentException(label+" : fps entre 0 et 60, perStride entre 0 et 10");
            }
            foreach(var name in required)if(Find(name)<0)throw new ArgumentException(label+" : état "+name+" requis");
            if(anchors!=null)foreach(var a in anchors)
                if(a==null||string.IsNullOrWhiteSpace(a.name)||a.cell<-1||a.cell>=CellCount||!(Mathf.Abs(a.x)<=20&&Mathf.Abs(a.y)<=20))throw new ArgumentException(label+" : ancrage invalide");
            if(layer<-100||layer>100)throw new ArgumentException(label+" : layer entre -100 et 100");
        }
    }
    [Serializable] public sealed class HeroSpecial
    {
        // "blast": strikes everything around the hero; "volley": a fan of explosive shots.
        public string type;
        public float cooldown,cooldownPerDifficulty,radius,bossReach,invulnerable,leap,speed,spread;
        public int damage,bossDamage,shots;
        public bool explosive;
        public static readonly string[] Types={"blast","volley"};
    }
    // Where enemy shots hit the hero: a circle y above the feet. Crouched, the circle is lower and the hero slower,
    // so shots fired at chest height fly over.
    [Serializable] public sealed class HeroHitbox {public float y=1,radius=.53f;}
    [Serializable] public sealed class HeroCrouch {public float speed,y,radius;}
    // With the fire button, an enemy within reach in front of the hero (and the hero below height) is stabbed instead of shot.
    // The square of the portrait cell shown in the menu, in fractions of the cell from its bottom-left corner.
    [Serializable] public sealed class PortraitFrame {public float x,y,size=1;}
    [Serializable] public sealed class HeroKnife {public float reach,height,cooldown,pose;public int damage;public string sound;[NonSerialized] public AudioClip clip;}
    // A playable hero. Heroes are listed by order, then id: replays and online play refer to them by that position.
    // weapon: the library weapon (Content/weapons) the hero starts each life with, never out of ammunition.
    [Serializable] public sealed class HeroDefinition
    {
        public string id,name,shortName,role,specialName,weapon;
        public int order,portraitCell;
        public float speed,jump,strideRate,bob;
        // Moved to the hero's weapon in lot 1b; still read only to refuse them with a pointer to the weapon.
        public int damage;public float fireRate,recoilShake;
        public HeroSpecial special=new HeroSpecial();
        public HeroHitbox hitbox=new HeroHitbox();
        public HeroCrouch crouch=new HeroCrouch();
        public HeroKnife knife=new HeroKnife();
        public PortraitFrame portraitFrame=new PortraitFrame();
        public AnimationSet animations=new AnimationSet();
        // States the engine reads; the optional ones fall back to a near pose: crouchWalk and crouchShoot to crouch, aimDiagonal to aimUp.
        public static readonly string[] Required={"idle","run","jump","crouch","aimUp","jumpDown","knife","throw"};
        [NonSerialized] public int idle,run,jumping,crouching,crouchWalk,crouchShoot,aimUp,aimDiagonal,jumpDown,stab,throwing;
        [NonSerialized] public Vector2[] muzzles;
        // Where a thrown grenade leaves the hand, in the last image of the throw.
        [NonSerialized] public Vector2 hand;
        [NonSerialized] public bool blast;
        [NonSerialized] public WeaponDefinition Weapon;
        public void Validate()
        {
            string label="Héros "+id;
            Content.Text(name,24,label+".name");Content.Text(shortName,12,label+".shortName");Content.Text(role,24,label+".role");Content.Text(specialName,30,label+".specialName");
            if(order<0||order>999)throw new ArgumentException(label+" : order entre 0 et 999");
            Content.Range(speed,1,30,label+".speed");Content.Range(jump,1,30,label+".jump");Content.Range(strideRate,.1f,60,label+".strideRate");Content.Range(bob,0,1,label+".bob");
            if(damage!=0||fireRate!=0||recoilShake!=0)throw new ArgumentException(label+" : damage, fireRate et recoilShake se règlent dans son arme (weapon, Content/weapons)");
            Weapon=ContentLibrary.FindWeapon(weapon);if(Weapon==null)throw new ArgumentException(label+" : weapon inconnue "+weapon+" (Content/weapons/<id>.json)");
            if(!Weapon.Unlimited)throw new ArgumentException(label+" : son arme de base ne doit pas manquer de munitions (ammo à 0)");
            int type=Array.IndexOf(HeroSpecial.Types,special.type);if(type<0)throw new ArgumentException(label+" : special.type doit être blast ou volley");
            blast=type==0;var s=special;
            Content.Range(s.cooldown,.5f,60,label+".special.cooldown");Content.Range(s.cooldownPerDifficulty,0,20,label+".special.cooldownPerDifficulty");
            if(s.damage<1||s.damage>100)throw new ArgumentException(label+" : special.damage entre 1 et 100");
            if(blast){Content.Range(s.radius,.5f,15,label+".special.radius");Content.Range(s.bossReach,0,10,label+".special.bossReach");Content.Range(s.invulnerable,0,5,label+".special.invulnerable");Content.Range(s.leap,0,30,label+".special.leap");if(s.bossDamage<0||s.bossDamage>500)throw new ArgumentException(label+" : special.bossDamage entre 0 et 500");}
            else{Content.Range(s.speed,1,60,label+".special.speed");Content.Range(s.spread,0,10,label+".special.spread");if(s.shots<1||s.shots>15)throw new ArgumentException(label+" : special.shots entre 1 et 15");}
            Content.Range(hitbox.y,.1f,5,label+".hitbox.y");Content.Range(hitbox.radius,.1f,3,label+".hitbox.radius");
            Content.Range(crouch.speed,0,30,label+".crouch.speed");Content.Range(crouch.y,.05f,5,label+".crouch.y");Content.Range(crouch.radius,.1f,3,label+".crouch.radius");
            var k=knife;Content.Range(k.reach,.1f,5,label+".knife.reach");Content.Range(k.height,0,5,label+".knife.height");Content.Range(k.cooldown,.02f,3,label+".knife.cooldown");Content.Range(k.pose,0,2,label+".knife.pose");
            if(k.damage<1||k.damage>500)throw new ArgumentException(label+" : knife.damage entre 1 et 500");
            Content.Path(k.sound,"Audio/",label+".knife.sound");Content.Require<AudioClip>(k.sound,label+" : son du couteau");
            animations.Validate(label,Required);
            if(portraitCell<0||portraitCell>=animations.CellCount)throw new ArgumentException(label+" : portraitCell hors de la planche");
            var f=portraitFrame;if(!(f.size>.05f&&f.x>=0&&f.y>=0&&f.x+f.size<=1.0001f&&f.y+f.size<=1.0001f))throw new ArgumentException(label+" : portraitFrame dans la case (x, y, size entre 0 et 1)");
            var a=animations;idle=a.Find("idle");run=a.Find("run");jumping=a.Find("jump");crouching=a.Find("crouch");crouchWalk=a.Find("crouchWalk",crouching);crouchShoot=a.Find("crouchShoot",crouching);
            aimUp=a.Find("aimUp");aimDiagonal=a.Find("aimDiagonal",aimUp);jumpDown=a.Find("jumpDown");stab=a.Find("knife");throwing=a.Find("throw");
            // Shots leave from the image shown, so every pose the hero fires from needs its barrel tip; the knife and the throw do not fire.
            foreach(var state in new[]{idle,run,jumping,crouching,crouchWalk,crouchShoot,aimUp,aimDiagonal,jumpDown})foreach(int cell in a.states[state].cells)if(!a.HasAnchor("muzzle",cell))throw new ArgumentException(label+" : ancrage muzzle manquant pour l'image "+cell);
            muzzles=a.Points("muzzle");
            var thrown=a.states[throwing].cells;int release=thrown[thrown.Length-1];
            if(!a.HasAnchor("hand",release))throw new ArgumentException(label+" : ancrage hand requis sur la dernière image de throw");hand=a.Points("hand")[release];
        }
    }
    public enum EnemyBehavior { Melee, Thrower, Gunman }
    // Hits a hero within reach, below a height (a jump clears it). Every unarmed enemy strikes up close.
    [Serializable] public sealed class EnemyMelee {public float reach,height,cooldown;public int damage;}
    [Serializable] public sealed class EnemyThrow {public float range,cooldown,aimHeight;public float[] speed;public int damage;public string projectile;}
    [Serializable] public sealed class EnemyGun
    {
        public float range,aimTime,cooldown,burstInterval,spread,spreadStep,life,pose,shake;
        public int burst,pellets,damage;
        public float[] speed;
        public string sound,projectile;
        public bool heavyFlash;
        [NonSerialized] public AudioClip clip;
    }
    // An enemy type: a behaviour of the engine with its numbers, its sheet and its animations.
    [Serializable] public sealed class EnemyDefinition
    {
        public string id,extends,behavior;
        public float health,speed,keepDistance,hitRadius,knockback,firstAttack,attackPose,strideRate;
        public int bonusScore;
        public bool armored;
        public EnemyMelee melee=new EnemyMelee();
        public EnemyThrow @throw=new EnemyThrow();
        public EnemyGun gun=new EnemyGun();
        public AnimationSet animations=new AnimationSet();
        public static readonly string[] Behaviors={"melee","thrower","gunman"};
        [NonSerialized] public EnemyBehavior kind;
        [NonSerialized] public int idle,run,attack,aim,hurt,death;
        // Where the gun fires or the bottle leaves the hand, in the attack pose.
        [NonSerialized] public Vector2 muzzle,hand;
        public bool Armed=>kind==EnemyBehavior.Gunman;
        public void Validate()
        {
            string label="Ennemi "+id;
            int b=Array.IndexOf(Behaviors,behavior);if(b<0)throw new ArgumentException(label+" : behavior doit être melee, thrower ou gunman");kind=(EnemyBehavior)b;
            Content.Range(health,1,500,label+".health");Content.Range(speed,0,30,label+".speed");Content.Range(keepDistance,0,20,label+".keepDistance");
            Content.Range(hitRadius,.1f,3,label+".hitRadius");Content.Range(knockback,0,20,label+".knockback");Content.Range(firstAttack,0,10,label+".firstAttack");
            Content.Range(attackPose,0,5,label+".attackPose");Content.Range(strideRate,.1f,60,label+".strideRate");
            if(bonusScore<0||bonusScore>10000)throw new ArgumentException(label+" : bonusScore entre 0 et 10000");
            animations.Validate(label,"idle","run","attack");
            idle=animations.Find("idle");run=animations.Find("run");attack=animations.Find("attack");aim=animations.Find("aim",attack);hurt=animations.Find("hurt");death=animations.Find("death");
            int pose=animations.states[attack].cells[0];
            if(!Armed){var m=melee;Content.Range(m.reach,.1f,5,label+".melee.reach");Content.Range(m.height,0,5,label+".melee.height");Content.Range(m.cooldown,.05f,20,label+".melee.cooldown");if(m.damage<1||m.damage>10)throw new ArgumentException(label+" : melee.damage entre 1 et 10");}
            if(kind==EnemyBehavior.Thrower)
            {
                var t=@throw;Content.Range(t.range,.5f,30,label+".throw.range");Content.Range(t.cooldown,.1f,20,label+".throw.cooldown");Content.Range(t.aimHeight,0,5,label+".throw.aimHeight");
                Content.PerDifficulty(t.speed,1,60,label+".throw.speed");if(t.damage<1||t.damage>10)throw new ArgumentException(label+" : throw.damage entre 1 et 10");
                Content.Look(t.projectile,label+".throw.projectile");
                if(!animations.HasAnchor("hand",pose))throw new ArgumentException(label+" : ancrage hand requis sur l'image d'attaque");hand=animations.Points("hand")[pose];
            }
            if(Armed)
            {
                var g=gun;Content.Range(g.range,.5f,30,label+".gun.range");Content.Range(g.aimTime,0,5,label+".gun.aimTime");Content.Range(g.cooldown,.1f,20,label+".gun.cooldown");
                Content.Range(g.burstInterval,.02f,2,label+".gun.burstInterval");Content.Range(g.spread,-45,45,label+".gun.spread");Content.Range(g.spreadStep,-45,45,label+".gun.spreadStep");
                Content.Range(g.life,.05f,5,label+".gun.life");Content.Range(g.pose,0,2,label+".gun.pose");Content.Range(g.shake,0,1,label+".gun.shake");
                if(g.burst<1||g.burst>12||g.pellets<1||g.pellets>9||g.damage<1||g.damage>10)throw new ArgumentException(label+" : gun.burst 1 à 12, gun.pellets 1 à 9, gun.damage 1 à 10");
                Content.PerDifficulty(g.speed,1,60,label+".gun.speed");Content.Look(g.projectile,label+".gun.projectile");
                Content.Path(g.sound,"Audio/",label+".gun.sound");Content.Require<AudioClip>(g.sound,label+" : son du tir");
                if(!animations.HasAnchor("muzzle",pose))throw new ArgumentException(label+" : ancrage muzzle requis sur l'image d'attaque");muzzle=animations.Points("muzzle")[pose];
            }
        }
    }
    public static class Content
    {
        // Projectile looks drawn by the engine.
        public static readonly string[] Looks={"tracer","molotov","bullet"};
        public static void Look(string look,string label){if(Array.IndexOf(Looks,look)<0)throw new ArgumentException(label+" : tracer, molotov ou bullet");}
        public static void Range(float value,float low,float high,string label){if(!(value>=low&&value<=high))throw new ArgumentException(label+" doit être entre "+low+" et "+high);}
        // Values per difficulty: DÉTENTE, ARCADE, DIFFICILE, LÉGENDE.
        public static void PerDifficulty(float[] values,float low,float high,string label){if(values==null||values.Length!=4)throw new ArgumentException(label+" : quatre valeurs, une par difficulté");foreach(var v in values)Range(v,low,high,label);}
        public static void PerDifficulty(int[] values,int low,int high,string label){if(values==null||values.Length!=4)throw new ArgumentException(label+" : quatre valeurs, une par difficulté");foreach(var v in values)if(v<low||v>high)throw new ArgumentException(label+" doit être entre "+low+" et "+high);}
        public static void Text(string value,int limit,string label){if(string.IsNullOrWhiteSpace(value)||value.Length>limit)throw new ArgumentException(label+" : texte requis, "+limit+" caractères au plus");}
        public static void Path(string path,string prefix,string label){if(string.IsNullOrEmpty(path)||!path.StartsWith(prefix,StringComparison.Ordinal)||path.Contains("..")||path.Contains(":")||path.Contains("\\"))throw new ArgumentException("Chemin invalide pour "+label);}
        public static void Require<T>(string path,string label) where T:UnityEngine.Object{if(!Resources.Load<T>(path))throw new ArgumentException(label+" manquant : "+path);}
    }
    // Weapons, heroes, enemies, bosses and rules shared by every mission, one JSON per entry under Resources/Content/<kind>/.
    // An entry may name a parent with "extends": the parent is read first, then the entry's own fields replace its values.
    public static class ContentLibrary
    {
        [Serializable] sealed class Header {public string id,extends;}
        static HeroDefinition[] heroes;
        static readonly Dictionary<string,EnemyDefinition> enemies=new Dictionary<string,EnemyDefinition>();
        static readonly Dictionary<string,BossDefinition> bosses=new Dictionary<string,BossDefinition>();
        static readonly Dictionary<string,GameRules> rules=new Dictionary<string,GameRules>();
        static readonly Dictionary<string,WeaponDefinition> weapons=new Dictionary<string,WeaponDefinition>();
        static readonly Dictionary<string,string> sources=new Dictionary<string,string>();
        static bool loaded;
        public static HeroDefinition[] Heroes{get{Load();return heroes;}}
        public static EnemyDefinition Enemy(string id){Load();if(id!=null&&enemies.TryGetValue(id,out var e))return e;throw new ArgumentException("Ennemi inconnu : "+id);}
        public static BossDefinition Boss(string id){Load();if(id!=null&&bosses.TryGetValue(id,out var b))return b;throw new ArgumentException("Boss inconnu : "+id);}
        public static GameRules Rules(string id){Load();if(id!=null&&rules.TryGetValue(id,out var r))return r;throw new ArgumentException("Règles inconnues : "+id);}
        public static WeaponDefinition Weapon(string id){Load();return FindWeapon(id)??throw new ArgumentException("Arme inconnue : "+id);}
        // While loading too: heroes, rules and missions resolve their weapons through it.
        public static WeaponDefinition FindWeapon(string id)=>id!=null&&weapons.TryGetValue(id,out var w)?w:null;
        public static void Load()
        {
            if(loaded)return;
            sources.Clear();enemies.Clear();bosses.Clear();rules.Clear();weapons.Clear();
            var weaponTexts=Read("weapons");var heroTexts=Read("heroes");var enemyTexts=Read("enemies");var bossTexts=Read("bosses");var ruleTexts=Read("rules");
            // Weapons first: heroes and rules name them.
            foreach(var text in weaponTexts){var weapon=Parse<WeaponDefinition>("weapons",text);weapon.Validate();weapons[weapon.id]=weapon;}
            var list=new List<HeroDefinition>();var ids=new HashSet<string>();
            foreach(var text in heroTexts){var hero=Parse<HeroDefinition>("heroes",text);hero.Validate();if(!ids.Add(hero.id))throw new ArgumentException("Héros dupliqué : "+hero.id);list.Add(hero);}
            list.Sort((a,b)=>a.order!=b.order?a.order.CompareTo(b.order):string.CompareOrdinal(a.id,b.id));
            if(list.Count<1||list.Count>4)throw new ArgumentException("Un à quatre héros sont requis dans Resources/Content/heroes");
            foreach(var text in enemyTexts){var enemy=Parse<EnemyDefinition>("enemies",text);enemy.Validate();enemies[enemy.id]=enemy;}
            foreach(var text in bossTexts){var boss=Parse<BossDefinition>("bosses",text);boss.Validate();bosses[boss.id]=boss;}
            foreach(var text in ruleTexts){var set=Parse<GameRules>("rules",text);set.Validate();rules[set.id]=set;}
            if(!rules.ContainsKey("default"))throw new ArgumentException("Resources/Content/rules/default.json est requis");
            heroes=list.ToArray();loaded=true;
        }
        static List<string> Read(string folder)
        {
            var texts=new List<string>();
            foreach(var asset in Resources.LoadAll<TextAsset>("Content/"+folder))
            {
                var header=JsonUtility.FromJson<Header>(asset.text);
                if(header==null||string.IsNullOrEmpty(header.id)||header.id!=asset.name)throw new ArgumentException("Content/"+folder+"/"+asset.name+" : id doit être le nom du fichier");
                if(!System.Text.RegularExpressions.Regex.IsMatch(header.id,"^[a-z0-9]+(-[a-z0-9]+)*$"))throw new ArgumentException(header.id+" : id en minuscules séparées par des tirets");
                sources[folder+"/"+header.id]=asset.text;texts.Add(asset.text);
            }
            return texts;
        }
        static T Parse<T>(string folder,string text,int depth=0) where T:class
        {
            var header=JsonUtility.FromJson<Header>(text);
            if(header==null||string.IsNullOrEmpty(header.id))throw new ArgumentException(folder+" : id requis");
            if(string.IsNullOrEmpty(header.extends))return JsonUtility.FromJson<T>(text);
            if(depth>8||header.extends==header.id)throw new ArgumentException(header.id+" : extends forme une boucle");
            if(!sources.TryGetValue(folder+"/"+header.extends,out var parentText))throw new ArgumentException(header.id+" : extends inconnu "+header.extends);
            var value=Parse<T>(folder,parentText,depth+1);JsonUtility.FromJsonOverwrite(text,value);return value;
        }
        // For tools and tests: adds or replaces an entry from JSON, which may extend a loaded one.
        public static EnemyDefinition AddEnemy(string json){Load();var e=Parse<EnemyDefinition>("enemies",json);e.Validate();enemies[e.id]=e;sources["enemies/"+e.id]=json;return e;}
        public static BossDefinition AddBoss(string json){Load();var b=Parse<BossDefinition>("bosses",json);b.Validate();bosses[b.id]=b;sources["bosses/"+b.id]=json;return b;}
        public static GameRules AddRules(string json){Load();var r=Parse<GameRules>("rules",json);r.Validate();rules[r.id]=r;sources["rules/"+r.id]=json;return r;}
        public static WeaponDefinition AddWeapon(string json){Load();var w=Parse<WeaponDefinition>("weapons",json);w.Validate();weapons[w.id]=w;sources["weapons/"+w.id]=json;return w;}
        public static void Reload(){loaded=false;Load();}
    }
}
