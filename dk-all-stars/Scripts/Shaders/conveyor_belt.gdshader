shader_type canvas_item;

// Total number of animation frames in your sheet
uniform int num_frames = 8;
// Controls which discrete frame (0 to 7) is being drawn
uniform int current_frame = 0;
// How many times the middle tile repeats across the node's width
uniform float tile_count = 1.0;

void fragment() {
    // 1. Tile the X coordinate across the width of the TextureRect
    float tiled_x = fract(UV.x * tile_count);
    
    // 2. Compress the tiled coordinate to fit within a single frame's slot
    tiled_x /= float(num_frames);
    
    // 3. Shift over to the active frame slot
    float frame_offset = float(current_frame) / float(num_frames);
    vec2 final_uv = vec2(tiled_x + frame_offset, UV.y);
    
    COLOR = texture(TEXTURE, final_uv);
}