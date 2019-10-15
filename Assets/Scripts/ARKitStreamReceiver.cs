﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using Klak.Ndi;

namespace ARKitStream
{
    [RequireComponent(typeof(NdiReceiver))]
    public class ARKitStreamReceiver : MonoBehaviour
    {
        [SerializeField] ARCameraManager cameraManager = null;
        [SerializeField] ARHumanBodyManager humanBodyManager = null;
        [SerializeField] bool isDrawGUI = false;

        NdiReceiver ndiReceiver = null;
        Vector2Int ndiSourceSize = Vector2Int.zero;

        CommandBuffer commandBuffer;
        Material bufferMaterial;


        RenderTexture[] renderTextures;
        Texture2D[] texture2Ds;


        void Start()
        {
            if (!Application.isEditor)
            {
                Destroy(gameObject);
                return;
            }

            ndiReceiver = GetComponent<NdiReceiver>();

            commandBuffer = new CommandBuffer();
            commandBuffer.name = "ARKitStreamReceiver";

            bufferMaterial = new Material(Shader.Find("Unlit/ARKitStreamReceiver"));
        }

        void OnDestroy()
        {
            if (commandBuffer != null)
            {
                commandBuffer.Dispose();
                commandBuffer = null;
            }
            if (renderTextures != null)
            {
                foreach (var rt in renderTextures)
                {
                    Release(rt);
                }
            }
            if (texture2Ds != null)
            {
                foreach (var tex in texture2Ds)
                {
                    Release(tex);
                }
            }
        }

        void Update()
        {
            var rt = ndiReceiver.receivedTexture;
            if (rt == null)
            {
                return;
            }
            if (ndiSourceSize.x != rt.width || ndiSourceSize.y != rt.height)
            {
                InitTexture(rt);
                ndiSourceSize = new Vector2Int(rt.width, rt.height);
            }

            // Decode Textures
            commandBuffer.Clear();
            for (int i = 0; i < renderTextures.Length; i++)
            {
                commandBuffer.Blit(rt, renderTextures[i], bufferMaterial, i);
            }

            Graphics.ExecuteCommandBuffer(commandBuffer);

            for (int i = 0; i < renderTextures.Length; i++)
            {
                Graphics.CopyTexture(renderTextures[i], texture2Ds[i]);
            }

            InvokeTextures();


        }

        void OnGUI()
        {
            if (!isDrawGUI)
            {
                return;
            }
            if (ndiSourceSize == Vector2Int.zero)
            {
                // Wait for connect
                return;
            }
            var w = Screen.width / 2;
            var h = Screen.height / 2;

            GUI.DrawTexture(new Rect(0, 0, w, h), texture2Ds[0]);
            GUI.DrawTexture(new Rect(w, 0, w, h), texture2Ds[1]);
            GUI.DrawTexture(new Rect(0, h, w, h), texture2Ds[2]);
            GUI.DrawTexture(new Rect(w, h, w, h), texture2Ds[3]);
        }

        void Release(RenderTexture tex)
        {
            if (tex == null)
            {
                return;
            }
            tex.Release();
            Destroy(tex);
        }

        void Release(Texture tex)
        {
            if (tex == null)
            {
                return;
            }
            Destroy(tex);
        }

        void InitTexture(Texture source)
        {
            int width = source.width;
            int height = source.height / 2;

            var rformat = new RenderTextureFormat[] {
                RenderTextureFormat.R8, // Camera Y
                RenderTextureFormat.RG16, // Camera CbCr
                RenderTextureFormat.R8, // Stencil
                RenderTextureFormat.RHalf, // Depth
            };
            var tformat = new TextureFormat[] {
                TextureFormat.R8,
                TextureFormat.RG16,
                TextureFormat.R8,
                TextureFormat.RHalf,
            };

            renderTextures = new RenderTexture[rformat.Length];
            texture2Ds = new Texture2D[rformat.Length];

            for (int i = 0; i < rformat.Length; i++)
            {
                renderTextures[i] = new RenderTexture(width, height, 0, rformat[i]);
                texture2Ds[i] = new Texture2D(width, height, tformat[i], 1, false);
            }
        }

        void InvokeTextures()
        {
            // HACK: Invoke another class's event from refrection
            // https://stackoverflow.com/questions/198543/how-do-i-raise-an-event-via-reflection-in-net-c
            // cameraManager.frameReceived(args);

            var args = new ARCameraFrameEventArgs();
            args.textures = new List<Texture2D>() {
                texture2Ds[0],
                texture2Ds[1],
            };
            args.propertyNameIds = new List<int>() {
                Shader.PropertyToID("_textureY"),
                Shader.PropertyToID("_textureCbCr")
            };
            args.displayMatrix = Matrix4x4.identity;

            var eventDelegate = (MulticastDelegate)cameraManager.GetType().GetField("frameReceived", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cameraManager);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { args });

                    Debug.Log("invoikinggg");
                }
            }
        }

    }
}