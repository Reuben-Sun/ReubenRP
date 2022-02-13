using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;
using LightType = UnityEngine.LightType;
public partial class ReubenRenderPipeline
{
   partial void InitializeForEditor();
#if UNITY_EDITOR
   partial void InitializeForEditor()
   {
      Lightmapping.SetDelegate(lightDelegate);
   }
#endif
   
#if UNITY_EDITOR
   //灯光委托，用于告诉 unity使用正确的（不同的）衰减
   private static Lightmapping.RequestLightsDelegate lightDelegate = (Light[] lights, NativeArray<LightDataGI> output) =>
   {
      var lightData = new LightDataGI();
      for (int i = 0; i < lights.Length; i++)
      {
         Light light = lights[i];
         switch (light.type)
         {
            case LightType.Directional:
               var directionalLight = new DirectionalLight();
               LightmapperUtils.Extract(light, ref directionalLight);
               lightData.Init(ref directionalLight);
               break;
            case LightType.Point:
               var pointLight = new PointLight();
               LightmapperUtils.Extract(light, ref pointLight);
               lightData.Init(ref pointLight);
               break;
            case LightType.Spot:
               var spotLight = new SpotLight();
               LightmapperUtils.Extract(light, ref spotLight);
               lightData.Init(ref spotLight);
               break;
            case LightType.Area:
               var rectangleLight = new RectangleLight();
               LightmapperUtils.Extract(light, ref rectangleLight);
               rectangleLight.mode = LightMode.Baked;    //强制设置为 baked模式
               lightData.Init(ref rectangleLight);
               break;
            default:
               lightData.InitNoBake(light.GetInstanceID());
               break;
         }

         lightData.falloff = FalloffType.InverseSquared;    //设置灯光衰减类型
         output[i] = lightData;
      }
   };
#endif
}
