using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderTest : MonoBehaviour
{
    [SerializeField]
    private ComputeShader ComputeShader;

    [SerializeField]
    private RenderTexture RenderTexture;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if(RenderTexture == null)
        {
            RenderTexture = new RenderTexture(256, 256, 24);
            RenderTexture.enableRandomWrite = true;
            RenderTexture.Create();
        }

        ComputeShader.SetTexture(0, "Result", RenderTexture);
        ComputeShader.SetFloat("Resolution", RenderTexture.width);
        ComputeShader.Dispatch(0, RenderTexture.width / 8, RenderTexture.height / 8, 1);

        Graphics.Blit(RenderTexture, destination);
    }
}
