using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Paname;
// Visual audit: each mission's whole route, one screen after the other (camera from cameraMin to cameraMax, one screen width
// apart), with its scenery, props, vehicles and hostages, as a contact sheet of 480x270 frames, 4 per row, labelled by x.
public static class CaptureRoute
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags|BindingFlags.Public).GetValue(o);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags,null,Array.ConvertAll(args,a=>a.GetType()),null).Invoke(o,args);
    const int W=480,H=270,Columns=4;
    public static object Main(string captures)
    {
        var lines=new List<string>();var g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();float scale=Time.timeScale;Time.timeScale=0;
        try
        {
            var missions=(MissionDefinition[])Get(g,"missions");
            for(int m=0;m<missions.Length;m++)
            {
                g.ReturnToLobby();Call(g,"LoadMission",m);g.SetDifficulty(1);
                for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=0;}
                g.BeginRound();var players=(PanameGame.Player[])Get(g,"players");
                var mission=g.CurrentMission;var cam=g.gameCamera;cam.aspect=16f/9;float half=cam.orthographicSize*cam.aspect;
                var xs=new List<float>();for(float x=mission.cameraMin;x<mission.cameraMax+half;x+=2*half)xs.Add(Mathf.Min(x,mission.cameraMax));
                int rows=(xs.Count+Columns-1)/Columns;var sheet=new Texture2D(W*Columns,H*rows,TextureFormat.RGB24,false);
                var rt=new RenderTexture(W*2,H*2,24);var shot=new Texture2D(W*2,H*2,TextureFormat.RGB24,false);
                for(int k=0;k<xs.Count;k++)
                {
                    float c=xs[k];players[0].position=new Vector2(c-6,0);Call(g,"SyncPlayer",0);
                    PanameArt.UpdatePanorama(c,half,mission.cameraMin,mission.cameraMax);cam.transform.position=new Vector3(c,3.6f,-10);
                    cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;shot.ReadPixels(new Rect(0,0,W*2,H*2),0,0);shot.Apply();
                    int col=k%Columns,row=rows-1-k/Columns;
                    for(int y=0;y<H;y++)for(int x=0;x<W;x++)sheet.SetPixel(col*W+x,row*H+y,shot.GetPixel(x*2,y*2));
                    lines.Add(missions[m].id+" #"+k+" : x "+c.ToString("0"));
                }
                cam.targetTexture=null;RenderTexture.active=null;sheet.Apply();
                File.WriteAllBytes(captures+"/route-"+missions[m].id+".png",sheet.EncodeToPNG());
            }
            g.ReturnToLobby();return string.Join("\n",lines);
        }
        finally{Time.timeScale=scale;}
    }
}
