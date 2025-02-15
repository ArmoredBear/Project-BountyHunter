RSRC                    VisualShader            ��������                                            J      resource_local_to_scene    resource_name    output_port_for_preview    default_input_values    expanded_output_ports    linked_parent_graph_frame    parameter_name 
   qualifier    texture_type    color_default    texture_filter    texture_repeat    texture_source    script    source    texture 	   function    input_name    op_type 	   operator    default_value_enabled    default_value    hint    min    max    step    code    graph_offset    mode    modes/blend    flags/skip_vertex_transform    flags/unshaded    flags/light_only    flags/world_vertex_coords    nodes/vertex/0/position    nodes/vertex/connections    nodes/fragment/0/position    nodes/fragment/2/node    nodes/fragment/2/position    nodes/fragment/3/node    nodes/fragment/3/position    nodes/fragment/4/node    nodes/fragment/4/position    nodes/fragment/5/node    nodes/fragment/5/position    nodes/fragment/6/node    nodes/fragment/6/position    nodes/fragment/7/node    nodes/fragment/7/position    nodes/fragment/8/node    nodes/fragment/8/position    nodes/fragment/9/node    nodes/fragment/9/position    nodes/fragment/10/node    nodes/fragment/10/position    nodes/fragment/11/node    nodes/fragment/11/position    nodes/fragment/connections    nodes/light/0/position    nodes/light/connections    nodes/start/0/position    nodes/start/connections    nodes/process/0/position    nodes/process/connections    nodes/collide/0/position    nodes/collide/connections    nodes/start_custom/0/position    nodes/start_custom/connections     nodes/process_custom/0/position !   nodes/process_custom/connections    nodes/sky/0/position    nodes/sky/connections    nodes/fog/0/position    nodes/fog/connections        1   local://VisualShaderNodeTexture2DParameter_4lfoq �	      &   local://VisualShaderNodeTexture_xifm2 �	      %   local://VisualShaderNodeUVFunc_tn154 3
      $   local://VisualShaderNodeInput_yegfd Z
      '   local://VisualShaderNodeVectorOp_twigj �
      ,   local://VisualShaderNodeVec2Parameter_36j28       )   local://VisualShaderNodeSmoothStep_lnq3k F      -   local://VisualShaderNodeFloatParameter_oia1w q      $   local://VisualShaderNodeInput_vw2ko �      '   local://VisualShaderNodeVectorOp_4p8cd        )   res://Common/Shaders/Thunder_Shader.tres 5      #   VisualShaderNodeTexture2DParameter             Basic_Texture                            VisualShaderNodeTexture                               VisualShaderNodeUVFunc             VisualShaderNodeInput             time          VisualShaderNodeVectorOp                    
                 
     �?                                VisualShaderNodeVec2Parameter             Speed          VisualShaderNodeSmoothStep             VisualShaderNodeFloatParameter             Vanishing_Value                   VisualShaderNodeInput             color          VisualShaderNodeVectorOp                      VisualShader          F  shader_type canvas_item;
render_mode blend_add;

uniform float Vanishing_Value : hint_range(0, 1);
uniform vec2 Speed;
uniform sampler2D Basic_Texture : source_color, repeat_enable;



void fragment() {
// Input:10
	vec4 n_out10p0 = COLOR;


// FloatParameter:9
	float n_out9p0 = Vanishing_Value;


// Input:5
	float n_out5p0 = TIME;


// Vector2Parameter:7
	vec2 n_out7p0 = Speed;


// VectorOp:6
	vec2 n_out6p0 = vec2(n_out5p0) * n_out7p0;


// UVFunc:4
	vec2 n_in4p1 = vec2(1.00000, 1.00000);
	vec2 n_out4p0 = n_out6p0 * n_in4p1 + UV;


	vec4 n_out3p0;
// Texture2D:3
	n_out3p0 = texture(Basic_Texture, n_out4p0);


// SmoothStep:8
	float n_in8p1 = 1.00000;
	float n_out8p0 = smoothstep(n_out9p0, n_in8p1, n_out3p0.x);


// VectorOp:11
	vec3 n_out11p0 = vec3(n_out10p0.xyz) * vec3(n_out8p0);


// Output:0
	COLOR.rgb = n_out11p0;


}
                              $   
    ��D  4C%             &   
     ��  �C'            (   
     �C  �C)            *   
      B  �B+            ,   
     4�  pB-            .   
     ��  �B/            0   
     /�  �C1            2   
     4D  �C3            4   
     �C  �B5            6   
     D    7         	   8   
     fD   C9       (                                                                                   	                           
                                   RSRC