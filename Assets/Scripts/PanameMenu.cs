using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    public sealed partial class PanameGame
    {
        Texture2D menuLogo,menuCard;
        int menuPlayer;
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
            LoadMenuArt();
            Panel(-2000,-2000,5280,4720,new Color(.025f,.055f,.085f,.65f));
            Panel(-2000,190,5280,520,new Color(.025f,.06f,.085f,.48f));
            if(menuLogo)GUI.DrawTexture(new Rect(30,9,690,170),menuLogo,ScaleMode.ScaleToFit,true);
            else Label(45,35,780,120,"ACTION LEGENDS",57,Cream,true);
            DrawDifficulty();DrawSoundOptions();
            IllustratedPanel(new Rect(913,27,315,155),Color.white);
            Panel(935,51,270,105,C("07192A"));
            Label(938,53,265,18,"MISSION "+(MissionIndex+1).ToString("00"),13,Gold,true);
            Label(938,77,265,27,CurrentMission.title.ToUpperInvariant(),20,Cream,true);
            Label(938,109,265,18,CurrentMission.location.ToUpperInvariant()+"  /  COOP LOCALE",12,C("B2C7BD"),true);
            if(MissionCount>1)
            {
                if(Button(936,142,26,24,"‹",C("304C56")))SelectMission(-1);
                Label(969,142,204,24,(MissionIndex+1)+" / "+MissionCount+" MISSIONS",11,Gold,true,TextAnchor.MiddleCenter);
                if(Button(1179,142,26,24,"›",C("304C56")))SelectMission(1);
            }
            else Label(938,132,265,20,JoinedCount+" / 4  HÉROS DANS L'ÉQUIPE",12,PlayerColors[1],true);
            int selectedSlot=menuPlayer;
            if(!slots[selectedSlot].joined){menuPlayer=0;selectedSlot=0;}
            for(int hero=0;hero<4;hero++)
            {
                float x=46+hero*298;Color accent=PlayerColors[selectedSlot];bool selected=slots[selectedSlot].hero==hero;
                var card=new Rect(x,211,286,380);
                if(GUI.Button(card,GUIContent.none,GUIStyle.none))SelectHero(selectedSlot,hero);
                IllustratedPanel(card,selected?Color.white:new Color(.8f,.85f,.85f,1));
                if(selected){Panel(x+14,222,258,28,accent);Label(x+18,222,250,28,"J"+(selectedSlot+1)+"  •  SÉLECTIONNÉ",13,Ink,true,TextAnchor.MiddleCenter);}
                else Label(x+20,224,246,28,"CHOISIR CE HÉROS",12,C("A5BBB8"),true,TextAnchor.MiddleCenter);
                Portrait(x+49,252,hero,accent,1.12f);
                Label(x+16,450,254,28,HeroNames[hero],21,Cream,true,TextAnchor.MiddleCenter);
                Label(x+16,478,254,20,HeroRoles[hero],11,selected?accent:C("9AAFB2"),true,TextAnchor.MiddleCenter);
                Panel(x+18,501,250,44,C("07192A"));
                Label(x+22,506,242,32,HeroSpecials[hero],13,Cream,false,TextAnchor.MiddleCenter);
                Label(x+20,548,246,27,selected?"PRÊT À PARTIR":"CLIQUE POUR CHOISIR",11,selected?accent:C("9AAFB2"),true,TextAnchor.MiddleCenter);
            }
            for(int i=0;i<4;i++)if(slots[i].joined)
            {
                if(Button(50+i*218,596,208,28,"J"+(i+1)+"  "+slots[i].DeviceName,menuPlayer==i?PlayerColors[i]:C("304C56"),menuPlayer==i))menuPlayer=i;
            }
            Label(51,633,800,22,"CLIC / ← / → : héros  •  ENTRÉE : partir  •  A / ✕ : rejoindre",13,Cream);
            Label(51,659,800,20,DifficultyHints[DifficultyIndex],12,Gold,true);
            if(Button(932,609,296,59,"DÉPLOYER L'ÉQUIPE  →",Coral,true))BeginRound();
            Panel(49,685,1178,1,new Color(.6f,.75f,.7f,.24f));
            Label(51,690,1176,22,"VERSION 0.5.0   /   ACTION LEGENDS   /   MISSION : "+CurrentMission.title.ToUpperInvariant()+"",10,C("92AEAE"),true);
        }
    }
}
