using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class PeriscopeAmbientAudioController : MonoBehaviour
{
    [Serializable]
    public class AmbientAudioLayer
    {
        [Tooltip("このレイヤーでループ再生する音源")]
        public AudioClip clip;

        [Range(0.0f, 1.0f), Tooltip("この音源単体の最大音量")]
        public float volume = 1.0f;

        [Range(0.5f, 2.0f), Tooltip("再生速度。通常は1")]
        public float pitch = 1.0f;

        [Tooltip(
            "ONならゲーム開始時にランダムな位置から再生し、" +
            "複数ループ音源の開始位置が揃うのを防ぐ")]
        public bool randomizeStartPosition = true;
    }

    private class RuntimeLayer
    {
        public AmbientAudioLayer settings;
        public AudioSource source;
    }

    [Header("Surface Audio")]
    [SerializeField, Tooltip(
        "潜望鏡が海面より上にあるときに重ねて再生する音源。例: 波、風、船体のきしみ")]
    private AmbientAudioLayer[] surfaceLayers =
        Array.Empty<AmbientAudioLayer>();

    [Header("Underwater Audio")]
    [SerializeField, Tooltip(
        "潜望鏡が海水中にあるときに重ねて再生する音源。例: 泡、水中環境音、低い機械音")]
    private AmbientAudioLayer[] underwaterLayers =
        Array.Empty<AmbientAudioLayer>();

    [Header("Transition")]
    [SerializeField, Min(0.0f), Tooltip(
        "海面上／水中を切り替えるときのクロスフェード時間。0なら即時切替")]
    private float crossFadeDuration =
        1.0f;

    [SerializeField, Tooltip(
        "ゲーム開始時にもクロスフェードする。OFFなら開始時の状態へ即時設定")]
    private bool fadeOnStart =
        false;

    [SerializeField, Tooltip(
        "Time.timeScaleが0のとき、環境音を一時停止する")]
    private bool pauseWhenTimeScaleZero =
        false;

    [Header("AudioSource Common Settings")]

    [SerializeField, Range(0.0f, 1.0f), Tooltip(
        "海面上グループ全体の音量倍率")]
    private float surfaceMasterVolume =
        1.0f;

    [SerializeField, Range(0.0f, 1.0f), Tooltip(
        "水中グループ全体の音量倍率")]
    private float underwaterMasterVolume =
        1.0f;

    [SerializeField, Range(0.0f, 1.0f), Tooltip(
        "0 = 2D、1 = 完全な3D。プレイヤー環境音なら通常は0")]
    private float spatialBlend =
        0.0f;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog =
        false;

    private readonly List<RuntimeLayer>
        surfaceRuntimeLayers =
            new List<RuntimeLayer>();

    private readonly List<RuntimeLayer>
        underwaterRuntimeLayers =
            new List<RuntimeLayer>();

    private float surfaceBlend =
        0.0f;

    private float underwaterBlend =
        0.0f;

    private bool previousAboveSurface =
        false;

    private bool initialized =
        false;

    private bool sourcesPausedByController =
        false;

    private void Awake()
    {
        CreateRuntimeLayers(
            surfaceLayers,
            surfaceRuntimeLayers,
            "Surface"
        );

        CreateRuntimeLayers(
            underwaterLayers,
            underwaterRuntimeLayers,
            "Underwater"
        );
    }

    private void Start()
    {
        bool isAboveSurface =
            DataManager
                .GetIsPeriscopeAboveSurface();

        previousAboveSurface =
            isAboveSurface;

        if (fadeOnStart)
        {
            surfaceBlend =
                0.0f;

            underwaterBlend =
                0.0f;
        }
        else
        {
            surfaceBlend =
                isAboveSurface
                    ? 1.0f
                    : 0.0f;

            underwaterBlend =
                isAboveSurface
                    ? 0.0f
                    : 1.0f;
        }

        ApplyVolumes();

        StartAllLayers();

        initialized =
            true;

        if (debugLog)
        {
            Debug.Log(
                "PeriscopeAmbientAudioController: 初期状態 = " +
                (
                    isAboveSurface
                        ? "海面上"
                        : "水中"
                )
            );
        }
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            return;
        }

        UpdatePauseState();

        if (sourcesPausedByController)
        {
            return;
        }

        bool isAboveSurface =
            DataManager
                .GetIsPeriscopeAboveSurface();

        if (
            isAboveSurface !=
            previousAboveSurface
        )
        {
            previousAboveSurface =
                isAboveSurface;

            if (debugLog)
            {
                Debug.Log(
                    "PeriscopeAmbientAudioController: " +
                    (
                        isAboveSurface
                            ? "海面上の環境音へ切替"
                            : "水中の環境音へ切替"
                    )
                );
            }
        }

        float targetSurfaceBlend =
            isAboveSurface
                ? 1.0f
                : 0.0f;

        float targetUnderwaterBlend =
            isAboveSurface
                ? 0.0f
                : 1.0f;

        if (
            crossFadeDuration <=
            Mathf.Epsilon
        )
        {
            surfaceBlend =
                targetSurfaceBlend;

            underwaterBlend =
                targetUnderwaterBlend;
        }
        else
        {
            float blendStep =
                Time.unscaledDeltaTime /
                crossFadeDuration;

            surfaceBlend =
                Mathf.MoveTowards(
                    surfaceBlend,
                    targetSurfaceBlend,
                    blendStep
                );

            underwaterBlend =
                Mathf.MoveTowards(
                    underwaterBlend,
                    targetUnderwaterBlend,
                    blendStep
                );
        }

        ApplyVolumes();
    }

    private void CreateRuntimeLayers(
        AmbientAudioLayer[] settingsArray,
        List<RuntimeLayer> runtimeList,
        string groupName
    )
    {
        runtimeList.Clear();

        if (settingsArray == null)
        {
            return;
        }

        for (
            int index = 0;
            index < settingsArray.Length;
            index++
        )
        {
            AmbientAudioLayer settings =
                settingsArray[index];

            if (
                settings == null ||
                settings.clip == null
            )
            {
                continue;
            }

            AudioSource source =
                gameObject
                    .AddComponent<AudioSource>();

            source.clip =
                settings.clip;

            source.playOnAwake =
                false;

            source.loop =
                true;

            source.volume =
                0.0f;

            source.pitch =
                settings.pitch;

            source.spatialBlend =
                spatialBlend;

            source.dopplerLevel =
                0.0f;

            RuntimeLayer runtimeLayer =
                new RuntimeLayer
                {
                    settings =
                        settings,

                    source =
                        source
                };

            runtimeList.Add(
                runtimeLayer
            );

            if (debugLog)
            {
                Debug.Log(
                    "PeriscopeAmbientAudioController: " +
                    groupName +
                    " Layer " +
                    index +
                    " = " +
                    settings.clip.name
                );
            }
        }
    }

    private void StartAllLayers()
    {
        StartRuntimeLayers(
            surfaceRuntimeLayers
        );

        StartRuntimeLayers(
            underwaterRuntimeLayers
        );
    }

    private void StartRuntimeLayers(
        List<RuntimeLayer> runtimeLayers
    )
    {
        for (
            int index = 0;
            index < runtimeLayers.Count;
            index++
        )
        {
            RuntimeLayer runtimeLayer =
                runtimeLayers[index];

            if (
                runtimeLayer == null ||
                runtimeLayer.settings == null ||
                runtimeLayer.source == null ||
                runtimeLayer.settings.clip == null
            )
            {
                continue;
            }

            AudioClip clip =
                runtimeLayer.settings.clip;

            if (
                runtimeLayer.settings
                    .randomizeStartPosition &&
                clip.length >
                Mathf.Epsilon
            )
            {
                runtimeLayer.source.time =
                    UnityEngine.Random.Range(
                        0.0f,
                        clip.length
                    );
            }

            runtimeLayer
                .source
                .Play();
        }
    }

    private void ApplyVolumes()
    {
        ApplyGroupVolume(
            surfaceRuntimeLayers,
            surfaceBlend,
            surfaceMasterVolume
        );

        ApplyGroupVolume(
            underwaterRuntimeLayers,
            underwaterBlend,
            underwaterMasterVolume
        );
    }

    private void ApplyGroupVolume(
        List<RuntimeLayer> runtimeLayers,
        float blend,
        float masterVolume
    )
    {
        for (
            int index = 0;
            index < runtimeLayers.Count;
            index++
        )
        {
            RuntimeLayer runtimeLayer =
                runtimeLayers[index];

            if (
                runtimeLayer == null ||
                runtimeLayer.settings == null ||
                runtimeLayer.source == null
            )
            {
                continue;
            }

            runtimeLayer.source.volume =
                Mathf.Clamp01(
                    runtimeLayer.settings.volume *
                    masterVolume *
                    blend
                );
        }
    }

    private void UpdatePauseState()
    {
        bool shouldPause =
            pauseWhenTimeScaleZero &&
            Time.timeScale <=
            Mathf.Epsilon;

        if (
            shouldPause ==
            sourcesPausedByController
        )
        {
            return;
        }

        sourcesPausedByController =
            shouldPause;

        if (sourcesPausedByController)
        {
            PauseRuntimeLayers(
                surfaceRuntimeLayers
            );

            PauseRuntimeLayers(
                underwaterRuntimeLayers
            );
        }
        else
        {
            UnPauseRuntimeLayers(
                surfaceRuntimeLayers
            );

            UnPauseRuntimeLayers(
                underwaterRuntimeLayers
            );
        }
    }

    private void PauseRuntimeLayers(
        List<RuntimeLayer> runtimeLayers
    )
    {
        for (
            int index = 0;
            index < runtimeLayers.Count;
            index++
        )
        {
            AudioSource source =
                runtimeLayers[index]
                    .source;

            if (source != null)
            {
                source.Pause();
            }
        }
    }

    private void UnPauseRuntimeLayers(
        List<RuntimeLayer> runtimeLayers
    )
    {
        for (
            int index = 0;
            index < runtimeLayers.Count;
            index++
        )
        {
            AudioSource source =
                runtimeLayers[index]
                    .source;

            if (source != null)
            {
                source.UnPause();
            }
        }
    }

    private void OnDisable()
    {
        StopRuntimeLayers(
            surfaceRuntimeLayers
        );

        StopRuntimeLayers(
            underwaterRuntimeLayers
        );
    }

    private void StopRuntimeLayers(
        List<RuntimeLayer> runtimeLayers
    )
    {
        for (
            int index = 0;
            index < runtimeLayers.Count;
            index++
        )
        {
            AudioSource source =
                runtimeLayers[index]
                    .source;

            if (source != null)
            {
                source.Stop();
            }
        }
    }

    private void OnValidate()
    {
        crossFadeDuration =
            Mathf.Max(
                0.0f,
                crossFadeDuration
            );

        surfaceMasterVolume =
            Mathf.Clamp01(
                surfaceMasterVolume
            );

        underwaterMasterVolume =
            Mathf.Clamp01(
                underwaterMasterVolume
            );

        spatialBlend =
            Mathf.Clamp01(
                spatialBlend
            );
    }
}