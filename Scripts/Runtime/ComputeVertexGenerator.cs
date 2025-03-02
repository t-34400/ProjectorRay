#nullable enable

using System;
using UnityEngine;

public class ComputeVertexGenerator : MonoBehaviour
{
    public ComputeShader computeShader;

    public int texWidth = 512;
    public int texHeight = 512;
    public float depthCamNear = 0.1f;
    public float depthCamFar = 1f;
    public float depthCamFoV = 60f;

    public float depthCamAspect = 1.0f; 

    public LayerMask layerMask = Physics.DefaultRaycastLayers;

    public float depthContinuityThreshold = 0.1f;

    public ComputeBuffer VertexBuffer { get; private set; }

    private int kernel;
    private Camera depthCamera;
    private RenderTexture depthTexture;

    public event Action? VertexGenerated;

    void Awake()
    {
        depthCamAspect = (float)texWidth / texHeight;

        gameObject.transform.parent = transform;
        depthCamera = gameObject.AddComponent<Camera>();

        depthCamera.nearClipPlane = depthCamNear;
        depthCamera.farClipPlane = depthCamFar;
        depthCamera.aspect = depthCamAspect;
        depthCamera.enabled = false;
        depthCamera.depthTextureMode = DepthTextureMode.Depth;
        depthCamera.cullingMask = layerMask;

        depthTexture = new RenderTexture(texWidth, texHeight, 24, RenderTextureFormat.Depth);
        depthTexture.enableRandomWrite = false;
        depthTexture.Create();
        depthCamera.targetTexture = depthTexture;
        depthCamera.fieldOfView = depthCamFoV;

        kernel = computeShader.FindKernel("CSMain");
        computeShader.SetTexture(kernel, "_DepthTex", depthTexture);

        var totalPixels = texWidth * texHeight;
        var stride = sizeof(float) * 4 + sizeof(uint);
        VertexBuffer = new ComputeBuffer(totalPixels, stride);
        computeShader.SetBuffer(kernel, "vertexBuffer", VertexBuffer);

        computeShader.SetBool("_DepthReversed", SystemInfo.usesReversedZBuffer);

        Debug.Log($"SystemInfo.usesReversedZBuffer: {SystemInfo.usesReversedZBuffer}");
    }

    void LateUpdate()
    {
        depthCamera.Render();

        var depthCamMatrix = depthCamera.transform.localToWorldMatrix;

        computeShader.SetInt("_TexWidth", texWidth);
        computeShader.SetInt("_TexHeight", texHeight);
        computeShader.SetFloat("_DepthCamAspect", depthCamAspect);
        computeShader.SetFloat("_DepthContinuityThreshold", depthContinuityThreshold);
        computeShader.SetFloat("_DepthCamNear", depthCamera.nearClipPlane);
        computeShader.SetFloat("_DepthCamFar", depthCamera.farClipPlane);
        computeShader.SetFloat("_DepthCamFoV", depthCamera.fieldOfView * Mathf.Deg2Rad);
        computeShader.SetMatrix("_DepthCamMatrix", depthCamMatrix);

        var threadGroupX = Mathf.CeilToInt(texWidth / 8f);
        var threadGroupY = Mathf.CeilToInt(texHeight / 8f);

        computeShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);

        VertexGenerated?.Invoke();
    }

    void OnDestroy()
    {
        if (VertexBuffer != null)
            VertexBuffer.Release();
        if (depthTexture != null)
            depthTexture.Release();
    }
}
