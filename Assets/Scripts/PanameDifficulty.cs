using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    public sealed partial class PanameGame
    {
        // Difficulties come from Resources/Content/difficulties; replays, online play and the leaderboard store their position in this list.
        public static DifficultyDefinition[] Difficulties=>ContentLibrary.Difficulties;
        public static int DifficultyCount=>Difficulties.Length;
        public static string[] DifficultyNames=>System.Array.ConvertAll(Difficulties,d=>d.name);
        // The position in the lobby's list; the content's per-difficulty values are read at the difficulty's column.
        public int SelectedDifficulty {get;private set;}=1;
        public DifficultyDefinition Level=>Difficulties[Mathf.Clamp(SelectedDifficulty,0,DifficultyCount-1)];
        int DifficultyIndex=>Level.column;
        int MaxPlayerHealth=>Level.health;
        int StartingReserves=>Level.livesPerPlayer?0:JoinedCount*Level.lives;
        // How often crates and drops come is part of the mission's rules (Content/rules).
        int KillsPerBonus=>Rules.killsPerDrop[DifficultyIndex];
        float SupplySpacing=>Rules.supplySpacing[DifficultyIndex];
        float FirstSupplyX=>Rules.firstSupply[DifficultyIndex];
        float HealthFactor=>Level.enemyHealth;
        float CountFactor=>Level.enemyCount;
        float SpeedFactor=>Level.enemySpeed;
        float AttackFactor=>Level.enemyCadence;
        public void SetDifficulty(int index)
        {
            if(HostOnly(()=>SetDifficulty(index))||phase!=Phase.Lobby)return;
            int previous=SelectedDifficulty;SelectedDifficulty=Mathf.Clamp(index,0,DifficultyCount-1);
            if(SelectedDifficulty!=previous)ClearReady();
        }
        void DrawDifficulty()
        {
            Label(54,166,110,26,"DIFFICULTÉ",12,Gold,true);
            int n=DifficultyCount;float step=692f/n;
            for(int i=0;i<n;i++)
                if(Button(172+i*step,166,step-8,26,Difficulties[i].name,SelectedDifficulty==i?Gold:C("2A414E"),SelectedDifficulty==i))SetDifficulty(i);
            if(!string.IsNullOrEmpty(Level.tagline))Label(172,192,692,16,Level.tagline,11,Cream,true,TextAnchor.MiddleCenter);
        }
    }
}
