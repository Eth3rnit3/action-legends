using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    // A mission is replayed from its inputs alone: local play now advances in the same fixed
    // 60 Hz steps as online play, so every tick can be recorded and simulated again elsewhere.
    [Serializable] public sealed class ReplayRecord
    {
        public int v=1;
        public string version,mission;
        public int difficulty,score,ticks,hash;
        // CONTINUE? answered yes during the run (difficulties with continues); 0 in older records.
        public int continues;
        public float time;
        public bool won;
        public int[] heroes=new int[4];
        public string[] names=new string[4];
        // Runs of identical ticks: "count*move.flags,move.flags,..;" per joined slot, with "Lmask:" when players leave.
        public string runs;
    }
    public sealed partial class PanameGame
    {
        readonly StringBuilder replayRuns=new StringBuilder();
        readonly bool[] localJump=new bool[4],localSpecial=new bool[4],localGrenade=new bool[4];
        readonly PlayerCommand[] localHeld=new PlayerCommand[4];
        string replayToken="",replayBroken="";
        int replayCount,replayTicks,replayStartScore;
        bool recording,headless;
        float localClock;
        public ReplayRecord LastReplay {get;private set;}
        public string ReplayProblem=>replayBroken;

        void LocalStep()
        {
            // Edge inputs are latched so a frame that runs no tick does not lose a jump.
            for(int i=0;i<4;i++)
            {
                var read=slots[i].joined?LocalControls.Read(slots[i],i):default;localHeld[i]=read;
                if(read.jump)localJump[i]=true;if(read.special)localSpecial[i]=true;if(read.grenade)localGrenade[i]=true;
            }
            localClock=Mathf.Min(localClock+Time.deltaTime,.25f);
            var moves=new int[4];var flags=new int[4];
            while(localClock>=NetStep&&phase==Phase.Playing)
            {
                localClock-=NetStep;
                for(int i=0;i<4;i++)
                {
                    var held=localHeld[i];
                    // Tenths of a stick: what is simulated is exactly what is recorded.
                    moves[i]=Mathf.RoundToInt(Mathf.Clamp(held.move,-1,1)*10)*10;
                    flags[i]=HeldFlags(held)|(localJump[i]?4:0)|(localSpecial[i]?8:0)|(localGrenade[i]?32:0);
                    frameInputs[i]=Command(moves[i],flags[i]);localJump[i]=false;localSpecial[i]=false;localGrenade[i]=false;
                }
                RecordTick(0,moves,flags);TickSimulation(NetStep,frameInputs);EndReplayTick();
            }
        }
        // Input bits shared by replays and online frames: 1 fire, 2 up, 4 jump, 8 special, 16 down (held), 32 grenade (pressed).
        const int HeldMask=1|2|16,FlagMask=63;
        static int HeldFlags(PlayerCommand c)=>(c.fire?1:0)|(c.aimUp?2:0)|(c.down?16:0);
        static PlayerCommand Command(int move,int flags)=>new PlayerCommand{move=move/100f,fire=(flags&1)!=0,aimUp=(flags&2)!=0,jump=(flags&4)!=0,special=(flags&8)!=0,down=(flags&16)!=0,grenade=(flags&32)!=0};

        void StartReplay()
        {
            replayRuns.Clear();replayToken="";replayCount=0;replayTicks=0;replayBroken="";replayStartScore=Score;localClock=0;
            for(int i=0;i<4;i++){localJump[i]=false;localSpecial[i]=false;localGrenade[i]=false;}
            recording=!headless;LastReplay=null;publish=Publish.None;verifyingOwn=false;
            var record=new ReplayRecord{version=Application.version,mission=CurrentMission.id,difficulty=SelectedDifficulty};
            for(int i=0;i<4;i++){record.heroes[i]=slots[i].joined?slots[i].hero:-1;record.names[i]=slots[i].joined&&Online?playerNames[i]??"":"";}
            pendingRecord=record;
        }
        ReplayRecord pendingRecord;
        void ReplayBroken(string reason){if(recording&&replayBroken.Length==0)replayBroken=reason;}
        void RecordTick(int left,int[] moves,int[] flags)
        {
            if(!recording||phase!=Phase.Playing)return;
            var token=new StringBuilder();
            if(left!=0)token.Append('L').Append(left).Append(':');
            bool first=true;
            for(int i=0;i<4;i++)if(pendingRecord.heroes[i]>=0){if(!first)token.Append(',');first=false;token.Append(moves[i]).Append('.').Append(flags[i]);}
            string text=token.ToString();
            if(text==replayToken)replayCount++;
            else{FlushRun();replayToken=text;replayCount=1;}
            replayTicks++;
        }
        void FlushRun(){if(replayCount>0)replayRuns.Append(replayCount).Append('*').Append(replayToken).Append(';');}
        void EndReplayTick()
        {
            if(!recording||phase==Phase.Playing||phase==Phase.Paused)return;
            recording=false;FlushRun();replayCount=0;
            var record=pendingRecord;record.runs=replayRuns.ToString();record.ticks=replayTicks;
            record.score=Score-replayStartScore;record.time=SimulationTime;record.won=phase==Phase.Won;record.hash=StateHash();record.continues=Continues;
            LastReplay=record;OnReplayFinished(record);
        }

        // ---------- Verification ----------
        // A second, inactive PanameGame replays a record: it never runs Awake, Start, Update or
        // OnGUI, owns no camera, sound or scenery, and its actors stay invisible.
        sealed class ReplayRun {public int count,left;public int[] moves=new int[4],flags=new int[4];}
        readonly List<ReplayRun> verifyRuns=new List<ReplayRun>();
        int verifyRun,verifyLeft,verifyTicks;
        ReplayRecord verifying;
        public static PanameGame CreateVerifier(MissionDefinition[] catalog)
        {
            var go=new GameObject("Vérification des replays");go.SetActive(false);
            var game=go.AddComponent<PanameGame>();game.InitHeadless(catalog);return game;
        }
        void InitHeadless(MissionDefinition[] catalog)
        {
            headless=true;missions=catalog;flashProperties=new MaterialPropertyBlock();
            actors=new GameObject("Acteurs vérifiés").transform;actors.SetParent(transform,false);
            projectileRoot=new GameObject("Projectiles vérifiés").transform;projectileRoot.SetParent(transform,false);
            effects=new ArcadeEffects(transform);
        }
        // Returns false when the record cannot even be set up (unknown mission, bad runs).
        public bool BeginVerify(ReplayRecord record)
        {
            verifying=null;verifyRuns.Clear();
            if(record==null||record.v!=1||record.heroes==null||record.heroes.Length!=4||string.IsNullOrEmpty(record.runs))return false;
            int index=Array.FindIndex(missions,m=>m.id==record.mission);
            if(index<0||record.difficulty<0||record.difficulty>=DifficultyCount)return false;
            int joined=0;
            for(int i=0;i<4;i++){slots[i].joined=record.heroes[i]>=0;slots[i].pad=null;if(slots[i].joined){slots[i].hero=Mathf.Clamp(record.heroes[i],0,HeroCount-1);joined++;}}
            if(joined==0||!ParseRuns(record.runs,record.heroes))return false;
            SelectedDifficulty=record.difficulty;missionIndex=index;
            carriedScore=0;phase=Phase.Lobby;BeginRound();
            verifying=record;verifyRun=0;verifyLeft=verifyRuns.Count>0?verifyRuns[0].count:0;verifyTicks=0;return true;
        }
        bool ParseRuns(string runs,int[] heroes)
        {
            int total=0;
            foreach(var chunk in runs.Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries))
            {
                int star=chunk.IndexOf('*');if(star<1||!int.TryParse(chunk.Substring(0,star),out int count)||count<1)return false;
                var run=new ReplayRun{count=count};string body=chunk.Substring(star+1);
                if(body.StartsWith("L")){int colon=body.IndexOf(':');if(colon<2||!int.TryParse(body.Substring(1,colon-1),out run.left))return false;body=body.Substring(colon+1);}
                var parts=body.Split(',');int k=0;
                for(int i=0;i<4;i++)if(heroes[i]>=0)
                {
                    if(k>=parts.Length)return false;var pair=parts[k++].Split('.');
                    if(pair.Length!=2||!int.TryParse(pair[0],out run.moves[i])||!int.TryParse(pair[1],out run.flags[i]))return false;
                    run.moves[i]=Mathf.Clamp(run.moves[i],-100,100);run.flags[i]&=FlagMask;
                }
                if(k!=parts.Length)return false;
                total+=count;if(total>60*60*60)return false;
                verifyRuns.Add(run);
            }
            return verifyRuns.Count>0;
        }
        // Advances the replay for at most budgetMs; null while running, then whether it matched.
        public bool? StepVerify(float budgetMs)
        {
            if(verifying==null)return false;
            var clock=Stopwatch.StartNew();
            while(clock.Elapsed.TotalMilliseconds<budgetMs)
            {
                if(phase!=Phase.Playing||verifyRun>=verifyRuns.Count)
                {
                    var record=verifying;verifying=null;
                    bool ended=phase==Phase.Won||phase==Phase.Lost;
                    return ended&&verifyRun>=verifyRuns.Count-1&&verifyTicks==record.ticks&&Score==record.score&&StateHash()==record.hash&&(phase==Phase.Won)==record.won&&Continues==record.continues;
                }
                var run=verifyRuns[verifyRun];
                for(int i=0;i<4;i++)frameInputs[i]=Command(run.moves[i],run.flags[i]);
                // A departure belongs to the first tick of its run only.
                if(run.left!=0&&verifyLeft==run.count)for(int i=0;i<4;i++)if((run.left&(1<<i))!=0)DropPlayer(i);
                TickSimulation(NetStep,frameInputs);verifyTicks++;
                if(--verifyLeft==0&&++verifyRun<verifyRuns.Count)verifyLeft=verifyRuns[verifyRun].count;
            }
            return null;
        }
        public float VerifyProgress=>verifying==null||verifying.ticks<=0?1:verifyTicks/(float)verifying.ticks;
    }
}
