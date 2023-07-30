#version 410

layout( triangles_adjacency ) in;
layout( triangle_strip, max_vertices = 18 ) out;

in vec3 VPosition[];    //相机坐标空间
in vec3 VNormal[];

uniform vec4 LightPosition;  // 光源位置 (eye coords)
uniform mat4 ProjMatrix;     // Projection matrix

//判断三角形是否面向光源
bool facesLight( vec3 a, vec3 b, vec3 c )
{
  vec3 n = cross( b - a, c - a );
  vec3 da = LightPosition.xyz - a;
  vec3 db = LightPosition.xyz - b;
  vec3 dc = LightPosition.xyz - c;

  return dot(n, da) > 0 || dot(n, db) > 0 || dot(n, dc) > 0; 
}

//生成阴影体面的四边形
void emitEdgeQuad( vec3 a, vec3 b )
{
  gl_Position = ProjMatrix * vec4(a, 1);
  EmitVertex();
  
  gl_Position = ProjMatrix * vec4(a - LightPosition.xyz, 0);
  EmitVertex();

  gl_Position = ProjMatrix * vec4(b, 1);
  EmitVertex();

  gl_Position = ProjMatrix * vec4(b - LightPosition.xyz, 0);
  EmitVertex();
  EndPrimitive();
}

void main()
{
    // If the main triangle faces the light, check each adjacent
    // triangle.  If an adjacent triangle does not face the light
    // we output a sihlouette edge quad for the corresponding edge.
    /*如果主三角形面向光源，检查邻居的三角形；如果邻居的三角形没有面向光源，
     那么我们就输出一个阴影体的四边形面
     */
    if( facesLight(VPosition[0], VPosition[2], VPosition[4]) ) {
        if( ! facesLight(VPosition[0],VPosition[1],VPosition[2]) ) 
          emitEdgeQuad(VPosition[0],VPosition[2]);
        if( ! facesLight(VPosition[2],VPosition[3],VPosition[4]) ) 
          emitEdgeQuad(VPosition[2],VPosition[4]);
        if( ! facesLight(VPosition[4],VPosition[5],VPosition[0]) ) 
          emitEdgeQuad(VPosition[4],VPosition[0]);
    }
}
