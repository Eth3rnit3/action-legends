using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Paname;
// Before/after material for import-setting changes: every texture of Resources/Art as the GPU samples it (tex/), and for
// each mission the four heroes and every enemy of its roster on the street, plus its boss, at screen size and zoomed 2x.
// Compare two runs pixel by pixel (the scene is deterministic: no simulation tick between placement and render).
public static class CaptureLight
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static void Set(object o,string name,object v)=>o.GetType().GetField(name,Flags).SetValue(o,v);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags,null,Array.ConvertAll(args,a=>a.GetType()),null).Invoke(o,args);
    static void SetField(object o,string name,object v)=>o.GetType().GetField(name).SetValue(o,v);
    static PanameGame g;static PanameGame.Player[] players;
    static readonly PlayerCommand[] commands=new PlayerCommand[4];
    static void Clear(IList list){foreach(var o in list){var r=o.GetType().GetField("root")?.GetValue(o) as Transform;if(r)r.gameObject.SetActive(false);}list.Clear();}
    public static object Main(string captures)
    {
        var lines=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();float scale=Time.timeScale;Time.timeScale=0;
        try
        {
            Directory.CreateDirectory(captures+"/tex");
            foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Art"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var rt=RenderTexture.GetTemporary(t.width,t.height,0,RenderTextureFormat.ARGB32);Graphics.Blit(t,rt);RenderTexture.active=rt;
                var shot=new Texture2D(t.width,t.height,TextureFormat.RGBA32,false);shot.ReadPixels(new Rect(0,0,t.width,t.height),0,0);shot.Apply();RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);
                File.WriteAllBytes(captures+"/tex/"+path.Substring("Assets/Resources/Art/".Length).Replace('/','~'),shot.EncodeToPNG());UnityEngine.Object.DestroyImmediate(shot);
                lines.Add(path+" "+t.width+"x"+t.height+" "+t.format);
            }
            var missions=(MissionDefinition[])Get(g,"missions");
            for(int m=0;m<missions.Length;m++)
            {
                var mission=missions[m];
                foreach(float at in new[]{.15f,.5f,.85f})
                {
                    float x=Mathf.Lerp(mission.cameraMin,mission.cameraMax,at);Street(m,x);
                    Capture(captures+"/"+mission.id+"-"+(int)(at*100)+".png",x,1);Capture(captures+"/"+mission.id+"-"+(int)(at*100)+"-zoom.png",x,2);
                }
                Street(m,mission.cameraMin+20,false);Set(g,"cameraX",mission.cameraMax);Call(g,"SpawnBoss");
                for(int i=0;i<4;i++)if(players[i]!=null)players[i].position=new Vector2(mission.cameraMax-8+i*1.4f,0);
                for(int n=0;n<40;n++){foreach(var p in players)if(p!=null)p.invulnerable=1;g.TickSimulation(1f/60,commands);}
                float bx=(float)Get(g,"cameraX");Capture(captures+"/"+mission.id+"-boss.png",bx,1);Capture(captures+"/"+mission.id+"-boss-zoom.png",bx+4,2);
            }
            g.ReturnToLobby();return string.Join("\n",lines);
        }
        finally{Time.timeScale=scale;}
    }
    // The four heroes standing on the left, every roster enemy idle, attacking and dying on the right.
    static void Street(int mission,float x,bool foes=true)
    {
        g.ReturnToLobby();Call(g,"LoadMission",mission);g.SetDifficulty(1);
        for(int i=0;i<4;i++){g.slots[i].joined=true;g.slots[i].hero=i;}
        g.BeginRound();players=(PanameGame.Player[])Get(g,"players");
        foreach(var name in new[]{"enemies","hostages","pickups","carriers","thrown"})Clear((IList)Get(g,name));((IList)Get(g,"arrivals")).Clear();
        var shots=(IList)Get(g,"shots");foreach(var shot in shots){SetField(shot,"active",false);((Transform)shot.GetType().GetField("root").GetValue(shot)).gameObject.SetActive(false);}shots.Clear();
        typeof(PanameGame).GetProperty("Wave").SetValue(g,g.CurrentMission.waves.Length);
        var started=(bool[])Get(g,"encounterStarted");for(int i=0;i<started.Length;i++)started[i]=true;
        Set(g,"screenLocked",false);Set(g,"encounterLock",false);Set(g,"cameraX",x);
        for(int i=0;i<4;i++){players[i].position=new Vector2(x-9+i*1.6f,0);players[i].invulnerable=0;Call(g,"SyncPlayer",i);}
        if(!foes)return;
        var roster=g.CurrentMission.enemies;var poses=new[]{"idle","attack","death"};int slot=0;
        foreach(var id in roster)foreach(var pose in poses)
        {
            var type=ContentLibrary.Enemy(id);var s=new EncounterSpawn{enemy=id,entry="none",x=x-2.2f+slot*1.45f,facing=-1,definition=type,kind=-1};slot++;
            var e=g.GetType().GetMethod("Arrive",Flags).Invoke(g,new object[]{s,0,null});int state=type.animations.Find(pose,0);
            SetField(e,"animState",state);SetField(e,"animTime",pose=="death"?.5f:0f);g.GetType().GetMethod("ShowEnemy",Flags).Invoke(g,new object[]{e,state,0f});
        }
    }
    static void Capture(string path,float x,float zoom)
    {
        Set(g,"shake",0f);var cam=g.gameCamera;float size=cam.orthographicSize;cam.aspect=16f/9;
        PanameArt.UpdatePanorama(x,size*cam.aspect,g.CurrentMission.cameraMin,g.CurrentMission.cameraMax);
        cam.orthographicSize=size/zoom;cam.transform.position=zoom>1?new Vector3(x-5,1.6f,-10):new Vector3(x,3.6f,-10);
        var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();
        RenderTexture.active=rt;var shot=new Texture2D(1280,720,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,1280,720),0,0);shot.Apply();
        cam.targetTexture=null;RenderTexture.active=null;cam.orthographicSize=size;File.WriteAllBytes(path,shot.EncodeToPNG());
    }
}
