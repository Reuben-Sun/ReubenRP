using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MaterialBlock : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] private Color baseColor = Color.white;
    private static MaterialPropertyBlock _block;

    private void OnValidate()
    {
        if (_block == null)
        {
            _block = new MaterialPropertyBlock();
        }
        _block.SetColor(baseColorId, baseColor);
        GetComponent<Renderer>().SetPropertyBlock(_block);
    }

    private void Awake()
    {
        OnValidate();
    }
}
