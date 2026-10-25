using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    public sealed partial class PanameGame
    {
        Texture2D menuLogo,menuCard;
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
            Label(54,171,800,26,"QUATRE LÉGENDES. UNE ÉQUIPE. À VOUS D'ÉCRIRE LA SUITE.",14,Gold,true);
            IllustratedPanel(new Rect(913,27,315,155),Color.white);
            Label(938,43,265,23,"MISSION "+(MissionIndex+1).ToString("00"),13,Gold,true);
            Label(938,70,265,35,CurrentMission.title.ToUpperInvariant(),20,Cream,true);
            Label(938,112,265,24,CurrentMission.location.ToUpperInvariant()+"  /  COOP LOCALE",12,C("B2C7BD"),true);
            if(MissionCount>1)
            {
                if(Button(936,142,26,24,"‹",C("304C56")))SelectMission(-1);
                Label(969,142,204,24,(MissionIndex+1)+" / "+MissionCount+" MISSIONS",11,Gold,true,TextAnchor.MiddleCenter);
                if(Button(1179,142,26,24,"›",C("304C56")))SelectMission(1);
            }
            else Label(938,140,265,24,JoinedCount+" / 4  HÉROS DANS L'ÉQUIPE",12,PlayerColors[1],true);
            for(int i=0;i<4;i++)
            {
                float x=46+i*298;var slot=slots[i];Color accent=PlayerColors[i];
                IllustratedPanel(new Rect(x,211,286,380),Color.white);
                Panel(x+16,224,4,29,accent);
                Label(x+28,224,140,28,"JOUEUR 0"+(i+1),13,accent,true);
                Label(x+176,224,89,28,slot.joined?"PRÊT":"LIBRE",11,slot.joined?PlayerColors[1]:C("9AAFB2"),true,TextAnchor.MiddleRight);
                // A subdued amber disc gives the portrait a readable silhouette without a flat card.
                GUI.color=new Color(accent.r,accent.g,accent.b,.07f);
                GUI.DrawTexture(new Rect(x+55,270,175,157),Circle.texture);GUI.color=Color.white;
                Portrait(x+49,252,slot.hero,accent,1.12f);
                Label(x+16,450,254,28,HeroNames[slot.hero],21,Cream,true,TextAnchor.MiddleCenter);
                Label(x+16,478,254,20,HeroRoles[slot.hero],11,accent,true,TextAnchor.MiddleCenter);
                Panel(x+18,501,250,44,C("07192A"));
                if(Button(x+18,506,34,32,"‹",C("354F59")))ChangeHero(i,-1);
                Label(x+57,506,172,32,HeroSpecials[slot.hero],12,Cream,false,TextAnchor.MiddleCenter);
                if(Button(x+234,506,34,32,"›",C("354F59")))ChangeHero(i,1);
                if(slot.joined)
                {
                    Label(x+20,550,246,23,slot.DeviceName.ToUpperInvariant(),11,C("ACC9C2"),true,TextAnchor.MiddleCenter);
                }
                else if(i==1)
                {
                    if(Button(x+27,548,232,26,"REJOINDRE AU CLAVIER",C("304C56")))AddSecondKeyboard();
                }
                else Label(x+20,548,246,28,"A / ✕  POUR REJOINDRE",12,C("A1BCB9"),true,TextAnchor.MiddleCenter);
            }
            Label(51,610,800,25,"Q / E : héros   •   ← / → : mission   •   ENTRÉE : partir",14,Cream);
            Label(51,637,800,23,"MANETTE : croix gauche / droite pour choisir, START pour partir",12,C("9AB6B6"));
            if(Button(932,609,296,59,"DÉPLOYER L'ÉQUIPE  →",Coral,true))BeginRound();
            Panel(49,685,1178,1,new Color(.6f,.75f,.7f,.24f));
            Label(51,690,1176,22,"ACTION LEGENDS   /   1–4 JOUEURS   /   MISSION : "+CurrentMission.title.ToUpperInvariant()+"",10,C("92AEAE"),true);
        }
    }
}
