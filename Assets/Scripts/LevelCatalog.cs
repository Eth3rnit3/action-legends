using System;
using System.Collections.Generic;
using UnityEngine;
namespace Paname
{
    [Serializable] public sealed class MissionWave
    {
        public float atX;
        public int baseEnemies=7,enemiesPerPlayer=3,maxAlive=10;
        public float spawnInterval=.65f;
        public int[] enemyPattern={0};
        // A locked wave holds the screen until its last enemy falls; "lock": false lets the team run on.
        public bool @lock=true;
    }
    // From a difficulty on, one spawn in every few is this enemy of the roster instead of the pattern's; enemy -1 turns it off.
    [Serializable] public sealed class MissionExtraEnemy {public int enemy=-1,every=4,fromDifficulty=1;}
    // The mission's end boss: a library boss, with optional texts and a health multiplier of its own.
    [Serializable] public sealed class MissionBoss
    {
        public bool enabled=true;
        public string id,name,intro,rage,defeat;
        public float health=1;
        // A flyer kept to a strip of the arena (water, sky): it sweeps from left to right of the arena centre on the line y.
        public MissionLane lane=new MissionLane();
        [NonSerialized] public BossDefinition definition;
        // The library boss and the texts it lends where the mission sets none.
        public void Resolve(string label)
        {
            definition=ContentLibrary.Boss(id);var b=definition;
            if(string.IsNullOrWhiteSpace(name))name=b.name;if(string.IsNullOrWhiteSpace(intro))intro=b.intro;
            if(string.IsNullOrWhiteSpace(rage))rage=b.rage;if(string.IsNullOrWhiteSpace(defeat))defeat=b.defeat;
            if(name.Length>30||intro.Length>80||rage.Length>80||defeat.Length>80)throw new ArgumentException(label+" : textes du boss trop longs");
            if(!(health>=.25f&&health<=4))throw new ArgumentException(label+" : health doit être entre 0.25 et 4");
            lane??=new MissionLane();
            if(lane.Set&&(b.walker||!(lane.left>=-10&&lane.right<=10&&lane.right-lane.left>=1&&lane.y>=0&&lane.y<=12)))
                throw new ArgumentException(label+" : lane pour un boss volant, left et right de -10 à 10 (au moins 1 d'écart), y de 0 à 12");
        }
    }
    [Serializable] public sealed class MissionLane {public float left,right,y;public bool Set=>right>left;}
    // Relief along the street, from x to x + width (PanameTerrain.cs). platform: a thin ledge height above the street, jumped onto
    // from below, dropped through with down + jump; block: a solid box from the street up to height; slope: solid ground rising
    // (or falling) from height at x to toHeight at x + width, stairs or a ramp; pit: the street gives way, down to height below it,
    // and a lethal pit costs a hero damage and puts him back on its edge. It is drawn by sprite, the cell's bottom at drawY above
    // the street and its sides on x and x + width, drawHeight tall (else as tall as the cell's proportions make it), mirrored with
    // flip, in front of the actors with front: a platform must show what holds it.
    [Serializable] public sealed class MapTerrain
    {
        public string type;public float x,width,height,toHeight,drawY,drawHeight;public bool lethal,flip,front;public int damage=1;
        public SpriteCell sprite=new SpriteCell();
        public static readonly string[] Types={"platform","block","slope","pit"};
        public const int Platform=0,Block=1,Slope=2,Pit=3;
        [NonSerialized] public int kind;
        public float Right=>x+width;
        public bool Drawn=>!string.IsNullOrEmpty(sprite?.texture);
        // The top of a block or slope, the ledge of a platform, at a point of its span.
        public float TopAt(float at)=>kind==Slope?Mathf.Lerp(height,toHeight,(at-x)/width):height;
    }
    // A prop of the library (Content/props) standing at x, its foot at y (the street, or the top of a block, slope or platform
    // there). wave: the locked wave it is the objective of: that wave ends when it breaks, not before. reward / weapon replace
    // its own rewards, flip mirrors it.
    [Serializable] public sealed class MapProp
    {
        public string id,reward,weapon;public float x,y;public int wave=-1;public bool flip;
        [NonSerialized] public PropDefinition definition;[NonSerialized] public int bonus=-1;[NonSerialized] public WeaponDefinition gun;
    }
    // A hostage of the library tied at x (y above the street); reward / weapon replace his own table of rewards; with a
    // wave, he only appears when that wave starts, inside its locked screen.
    [Serializable] public sealed class MapHostage
    {
        public string id,reward,weapon;public float x,y;public int wave=-1;
        [NonSerialized] public HostageDefinition definition;[NonSerialized] public int bonus=-1;[NonSerialized] public WeaponDefinition gun;
    }
    [Serializable] public sealed class MapVehicle {public string id;public float x;}
    // Scenery standing in the street, drawn only: a cell of a sheet height units tall, the bottom centre of the cell at (x, y)
    // (y below 0 sinks a drawing that does not reach the bottom of its cell), mirrored with flip, in front of the actors with front.
    [Serializable] public sealed class MapDecor {public SpriteCell sprite=new SpriteCell();public float x,y,height=2;public bool flip,front;}
    // One enemy of an encounter: a library enemy that comes delay seconds after the encounter starts and takes its place at (x, y),
    // y above the street for a ledge, a window or a roof it holds. entry: none (already there, off screen ahead of the team),
    // walk (from the side of the screen, side left or right, else the side away from the team), door and window (out of an
    // opening of the rules drawn at fromX, fromY, then a leap down when it stands lower), parachute (down from the sky),
    // drop (falls from above the screen), truck (out of the encounter's truck). unaware: it starts off guard, facing facing
    // (-1 left, 1 right, 0 towards the team).
    [Serializable] public sealed class EncounterSpawn
    {
        public string enemy,entry="walk",opening,side;public float x,y,delay,fromX,fromY;public int facing;public bool unaware;
        public static readonly string[] Entries={"none","walk","door","window","parachute","drop","truck"};
        [NonSerialized] public EnemyDefinition definition;[NonSerialized] public int kind;[NonSerialized] public OpeningLook look;
    }
    // A carrier of the library (Content/carriers) that drives in from side and stops with its centre at x.
    [Serializable] public sealed class EncounterTruck {public string id,side="right";public float x;[NonSerialized] public CarrierDefinition definition;}
    // A staged encounter. It starts when the leading hero reaches atX, or with wave (on "start", its enemies then belong to the
    // wave, which waits for them) or once wave is cleared (on "clear"). lock holds the screen until its enemies have fallen.
    // boss: a library boss fought on the spot in a locked arena, mid-mission; its defeat lets the team move on.
    [Serializable] public sealed class MissionEncounter
    {
        public float atX;public int wave=-1;public string on="start",announce;public bool @lock;
        public EncounterSpawn[] spawns;public EncounterTruck truck=new EncounterTruck();public MissionBoss boss=new MissionBoss();
        public bool HasBoss=>!string.IsNullOrEmpty(boss?.id);
        public bool OnClear=>wave>=0&&on=="clear";
    }
    // Everything drawn along the course: panorama pieces in order, the image whose bottom is the street, and the reserved object lists.
    [Serializable] public sealed class MissionMap
    {
        public string[] backgrounds;
        public string ground;
        // Framing, drawing only: the walking line (y = 0) at this fraction of the panoramas' height; the bottom groundBand
        // of the ground image repeated in front of the actors with its top edge at y = groundTop, faded over groundFade of it.
        public float horizon=.2f,groundBand=.2f,groundTop,groundFade=.22f;
        public MapTerrain[] terrain;
        public MapProp[] props;
        public MapHostage[] hostages;
        public MapVehicle[] vehicles;
        public MapDecor[] decor;
    }
    [Serializable] public sealed class MissionDefinition
    {
        public const int Schema=2;
        public int schemaVersion,order=1;
        public string id,title,location,intro,exitLabel,winTitle,code,music,bossMusic;
        // Optional: an image drawn in the lobby's mission card in place of the title.
        public string logo;
        public MissionMap map=new MissionMap();
        // Library enemy ids; waves refer to them by their position in this list.
        public string[] enemies;
        public MissionExtraEnemy extraEnemy=new MissionExtraEnemy();
        public float length=80,exitX=77,cameraMin=7,cameraMax=74;
        public MissionWave[] waves;
        public MissionEncounter[] encounters;
        public MissionBoss boss=new MissionBoss();
        // Library rules (grenades, bonuses, GO sign) the mission plays by.
        public string rules="default";
        // Seconds under which the end-of-mission tally gives a time bonus (rules' tally.perSecond); 0: none.
        public float parTime;
        // Optional: the weapons this mission's weapon crates give and their weights, instead of the rules' table.
        public WeaponChance[] weapons;
        [NonSerialized] public EnemyDefinition[] roster;
        [NonSerialized] public GameRules ruleSet;
        [NonSerialized] public WeaponChance[] weaponTable;
        public static MissionDefinition Parse(string text)
        {
            var mission=JsonUtility.FromJson<MissionDefinition>(text);
            if(mission==null)throw new ArgumentException("Mission JSON vide");mission.Validate();return mission;
        }
        static void Reserved(Array list,string field){if(list!=null&&list.Length>0)throw new ArgumentException(field+" est réservé à un lot à venir : laisser la liste vide");}
        // Hostages are checked against the mission's rules: their rewards name its bonuses.
        void ValidateHostages()
        {
            if(!(parTime>=0&&parTime<=3600))throw new ArgumentException("parTime entre 0 et 3600 secondes");
            var list=map.hostages??new MapHostage[0];map.hostages=list;
            if(list.Length>40)throw new ArgumentException("map.hostages : 40 otages au plus");
            foreach(var h in list)
            {
                if(h==null)throw new ArgumentException("map.hostages : otage vide");
                string label="map.hostages "+h.id+" à x "+h.x;
                h.definition=ContentLibrary.Hostage(h.id);
                if(!(h.x>=1&&h.x<=length-1&&h.y>=0&&h.y<=10))throw new ArgumentException(label+" : x dans la mission, y entre 0 et 10");
                if(h.wave<-1||h.wave>=waves.Length)throw new ArgumentException(label+" : wave de -1 au nombre de vagues");
                if(!OnSurface(h.x,h.y))throw new ArgumentException(label+" : y à 0 (la rue) ou sur le dessus d'un relief à cet endroit");
                // A wave's hostage must stand inside the screen that the wave locks.
                if(h.wave>=0&&!(h.x>=waves[h.wave].atX-14&&h.x<=waves[h.wave].atX+2))throw new ArgumentException(label+" : un otage de vague se place entre atX - 14 et atX + 2 ("+waves[h.wave].atX+")");
                foreach(var w in h.definition.rewards)if(HostageDefinition.BonusIndex(ruleSet,w.bonus,w.weapon)<0)throw new ArgumentException(label+" : la récompense "+(w.bonus??w.weapon)+" n'est pas un bonus des règles "+ruleSet.id);
                h.bonus=-1;h.gun=null;
                if(!string.IsNullOrEmpty(h.reward)||!string.IsNullOrEmpty(h.weapon))
                {
                    HostageDefinition.CheckReward(h.reward,h.weapon,label);
                    h.bonus=HostageDefinition.BonusIndex(ruleSet,h.reward,h.weapon);if(h.bonus<0)throw new ArgumentException(label+" : récompense inconnue des règles "+ruleSet.id);
                    if(!string.IsNullOrEmpty(h.weapon))h.gun=ContentLibrary.FindWeapon(h.weapon);
                }
            }
        }
        // Where an encounter's enemies may stand so that the team sees them come: inside the screen of its wave, or of the team
        // reaching atX; an enemy already there (entry none) waits off screen ahead.
        void ValidateEncounters()
        {
            var list=encounters??new MissionEncounter[0];encounters=list;
            if(list.Length>30)throw new ArgumentException("encounters : 30 rencontres au plus");
            int bosses=0;
            for(int i=0;i<list.Length;i++)
            {
                var c=list[i];string label="encounters["+i+"]";if(c==null)throw new ArgumentException(label+" : rencontre vide");
                c.spawns??=new EncounterSpawn[0];c.truck??=new EncounterTruck();c.boss??=new MissionBoss();
                if(c.wave<-1||c.wave>=waves.Length)throw new ArgumentException(label+" : wave de -1 au nombre de vagues");
                if(c.on!="start"&&c.on!="clear")throw new ArgumentException(label+" : on vaut start ou clear");
                if(!string.IsNullOrEmpty(c.announce))Content.Text(c.announce,40,label+".announce");
                float at=c.wave>=0?waves[c.wave].atX:c.atX;
                if(c.wave<0&&!(c.atX>=1&&c.atX<exitX-1))throw new ArgumentException(label+" : atX dans la mission, avant la sortie");
                if(c.HasBoss)
                {
                    if(c.wave>=0||c.spawns.Length>0||c.@lock||!string.IsNullOrEmpty(c.truck.id))throw new ArgumentException(label+" : un boss de mi-parcours se déclenche seul, à atX (sans wave, spawns, lock ni truck)");
                    if(c.atX>exitX-30)throw new ArgumentException(label+" : un boss de mi-parcours se place au moins 30 u avant la sortie");
                    if(++bosses>3)throw new ArgumentException("encounters : trois boss de mi-parcours au plus");
                    // Waves wait while a boss is fought: its arena must not fall within the screen a wave holds.
                    foreach(var w in waves)if(c.atX>w.atX-10&&c.atX<w.atX+5)throw new ArgumentException(label+" : un boss de mi-parcours se place hors de l'écran d'une vague (atX - 10 à atX + 5, "+w.atX+")");
                    c.boss.Resolve(label+".boss");continue;
                }
                if(c.spawns.Length<1||c.spawns.Length>16)throw new ArgumentException(label+" : une à seize apparitions");
                // The screen the team looks at when the encounter starts.
                float low=c.wave>=0?at-15:at-7,high=c.wave>=0?at+2.5f:at+12;
                bool truck=!string.IsNullOrEmpty(c.truck.id);
                if(truck)
                {
                    c.truck.definition=ContentLibrary.Carrier(c.truck.id);
                    if(c.truck.side!="left"&&c.truck.side!="right")throw new ArgumentException(label+".truck.side : left ou right");
                    if(!(c.truck.x>=low&&c.truck.x<=high))throw new ArgumentException(label+".truck.x : dans l'écran de la rencontre ("+low+" à "+high+")");
                }
                foreach(var sp in c.spawns)
                {
                    if(sp==null)throw new ArgumentException(label+" : apparition vide");
                    string sl=label+" "+sp.enemy+" à x "+sp.x;
                    sp.definition=ContentLibrary.Enemy(sp.enemy);sp.kind=Array.IndexOf(enemies,sp.enemy);
                    int entry=Array.IndexOf(EncounterSpawn.Entries,sp.entry);if(entry<0)throw new ArgumentException(sl+" : entry parmi none, walk, door, window, parachute, drop, truck");
                    if(!(sp.x>=1&&sp.x<=length-1&&sp.y>=0&&sp.y<=8&&sp.delay>=0&&sp.delay<=30))throw new ArgumentException(sl+" : x dans la mission, y de 0 à 8, delay de 0 à 30");
                    if(sp.facing<-1||sp.facing>1)throw new ArgumentException(sl+" : facing vaut -1, 0 ou 1");
                    if(!string.IsNullOrEmpty(sp.side)&&sp.side!="left"&&sp.side!="right")throw new ArgumentException(sl+" : side vaut left ou right");
                    if(sp.y>0&&!sp.definition.Armed&&sp.definition.kind!=EnemyBehavior.Thrower&&!sp.definition.grenade.Enabled)throw new ArgumentException(sl+" : un ennemi de corps à corps reste dans la rue (y 0)");
                    if(entry==0)
                    {
                        if(c.wave>=0||c.@lock||!(sp.x>=c.atX+14&&sp.x<=c.atX+40))throw new ArgumentException(sl+" : entry none ne sert qu'à une rencontre déclenchée par atX, sans lock, entre atX + 14 et atX + 40 (hors de l'écran)");
                    }
                    else if(!(sp.x>=low&&sp.x<=high))throw new ArgumentException(sl+" : dans l'écran de la rencontre ("+low+" à "+high+")");
                    if((entry==1||entry==6)&&sp.y>0)throw new ArgumentException(sl+" : walk et truck arrivent dans la rue (y 0)");
                    if(entry==6&&!truck)throw new ArgumentException(sl+" : entry truck demande le truck de la rencontre");
                    sp.look=null;
                    if(entry==2||entry==3)
                    {
                        if(!(sp.fromX>=low&&sp.fromX<=high&&sp.fromY>=0&&sp.fromY<=8))throw new ArgumentException(sl+" : fromX dans l'écran de la rencontre, fromY de 0 à 8");
                        if(sp.y>0&&Mathf.Abs(sp.y-sp.fromY)>.01f)throw new ArgumentException(sl+" : depuis une porte ou une fenêtre, il descend dans la rue (y 0) ou y reste (y = fromY)");
                        string open=string.IsNullOrEmpty(sp.opening)?sp.entry:sp.opening;
                        if(open!="none"){sp.look=ruleSet.stage.Opening(open);if(sp.look==null&&!string.IsNullOrEmpty(sp.opening))throw new ArgumentException(sl+" : opening "+sp.opening+" absente des règles "+ruleSet.id+" (stage.openings)");}
                    }
                    else if(!string.IsNullOrEmpty(sp.opening))throw new ArgumentException(sl+" : opening ne sert qu'aux entrées door et window");
                }
            }
        }
        // The boss arenas stay flat: the bosses' moves read the heroes' height above the street.
        bool InArena(float left,float right,out string arena)
        {
            arena=null;
            if(boss.enabled&&right>exitX-30){arena="l'arène du boss (à partir de exitX - 30)";return true;}
            foreach(var c in encounters)if(c.HasBoss&&right>c.atX-12.5f&&left<c.atX+15){arena="l'arène du boss de mi-parcours à "+c.atX+" (atX - 12,5 à atX + 15)";return true;}
            return false;
        }
        void ValidateTerrain()
        {
            var list=map.terrain??new MapTerrain[0];map.terrain=list;
            if(list.Length>60)throw new ArgumentException("map.terrain : 60 reliefs au plus");
            for(int i=0;i<list.Length;i++)
            {
                var t=list[i];string label="map.terrain["+i+"]";if(t==null)throw new ArgumentException(label+" : relief vide");
                t.sprite??=new SpriteCell();t.kind=Array.IndexOf(MapTerrain.Types,t.type);
                if(t.kind<0)throw new ArgumentException(label+" : type parmi platform, block, slope, pit");
                if(!(t.x>=1&&t.Right<=length-1&&t.width>=.5f&&t.width<=60))throw new ArgumentException(label+" : x et x + width dans la mission, width de 0,5 à 60");
                if(t.kind==MapTerrain.Platform&&!(t.height>=.4f&&t.height<=6))throw new ArgumentException(label+" : une plateforme a une height de 0,4 à 6");
                if(t.kind==MapTerrain.Block&&!(t.height>=.2f&&t.height<=4))throw new ArgumentException(label+" : un bloc a une height de 0,2 à 4");
                if(t.kind==MapTerrain.Slope&&!(t.height>=0&&t.height<=4&&t.toHeight>=0&&t.toHeight<=4&&Mathf.Abs(t.toHeight-t.height)<=t.width))throw new ArgumentException(label+" : une pente va de height à toHeight (0 à 4), pas plus raide qu'une unité par unité");
                if(t.kind==MapTerrain.Pit&&!(t.height>=.3f&&t.height<=8&&t.width<=8&&t.damage>=0&&t.damage<=10))throw new ArgumentException(label+" : un trou a une height (profondeur) de 0,3 à 8, une width de 8 au plus, damage de 0 à 10");
                if(t.kind!=MapTerrain.Pit&&t.lethal)throw new ArgumentException(label+" : lethal ne sert qu'aux trous");
                if(InArena(t.x,t.Right,out var arena))throw new ArgumentException(label+" : pas de relief dans "+arena);
                // What stands there must be seen: platforms, blocks and slopes are drawn.
                if(t.kind!=MapTerrain.Pit&&!t.Drawn)throw new ArgumentException(label+" : un relief est dessiné (sprite), sinon on ne le voit pas");
                if(t.Drawn){t.sprite.Validate(label+".sprite");if(!(t.drawY>=-8&&t.drawY<=6&&t.drawHeight>=0&&t.drawHeight<=12))throw new ArgumentException(label+" : drawY de -8 à 6, drawHeight de 0 à 12");}
                // Solid ground does not overlap: a pit beside a block, a block between two slopes.
                if(t.kind!=MapTerrain.Platform)for(int j=0;j<i;j++){var o=list[j];if(o.kind!=MapTerrain.Platform&&t.x<o.Right-.001f&&o.x<t.Right-.001f)throw new ArgumentException(label+" : chevauche map.terrain["+j+"] (seules les plateformes se superposent)");}
            }
        }
        // The tops of the terrain at x: where a hostage (ledges too) or a prop (solid ground only) may stand.
        bool OnSurface(float x,float y,bool ledges=true)
        {
            if(Mathf.Abs(y)<.01f){foreach(var t in map.terrain??new MapTerrain[0])if(t.kind!=MapTerrain.Platform&&x>t.x&&x<t.Right)return false;return true;}
            foreach(var t in map.terrain??new MapTerrain[0])if(t.kind!=MapTerrain.Pit&&(ledges||t.kind!=MapTerrain.Platform)&&x>=t.x&&x<=t.Right&&Mathf.Abs(t.TopAt(x)-y)<.05f)return true;
            return false;
        }
        void ValidateProps()
        {
            var list=map.props??new MapProp[0];map.props=list;
            if(list.Length>60)throw new ArgumentException("map.props : 60 décors destructibles au plus");
            for(int i=0;i<list.Length;i++)
            {
                var p=list[i];string label="map.props["+i+"]";if(p==null)throw new ArgumentException(label+" : décor vide");
                label+=" "+p.id+" à x "+p.x;p.definition=ContentLibrary.Prop(p.id);var d=p.definition;float half=d.box.width*.5f;
                if(!(p.x-half>=1&&p.x+half<=length-1&&p.y>=0&&p.y<=6))throw new ArgumentException(label+" : dans la mission, y de 0 à 6");
                if(!OnSurface(p.x,p.y,false))throw new ArgumentException(label+" : y à 0 (la rue) ou sur le dessus d'un bloc ou d'une pente à cet endroit");
                if(InArena(p.x-half,p.x+half,out var arena))throw new ArgumentException(label+" : pas de décor destructible dans "+arena);
                if(p.wave<-1||p.wave>=waves.Length)throw new ArgumentException(label+" : wave de -1 au nombre de vagues");
                // An objective stands in the screen its wave holds, and the wave holds it.
                if(p.wave>=0&&(!waves[p.wave].@lock||!(p.x>=waves[p.wave].atX-14&&p.x<=waves[p.wave].atX+2)))throw new ArgumentException(label+" : l'objectif d'une vague bloquée se place entre atX - 14 et atX + 2 ("+waves[p.wave].atX+")");
                if(d.rewards!=null)foreach(var w in d.rewards)if(HostageDefinition.BonusIndex(ruleSet,w.bonus,w.weapon)<0)throw new ArgumentException(label+" : la récompense "+(w.bonus??w.weapon)+" n'est pas un bonus des règles "+ruleSet.id);
                p.bonus=-1;p.gun=null;
                if(!string.IsNullOrEmpty(p.reward)||!string.IsNullOrEmpty(p.weapon))
                {
                    HostageDefinition.CheckReward(p.reward,p.weapon,label);
                    p.bonus=HostageDefinition.BonusIndex(ruleSet,p.reward,p.weapon);if(p.bonus<0)throw new ArgumentException(label+" : récompense inconnue des règles "+ruleSet.id);
                    if(!string.IsNullOrEmpty(p.weapon))p.gun=ContentLibrary.FindWeapon(p.weapon);
                }
            }
        }
        public void Validate()
        {
            if(schemaVersion!=Schema)throw new ArgumentException("schemaVersion doit être "+Schema);
            if(order<0||order>999)throw new ArgumentException("order doit être entre 0 et 999");
            if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(title)||string.IsNullOrWhiteSpace(location))throw new ArgumentException("id, title et location sont requis");
            if(string.IsNullOrWhiteSpace(intro)||string.IsNullOrWhiteSpace(exitLabel)||string.IsNullOrWhiteSpace(winTitle))throw new ArgumentException("intro, exitLabel et winTitle sont requis");
            if(id.Length>60||title.Length>45||location.Length>45||intro.Length>80||exitLabel.Length>40||winTitle.Length>80)throw new ArgumentException("Identifiants ou titres trop longs");
            if(!(length>=30&&length<=500&&exitX>10&&exitX<=length&&cameraMin>=0&&cameraMax>cameraMin&&cameraMax<=length))throw new ArgumentException("Limites de mission invalides");
            if(!string.IsNullOrEmpty(code)&&(code.Length<4||code.Length>10||!System.Text.RegularExpressions.Regex.IsMatch(code,"^[A-Z0-9]+$")))throw new ArgumentException("code : 4 à 10 lettres majuscules ou chiffres");
            Content.Path(music,"Audio/","music");Content.Require<AudioClip>(music,"Musique");
            if(!string.IsNullOrEmpty(logo)){Content.Path(logo,"Art/","logo");Content.Require<Texture2D>(logo,"Logo");}
            map??=new MissionMap();
            if(map.backgrounds==null||map.backgrounds.Length<1||map.backgrounds.Length>8)throw new ArgumentException("map.backgrounds : une à huit images sont requises");
            foreach(var path in map.backgrounds){Content.Path(path,"Art/","map.backgrounds");Content.Require<Texture2D>(path,"Image");}
            Content.Path(map.ground,"Art/","map.ground");Content.Require<Texture2D>(map.ground,"Sol");
            if(!(map.horizon>=0&&map.horizon<=.6f&&map.groundBand>=.02f&&map.groundBand<=1&&map.groundTop>=-6&&map.groundTop<=3&&map.groundFade>=0&&map.groundFade<=1))
                throw new ArgumentException("map : horizon de 0 à 0.6, groundBand de 0.02 à 1, groundTop de -6 à 3, groundFade de 0 à 1");
            Reserved(map.vehicles,"map.vehicles");
            map.decor??=new MapDecor[0];if(map.decor.Length>120)throw new ArgumentException("map.decor : 120 éléments au plus");
            for(int i=0;i<map.decor.Length;i++)
            {
                var d=map.decor[i];string label="map.decor["+i+"]";if(d==null)throw new ArgumentException(label+" : élément vide");
                d.sprite.Validate(label+".sprite");
                if(!(d.x>=-10&&d.x<=length+10&&d.y>=-3&&d.y<=8&&d.height>=.2f&&d.height<=12))throw new ArgumentException(label+" : x dans la mission, y de -3 à 8, height de 0.2 à 12");
            }
            if(enemies==null||enemies.Length<1||enemies.Length>8)throw new ArgumentException("enemies : un à huit identifiants d'ennemis");
            roster=new EnemyDefinition[enemies.Length];for(int i=0;i<enemies.Length;i++)roster[i]=ContentLibrary.Enemy(enemies[i]);
            if(waves==null||waves.Length<1||waves.Length>20)throw new ArgumentException("Une à vingt vagues sont requises");
            float previous=0;
            foreach(var wave in waves)
            {
                if(wave==null||!(wave.atX>previous&&wave.atX<exitX))throw new ArgumentException("Les vagues doivent être ordonnées avant la sortie");previous=wave.atX;
                if(wave.baseEnemies<0||wave.baseEnemies>100||wave.enemiesPerPlayer<0||wave.enemiesPerPlayer>20||wave.maxAlive<1||wave.maxAlive>30||!(wave.spawnInterval>=.1f&&wave.spawnInterval<=10))throw new ArgumentException("Paramètres de vague invalides");
                if(wave.enemyPattern==null||wave.enemyPattern.Length<1||wave.enemyPattern.Length>30)throw new ArgumentException("enemyPattern est requis");
                foreach(int kind in wave.enemyPattern)if(kind<0||kind>=enemies.Length)throw new ArgumentException("Type d'ennemi inconnu");
            }
            ruleSet=ContentLibrary.Rules(string.IsNullOrEmpty(rules)?"default":rules);
            GameRules.Table(weapons,"weapons",false);weaponTable=weapons!=null&&weapons.Length>0?weapons:ruleSet.weapons;
            ValidateEncounters();ValidateTerrain();ValidateHostages();ValidateProps();
            extraEnemy??=new MissionExtraEnemy();
            if(extraEnemy.enemy<-1||extraEnemy.enemy>=enemies.Length||extraEnemy.every<1||extraEnemy.every>30||extraEnemy.fromDifficulty<0||extraEnemy.fromDifficulty>3)throw new ArgumentException("extraEnemy : enemy de -1 à la taille du roster, every de 1 à 30, fromDifficulty de 0 à 3");
            boss??=new MissionBoss();
            if(!boss.enabled){boss.definition=null;return;}
            boss.Resolve("boss");
            Content.Path(bossMusic,"Audio/","bossMusic");Content.Require<AudioClip>(bossMusic,"Musique du boss");
        }
    }
    public static class LevelCatalog
    {
        public static MissionDefinition[] LoadAll()
        {
            var missions=new List<MissionDefinition>();var ids=new HashSet<string>();var codes=new HashSet<string>();
            foreach(var asset in Resources.LoadAll<TextAsset>("Levels"))
            {
                var mission=MissionDefinition.Parse(asset.text);
                if(!ids.Add(mission.id))throw new ArgumentException("Mission dupliquée : "+mission.id);
                if(!string.IsNullOrEmpty(mission.code)&&!codes.Add(mission.code))throw new ArgumentException("Code de mission dupliqué : "+mission.code);
                missions.Add(mission);
            }
            missions.Sort((a,b)=>a.order!=b.order?a.order.CompareTo(b.order):string.CompareOrdinal(a.id,b.id));
            if(missions.Count==0)throw new ArgumentException("Aucune mission dans Resources/Levels");
            return missions.ToArray();
        }
    }
}
