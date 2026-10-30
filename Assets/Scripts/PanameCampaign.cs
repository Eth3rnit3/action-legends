using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static Paname.PanameArt;
namespace Paname
{
    // Missions follow each other in catalog order. Winning one unlocks the next in this browser and
    // reveals its code; typing that code in the lobby unlocks it, and every mission before it, anywhere else.
    // A secret mission (hidden) is left out of that chain: only its code opens it, and it opens nothing else.
    public sealed partial class PanameGame
    {
        const string UnlockKey="action-legends.unlocked.";
        int carriedScore;
        bool codeEntry;
        string codeText="",codeMessage="";
        float codeMessageUntil,codeClosedAt=-1;
        // The browser can deliver the Enter that validated a code to the Input System a frame
        // later: the lobby ignores its keys briefly so that Enter does not also deploy the team.
        bool LobbyKeysBlocked=>codeEntry||boardOpen||Time.unscaledTime<codeClosedAt+.35f;
        void CloseCodeEntry(){codeEntry=false;codeClosedAt=Time.unscaledTime;}
        // A mission is open once it or any mission after it in the catalog is unlocked: a mission
        // inserted earlier in the campaign opens for players who already reached a later one.
        public bool IsUnlocked(int index)
        {
            if(index==0)return true;
            if(missions[index].hidden)return Unlocked(index);
            for(int i=index;i<MissionCount;i++)if(!missions[i].hidden&&Unlocked(i))return true;
            return false;
        }
        bool Unlocked(int index)=>PlayerPrefs.GetInt(UnlockKey+missions[index].id,0)==1;
        public int UnlockedCount{get{int n=0;for(int i=0;i<MissionCount;i++)if(IsUnlocked(i))n++;return n;}}
        // The missions the lobby counts: the campaign, and the secret ones already found.
        public int ShownCount{get{int n=0;for(int i=0;i<MissionCount;i++)if(!missions[i].hidden||IsUnlocked(i))n++;return n;}}
        // Its place in the campaign (1 for the first), 0 for a secret mission.
        public int CampaignNumber(int index){if(missions[index].hidden)return 0;int n=0;for(int i=0;i<=index;i++)if(!missions[i].hidden)n++;return n;}
        // The campaign mission after the current one, skipping the secret ones; none after a secret mission.
        int NextIndex()
        {
            if(missions==null||CurrentMission.hidden)return -1;
            for(int i=missionIndex+1;i<missions.Length;i++)if(!missions[i].hidden)return i;
            return -1;
        }
        public MissionDefinition NextMissionDefinition{get{int next=NextIndex();return next<0?null:missions[next];}}
        // The secret mission the end of the campaign hints at: the first one not found yet.
        public MissionDefinition SecretHint{get{if(missions!=null)for(int i=0;i<missions.Length;i++)if(missions[i].hidden&&!IsUnlocked(i))return missions[i];return null;}}
        void Unlock(int index)
        {
            if(index<=0||index>=MissionCount||IsUnlocked(index))return;
            // WebGL keeps PlayerPrefs in IndexedDB and only writes them on Save.
            PlayerPrefs.SetInt(UnlockKey+missions[index].id,1);PlayerPrefs.Save();
        }
        void WinMission()
        {
            // A replay checked for the leaderboard must not unlock missions for the viewer.
            TallyMission();phase=Phase.Won;Announce("MISSION ACCOMPLIE !");Speak(winVoice);if(!headless)Unlock(NextIndex());
        }
        // The team keeps its heroes and score; online, the host's restart message carries both to the guests.
        public void NextMission()
        {
            if(phase!=Phase.Won||NextMissionDefinition==null||(Online&&!IsHost))return;
            carriedScore=Score;LoadMission(NextIndex());BeginRound();
        }
        bool ContinuePressed()
        {
            if(Keyboard.current!=null&&(Keyboard.current.enterKey.wasPressedThisFrame||Keyboard.current.numpadEnterKey.wasPressedThisFrame))return true;
            if(Online)return Gamepad.current!=null&&Gamepad.current.startButton.wasPressedThisFrame;
            for(int i=0;i<4;i++)if(slots[i].joined&&slots[i].Connected&&slots[i].pad.startButton.wasPressedThisFrame)return true;
            return false;
        }
        public bool EnterMissionCode(string code)
        {
            code=(code??"").Trim().ToUpperInvariant();
            int index=Array.FindIndex(missions,m=>!string.IsNullOrEmpty(m.code)&&m.code==code);
            if(index<0||phase!=Phase.Lobby||(Online&&!IsHost)){codeMessage="CODE INCONNU";codeMessageUntil=Time.unscaledTime+2.5f;return false;}
            Unlock(index);LoadMission(index);if(Online)HostSendLobby();
            codeMessage="MISSION DÉBLOQUÉE !";codeMessageUntil=Time.unscaledTime+2.5f;return true;
        }
        void DrawCodeButton()
        {
            if(Online&&!IsHost)return;
            if(Button(740,98,153,25,"CODE MISSION",C("2A414E"))){codeEntry=true;codeText="";}
            if(Time.unscaledTime<codeMessageUntil&&!codeEntry)Label(913,184,315,20,codeMessage,12,codeMessage=="CODE INCONNU"?Coral:Gold,true,TextAnchor.MiddleCenter);
        }
        void DrawCodeEntry()
        {
            var e=Event.current;
            if(e.type==EventType.KeyDown&&(e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter)){e.Use();if(EnterMissionCode(codeText))CloseCodeEntry();return;}
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){e.Use();CloseCodeEntry();return;}
            Panel(-2000,-2000,5280,4720,new Color(.025f,.055f,.085f,.7f));
            IllustratedPanel(new Rect(400,215,480,270),Color.white);
            Panel(424,239,432,222,C("07192A"));
            Label(424,250,432,30,"CODE MISSION",22,Gold,true,TextAnchor.MiddleCenter);
            Label(424,282,432,22,"Chaque mission terminée révèle le code de la suivante.",12,C("B2C7BD"),false,TextAnchor.MiddleCenter);
            Panel(470,316,340,48,C("18303B"));
            GUI.SetNextControlName("CodeMission");
            var field=new GUIStyle(Style(26,Cream,true,TextAnchor.MiddleCenter)){normal={background=null,textColor=Cream},focused={background=null,textColor=Cream},hover={background=null,textColor=Cream}};
            string typed=GUI.TextField(new Rect(470,316,340,48),codeText,10,field);
            var clean=new System.Text.StringBuilder();foreach(char c in typed.ToUpperInvariant())if((c>='A'&&c<='Z')||(c>='0'&&c<='9'))clean.Append(c);
            codeText=clean.ToString();CheckGlyphs(codeText);
            GUI.FocusControl("CodeMission");
            if(Time.unscaledTime<codeMessageUntil)Label(424,368,432,22,codeMessage,13,Coral,true,TextAnchor.MiddleCenter);
            if(Button(470,398,166,42,"ANNULER",C("34515C")))CloseCodeEntry();
            if(Button(644,398,166,42,"VALIDER",Coral,true)&&EnterMissionCode(codeText))CloseCodeEntry();
        }
    }
}
