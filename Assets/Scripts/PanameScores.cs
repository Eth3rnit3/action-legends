using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    // Global leaderboard on Nostr relays (WebGLTemplates/ActionLegends/scores.js). The page only
    // signs and moves events; this side decides what is shown: a score appears only once its
    // replay, run again here by a hidden PanameGame, ends on the same score and state hash.
    public sealed partial class PanameGame
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int ALScoreAvailable();
        [DllImport("__Internal")] static extern void ALScorePublish(string json);
        [DllImport("__Internal")] static extern void ALScoreFetch(string json);
        [DllImport("__Internal")] static extern void ALScoreVerdict(string id,int ok,string version);
#else
        static int ALScoreAvailable()=>0;
        static void ALScorePublish(string json){}
        static void ALScoreFetch(string json){}
        static void ALScoreVerdict(string id,int ok,string version){}
#endif
        [Serializable] sealed class BoardEntry {public string id,name,verdict;public int players,score;public float time;public bool won,mine;public ReplayRecord replay;}
        [Serializable] sealed class ScoreMessage {public string type,board,message;public int ok;public BoardEntry[] entries;}
        [Serializable] sealed class PublishRequest {public string board;public ReplayRecord record;}
        [Serializable] sealed class FetchRequest {public string board,version;}
        enum Publish { None, Broken, Checking, Ready, Sending, Sent, Better, Failed, Invalid, Unavailable }
        const int BoardSize=10,BoardChecks=25;
        PanameGame verifier;
        BoardEntry verifyingEntry;
        bool verifyingOwn,boardOpen,boardLoading;
        Publish publish;
        string publishNote="",boardBoard="",boardNote="";
        int boardMission,boardDifficulty,boardChecks;
        readonly List<BoardEntry> board=new List<BoardEntry>();
        static bool ScoresAvailable=>ALScoreAvailable()!=0;
        string BoardKey(int mission,int difficulty)=>"action-legends/"+missions[mission].id+"/"+difficulty;

        void OnReplayFinished(ReplayRecord record)
        {
            // Online, the host publishes for the whole team.
            if(Online&&!IsHost){publish=Publish.None;return;}
            if(replayBroken.Length>0){publish=Publish.Broken;publishNote=replayBroken;return;}
            if(record.score<=0){publish=Publish.None;return;}
            if(!ScoresAvailable){publish=Publish.Unavailable;return;}
            // Check our own replay first: what this browser cannot reproduce, nobody else will.
            verifier??=CreateVerifier(missions);verifyingEntry=null;
            verifyingOwn=verifier.BeginVerify(record);publish=verifyingOwn?Publish.Checking:Publish.Invalid;
        }
        void TickVerifier()
        {
            if(verifier==null||(!verifyingOwn&&verifyingEntry==null))return;
            // Never during play: a frame spent replaying someone else's run would stutter.
            if(phase==Phase.Playing)return;
            var result=verifier.StepVerify(4);if(result==null)return;
            if(verifyingOwn){verifyingOwn=false;publish=result.Value?Publish.Ready:Publish.Invalid;}
            else
            {
                verifyingEntry.verdict=result.Value?"ok":"ko";ALScoreVerdict(verifyingEntry.id,result.Value?1:0,Application.version);
                verifyingEntry=null;boardChecks++;
            }
            NextBoardCheck();
        }
        void NextBoardCheck()
        {
            if(verifyingOwn||verifyingEntry!=null||!boardOpen)return;
            int shown=0;
            foreach(var entry in board)
            {
                if(entry.verdict=="ok"&&++shown>=BoardSize)return;
                if(entry.verdict.Length>0)continue;
                if(boardChecks>=BoardChecks)return;
                verifier??=CreateVerifier(missions);
                if(verifier.BeginVerify(entry.replay)){verifyingEntry=entry;return;}
                entry.verdict="ko";boardChecks++;
            }
        }
        void PublishScore()
        {
            if(publish!=Publish.Ready&&publish!=Publish.Failed||LastReplay==null)return;
            publish=Publish.Sending;
            ALScorePublish(JsonUtility.ToJson(new PublishRequest{board=BoardKey(Array.FindIndex(missions,m=>m.id==LastReplay.mission),LastReplay.difficulty),record=LastReplay}));
        }
        void OpenBoard()
        {
            boardOpen=true;boardMission=missionIndex;boardDifficulty=SelectedDifficulty;LoadBoard();
        }
        void LoadBoard()
        {
            board.Clear();boardChecks=0;verifyingEntry=null;boardNote="";boardBoard=BoardKey(boardMission,boardDifficulty);
            if(!ScoresAvailable){boardLoading=false;boardNote="LE CLASSEMENT MONDIAL EST DISPONIBLE DANS LA VERSION NAVIGATEUR.";return;}
            boardLoading=true;ALScoreFetch(JsonUtility.ToJson(new FetchRequest{board=boardBoard,version=Application.version}));
        }
        // Called from the page through SendMessage.
        public void OnScoreEvent(string json)
        {
            ScoreMessage message;try{message=JsonUtility.FromJson<ScoreMessage>(json);}catch(ArgumentException){return;}
            if(message==null)return;
            if(message.type=="published")
            {
                publish=message.ok>0?Publish.Sent:message.message=="better"?Publish.Better:Publish.Failed;
                publishNote=message.ok>0?message.ok+(message.ok>1?" RELAIS":" RELAIS"):message.message;
            }
            else if(message.type=="board"&&message.board==boardBoard)
            {
                boardLoading=false;board.Clear();
                if(message.entries!=null)foreach(var entry in message.entries)if(entry!=null&&entry.replay!=null){entry.verdict??="";board.Add(entry);}
                if(!string.IsNullOrEmpty(message.message))boardNote="RELAIS INJOIGNABLES : RÉESSAIE PLUS TARD.";
                else if(board.Count==0)boardNote="AUCUN SCORE POUR L'INSTANT : À TOI D'OUVRIR LE BAL !";
                NextBoardCheck();
            }
        }

        // ---------- Interface ----------
        void DrawBoardButton()
        {
            if(Button(740,127,153,25,"CLASSEMENT",C("2A414E")))OpenBoard();
        }
        void DrawPublishRow()
        {
            if((phase!=Phase.Won&&phase!=Phase.Lost)||LastReplay==null)return;
            const float x=440,y=530,w=400,h=40;
            string points=LastReplay.score.ToString("N0")+" PTS";
            switch(publish)
            {
                case Publish.Checking:Label(x,y,w,h,"VÉRIFICATION DU REPLAY...  "+Mathf.RoundToInt(verifier.VerifyProgress*100)+" %",14,Gold,true,TextAnchor.MiddleCenter);break;
                case Publish.Ready:if(Button(x,y,w,h,"PUBLIER MON SCORE  ·  "+points,C("34515C")))PublishScore();break;
                case Publish.Sending:Label(x,y,w,h,"PUBLICATION...",14,Gold,true,TextAnchor.MiddleCenter);break;
                case Publish.Sent:Label(x,y,w,h,"SCORE PUBLIÉ SUR "+publishNote+" !",14,Gold,true,TextAnchor.MiddleCenter);break;
                case Publish.Better:Label(x,y,w,h,"TON RECORD SUR CE CLASSEMENT EST DÉJÀ MEILLEUR.",13,Cream,true,TextAnchor.MiddleCenter);break;
                case Publish.Failed:if(Button(x,y,w,h,"ÉCHEC DE LA PUBLICATION : RÉESSAYER",C("6A3A32")))PublishScore();break;
                case Publish.Broken:Label(x-100,y,w+200,h,"SCORE NON PUBLIABLE : "+publishNote+".",13,Cream,true,TextAnchor.MiddleCenter);break;
                case Publish.Invalid:Label(x-100,y,w+200,h,"CE REPLAY NE SE REJOUE PAS À L'IDENTIQUE : SCORE NON PUBLIABLE.",13,Coral,true,TextAnchor.MiddleCenter);break;
                case Publish.Unavailable:Label(x-100,y,w+200,h,"LE CLASSEMENT MONDIAL EST DISPONIBLE DANS LA VERSION NAVIGATEUR.",13,Cream,true,TextAnchor.MiddleCenter);break;
            }
            if(publish==Publish.Ready)Label(x-100,y+42,w+200,18,"Ton pseudo et ta partie seront publiés sur des relais Nostr publics.",11,C("9AAFB2"),false,TextAnchor.MiddleCenter);
        }
        void DrawBoard()
        {
            var e=Event.current;
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){e.Use();CloseBoard();return;}
            Panel(-2000,-2000,5280,4720,new Color(.025f,.055f,.085f,.75f));
            IllustratedPanel(new Rect(250,70,780,580),Color.white);
            Panel(274,94,732,532,C("07192A"));
            Label(274,100,732,30,"CLASSEMENT MONDIAL",22,Gold,true,TextAnchor.MiddleCenter);
            if(Button(300,136,30,26,"<",C("304C56"))){boardMission=Mod(boardMission-1,MissionCount);LoadBoard();}
            Label(334,136,240,26,missions[boardMission].title.ToUpperInvariant(),15,Cream,true,TextAnchor.MiddleCenter);
            if(Button(578,136,30,26,">",C("304C56"))){boardMission=Mod(boardMission+1,MissionCount);LoadBoard();}
            if(Button(672,136,30,26,"<",C("304C56"))){boardDifficulty=Mod(boardDifficulty-1,DifficultyCount);LoadBoard();}
            Label(706,136,240,26,Difficulties[boardDifficulty].name,15,Cream,true,TextAnchor.MiddleCenter);
            if(Button(950,136,30,26,">",C("304C56"))){boardDifficulty=Mod(boardDifficulty+1,DifficultyCount);LoadBoard();}
            float y=180;
            Label(300,y,40,20,"#",11,C("9AAFB2"),true);Label(340,y,330,20,"ÉQUIPE",11,C("9AAFB2"),true);Label(670,y,130,20,"SCORE",11,C("9AAFB2"),true,TextAnchor.MiddleRight);
            Label(810,y,80,20,"TEMPS",11,C("9AAFB2"),true,TextAnchor.MiddleRight);Label(900,y,90,20,"",11,C("9AAFB2"),true,TextAnchor.MiddleRight);
            y+=26;int rank=0;
            foreach(var entry in board)
            {
                if(rank>=BoardSize)break;
                bool ok=entry.verdict=="ok",pending=entry.verdict.Length==0&&entry==verifyingEntry;
                if(!ok&&!pending)continue;
                if(ok)rank++;
                Color tint=entry.mine?Gold:ok?Cream:C("7C929A");
                if(entry.mine)Panel(292,y-2,696,30,new Color(.96f,.78f,.36f,.12f));
                Label(300,y,40,26,ok?rank.ToString():"",15,tint,true);
                Label(340,y,330,26,(entry.name??"?").ToUpperInvariant()+(entry.players>1?"  ("+entry.players+" J)":""),14,tint,true);
                Label(670,y,130,26,entry.score.ToString("N0"),15,tint,true,TextAnchor.MiddleRight);
                Label(810,y,80,26,Clock(entry.time),13,tint,false,TextAnchor.MiddleRight);
                Label(900,y,90,26,ok?(entry.won?"VICTOIRE":"K.O."):Mathf.RoundToInt(verifier.VerifyProgress*100)+" %",11,ok?C("9AAFB2"):Gold,true,TextAnchor.MiddleRight);
                y+=32;
            }
            string note=boardLoading?"CHARGEMENT DES SCORES...":verifyingEntry!=null?"VÉRIFICATION DES REPLAYS EN COURS...":rank==0&&boardNote.Length==0&&board.Count>0?"AUCUN SCORE VÉRIFIÉ POUR CETTE VERSION DU JEU.":boardNote;
            if(note.Length>0)Label(300,rank==0?300:y+6,680,26,note,14,Gold,true,TextAnchor.MiddleCenter);
            Label(300,546,680,34,"Chaque score est rejoué dans ton navigateur avant d'être affiché : seuls les scores reproduits à l'identique apparaissent.",11,C("9AAFB2"),false,TextAnchor.MiddleCenter);
            if(Button(540,584,200,34,"FERMER",Coral,true))CloseBoard();
        }
        void CloseBoard(){boardOpen=false;codeClosedAt=Time.unscaledTime;}
    }
}
