using UnityEngine;
using UnityEngine.Rendering;

public class UnderwaterVolumeSwitcher : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;    // Main Camera を入れる
    [SerializeField]private Volume underwaterVolume;   // UnderwaterVolume を入れる

    void Reset()
    {
        targetCamera = Camera.main ? Camera.main.transform : null;
    }

    void Update()
    {
        if (!targetCamera || !underwaterVolume) return;

        underwaterVolume.weight = (targetCamera.position.y < 0f) ? 1f : 0f;
    }
}
