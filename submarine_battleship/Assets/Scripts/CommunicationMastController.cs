using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CommunicationMastController : MonoBehaviour
{
    // ============================================================
    // State
    // ============================================================

    public enum MastState
    {
        Lowered = 0,
        Raising = 1,
        Transmitting = 2,
        Lowering = 3
    }


    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultRaiseHeight =
        1.0f;

    private const float DefaultRaiseDuration =
        0.5f;

    private const float DefaultTransmissionDuration =
        2.0f;

    private const float DefaultLowerDuration =
        0.5f;

    private const float MinimumNonNegativeValue =
        0.0f;


    // ============================================================
    // Event
    // ============================================================

    public event Action<MastState>
        MastStateChanged;


    public event Action
        TransmissionCompleted;


    // ============================================================
    // Mast
    // ============================================================

    [Header("Mast")]

    [SerializeField, Tooltip(
        "上下移動させる通信マスト。" +
        "未設定の場合はこのGameObject自身を使用する")]
    private Transform mastTransform;


    [SerializeField, Tooltip(
        "格納位置からどれだけ上へ伸ばすか")]
    [Min(MinimumNonNegativeValue)]
    private float raiseHeight =
        DefaultRaiseHeight;


    // ============================================================
    // Timing
    // ============================================================

    [Header("Timing")]

    [SerializeField, Tooltip(
        "通信マストを上げる時間")]
    [Min(MinimumNonNegativeValue)]
    private float raiseDuration =
        DefaultRaiseDuration;


    [SerializeField, Tooltip(
        "通信マストが上がった状態で送信する時間")]
    [Min(MinimumNonNegativeValue)]
    private float transmissionDuration =
        DefaultTransmissionDuration;


    [SerializeField, Tooltip(
        "通信マストを下げる時間")]
    [Min(MinimumNonNegativeValue)]
    private float lowerDuration =
        DefaultLowerDuration;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        true;


    // ============================================================
    // 内部状態
    // ============================================================

    [SerializeField]
    private MastState currentState =
        MastState.Lowered;


    private Vector3 loweredLocalPosition;

    private Vector3 raisedLocalPosition;


    private Coroutine
        transmissionCoroutine;


    private Action
        completionCallback;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        if (mastTransform == null)
        {
            mastTransform =
                transform;
        }


        // シーン上で設定してある現在位置を
        // 完全格納位置として扱う
        loweredLocalPosition =
            mastTransform.localPosition;


        raisedLocalPosition =
            loweredLocalPosition +
            Vector3.up *
            raiseHeight;


        currentState =
            MastState.Lowered;
    }


    // ============================================================
    // Transmission開始
    // ============================================================

    public bool BeginTransmission()
    {
        return
            BeginTransmission(
                null
            );
    }


    public bool BeginTransmission(
        Action onCompleted
    )
    {
        if (
            currentState !=
            MastState.Lowered
        )
        {
            return false;
        }


        if (
            transmissionCoroutine !=
            null
        )
        {
            return false;
        }


        completionCallback =
            onCompleted;


        transmissionCoroutine =
            StartCoroutine(
                TransmissionRoutine()
            );


        return true;
    }


    // 以前のコードなどから呼びやすいように
    // Aliasも用意
    public bool StartTransmission()
    {
        return
            BeginTransmission();
    }


    public bool StartTransmissionSequence()
    {
        return
            BeginTransmission();
    }


    // ============================================================
    // 通信処理
    // ============================================================

    private IEnumerator TransmissionRoutine()
    {
        // ========================================================
        // 上昇
        // ========================================================

        SetState(
            MastState.Raising
        );


        if (debugLog)
        {
            Debug.Log(
                "通信マスト展開開始"
            );
        }


        yield return
            MoveMastRoutine(
                mastTransform.localPosition,
                raisedLocalPosition,
                raiseDuration
            );


        mastTransform.localPosition =
            raisedLocalPosition;


        // ========================================================
        // 送信
        // ========================================================

        SetState(
            MastState.Transmitting
        );


        if (debugLog)
        {
            Debug.Log(
                "通信マスト送信中"
            );
        }


        if (
            transmissionDuration >
            MinimumNonNegativeValue
        )
        {
            yield return
                new WaitForSeconds(
                    transmissionDuration
                );
        }


        // ========================================================
        // 下降
        // ========================================================

        SetState(
            MastState.Lowering
        );


        if (debugLog)
        {
            Debug.Log(
                "通信マスト格納開始"
            );
        }


        yield return
            MoveMastRoutine(
                mastTransform.localPosition,
                loweredLocalPosition,
                lowerDuration
            );


        mastTransform.localPosition =
            loweredLocalPosition;


        // ========================================================
        // 完全格納
        // ========================================================

        SetState(
            MastState.Lowered
        );


        if (debugLog)
        {
            Debug.Log(
                "通信マスト完全格納"
            );
        }


        transmissionCoroutine =
            null;


        Action callback =
            completionCallback;


        completionCallback =
            null;


        // ========================================================
        // 通信終了Event
        // ========================================================

        TransmissionCompleted?.Invoke();


        callback?.Invoke();
    }


    // ============================================================
    // Mast移動
    // ============================================================

    private IEnumerator MoveMastRoutine(
        Vector3 startPosition,
        Vector3 endPosition,
        float duration
    )
    {
        if (mastTransform == null)
        {
            yield break;
        }


        if (
            duration <=
            MinimumNonNegativeValue
        )
        {
            mastTransform.localPosition =
                endPosition;


            yield break;
        }


        float elapsedTime =
            MinimumNonNegativeValue;


        while (
            elapsedTime <
            duration
        )
        {
            elapsedTime +=
                Time.deltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    duration
                );


            mastTransform.localPosition =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    normalizedTime
                );


            yield return
                null;
        }


        mastTransform.localPosition =
            endPosition;
    }


    // ============================================================
    // State変更
    // ============================================================

    private void SetState(
        MastState newState
    )
    {
        if (
            currentState ==
            newState
        )
        {
            return;
        }


        currentState =
            newState;


        MastStateChanged?.Invoke(
            currentState
        );
    }


    // ============================================================
    // Getter
    // ============================================================

    public MastState GetCurrentState()
    {
        return
            currentState;
    }


    public bool GetIsMastExposed()
    {
        // Lowered以外は、
        // 上昇途中・送信中・下降途中を含め
        // 敵に見つかる可能性がある
        return
            currentState !=
            MastState.Lowered;
    }


    public bool GetIsBusy()
    {
        return
            currentState !=
            MastState.Lowered;
    }


    public bool GetIsFullyLowered()
    {
        return
            currentState ==
            MastState.Lowered;
    }


    public float GetTransmissionDuration()
    {
        return
            transmissionDuration;
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        raiseHeight =
            Mathf.Max(
                MinimumNonNegativeValue,
                raiseHeight
            );


        raiseDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                raiseDuration
            );


        transmissionDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                transmissionDuration
            );


        lowerDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                lowerDuration
            );
    }
}