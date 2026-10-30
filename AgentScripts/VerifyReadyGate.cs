using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Paname;
// The lobby only starts a mission once every joined player is ready (alone, one press still starts it), a hero or
// mission change takes the ready back, and the first pad's A goes to J1 while Y adds a new player.
public static class VerifyReadyGate
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags|BindingFlags.Public,null,Array.ConvertAll(args,a=>a.GetType()),null).Invoke(o,args);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    static void Press(Gamepad pad,GamepadButton button)
    {
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));InputSystem.Update();
    }
    static void Release(Gamepad pad){InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();}
    public static object Main(string captures)
    {
        var checks=new List<string>();var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        var pads=new List<Gamepad>();
        // A batch run never has the focus: let the simulated pads through anyway.
        var settings=InputSystem.settings;var background=settings.backgroundBehavior;var editor=settings.editorInputBehaviorInPlayMode;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        try
        {
            g.ReturnToLobby();for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].pad=null;g.slots[i].ready=false;}
            g.SetReady(0,true);
            Check(g.phase==PanameGame.Phase.Playing,"Seul, J1 prêt lance la mission tout de suite",checks);
            g.ReturnToLobby();
            Check(!g.slots[0].ready,"Le retour au menu remet J1 « pas prêt »",checks);
            g.slots[1].joined=true;g.slots[2].joined=true;
            g.SetReady(0,true);g.SetReady(2,true);
            Check(g.phase==PanameGame.Phase.Lobby&&g.ReadyCount==2,"À trois, deux joueurs prêts ne lancent pas la mission",checks);
            g.SelectHero(2,3);
            Check(!g.slots[2].ready&&g.slots[0].ready,"Changer de héros retire le « prêt » de ce joueur seulement",checks);
            g.SetReady(2,true);g.SelectMission(1);
            Check(g.ReadyCount==0||g.MissionCount<2||g.UnlockedCount<2,"Changer de mission retire tous les « prêt »",checks);
            g.SetReady(0,true);g.SetReady(1,true);g.SetReady(2,true);
            Check(g.phase==PanameGame.Phase.Playing,"Le dernier joueur prêt lance la mission",checks);
            Check(!g.slots[0].ready&&!g.slots[1].ready,"La mission lancée, plus personne n'est marqué prêt",checks);
            g.ReturnToLobby();g.SelectMission(-1);
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].pad=null;g.slots[i].ready=false;}
            // Pads: A on the first one takes J1, Y on a second one adds J2; a later A on J1's pad says ready.
            var a=InputSystem.AddDevice<Gamepad>();pads.Add(a);var b=InputSystem.AddDevice<Gamepad>();pads.Add(b);
            Press(a,GamepadButton.South);
            Call(g,"Update");
            Check(g.slots[0].pad==a&&!g.slots[1].joined,"A sur une première manette la donne à J1 (sans ajouter de joueur)",checks);
            Check(!g.slots[0].ready,"Cette même pression ne marque pas J1 prêt",checks);
            Release(a);Call(g,"Update");
            Press(b,GamepadButton.North);Call(g,"Update");Release(b);
            Check(g.slots[1].joined&&g.slots[1].pad==b,"Y sur une autre manette ajoute J2",checks);
            Press(a,GamepadButton.South);Call(g,"Update");Release(a);
            Check(g.slots[0].ready&&g.phase==PanameGame.Phase.Lobby,"A sur la manette de J1 le marque prêt, J2 ne l'est pas encore",checks);
            var cmd=LocalControls.Read(g.slots[0],0);Press(a,GamepadButton.West);cmd=LocalControls.Read(g.slots[0],0);Release(a);
            Check(cmd.fire,"En jeu, la manette de J1 tire",checks);
            InputSystem.RemoveDevice(a);Call(g,"Update");
            Check(g.slots[0].joined&&g.slots[0].pad==null,"J1 qui perd sa manette reste au clavier",checks);
            // Online, the lobby message carries who is ready to the guests.
            g.ReturnToLobby();for(int i=0;i<4;i++){g.slots[i].joined=i<3;g.slots[i].pad=null;g.slots[i].ready=i==1;}
            var missions=(MissionDefinition[])g.GetType().GetField("missions",Flags).GetValue(g);
            var json=(string)Call(g,"LobbyJson");var guest=PanameGame.CreateVerifier(missions);Call(guest,"ApplyLobby",json,false);
            bool carried=guest.slots[1].ready&&!guest.slots[0].ready&&!guest.slots[2].ready&&guest.ReadyCount==1;UnityEngine.Object.DestroyImmediate(guest.gameObject);
            Check(carried,"Le salon en ligne transmet aux invités qui est prêt",checks);
            g.ReturnToLobby();return string.Join("\n",checks);
        }
        finally
        {
            foreach(var p in pads)if(p.added)InputSystem.RemoveDevice(p);
            settings.backgroundBehavior=background;settings.editorInputBehaviorInPlayMode=editor;
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].pad=null;g.slots[i].ready=false;}
        }
    }
}
