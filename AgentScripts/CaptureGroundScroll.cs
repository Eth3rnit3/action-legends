using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Paname;
// The street everything stands on scrolls with the world: for each mission, two frames 6 units apart with a hero, the props
// and the decor in view. Between the two, the pavement under their feet must not slide; only the facades behind do.
public static class CaptureGroundScroll
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags,null,Array.ConvertAll(args,a=>a.GetType()),null).Invoke(o,args);
    static PanameGame g;
    public static object Main(string captures)
    {
        var lines=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();float scale=Time.timeScale;Time.timeScale=0;
        try
        {
            var missions=(MissionDefinition[])Get(g,"missions");
            for(int m=0;m<missions.Length;m++)
            {
                g.ReturnToLobby();Call(g,"LoadMission",m);g.SetDifficulty(1);
                for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=0;}
                g.BeginRound();var players=(PanameGame.Player[])Get(g,"players");
                ((IList)Get(g,"enemies")).Clear();
                var map=g.CurrentMission.map;float at=map.decor!=null&&map.decor.Length>0?143:map.props!=null&&map.props.Length>0?map.props[0].x-2:30;
                Transform street=null;foreach(Transform t in g.transform)if(t.name=="Décor de mission")street=t;
                float top=float.MinValue,bottom=float.MaxValue;foreach(Transform t in street)if(t.name.StartsWith("Chaussée")){var b=t.GetComponent<SpriteRenderer>().bounds;top=Mathf.Max(top,b.max.y);bottom=Mathf.Min(bottom,b.min.y);}
                lines.Add(missions[m].id+" : bande de "+top.ToString("0.00")+" à "+bottom.ToString("0.00")+", écran jusqu'à "+(3.6f-g.gameCamera.orthographicSize).ToString("0.0"));
                foreach(var dx in new[]{0f,6f})
                {
                    float c=at+dx;players[0].position=new Vector2(at+2,0);Call(g,"SyncPlayer",0);
                    var cam=g.gameCamera;cam.aspect=16f/9;PanameArt.UpdatePanorama(c,cam.orthographicSize*cam.aspect,g.CurrentMission.cameraMin,g.CurrentMission.cameraMax);
                    cam.transform.position=new Vector3(c,3.6f,-10);
                    var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();
                    RenderTexture.active=rt;var shot=new Texture2D(1280,720,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,1280,720),0,0);shot.Apply();
                    cam.targetTexture=null;RenderTexture.active=null;File.WriteAllBytes(captures+"/ground-"+missions[m].id+"-"+(int)dx+".png",shot.EncodeToPNG());
                }
            }
            g.ReturnToLobby();return string.Join("\n",lines);
        }
        finally{Time.timeScale=scale;}
    }
}
