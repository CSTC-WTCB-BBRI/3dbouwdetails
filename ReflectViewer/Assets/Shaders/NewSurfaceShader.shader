Shader "Custom/Instanced Atlas"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
        _Tiles("Tiles", Vector) = (8,8,0.125,0.125)
        _Index("Tile Start Index", Float) = 0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 100

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float2 uv : TEXCOORD0;
                };

                sampler2D _MainTex;
                float4 _Tiles;
                float _Index;

                v2f vert(appdata v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);

                    o.vertex = UnityObjectToClipPos(v.vertex);

                    float index = max(0.0, _Index);

                    #if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)
                    index += unity_InstanceID;
                    #endif

                    float2 uvOffset = float2(
                        floor(fmod(index, _Tiles.x)),
                        floor(fmod(index * _Tiles.z, _Tiles.y))
                        );

                    o.uv = v.uv.xy + uvOffset.xy * _Tiles.zw;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    return tex2D(_MainTex, i.uv);
                }
                ENDCG
            }
        }
}