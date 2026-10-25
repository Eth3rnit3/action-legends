using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Paname;
public static class VerifyPrototype
{
    static void Check(bool ok,string label,List<string> passed){if(!ok)throw new Exception("ÉCHEC : "+label);passed.Add(label);}
    public static object Main()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        if(g==null)throw new Exception("Jeu absent");
        var results=new List<string>();var pad=InputSystem.AddDevice<Gamepad>();
        float previousScale=Time.timeScale;Time.timeScale=0;
        try
        {
            var state=new GamepadState{leftStick=Vector2.right}.WithButton(GamepadButton.West);
            InputState.Change(pad,state,InputState.currentUpdateType);
            var command=LocalControls.Read(new LocalSlot{joined=true,pad=pad},3);
            Check(command.move>.9f&&command.fire,"Adaptateur manette : déplacement et tir",results);
            for(int i=0;i<4;i++){g.slots[i].joined=true;g.slots[i].hero=i;}
            g.BeginRound();Check(g.JoinedCount==4,"Quatre joueurs créés",results);
            var commands=new PlayerCommand[4];var start=new Vector2[4];
            for(int i=0;i<4;i++){start[i]=g.GetPlayerPosition(i);commands[i]=new PlayerCommand{move=1,jump=true};}
            g.TickSimulation(1f/60,commands);
            for(int i=0;i<4;i++)Check(g.GetPlayerPosition(i).y>0,"Saut du joueur "+(i+1),results);
            for(int n=0;n<90;n++){for(int i=0;i<4;i++)commands[i]=new PlayerCommand{move=1,fire=true};g.TickSimulation(1f/60,commands);}
            for(int i=0;i<4;i++)Check(g.GetPlayerPosition(i).x>start[i].x+3&&g.GetPlayerPosition(i).y<.01f,"Déplacement et atterrissage J"+(i+1),results);
            Check(g.EnemyCount>0||g.Score>0,"Déclenchement d'une vague",results);
            // Run the complete level with scripted commands; invulnerability isolates progression from balancing.
            var enemiesField=typeof(PanameGame).GetField("enemies",BindingFlags.Instance|BindingFlags.NonPublic);
            var playersField=typeof(PanameGame).GetField("players",BindingFlags.Instance|BindingFlags.NonPublic);
            for(int n=0;n<7200 && g.phase==PanameGame.Phase.Playing;n++)
            {
                var foes=(IList)enemiesField.GetValue(g);var players=(Array)playersField.GetValue(g);
                for(int i=0;i<4;i++)
                {
                    var p=(PanameGame.Player)players.GetValue(i);p.invulnerable=10;
                    float move=1;
                    if(foes.Count>0)
                    {
                        float nearest=999,dx=0;
                        foreach(var e in foes){var pos=(Vector2)e.GetType().GetField("position").GetValue(e);float d=pos.x-p.position.x;if(Mathf.Abs(d)<nearest){nearest=Mathf.Abs(d);dx=d;}}
                        move=Mathf.Sign(dx)*(nearest>4?1:.02f);
                    }
                    commands[i]=new PlayerCommand{move=move,fire=true,special=true};
                }
                g.TickSimulation(1f/60,commands);
            }
            Check(g.Score>0,"Projectiles : dégâts et score",results);
            Check(g.Wave==3&&g.phase==PanameGame.Phase.Won,"Trois vagues puis arrivée à Bastille",results);
            g.ReturnToLobby();Check(g.phase==PanameGame.Phase.Lobby,"Retour à la sélection",results);
            // Restore a clean single-player lobby after the checks.
            for(int i=1;i<4;i++)g.slots[i].joined=false;g.ReturnToLobby();
            return new{passed=results,count=results.Count,hardware="Manette virtuelle Input System ; pas de test de manettes physiques",progression="Parcours automatisé avec invulnérabilité pour isoler la progression"};
        }
        finally {InputSystem.RemoveDevice(pad);Time.timeScale=previousScale;}
    }
}
