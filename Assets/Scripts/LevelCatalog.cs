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
        public int[] enemyPattern={0,1,2};
        // A locked wave holds the screen until its last enemy falls; "lock": false lets the team run on.
        public bool @lock=true;
    }
    // Engine behaviours an enemy row can use; the first three are the looters of Panique à Paname.
    public enum EnemyRole { Rusher, Thrower, Shield, Pistol, Smg, Shotgun }
    // One enemy type of a mission: its behaviour, its row in the mission's enemy sheet and, for
    // gunmen, the barrel tip of its shooting frame, from the feet and facing right.
    [Serializable] public sealed class MissionEnemy
    {
        public string role;
        public int row;
        public Vector2 muzzle;
        [NonSerialized] public EnemyRole kind;
        public static readonly string[] RoleIds={"rusher","thrower","shield","pistol","smg","shotgun"};
        public bool Armed=>kind>=EnemyRole.Pistol;
        public bool Heavy=>kind==EnemyRole.Shield||kind==EnemyRole.Shotgun;
    }
    // Every field is optional: a mission that omits the boss gets the helicopter of Panique à Paname.
    [Serializable] public sealed class MissionBoss
    {
        public bool enabled=true;
        public string kind="helicopter",name="MACRON",sprite="Art/macron-helicopter",intro="BOSS : MACRON EN HÉLICO !",rage="MACRON S'EMBALLE !",defeat="HÉLICO HORS SERVICE !";
        public float health=1;
        public static readonly string[] Kinds={"helicopter","mecha"};
        public bool Mecha=>kind=="mecha";
    }
    [Serializable] public sealed class MissionDefinition
    {
        public int schemaVersion=1,order=1;
        public string id,title,location,intro,exitLabel,winTitle,code;
        public string[] backgrounds;
        public string ground="Art/paris",music="Audio/Music/marseillaise-arcade",bossMusic="Audio/Music/marseillaise-boss",enemySheet="Art/enemies";
        public MissionEnemy[] enemies;
        public float length=80,exitX=77,cameraMin=7,cameraMax=74;
        public MissionWave[] waves;
        public MissionBoss boss=new MissionBoss();
        static MissionEnemy[] DefaultEnemies()=>new[]{new MissionEnemy{role="rusher",row=0},new MissionEnemy{role="thrower",row=1},new MissionEnemy{role="shield",row=2}};
        // Square frames, four per row: the row count follows from the sheet's proportions.
        public static int SheetRows(Texture2D sheet)=>Mathf.Max(1,Mathf.RoundToInt(sheet.height/(sheet.width/4f)));
        static string Or(string value,string fallback)=>string.IsNullOrWhiteSpace(value)?fallback:value;
        static void Resource<T>(string path,string prefix,string label) where T:UnityEngine.Object
        {
            if(!path.StartsWith(prefix,StringComparison.Ordinal)||path.Contains("..")||path.Contains(":")||path.Contains("\\"))throw new ArgumentException("Chemin invalide pour "+label);
            if(!Resources.Load<T>(path))throw new ArgumentException(label+" manquant : "+path);
        }
        public static MissionDefinition Parse(string text)
        {
            var mission=JsonUtility.FromJson<MissionDefinition>(text);
            if(mission==null)throw new ArgumentException("Mission JSON vide");mission.Validate();return mission;
        }
        public void Validate()
        {
            if(schemaVersion!=1)throw new ArgumentException("schemaVersion doit être 1");
            if(order<0||order>999)throw new ArgumentException("order doit être entre 0 et 999");
            if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(title)||string.IsNullOrWhiteSpace(location))throw new ArgumentException("id, title et location sont requis");
            if(string.IsNullOrWhiteSpace(intro)||string.IsNullOrWhiteSpace(exitLabel)||string.IsNullOrWhiteSpace(winTitle))throw new ArgumentException("intro, exitLabel et winTitle sont requis");
            if(id.Length>60||title.Length>45||location.Length>45||intro.Length>80||exitLabel.Length>40||winTitle.Length>80)throw new ArgumentException("Identifiants ou titres trop longs");
            if(!(length>=30&&length<=500&&exitX>10&&exitX<=length&&cameraMin>=0&&cameraMax>cameraMin&&cameraMax<=length))throw new ArgumentException("Limites de mission invalides");
            if(backgrounds==null||backgrounds.Length<1||backgrounds.Length>8)throw new ArgumentException("Une à huit images sont requises");
            foreach(var path in backgrounds)
            {
                if(string.IsNullOrEmpty(path)||!path.StartsWith("Art/",StringComparison.Ordinal)||path.Contains("..")||path.Contains(":")||path.Contains("\\"))throw new ArgumentException("Chemin d'image invalide");
                if(!Resources.Load<Texture2D>(path))throw new ArgumentException("Image manquante : "+path);
            }
            if(waves==null||waves.Length<1||waves.Length>20)throw new ArgumentException("Une à vingt vagues sont requises");
            float previous=0;
            foreach(var wave in waves)
            {
                if(wave==null||!(wave.atX>previous&&wave.atX<exitX))throw new ArgumentException("Les vagues doivent être ordonnées avant la sortie");previous=wave.atX;
                if(wave.baseEnemies<1||wave.baseEnemies>100||wave.enemiesPerPlayer<0||wave.enemiesPerPlayer>20||wave.maxAlive<1||wave.maxAlive>30||!(wave.spawnInterval>=.1f&&wave.spawnInterval<=10))throw new ArgumentException("Paramètres de vague invalides");
                if(wave.enemyPattern==null||wave.enemyPattern.Length<1||wave.enemyPattern.Length>30)throw new ArgumentException("enemyPattern est requis");
            }
            var defaults=new MissionDefinition();boss??=new MissionBoss();var defaultBoss=defaults.boss;
            if(!string.IsNullOrEmpty(code)&&(code.Length<4||code.Length>10||!System.Text.RegularExpressions.Regex.IsMatch(code,"^[A-Z0-9]+$")))throw new ArgumentException("code : 4 à 10 lettres majuscules ou chiffres");
            enemySheet=Or(enemySheet,defaults.enemySheet);Resource<Texture2D>(enemySheet,"Art/","Planche d'ennemis");
            if(enemies==null||enemies.Length==0)enemies=DefaultEnemies();
            if(enemies.Length>6)throw new ArgumentException("Six types d'ennemis au plus");
            int rows=SheetRows(Resources.Load<Texture2D>(enemySheet));
            foreach(var enemy in enemies)
            {
                int role=enemy==null?-1:Array.IndexOf(MissionEnemy.RoleIds,enemy.role);
                if(role<0)throw new ArgumentException("Rôle d'ennemi inconnu");
                if(enemy.row<0||enemy.row>=rows)throw new ArgumentException("Ligne absente de la planche d'ennemis");
                enemy.kind=(EnemyRole)role;
                if(enemy.Armed&&!(enemy.muzzle.x>0&&enemy.muzzle.x<3&&enemy.muzzle.y>0&&enemy.muzzle.y<3))throw new ArgumentException("muzzle requis pour un ennemi armé");
            }
            foreach(var wave in waves)foreach(int kind in wave.enemyPattern)if(kind<0||kind>=enemies.Length)throw new ArgumentException("Type d'ennemi inconnu");
            ground=Or(ground,defaults.ground);music=Or(music,defaults.music);bossMusic=Or(bossMusic,defaults.bossMusic);
            boss.kind=Or(boss.kind,defaultBoss.kind);if(Array.IndexOf(MissionBoss.Kinds,boss.kind)<0)throw new ArgumentException("boss.kind inconnu");
            boss.name=Or(boss.name,defaultBoss.name);boss.sprite=Or(boss.sprite,defaultBoss.sprite);boss.intro=Or(boss.intro,defaultBoss.intro);boss.rage=Or(boss.rage,defaultBoss.rage);boss.defeat=Or(boss.defeat,defaultBoss.defeat);
            Resource<Texture2D>(ground,"Art/","Sol");Resource<AudioClip>(music,"Audio/","Musique");Resource<AudioClip>(bossMusic,"Audio/","Musique du boss");
            if(boss.name.Length>30||boss.intro.Length>80||boss.rage.Length>80||boss.defeat.Length>80)throw new ArgumentException("Textes du boss trop longs");
            if(!(boss.health>=.25f&&boss.health<=4))throw new ArgumentException("boss.health doit être entre 0.25 et 4");
            Resource<Texture2D>(boss.sprite,"Art/","Image du boss");
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
