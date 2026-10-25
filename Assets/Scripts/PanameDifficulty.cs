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
        static readonly string[] DifficultyHints={"6 PV • 3 renforts / joueur • bonus généreux", "5 PV • 2 renforts / joueur • ennemis plus agressifs", "4 PV • 1 renfort / joueur • peu de bonus", "3 PV • dégâts doublés • survie sans répit"};
        static readonly int[] HealthByDifficulty={6,5,4,3},LivesByDifficulty={3,2,1,1},BonusKills={15,22,28,35};
        static readonly float[] HealthFactors={1,1.8f,2.5f,3.2f},CountFactors={1,1.2f,1.5f,1.8f},SpeedFactors={1,1.12f,1.25f,1.4f},AttackFactors={1.15f,.85f,.65f,.5f};
        int MaxPlayerHealth=>HealthByDifficulty[DifficultyIndex];
        int StartingReserves=>JoinedCount*LivesByDifficulty[DifficultyIndex];
        int KillsPerBonus=>BonusKills[DifficultyIndex];
        float SupplySpacing=>SelectedDifficulty==Difficulty.Detente?72:SelectedDifficulty==Difficulty.Arcade?90:SelectedDifficulty==Difficulty.Difficile?110:130;
        float FirstSupplyX=>SelectedDifficulty==Difficulty.Detente?24:SelectedDifficulty==Difficulty.Arcade?35:SelectedDifficulty==Difficulty.Difficile?45:55;
        public void SetDifficulty(int index)
        {
            if(phase!=Phase.Lobby)return;
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
