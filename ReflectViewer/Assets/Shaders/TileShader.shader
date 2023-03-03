Shader "Custom/TileShader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _EdgeTex("Albedo (RGB)", 2D) = "white" {}
        //[NoScaleOffset] _NormalMap("Normals", 2D) = "bump" {}
        _MortarSize("Mortar Size", Range(0,1)) = 0.02
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 200

            CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma surface surf Standard fullforwardshadows

            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0

            sampler2D _MainTex;
            sampler2D _EdgeTex;
            //sampler2D _NormalMap;

            struct Input
            {
                float2 uv_MainTex;
                float2 uv_EdgeTex;
            };
            float _MortarSize;
            half _Glossiness;
            half _Metallic;
            fixed4 _Color;
            float _ExtrudeVal;


        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        //UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        //UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            float scale = 1 / (1 + _MortarSize);
            float2 scaledTex = IN.uv_MainTex  - float2(floor(IN.uv_MainTex.r*scale) * _MortarSize, floor(IN.uv_MainTex.g*scale) * _MortarSize);
            fixed4 c = tex2D(_MainTex, scaledTex);
            fixed4 c2 = tex2D(_EdgeTex, IN.uv_EdgeTex) * _Color;

            o.Albedo = c.rgb;
            //_ExtrudeVal = 1;
            //if (fmod(IN.uv_MainTex.r+_MortarSize+10000/scale, 1/scale) < _MortarSize) {
                //o.Albedo = c2.rgb;
               // _ExtrudeVal = 0;
            //}
            //if (fmod(IN.uv_MainTex.g+_MortarSize+10000/scale, 1/scale) < _MortarSize) {
                //o.Albedo = c2.rgb;
               // _ExtrudeVal = 0;
            //}
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }

        //void vert(inout appdata_full v) { //WHY DOESNT THIS WORK?
        //    v.vertex.xyz += v.normal * _ExtrudeVal;
        //}
        ENDCG
    }
    FallBack "Diffuse"
}
