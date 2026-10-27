using System.Collections.Generic;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    // Hostages (POW) placed by the mission's map: tied up until a hero shoots the ropes or touches them, then they thank,
    // hand over a reward that drops as a bonus, and run off the screen. Every hero counts the hostages he freed until he
    // falls; the count is paid at the end of the mission (WinMission).
    public sealed partial class PanameGame
    {
        const int HostageTied=0,HostageThanks=1,HostageGive=2,HostageRun=3;
        sealed class Hostage
        {
            public MapHostage place;public HostageDefinition type;public Vector2 position;public int state,hits,facing=-1,animState=-1;public float time,animTime;public bool given;
            public Player rescuer;public Transform root;public SpriteRenderer illustrated;public int lastShot;
        }
        readonly List<Hostage> hostages=new List<Hostage>();
        // Per hostage type, the credits of its weighted rewards table (same round robin as DrawWeapon).
        readonly Dictionary<HostageDefinition,int[]> rewardCredit=new Dictionary<HostageDefinition,int[]>();
        public int HostagesFreed {get;private set;}
        // What the last won mission's tally paid: hostages per slot, points for them, seconds under parTime and points for them.
        public readonly int[] TallyHostages=new int[4];
        public int TallyHostagePoints {get;private set;}
        public int TallyTime {get;private set;}
        public int TallyTimePoints {get;private set;}
        void ResetHostages(){hostages.Clear();rewardCredit.Clear();HostagesFreed=0;}
        // At the start of a round: every hostage that waits for no wave.
        void PlaceHostages(){foreach(var place in CurrentMission.map.hostages)if(place.wave<0)SpawnHostage(place);}
        void PlaceWaveHostages(int wave){foreach(var place in CurrentMission.map.hostages)if(place.wave==wave)SpawnHostage(place);}
        void SpawnHostage(MapHostage place)
        {
            var type=place.definition;var h=new Hostage{place=place,type=type,position=new Vector2(place.x,place.y)};
            h.root=Actor(actors,type.animations,type.animations.states[type.tied].cells[0],Cream,false);h.root.name="Otage • "+type.name;
            h.illustrated=h.root.Find("Illustration").GetComponent<SpriteRenderer>();hostages.Add(h);ShowHostage(h,0);
        }
        bool HostageOnScreen(Hostage h)=>Mathf.Abs(h.position.x-cameraX)<SimHalfWidth-.3f;
        // A friendly shot through a tied hostage's ropes; true when it stops there.
        bool ShotFreesHostage(Shot s,Vector2 before)
        {
            for(int i=0;i<hostages.Count;i++)
            {
                var h=hostages[i];if(h.state!=HostageTied||!h.type.byShot||h.lastShot==s.serial||!HostageOnScreen(h))continue;
                var c=h.position+Vector2.up*h.type.hitbox.y;if(SegmentDistance(before,s.position,c)>=h.type.hitbox.radius+Reach(s))continue;
                h.lastShot=s.serial;if(++h.hits>=h.type.hits)FreeHostage(h,s.owner);
                return s.pierce==0;
            }
            return false;
        }
        void BeamFreesHostages(Vector2 from,Vector2 to,Player owner)
        {
            foreach(var h in hostages)if(h.state==HostageTied&&h.type.byShot&&HostageOnScreen(h)&&SegmentDistance(from,to,h.position+Vector2.up*h.type.hitbox.y)<h.type.hitbox.radius)FreeHostage(h,owner);
        }
        // Explosions (grenades, rockets, specials) cut the ropes of every hostage they reach.
        void BlastFreesHostages(Vector2 at,float radius)
        {
            foreach(var h in hostages)if(h.state==HostageTied&&h.type.byShot&&HostageOnScreen(h)&&Vector2.Distance(h.position+Vector2.up*h.type.hitbox.y,at)<radius+h.type.hitbox.radius)FreeHostage(h,null);
        }
        void FreeHostage(Hostage h,Player by)
        {
            // Credit goes to the hero who fired, else to the nearest one standing.
            if(by==null||by.hp<=0){float best=float.MaxValue;foreach(var p in players)if(p!=null&&p.hp>0&&Mathf.Abs(p.position.x-h.position.x)<best){best=Mathf.Abs(p.position.x-h.position.x);by=p;}}
            h.state=HostageThanks;h.time=0;h.rescuer=by;HostagesFreed++;
            if(by!=null){by.rescued++;h.facing=by.position.x>=h.position.x?1:-1;}
            effects.Play(1,h.position+Vector2.up*.8f,.8f,.2f);Announce("OTAGE LIBÉRÉ !");
            var t=h.type;Sound(t.clip?t.clip:string.IsNullOrEmpty(t.sound)?pickupClip:(t.clip=Resources.Load<AudioClip>(t.sound)));
            if(!string.IsNullOrEmpty(t.voice)){if(!bonusTakes.TryGetValue(t.voice,out var takes))bonusTakes[t.voice]=takes=TakesAt(t.voice);Speak(takes);}
        }
        // The reward of a placement, else one drawn from the hostage's table.
        void GiveHostageReward(Hostage h)
        {
            h.given=true;int kind;WeaponDefinition gun=null;
            if(h.place.bonus>=0){kind=h.place.bonus;gun=h.place.gun;}
            else
            {
                var table=h.type.rewards;if(!rewardCredit.TryGetValue(h.type,out var credit))rewardCredit[h.type]=credit=new int[table.Length];
                int total=0,best=0;for(int i=0;i<table.Length;i++){credit[i]+=table[i].weight;total+=table[i].weight;if(credit[i]>credit[best])best=i;}
                credit[best]-=total;var w=table[best];kind=HostageDefinition.BonusIndex(Rules,w.bonus,w.weapon);if(!string.IsNullOrEmpty(w.weapon))gun=ContentLibrary.FindWeapon(w.weapon);
            }
            if(kind>=0)DropPickup(h.position+new Vector2(h.facing*.9f,0),kind,gun);
        }
        void TickHostages(float dt)
        {
            for(int i=hostages.Count-1;i>=0;i--)
            {
                var h=hostages[i];var t=h.type;h.time+=dt;
                if(h.state==HostageTied)
                {
                    if(t.byTouch&&HostageOnScreen(h))foreach(var p in players)if(p!=null&&p.hp>0&&Mathf.Abs(p.position.x-h.position.x)<t.touchRadius&&Mathf.Abs(p.position.y-h.position.y)<1.2f){FreeHostage(h,p);break;}
                }
                else if(h.state==HostageThanks){if(h.time>=t.thanks){h.state=HostageGive;h.time=0;}}
                else if(h.state==HostageGive)
                {
                    if(!h.given&&h.time>=t.giveAt)GiveHostageReward(h);
                    if(h.time>=t.give){h.state=HostageRun;h.time=0;h.facing=t.run.direction!=0?t.run.direction:h.rescuer!=null&&h.rescuer.position.x>h.position.x?-1:1;}
                }
                else
                {
                    // Off the screen, the hostage is gone: a locked wave's screen holds the heroes, not him.
                    h.position.x+=h.facing*t.run.speed*dt;h.position.y=Mathf.Max(0,h.position.y-dt*6);
                    if(Mathf.Abs(h.position.x-cameraX)>SimHalfWidth+2){h.root.gameObject.SetActive(false);Destroy(h.root.gameObject);hostages.RemoveAt(i);continue;}
                }
                ShowHostage(h,dt);
            }
        }
        void ShowHostage(Hostage h,float dt)
        {
            var t=h.type;var set=t.animations;int state=h.state==HostageTied?t.tied:h.state==HostageThanks?t.thanking:h.state==HostageGive?t.giving:t.running;
            if(state!=h.animState){h.animState=state;h.animTime=0;}else h.animTime+=dt;
            h.root.position=h.position;h.root.localScale=new Vector3(h.facing,1,1);
            h.illustrated.sprite=set.Sprite(set.Cell(state,h.animTime,h.animTime));
        }
        // When the mission is won: each hero's hostages and the seconds under par become points, before the victory screen.
        void TallyMission()
        {
            var tally=Rules.tally;int count=0;tallyShownAt=-1;
            for(int i=0;i<4;i++){TallyHostages[i]=players[i]!=null?players[i].rescued:0;count+=TallyHostages[i];}
            TallyHostagePoints=count*tally.hostage[DifficultyIndex];
            float par=CurrentMission.parTime;TallyTime=par>0?Mathf.Max(0,Mathf.FloorToInt(par-SimulationTime)):0;TallyTimePoints=TallyTime*tally.perSecond[DifficultyIndex];
            Score+=TallyHostagePoints+TallyTimePoints;
        }

        // ---------- Interface ----------
        // Under a player's panel: the hostages he counts, and his lives when every hero has his own.
        void DrawPlayerTallies(int i,float x)
        {
            var p=players[i];var icon=Rules.tally.icon;
            if(icon.Texture)GUI.DrawTextureWithTexCoords(new Rect(x+8,77,20,20),icon.Texture,icon.TexCoords);
            Label(x+29,78,60,19,"×"+p.rescued,12,p.rescued>0?Gold:C("7C929A"),true);
            if(Level.livesPerPlayer)Label(x+130,78,124,19,p.lives+(p.lives>1?" VIES":" VIE"),12,p.lives>0?Cream:Coral,true,TextAnchor.MiddleRight);
        }
        float tallyShownAt=-1;
        const float TallySeconds=4.2f;
        int TallyTotal=>TallyHostagePoints+TallyTimePoints;
        // The end-of-mission tally, counted up for a few seconds before the victory screen. Visual only: the points
        // were added when the mission was won, and Enter or Start already moves on to the next mission.
        bool DrawTally()
        {
            if(TallyTotal<=0)return false;
            if(tallyShownAt<0||Time.unscaledTime<tallyShownAt)tallyShownAt=Time.unscaledTime;
            float t=Time.unscaledTime-tallyShownAt;if(t>TallySeconds)return false;
            Panel(-2000,-2000,5280,4720,new Color(.06f,.12f,.16f,.88f));
            IllustratedPanel(new Rect(300,150,680,420),Color.white);Panel(324,174,632,372,C("07192A"));
            Label(324,186,632,40,"FIN DE MISSION",30,Gold,true,TextAnchor.MiddleCenter);
            int Count(int value,float start)=>Mathf.RoundToInt(value*Mathf.Clamp01((t-start)/.8f));
            float y=244;int row=0;var icon=Rules.tally.icon;int each=Rules.tally.hostage[DifficultyIndex];
            for(int i=0;i<4;i++)
            {
                if(!slots[i].joined||players[i]==null)continue;
                float start=.3f+row*.45f;row++;
                Label(350,y,220,30,PlayerTag(i)+"  "+players[i].Definition.shortName,16,PlayerColors[i],true);
                if(icon.Texture)GUI.DrawTextureWithTexCoords(new Rect(580,y,30,30),icon.Texture,icon.TexCoords);
                Label(612,y,140,30,"×"+TallyHostages[i]+"  ×"+each.ToString("N0"),15,Cream,true);
                Label(760,y,170,30,Count(TallyHostages[i]*each,start).ToString("N0"),17,Gold,true,TextAnchor.MiddleRight);
                y+=38;
            }
            float late=.3f+row*.45f;
            if(CurrentMission.parTime>0)
            {
                Label(350,y,400,30,"TEMPS "+Clock(SimulationTime)+"  ·  "+TallyTime+" S D'AVANCE",15,Cream,true);
                Label(760,y,170,30,Count(TallyTimePoints,late).ToString("N0"),17,Gold,true,TextAnchor.MiddleRight);y+=38;late+=.45f;
            }
            Panel(350,y+4,580,2,C("34515C"));
            Label(350,y+12,300,34,"TOTAL",20,Cream,true);Label(650,y+12,280,34,"+"+Count(TallyTotal,late).ToString("N0"),22,Gold,true,TextAnchor.MiddleRight);
            if(Button(540,498,200,34,"PASSER  »",C("34515C")))tallyShownAt=Time.unscaledTime-TallySeconds-1;
            return true;
        }
    }
}
