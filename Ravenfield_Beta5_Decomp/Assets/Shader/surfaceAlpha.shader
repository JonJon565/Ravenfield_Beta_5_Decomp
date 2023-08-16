Shader "EasyRoads3D/EasyRoads3D Surface Transparant" {
Properties {
 _Color ("Main Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 _MainTex ("Base (RGB) Trans (A)", 2D) = "white" { }
}
Fallback "Transparent/VertexLit"
}