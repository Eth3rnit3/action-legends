using System;
using UnityEngine;
namespace Paname
{
    // Every few waves cleared, the team gets lives back and the heroes standing get health back (every 0: never).
    [Serializable] public sealed class WaveReward {public int every,lives,health;}
    // A hero who falls comes back after delay seconds, invulnerable for a while, next to the leading hero or where he fell.
    [Serializable] public sealed class RespawnRules {public float delay=2.5f,invulnerable=3;public bool inPlace;}
    // When the whole team is out of lives: a CONTINUE? countdown of seconds; inputs are ignored for grace seconds so a
    // held button does not continue by accident.
    [Serializable] public sealed class ContinueRules {public bool enabled;public float seconds=10,grace=1;public int lives;}
    // A difficulty of the lobby, Resources/Content/difficulties/<id>.json, listed by order then id: replays, online
    // play and the leaderboard refer to it by that position. column: which of the four values of every per-difficulty
    // list of the content (enemies, bosses, weapons, rules) it plays with.
    [Serializable] public sealed class DifficultyDefinition
    {
        public string id,name,tagline;
        public int order,column,health=5,lives=2,damageTaken=1;
        // livesPerPlayer: each hero has his own lives instead of the team sharing lives × players.
        public bool livesPerPlayer;
        // Enemies' health, numbers, speed and time between attacks, the gunmen's aim time, all as factors.
        public float enemyHealth=1,enemyCount=1,enemySpeed=1,enemyCadence=1,gunmanAim=1,hurtInvulnerable=.75f;
        public WaveReward waveReward=new WaveReward();
        public RespawnRules respawn=new RespawnRules();
        public ContinueRules continues=new ContinueRules();
        public void Validate()
        {
            string label="Difficulté "+id;
            Content.Text(name,14,label+".name");if(!string.IsNullOrEmpty(tagline))Content.Text(tagline,70,label+".tagline");
            if(order<0||order>99||column<0||column>3)throw new ArgumentException(label+" : order entre 0 et 99, column entre 0 et 3");
            if(health<1||health>20||lives<0||lives>20||damageTaken<1||damageTaken>10)throw new ArgumentException(label+" : health 1 à 20, lives 0 à 20, damageTaken 1 à 10");
            Content.Range(enemyHealth,.1f,10,label+".enemyHealth");Content.Range(enemyCount,.1f,10,label+".enemyCount");Content.Range(enemySpeed,.1f,10,label+".enemySpeed");
            Content.Range(enemyCadence,.1f,10,label+".enemyCadence");Content.Range(gunmanAim,.1f,10,label+".gunmanAim");Content.Range(hurtInvulnerable,0,10,label+".hurtInvulnerable");
            var w=waveReward;if(w.every<0||w.every>20||w.lives<0||w.lives>10||w.health<0||w.health>20)throw new ArgumentException(label+" : waveReward.every 0 à 20, lives 0 à 10, health 0 à 20");
            Content.Range(respawn.delay,.1f,10,label+".respawn.delay");Content.Range(respawn.invulnerable,0,10,label+".respawn.invulnerable");
            var c=continues;if(c.enabled){Content.Range(c.seconds,1,60,label+".continues.seconds");Content.Range(c.grace,0,c.seconds,label+".continues.grace");if(c.lives<0||c.lives>20)throw new ArgumentException(label+" : continues.lives 0 à 20");}
        }
        // Lives a continue gives each hero: its own number, else the difficulty's.
        public int ContinueLives=>continues.lives>0?continues.lives:Mathf.Max(1,lives);
    }
}
