using System;
using System.Collections.Generic;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    // Effects use bounded pools: sustained four-player fire does not allocate a GameObject per flash.
    public sealed class ArcadeEffects
    {
        sealed class Effect
        {
            public GameObject go;public SpriteRenderer renderer;public Vector2 velocity;
            public int row;public float age,duration,size,rotation,spin,fps;public bool active,casing;public Sprite[] frames;
        }
        readonly List<Effect> pool=new List<Effect>();
        readonly Transform parent;
        readonly System.Random rng=new System.Random(42);
        static Texture2D atlas;static readonly Sprite[,] frames=new Sprite[4,6];
        static Material actorMaterial,fxMaterial;
        static Material FxMaterial
        {get{if(!fxMaterial){var shader=Resources.Load<Shader>("ArcadeFX");if(shader)fxMaterial=new Material(shader);}return fxMaterial;}}
        static Material normalMaterial;
        static Material NormalMaterial=>normalMaterial?normalMaterial:(normalMaterial=new Material(Shader.Find("Sprites/Default")));
        static readonly int FlashId=Shader.PropertyToID("_Flash");
        public int ActiveCount {get{int n=0;foreach(var e in pool)if(e.active)n++;return n;}}
        public int Capacity=>pool.Count;
        public static bool HasAtlas=>Atlas!=null;
        static Texture2D Atlas=>atlas?atlas:(atlas=Resources.Load<Texture2D>("Art/arcade-fx"));
        public ArcadeEffects(Transform root){parent=new GameObject("Effets arcade • pool").transform;parent.SetParent(root,false);}
        public static Material ActorMaterial
        {
            get
            {
                if(!actorMaterial){var shader=Resources.Load<Shader>("ArcadeSprite");if(shader)actorMaterial=new Material(shader);}
                return actorMaterial;
            }
        }
        public static void HitFlash(SpriteRenderer renderer,float amount,MaterialPropertyBlock properties)
        {
            if(!renderer||ActorMaterial==null)return;
            properties.Clear();properties.SetFloat(FlashId,Mathf.Clamp01(amount));renderer.SetPropertyBlock(properties);
        }
        public static Sprite Frame(int row,int column)
        {
            if(frames[row,column])return frames[row,column];var t=Atlas;if(!t)return null;
            float w=t.width/6f,h=t.height/4f;
            return frames[row,column]=Sprite.Create(t,new Rect(column*w,t.height-(row+1)*h,w,h),new Vector2(row==0?.2f:.5f,.5f),h,0,SpriteMeshType.FullRect);
        }
        Effect Acquire()
        {
            foreach(var effect in pool)if(!effect.active){effect.active=true;effect.go.SetActive(true);return effect;}
            if(pool.Count>=128)return null;
            var go=new GameObject("Effet");go.transform.SetParent(parent,false);var e=new Effect{go=go,renderer=go.AddComponent<SpriteRenderer>(),active=true};pool.Add(e);return e;
        }
        public void Play(int row,Vector2 position,float size,float duration,int direction=1,Vector2 velocity=default,float angle=0)
        {
            var sprite=Frame(row,0);if(!sprite)return;var e=Acquire();if(e==null)return;
            e.row=row;e.age=0;e.duration=duration;e.size=size;e.velocity=velocity;e.casing=false;e.spin=0;e.rotation=0;e.frames=null;
            e.go.name=row==0?"Flamme de bouche":row==1?"Impact":row==2?"Explosion":"Fumée";
            e.go.transform.position=position;e.go.transform.localScale=new Vector3(size*direction,size,1);e.go.transform.localRotation=Quaternion.Euler(0,0,angle);
            e.renderer.sprite=sprite;e.renderer.sharedMaterial=FxMaterial?FxMaterial:NormalMaterial;e.renderer.color=Color.white;e.renderer.sortingOrder=row==3?43:75;
        }
        // The flash points along the shot: mirrored for left-facing shots, then tilted for upward or diagonal ones.
        public void Muzzle(Vector2 muzzle,Vector2 aim,bool heavy)
        {
            aim=aim.sqrMagnitude>.0001f?aim.normalized:Vector2.right;int direction=aim.x<-.01f?-1:1;
            Play(0,muzzle,heavy?1.25f:.9f,.105f,direction,default,Mathf.Atan2(aim.y,Mathf.Abs(aim.x))*Mathf.Rad2Deg*direction);
            Play(3,muzzle+aim*.12f+Vector2.up*.05f,.4f,.32f,1,aim*.13f+Vector2.up*.4f);
        }
        public void Impact(Vector2 at,int direction,bool metal=false,bool explosive=false)
        {
            Play(explosive?2:1,at,explosive?2.4f:metal?1.05f:.8f,explosive?.44f:.18f,direction);
            Play(3,at,.65f,.38f,1,new Vector2(direction*.5f,.7f));
            for(int i=0;i<(metal?6:3);i++)Debris(at,new Vector2(direction*(1+(float)rng.NextDouble()*3),1+(float)rng.NextDouble()*4),metal?Gold:PanameArt.C("9BA8AC"),.04f,.35f);
        }
        // The defeated sprite tumbles away; an enemy with a death animation plays it on the way.
        public void Defeat(SpriteRenderer source,int direction,Sprite[] frames=null,float fps=0)
        {
            var e=Acquire();if(e==null)return;e.row=-2;e.age=0;e.duration=.34f;e.velocity=new Vector2(direction*3.2f,3.8f);e.casing=false;e.spin=-direction*210;e.rotation=0;e.frames=frames!=null&&frames.Length>0?frames:null;e.fps=fps;
            e.go.name="Réaction de défaite";e.go.transform.position=source.transform.position;e.go.transform.localScale=source.transform.lossyScale;e.go.transform.localRotation=Quaternion.identity;
            e.renderer.sprite=e.frames!=null?e.frames[0]:source.sprite;e.renderer.sharedMaterial=NormalMaterial;e.renderer.color=Color.white;e.renderer.sortingOrder=44;
        }
        public void Casing(Vector2 position,int direction)
        {
            Debris(position,new Vector2(-direction*(1.2f+(float)rng.NextDouble()),2+(float)rng.NextDouble()*2),Gold,.055f,.62f);
        }
        void Debris(Vector2 at,Vector2 velocity,Color color,float size,float life)
        {
            var e=Acquire();if(e==null)return;e.frames=null;e.row=-1;e.age=0;e.duration=life;e.size=size;e.velocity=velocity;e.casing=true;e.rotation=0;e.spin=(float)rng.NextDouble()*900+500;
            e.go.name="Douille ou éclat";e.go.transform.position=at;e.go.transform.localScale=new Vector3(size*2,size,1);e.go.transform.localRotation=Quaternion.identity;
            e.renderer.sprite=Square;e.renderer.sharedMaterial=NormalMaterial;e.renderer.color=color;e.renderer.sortingOrder=66;
        }
        public void Tick(float dt)
        {
            foreach(var e in pool)
            {
                if(!e.active)continue;e.age+=dt;
                if(e.age>=e.duration){e.active=false;e.go.SetActive(false);continue;}
                e.go.transform.position+=(Vector3)e.velocity*dt;
                if(e.casing||e.row==-2)
                {
                    e.velocity+=Vector2.down*13*dt;e.rotation+=e.spin*dt;e.go.transform.localRotation=Quaternion.Euler(0,0,e.rotation);
                    if(e.row==-2){var c=e.renderer.color;c.a=1-Mathf.Pow(e.age/e.duration,2);e.renderer.color=c;if(e.frames!=null)e.renderer.sprite=e.frames[Mathf.Min(e.frames.Length-1,(int)(e.age*(e.fps>0?e.fps:e.frames.Length/e.duration)))];}
                    if(e.casing&&e.go.transform.position.y<.015f){var p=e.go.transform.position;p.y=.015f;e.go.transform.position=p;e.velocity=new Vector2(e.velocity.x*.6f,Mathf.Abs(e.velocity.y)*.3f);e.spin*=.5f;}
                }
                else
                {
                    int index=Mathf.Min(5,(int)(e.age/e.duration*6));e.renderer.sprite=Frame(e.row,index);
                    if(e.row==3){var c=e.renderer.color;c.a=1-e.age/e.duration;e.renderer.color=c;}
                }
            }
        }
        public void Clear(){foreach(var e in pool){e.active=false;e.go.SetActive(false);}}
    }
}
