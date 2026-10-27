using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    public sealed partial class PanameGame
    {
        public enum Difficulty { Detente, Arcade, Difficile, Legende }
        public Difficulty SelectedDifficulty {get;private set;}=Difficulty.Arcade;
        int DifficultyIndex=>(int)SelectedDifficulty;
        public static readonly string[] DifficultyNames={"DÉTENTE","ARCADE","DIFFICILE","LÉGENDE"};
        static readonly int[] HealthByDifficulty={6,5,4,3},LivesByDifficulty={3,2,1,1};
        static readonly float[] HealthFactors={1,1.8f,2.5f,3.2f},CountFactors={1,1.2f,1.5f,1.8f},SpeedFactors={1,1.12f,1.25f,1.4f},AttackFactors={1.15f,.85f,.65f,.5f};
        int MaxPlayerHealth=>HealthByDifficulty[DifficultyIndex];
        int StartingReserves=>JoinedCount*LivesByDifficulty[DifficultyIndex];
        // How often crates and drops come is part of the mission's rules (Content/rules).
        int KillsPerBonus=>Rules.killsPerDrop[DifficultyIndex];
        float SupplySpacing=>Rules.supplySpacing[DifficultyIndex];
        float FirstSupplyX=>Rules.firstSupply[DifficultyIndex];
        public void SetDifficulty(int index)
        {
            if(HostOnly(()=>SetDifficulty(index))||phase!=Phase.Lobby)return;
            SelectedDifficulty=(Difficulty)Mathf.Clamp(index,0,3);
        }
        void DrawDifficulty()
        {
            Label(54,174,110,28,"DIFFICULTÉ",12,Gold,true);
            for(int i=0;i<4;i++)
                if(Button(172+i*174,174,164,29,DifficultyNames[i],DifficultyIndex==i?Gold:C("2A414E"),DifficultyIndex==i))SetDifficulty(i);
        }
    }
}
