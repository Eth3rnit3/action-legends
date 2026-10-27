using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Paname;
public static class VerifyUpgrade
{
    static void Check(bool condition,string name,List<string> checks){if(!condition)throw new Exception("ÉCHEC : "+name);checks.Add(name);}
    public static object Main()
    {
        var checks=new List<string>();var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();
        float scale=Time.timeScale;Time.timeScale=0;
        try
        {
            Check(Resources.Load<Texture2D>("Art/action-legends-logo")&&Resources.Load<Texture2D>("Art/menu-card"),"Logo et cadres du menu chargés",checks);
            Check(PanameArt.RouteSegments==g.CurrentMission.map.backgrounds.Length,"Tous les décors de la mission chargés",checks);
            var master=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(master,System.IO.File.ReadAllBytes("Assets/ArtSources/route-master.png"));var original=master.GetPixels32();bool exact=true;int offset=0;
            for(int n=1;n<=3;n++)
            {
                var part=Resources.Load<Texture2D>("Art/route-"+n.ToString("00"));var pixels=part.GetPixels32();
                for(int y=0;y<part.height&&exact;y++)for(int x=0;x<part.width;x++)if(!pixels[y*part.width+x].Equals(original[y*master.width+offset+x])){exact=false;break;}
                offset+=part.width;
            }
            Check(exact&&offset==master.width,"Découpes de décor identiques pixel par pixel à la fresque",checks);UnityEngine.Object.DestroyImmediate(master);
            var route=g.transform.Find("Décor de mission/Panorama");
            var views=route.GetComponentsInChildren<SpriteRenderer>();
            bool joins=true,covered=true;var mission=g.CurrentMission;int last=views.Length-1;
            foreach(float half in new[]{8f,10f,12.5f,14f})for(float x=mission.cameraMin;x<=mission.cameraMax;x+=.5f)
            {
                PanameArt.UpdatePanorama(x,half,mission.cameraMin,mission.cameraMax);
                // Wide panoramas overlap their neighbour on purpose to cross-fade; only a gap is a fault.
                for(int n=0;n<last;n++)if(views[n+1].bounds.min.x>views[n].bounds.max.x+.001f)joins=false;
                if(views[0].bounds.min.x>x-half+.01f||views[last].bounds.max.x<x+half-.01f)covered=false;
            }
            Check(joins,"Raccords du décor sans trou",checks);
            Check(covered,"Panorama couvre tout le parcours et plusieurs formats d'écran",checks);
            Check(ArcadeEffects.HasAtlas,"Atlas des 24 effets chargé",checks);
            bool shaders=true;foreach(string name in new[]{"ArcadeSprite","ArcadeFX"}){var shader=Resources.Load<Shader>(name);if(!shader||!shader.isSupported||UnityEditor.ShaderUtil.GetShaderMessages(shader).Length>0)shaders=false;}
            Check(shaders,"Rendu des impacts et des effets compilé sans diagnostic",checks);
            for(int i=0;i<4;i++)g.slots[i].joined=i==0;g.BeginRound();
            var spawn=typeof(PanameGame).GetMethod("SpawnEnemy",BindingFlags.Instance|BindingFlags.NonPublic);spawn.Invoke(g,new object[]{5.5f,2});
            var foes=(IList)typeof(PanameGame).GetField("enemies",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(g);
            var commands=new PlayerCommand[4];commands[0]=new PlayerCommand{fire=true};bool hit=false,white=false,recoil=false;
            for(int n=0;n<40&&foes.Count>0;n++)
            {
                g.TickSimulation(1f/60,commands);var foe=foes[0];var t=foe.GetType();float flash=(float)t.GetField("flash").GetValue(foe);
                if(flash>0)
                {
                    hit=true;recoil=(float)t.GetField("stagger").GetValue(foe)>0&&Mathf.Abs((float)t.GetField("knockback").GetValue(foe))>0;
                    var r=(SpriteRenderer)t.GetField("illustrated").GetValue(foe);var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);white=block.GetFloat("_Flash")>.5f;break;
                }
            }
            Check(hit&&white&&recoil,"Un tir déclenche flash blanc, recul et réaction de l'ennemi",checks);
            Check(g.ActiveEffects>0&&g.EffectPoolCapacity<=128,"Effets actifs dans un pool borné",checks);
            commands[0]=default;for(int n=0;n<180;n++)g.TickSimulation(1f/60,commands);
            Check(g.ActiveEffects==0,"Les effets expirent et leurs objets sont réutilisables",checks);
            g.ReturnToLobby();return new{passed=checks,count=checks.Count,pool=g.EffectPoolCapacity};
        }
        finally{Time.timeScale=scale;}
    }
}
