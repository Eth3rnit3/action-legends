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
    }
    [Serializable] public sealed class MissionDefinition
    {
        public int schemaVersion=1;
        public string id,title,location,intro,exitLabel,winTitle;
        public string[] backgrounds;
        public float length=80,exitX=77,cameraMin=7,cameraMax=74;
        public MissionWave[] waves;
        public static MissionDefinition Parse(string text)
        {
            var mission=JsonUtility.FromJson<MissionDefinition>(text);
            if(mission==null)throw new ArgumentException("Mission JSON vide");mission.Validate();return mission;
        }
        public void Validate()
        {
            if(schemaVersion!=1)throw new ArgumentException("schemaVersion doit être 1");
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
                foreach(int kind in wave.enemyPattern)if(kind<0||kind>2)throw new ArgumentException("Type d'ennemi inconnu");
            }
        }
    }
    public static class LevelCatalog
    {
        public static MissionDefinition[] LoadAll()
        {
            var missions=new List<MissionDefinition>();var ids=new HashSet<string>();
            foreach(var asset in Resources.LoadAll<TextAsset>("Levels"))
            {
                var mission=MissionDefinition.Parse(asset.text);
                if(!ids.Add(mission.id))throw new ArgumentException("Mission dupliquée : "+mission.id);
                missions.Add(mission);
            }
            missions.Sort((a,b)=>string.CompareOrdinal(a.id,b.id));
            if(missions.Count==0)throw new ArgumentException("Aucune mission dans Resources/Levels");
            return missions.ToArray();
        }
    }
}
