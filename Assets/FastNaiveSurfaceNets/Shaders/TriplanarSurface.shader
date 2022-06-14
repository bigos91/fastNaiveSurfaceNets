Shader "NaiveSurfaceNets/TriplanarSurface"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        float4 _MainTex_ST;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        struct Input
        {
            float3 localPos;
            float3 localNormal;
        };


        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)


		void vert(inout appdata_base v, out Input o) 
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);

            o.localPos = v.vertex;
            o.localNormal = v.normal.xyz;
		}


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 blending = normalize(abs(IN.localNormal));
            blending /= dot(blending, 1.0f);

            float2 uvx = IN.localPos.yz * _MainTex_ST;
            float2 uvy = IN.localPos.zx * _MainTex_ST;
            float2 uvz = IN.localPos.xy * _MainTex_ST;

            half4 cx = tex2D(_MainTex, uvx) * blending.x;
            half4 cy = tex2D(_MainTex, uvy) * blending.y;
            half4 cz = tex2D(_MainTex, uvz) * blending.z;
            half4 color = (cx + cy + cz) * _Color;

            o.Alpha = color.a;
            o.Albedo = color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
