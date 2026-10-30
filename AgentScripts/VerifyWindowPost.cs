using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Paname;
// An enemy who stays up at a window (y = fromY > 0) keeps his window around him while he lives, instead of standing in thin
// air once it faded; the window goes when he falls. Paris, wave 4: two throwers at windows at x 119 and 126.
public static class VerifyWindowPost
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags|BindingFlags.Public).GetValue(o);
    static void Set(object o,string name,object v)=>o.GetType().GetField(name,Flags|BindingFlags.Public).SetValue(o,v);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags,null,Array.ConvertAll(args,a=>a.GetType()),null).Invoke(o,args);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    static PanameGame g;static PanameGame.Player[] players;
    static readonly PlayerCommand[] commands=new PlayerCommand[4];
    static void Tick(int n){for(int t=0;t<n;t++){foreach(var p in players)if(p!=null&&p.hp>0)p.invulnerable=1;g.TickSimulation(1f/60,commands);}}
    // The openings on screen, as (x, alpha), and the enemies standing above the street.
    static List<Vector2> Windows(){var l=new List<Vector2>();foreach(var p in (IList)Get(g,"props")){var r=(SpriteRenderer)Get(p,"renderer");if(r)l.Add(new Vector2(r.transform.position.x,r.color.a));}return l;}
    static List<object> Perched(){var l=new List<object>();foreach(var e in (IList)Get(g,"enemies"))if(((Vector2)Get(e,"position")).y>1&&(int)Get(e,"hp")>0)l.Add(e);return l;}
    public static object Main(string captures)
    {
        var checks=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();float scale=Time.timeScale;Time.timeScale=0;
        try
        {
            var missions=(MissionDefinition[])Get(g,"missions");int paris=Array.FindIndex(missions,m=>m.id=="panique-a-paname");
            g.ReturnToLobby();Call(g,"LoadMission",paris);g.SetDifficulty(1);
            for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=0;}
            g.BeginRound();players=(PanameGame.Player[])Get(g,"players");
            // Straight to wave 4, its throwers' encounter included.
            typeof(PanameGame).GetProperty("Wave").SetValue(g,3);var started=(bool[])Get(g,"encounterStarted");var enc=g.CurrentMission.encounters;
            for(int i=0;i<started.Length;i++)started[i]=enc[i].wave>=0&&enc[i].wave<3||enc[i].wave<0&&enc[i].atX<128;
            players[0].position=new Vector2(124,0);commands[0]=new PlayerCommand{move=1};Tick(20);commands[0]=default;
            Tick(60*6);
            var perched=Perched();var windows=Windows();int shown=windows.FindAll(w=>w.y>.99f).Count;
            Check(perched.Count>=2&&shown>=perched.Count,perched.Count+" ennemis postés à une fenêtre 6 s après leur entrée, "+shown+" fenêtres encore affichées en plein",checks);
            Capture(captures+"/window-post.png",(float)Get(g,"cameraX"));
            foreach(var e in perched)Set(e,"hp",0);Tick(60);
            int left=Windows().FindAll(w=>w.y>.01f).Count;
            Check(left==0,"Leurs fenêtres s'effacent quand ils tombent ("+left+" encore visibles)",checks);
            g.ReturnToLobby();return string.Join("\n",checks);
        }
        finally{Time.timeScale=scale;}
    }
    static void Capture(string path,float x)
    {
        players[0].invulnerable=0;Call(g,"SyncPlayer",0);
        var cam=g.gameCamera;cam.aspect=16f/9;PanameArt.UpdatePanorama(x,cam.orthographicSize*cam.aspect,g.CurrentMission.cameraMin,g.CurrentMission.cameraMax);
        cam.transform.position=new Vector3(x,3.6f,-10);
        var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();
        RenderTexture.active=rt;var shot=new Texture2D(1280,720,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,1280,720),0,0);shot.Apply();
        cam.targetTexture=null;RenderTexture.active=null;File.WriteAllBytes(path,shot.EncodeToPNG());
    }
}
