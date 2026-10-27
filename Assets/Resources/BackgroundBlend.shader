Shader "Paname/BackgroundBlend"
{
    Properties {[PerRendererData] _MainTex("Texture",2D)="white"{} _LeftFade("Left blend",Float)=0 _TopFade("Top blend",Float)=0 _Band("Ground band height in UV",Float)=.2}
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            sampler2D _MainTex;float _LeftFade,_TopFade,_Band;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv)*i.color;if(_LeftFade>0)c.a*=smoothstep(0,_LeftFade,i.uv.x);if(_TopFade>0)c.a*=smoothstep(0,_TopFade,1-i.uv.y/_Band);return c;}
            ENDCG
        }
    }
}
