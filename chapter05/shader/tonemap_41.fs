#version 410

struct LightInfo {
  vec4 Position;  // Light position in eye coords.
  vec3 Intensity; // A,D,S intensity
};
uniform LightInfo Lights[3];

struct MaterialInfo {
  vec3 Ka;
  vec3 Kd;            // Diffuse reflectivity
  vec3 Ks;
  float Shine;
};
uniform MaterialInfo Material;

uniform float AveLum;

subroutine void RenderPassType();
subroutine uniform RenderPassType RenderPass;

uniform sampler2D HdrTex;

in vec3 Position;
in vec3 Normal;
in vec2 TexCoord;

layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec3 HdrColor;

// XYZ/RGB conversion matrices from:
// http://www.brucelindbloom.com/index.html?Eqn_RGB_XYZ_Matrix.html

uniform mat3 rgb2xyz = mat3( 
  0.4124564, 0.2126729, 0.0193339,
  0.3575761, 0.7151522, 0.1191920,
  0.1804375, 0.0721750, 0.9503041 );

uniform mat3 xyz2rgb = mat3(
  3.2404542, -0.9692660, 0.0556434,
  -1.5371385, 1.8760108, -0.2040259,
  -0.4985314, 0.0415560, 1.0572252 );

uniform float Exposure = 0.35;
uniform float White = 0.928;
uniform bool DoToneMap = true;

vec3 ads( vec3 pos, vec3 norm )
{
    vec3 v = normalize(vec3(-pos));
    vec3 total = vec3(0.0f, 0.0f, 0.0f);

    for( int i = 0; i < 3; i++ ) {
      vec3 s = normalize( vec3(Lights[i].Position) - pos) ;
      vec3 r = reflect( -s, norm );

      total += 
        Lights[i].Intensity * ( Material.Ka +
            Material.Kd * max( dot(s, norm), 0.0 ) +
            Material.Ks * pow( max( dot(r,v), 0.0 ), Material.Shine ) );
    }
    return total;
}

subroutine (RenderPassType)
void pass1()
{
    // Compute shading and store result in high-res framebuffer
    HdrColor = ads(Position, Normal); 
}

//aces算法矫正，效果个人觉得还可以
vec3 aces_approx(vec3 v)
{
    v *= 0.6f;
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    return clamp((v*(a*v+b))/(v*(c*v+d)+e), 0.0f, 1.0f);
}

float aces_approx_1(float v)
{
    v *= 0.6f;
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    return clamp((v*(a*v+b))/(v*(c*v+d)+e), 0.0f, 1.0f);
}

//ACES_FULL算法，据说效果是最好的

mat3 LinearToACES = mat3
(
    0.59719f, 0.07600f, 0.02840f,
    0.35458f, 0.90834f, 0.13383f,
    0.04823f, 0.01566f, 0.83777f
);

mat3 ACESToLinear = mat3
(
    1.60475f, -0.10208f, -0.00327f,
    -0.53108f,  1.10813f, -0.07276f,
    -0.07367f, -0.00605f,  1.07602f
);


vec3 rtt_and_odt_fit(vec3 col)
{
    vec3 a = col * (col + 0.0245786f) - 0.000090537f;
    vec3 b = col * (0.983729f * col + 0.4329510f) + 0.238081f;
    return a / b;
}

vec4 ACESFull(vec4 col)
{
    vec3 aces = LinearToACES * col.rgb;
    aces = rtt_and_odt_fit(aces);
    col.rgb = ACESToLinear * aces;
    return col;
}

// This pass computes the sum of the luminance of all pixels
subroutine(RenderPassType)
void pass2()
{
    // Retrieve high-res color from texture
    vec4 color = texture( HdrTex, TexCoord );
    
    // Convert to XYZ
    vec3 xyzCol = rgb2xyz * vec3(color);

    // Convert to xyY
    float xyzSum = xyzCol.x + xyzCol.y + xyzCol.z;
    vec3 xyYCol = vec3( xyzCol.x / xyzSum, xyzCol.y / xyzSum, xyzCol.y);

    // Apply the tone mapping operation to the luminance (xyYCol.z or xyzCol.y)
    float L = (Exposure * xyYCol.z) / AveLum;
    L = (L * ( 1 + L / (White * White) )) / ( 1 + L );
    L = aces_approx_1(xyYCol.z);

    // Using the new luminance, convert back to XYZ
    xyzCol.x = (L * xyYCol.x) / (xyYCol.y);
    xyzCol.y = L;
    xyzCol.z = (L * (1 - xyYCol.x - xyYCol.y))/xyYCol.y;

    // Convert back to RGB and send to output buffer
    if( DoToneMap )
    {
        FragColor = vec4( xyz2rgb * xyzCol, 1.0);
        FragColor = ACESFull(color);
    }
    else
    {
        FragColor = color;
    }
}

void main() {
    // This will call either pass1 or pass2
    RenderPass();
}
