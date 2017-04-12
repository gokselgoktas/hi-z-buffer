using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof (Camera))]
public class HiZBuffer : MonoBehaviour
{
    private enum Pass
    {
        Resolve,
        Reduce,
        Blit
    }

    private Shader m_Shader;
    public Shader shader
    {
        get
        {
            if (m_Shader == null)
                m_Shader = Shader.Find("Hidden/Hi-Z Buffer");

            return m_Shader;
        }
    }
    private Material m_Material;
    public Material material
    {
        get
        {
            if (m_Material == null)
            {
                if (shader == null || shader.isSupported == false)
                    return null;

                m_Material = new Material(shader);
            }

            return m_Material;
        }
    }

    private Camera m_Camera;
    public new Camera camera
    {
        get
        {
            if (m_Camera == null)
                m_Camera = GetComponent<Camera>();

            return m_Camera;
        }
    }

    private Mesh m_Quad;
    private Mesh quad
    {
        get
        {
            if (m_Quad == null)
            {
                Vector3[] vertices = new Vector3[4]
                {
                    new Vector3(1.0f, 1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f, 1.0f, 0.0f),
                    new Vector3(-1.0f, -1.0f, 0.0f),
                };

                Vector2[] uv = new Vector2[]
                {
                    new Vector2(1, 0),
                    new Vector2(1, 1),
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                };

                int[] indices = new int[6] { 0, 1, 2, 2, 1, 3 };

                m_Quad = new Mesh();
                m_Quad.vertices = vertices;
                m_Quad.uv = uv;
                m_Quad.triangles = indices;
            }

            return m_Quad;
        }
    }

    private RenderTexture m_HiZ;
    public RenderTexture texture
    {
        get
        {
            return m_HiZ;
        }
    }

    private int m_LevelCount = 0;
    public int levelCount
    {
        get
        {
            if (m_HiZ == null)
                return 0;

            return 1 + m_LevelCount;
        }
    }

    private CommandBuffer m_CommandBuffer;
    private CameraEvent m_CameraEvent = CameraEvent.BeforeReflections;

    private int[] m_Temporaries;

    void OnEnable()
    {
        camera.depthTextureMode = DepthTextureMode.Depth;
    }

    void OnDisable()
    {
        if (camera != null)
        {
            if (m_CommandBuffer != null)
            {
                camera.RemoveCommandBuffer(m_CameraEvent, m_CommandBuffer);
                m_CommandBuffer = null;
            }
        }

        if (m_HiZ != null)
        {
            m_HiZ.Release();
            m_HiZ = null;
        }
    }

    void OnPreRender()
    {
        int width = camera.pixelWidth;
        int height = camera.pixelHeight;

        m_LevelCount = (int) Mathf.Floor(Mathf.Log(Mathf.Max(width, height), 2f));

        bool isCommandBufferInvalid = false;

        if (m_LevelCount == 0)
            return;

        if (m_HiZ == null || (m_HiZ.width != width || m_HiZ.height != height))
        {
            if (m_HiZ != null)
                m_HiZ.Release();

            m_HiZ = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            m_HiZ.filterMode = FilterMode.Point;

            m_HiZ.useMipMap = true;
            m_HiZ.autoGenerateMips = false;

            m_HiZ.Create();

            m_HiZ.hideFlags = HideFlags.HideAndDontSave;

            isCommandBufferInvalid = true;
        }

        if (m_CommandBuffer == null || isCommandBufferInvalid == true)
        {
            m_Temporaries = new int[m_LevelCount];

            if (m_CommandBuffer != null)
                camera.RemoveCommandBuffer(m_CameraEvent, m_CommandBuffer);

            m_CommandBuffer = new CommandBuffer();
            m_CommandBuffer.name = "Hi-Z Buffer";

            RenderTargetIdentifier id = new RenderTargetIdentifier(m_HiZ);

            m_CommandBuffer.Blit(null, id, material, (int) Pass.Resolve);

            for (int i = 0; i < m_LevelCount; ++i)
            {
                m_Temporaries[i] = Shader.PropertyToID("_09659d57_Temporaries" + i.ToString());

                width >>= 1;
                height >>= 1;

                if (width == 0)
                    width = 1;

                if (height == 0)
                    height = 1;

                m_CommandBuffer.GetTemporaryRT(m_Temporaries[i], width, height, 0, FilterMode.Point, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);

                if (i == 0)
                    m_CommandBuffer.Blit(id, m_Temporaries[0], material, (int) Pass.Reduce);
                else
                    m_CommandBuffer.Blit(m_Temporaries[i - 1], m_Temporaries[i], material, (int) Pass.Reduce);

                m_CommandBuffer.CopyTexture(m_Temporaries[i], 0, 0, id, 0, i + 1);

                if (i >= 1)
                    m_CommandBuffer.ReleaseTemporaryRT(m_Temporaries[i - 1]);
            }

            m_CommandBuffer.ReleaseTemporaryRT(m_Temporaries[m_LevelCount - 1]);

            camera.AddCommandBuffer(m_CameraEvent, m_CommandBuffer);
        }
    }
}
