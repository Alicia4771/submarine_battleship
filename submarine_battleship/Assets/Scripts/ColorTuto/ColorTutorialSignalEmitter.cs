using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ColorTutorialSignalEmitter : MonoBehaviour
{
    // ============================================================
    // Sequence
    // ============================================================

    [Header("Fixed Tutorial Sequence")]

    [SerializeField, Tooltip(
        "チュートリアルで発光する固定色列")]
    private ColorSignalSymbol[] fixedSequence =
    {
        ColorSignalSymbol.Red,
        ColorSignalSymbol.Blue,
        ColorSignalSymbol.Red,
        ColorSignalSymbol.Red
    };


    // ============================================================
    // Signal Timing
    // ============================================================

    [Header("Signal Timing")]

    [SerializeField, Tooltip(
        "ONなら、StopSignal()が呼ばれるまで " +
        "白→固定色列 を無限に繰り返す。チュートリアルではON推奨")]
    private bool loopUntilStopped =
        true;


    [SerializeField, Min(1), Tooltip(
        "Loop Until StoppedがOFFの場合の繰り返し回数")]
    private int sequenceRepeatCount =
        1;


    [SerializeField, Tooltip(
        "各色列の開始前に白い開始合図を表示する")]
    private bool useStartMarker =
        true;


    [SerializeField, Min(0.0f), Tooltip(
        "白い開始合図の点灯時間")]
    private float startMarkerDuration =
        0.4f;


    [SerializeField, Min(0.0f), Tooltip(
        "白い開始合図後の消灯時間")]
    private float startMarkerBlankDuration =
        0.3f;


    [SerializeField, Min(0.0f), Tooltip(
        "各色の点灯時間")]
    private float colorDuration =
        0.6f;


    [SerializeField, Min(0.0f), Tooltip(
        "各色の間の消灯時間")]
    private float colorBlankDuration =
        0.25f;


    [SerializeField, Min(0.0f), Tooltip(
        "色列を複数回繰り返す場合の間隔")]
    private float repeatInterval =
        1.0f;


    // ============================================================
    // Colors
    // ============================================================

    [Header("Colors")]

    [SerializeField]
    private Color startMarkerColor =
        Color.white;


    [SerializeField]
    private Color redColor =
        Color.red;


    [SerializeField]
    private Color blueColor =
        Color.blue;


    [SerializeField]
    private Color yellowColor =
        Color.yellow;


    // ============================================================
    // Light
    // ============================================================

    [Header("Signal Light")]

    [SerializeField, Tooltip(
        "信号ライトを置くTransform。未設定ならこの敵艦Objectを基準にする")]
    private Transform signalOrigin;


    [SerializeField, Tooltip(
        "Signal Origin未設定時、またはOriginからのローカル位置")]
    private Vector3 signalLightLocalPosition =
        new Vector3(
            0.0f,
            3.0f,
            0.0f
        );


    [SerializeField, Min(0.0f), Tooltip(
        "信号ライトの明るさ")]
    private float signalIntensity =
        1500.0f;


    [SerializeField, Min(0.0f), Tooltip(
        "信号ライトの届く範囲")]
    private float signalRange =
        150.0f;


    [SerializeField, Tooltip(
        "使用するLightの種類")]
    private LightType signalLightType =
        LightType.Point;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    // ============================================================
    // Event
    // ============================================================

    /// <summary>
    /// 白い開始合図 + 固定色列を1周再生し終えた時に発生。
    /// intは完了した周回数（1, 2, 3 ...）。
    /// </summary>
    public event Action<int> SequenceCycleCompleted;


    /// <summary>
    /// Loop Until StoppedがOFFで、指定回数を最後まで再生した時に発生。
    /// </summary>
    public event Action SignalFinished;


    // ============================================================
    // Internal
    // ============================================================

    private Light signalLight;

    private Coroutine signalCoroutine;

    private bool isPlaying =
        false;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        CreateSignalLight();
    }


    // ============================================================
    // Disable
    // ============================================================

    private void OnDisable()
    {
        StopSignal();
    }


    // ============================================================
    // Light
    // ============================================================

    private void CreateSignalLight()
    {
        if (signalLight != null)
        {
            return;
        }


        Transform parent =
            signalOrigin != null
                ? signalOrigin
                : transform;


        GameObject lightObject =
            new GameObject(
                "ColorTutorialSignalLight"
            );


        lightObject.transform.SetParent(
            parent,
            false
        );


        lightObject.transform.localPosition =
            signalLightLocalPosition;


        signalLight =
            lightObject.AddComponent<Light>();


        signalLight.type =
            signalLightType;


        signalLight.intensity =
            signalIntensity;


        signalLight.range =
            signalRange;


        signalLight.enabled =
            false;
    }


    // ============================================================
    // Public
    // ============================================================

    public bool PlaySignal()
    {
        if (isPlaying)
        {
            return false;
        }


        if (
            fixedSequence == null ||
            fixedSequence.Length <= 0
        )
        {
            Debug.LogError(
                "ColorTutorialSignalEmitter: " +
                "Fixed Sequenceが設定されていません。"
            );

            return false;
        }


        if (signalLight == null)
        {
            CreateSignalLight();
        }


        signalCoroutine =
            StartCoroutine(
                SignalRoutine()
            );


        return true;
    }


    public void StopSignal()
    {
        if (signalCoroutine != null)
        {
            StopCoroutine(
                signalCoroutine
            );

            signalCoroutine =
                null;
        }


        isPlaying =
            false;


        if (signalLight != null)
        {
            signalLight.enabled =
                false;
        }
    }


    public IReadOnlyList<ColorSignalSymbol>
        GetFixedSequence()
    {
        return
            fixedSequence;
    }


    public bool GetIsPlaying()
    {
        return
            isPlaying;
    }


    // ============================================================
    // Routine
    // ============================================================

    private IEnumerator SignalRoutine()
    {
        isPlaying =
            true;


        int repeatCount =
            Mathf.Max(
                1,
                sequenceRepeatCount
            );


        int completedCycleCount =
            0;


        while (isPlaying)
        {
            // ====================================================
            // 白い開始合図
            // ====================================================

            if (useStartMarker)
            {
                yield return
                    Flash(
                        startMarkerColor,
                        startMarkerDuration,
                        startMarkerBlankDuration
                    );
            }


            // ====================================================
            // 固定色列
            // ====================================================

            for (
                int index = 0;
                index < fixedSequence.Length;
                index++
            )
            {
                yield return
                    Flash(
                        GetColor(
                            fixedSequence[index]
                        ),
                        colorDuration,
                        colorBlankDuration
                    );
            }


            // ====================================================
            // 1周完了
            // ====================================================

            completedCycleCount++;


            if (debugLog)
            {
                Debug.Log(
                    "ColorTutorialSignalEmitter: " +
                    "信号1周完了 / Cycle=" +
                    completedCycleCount
                );
            }


            SequenceCycleCompleted?.Invoke(
                completedCycleCount
            );


            // ====================================================
            // 有限再生の場合は指定回数で終了
            // ====================================================

            if (
                !loopUntilStopped &&
                completedCycleCount >=
                    repeatCount
            )
            {
                break;
            }


            // ====================================================
            // 次の周回までの間隔
            // ====================================================

            if (repeatInterval > 0.0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        repeatInterval
                    );
            }
        }


        if (signalLight != null)
        {
            signalLight.enabled =
                false;
        }


        signalCoroutine =
            null;


        isPlaying =
            false;


        if (debugLog)
        {
            Debug.Log(
                "ColorTutorialSignalEmitter: " +
                "固定色信号の有限再生が完了しました。"
            );
        }


        // Loop Until Stopped = ON の場合は、
        // StopSignal()がCoroutine自体を停止するため、
        // ここには通常到達しない。
        SignalFinished?.Invoke();
    }


    private IEnumerator Flash(
        Color color,
        float duration,
        float blankDuration
    )
    {
        if (signalLight == null)
        {
            yield break;
        }


        signalLight.color =
            color;


        signalLight.enabled =
            true;


        if (duration > 0.0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    duration
                );
        }


        signalLight.enabled =
            false;


        if (blankDuration > 0.0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    blankDuration
                );
        }
    }


    // ============================================================
    // Color
    // ============================================================

    private Color GetColor(
        ColorSignalSymbol symbol
    )
    {
        switch (symbol)
        {
            case ColorSignalSymbol.Red:

                return
                    redColor;


            case ColorSignalSymbol.Blue:

                return
                    blueColor;


            case ColorSignalSymbol.Yellow:

                return
                    yellowColor;


            default:

                return
                    Color.white;
        }
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        sequenceRepeatCount =
            Mathf.Max(
                1,
                sequenceRepeatCount
            );


        startMarkerDuration =
            Mathf.Max(
                0.0f,
                startMarkerDuration
            );


        startMarkerBlankDuration =
            Mathf.Max(
                0.0f,
                startMarkerBlankDuration
            );


        colorDuration =
            Mathf.Max(
                0.0f,
                colorDuration
            );


        colorBlankDuration =
            Mathf.Max(
                0.0f,
                colorBlankDuration
            );


        repeatInterval =
            Mathf.Max(
                0.0f,
                repeatInterval
            );


        signalIntensity =
            Mathf.Max(
                0.0f,
                signalIntensity
            );


        signalRange =
            Mathf.Max(
                0.0f,
                signalRange
            );
    }
}
