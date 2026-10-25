using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Paname;
public static class CaptureArcadeEffects
{
    public static object Main()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var cam=g.gameCamera;float oldTime=Time.timeScale;var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;
        var rt=new RenderTexture(1000,500,24);rt.Create();var image=new Texture2D(1000,500,TextureFormat.RGB24,false);
        string folder=Path.GetFullPath("Temp/ArcadeFrames");Directory.CreateDirectory(folder);
        try
        {
            for(int i=0;i<4;i++){g.slots[i].joined=i<2;g.slots[i].hero=i;}
            g.BeginRound();Time.timeScale=0;
            var players=(PanameGame.Player[])typeof(PanameGame).GetField("players",flags).GetValue(g);
            players[0].position=new Vector2(1.5f,0);players[1].position=new Vector2(3.6f,0);players[0].invulnerable=10;players[1].invulnerable=10;
            typeof(PanameGame).GetMethod("SpawnEnemy",flags).Invoke(g,new object[]{9.5f,2});
            var foes=(System.Collections.IList)typeof(PanameGame).GetField("enemies",flags).GetValue(g);
            foes[0].GetType().GetField("hp").SetValue(foes[0],999);
            var commands=new PlayerCommand[4];cam.targetTexture=rt;cam.aspect=2;cam.orthographicSize=3.6f;cam.transform.position=new Vector3(6,2,-10);
            PanameArt.UpdatePanorama(6,7.2f);
            for(int frame=0;frame<50;frame++)
            {
                for(int i=0;i<2;i++)commands[i]=new PlayerCommand{fire=true,special=i==1&&frame==22};
                for(int sub=0;sub<3;sub++){g.TickSimulation(1f/60,commands);commands[1].special=false;}
                cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1000,500),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(folder,frame.ToString("000")+".png"),image.EncodeToPNG());
            }
            return new{frames=50,fps=20,path=folder};
        }
        finally
        {
            cam.targetTexture=oldTarget;RenderTexture.active=oldActive;cam.ResetAspect();rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
            for(int i=1;i<4;i++)g.slots[i].joined=false;g.ReturnToLobby();Time.timeScale=oldTime;
        }
    }
}
