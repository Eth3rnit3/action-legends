using UnityEngine;
namespace Paname
{
    // Heroes' weapons come from Content/weapons: each hero starts every life with its own unlimited weapon, crates hand out
    // lettered ones with limited ammunition, lost on death or when empty. The engine only moves generic projectiles
    // (bullet, flame, rocket, beam) with the numbers of the weapon that fired them.
    public sealed partial class PanameGame
    {
        // A fixed scatter pattern: a jittery weapon spreads its shots the same way on every machine.
        static readonly float[] Scatter={0,.62f,-.81f,.27f,-.45f,.93f,-.18f,.54f,-1,.36f,-.69f,.08f};
        int[] weaponCredit=new int[0];
        WeaponChance[] WeaponTable=>CurrentMission.weaponTable;
        void ResetWeaponDraws(){weaponCredit=new int[WeaponTable?.Length??0];}
        // Smooth weighted round robin: in every run of draws as long as the sum of weights, each weapon comes up weight times, spread out.
        WeaponDefinition DrawWeapon()
        {
            var table=WeaponTable;if(table==null||table.Length==0)return null;
            if(weaponCredit.Length!=table.Length)weaponCredit=new int[table.Length];
            int total=0,best=0;
            for(int i=0;i<table.Length;i++){weaponCredit[i]+=table[i].weight;total+=table[i].weight;if(weaponCredit[i]>weaponCredit[best])best=i;}
            weaponCredit[best]-=total;return table[best].gun;
        }
        // The entry point for anything that hands a hero a weapon (crates now, hostages later). False if the hero or the weapon is missing.
        public bool GiveWeapon(int slot,string id)
        {
            var weapon=ContentLibrary.FindWeapon(id);var p=slot>=0&&slot<4?players[slot]:null;
            if(weapon==null||p==null||p.hp<=0)return false;Arm(p,weapon,null);return true;
        }
        // Picking up the weapon already held fills its ammunition back up.
        void Arm(Player p,WeaponDefinition weapon,BonusDefinition bonus)
        {
            if(weapon==null)return;
            p.weapon=weapon;p.ammo=weapon.Unlimited?0:weapon.ammo[DifficultyIndex];
            Celebrate(p,!string.IsNullOrEmpty(bonus?.announce)?bonus.announce:weapon.announce,!string.IsNullOrEmpty(bonus?.voice)?bonus.voice:weapon.voice);
        }
        static void Disarm(Player p){p.weapon=p.Definition.Weapon;p.ammo=0;}
        void FirePlayer(Player p)
        {
            var w=p.weapon;var pr=w.projectile;Vector2 aim=AimDirection(p),muzzle=MuzzlePoint(p);
            if(pr.Beam)FireBeam(p,w,muzzle,aim);
            else
            {
                float scatter=pr.jitter*Scatter[p.shots%Scatter.Length];
                for(int j=0;j<pr.pellets;j++)
                {
                    float angle=(j-(pr.pellets-1)*.5f)*pr.spread+scatter;
                    Vector2 velocity=angle==0?aim*pr.speed:(Vector2)(Quaternion.Euler(0,0,angle)*aim)*pr.speed;
                    Load(Shoot(pr.drawn?ShotLook.Sprite:ShotLook.Tracer,muzzle,velocity,w.damage,false,true),w,p);
                }
            }
            p.shots++;
            if(w.flashKind>0)effects.Muzzle(muzzle,aim,w.flashKind==2);
            p.fireTimer=w.fireRate;p.recoil=.045f;if(w.recoil.push>0)p.kick=-aim.x*w.recoil.push;
            if(!w.Unlimited&&--p.ammo<=0)Disarm(p);
            if(w.casings)effects.Casing(muzzle+new Vector2(-p.facing*.62f,.04f),p.facing);
            shake=Mathf.Max(shake,w.recoil.shake);Sound(w.Clip);
        }
        // A weapon's projectile: its life, piercing and explosion, and its look.
        void Load(Shot s,WeaponDefinition w,Player owner)
        {
            if(s==null)return;var pr=w.projectile;var x=w.explosion;
            s.weapon=w;s.owner=owner;s.life=pr.life;s.pierce=pr.pierce;
            s.blast=x.radius;s.blastDamage=x.damage>0?x.damage:w.damage;s.blastBoss=x.bossDamage;s.blastAnywhere=true;
            if(s.look==ShotLook.Sprite){s.sprite.sprite=pr.sprite.Sprite;s.sprite.color=pr.tint;s.root.localScale=Vector3.one*pr.size;}
            else{if(!string.IsNullOrEmpty(pr.color)){var c=pr.tint;c.a=.65f;s.halo.color=c;}s.root.localScale=Vector3.one*pr.size;}
        }
        // A ray from the barrel: every enemy along it is hit at once (up to pierce + 1, nearest first), the boss too.
        void FireBeam(Player p,WeaponDefinition w,Vector2 muzzle,Vector2 aim)
        {
            // The street stops a ray fired downwards.
            var pr=w.projectile;float length=aim.y<-.01f?Mathf.Min(pr.range,muzzle.y/-aim.y):pr.range;Vector2 end=muzzle+aim*length;int direction=aim.x>=0?1:-1;
            beamTargets.Clear();beamDepths.Clear();
            foreach(var e in enemies)
            {
                Vector2 centre=e.position+Vector2.up;float depth=Vector2.Dot(centre-muzzle,aim);
                if(depth<-e.type.hitRadius||SegmentDistance(muzzle,end,centre)>=e.type.hitRadius+pr.radius)continue;
                int at=beamDepths.Count;while(at>0&&beamDepths[at-1]>depth)at--;beamDepths.Insert(at,depth);beamTargets.Insert(at,e);
            }
            int hits=pr.pierce<0?beamTargets.Count:Mathf.Min(beamTargets.Count,pr.pierce+1);
            if(pr.pierce>=0&&hits==pr.pierce+1)length=Mathf.Min(length,beamDepths[hits-1]+.4f);
            for(int i=0;i<hits;i++){int index=enemies.IndexOf(beamTargets[i]);if(index>=0)HitEnemy(index,w.damage,direction);}
            HitBossOrBomb(muzzle,muzzle+aim*length,w.damage);BeamFreesHostages(muzzle,muzzle+aim*length,p);
            var s=Shoot(ShotLook.Sprite,muzzle+aim*(length*.5f),Vector2.zero,w.damage,false,true);Load(s,w,p);
            if(s!=null){s.heading=aim;s.length=length;s.root.localScale=new Vector3(length,pr.size,1);s.root.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg);}
        }
        readonly System.Collections.Generic.List<Enemy> beamTargets=new System.Collections.Generic.List<Enemy>();
        readonly System.Collections.Generic.List<float> beamDepths=new System.Collections.Generic.List<float>();
        // Speeds up or slows down, and homes in on the nearest enemy (else the boss) within 14 units.
        void Steer(Shot s,float dt)
        {
            var pr=s.weapon.projectile;
            if(pr.Beam){if(s.owner!=null&&s.owner.hp>0)s.position=MuzzlePoint(s.owner)+s.heading*(s.length*.5f);return;}
            float speed=s.velocity.magnitude;
            if(pr.acceleration!=0&&speed>.0001f){float next=Mathf.Clamp(speed+pr.acceleration*dt,0,80);s.velocity*=next/speed;speed=next;}
            if(pr.homing>0&&speed>.0001f)
            {
                bool found=false;Vector2 target=default;float best=14;
                foreach(var e in enemies){float d=Vector2.Distance(e.position+Vector2.up,s.position);if(d<best){best=d;target=e.position+Vector2.up;found=true;}}
                if(!found&&BossActive){target=bossPosition;found=true;}
                if(found)
                {
                    float heading=Mathf.Atan2(s.velocity.y,s.velocity.x)*Mathf.Rad2Deg,wanted=Mathf.Atan2(target.y-s.position.y,target.x-s.position.x)*Mathf.Rad2Deg;
                    float turn=Mathf.Clamp(Mathf.DeltaAngle(heading,wanted),-pr.homing*dt,pr.homing*dt)+heading;
                    s.velocity=new Vector2(Mathf.Cos(turn*Mathf.Deg2Rad),Mathf.Sin(turn*Mathf.Deg2Rad))*speed;
                }
            }
        }
        // How far from its path a weapon's projectile reaches, growing with a flame.
        static float Reach(Shot s){var pr=s.weapon?.projectile;return pr==null?0:pr.radius*(1+pr.growth*Mathf.Clamp01(s.age/pr.life));}
        // Visual only: frames, growth, heading and the rocket's smoke.
        void DrawWeaponShot(Shot s,float dt)
        {
            var pr=s.weapon.projectile;if(pr.Beam)return;float t=Mathf.Clamp01(s.age/pr.life);
            if(s.look==ShotLook.Sprite&&pr.frames>1)s.sprite.sprite=pr.sprite.At(Mathf.Min(pr.frames-1,(int)(t*pr.frames)));
            if(pr.growth>0)s.root.localScale=Vector3.one*(pr.size*(1+pr.growth*t));
            if(s.velocity.sqrMagnitude>.0001f)s.root.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(s.velocity.y,s.velocity.x)*Mathf.Rad2Deg);
            if(pr.kind==2&&(int)(s.age/.045f)!=(int)((s.age-dt)/.045f))effects.Play(3,s.position-s.velocity.normalized*(pr.size*.45f),.38f,.32f,1,-s.velocity*.04f+Vector2.up*.3f);
        }
        // An explosion of a projectile: every enemy whose centre is within radius, and the boss for bossDamage.
        void Blast(Vector2 at,float radius,int damage,int bossDamage,int direction,bool shown)
        {
            if(shown){effects.Play(2,at,radius*1.1f,.44f);Sound(explosionClips);shake=Mathf.Max(shake,.3f);}
            for(int k=enemies.Count-1;k>=0;k--)if(Vector2.Distance(enemies[k].position+Vector2.up,at)<radius)HitEnemy(k,damage,direction,true);
            if(bossDamage>0&&BossActive&&BossWithin(at,radius))DamageBoss(bossDamage);
            BlastFreesHostages(at,radius);
        }
    }
}
