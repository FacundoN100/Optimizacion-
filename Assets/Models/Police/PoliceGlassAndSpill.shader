Shader "Asfalto Rebelde/Police glass and spill"
{
    Properties { _Pulse ("Red / blue", Vector) = (0, 0, 0, 0) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Offset -1, -1
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Pulse;
            CBUFFER_END
            struct Input { float4 position : POSITION; half4 channels : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; half4 tint : COLOR; };
            Varyings Vert(Input v)
            {
                Varyings o;
                o.position = TransformObjectToHClip(v.position.xyz);
                // A nonzero second UV coordinate keeps a player taillamp steadily lit.
                // The patrol meshes use zero and continue to read their individual pulse.
                half3 red = lerp(half3(.12,.003,.004), half3(2.4,.055,.025), max(_Pulse.x, v.uv.y));
                half3 blue = lerp(half3(.004,.018,.13), half3(.045,.25,2.7), _Pulse.y);
                half3 warm = half3(1.32,.98,.60);
                // The asphalt reflects only part of the light; the glass keeps its full luminance.
                half strength = dot(v.channels.rgb, half3(.57h * _Pulse.x, .85h, .57h * _Pulse.y));
                o.tint = half4(v.channels.r*red + v.channels.g*warm + v.channels.b*blue,
                    v.channels.a * lerp(1, strength, v.uv.x));
                return o;
            }
            half4 Frag(Varyings i) : SV_Target { return i.tint; }
            ENDHLSL
        }
    }
}
