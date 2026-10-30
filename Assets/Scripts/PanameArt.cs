using UnityEngine;

namespace Paname
{
    public static class PanameArt
    {
        static Sprite square, circle;
        static Transform routeRoot;
        public static float RouteWidth {get;private set;}
        public static int RouteSegments {get;private set;}
        public static void UpdatePanorama(float cameraX,float halfWidth,float cameraMin=7,float cameraMax=74)
        {
            if(!routeRoot)return;
            float scale=Mathf.Max(1,(2*halfWidth+3)/RouteWidth);
            routeRoot.localScale=new Vector3(scale,1,1);
            float scroll=Mathf.Clamp((RouteWidth*scale-2*halfWidth-2)/Mathf.Max(1,cameraMax-cameraMin),0,.5f);
            routeRoot.localPosition=new Vector3(cameraMin-halfWidth-1+(1-scroll)*(cameraX-cameraMin),0,0);
        }
        public static readonly Color Ink = C("203441"), Cream = C("F6E8CC"), Teal = C("277D80"), Coral = C("F87854"), Gold = C("F4C65D");
        public static Color C(string s) { ColorUtility.TryParseHtmlString("#" + s, out var c); return c; }
        static readonly System.Collections.Generic.HashSet<string> glyphWarnings = new System.Collections.Generic.HashSet<string>();
        // The browser build only embeds the Latin-1 glyphs of the built-in font: anything beyond renders blank there,
        // while the editor silently falls back to system fonts. Warn while developing instead.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void CheckGlyphs(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (char c in text) if (c > 'ÿ' && glyphWarnings.Add(text)) { Debug.LogWarning($"Caractère U+{(int)c:X4} « {c} » absent de la police web dans : {text}"); return; }
        }
        public static Sprite Square
        {
            get
            {
                if (!square) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, Color.white); t.Apply(); square = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(.5f,.5f), 1); }
                return square;
            }
        }
        public static Sprite Circle
        {
            get
            {
                if (!circle)
                {
                    var t = new Texture2D(64,64); t.filterMode = FilterMode.Bilinear;
                    for (int y=0;y<64;y++) for(int x=0;x<64;x++) { float d = Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32)); t.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(32-d))); }
                    t.Apply(); circle=Sprite.Create(t,new Rect(0,0,64,64),new Vector2(.5f,.5f),64);
                }
                return circle;
            }
        }
        public static Transform Shape(Transform parent,string name,float x,float y,float w,float h,Color color,int layer,bool round=false,float angle=0)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=new Vector3(x,y,0); go.transform.localScale=new Vector3(w,h,1); go.transform.localRotation=Quaternion.Euler(0,0,angle);
            var s=go.AddComponent<SpriteRenderer>(); s.sprite=round?Circle:Square; s.color=color; s.sortingOrder=layer; return go.transform;
        }
        public static Transform Box(Transform p,string name,float x,float y,float w,float h,Color color,int order,bool round=false)
        {
            Shape(p,name+" contour",x,y,w+.07f,h+.07f,Ink,order,round); return Shape(p,name,x,y,w,h,color,order+1,round);
        }
        public static void Text(Transform p,string text,float x,float y,float size,Color color,int order)
        {
            var g=new GameObject(text); g.transform.SetParent(p,false); g.transform.localPosition=new Vector3(x,y,-.1f);
            CheckGlyphs(text); var t=g.AddComponent<TextMesh>(); t.text=text; t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize=48; t.characterSize=size/10; t.anchor=TextAnchor.MiddleCenter; t.color=color;
            var r=g.GetComponent<MeshRenderer>(); r.sharedMaterial=t.font.material; r.sortingOrder=order;
        }
        // The panorama pieces side by side, each 13 units tall with the walking line at map.horizon of its height; wide pieces
        // cross-fade into the previous one. The bottom map.groundBand of the ground image is repeated along the street, in
        // world space: what stands on it (heroes, props, decor) stays put on it while the facades behind scroll in parallax.
        // It is stretched down to viewBottom when it would not reach the bottom of the screen.
        public static void Street(Transform root,MissionMap map,float length,float viewBottom)
        {
            routeRoot=new GameObject("Panorama").transform;routeRoot.SetParent(root,false);
            RouteWidth=0;RouteSegments=0;var routePaths=map.backgrounds;
            for(int i=0;i<routePaths.Length;i++)
            {
                var texture=Resources.Load<Texture2D>(routePaths[i]);if(!texture)continue;
                var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(0,map.horizon),texture.height/13f,0,SpriteMeshType.FullRect);
                bool blend=i>0&&(texture.width/(float)texture.height>2||Resources.Load<Texture2D>(routePaths[i-1]).width/(float)Resources.Load<Texture2D>(routePaths[i-1]).height>2);
                if(blend)RouteWidth-=1.5f;
                var g=new GameObject("Décor "+(i+1)+" • "+routePaths[i]);g.transform.SetParent(routeRoot,false);g.transform.localPosition=new Vector3(RouteWidth,0,0);
                var r=g.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=-85+i;
                if(blend){var shader=Resources.Load<Shader>("BackgroundBlend");if(shader){r.sharedMaterial=new Material(shader);r.sharedMaterial.SetFloat("_LeftFade",1.5f/sprite.bounds.size.x);}}
                RouteWidth+=sprite.bounds.size.x;RouteSegments++;
            }
            var ground=Resources.Load<Texture2D>(map.ground);if(!ground)return;
            var road=Sprite.Create(ground,new Rect(0,0,ground.width,ground.height*map.groundBand),new Vector2(0,1),ground.height/13f,0,SpriteMeshType.FullRect);
            var blendShader=Resources.Load<Shader>("BackgroundBlend");var material=blendShader?new Material(blendShader):null;
            if(material){material.SetFloat("_LeftFade",.05f);material.SetFloat("_TopFade",map.groundFade);material.SetFloat("_Band",map.groundBand);}
            float stretch=Mathf.Max(1,(map.groundTop-viewBottom)/road.bounds.size.y);
            // Each strip a little nearer than the previous one, so its faded left edge is drawn over the previous strip's end.
            int strip=0;
            for(float x=-12;x<length+25;x+=road.bounds.size.x-2,strip++)
            {
                var go=new GameObject("Chaussée • premier plan");go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(x,map.groundTop,-.001f*strip);go.transform.localScale=new Vector3(1,stretch,1);
                var r=go.AddComponent<SpriteRenderer>();r.sprite=road;r.sortingOrder=-35;if(material)r.sharedMaterial=material;
            }
            // The relief: each piece drawn over its span, its cell's bottom at drawY.
            if(map.terrain!=null)foreach(var t in map.terrain)
            {
                if(!t.Drawn)continue;var sprite=t.sprite.Sprite;if(!sprite)continue;
                float w=t.drawWidth>0?t.drawWidth:t.width,h=t.drawHeight>0?t.drawHeight:w*sprite.rect.height/sprite.rect.width;
                var go=new GameObject("Relief • "+t.type+" "+t.x);go.transform.SetParent(root,false);
                go.transform.localPosition=new Vector3(t.x+t.drawX+w*.5f,t.drawY+h*.5f,0);go.transform.localScale=new Vector3((t.flip?-w:w)*sprite.rect.height/sprite.rect.width,h,1);
                var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=t.front?70:21;
            }
            // Street scenery: behind the actors (a fountain an enemy steps out from), or in front of them.
            if(map.decor!=null)foreach(var d in map.decor)
            {
                var sprite=d.sprite.Sprite;if(!sprite)continue;
                var go=new GameObject("Décor posé • "+d.sprite.texture+" "+d.sprite.cell);go.transform.SetParent(root,false);
                go.transform.localPosition=new Vector3(d.x,d.y+d.height*.5f,0);go.transform.localScale=new Vector3(d.flip?-d.height:d.height,d.height,1);
                var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=d.front?70:22;
            }
        }
        // A hero or an enemy: a ground shadow, its illustrated sprite and, for heroes, the player's colour mark.
        public static Transform Actor(Transform parent,AnimationSet animations,int cell,Color accent,bool hero)
        {
            var r=new GameObject(hero?"Héros":"Ennemi").transform; r.SetParent(parent,false);
            Shape(r,"Ombre",0,.035f,1.2f,.17f,new Color(.06f,.1f,.15f,.28f),10,true);
            var g=new GameObject("Illustration");g.transform.SetParent(r,false);
            var renderer=g.AddComponent<SpriteRenderer>();renderer.sprite=animations.Sprite(cell);renderer.sortingOrder=animations.layer;if(ArcadeEffects.ActorMaterial)renderer.sharedMaterial=ArcadeEffects.ActorMaterial;
            if(hero)Shape(r,"Repère joueur",0,-.04f,.55f,.055f,accent,31);
            return r;
        }
    }
}
