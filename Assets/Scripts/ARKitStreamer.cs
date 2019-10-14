﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARKitStreamer : MonoBehaviour
{
    [SerializeField] ARHumanBodyManager humanBodyManager = null;
    [SerializeField] Material material = null;

    enum Mode
    {
        Stencil,
        Depth,
    }
    [SerializeField] Mode mode;

    static readonly int _textureStencil = Shader.PropertyToID("_textureStencil");

    void Start()
    {
        switch (mode)
        {
            case Mode.Stencil:
                OnToggleStencil();
                break;
            case Mode.Depth:
                OnToggleDepth();
                break;
        }
    }

    void Update()
    {
        var subsystem = humanBodyManager.subsystem;
        if (subsystem == null)
        {
            return;
        }

        Texture2D texture = null;
        switch (mode)
        {
            case Mode.Stencil:
                texture = humanBodyManager.humanStencilTexture;
                break;
            case Mode.Depth:
                texture = humanBodyManager.humanDepthTexture;
                break;
        }
        material.SetTexture(_textureStencil, texture);
    }

    public void OnToggleStencil()
    {
        humanBodyManager.humanSegmentationStencilMode = HumanSegmentationMode.FullScreenResolution;
        humanBodyManager.humanSegmentationDepthMode = HumanSegmentationMode.Disabled;
    }
    public void OnToggleDepth()
    {
        humanBodyManager.humanSegmentationStencilMode = HumanSegmentationMode.Disabled;
        humanBodyManager.humanSegmentationDepthMode = HumanSegmentationMode.FullScreenResolution;
    }
}