using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    // Lives and the arcade CONTINUE?: a fallen hero comes back from the team's lives or his own (difficulty data), next to
    // the leading hero or where he fell; when nobody has any left, a difficulty with continues counts down. Answering is
    // an input of the simulation (jump, or a fresh press of fire, after a grace time; grenade gives up), so replays and
    // online play go through it like any other tick.
    public sealed partial class PanameGame
    {
        float continueClock;
        readonly bool[] continueFire=new bool[4];
        public int Continues {get;private set;}
        public bool ContinueOffered=>continueClock>0;
        public float ContinueSeconds=>continueClock;
        string ContinueNote=>Continues>0?"  ·  "+Continues+(Continues>1?" CONTINUES":" CONTINUE"):"";
        void ResetContinue(){continueClock=0;Continues=0;System.Array.Clear(continueFire,0,4);}
        bool SpareLives
        {
            get
            {
                if(!Level.livesPerPlayer)return Reserves>0;
                foreach(var p in players)if(p!=null&&p.lives>0)return true;
                return false;
            }
        }
        // Standing again with full health and the hero's own weapon, invulnerable for a while.
        void Revive(Player p,float rear,bool inPlace)
        {
            // Health first: the hero back on his feet counts as the leading hero, as he always did.
            p.hp=MaxPlayerHealth;float x=inPlace?Mathf.Clamp(p.position.x,rear,BossActive?bossArenaX+ArenaHalfWidth-1:screenLocked?lockX+SimHalfWidth-1:CurrentMission.length-1):Mathf.Clamp(LeaderX()-1,1,CurrentMission.length-1);
            // Out of a lethal pit's span, on top of what stands there.
            var pit=PitAt(x);if(pit!=null&&pit.lethal)x=pit.x-.7f;
            p.position=new Vector2(x,SolidAt(x,ForHeroes));p.vertical=0;p.dropFrom=float.MaxValue;p.invulnerable=Level.respawn.invulnerable;p.root.gameObject.SetActive(true);Disarm(p);
            if(Rules.grenades.refillOnRespawn)p.grenades=Mathf.Max(p.grenades,Rules.grenades.start[DifficultyIndex]);
        }
        void OutOfLives()
        {
            if(!Level.continues.enabled){phase=Phase.Lost;Announce("ON REMET ÇA ?");return;}
            // The last hero to fall finishes falling before the countdown starts.
            foreach(var p in players)if(p!=null&&p.downTimer>0)return;
            continueClock=Level.continues.seconds;for(int i=0;i<4;i++)continueFire[i]=true;Announce("CONTINUE ?");
        }
        void TickContinue(float dt,PlayerCommand[] commands)
        {
            var c=Level.continues;bool listening=c.seconds-continueClock>=c.grace;continueClock-=dt;
            for(int i=0;i<4;i++)
            {
                if(players[i]==null)continue;var input=i<commands.Length?commands[i]:default;
                bool fresh=input.fire&&!continueFire[i];continueFire[i]=input.fire;
                if(!listening)continue;
                if(input.jump||fresh){Continue();return;}
                if(input.grenade){continueClock=0;phase=Phase.Lost;Announce("ON REMET ÇA ?");return;}
            }
            if(continueClock<=0){continueClock=0;phase=Phase.Lost;Announce("ON REMET ÇA ?");}
        }
        // The mission goes on where it stopped: every hero stands up where he fell, with the lives of a new credit.
        void Continue()
        {
            continueClock=0;Continues++;float rear=Mathf.Max(.5f,cameraX-SimHalfWidth+.7f);
            if(!Level.livesPerPlayer)Reserves=JoinedCount*Level.ContinueLives;
            foreach(var p in players)if(p!=null){if(Level.livesPerPlayer)p.lives=Level.ContinueLives;Revive(p,rear,true);}
            Announce("C'EST REPARTI !");Sound(coinClip);
        }

        // ---------- Interface ----------
        // Buttons answer through the input of this browser's player, exactly like pressing jump or grenade.
        void DrawContinue()
        {
            if(phase!=Phase.Playing||!ContinueOffered)return;
            Panel(-2000,-2000,5280,4720,new Color(.06f,.12f,.16f,.72f));
            Label(90,190,1100,80,"CONTINUE ?",64,Gold,true,TextAnchor.MiddleCenter);
            Label(90,272,1100,110,Mathf.CeilToInt(continueClock).ToString(),96,Cream,true,TextAnchor.MiddleCenter);
            Label(90,384,1100,30,"SAUT OU TIR : CONTINUER   ·   GRENADE : ABANDONNER",16,Cream,true,TextAnchor.MiddleCenter);
            if(Continues>0)Label(90,414,1100,24,"DÉJÀ "+Continues+(Continues>1?" CONTINUES":" CONTINUE")+" : LE SCORE NE SERA PAS PUBLIABLE",12,C("9AAFB2"),true,TextAnchor.MiddleCenter);
            else Label(90,414,1100,24,"UNE PARTIE CONTINUÉE NE SE PUBLIE PAS AU CLASSEMENT",12,C("9AAFB2"),true,TextAnchor.MiddleCenter);
            if(Level.continues.seconds-continueClock<Level.continues.grace)return;
            if(Button(330,452,300,52,"CONTINUER  »",Coral,true))AnswerContinue(true);
            if(Button(650,452,300,52,"ABANDONNER",C("34515C")))AnswerContinue(false);
        }
        void AnswerContinue(bool yes)
        {
            if(Online){if(yes)localJumps++;else localGrenades++;return;}
            int slot=Mathf.Clamp(menuPlayer,0,3);if(!slots[slot].joined)slot=0;
            if(yes)localJump[slot]=true;else localGrenade[slot]=true;
        }
    }
}
