Shader "VelvetLoto/Velvet" {
 Properties { _Color("Dye",Color)=(.3,.03,.06,1) _MainTex("Pile",2D)="white"{} }
 SubShader { Tags {"RenderType"="Opaque"} LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 sampler2D _MainTex;fixed4 _Color;
 struct Input {float2 uv_MainTex;float3 viewDir;};
 void surf(Input IN,inout SurfaceOutputStandard o){fixed3 grain=tex2D(_MainTex,IN.uv_MainTex*3).rgb;float rim=pow(1-saturate(dot(normalize(IN.viewDir),float3(0,0,1))),3);o.Albedo=_Color.rgb*(.8+grain.r*.4);o.Smoothness=.18;o.Metallic=0;o.Emission=_Color.rgb*rim*.28;}
 ENDCG
 } Fallback "Standard"
}
