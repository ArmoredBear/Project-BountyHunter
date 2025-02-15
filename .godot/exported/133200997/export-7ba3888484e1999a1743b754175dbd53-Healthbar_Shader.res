RSRC                    VisualShader            ��������                                            V      resource_local_to_scene    resource_name    output_port_for_preview    default_input_values    expanded_output_ports    linked_parent_graph_frame    parameter_name 
   qualifier    texture_type    color_default    texture_filter    texture_repeat    texture_source    script    source    texture 	   function    input_name    op_type 	   operator    default_value_enabled    default_value    hint    min    max    step    code    graph_offset    mode    modes/blend    flags/skip_vertex_transform    flags/unshaded    flags/light_only    flags/world_vertex_coords    nodes/vertex/0/position    nodes/vertex/connections    nodes/fragment/0/position    nodes/fragment/2/node    nodes/fragment/2/position    nodes/fragment/3/node    nodes/fragment/3/position    nodes/fragment/4/node    nodes/fragment/4/position    nodes/fragment/5/node    nodes/fragment/5/position    nodes/fragment/6/node    nodes/fragment/6/position    nodes/fragment/8/node    nodes/fragment/8/position    nodes/fragment/10/node    nodes/fragment/10/position    nodes/fragment/11/node    nodes/fragment/11/position    nodes/fragment/12/node    nodes/fragment/12/position    nodes/fragment/13/node    nodes/fragment/13/position    nodes/fragment/14/node    nodes/fragment/14/position    nodes/fragment/15/node    nodes/fragment/15/position    nodes/fragment/16/node    nodes/fragment/16/position    nodes/fragment/17/node    nodes/fragment/17/position    nodes/fragment/18/node    nodes/fragment/18/position    nodes/fragment/19/node    nodes/fragment/19/position    nodes/fragment/connections    nodes/light/0/position    nodes/light/connections    nodes/start/0/position    nodes/start/connections    nodes/process/0/position    nodes/process/connections    nodes/collide/0/position    nodes/collide/connections    nodes/start_custom/0/position    nodes/start_custom/connections     nodes/process_custom/0/position !   nodes/process_custom/connections    nodes/sky/0/position    nodes/sky/connections    nodes/fog/0/position    nodes/fog/connections        1   local://VisualShaderNodeTexture2DParameter_4lfoq       &   local://VisualShaderNodeTexture_xifm2 �      %   local://VisualShaderNodeUVFunc_tn154 �      $   local://VisualShaderNodeInput_yegfd �      '   local://VisualShaderNodeVectorOp_twigj +      )   local://VisualShaderNodeSmoothStep_lnq3k �      $   local://VisualShaderNodeInput_vw2ko �      '   local://VisualShaderNodeVectorOp_4p8cd       ,   local://VisualShaderNodeVec2Parameter_wmw3t 8      -   local://VisualShaderNodeFloatParameter_vg6lr �      &   local://VisualShaderNodeFloatOp_feift       '   local://VisualShaderNodeVectorOp_3bvxj O      )   local://VisualShaderNodeVectorFunc_7lywd �      $   local://VisualShaderNodeInput_u3jth C      $   local://VisualShaderNodeInput_ivfl5 x      &   local://VisualShaderNodeColorOp_tbr1n �      +   res://Common/Shaders/Healthbar_Shader.tres �      #   VisualShaderNodeTexture2DParameter             Basic_Texture          
                           VisualShaderNodeTexture                               VisualShaderNodeUVFunc             VisualShaderNodeInput             time          VisualShaderNodeVectorOp                    
                 
     �?                                VisualShaderNodeSmoothStep             VisualShaderNodeInput             color          VisualShaderNodeVectorOp                      VisualShaderNodeVec2Parameter             Move_Speed             
     �?             VisualShaderNodeFloatParameter             Fade_Speed                   �         A                  �         VisualShaderNodeFloatOp                      VisualShaderNodeVectorOp                    
                 
                                       VisualShaderNodeVectorFunc                                                                                  VisualShaderNodeInput             uv          VisualShaderNodeInput             time          VisualShaderNodeColorOp                      VisualShader (         �  shader_type canvas_item;
render_mode blend_add;

uniform vec2 Move_Speed = vec2(1.000000, 0.000000);
uniform sampler2D Basic_Texture : source_color, filter_nearest, repeat_enable;
uniform float Fade_Speed : hint_range(-10, 10) = -2;



void fragment() {
// Input:10
	vec4 n_out10p0 = COLOR;


// Input:5
	float n_out5p0 = TIME;


// Vector2Parameter:12
	vec2 n_out12p0 = Move_Speed;


// VectorOp:6
	vec2 n_out6p0 = vec2(n_out5p0) * n_out12p0;


// UVFunc:4
	vec2 n_in4p1 = vec2(1.00000, 1.00000);
	vec2 n_out4p0 = n_out6p0 * n_in4p1 + UV;


	vec4 n_out3p0;
// Texture2D:3
	n_out3p0 = texture(Basic_Texture, n_out4p0);


// SmoothStep:8
	float n_in8p0 = 0.00000;
	float n_in8p1 = 1.00000;
	float n_out8p0 = smoothstep(n_in8p0, n_in8p1, n_out3p0.x);


// VectorOp:11
	vec3 n_out11p0 = vec3(n_out10p0.xyz) * vec3(n_out8p0);


// Input:17
	vec2 n_out17p0 = UV;


// Input:18
	float n_out18p0 = TIME;


// FloatParameter:13
	float n_out13p0 = Fade_Speed;


// FloatOp:14
	float n_out14p0 = n_out18p0 * n_out13p0;


// VectorOp:15
	vec2 n_out15p0 = n_out17p0 - vec2(n_out14p0);


// VectorFunc:16
	vec4 n_out16p0 = tan(vec4(n_out15p0, 0.0, 0.0));
	float n_out16p1 = n_out16p0.r;
	float n_out16p2 = n_out16p0.g;


	vec3 n_out19p0;
// ColorOp:19
	{
		float base = vec3(n_out16p1).x;
		float blend = vec3(n_out16p2).x;
		if (base < 0.5) {
			n_out19p0.x = (base * (2.0 * blend));
		} else {
			n_out19p0.x = (1.0 - (1.0 - base) * (1.0 - 2.0 * (blend - 0.5)));
		}
	}
	{
		float base = vec3(n_out16p1).y;
		float blend = vec3(n_out16p2).y;
		if (base < 0.5) {
			n_out19p0.y = (base * (2.0 * blend));
		} else {
			n_out19p0.y = (1.0 - (1.0 - base) * (1.0 - 2.0 * (blend - 0.5)));
		}
	}
	{
		float base = vec3(n_out16p1).z;
		float blend = vec3(n_out16p2).z;
		if (base < 0.5) {
			n_out19p0.z = (base * (2.0 * blend));
		} else {
			n_out19p0.z = (1.0 - (1.0 - base) * (1.0 - 2.0 * (blend - 0.5)));
		}
	}


// Output:0
	COLOR.rgb = n_out11p0;
	COLOR.a = n_out19p0.x;


}
                              "   
     D  �C$   
    �E  �C%             &   
     ��  �C'            (   
     4C  �C)            *   
     ��  pB+            ,   
      �  �B-            .   
     ��  �B/            0   
     �C  �C1            2   
     4C  ��3            4   
     /D  C5            6   
     �  pC7         	   8   
      �  zD9         
   :   
     4C  �D;            <   
     �C  WD=            >   
     pD  RD?            @   
      �  CDA            B   
      �  \DC            D   
    ��D  fDE       D                                                         
                                                                                                                                                                                     RSRC