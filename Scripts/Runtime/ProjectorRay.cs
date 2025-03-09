#nullable enable

using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectorRay
{
    class ProjectorRay : MonoBehaviour
    {
        [Header("Depth Camera")]
        [SerializeField] private ComputeShader computeShader = default!;
        [SerializeField] private int texWidth = 512;
        [SerializeField] private int texHeight = 512;
        [SerializeField] private float depthCamNear = 0.1f;
        [SerializeField] private float depthCamFar = 1f;
        [SerializeField] private float depthCamFoV = 60f;
        [SerializeField] private LayerMask layerMask = Physics.DefaultRaycastLayers;
        [Header("Rendering")]
        [SerializeField] private Material renderMaterial = default!;
        [SerializeField] private float depthContinuityThreshold = 0.1f;
        [SerializeField] private bool setDepthFactor = true;
        [SerializeField] private float depthFactor = 0.5f;

        private float depthCamAspect = 1.0f; 

        private Camera? depthCamera;
        private RenderTexture? depthTexture;
        private int kernel;
        private MeshRenderer? meshRenderer;

        void Start()
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = renderMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            var meshFilter = gameObject.AddComponent<MeshFilter>();
            var mesh = GenerateGridMesh(texWidth, texHeight, 1);
            meshFilter.mesh = mesh;

            depthCamAspect = (float)texWidth / texHeight;
            (depthCamera, depthTexture) = CreateDepthCamera(gameObject, texWidth, texHeight, depthCamNear, depthCamFar, depthCamFoV, layerMask);

            kernel = computeShader.FindKernel("CSMain");
            computeShader.SetTexture(kernel, "_DepthTex", depthTexture);

            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
            var vertexBuffer = mesh.GetVertexBuffer(0);
            computeShader.SetBuffer(kernel, "vertexBuffer", vertexBuffer);

            Debug.Log($"SystemInfo.usesReversedZBuffer: {SystemInfo.usesReversedZBuffer}");
            computeShader.SetBool("_DepthReversed", SystemInfo.usesReversedZBuffer);
        }

        void LateUpdate()
        {
            if (depthCamera == null || depthTexture == null || meshRenderer == null)
                return;

            meshRenderer.enabled = false;
            depthCamera.Render();
            meshRenderer.enabled = true;

            computeShader.SetInt("_TexWidth", texWidth);
            computeShader.SetInt("_TexHeight", texHeight);
            computeShader.SetFloat("_DepthCamAspect", depthCamAspect);
            computeShader.SetFloat("_DepthContinuityThreshold", depthContinuityThreshold);
            computeShader.SetFloat("_DepthCamNear", depthCamera.nearClipPlane);
            computeShader.SetFloat("_DepthCamFar", depthCamera.farClipPlane);
            computeShader.SetFloat("_DepthCamFoV", depthCamera.fieldOfView * Mathf.Deg2Rad);

            var threadGroupX = Mathf.CeilToInt(texWidth / 8f);
            var threadGroupY = Mathf.CeilToInt(texHeight / 8f);

            computeShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);

            if (setDepthFactor)
            {
                renderMaterial.SetFloat("_DepthFactor", depthFactor);
            }
        }

        void OnDestroy()
        {
            if (depthTexture != null)
                depthTexture.Release();
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            var originalColor = Gizmos.color;
            var originalMatrix = Gizmos.matrix;

            Gizmos.color = new Color(1, 1, 0, 0.75F);
            Gizmos.matrix = transform.localToWorldMatrix;
            depthCamAspect = (float)texWidth / texHeight;
            Gizmos.DrawFrustum(Vector3.zero, depthCamFoV, depthCamFar, depthCamNear, depthCamAspect);

            Gizmos.color = originalColor;
            Gizmos.matrix = originalMatrix;
        }
#endif

        static (Camera, RenderTexture) CreateDepthCamera(GameObject gameObject, int texWidth, int texHeight, float near, float far, float fov, LayerMask layerMask)
        {
            var depthCamera = gameObject.AddComponent<Camera>();

            depthCamera.nearClipPlane = near;
            depthCamera.farClipPlane = far;
            depthCamera.aspect = (float)texWidth / texHeight;
            depthCamera.enabled = false;
            depthCamera.depthTextureMode = DepthTextureMode.Depth;
            depthCamera.cullingMask = layerMask;

            var depthTexture = new RenderTexture(texWidth, texHeight, 24, RenderTextureFormat.Depth);
            depthTexture.enableRandomWrite = false;
            depthTexture.Create();
            depthCamera.targetTexture = depthTexture;
            depthCamera.fieldOfView = fov;

            return (depthCamera, depthTexture);
        }

        static Mesh GenerateGridMesh(int width, int height, float size) {
            var mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;

            var vertexCount = width * height;
            var triangleCount = (width - 1) * (height - 1) * 6;

            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var shaderParams = new Vector2[vertexCount];
            var triangles = new int[triangleCount];

            for (var z = 0; z < height; z++) {
                for (var x = 0; x < width; x++) {
                    var index = x + z * width;
                    var px = (x / (float)width - 0.5f) * size;
                    var pz = (z / (float)height - 0.5f) * size;
                    vertices[index] = new Vector3(px, 0, pz);
                    uvs[index] = new Vector2(x / (float)(width - 1), z / (float)(height - 1));
                }
            }

            int ti = 0;
            for (var z = 0; z < height - 1; z++) {
                for (var x = 0; x < width - 1; x++) {
                    var i = x + z * width;
                    triangles[ti++] = i;
                    triangles[ti++] = i + width;
                    triangles[ti++] = i + 1;
                    triangles[ti++] = i + 1;
                    triangles[ti++] = i + width;
                    triangles[ti++] = i + width + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.uv2 = shaderParams;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}