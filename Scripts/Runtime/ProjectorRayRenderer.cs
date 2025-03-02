using UnityEngine;

public class ProjectorRayRenderer : MonoBehaviour
{
    public ComputeVertexGenerator vertexGenerator = default!;
    public Material renderMaterial;

    public float depthFactor = 0.5f;

    private bool initialized = false;

    void Start()
    {
        if (vertexGenerator == null)
        {
            Debug.LogError("ComputeVertexGenerator not found");
            return;
        }
    }

    struct VertexData
    {
        public Vector3 worldPos;
        public uint valid;
    }

    void OnPostRender()
    {
        if (renderMaterial == null || vertexGenerator == null || vertexGenerator.VertexBuffer == null || !vertexGenerator.gameObject.activeInHierarchy)
            return;

        if (!initialized)
        {
            renderMaterial.SetBuffer("_VertexBuffer", vertexGenerator.VertexBuffer);
            initialized = true;
        }

        var texWidth = vertexGenerator.texWidth;
        var texHeight = vertexGenerator.texHeight;
        int cellCount = (texWidth - 1) * (texHeight - 1);

        renderMaterial.SetFloat("_DepthFactor", depthFactor);
        renderMaterial.SetInt("_TexWidth", vertexGenerator.texWidth);
        renderMaterial.SetInt("_TexHeight", vertexGenerator.texHeight);
        renderMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, cellCount);
    }
}
