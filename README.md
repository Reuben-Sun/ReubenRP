# ReubenRP

这个一个基于Unity SRP的渲染管线

### 管线内容

- SRP
- 前向渲染
- 阴影投射
  - 多光源阴影
  - PCF
  - 法线修正
  - 阴影级联
    - 混合级联
    - 抖动过渡
  - 透明物体阴影
- 间接光漫反射
  - Lightmap
  - Light Probe
- 间接光高光
  - Cubemap
- LOD
- 后效
  - Bloom

### Shader

- 一个UnLit

- 一个PBR Lit
  - diffuse：兰伯特
  - specular：迪士尼brdf

### 后续更新计划

- 打算在里面加一点卡渲，写个ssgi

### 构建与运行
Windows平台Unity打开即可

### 资料
Catelike SRP Tutorials
