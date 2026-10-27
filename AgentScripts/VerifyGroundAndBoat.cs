using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Paname;
// 0.8.0 polish: every mission frames its panoramas so the actors stand in the middle of the painted pavement (map.horizon,
// groundBand, groundTop, groundFade), and Marseille's speedboat sails the Vieux-Port's water (a boss lane): it stays on its
// waterline and between its bounds, rolls and trails its wake, and heroes on the quay sink it with straight and diagonal shots.
public static class VerifyGroundAndBoat
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static void Set(object o,string name,object v)=>o.GetType().GetField(name,Flags).SetValue(o,v);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags,null,Array.ConvertAll(args,a=>a.GetType()),null).Invoke(o,args);
    static void Check(bool ok,string label,List<string> checks){if(!ok)throw new Exception("ÉCHEC : "+label);checks.Add(label);}
    static PanameGame g;static PanameGame.Player[] players;static IList enemies,hostages;
    static readonly PlayerCommand[] commands=new PlayerCommand[4];
    static void Start(int mission,int difficulty)
    {
        g.ReturnToLobby();Call(g,"LoadMission",mission);g.SetDifficulty(difficulty);
        for(int i=0;i<4;i++){g.slots[i].joined=i==0;g.slots[i].hero=0;}
        g.BeginRound();players=(PanameGame.Player[])Get(g,"players");enemies=(IList)Get(g,"enemies");hostages=(IList)Get(g,"hostages");
        for(int i=0;i<4;i++)commands[i]=default;
    }
    static void Tick(int n=1){for(int t=0;t<n;t++){foreach(var p in players)if(p!=null&&p.hp>0)p.invulnerable=1;g.TickSimulation(1f/60,commands);}}
    static void Clear(IList list){foreach(var o in list){var r=o.GetType().GetField("root")?.GetValue(o) as Transform;if(r)r.gameObject.SetActive(false);}list.Clear();}
    static Vector2 BoatAt=>(Vector2)Get(g,"bossPosition");

    public static object Main(string captures)
    {
        var checks=new List<string>();g=UnityEngine.Object.FindAnyObjectByType<PanameGame>();float scale=Time.timeScale;Time.timeScale=0;
        try
        {
            var missions=(MissionDefinition[])Get(g,"missions");
            // Framing: the panoramas' pivot is the walking line, the foreground band starts below the feet and covers the screen's bottom.
            var lines=new List<string>();
            for(int m=0;m<missions.Length;m++)
            {
                Start(m,1);var map=g.CurrentMission.map;
                // The previous mission's scenery is destroyed at the end of the frame: take the newest.
                Transform street=null;foreach(Transform t in g.transform)if(t.name=="Décor de mission")street=t;var route=street.Find("Panorama");bool pivots=route.childCount==map.backgrounds.Length;
                foreach(Transform piece in route){var s=piece.GetComponent<SpriteRenderer>().sprite;pivots&=Mathf.Abs(s.pivot.y/s.rect.height-map.horizon)<1e-4f;}
                float bottom=float.MaxValue,top=float.MinValue;int strips=0;
                foreach(Transform t in street)if(t.name.StartsWith("Chaussée")){var b=t.GetComponent<SpriteRenderer>().bounds;bottom=Mathf.Min(bottom,b.min.y);top=Mathf.Max(top,b.max.y);strips++;}
                float panoramaBottom=-13*map.horizon,screenBottom=3.6f-g.gameCamera.orthographicSize;
                Check(pivots&&strips>0&&top<0&&top>panoramaBottom&&bottom<screenBottom-.05f&&map.horizon<.2f,missions[m].id+" : pieds au milieu du sol peint (horizon "+map.horizon+"), bande de premier plan de "+top.ToString("0.00")+" à "+bottom.ToString("0.00")+" sous les pieds, sur le bas du panorama ("+panoramaBottom.ToString("0.00")+") et jusqu'au bas de l'écran ("+screenBottom.ToString("0.0")+")",checks);
            }
            int marseille=Array.FindIndex(missions,x=>x.id=="mistral-gagnant");
            Boat(marseille,checks,captures);
            Shots(marseille,checks);
            g.ReturnToLobby();return string.Join("\n",checks);
        }
        finally{Time.timeScale=scale;}
    }
    // Straight to the speedboat's encounter, as VerifyLot2b's mid-bosses.
    static (MissionEncounter enc,float arena) Meet(int mission,int difficulty)
    {
        Start(mission,difficulty);var p=players[0];var m=g.CurrentMission;int index=Array.FindIndex(m.encounters,c=>c.HasBoss&&c.boss.id=="speedboat");var enc=m.encounters[index];
        Clear(enemies);Clear(hostages);int next=Array.FindIndex(m.waves,w=>w.atX>enc.atX);typeof(PanameGame).GetProperty("Wave").SetValue(g,next);
        var started=(bool[])Get(g,"encounterStarted");for(int i=0;i<started.Length;i++)started[i]=i<index||m.encounters[i].wave>=0&&m.encounters[i].wave<next;
        p.position=new Vector2(enc.atX-.5f,0);commands[0]=new PlayerCommand{move=1};Tick(30);commands[0]=default;
        return (enc,(float)Get(g,"bossArenaX"));
    }
    static void Boat(int mission,List<string> checks,string captures)
    {
        var (enc,arena)=Meet(mission,1);var lane=enc.boss.lane;var f=enc.boss.definition.flight;
        Check(g.MidBossActive&&lane.Set,"Le hors-bord de Marseille apparaît à x "+enc.atX+" avec un couloir d'eau ("+lane.left+" à "+lane.right+" du centre, ligne de flottaison "+lane.y+")",checks);
        float minX=99999,maxX=-99999,minY=99,maxY=-99,minRoll=99,maxRoll=-99;bool wake=false;var root=(Transform)Get(g,"bossRoot");
        for(int t=0;t<60*14;t++)
        {
            Tick(1);var b=BoatAt;minX=Mathf.Min(minX,b.x);maxX=Mathf.Max(maxX,b.x);minY=Mathf.Min(minY,b.y);maxY=Mathf.Max(maxY,b.y);
            float roll=Mathf.DeltaAngle(0,root.localEulerAngles.z);minRoll=Mathf.Min(minRoll,roll);maxRoll=Mathf.Max(maxRoll,roll);
            var w=(SpriteRenderer)Get(g,"bossWake");wake|=w&&w.enabled&&w.sprite;
            if(t==150||t==420||t==700)Capture(captures+"/boat-"+t/60+"s.png",(float)Get(g,"cameraX"),1);
            if(t==420)Capture(captures+"/boat-zoom.png",BoatAt.x+2,2);
        }
        Check(minX>=arena+lane.left-.01f&&maxX<=arena+lane.right+.01f&&maxX-minX>(lane.right-lane.left)*.8f,"Il navigue dans son couloir sans monter sur le quai (x de "+(minX-arena).ToString("0.0")+" à "+(maxX-arena).ToString("0.0")+" du centre)",checks);
        Check(minY>=lane.y-f.bob-.01f&&maxY<=lane.y+f.bob+.01f,"Il flotte sur sa ligne d'eau ("+minY.ToString("0.00")+" à "+maxY.ToString("0.00")+")",checks);
        Check(wake&&maxRoll-minRoll>f.roll,"Il tangue ("+minRoll.ToString("0.0")+"° à "+maxRoll.ToString("0.0")+"°) et traîne son sillage d'écume ; captures boat-*.png",checks);
    }
    // A hero on the quay damages it by shooting straight ahead and diagonally up, never standing in the water.
    static void Shots(int mission,List<string> checks)
    {
        var results=new List<string>();
        foreach(var diagonal in new[]{false,true})
        {
            var (enc,arena)=Meet(mission,1);var p=players[0];Clear(enemies);var lane=enc.boss.lane;
            // From the quay left of the water: straight shots cross the whole lane, diagonal ones while walking under the boat's path.
            p.position=new Vector2(arena+lane.left-2.5f,0);commands[0]=new PlayerCommand{move=.2f};Tick(2);
            int before=g.BossHealth;float lowest=99;
            for(int t=0;t<60*6;t++)
            {
                Clear(enemies);commands[0]=new PlayerCommand{move=diagonal?.3f:0,fire=true,aimUp=diagonal};Tick(1);lowest=Mathf.Min(lowest,p.position.y);
            }
            int dealt=before-g.BossHealth;results.Add((diagonal?"en diagonale ":"droit devant ")+dealt+" PV");
            Check(dealt>0&&lowest>=0,"Un héros sur le quai touche le hors-bord en tirant "+(diagonal?"en diagonale":"droit devant")+" ("+dealt+" PV en 6 s)",checks);
        }
    }
    static void Capture(string path,float x,float zoom)
    {
        foreach(var q in players)if(q!=null)q.invulnerable=0;for(int i=0;i<4;i++)if(players[i]!=null)Call(g,"SyncPlayer",i);
        Set(g,"shake",0f);var cam=g.gameCamera;float size=cam.orthographicSize;cam.aspect=16f/9;
        PanameArt.UpdatePanorama((float)Get(g,"cameraX"),size*cam.aspect,g.CurrentMission.cameraMin,g.CurrentMission.cameraMax);
        cam.orthographicSize=size/zoom;cam.transform.position=zoom>1?new Vector3(x,2.2f,-10):new Vector3(x,3.6f,-10);
        var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();
        RenderTexture.active=rt;var shot=new Texture2D(1280,720,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,1280,720),0,0);shot.Apply();
        cam.targetTexture=null;RenderTexture.active=null;cam.orthographicSize=size;File.WriteAllBytes(path,shot.EncodeToPNG());
    }
}
