using System.Collections.Generic;
using UnityEngine;
namespace Paname
{
    // Vehicles the heroes drive (map.vehicles, Content/vehicles), like the SV-001 of Metal Slug. A hero on foot who walks into an
    // empty one (or lands on it) gets in; while he drives, his position is the vehicle's, so everything that looks for the heroes
    // (camera, enemies, bosses, bonuses, hostages) finds him there, and every hit he would take wears the vehicle's own health down
    // instead. Fire shoots the gun that turns with the aim, grenade the cannon (its shells), down + jump gets out. At zero health
    // the pilot is thrown out, the vehicle blinks and blows up; an empty vehicle waits for anyone to take it.
    public sealed partial class PanameGame
    {
        public sealed class Vehicle
        {
            public MapVehicle place;public VehicleDefinition type;public Vector2 position;public Player rider,left;
            public int hp,maxHp,shells,facing=1,animState=-1;public float vertical,lift,aim,fireTimer,cannonTimer,ramTimer,invulnerable,flash,walk,firePose,wreck=-1,animTime,soundAt;
            public bool moving,firing,exploded;
            public Transform root,turretPivot;public SpriteRenderer body,gun,pilot;
            public bool Wrecked=>hp<=0;
            public float Left=>position.x-type.Half;public float Right=>position.x+type.Half;public float Top=>position.y+type.box.height;
        }
        readonly List<Vehicle> vehicles=new List<Vehicle>();
        public int VehicleCount=>vehicles.Count;
        void ResetVehicles(){vehicles.Clear();}
        void PlaceVehicles()
        {
            foreach(var place in CurrentMission.map.vehicles??new MapVehicle[0])
            {
                var t=place.definition;var v=new Vehicle{place=place,type=t,hp=t.health[DifficultyIndex],shells=t.cannon.shells[DifficultyIndex],facing=place.flip?-1:1};v.maxHp=v.hp;
                v.position=new Vector2(place.x,SolidAt(place.x,ForHeroes));
                v.root=new GameObject("Véhicule • "+t.name).transform;v.root.SetParent(actors,false);
                PanameArt.Shape(v.root,"Ombre",0,.035f,t.box.width*.9f,.2f,new Color(.06f,.1f,.15f,.28f),10,true);
                v.body=new GameObject("Carrosserie").AddComponent<SpriteRenderer>();v.body.transform.SetParent(v.root,false);v.body.sortingOrder=t.animations.layer;
                v.pilot=new GameObject("Pilote").AddComponent<SpriteRenderer>();v.pilot.transform.SetParent(v.root,false);v.pilot.sortingOrder=t.animations.layer-1;v.pilot.enabled=false;
                v.turretPivot=new GameObject("Tourelle").transform;v.turretPivot.SetParent(v.root,false);
                v.gun=new GameObject("Arme").AddComponent<SpriteRenderer>();v.gun.transform.SetParent(v.turretPivot,false);v.gun.sortingOrder=t.animations.layer+1;
                foreach(var r in new[]{v.body,v.pilot,v.gun})if(ArcadeEffects.ActorMaterial)r.sharedMaterial=ArcadeEffects.ActorMaterial;
                vehicles.Add(v);DrawVehicle(v,0);
            }
        }
        // The state shown follows the simulation, as the heroes' do: the gun's pivot and the cannon's mouth are read from its image.
        static int VehicleState(Vehicle v)
        {
            var t=v.type;
            if(v.Wrecked)return v.exploded&&t.destroyed>=0?t.destroyed:t.wrecked;
            bool hurt=v.hp<=v.maxHp*t.damagedAt;
            if(v.firePose>0)return hurt?t.damaged:t.fire;
            if(v.position.y-FloorUnder(v)>.1f)return t.jumping;
            return v.moving?(hurt?t.damagedRun:t.run):hurt?t.damaged:t.idle;
        }
        // The floor is cached in the vehicle's height above it: the state only needs to know whether it flies.
        static float FloorUnder(Vehicle v)=>v.position.y-v.lift;
        static int VehicleCell(Vehicle v)=>v.type.animations.Cell(v.animState<0?VehicleState(v):v.animState,v.animTime,v.walk*v.type.strideRate);
        Vector2 VehiclePoint(Vehicle v,string anchor){var local=v.type.animations.Points(anchor)[VehicleCell(v)];return v.position+new Vector2(local.x*v.facing,local.y);}
        Vector2 GunAim(Vehicle v)=>new Vector2(Mathf.Cos(v.aim*Mathf.Deg2Rad)*v.facing,Mathf.Sin(v.aim*Mathf.Deg2Rad));
        // The vehicle's box, for the enemies' shots and blasts that look for its pilot.
        static Vector2 VehicleCentre(Vehicle v)=>v.position+Vector2.up*(v.type.box.height*.5f);
        static float VehicleRadius(Vehicle v)=>(v.type.box.width+v.type.box.height)*.25f;
        bool InVehicleBox(Vehicle v,Vector2 at,float margin)=>at.x>v.Left-margin&&at.x<v.Right+margin&&at.y>v.position.y-margin&&at.y<v.Top+margin;

        void Board(Player p,Vehicle v)
        {
            var t=v.type;p.vehicle=v;v.rider=p;v.left=null;p.vertical=0;p.dash=0;p.salvo=0;p.crouching=p.aimUp=p.aimDown=false;p.knifePose=p.throwPose=p.specialPose=0;
            v.facing=p.facing;v.vertical=0;v.fireTimer=0;p.root.gameObject.SetActive(false);
            Sound(t.BoardClip);effects.Play(3,v.position+Vector2.up*.3f,1.4f,.4f);
            if(!string.IsNullOrEmpty(t.announce))Announce(t.announce);
            if(!string.IsNullOrEmpty(t.voice)){if(!bonusTakes.TryGetValue(t.voice,out var takes))bonusTakes[t.voice]=takes=TakesAt(t.voice);Speak(takes);}
            DrawVehicle(v,0);
        }
        // Out with down + jump, or thrown out when it breaks: up out of the cockpit, and not back in before he has stepped off it.
        void LeaveVehicle(Player p,bool thrown)
        {
            var v=p.vehicle;if(v==null)return;var b=v.type.boarding;p.vehicle=null;v.rider=null;v.left=p;v.moving=false;
            p.position=v.position;p.vertical=thrown?b.ejectJump:b.exitJump;p.dropFrom=float.MaxValue;p.invulnerable=Mathf.Max(p.invulnerable,thrown?b.ejectInvulnerable:b.exitInvulnerable);
            p.root.gameObject.SetActive(p.hp>0);Sound(thrown?v.type.EjectClip:v.type.ExitClip);
            DrawVehicle(v,0);
        }
        // The driving part of the hero's tick: his stick and buttons go to the vehicle, which carries him.
        void Drive(Player p,PlayerCommand input,float dt,float rear)
        {
            var v=p.vehicle;var t=v.type;
            if(input.jump&&input.down){LeaveVehicle(p,false);return;}
            float move=Mathf.Clamp(input.move,-1,1);v.moving=p.moving=Mathf.Abs(move)>.1f;if(v.moving)v.facing=p.facing=move>0?1:-1;
            bool grounded=v.position.y<=FloorAt(v.position.x,v.position.y,ForHeroes)+.001f;
            float from=v.position.x,dx=move*t.speed*(p.boost>0?1.2f:1)*dt;
            if(dx!=0&&breakables.Count>0)Crush(v,dx);
            v.position.x=VehicleAdvance(v,dx,out var rammed);
            float half=t.Half;v.position.x=Mathf.Clamp(v.position.x,rear+half-.7f,Mathf.Max(rear+half-.7f,(BossActive?bossArenaX+ArenaHalfWidth:screenLocked?lockX+SimHalfWidth:CurrentMission.length+1)-half));
            v.walk+=Mathf.Abs(v.position.x-from);
            if(input.jump&&grounded&&t.jump>0)v.vertical=t.jump;
            float fall=v.vertical;
            bool landed=Settle(ref v.position,ref v.vertical,t.gravity,dt,ForHeroes,float.MaxValue,grounded&&v.vertical<=0);
            p.position=v.position;if(relief){PitFall(p);v.position=p.position;}
            v.lift=v.position.y-FloorAt(v.position.x,v.position.y,ForHeroes);
            // Landing from a jump or a fall crushes the enemies under it.
            if(landed&&!grounded&&fall<-4&&t.ram.stomp>0)for(int e=enemies.Count-1;e>=0;e--){var en=enemies[e];if(Shootable(en)&&Mathf.Abs(en.position.x-v.position.x)<half+en.type.hitRadius*.5f&&Mathf.Abs(en.position.y-v.position.y)<1.2f)HitEnemy(e,t.ram.stomp,en.position.x>=v.position.x?1:-1,t.ram.woundKind);}
            // Ramming, every interval while it moves: the enemies in its way, the prop that walls it.
            v.ramTimer-=dt;
            if(v.ramTimer<=0&&v.moving)
            {
                bool hit=false;
                if(rammed!=null&&t.ram.propDamage>0){HitProp(rammed,t.ram.propDamage,v.facing);hit=true;}
                if(t.ram.damage>0)for(int e=enemies.Count-1;e>=0;e--){var en=enemies[e];if(Shootable(en)&&InVehicleBox(v,en.position+Vector2.up,en.type.hitRadius*.5f)){HitEnemy(e,t.ram.damage,v.facing,t.ram.woundKind);hit=true;}}
                if(hit)v.ramTimer=t.ram.interval;
            }
            // The gun turns towards the aim: ahead, up, diagonal while moving, down (straight down in the air).
            var tr=t.turret;float wanted=input.aimUp?(v.moving?tr.diagonal:tr.up):input.down?(v.lift>.1f?tr.downAir:tr.down):0;
            v.aim=Mathf.MoveTowards(v.aim,wanted,tr.turnRate*dt);
            v.fireTimer-=dt;v.cannonTimer-=dt;v.firePose=Mathf.Max(0,v.firePose-dt);v.firing=input.fire;
            if(input.fire&&v.fireTimer<=0)FireVehicleGun(p,v);
            if(input.grenade&&v.cannonTimer<=0&&v.shells>0)FireCannon(p,v);
            if(v.moving&&Time.unscaledTime>=v.soundAt&&t.EngineClip){Sound(t.EngineClip);v.soundAt=Time.unscaledTime+t.EngineClip.length*.9f;}
            p.position=v.position;p.floor=v.position.y-v.lift;p.vertical=0;p.crouching=p.aimUp=p.aimDown=false;
        }
        // Where a drive of dx ends: walls of the relief and standing props stop the front of its box, not only its middle. A wall is a
        // rise of more than a step from the ground before it and from the vehicle's height (a slope that runs into a block of its own
        // height is no wall). rammed: the prop that stopped it.
        float VehicleAdvance(Vehicle v,float dx,out Breakable rammed)
        {
            rammed=null;float x=v.position.x,y=v.position.y;if(!relief||dx==0)return x+dx;
            float to=Advance(x,y,dx,ForHeroes),s=dx>0?1:-1,half=v.type.Half,lead0=x+s*half,lead1=to+s*half;
            foreach(var g in solids)
            {
                if((g.blocks&ForHeroes)==0||!g.Standing)continue;
                float edge=s>0?g.left:g.right;
                if(!(s>0?edge>lead0-.0001f&&edge<=lead1:edge<lead0+.0001f&&edge>=lead1))continue;
                float top=TopAt(g,edge);if(top<=Mathf.Max(y,SolidAt(edge-s*.02f,ForHeroes))+StepUp)continue;
                float stop=edge-s*(half+.002f);
                if(s>0?stop<to:stop>to){to=s>0?Mathf.Max(x,stop):Mathf.Min(x,stop);rammed=g.prop;}
            }
            return to;
        }
        // Props that a vehicle drives over ("crush") break under it at once.
        void Crush(Vehicle v,float dx)
        {
            float left=Mathf.Min(v.Left,v.Left+dx),right=Mathf.Max(v.Right,v.Right+dx);
            for(int i=0;i<breakables.Count;i++){var b=breakables[i];if(b.type.crush&&b.hp>0&&PropHittable(b)&&b.Right>left&&b.Left<right&&b.position.y<v.Top&&b.Top>v.position.y)HitProp(b,b.hp,v.facing,true);}
        }
        // The heroes' weapons fire from the gun's barrel, along it.
        void FireVehicleGun(Player p,Vehicle v)
        {
            var t=v.type;var w=t.Gun;Vector2 aim=GunAim(v),muzzle=VehiclePoint(v,t.turret.anchor)+aim*t.turret.length;
            Volley(w,muzzle,aim,p,p.shots);p.shots++;
            if(w.flashKind>0)effects.Muzzle(muzzle,aim,w.flashKind==2);
            if(w.casings)effects.Casing(VehiclePoint(v,t.turret.anchor)+Vector2.up*.1f,v.facing);
            v.fireTimer=w.fireRate;shake=Mathf.Max(shake,w.recoil.shake);Sound(w.Clip);
        }
        void FireCannon(Player p,Vehicle v)
        {
            var t=v.type;var c=t.cannon;var w=c.Weapon;
            Vector2 aim=c.aimed?GunAim(v):new Vector2(Mathf.Cos(c.angle*Mathf.Deg2Rad)*v.facing,Mathf.Sin(c.angle*Mathf.Deg2Rad)),muzzle=VehiclePoint(v,c.anchor);
            Volley(w,muzzle,aim,p,v.shells);v.shells--;v.cannonTimer=w.fireRate;v.firePose=c.pose;
            effects.Muzzle(muzzle,aim,true);shake=Mathf.Max(shake,w.recoil.shake);Sound(w.Clip);
        }
        // Every hit its pilot would take: the vehicle's health, briefly flashing; at zero the pilot is thrown out and it blinks.
        void DamageVehicle(Vehicle v,int amount)
        {
            if(v.invulnerable>0||v.Wrecked||amount<=0)return;
            v.hp=Mathf.Max(0,v.hp-amount);v.invulnerable=v.type.invulnerable;v.flash=.075f;shake=Mathf.Max(shake,.15f);
            effects.Impact(VehicleCentre(v),-v.facing,true);Sound(impactClips);
            if(v.hp>0)return;
            v.wreck=v.type.wreck.time;if(v.rider!=null)LeaveVehicle(v.rider,true);
            effects.Play(2,VehicleCentre(v),1.6f,.4f);Sound(explosionClips);
        }
        void TickVehicles(float dt)
        {
            for(int i=0;i<vehicles.Count;i++)
            {
                var v=vehicles[i];v.invulnerable=Mathf.Max(0,v.invulnerable-dt);v.flash=Mathf.Max(0,v.flash-dt);
                // Its pilot gone from an online game: it stops where it is.
                if(v.rider!=null&&System.Array.IndexOf(players,v.rider)<0){v.rider=null;v.moving=false;}
                if(v.Wrecked&&!v.exploded){v.wreck-=dt;if(v.wreck<=0)ExplodeVehicle(v);}
                if(v.rider==null)
                {
                    v.moving=false;v.firePose=Mathf.Max(0,v.firePose-dt);
                    // Empty, it still falls when what it stood on breaks.
                    if(relief||v.vertical!=0){bool stood=v.position.y<=FloorAt(v.position.x,v.position.y,ForHeroes)+.001f;Settle(ref v.position,ref v.vertical,v.type.gravity,dt,ForHeroes,float.MaxValue,stood&&v.vertical<=0);v.lift=v.position.y-FloorAt(v.position.x,v.position.y,ForHeroes);}
                    if(!v.Wrecked)for(int j=0;j<4;j++)
                    {
                        var p=players[j];if(p==null||p.hp<=0||p.vehicle!=null||p.dash>0)continue;
                        bool inside=p.position.x>v.Left+.2f&&p.position.x<v.Right-.2f&&p.position.y>v.position.y-.3f&&p.position.y<v.Top+.3f;
                        // The hero who just got out has to step off it first.
                        if(v.left==p){if(!inside)v.left=null;continue;}
                        if(inside&&(!v.type.byJump||p.vertical<0&&p.position.y>v.position.y+v.type.box.height*.4f)){Board(p,v);break;}
                    }
                }
                DrawVehicle(v,dt);
            }
        }
        // It blows up: enemies, props and the boss around it, heroes too close when it hurts them; debris flies.
        void ExplodeVehicle(Vehicle v)
        {
            var t=v.type;var x=t.wreck;v.exploded=true;var at=VehicleCentre(v);
            effects.Play(2,at,Mathf.Max(2.4f,x.radius*1.2f),.6f);effects.Play(2,at+new Vector2(-.8f,.5f),x.radius*.8f,.5f);effects.Play(0,at+Vector2.up*.4f,x.radius,.45f,v.facing,Vector2.up*1.5f,90);
            Sound(explosionClips);Sound(t.BreakClip);shake=Mathf.Max(shake,.5f);
            if(x.radius>0)
            {
                for(int k=enemies.Count-1;k>=0;k--){var e=enemies[k];if(Shootable(e)&&Vector2.Distance(e.position+Vector2.up,at)<x.radius+e.type.hitRadius)HitEnemy(k,x.damage,e.position.x>=at.x?1:-1,Wounds.Explosion);}
                if(x.bossDamage>0&&BossActive&&BossWithin(at,x.radius))DamageBoss(x.bossDamage);
                if(x.heroDamage>0)foreach(var p in players)if(p!=null&&p.hp>0&&Vector2.Distance(HitCenter(p),at)<x.radius+HitRadius(p))Damage(p,x.heroDamage);
                BlastFreesHostages(at,x.radius);if(breakables.Count>0)BlastProps(at,x.radius,x.damage,v.facing);
            }
            if(t.debris.Drawn)for(int i=0;i<t.debris.count;i++)
            {
                float side=(i%2==0?1:-1)*(.35f+.65f*((i*37)%10)/10f),up=.55f+.45f*((i*53)%10)/10f;
                effects.Piece(t.debris.Piece(i),at+new Vector2(side*t.box.width*.3f,(i%3-1)*t.box.height*.2f),new Vector2(side*t.debris.speed,up*t.debris.speed*1.5f),(i%2==0?1:-1)*(280+i*40),t.debris.size,1.5f+.1f*(i%4),v.position.y);
            }
            DrawVehicle(v,0);
        }

        // ---------- Drawing ----------
        void DrawVehicle(Vehicle v,float dt)
        {
            if(!v.root)return;var t=v.type;var set=t.animations;
            int state=VehicleState(v);if(state!=v.animState){v.animState=state;v.animTime=0;}else v.animTime+=dt;
            bool gone=v.exploded&&t.destroyed<0;
            v.root.position=v.position;v.root.localScale=new Vector3(v.facing,1,1);
            int cell=VehicleCell(v);v.body.enabled=!gone;v.body.sprite=set.Sprite(cell);
            // A wreck about to blow up blinks (drawn only).
            float blink=v.Wrecked&&!v.exploded&&Mathf.Repeat(Time.unscaledTime,.16f)<.08f?1:v.flash>0?1:0;
            ArcadeEffects.HitFlash(v.body,blink,flashProperties);
            var tr=t.turret;bool armed=!v.Wrecked;v.turretPivot.gameObject.SetActive(armed);
            if(armed)
            {
                var pivot=set.Points(tr.anchor)[cell];v.turretPivot.localPosition=pivot;v.turretPivot.localRotation=Quaternion.Euler(0,0,v.aim);
                var look=v.firing&&v.rider!=null&&!string.IsNullOrEmpty(tr.firing?.texture)&&Mathf.Repeat(Time.unscaledTime,.1f)<.05f?tr.firing:tr.sprite;v.gun.sprite=look.Sprite;
                float width=look.Texture?tr.size*look.Texture.width/look.columns/(look.Texture.height/(float)look.rows):tr.size;
                v.gun.transform.localScale=Vector3.one*tr.size;v.gun.transform.localPosition=new Vector3((.5f-tr.pivot)*width,0,0);
                ArcadeEffects.HitFlash(v.gun,blink,flashProperties);
            }
            // The pilot: his own image, behind the body.
            var p=v.rider;v.pilot.enabled=p!=null&&!gone;
            if(p!=null)
            {
                var hero=p.Definition;var hs=hero.animations;int ps=hs.Find(t.pilot.state,hero.idle);
                v.pilot.sprite=hs.Sprite(hs.states[ps].cells[0]);v.pilot.transform.localPosition=set.Points(t.pilot.anchor)[cell];v.pilot.transform.localScale=Vector3.one*t.pilot.scale;
                ArcadeEffects.HitFlash(v.pilot,v.flash>0?1:0,flashProperties);
            }
        }
        // HUD: the vehicle's gauge replaces the pilot's health, its shells his grenades.
        void DrawVehicleHud(Player p,float x,Color accent)
        {
            var v=p.vehicle;var t=v.type;
            if(t.icon.Texture)GUI.DrawTextureWithTexCoords(new Rect(x+10,48,28,18),t.icon.Texture,t.icon.TexCoords);
            float w=130,fill=Mathf.Clamp01(v.hp/(float)v.maxHp);Panel(x+42,52,w,9,PanameArt.C("344A55"));
            Panel(x+42,52,w*fill,9,fill>t.damagedAt?accent:Mathf.Repeat(Time.unscaledTime,.4f)<.2f?PanameArt.Coral:PanameArt.Gold);
            var icon=t.shellIcon;if(icon.Texture)GUI.DrawTextureWithTexCoords(new Rect(x+10,61,17,17),icon.Texture,icon.TexCoords);
            Label(x+28,60,60,19,"×"+v.shells,12,v.shells>0?PanameArt.Cream:PanameArt.C("7C929A"),true);
        }
        public int VehicleHealth(int index)=>index>=0&&index<vehicles.Count?vehicles[index].hp:0;
    }
}
