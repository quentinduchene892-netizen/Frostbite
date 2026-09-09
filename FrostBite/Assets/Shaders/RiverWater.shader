Shader "FrostBite/RiverWater"
{
    Properties
    {
        _ShallowColor ("Couleur haut-fond", Color) = (0.55, 0.78, 0.82, 0.70)
        _DeepColor    ("Couleur profondeur", Color) = (0.05, 0.20, 0.30, 0.95)
        _DepthRange   ("Portee de profondeur", Float) = 3.5

        _FoamColor    ("Couleur ecume", Color) = (1, 1, 1, 1)
        _FoamEdge     ("Ecume de rive", Float) = 1.2
        _FoamCutoff   ("Seuil ecume de courant", Range(0,1)) = 0.62
        _FoamAmount   ("Quantite d'ecume", Range(0,1)) = 0.55

        _FlowDir      ("Direction du courant (xz)", Vector) = (0.9587, 0.2839, 0, 0)
        _FlowSpeed    ("Vitesse du courant (m/s)", Float) = 1.5
        _WaveScale    ("Echelle des vagues", Float) = 0.22
        _Stretch      ("Etirement transversal", Float) = 3.0
        _WaveHeight   ("Hauteur des vagues", Float) = 0.35
        _NormalStrength ("Force des normales", Float) = 1.6

        _SpecPower    ("Durete du speculaire", Float) = 48
        _SpecStrength ("Force du speculaire", Range(0,4)) = 1.4
        _Fresnel      ("Fresnel", Range(0,4)) = 1.2
        [Toggle] _UseUVFlow ("Courant depuis les UV du maillage", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float  _DepthRange;
                float4 _FoamColor;
                float  _FoamEdge;
                float  _FoamCutoff;
                float  _FoamAmount;
                float4 _FlowDir;
                float  _FlowSpeed;
                float  _WaveScale;
                float  _Stretch;
                float  _WaveHeight;
                float  _NormalStrength;
                float  _SpecPower;
                float  _SpecStrength;
                float  _Fresnel;
                float  _UseUVFlow;
            CBUFFER_END

            float hash21(float2 p)
            {
                // Repli des coordonnees sur une periode de 256 cellules : au-dela de
                // quelques centaines de metres, frac() sur de grands nombres perd sa
                // precision et le bruit se casse en blocs. Le repli est sans couture.
                p = p - floor(p / 256.0) * 256.0;
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                // interpolation quintique : continue en courbure, la grille ne se voit plus
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                return vnoise(p) * 0.50
                     + vnoise(p * 2.03 + 13.7) * 0.26
                     + vnoise(p * 4.11 + 7.10) * 0.15
                     + vnoise(p * 8.07 + 21.3) * 0.09;
            }

            // Repere lie au courant, en metres : x = travers, y = amont-aval.
            // Depuis les UV du maillage (riviere generee, suit les virages) ou
            // depuis une direction monde fixe (troncon droit).
            float2 FlowBase(float3 posWS, float2 uv)
            {
                if (_UseUVFlow > 0.5) return uv;

                float2 d = normalize(_FlowDir.xy);
                return float2(dot(posWS.xz, float2(-d.y, d.x)), dot(posWS.xz, d));
            }

            float WaveFieldAt(float2 b)
            {
                // Le defilement est multiplie par _WaveScale comme la coordonnee d'espace :
                // _FlowSpeed est donc directement une vitesse en metres par seconde, et
                // changer la taille des vagues ne change plus la vitesse du courant.
                float scroll = _Time.y * _FlowSpeed * _WaveScale;

                float2 c1 = float2(b.x * _WaveScale * _Stretch,
                                   b.y * _WaveScale - scroll);
                float2 c2 = float2(b.x * _WaveScale * _Stretch,
                                   b.y * _WaveScale - scroll * 1.7) * 2.3 + 31.0;
                return fbm(c1) * 0.65 + fbm(c2) * 0.35;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
                float2 flowBase   : TEXCOORD2;
                float  fogCoord   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float2 base = FlowBase(posWS, IN.uv);
                posWS.y += (WaveFieldAt(base) - 0.5) * _WaveHeight;
                OUT.flowBase = base;
                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.screenPos  = ComputeScreenPos(OUT.positionCS);
                OUT.fogCoord   = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 suv = IN.screenPos.xy / max(IN.screenPos.w, 1e-5);

                float sceneEye = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float surfEye  = IN.screenPos.w;
                float waterDepth = max(sceneEye - surfEye, 0.0);

                // normale reconstruite depuis le champ de vagues
                const float e = 0.35;
                float hC = WaveFieldAt(IN.flowBase);
                float hX = WaveFieldAt(IN.flowBase + float2(e, 0));
                float hZ = WaveFieldAt(IN.flowBase + float2(0, e));
                float3 nrm = normalize(float3((hC - hX) * _NormalStrength, 1.0, (hC - hZ) * _NormalStrength));

                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                Light main = GetMainLight();

                float3 body = lerp(_ShallowColor.rgb, _DeepColor.rgb, saturate(waterDepth / max(_DepthRange, 0.01)));
                float  alpha = lerp(_ShallowColor.a, _DeepColor.a, saturate(waterDepth / max(_DepthRange, 0.01)));

                // ecume de rive : la ou le fond remonte vers la surface
                float edgeFoam = 1.0 - saturate(waterDepth / max(_FoamEdge, 0.01));
                edgeFoam = edgeFoam * edgeFoam;

                // ecume de courant : cretes de la turbulence
                float crest = WaveFieldAt(IN.flowBase * 1.6);
                float streak = smoothstep(_FoamCutoff, _FoamCutoff + 0.30, crest) * _FoamAmount;

                float foam = saturate(edgeFoam + streak);

                float ndl = saturate(dot(nrm, main.direction));
                float3 lit = body * (0.45 + 0.55 * ndl) * main.color;

                float3 h = normalize(main.direction + viewDir);
                float spec = pow(saturate(dot(nrm, h)), _SpecPower) * _SpecStrength;

                float fres = pow(1.0 - saturate(dot(nrm, viewDir)), 3.0) * _Fresnel;

                float3 col = lerp(lit, _FoamColor.rgb, foam);
                col += main.color * spec * (1.0 - foam * 0.6);
                col += _ShallowColor.rgb * fres * 0.35;
                col += SampleSH(nrm) * body * 0.35;

                alpha = saturate(alpha + foam * 0.8 + fres * 0.2);

                // Sans ca, la riviere reste nette a 400 m et traverse la tempete de neige.
                col = MixFog(col, IN.fogCoord);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
