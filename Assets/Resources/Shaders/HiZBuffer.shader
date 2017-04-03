Shader "Hidden/Hi-Z Buffer"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vertex
            #pragma fragment resolve
            #include "HiZBuffer.cginc"
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vertex
            #pragma fragment reduce
            #include "HiZBuffer.cginc"
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vertex
            #pragma fragment blit
            #define HI_Z_BLIT_FALL_THROUGH
            #include "HiZBuffer.cginc"
            ENDCG
        }
    }
}
