using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    public sealed partial class PanameGame
    {
        Texture2D menuLogo,menuCard,missionLogo;
        // The last local player who changed hero with the mouse: the CONTINUE? buttons answer for him.
        int menuPlayer;
        // One card per player, J1 to J4: the hero he plays (portrait, name, role, special, place in the list) with arrows to go
        // through every hero; an empty place invites to join. Online, only this browser's card has arrows.
        public static Rect HeroArrow(int slot,int direction)=>new Rect(46+slot*298+(direction<0?8:248),318,30,52);
        static Rect PlayerCard(int slot)=>new Rect(46+slot*298,211,286,380);
        void DrawPlayerCard(int i)
        {
            var card=PlayerCard(i);float x=card.x;bool joined=slots[i].joined;Color accent=PlayerColors[i];
            IllustratedPanel(card,joined?Color.white:new Color(.55f,.6f,.62f,1));
            Panel(x+14,222,258,28,joined?accent:C("304C56"));
            Label(x+18,222,250,28,joined?SlotLabel(i).ToUpperInvariant():"J"+(i+1)+"  ·  LIBRE",13,joined?Ink:C("A5BBB8"),true,TextAnchor.MiddleCenter);
            if(!joined)
            {
                Label(x+16,290,254,120,"?",96,new Color(.65f,.75f,.75f,.55f),true,TextAnchor.MiddleCenter);
                Label(x+16,440,254,28,Online?"PLACE LIBRE":"REJOINDRE",21,Cream,true,TextAnchor.MiddleCenter);
                Panel(x+18,515,250,44,C("07192A"));
                Label(x+22,520,242,32,Online?"UN AMI REJOINT AVEC LE LIEN DE LA PARTIE":"APPUIE SUR A / CROIX D'UNE MANETTE",12,C("B2C7BD"),true,TextAnchor.MiddleCenter);
                return;
            }
            int index=Mod(slots[i].hero,HeroCount);var hero=Heroes[index];
            Portrait(x+49,252,index,1.12f);
            Label(x+16,450,254,28,hero.name,21,Cream,true,TextAnchor.MiddleCenter);
            Label(x+16,478,254,20,hero.role,11,accent,true,TextAnchor.MiddleCenter);
            Panel(x+18,515,250,44,C("07192A"));
            Label(x+22,520,242,32,hero.specialName,13,Cream,false,TextAnchor.MiddleCenter);
            Label(x+16,563,254,20,(index+1)+" / "+HeroCount,12,Gold,true,TextAnchor.MiddleCenter);
            if(HeroCount<2||Online&&i!=LocalSlotIndex)return;
            for(int d=-1;d<=1;d+=2){var r=HeroArrow(i,d);if(Button(r.x,r.y,r.width,r.height,d<0?"<":">",C("304C56")))MenuArrow(i,d);}
        }
        // A card's arrow: this player goes to the previous or next hero (online, through the host).
        void MenuArrow(int slot,int direction)
        {
            if(slot<0||slot>=4||!slots[slot].joined)return;
            if(Online){if(slot==LocalSlotIndex)OnlineHero(Mod(slots[slot].hero+direction,HeroCount));return;}
            menuPlayer=slot;ChangeHero(slot,direction);
        }
        void LoadMenuArt()
        {
            if(!menuLogo)menuLogo=Resources.Load<Texture2D>("Art/action-legends-logo");
            if(!menuCard)menuCard=Resources.Load<Texture2D>("Art/menu-card");
        }
        // Nine-slicing preserves the illustrated screws and beveled corners at any panel size.
        void IllustratedPanel(Rect rect,Color tint)
        {
            LoadMenuArt();
            if(!menuCard){Panel(rect.x,rect.y,rect.width,rect.height,C("18303B"));return;}
            const float edge=22f,uv=.08f;
            float[] x={rect.x,rect.x+edge,rect.xMax-edge,rect.xMax};
            float[] y={rect.y,rect.y+edge,rect.yMax-edge,rect.yMax};
            float[] u={0,uv,1-uv,1};float[] v={1,1-uv,uv,0};
            GUI.color=tint;
            for(int r=0;r<3;r++)for(int c=0;c<3;c++)
                GUI.DrawTextureWithTexCoords(new Rect(x[c],y[r],x[c+1]-x[c],y[r+1]-y[r]),menuCard,new Rect(u[c],v[r+1],u[c+1]-u[c],v[r]-v[r+1]));
            GUI.color=Color.white;
        }
        void DrawLobby()
        {
            // While the code panel is open, the lobby underneath stays visible but ignores clicks.
            GUI.enabled=!codeEntry&&!boardOpen;DrawLobbyPage();GUI.enabled=true;
            if(codeEntry)DrawCodeEntry();
            else if(boardOpen)DrawBoard();
        }
        void DrawLobbyPage()
        {
            LoadMenuArt();
            Panel(-2000,-2000,5280,4720,new Color(.025f,.055f,.085f,.65f));
            Panel(-2000,190,5280,520,new Color(.025f,.06f,.085f,.48f));
            if(menuLogo)GUI.DrawTexture(new Rect(30,9,690,170),menuLogo,ScaleMode.ScaleToFit,true);
            else Label(45,35,780,120,"ACTION LEGENDS",57,Cream,true);
            DrawDifficulty();DrawSoundOptions();DrawCodeButton();DrawBoardButton();
            IllustratedPanel(new Rect(913,27,315,155),Color.white);
            Panel(935,51,270,105,C("07192A"));
            Label(938,53,265,18,"MISSION "+(MissionIndex+1).ToString("00"),13,Gold,true);
            // A mission logo takes the title's place; the location moves down under it.
            if(missionLogo)
            {
                GUI.DrawTexture(new Rect(938,71,265,50),missionLogo,ScaleMode.ScaleToFit,true);
                Label(938,121,265,18,CurrentMission.location.ToUpperInvariant(),11,C("B2C7BD"),true,TextAnchor.MiddleCenter);
            }
            else
            {
                Label(938,77,265,27,CurrentMission.title.ToUpperInvariant(),20,Cream,true);
                Label(938,109,265,18,CurrentMission.location.ToUpperInvariant(),12,C("B2C7BD"),true);
            }
            if(MissionCount>1)
            {
                bool browse=UnlockedCount>1;
                if(browse&&Button(936,142,26,24,"<",C("304C56")))SelectMission(-1);
                Label(969,142,204,24,UnlockedCount+" / "+MissionCount+" MISSIONS DÉBLOQUÉES",11,Gold,true,TextAnchor.MiddleCenter);
                if(browse&&Button(1179,142,26,24,">",C("304C56")))SelectMission(1);
            }
            for(int i=0;i<4;i++)DrawPlayerCard(i);
            if(!DrawOnlineLobbyFooter())
            {
                Label(51,606,800,22,"MANETTE : A / CROIX POUR REJOINDRE  ·  CROIX GAUCHE / DROITE POUR CHANGER DE HÉROS",13,Cream);
                Label(51,630,800,22,"J1 AU CLAVIER : Q / E OU FLÈCHES  ·  À LA SOURIS : LES FLÈCHES DE CHAQUE CARTE",13,C("B2C7BD"));
                if(Button(932,609,296,59,"DÉPLOYER L'ÉQUIPE  »",Coral,true))BeginRound();
            }
        }
    }
}
