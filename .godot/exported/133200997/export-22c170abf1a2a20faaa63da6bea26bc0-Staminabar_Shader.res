RSRC                    VisualShader            ��������                                            J      resource_local_to_scene    resource_name    output_port_for_preview    default_input_values    expanded_output_ports    linked_parent_graph_frame 	   function    script    input_name    parameter_name 
   qualifier    default_value_enabled    default_value    op_type 	   operator    source    texture    texture_type    color_default    texture_filter    texture_repeat    texture_source    hint    min    max    step    code    graph_offset    mode    modes/blend    flags/skip_vertex_transform    flags/unshaded    flags/light_only    flags/world_vertex_coords    nodes/vertex/0/position    nodes/vertex/connections    nodes/fragment/0/position    nodes/fragment/8/node    nodes/fragment/8/position    nodes/fragment/9/node    nodes/fragment/9/position    nodes/fragment/10/node    nodes/fragment/10/position    nodes/fragment/11/node    nodes/fragment/11/position    nodes/fragment/13/node    nodes/fragment/13/position    nodes/fragment/14/node    nodes/fragment/14/position    nodes/fragment/15/node    nodes/fragment/15/position    nodes/fragment/16/node    nodes/fragment/16/position    nodes/fragment/17/node    nodes/fragment/17/position    nodes/fragment/18/node    nodes/fragment/18/position    nodes/fragment/connections    nodes/light/0/position    nodes/light/connections    nodes/start/0/position    nodes/start/connections    nodes/process/0/position    nodes/process/connections    nodes/collide/0/position    nodes/collide/connections    nodes/start_custom/0/position    nodes/start_custom/connections     nodes/process_custom/0/position !   nodes/process_custom/connections    nodes/sky/0/position    nodes/sky/connections    nodes/fog/0/position    nodes/fog/connections    
   Texture2D 9   res://Player/User Interface/UI_Art/StaminaFogTexture.png g7s�zE   %   local://VisualShaderNodeUVFunc_2f5kq �	      $   local://VisualShaderNodeInput_m5q7v 
      ,   local://VisualShaderNodeVec2Parameter_nlbek L
      '   local://VisualShaderNodeVectorOp_6dpib �
      '   local://VisualShaderNodeVectorOp_gmp5p 6      &   local://VisualShaderNodeTexture_qph3d �      1   local://VisualShaderNodeTexture2DParameter_mj3os       $   local://VisualShaderNodeInput_wgcqa l      '   local://VisualShaderNodeVectorOp_c26c2 �      -   local://VisualShaderNodeFloatParameter_xq2nm �      ,   res://Common/Shaders/Staminabar_Shader.tres G         VisualShaderNodeUVFunc             VisualShaderNodeInput             time          VisualShaderNodeVec2Parameter                    	         Move_Speed             
     ��             VisualShaderNodeVectorOp                    
                 
                                       VisualShaderNodeVectorOp                                                                                           VisualShaderNodeTexture                                         #   VisualShaderNodeTexture2DParameter    	      
   Texture2D                            VisualShaderNodeInput             color          VisualShaderNodeVectorOp                      VisualShaderNodeFloatParameter    	         FloatParameter                   @        �@         VisualShader          T  shader_type canvas_item;
render_mode blend_mix;

uniform float FloatParameter : hint_range(2, 5);
uniform vec2 Move_Speed = vec2(-1.000000, 0.000000);
uniform sampler2D Texture2D : source_color, repeat_enable;



void fragment() {
// FloatParameter:18
	float n_out18p0 = FloatParameter;


// Input:16
	vec4 n_out16p0 = COLOR;


// VectorOp:17
	vec3 n_out17p0 = vec3(n_out18p0) * vec3(n_out16p0.xyz);


// Input:9
	float n_out9p0 = TIME;


// Vector2Parameter:10
	vec2 n_out10p0 = Move_Speed;


// VectorOp:11
	vec2 n_out11p0 = vec2(n_out9p0) * n_out10p0;


// UVFunc:8
	vec2 n_in8p1 = vec2(1.00000, 1.00000);
	vec2 n_out8p0 = n_out11p0 * n_in8p1 + UV;


	vec4 n_out14p0;
// Texture2D:14
	n_out14p0 = texture(Texture2D, n_out8p0);


// VectorOp:13
	vec4 n_out13p0 = vec4(n_out17p0, 0.0) * n_out14p0;


// Output:0
	COLOR.rgb = vec3(n_out13p0.xyz);


}
                     $   
     �D  pB%             &   
     ��  ��'            (   
     ��  ��)            *   
     ��  �+            ,   
     R�  \�-            .   
     9D  pB/            0   
     �B  pB1            2   
     �  �B3            4   
     ��  ��5            6   
     �C  ��7         	   8   
     ��  ��9       (   	                                                     
                                                                                         RSRC