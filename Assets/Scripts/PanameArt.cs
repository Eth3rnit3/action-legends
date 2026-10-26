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
        static Texture2D heroesTexture;
        static readonly Sprite[,] heroFrames=new Sprite[4,4];
        // Enemy sheets differ between missions, so their frames are cached per texture.
        static readonly System.Collections.Generic.Dictionary<(Texture2D,int,int),Sprite> enemyFrames=new System.Collections.Generic.Dictionary<(Texture2D,int,int),Sprite>();
        public static Texture2D HeroesTexture => heroesTexture ? heroesTexture : (heroesTexture=Resources.Load<Texture2D>("Art/heroes"));
        public static Sprite Frame(bool enemy,int row,int frame)
        {
            if(enemy)return EnemyFrame(Resources.Load<Texture2D>("Art/enemies"),row,frame);
            row=Mathf.Clamp(row,0,3);frame=Mathf.Clamp(frame,0,3);
            if(heroFrames[row,frame])return heroFrames[row,frame];
            var t=HeroesTexture;if(!t)return null;
            return heroFrames[row,frame]=Cut(t,4,row,frame);
        }
        public static Sprite EnemyFrame(Texture2D sheet,int row,int frame)
        {
            if(!sheet)return null;
            int rows=MissionDefinition.SheetRows(sheet);row=Mathf.Clamp(row,0,rows-1);frame=Mathf.Clamp(frame,0,3);
            var key=(sheet,row,frame);
            if(enemyFrames.TryGetValue(key,out var sprite)&&sprite)return sprite;
            return enemyFrames[key]=Cut(sheet,rows,row,frame);
        }
        static Sprite Cut(Texture2D t,int rows,int row,int frame)
        {
            float w=t.width/4f,h=t.height/(float)rows;
            float left=w*.065f,right=w*.025f;
            return Sprite.Create(t,new Rect(frame*w+left,t.height-(row+1)*h,w-left-right,h),new Vector2((w*.4f-left)/(w-left-right),.045f),h*.9f/2.05f,0,SpriteMeshType.FullRect);
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
        public static void Line(Transform p,Vector2 a,Vector2 b,float width,Color c,int layer)
        {
            Vector2 v=b-a, mid=(a+b)*.5f; Shape(p,"Trait",mid.x,mid.y,v.magnitude,width,c,layer,false,Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg);
        }
        public static void Text(Transform p,string text,float x,float y,float size,Color color,int order)
        {
            var g=new GameObject(text); g.transform.SetParent(p,false); g.transform.localPosition=new Vector3(x,y,-.1f);
            CheckGlyphs(text); var t=g.AddComponent<TextMesh>(); t.text=text; t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize=48; t.characterSize=size/10; t.anchor=TextAnchor.MiddleCenter; t.color=color;
            var r=g.GetComponent<MeshRenderer>(); r.sharedMaterial=t.font.material; r.sortingOrder=order;
        }
        public static void Street(Transform root,string[] routePaths=null,float length=80,string groundPath="Art/paris")
        {
            if(routePaths==null)routePaths=new[]{"Art/route-01","Art/route-02","Art/route-03"};
            var first=Resources.Load<Texture2D>(routePaths[0]);
            if(first)
            {
                routeRoot=new GameObject("Panorama • trois quartiers continus").transform;routeRoot.SetParent(root,false);
                RouteWidth=0;RouteSegments=0;
                for(int i=0;i<routePaths.Length;i++)
                {
                    var texture=Resources.Load<Texture2D>(routePaths[i]);if(!texture)continue;
                    var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(0,.2f),texture.height/13f,0,SpriteMeshType.FullRect);
                    bool blend=i>0&&(texture.width/(float)texture.height>2||Resources.Load<Texture2D>(routePaths[i-1]).width/(float)Resources.Load<Texture2D>(routePaths[i-1]).height>2);
                    if(blend)RouteWidth-=1.5f;
                    var g=new GameObject(i==0?"Boulevard parisien":i==1?"Quais de Seine":"Place de la Bastille");g.transform.SetParent(routeRoot,false);g.transform.localPosition=new Vector3(RouteWidth,0,0);
                    var r=g.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=-85+i;
                    if(blend){var shader=Resources.Load<Shader>("BackgroundBlend");if(shader){r.sharedMaterial=new Material(shader);r.sharedMaterial.SetFloat("_LeftFade",1.5f/sprite.bounds.size.x);}}
                    RouteWidth+=sprite.bounds.size.x;RouteSegments++;
                }
                var ground=Resources.Load<Texture2D>(groundPath);
                if(ground)
                {
                    var road=Sprite.Create(ground,new Rect(0,0,ground.width,ground.height*.2f),new Vector2(0,1),ground.height/13f,0,SpriteMeshType.FullRect);
                    var shader=Resources.Load<Shader>("BackgroundBlend");var material=shader?new Material(shader):null;
                    if(material){material.SetFloat("_LeftFade",.05f);material.SetFloat("_TopFade",.22f);}
                    for(float x=-12;x<length+25;x+=road.bounds.size.x-2)
                    {
                        var go=new GameObject("Chaussée • premier plan");go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(x,0,0);
                        var r=go.AddComponent<SpriteRenderer>();r.sprite=road;r.sortingOrder=-35;if(material)r.sharedMaterial=material;
                    }
                }
                return;
            }
            var background=Resources.Load<Texture2D>("Art/paris");
            if(background)
            {
                var sprite=Sprite.Create(background,new Rect(0,0,background.width,background.height),new Vector2(.5f,.2f),background.height/13f);
                float width=sprite.bounds.size.x;
                for(int i=0;i<4;i++)
                {
                    var go=new GameObject("Paris illustré • secteur "+(i+1));go.transform.SetParent(root,false);
                    go.transform.localPosition=new Vector3(-8+width*.5f+i*width,0,0);
                    var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=-80;
                }
                return;
            }
            Shape(root,"Ciel",40,5,120,30,C("C6DFD9"),-100);
            Shape(root,"Soleil",14,8,4,4,C("F9D99B"),-99,true);
            // Distant Paris skyline and Eiffel tower.
            for(int i=-2;i<27;i++)
            {
                float x=i*4.1f, h=3.4f+Mathf.Sin(i*2.3f)*.7f;
                Shape(root,"Paris au loin",x,3+h*.5f,3.8f,h,C("95B9B6"),-95);
                Shape(root,"Toit lointain",x,3+h,3.9f,.3f,C("8BABA9"),-94);
            }
            var tower=new GameObject("Tour Eiffel au loin").transform; tower.SetParent(root,false); tower.localPosition=new Vector3(15,2.8f,0);
            Line(tower,new Vector2(-1.5f,0),new Vector2(0,5.9f),.13f,C("698E90"),-90); Line(tower,new Vector2(1.5f,0),new Vector2(0,5.9f),.13f,C("698E90"),-90);
            for(int i=0;i<5;i++) {float y=i*.9f; float w=1.4f*(1-y/6); Line(tower,new Vector2(-w,y),new Vector2(w,y),.1f,C("698E90"),-89); if(i<4) {Line(tower,new Vector2(-w,y),new Vector2(w*.8f,y+.9f),.06f,C("698E90"),-89);Line(tower,new Vector2(w,y),new Vector2(-w*.8f,y+.9f),.06f,C("698E90"),-89);} }
            string[] shops={"CAFÉ   PANAME","BOULANGERIE","DISQUES  2026","LE PETIT BISTROT","LIBRAIRIE"};
            Color[] walls={C("E6CEAA"),C("E9DABD"),C("DAC6AF"),C("D5D3B7")};
            for(int i=-1;i<13;i++)
            {
                float x=i*7.2f+2; float height=6.5f+(i%3)*.45f; var wall=walls[(i+4)%4];
                Box(root,"Immeuble haussmannien",x,height*.5f+1.05f,7,height,wall,-70);
                Shape(root,"Toit zinc",x,height+1.5f,7.25f,.85f,C("5C7681"),-68);
                Shape(root,"Lumière zinc",x,height+1.93f,7.3f,.09f,C("9BB3B3"),-67);
                for(int j=0;j<3;j++)
                {
                    float wx=x+(j-1)*2;
                    Box(root,"Lucarne",wx,height+1.45f,.65f,.7f,C("E7D4B4"),-65);
                    Shape(root,"Vitre lucarne",wx,height+1.45f,.38f,.45f,C("436572"),-63);
                    for(int row=0;row<2;row++)
                    {
                        float wy=4.25f+row*1.7f;
                        Box(root,"Encadrement",wx,wy,.96f,1.28f,C("F6E6C7"),-64);
                        Shape(root,"Fenêtre",wx,wy,.7f,1.08f,C("416575"),-62);
                        Shape(root,"Reflet",wx-.18f,wy,.11f,1.04f,C("91B5B3"),-61);
                        Shape(root,"Croisée",wx,wy,.045f,1.1f,C("E4CBA5"),-60);
                        Shape(root,"Balcon",wx,wy-.45f,1.28f,.07f,Ink,-58);
                        for(int b=-3;b<=3;b++) Shape(root,"Fer forgé",wx+b*.18f,wy-.65f,.035f,.4f,Ink,-58);
                        Shape(root,"Appui",wx,wy-.87f,1.35f,.08f,C("B49E86"),-57);
                    }
                }
                Shape(root,"Corniche",x,3.13f,7.25f,.18f,C("F6E8CB"),-55);
                Shape(root,"Commerce",x,1.7f,6.45f,2.45f,(i%2==0?Teal:C("994D4C")),-54);
                for(int j=-1;j<=1;j++) Box(root,"Vitrine",x+j*1.85f,1.57f,1.55f,1.7f,C("304E59"),-52);
                Shape(root,"Reflet vitrine",x-.4f,1.6f,.1f,1.55f,C("71968E"),-50,false,-20);
                Shape(root,"Enseigne",x,2.9f,6.45f,.5f,i%2==0?Teal:C("994D4C"),-48);
                Text(root,shops[(i+5)%5],x,2.91f,1.05f,Cream,-46);
                for(int j=0;j<12;j++) Shape(root,"Store rayé",x-3+j*.55f,2.52f,.55f,.22f,j%2==0?Cream:Coral,-45);
                Shape(root,"Descente d'eau",x+3.3f,4.1f,.08f,6.2f,C("8F8E80"),-44);
            }
            Shape(root,"Trottoir",40,.15f,110,.65f,C("B7B4A0"),-30); Shape(root,"Bordure",40,-.12f,110,.14f,C("F2DFB7"),-29);
            Shape(root,"Chaussée",40,-1.5f,110,2.7f,C("3F5962"),-28);
            for(int i=-5;i<95;i+=3) Shape(root,"Ligne de route",i,-1.1f,1.5f,.07f,C("B8C2B0"),-27);
            for(int i=-4;i<95;i+=8)
            {
                Box(root,"Lampadaire",i,2.1f,.12f,4.3f,C("375A61"),-20);
                Box(root,"Lanterne",i,4.35f,.52f,.75f,Gold,-18); Shape(root,"Chapeau de lanterne",i,4.79f,.7f,.12f,Ink,-16);
                Shape(root,"Pied du lampadaire",i,.16f,.38f,.3f,Ink,-16);
            }
            for(int i=0;i<4;i++)
            {
                float x=10+i*21;
                Box(root,"Jardinière",x,.25f,1.5f,.5f,C("A66C55"),-15);
                Box(root,"Tronc",x,1.7f,.25f,2.9f,C("906C56"),-14);
                Box(root,"Feuillage",x,3.35f,2.55f,2.5f,C("568B75"),-12,true);
                Shape(root,"Lumière feuillage",x-.42f,3.7f,1.55f,1.4f,C("79A080"),-10,true);
            }
            // Metro entrance, street furniture and comic props.
            Box(root,"Poteau métro",1,1.6f,.1f,3.2f,Teal,-8); Box(root,"Métro",1,3.3f,1.3f,.65f,Teal,-6); Text(root,"MÉTRO",1,3.3f,.85f,Cream,-4);
            Box(root,"Kiosque",34,1.3f,2.4f,2.6f,Teal,-8); Box(root,"Affiche kiosque",34,1.5f,1.9f,1.6f,Gold,-6); Text(root,"PARIS\n2026",34,1.5f,1.4f,Ink,-4);
            for(int i=0;i<3;i++)
            {
                float x=18+i*24; Box(root,"Barricade",x,.55f,2.1f,1.1f,C("B88C62"),-5);
                for(int j=-1;j<=1;j++) Shape(root,"Planches",x,.3f+j*.3f,2.05f,.16f,C("D6AD77"),-3,false,j*5);
            }
            Box(root,"Poteau sortie",79,1.9f,.14f,3.8f,Teal,-6); Box(root,"Sortie",79,3.7f,3,.65f,Teal,-4); Text(root,"BASTILLE  »",79,3.7f,1.1f,Cream,-2);
            // Tricolour flag above the café.
            Shape(root,"Drapeau bleu",5.5f,4.3f,.22f,.7f,C("476C9B"),-10); Shape(root,"Drapeau blanc",5.72f,4.3f,.22f,.7f,Cream,-10); Shape(root,"Drapeau rouge",5.94f,4.3f,.22f,.7f,Coral,-10);
        }
        public static Transform Actor(Transform parent,int hero,Color accent,bool enemy=false,int kind=0,Texture2D enemySheet=null)
        {
            var r=new GameObject(enemy?"Ennemi":"Héros").transform; r.SetParent(parent,false);
            var illustration=enemy&&enemySheet?EnemyFrame(enemySheet,kind,0):Frame(enemy,enemy?kind:hero,0);
            if(illustration)
            {
                Shape(r,"Ombre",0,.035f,1.2f,.17f,new Color(.06f,.1f,.15f,.28f),10,true);
                var g=new GameObject("Illustration");g.transform.SetParent(r,false);
                var renderer=g.AddComponent<SpriteRenderer>();renderer.sprite=illustration;renderer.sortingOrder=30;if(ArcadeEffects.ActorMaterial)renderer.sharedMaterial=ArcadeEffects.ActorMaterial;
                if(!enemy)Shape(r,"Repère joueur",0,-.04f,.55f,.055f,accent,31);
                return r;
            }
            Color skin=C("E3AB7A"), suit=enemy?C("242C36"):hero==0?C("A57B49"):hero==1?C("375751"):hero==2?C("E8D9BC"):C("465E58");
            float bulk=!enemy&&hero==1?1.18f:enemy&&kind==2?1.25f:1;
            Shape(r,"Ombre",0,.04f,1.25f,.2f,new Color(.08f,.16f,.2f,.2f),10,true);
            Box(r,"Jambe arrière",-.2f,.35f,.24f,.55f,enemy?C("222935"):C("394B53"),12);
            Box(r,"Jambe avant",.2f,.35f,.24f,.55f,enemy?C("222935"):C("394B53"),14);
            Box(r,"Botte arrière",-.15f,.09f,.39f,.2f,Ink,16); Box(r,"Botte avant",.26f,.09f,.39f,.2f,Ink,18);
            Box(r,"Corps",0,.95f,.65f*bulk,.8f,suit,20);
            Shape(r,"Veste lumière",-.2f,.99f,.12f,.62f,enemy?C("3A424B"):new Color(suit.r+.1f,suit.g+.1f,suit.b+.1f),22);
            Shape(r,"Ceinture",0,.61f,.72f*bulk,.1f,Ink,24); Shape(r,"Boucle",.1f,.61f,.13f,.12f,Gold,25);
            Box(r,"Tête",0,1.59f,.65f,.66f,enemy?C("242C36"):skin,26,true);
            if(enemy)
            {
                Box(r,"Capuche",-.16f,1.6f,.34f,.7f,C("303741"),27,true);
                Shape(r,"Ouverture cagoule",.15f,1.64f,.35f,.12f,C("CFAD8B"),29); Shape(r,"Œil",.22f,1.65f,.065f,.055f,Ink,30);
                Shape(r,"Marque de bande",-.16f,1.12f,.09f,.22f,kind==0?Coral:kind==1?Gold:C("8CABBA"),30);
                if(kind==1) Box(r,"Sac à dos",-.45f,1,.32f,.6f,C("6A6759"),21);
                if(kind==2) Box(r,"Bouclier improvisé",.55f,.9f,.22f,1.35f,C("687A80"),35);
            }
            else
            {
                var hair=hero==0?C("935434"):C("3F3733");
                Shape(r,"Cheveux",-.04f,1.9f,.56f,.22f,hair,28);
                Shape(r,"Patte",-.28f,1.7f,.13f,.33f,hair,29);
                Shape(r,"Œil",.19f,1.66f,.065f,.07f,Ink,30); Shape(r,"Sourcil",.18f,1.76f,.16f,.045f,Ink,30,false,-10);
                Shape(r,"Nez",.35f,1.55f,.14f,.13f,skin,30);
                if(hero==0) Shape(r,"Barbe rousse",.08f,1.39f,.43f,.2f,hair,30);
                if(hero==1) Shape(r,"Lunettes",.13f,1.67f,.4f,.15f,Ink,31);
                if(hero>=2) {Shape(r,"Bandeau",0,1.84f,.67f,.1f,hero==2?Teal:Coral,31);Shape(r,"Ruban",-.43f,1.74f,.3f,.1f,hero==2?Teal:Coral,31,false,25);}
                Shape(r,"Brassard joueur",-.33f,1.13f,.13f,.25f,accent,32);
            }
            Box(r,"Bras",.37f,1.05f,.47f,.25f,enemy?suit:skin,33);
            if(!enemy||kind==1)
            {
                Box(r,"Arme",.65f,1.17f,hero==1&&!enemy?.88f:.65f,.22f,C("4A616A"),36);
                Shape(r,"Canon",1.02f,1.17f,.26f,.1f,Ink,38);
                Shape(r,"Poignée",.49f,1.03f,.12f,.25f,Ink,35,false,12);
            }
            else Shape(r,"Barre",.73f,1.05f,.12f,.85f,C("81919A"),34,false,-25);
            return r;
        }
    }
}
