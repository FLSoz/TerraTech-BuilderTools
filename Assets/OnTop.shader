
Shader "OnTop" {
Properties {
    _MainTex ("MainTex", 2D) = "white" {}
    _Color ("Color", Color) = (1,1,1,1)
}

SubShader {
    Tags {"IgnoreProjector"="True" "RenderType"="Opaque"}

    ZTest Always
    ZWrite On
    Blend SrcAlpha OneMinusSrcAlpha

    Pass {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct VertexInput {
                float4 pos : POSITION;
                float2 uv0 : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            uniform sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            uniform float4 _Color;

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos = UnityObjectToClipPos(v.pos);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (VertexOutput i) : COLOR
            {
                fixed4 col = tex2D(_MainTex, i.uv0);
                return col * _Color * i.color;
            }
        ENDCG
    }
}

}