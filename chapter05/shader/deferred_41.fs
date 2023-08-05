#version 410

struct LightInfo {
  vec4 Position;  // Light position in eye coords.
  vec3 Intensity; // A,D,S intensity
};
uniform LightInfo Light;

struct MaterialInfo {
  vec3 Kd;            // Diffuse reflectivity
};
uniform MaterialInfo Material;

subroutine void RenderPassType();
subroutine uniform RenderPassType RenderPass;

uniform sampler2D NormalTex;
uniform sampler2D ColorTex;
uniform sampler2D DepthTex;

//屏幕宽高
uniform float screenWidth;
uniform float screenHeight;

//远近裁剪参数
uniform float near;
uniform float far;

uniform float fov;

in vec3 Position;
in vec3 Normal;
in vec2 TexCoord;

layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec3 PositionData;
layout (location = 2) out vec3 NormalData;
layout (location = 3) out vec3 ColorData;

float ViewSpaceZFromDepth(float d)
{
    d = d * 2.0 - 1.0;
    //视线坐标系看向的z轴负方向，因此要求视觉空间的z值应该要把线性深度变成负值
    return (2.0 * near * far) / (far + near - d * (far - near));
}

//根据uv和视空间中的z坐标计算视空间中的顶点坐标
vec3 UVToViewSpace(vec2 uv, float z)
{
    uv = uv * 2.0 - 1.0;
    uv.x = uv.x * tan(fov / 2.0) * screenWidth / screenHeight * z;
    uv.y = uv.y * tan(fov / 2.0)  * z ;
    return vec3(uv, -z);
}

//获得视空间中的顶点坐标
vec3 GetViewPos(vec2 uv)
{
    float z = ViewSpaceZFromDepth(texture(DepthTex, uv).r);
    return UVToViewSpace(uv, z);
}

float Length2(vec3 V)
{
    return dot(V,V);
}

vec3 MinDiff(vec3 P, vec3 Pr, vec3 Pl)
{
    vec3 V1 = Pr - P;
    vec3 V2 = P - Pl;
    return (Length2(V1) < Length2(V2)) ? V1 : V2;
}

//获得视线空间中的法线向量
vec3 GetViewNormal(vec2 uv)
{
    float xOffset = 1.0 / screenWidth;
    float yOffset = 1.0 / screenHeight;
    vec3 P = GetViewPos(uv);
    vec3 Pl = GetViewPos(uv + vec2(-xOffset,0));
    vec3 Pr = GetViewPos(uv + vec2(xOffset,0));
    vec3 Pu = GetViewPos(uv + vec2(0,yOffset));
    vec3 Pd = GetViewPos(uv + vec2(0,-yOffset));
    vec3 leftDir = MinDiff(P, Pr, Pl);
    vec3 upDir = MinDiff(P, Pu, Pd);
    vec3 Nomal = normalize(cross(leftDir, upDir));
    
    return Nomal;
}

vec3 diffuseModel( vec3 pos, vec3 norm, vec3 diff )
{
    vec3 s = normalize(vec3(Light.Position) - pos);
    float sDotN = max( dot(s,norm), 0.0 );
    vec3 diffuse = Light.Intensity * diff * sDotN;

    return diffuse;
}

subroutine (RenderPassType)
void pass1()
{
    // Store position, normal, and diffuse color in textures
    //PositionData = Position;
    NormalData = Normal;
    ColorData = Material.Kd;
}

subroutine(RenderPassType)
void pass2()
{
    // Retrieve position and normal information from textures
    //vec3 pos = vec3( texture( PositionTex, TexCoord ) );
    vec3 pos = GetViewPos(TexCoord);   //重建出来的位置还是比较可以的
    vec3 norm = vec3( texture( NormalTex, TexCoord ) );
    //vec3 norm = GetViewNormal(TexCoord);  //法线计算还是有比较大的瑕疵
    vec3 diffColor = vec3( texture(ColorTex, TexCoord) );

    FragColor = vec4( diffuseModel(pos,norm,diffColor), 1.0 );
}

void main() {
    // This will call either pass1 or pass2
    RenderPass();
}
