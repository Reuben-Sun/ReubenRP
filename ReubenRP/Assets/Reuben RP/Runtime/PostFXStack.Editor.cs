using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

partial class PostFXStack
{
    partial void ApplySceneViewState();
#if UNITY_EDITOR
    partial void ApplySceneViewState()
    {
        if (_camera.cameraType == CameraType.SceneView &&
            !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)     //如果在编辑器界面禁用了 Post Processings
        {
            _settings = null;
        }
    }
#endif
}
