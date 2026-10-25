Shader "Paname/ArcadeFX"
{
    Properties {[PerRendererData] _MainTex("Sprite",2D)="white"{}}
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
        Cull Off ZWrite Off Lighting Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            sampler2D _MainTex;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*i.color;
                float2 uv=frac(i.uv*float2(6,4));float2 edge=min(uv,1-uv);
                c.a*=smoothstep(0,.035,min(edge.x,edge.y));return c;
            }
            ENDCG
        }
    }
}
