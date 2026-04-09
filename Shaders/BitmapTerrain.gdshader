shader_type canvas_item;

// Terrain mask: R=destruction edge, G=unused, B=indestructible, A=solid
uniform sampler2D terrain_mask : filter_nearest;
uniform vec4 earth_color : source_color = vec4(0.396, 0.263, 0.129, 1.0);
uniform vec4 surface_color : source_color = vec4(0.482, 0.753, 0.263, 1.0);
uniform vec4 destruction_color : source_color = vec4(0.18, 0.12, 0.08, 1.0);
uniform vec4 indestructible_color : source_color = vec4(0.333, 0.333, 0.4, 1.0);
uniform float surface_depth : hint_range(1.0, 32.0) = 8.0;

void fragment() {
    vec4 mask = texture(terrain_mask, UV);

    // Discard transparent pixels (not solid terrain)
    if (mask.a < 0.01) {
        discard;
    }

    // Start with earth color
    vec4 result = earth_color;

    // Surface detection: check if any pixel above is empty (within surface_depth).
    // The image is stored with simulation Y-up mapped so that image Y=0 is terrain top.
    // In UV space, Y=0 is the top of the image (= top of terrain).
    // "Above" in world space means toward lower UV.y values.
    vec2 texel_size = vec2(1.0) / vec2(textureSize(terrain_mask, 0));
    bool is_surface = false;
    for (int s = 1; s <= 16; s++) {
        if (float(s) > surface_depth) break;
        vec2 check_uv = UV + vec2(0.0, -float(s) * texel_size.y);
        if (check_uv.y < 0.0) {
            is_surface = true;
            break;
        }
        vec4 above = texture(terrain_mask, check_uv);
        if (above.a < 0.01) {
            is_surface = true;
            break;
        }
    }

    if (is_surface) {
        result = surface_color;
    }

    // Destruction edge (R channel)
    if (mask.r > 0.5) {
        result = destruction_color;
    }

    // Indestructible (B channel)
    if (mask.b > 0.5) {
        result = indestructible_color;
    }

    result.a = 1.0;
    COLOR = result;
}
