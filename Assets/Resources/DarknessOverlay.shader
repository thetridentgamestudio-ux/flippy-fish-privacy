Shader "Custom/DarknessOverlay"
{
    Properties
    {
        _MainTex     ("Texture",          2D)     = "white" {}
        _Center      ("Fish UV Center",   Vector) = (0.5, 0.5, 0, 0)
        _InnerRadius ("Spotlight Radius", Float)  = 0.18
        _OuterRadius ("Falloff End",      Float)  = 0.30
        _Darkness    ("Max Darkness",     Float)  = 0.92
    }

    SubShader
    {
        Tags { "Queue"="Overlay+1" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            float4 _Center;
            float  _InnerRadius;
            float  _OuterRadius;
            float  _Darkness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 delta = i.uv - _Center.xy;
                // Correct for portrait aspect ratio (1080x1920 → width/height = 0.5625)
                delta.x *= _ScreenParams.x / _ScreenParams.y;
                float dist  = length(delta);
                float alpha = smoothstep(_InnerRadius, _OuterRadius, dist) * _Darkness;
                return fixed4(0.0, 0.0, 0.0, alpha);
            }
            ENDCG
        }
    }
}
