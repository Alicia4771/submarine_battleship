using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CommunicationMastController : MonoBehaviour
{
    // ============================================================
    // 通信マスト状態
    // ============================================================

    public enum MastState
    {
        Lowered,
        Raising,
        Transmitting,
        Lowering
    }


    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultLoweredLocalY = 0.0f;
    private const float DefaultRaisedLocalY = 2.0f;

    private const float DefaultRaiseDuration = 0.5f;
    private const float DefaultTransmissionDuration = 2.0f;
    private const float DefaultLowerDuration = 0.5f;

    private const float MinimumDuration = 0.0f;

    private const float NormalizedMinimum = 0.0f;
    private const float NormalizedMaximum = 1.0f;

    private const float DurationEpsilon = 0.0001f;


    // ============================================================
    // Inspector設定
    // ============================================================

    [Header("Mast Transform")]

    [SerializeField, Tooltip(
        "上下移動させる通信マストのTransform。" +
        "未設定の場合はこのGameObject自身を使用する")]
    private Transform mastTransform;


    [SerializeField, Tooltip(
        "ゲーム開始時に通信マストを格納位置へ移動する")]
    private bool initializeAtLoweredPosition = true;


    // ============================================================
    // マスト位置
    // ============================================================

    [Header("Mast Position")]

    [SerializeField, Tooltip(
        "通信マスト格納時のLocal Y")]
    private float loweredLocalY =
        DefaultLoweredLocalY;


    [SerializeField, Tooltip(
        "通信マスト展開時のLocal Y")]
    private float raisedLocalY =
        DefaultRaisedLocalY;


    // ============================================================
    // 動作時間
    // ============================================================

    [Header("Mast Timing")]

    [SerializeField, Tooltip(
        "通信マストを展開するのにかかる時間")]
    [Min(MinimumDuration)]
    private float raiseDuration =
        DefaultRaiseDuration;


    [SerializeField, Tooltip(
        "通信マスト展開後、司令部へ送信する時間")]
    [Min(MinimumDuration)]
    private float transmissionDuration =
        DefaultTransmissionDuration;


    [SerializeField, Tooltip(
        "通信マストを格納するのにかかる時間")]
    [Min(MinimumDuration)]
    private float lowerDuration =
        DefaultLowerDuration;


    // ============================================================
    // 表示
    // ============================================================

    [Header("Visual")]

    [SerializeField, Tooltip(
        "ONの場合、Mast Transformを実際に上下移動させる。" +
        "OFFでも通信時間・状態管理は行われる")]
    private bool animateMast = true;


    // ============================================================
    // デバッグ
    // ============================================================

    [Header("Debug")]

    [SerializeField, Tooltip(
        "通信マストの状態をConsoleへ表示する")]
    private bool debugLog = true;


    // ============================================================
    // 内部状態
    // ============================================================

    private MastState currentState =
        MastState.Lowered;


    private Coroutine transmissionCoroutine;


    // ============================================================
    // イベント
    // ============================================================

    /// <summary>
    /// 通信マストが完全に上がり、
    /// 実際の送信を開始した時に発生する。
    /// </summary>
    public event Action TransmissionStarted;


    /// <summary>
    /// 通信が終了し、
    /// 通信マストの格納まで完了した時に発生する。
    /// </summary>
    public event Action TransmissionCompleted;


    /// <summary>
    /// マスト状態が変化した時に発生する。
    /// 第4段階の警戒度システムなどでも利用できる。
    /// </summary>
    public event Action<MastState> MastStateChanged;


    // ============================================================
    // Reset
    // ============================================================

    private void Reset()
    {
        mastTransform =
            transform;
    }


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ResolveReferences();

        ValidateSettings();
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        if (initializeAtLoweredPosition)
        {
            SetMastLocalY(
                loweredLocalY
            );
        }


        SetState(
            MastState.Lowered
        );
    }


    // ============================================================
    // 参照取得
    // ============================================================

    private void ResolveReferences()
    {
        if (mastTransform == null)
        {
            mastTransform =
                transform;
        }
    }


    // ============================================================
    // 通信開始
    // ============================================================

    /// <summary>
    /// 通信マストによる送信処理を開始する。
    ///
    /// 既に通信処理中の場合はfalseを返す。
    /// </summary>
    public bool TryStartTransmission()
    {
        if (
            transmissionCoroutine != null ||
            currentState != MastState.Lowered
        )
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    "通信マストは既に使用中です。"
                );
            }

            return false;
        }


        transmissionCoroutine =
            StartCoroutine(
                TransmissionRoutine()
            );


        return true;
    }


    // ============================================================
    // 通信処理
    // ============================================================

    private IEnumerator TransmissionRoutine()
    {
        // =========================
        // マスト展開
        // =========================

        SetState(
            MastState.Raising
        );


        if (debugLog)
        {
            Debug.Log(
                "通信マストを展開します。"
            );
        }


        yield return
            MoveMast(
                raisedLocalY,
                raiseDuration
            );


        // =========================
        // 送信
        // =========================

        SetState(
            MastState.Transmitting
        );


        if (debugLog)
        {
            Debug.Log(
                "司令部へ送信中..."
            );
        }


        TransmissionStarted?.Invoke();


        yield return
            WaitForDuration(
                transmissionDuration
            );


        // =========================
        // マスト格納
        // =========================

        SetState(
            MastState.Lowering
        );


        if (debugLog)
        {
            Debug.Log(
                "通信終了。通信マストを格納します。"
            );
        }


        yield return
            MoveMast(
                loweredLocalY,
                lowerDuration
            );


        // =========================
        // 完了
        // =========================

        SetState(
            MastState.Lowered
        );


        transmissionCoroutine =
            null;


        if (debugLog)
        {
            Debug.Log(
                "通信マストの格納が完了しました。"
            );
        }


        TransmissionCompleted?.Invoke();
    }


    // ============================================================
    // マスト移動
    // ============================================================

    private IEnumerator MoveMast(
        float targetLocalY,
        float duration
    )
    {
        // Transformが存在しない場合でも、
        // 機械的な動作時間だけは再現する
        if (mastTransform == null)
        {
            yield return
                WaitForDuration(
                    duration
                );

            yield break;
        }


        float startLocalY =
            mastTransform.localPosition.y;


        // アニメーション無効の場合
        if (!animateMast)
        {
            SetMastLocalY(
                targetLocalY
            );


            yield return
                WaitForDuration(
                    duration
                );


            yield break;
        }


        // 動作時間0なら即座に移動
        if (duration <= DurationEpsilon)
        {
            SetMastLocalY(
                targetLocalY
            );

            yield break;
        }


        float elapsedTime =
            0.0f;


        while (elapsedTime < duration)
        {
            elapsedTime +=
                Time.deltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    duration
                );


            // 急に動き始めたり止まったりしないよう
            // SmoothStepで補間する
            float smoothTime =
                Mathf.SmoothStep(
                    NormalizedMinimum,
                    NormalizedMaximum,
                    normalizedTime
                );


            float currentLocalY =
                Mathf.Lerp(
                    startLocalY,
                    targetLocalY,
                    smoothTime
                );


            SetMastLocalY(
                currentLocalY
            );


            yield return null;
        }


        // 浮動小数点誤差を防ぐため
        // 最後に正確な位置を設定する
        SetMastLocalY(
            targetLocalY
        );
    }


    // ============================================================
    // 指定時間待機
    // ============================================================

    private IEnumerator WaitForDuration(
        float duration
    )
    {
        if (duration <= DurationEpsilon)
        {
            yield break;
        }


        float elapsedTime =
            0.0f;


        while (elapsedTime < duration)
        {
            elapsedTime +=
                Time.deltaTime;

            yield return null;
        }
    }


    // ============================================================
    // マスト位置設定
    // ============================================================

    private void SetMastLocalY(
        float localY
    )
    {
        if (mastTransform == null)
        {
            return;
        }


        Vector3 localPosition =
            mastTransform.localPosition;


        localPosition.y =
            localY;


        mastTransform.localPosition =
            localPosition;
    }


    // ============================================================
    // 通信キャンセル
    // ============================================================

    /// <summary>
    /// シーン終了やミッション強制終了などで
    /// 通信を途中キャンセルする。
    ///
    /// キャンセル時は即座に格納位置へ戻す。
    /// TransmissionCompletedは発生させない。
    /// </summary>
    public void CancelTransmission()
    {
        if (transmissionCoroutine != null)
        {
            StopCoroutine(
                transmissionCoroutine
            );

            transmissionCoroutine =
                null;
        }


        SetMastLocalY(
            loweredLocalY
        );


        SetState(
            MastState.Lowered
        );


        if (debugLog)
        {
            Debug.Log(
                "通信をキャンセルし、通信マストを格納しました。"
            );
        }
    }


    // ============================================================
    // 状態変更
    // ============================================================

    private void SetState(
        MastState newState
    )
    {
        if (currentState == newState)
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
    // 状態取得
    // ============================================================

    public MastState GetCurrentState()
    {
        return
            currentState;
    }


    /// <summary>
    /// 通信マストが少しでも外へ出ている状態か。
    ///
    /// 第4段階の発見危険度に利用できる。
    /// </summary>
    public bool GetIsMastExposed()
    {
        return
            currentState !=
            MastState.Lowered;
    }


    /// <summary>
    /// 現在実際に送信中か。
    /// </summary>
    public bool GetIsTransmitting()
    {
        return
            currentState ==
            MastState.Transmitting;
    }


    public float GetTransmissionDuration()
    {
        return
            transmissionDuration;
    }


    // ============================================================
    // Inspector検証
    // ============================================================

    private void OnValidate()
    {
        ValidateSettings();
    }


    private void ValidateSettings()
    {
        raiseDuration =
            Mathf.Max(
                MinimumDuration,
                raiseDuration
            );


        transmissionDuration =
            Mathf.Max(
                MinimumDuration,
                transmissionDuration
            );


        lowerDuration =
            Mathf.Max(
                MinimumDuration,
                lowerDuration
            );
    }
}