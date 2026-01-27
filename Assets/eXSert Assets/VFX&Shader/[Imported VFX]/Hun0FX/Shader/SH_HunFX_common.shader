// Made with Amplify Shader Editor v1.9.3.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "HunFX/SH_HunFX_common"
{
	Properties
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		[HDR]_color1("color1", Color) = (1,1,1,1)
		[HDR]_color2("color2", Color) = (0,0,0,1)
		[Toggle(_USEDISSOLVECOLOR_ON)] _UseDissolveColor("UseDissolveColor", Float) = 0
		_DissolveColor_step("DissolveColor_step", Vector) = (0,1,0,0)
		[Toggle(_USECOLOR_ON)] _UseColor("UseColor", Float) = 1
		_ColorIntensity("ColorIntensity", Float) = 1
		_MainTexture("MainTexture", 2D) = "white" {}
		[Toggle]_MainTex_UV_invert("MainTex_UV_invert", Float) = 0
		_MainTex_uv("MainTex_uv", Vector) = (0,0,1,1)
		_MainTex_speed("MainTex_speed", Vector) = (0,0,0,0)
		_DissolveTex("DissolveTex", 2D) = "white" {}
		_DissolveTex_uv("DissolveTex_uv", Vector) = (0,0,1,1)
		_Smoothstep("Smoothstep", Vector) = (0,1,0,0)
		[Toggle]_DissTex_UV_invert("DissTex_UV_invert", Float) = 0
		_DissolveTex_speed("DissolveTex_speed", Vector) = (0,0,0,0)
		_VOTex("VOTex", 2D) = "white" {}
		_VOTex_uv("VOTex_uv", Vector) = (0,0,1,1)
		_VOTex_speed("VOTex_speed", Vector) = (0,0,0,0)
		_VOintensity("VOintensity", Float) = 0
		_DistTex("DistTex", 2D) = "white" {}
		_DistTex_uv_speed("DistTex_uv_speed", Vector) = (1,1,0,0)
		_Distort_intensity("Distort_intensity", Range( 0 , 0.5)) = 0
		[Toggle(_USEFRESNEL_ON)] _UseFresnel("UseFresnel", Float) = 0
		_Fresnel_pow("Fresnel_pow", Float) = 5
		[Toggle]_MaskTex_UV_invert("MaskTex_UV_invert", Float) = 0
		_Mask("Mask", 2D) = "white" {}
		_MaskMult("MaskMult", Float) = 1
		_Mask_udrl("Mask_udrl", Vector) = (0,0,0,0)
		[Toggle(_USEPOSXSCROLL_ON)] _UsePosXScroll("UsePosXScroll", Float) = 0
		_SubDissolveTex("SubDissolveTex", 2D) = "white" {}
		[Toggle]_UseSubDissTex("UseSubDissTex", Float) = 0
		_SubDissolveTex_multi("SubDissolveTex_multi", Float) = 1
		_SubDissolveTex_add("SubDissolveTex_add", Float) = 0
		[Toggle(_USEDEPTHFADE_ON)] _UseDepthFade("UseDepthFade", Float) = 0
		_DepthFade_Distance("DepthFade_Distance", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

	}


	Category 
	{
		SubShader
		{
		LOD 0

			Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMask RGB
			Cull Off
			Lighting Off 
			ZWrite Off
			ZTest LEqual
			
			Pass {
			
				CGPROGRAM
				
				#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
				#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
				#endif
				
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#pragma multi_compile_instancing
				#pragma multi_compile_particles
				#pragma multi_compile_fog
				#include "UnityShaderVariables.cginc"
				#define ASE_NEEDS_FRAG_COLOR
				#pragma shader_feature_local _USECOLOR_ON
				#pragma shader_feature_local _USEPOSXSCROLL_ON
				#pragma shader_feature_local _USEDISSOLVECOLOR_ON
				#pragma shader_feature_local _USEDEPTHFADE_ON
				#pragma shader_feature_local _USEFRESNEL_ON


				#include "UnityCG.cginc"

				struct appdata_t 
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					float3 ase_normal : NORMAL;
					float4 ase_texcoord1 : TEXCOORD1;
				};

				struct v2f 
				{
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD2;
					#endif
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
					float4 ase_texcoord3 : TEXCOORD3;
					float4 ase_texcoord4 : TEXCOORD4;
					float4 ase_texcoord5 : TEXCOORD5;
					float4 ase_texcoord6 : TEXCOORD6;
				};
				
				
				#if UNITY_VERSION >= 560
				UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
				#else
				uniform sampler2D_float _CameraDepthTexture;
				#endif

				//Don't delete this comment
				// uniform sampler2D_float _CameraDepthTexture;

				uniform sampler2D _MainTex;
				uniform fixed4 _TintColor;
				uniform float4 _MainTex_ST;
				uniform float _InvFade;
				uniform sampler2D _VOTex;
				uniform float2 _VOTex_speed;
				uniform float4 _VOTex_uv;
				uniform float _VOintensity;
				uniform sampler2D _MainTexture;
				uniform float _MainTex_UV_invert;
				uniform float2 _MainTex_speed;
				uniform sampler2D _DistTex;
				uniform float4 _DistTex_uv_speed;
				uniform float _Distort_intensity;
				uniform float4 _MainTex_uv;
				uniform float4 _color2;
				uniform float4 _color1;
				uniform float2 _DissolveColor_step;
				uniform sampler2D _DissolveTex;
				uniform float _DissTex_UV_invert;
				uniform float2 _DissolveTex_speed;
				uniform float4 _DissolveTex_uv;
				uniform float _UseSubDissTex;
				uniform sampler2D _SubDissolveTex;
				uniform float4 _SubDissolveTex_ST;
				uniform float _SubDissolveTex_multi;
				uniform float _SubDissolveTex_add;
				uniform float _ColorIntensity;
				uniform float2 _Smoothstep;
				uniform float _Fresnel_pow;
				uniform sampler2D _Mask;
				uniform float _MaskTex_UV_invert;
				uniform float _MaskMult;
				uniform float4 _CameraDepthTexture_TexelSize;
				uniform float _DepthFade_Distance;
				uniform float4 _Mask_udrl;


				v2f vert ( appdata_t v  )
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					float2 texCoord26 = v.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 appendResult28 = (float2(_VOTex_uv.x , _VOTex_uv.y));
					float2 appendResult29 = (float2(_VOTex_uv.z , _VOTex_uv.w));
					float2 panner22 = ( 1.0 * _Time.y * _VOTex_speed + ( ( texCoord26 + appendResult28 ) * appendResult29 ));
					
					float3 ase_worldPos = mul(unity_ObjectToWorld, float4( (v.vertex).xyz, 1 )).xyz;
					o.ase_texcoord4.xyz = ase_worldPos;
					float3 ase_worldNormal = UnityObjectToWorldNormal(v.ase_normal);
					o.ase_texcoord5.xyz = ase_worldNormal;
					float4 ase_clipPos = UnityObjectToClipPos(v.vertex);
					float4 screenPos = ComputeScreenPos(ase_clipPos);
					o.ase_texcoord6 = screenPos;
					
					o.ase_texcoord3 = v.ase_texcoord1;
					
					//setting value to unused interpolator channels and avoid initialization warnings
					o.ase_texcoord4.w = 0;
					o.ase_texcoord5.w = 0;

					v.vertex.xyz += ( ( tex2Dlod( _VOTex, float4( panner22, 0, 0.0) ).r * v.ase_normal ) * _VOintensity );
					o.vertex = UnityObjectToClipPos(v.vertex);
					#ifdef SOFTPARTICLES_ON
						o.projPos = ComputeScreenPos (o.vertex);
						COMPUTE_EYEDEPTH(o.projPos.z);
					#endif
					o.color = v.color;
					o.texcoord = v.texcoord;
					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}

				fixed4 frag ( v2f i  ) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( i );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( i );

					#ifdef SOFTPARTICLES_ON
						float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
						float partZ = i.projPos.z;
						float fade = saturate (_InvFade * (sceneZ-partZ));
						i.color.a *= fade;
					#endif

					float2 texCoord13 = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 appendResult100 = (float2(_DistTex_uv_speed.z , _DistTex_uv_speed.w));
					float2 texCoord96 = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 appendResult98 = (float2(_DistTex_uv_speed.x , _DistTex_uv_speed.y));
					float2 panner101 = ( 1.0 * _Time.y * appendResult100 + ( texCoord96 * appendResult98 ));
					float temp_output_104_0 = ( tex2D( _DistTex, panner101 ).r * _Distort_intensity );
					float4 texCoord59 = i.ase_texcoord3;
					texCoord59.xy = i.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
					float2 appendResult63 = (float2(0.0 , texCoord59.y));
					float2 appendResult62 = (float2(texCoord59.y , 0.0));
					#ifdef _USEPOSXSCROLL_ON
					float2 staticSwitch60 = appendResult62;
					#else
					float2 staticSwitch60 = appendResult63;
					#endif
					float2 appendResult18 = (float2(_MainTex_uv.x , _MainTex_uv.y));
					float2 appendResult19 = (float2(_MainTex_uv.z , _MainTex_uv.w));
					float2 panner15 = ( 1.0 * _Time.y * _MainTex_speed + ( ( ( ( texCoord13 + temp_output_104_0 ) + staticSwitch60 ) + appendResult18 ) * appendResult19 ));
					float2 break87 = panner15;
					float2 appendResult90 = (float2(break87.y , break87.x));
					float4 tex2DNode1 = tex2D( _MainTexture, (( _MainTex_UV_invert )?( appendResult90 ):( panner15 )) );
					float smoothstepResult121 = smoothstep( _DissolveColor_step.x , _DissolveColor_step.y , tex2DNode1.r);
					float2 texCoord35 = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 appendResult37 = (float2(_DissolveTex_uv.x , _DissolveTex_uv.y));
					float2 appendResult38 = (float2(_DissolveTex_uv.z , _DissolveTex_uv.w));
					float2 panner31 = ( 1.0 * _Time.y * _DissolveTex_speed + ( ( ( staticSwitch60 + ( texCoord35 + temp_output_104_0 ) ) + appendResult37 ) * appendResult38 ));
					float2 break91 = panner31;
					float2 appendResult92 = (float2(break91.y , break91.x));
					float2 uv_SubDissolveTex = i.texcoord.xy * _SubDissolveTex_ST.xy + _SubDissolveTex_ST.zw;
					float temp_output_67_0 = ( tex2D( _DissolveTex, (( _DissTex_UV_invert )?( appendResult92 ):( panner31 )) ).g - (( _UseSubDissTex )?( ( ( tex2D( _SubDissolveTex, uv_SubDissolveTex ).r * _SubDissolveTex_multi ) + _SubDissolveTex_add ) ):( 0.0 )) );
					float smoothstepResult118 = smoothstep( _DissolveColor_step.x , _DissolveColor_step.y , temp_output_67_0);
					#ifdef _USEDISSOLVECOLOR_ON
					float staticSwitch119 = smoothstepResult118;
					#else
					float staticSwitch119 = smoothstepResult121;
					#endif
					float4 lerpResult9 = lerp( _color2 , _color1 , staticSwitch119);
					#ifdef _USECOLOR_ON
					float4 staticSwitch73 = lerpResult9;
					#else
					float4 staticSwitch73 = tex2DNode1;
					#endif
					float4 texCoord14 = i.ase_texcoord3;
					texCoord14.xy = i.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
					float4 break6 = ( ( staticSwitch73 * ( _ColorIntensity + texCoord14.z ) ) * i.color );
					float smoothstepResult40 = smoothstep( _Smoothstep.x , _Smoothstep.y , ( temp_output_67_0 - texCoord14.x ));
					float temp_output_7_0 = ( tex2DNode1.a * ( i.color.a * smoothstepResult40 ) );
					float3 ase_worldPos = i.ase_texcoord4.xyz;
					float3 ase_worldViewDir = UnityWorldSpaceViewDir(ase_worldPos);
					ase_worldViewDir = normalize(ase_worldViewDir);
					float3 ase_worldNormal = i.ase_texcoord5.xyz;
					float fresnelNdotV47 = dot( ase_worldNormal, ase_worldViewDir );
					float fresnelNode47 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV47, _Fresnel_pow ) );
					float clampResult110 = clamp( fresnelNode47 , 0.0 , 1.0 );
					#ifdef _USEFRESNEL_ON
					float staticSwitch48 = ( temp_output_7_0 * clampResult110 );
					#else
					float staticSwitch48 = temp_output_7_0;
					#endif
					float2 texCoord117 = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 break114 = texCoord117;
					float2 appendResult115 = (float2(break114.y , break114.x));
					float temp_output_53_0 = ( staticSwitch48 * saturate( ( tex2D( _Mask, (( _MaskTex_UV_invert )?( appendResult115 ):( texCoord117 )) ).b * _MaskMult ) ) );
					float4 screenPos = i.ase_texcoord6;
					float4 ase_screenPosNorm = screenPos / screenPos.w;
					ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
					float screenDepth122 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
					float distanceDepth122 = abs( ( screenDepth122 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _DepthFade_Distance ) );
					float clampResult126 = clamp( distanceDepth122 , 0.0 , 1.0 );
					#ifdef _USEDEPTHFADE_ON
					float staticSwitch123 = ( clampResult126 * temp_output_53_0 );
					#else
					float staticSwitch123 = temp_output_53_0;
					#endif
					float2 texCoord74 = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float smoothstepResult75 = smoothstep( 0.0 , _Mask_udrl.y , texCoord74.y);
					float smoothstepResult77 = smoothstep( 0.0 , _Mask_udrl.x , ( 1.0 - texCoord74.y ));
					float smoothstepResult79 = smoothstep( 0.0 , _Mask_udrl.w , texCoord74.x);
					float smoothstepResult80 = smoothstep( 0.0 , _Mask_udrl.z , ( 1.0 - texCoord74.x ));
					float4 appendResult8 = (float4(break6.r , break6.g , break6.b , ( staticSwitch123 * ( ( smoothstepResult75 * smoothstepResult77 ) * ( smoothstepResult79 * smoothstepResult80 ) ) )));
					

					fixed4 col = appendResult8;
					UNITY_APPLY_FOG(i.fogCoord, col);
					return col;
				}
				ENDCG 
			}
		}	
	}
	CustomEditor "ASEMaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19303
Node;AmplifyShaderEditor.Vector4Node;97;-4400,848;Inherit;False;Property;_DistTex_uv_speed;DistTex_uv_speed;20;0;Create;True;0;0;0;False;0;False;1,1,0,0;1,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;96;-4496,672;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;98;-4128,880;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;99;-4192,672;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;100;-3872,928;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;101;-4016,672;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;61;-3264,448;Inherit;False;Constant;_Float0;Float 0;17;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;95;-3792,672;Inherit;True;Property;_DistTex;DistTex;19;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;103;-3632,912;Inherit;False;Property;_Distort_intensity;Distort_intensity;21;0;Create;True;0;0;0;False;0;False;0;0;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;59;-3376,256;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;62;-3024,320;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;63;-3008,448;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;35;-3088,592;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;104;-3440,704;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;36;-2624,688;Inherit;False;Property;_DissolveTex_uv;DissolveTex_uv;11;0;Create;True;0;0;0;False;0;False;0,0,1,1;0,0,1,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;60;-2816,336;Inherit;False;Property;_UsePosXScroll;UsePosXScroll;28;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;107;-2772.888,572.155;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;37;-2432,688;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;64;-2528,560;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;34;-2368,560;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;38;-2400,800;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;13;-3104,-112;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;-2224,560;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;39;-2224,768;Inherit;False;Property;_DissolveTex_speed;DissolveTex_speed;14;0;Create;True;0;0;0;False;0;False;0,0;-1,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector4Node;17;-2528,48;Inherit;False;Property;_MainTex_uv;MainTex_uv;8;0;Create;True;0;0;0;False;0;False;0,0,1,1;0,0,1,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;106;-2841.788,-96.04502;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;31;-2016,624;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;18;-2336,48;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;86;-2560,-80;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;65;-1536,976;Inherit;True;Property;_SubDissolveTex;SubDissolveTex;29;0;Create;True;0;0;0;False;0;False;-1;None;571d0c03d9666554097d8b3e0484e9b8;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;68;-1536,1200;Inherit;False;Property;_SubDissolveTex_multi;SubDissolveTex_multi;31;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;91;-1744,800;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleAddOpNode;20;-2272,-80;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;19;-2304,160;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;69;-1200,1056;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;71;-1232,1200;Inherit;False;Property;_SubDissolveTex_add;SubDissolveTex_add;32;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;92;-1632,800;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-2096,-96;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;16;-2128,128;Inherit;False;Property;_MainTex_speed;MainTex_speed;9;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;70;-1024.716,1079.287;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;94;-1600,608;Inherit;False;Property;_DissTex_UV_invert;DissTex_UV_invert;13;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;15;-1920,-16;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;32;-1280,624;Inherit;True;Property;_DissolveTex;DissolveTex;10;0;Create;True;0;0;0;False;0;False;-1;None;9d667e83ca8a1b44e97e8b4624ba905c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;66;-944,880;Inherit;False;Property;_UseSubDissTex;UseSubDissTex;30;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;87;-1686.374,113.0622;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleSubtractOpNode;67;-720,640;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;117;400,1056;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;14;-592,896;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;90;-1568,112;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;50;-304,640;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;114;704,1264;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.Vector2Node;41;-160,768;Inherit;False;Property;_Smoothstep;Smoothstep;12;0;Create;True;0;0;0;False;0;False;0,1;0,0.1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ToggleSwitchNode;89;-1552,-112;Inherit;False;Property;_MainTex_UV_invert;MainTex_UV_invert;7;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.VertexColorNode;4;-464,272;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SmoothstepOpNode;40;-160,640;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;108;112,880;Inherit;False;Property;_Fresnel_pow;Fresnel_pow;23;0;Create;True;0;0;0;False;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;115;832,1264;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;120;-1504,-544;Inherit;False;Property;_DissolveColor_step;DissolveColor_step;3;0;Create;True;0;0;0;False;0;False;0,1;0,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;1;-1280,-32;Inherit;True;Property;_MainTexture;MainTexture;6;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;42;-64,352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;47;288,752;Inherit;True;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;116;848,1040;Inherit;False;Property;_MaskTex_UV_invert;MaskTex_UV_invert;24;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;27;944,1680;Inherit;False;Property;_VOTex_uv;VOTex_uv;16;0;Create;True;0;0;0;False;0;False;0,0,1,1;0,0,1,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SmoothstepOpNode;118;-1216,-560;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;121;-1216,-400;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;128,240;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;110;592,752;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;111;864,704;Inherit;False;Property;_MaskMult;MaskMult;26;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;52;784,496;Inherit;True;Property;_Mask;Mask;25;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;28;1136,1680;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;26;960,1552;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;74;1216,704;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;12;-1168,-800;Inherit;False;Property;_color2;color2;1;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,1;0,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;11;-1168,-976;Inherit;False;Property;_color1;color1;0;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;2,2,2,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;448,352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;112;1104,560;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;125;624,80;Inherit;False;Property;_DepthFade_Distance;DepthFade_Distance;34;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;119;-944,-560;Inherit;False;Property;_UseDissolveColor;UseDissolveColor;2;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;25;1200,1552;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;29;1168,1792;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;76;1472,736;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;81;1488,976;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;78;1232,848;Inherit;False;Property;_Mask_udrl;Mask_udrl;27;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0.5,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;9;-816,-832;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;48;656,240;Inherit;True;Property;_UseFresnel;UseFresnel;22;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;113;1296,560;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;122;848,64;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;2;-720,-64;Inherit;False;Property;_ColorIntensity;ColorIntensity;5;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;1376,1536;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;30;1344,1760;Inherit;False;Property;_VOTex_speed;VOTex_speed;17;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SmoothstepOpNode;75;1632,544;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;77;1632,688;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;79;1637.608,826.8312;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;80;1637.608,970.8312;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;53;1040,240;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;126;1136,80;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;127;-512,32;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;73;-432,-384;Inherit;False;Property;_UseColor;UseColor;4;0;Create;True;0;0;0;False;0;False;0;1;1;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-286,-12;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PannerNode;22;1552,1616;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;82;1856,608;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;83;1888,912;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;124;1296,112;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;32,0;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;23;1760,1600;Inherit;True;Property;_VOTex;VOTex;15;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NormalVertexDataNode;45;1792,1824;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;84;2076.603,749.1683;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;123;1360,272;Inherit;False;Property;_UseDepthFade;UseDepthFade;33;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;6;1648,-32;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;43;2016,1952;Inherit;False;Property;_VOintensity;VOintensity;18;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;2080,1680;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;85;1646.618,245.2945;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;8;1808,16;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;2304,1648;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;2160,16;Float;False;True;-1;2;ASEMaterialInspector;0;11;HunFX/SH_HunFX_common;0b6a9f8b4f707c74ca64c0be8e590de0;True;SubShader 0 Pass 0;0;0;SubShader 0 Pass 0;2;False;True;2;5;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;True;True;2;False;;False;True;True;True;True;False;0;False;;False;False;False;False;False;False;False;False;False;True;2;False;;True;3;False;;False;True;4;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;98;0;97;1
WireConnection;98;1;97;2
WireConnection;99;0;96;0
WireConnection;99;1;98;0
WireConnection;100;0;97;3
WireConnection;100;1;97;4
WireConnection;101;0;99;0
WireConnection;101;2;100;0
WireConnection;95;1;101;0
WireConnection;62;0;59;2
WireConnection;62;1;61;0
WireConnection;63;0;61;0
WireConnection;63;1;59;2
WireConnection;104;0;95;1
WireConnection;104;1;103;0
WireConnection;60;1;63;0
WireConnection;60;0;62;0
WireConnection;107;0;35;0
WireConnection;107;1;104;0
WireConnection;37;0;36;1
WireConnection;37;1;36;2
WireConnection;64;0;60;0
WireConnection;64;1;107;0
WireConnection;34;0;64;0
WireConnection;34;1;37;0
WireConnection;38;0;36;3
WireConnection;38;1;36;4
WireConnection;33;0;34;0
WireConnection;33;1;38;0
WireConnection;106;0;13;0
WireConnection;106;1;104;0
WireConnection;31;0;33;0
WireConnection;31;2;39;0
WireConnection;18;0;17;1
WireConnection;18;1;17;2
WireConnection;86;0;106;0
WireConnection;86;1;60;0
WireConnection;91;0;31;0
WireConnection;20;0;86;0
WireConnection;20;1;18;0
WireConnection;19;0;17;3
WireConnection;19;1;17;4
WireConnection;69;0;65;1
WireConnection;69;1;68;0
WireConnection;92;0;91;1
WireConnection;92;1;91;0
WireConnection;21;0;20;0
WireConnection;21;1;19;0
WireConnection;70;0;69;0
WireConnection;70;1;71;0
WireConnection;94;0;31;0
WireConnection;94;1;92;0
WireConnection;15;0;21;0
WireConnection;15;2;16;0
WireConnection;32;1;94;0
WireConnection;66;1;70;0
WireConnection;87;0;15;0
WireConnection;67;0;32;2
WireConnection;67;1;66;0
WireConnection;90;0;87;1
WireConnection;90;1;87;0
WireConnection;50;0;67;0
WireConnection;50;1;14;1
WireConnection;114;0;117;0
WireConnection;89;0;15;0
WireConnection;89;1;90;0
WireConnection;40;0;50;0
WireConnection;40;1;41;1
WireConnection;40;2;41;2
WireConnection;115;0;114;1
WireConnection;115;1;114;0
WireConnection;1;1;89;0
WireConnection;42;0;4;4
WireConnection;42;1;40;0
WireConnection;47;3;108;0
WireConnection;116;0;117;0
WireConnection;116;1;115;0
WireConnection;118;0;67;0
WireConnection;118;1;120;1
WireConnection;118;2;120;2
WireConnection;121;0;1;1
WireConnection;121;1;120;1
WireConnection;121;2;120;2
WireConnection;7;0;1;4
WireConnection;7;1;42;0
WireConnection;110;0;47;0
WireConnection;52;1;116;0
WireConnection;28;0;27;1
WireConnection;28;1;27;2
WireConnection;49;0;7;0
WireConnection;49;1;110;0
WireConnection;112;0;52;3
WireConnection;112;1;111;0
WireConnection;119;1;121;0
WireConnection;119;0;118;0
WireConnection;25;0;26;0
WireConnection;25;1;28;0
WireConnection;29;0;27;3
WireConnection;29;1;27;4
WireConnection;76;0;74;2
WireConnection;81;0;74;1
WireConnection;9;0;12;0
WireConnection;9;1;11;0
WireConnection;9;2;119;0
WireConnection;48;1;7;0
WireConnection;48;0;49;0
WireConnection;113;0;112;0
WireConnection;122;0;125;0
WireConnection;24;0;25;0
WireConnection;24;1;29;0
WireConnection;75;0;74;2
WireConnection;75;2;78;2
WireConnection;77;0;76;0
WireConnection;77;2;78;1
WireConnection;79;0;74;1
WireConnection;79;2;78;4
WireConnection;80;0;81;0
WireConnection;80;2;78;3
WireConnection;53;0;48;0
WireConnection;53;1;113;0
WireConnection;126;0;122;0
WireConnection;127;0;2;0
WireConnection;127;1;14;3
WireConnection;73;1;1;0
WireConnection;73;0;9;0
WireConnection;3;0;73;0
WireConnection;3;1;127;0
WireConnection;22;0;24;0
WireConnection;22;2;30;0
WireConnection;82;0;75;0
WireConnection;82;1;77;0
WireConnection;83;0;79;0
WireConnection;83;1;80;0
WireConnection;124;0;126;0
WireConnection;124;1;53;0
WireConnection;5;0;3;0
WireConnection;5;1;4;0
WireConnection;23;1;22;0
WireConnection;84;0;82;0
WireConnection;84;1;83;0
WireConnection;123;1;53;0
WireConnection;123;0;124;0
WireConnection;6;0;5;0
WireConnection;46;0;23;1
WireConnection;46;1;45;0
WireConnection;85;0;123;0
WireConnection;85;1;84;0
WireConnection;8;0;6;0
WireConnection;8;1;6;1
WireConnection;8;2;6;2
WireConnection;8;3;85;0
WireConnection;44;0;46;0
WireConnection;44;1;43;0
WireConnection;0;0;8;0
WireConnection;0;1;44;0
ASEEND*/
//CHKSM=DF8FBC3E94360E83272F8579BB87266C24D4B34C