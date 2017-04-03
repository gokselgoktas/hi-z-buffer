using UnityEngine;

[RequireComponent(typeof (HiZBuffer))]
public class Viewer : MonoBehaviour
{
    [Range(0, 16)]
    public int level = 0;

    private Shader m_Shader;
    public Shader shader
    {
        get
        {
            if (m_Shader == null)
                m_Shader = Shader.Find("Hidden/Viewer");

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

    private RenderTexture m_HiZ
    {
        get
        {
            var hiZ = GetComponent<HiZBuffer>();

            if (hiZ == null)
                return null;

            return hiZ.texture;
        }
    }

    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (m_HiZ == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        material.SetInt("_Level", level);
        Graphics.Blit(m_HiZ, destination, material);
    }
}
