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
        [NonSerialized] public BossDefinition definition;
    }
    // Reserved for later work: terrain, props and vehicles must stay empty until the engine reads them (see docs/ADDING_A_LEVEL.md).
    [Serializable] public sealed class MapTerrain {public string type;public float x,width,height,toHeight;}
    [Serializable] public sealed class MapProp {public string id;public float x,y;public int wave=-1;}
    // A hostage of the library tied at x (y above the street); reward / weapon replace his own table of rewards; with a
    // wave, he only appears when that wave starts, inside its locked screen.
    [Serializable] public sealed class MapHostage
    {
        public string id,reward,weapon;public float x,y;public int wave=-1;
        [NonSerialized] public HostageDefinition definition;[NonSerialized] public int bonus=-1;[NonSerialized] public WeaponDefinition gun;
    }
    [Serializable] public sealed class MapVehicle {public string id;public float x;}
    [Serializable] public sealed class EncounterSpawn {public string enemy,entry;public float x,delay;}
    [Serializable] public sealed class MissionEncounter {public float atX;public int wave=-1;public EncounterSpawn[] spawns;}
    // Everything drawn along the course: panorama pieces in order, the image whose bottom is the street, and the reserved object lists.
    [Serializable] public sealed class MissionMap
    {
        public string[] backgrounds;
        public string ground;
        public MapTerrain[] terrain;
        public MapProp[] props;
        public MapHostage[] hostages;
        public MapVehicle[] vehicles;
    }
    [Serializable] public sealed class MissionDefinition
    {
        public const int Schema=2;
        public int schemaVersion,order=1;
        public string id,title,location,intro,exitLabel,winTitle,code,music,bossMusic;
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
            map??=new MissionMap();
            if(map.backgrounds==null||map.backgrounds.Length<1||map.backgrounds.Length>8)throw new ArgumentException("map.backgrounds : une à huit images sont requises");
            foreach(var path in map.backgrounds){Content.Path(path,"Art/","map.backgrounds");Content.Require<Texture2D>(path,"Image");}
            Content.Path(map.ground,"Art/","map.ground");Content.Require<Texture2D>(map.ground,"Sol");
            Reserved(map.terrain,"map.terrain");Reserved(map.props,"map.props");Reserved(map.vehicles,"map.vehicles");Reserved(encounters,"encounters");
            if(enemies==null||enemies.Length<1||enemies.Length>8)throw new ArgumentException("enemies : un à huit identifiants d'ennemis");
            roster=new EnemyDefinition[enemies.Length];for(int i=0;i<enemies.Length;i++)roster[i]=ContentLibrary.Enemy(enemies[i]);
            if(waves==null||waves.Length<1||waves.Length>20)throw new ArgumentException("Une à vingt vagues sont requises");
            float previous=0;
            foreach(var wave in waves)
            {
                if(wave==null||!(wave.atX>previous&&wave.atX<exitX))throw new ArgumentException("Les vagues doivent être ordonnées avant la sortie");previous=wave.atX;
                if(wave.baseEnemies<1||wave.baseEnemies>100||wave.enemiesPerPlayer<0||wave.enemiesPerPlayer>20||wave.maxAlive<1||wave.maxAlive>30||!(wave.spawnInterval>=.1f&&wave.spawnInterval<=10))throw new ArgumentException("Paramètres de vague invalides");
                if(wave.enemyPattern==null||wave.enemyPattern.Length<1||wave.enemyPattern.Length>30)throw new ArgumentException("enemyPattern est requis");
                foreach(int kind in wave.enemyPattern)if(kind<0||kind>=enemies.Length)throw new ArgumentException("Type d'ennemi inconnu");
            }
            ruleSet=ContentLibrary.Rules(string.IsNullOrEmpty(rules)?"default":rules);
            GameRules.Table(weapons,"weapons",false);weaponTable=weapons!=null&&weapons.Length>0?weapons:ruleSet.weapons;
            ValidateHostages();
            extraEnemy??=new MissionExtraEnemy();
            if(extraEnemy.enemy<-1||extraEnemy.enemy>=enemies.Length||extraEnemy.every<1||extraEnemy.every>30||extraEnemy.fromDifficulty<0||extraEnemy.fromDifficulty>3)throw new ArgumentException("extraEnemy : enemy de -1 à la taille du roster, every de 1 à 30, fromDifficulty de 0 à 3");
            boss??=new MissionBoss();
            if(!boss.enabled){boss.definition=null;return;}
            boss.definition=ContentLibrary.Boss(boss.id);var b=boss.definition;
            if(string.IsNullOrWhiteSpace(boss.name))boss.name=b.name;if(string.IsNullOrWhiteSpace(boss.intro))boss.intro=b.intro;
            if(string.IsNullOrWhiteSpace(boss.rage))boss.rage=b.rage;if(string.IsNullOrWhiteSpace(boss.defeat))boss.defeat=b.defeat;
            if(boss.name.Length>30||boss.intro.Length>80||boss.rage.Length>80||boss.defeat.Length>80)throw new ArgumentException("Textes du boss trop longs");
            if(!(boss.health>=.25f&&boss.health<=4))throw new ArgumentException("boss.health doit être entre 0.25 et 4");
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
