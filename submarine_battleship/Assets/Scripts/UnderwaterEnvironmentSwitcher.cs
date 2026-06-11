using UnityEngine;
using UnityEngine.Rendering;

public class UnderwaterEnvironmentSwitcher : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;
    [SerializeField] private Volume underwaterVolume;

    [Header("Surface")]
    [SerializeField] private float surfaceY = 0f;
    [SerializeField] private float blendRange = 1.0f;

    [Header("Fog (Underwater)")]
    [SerializeField] private Color underwaterFogColor = new Color(0.1f, 0.4f, 0.7f, 1f);
    [SerializeField] private float underwaterFogDensity = 0.1f;

    [SerializeField] private GameObject underwaterDustObj;
    [SerializeField] private GameObject smallBubblesObj;

    private Color defaultFogColor;
    private float defaultFogDensity;
    private bool defaultFogEnabled;
    

    void Start()
    {
        defaultFogEnabled = RenderSettings.fog;
        defaultFogColor = RenderSettings.fogColor;
        defaultFogDensity = RenderSettings.fogDensity;
    }

    void Update()
    {
        if (!targetCamera) return;

        float depth = surfaceY - targetCamera.position.y; // +なら水中
        float w = Mathf.InverseLerp(-blendRange, blendRange, depth);
        w = Mathf.SmoothStep(0f, 1f, w);

        if (underwaterVolume) underwaterVolume.weight = w;

        // Fogを水中だけ強く（wでブレンド）
        RenderSettings.fog = (w > 0.001f) || defaultFogEnabled;
        RenderSettings.fogColor = underwaterFogColor;
        RenderSettings.fogDensity = Mathf.Lerp(defaultFogDensity, underwaterFogDensity, w);

        bool on = Camera.main.transform.position.y < 0f;
        underwaterDustObj.SetActive(on);
        smallBubblesObj.SetActive(on);
    }
}
