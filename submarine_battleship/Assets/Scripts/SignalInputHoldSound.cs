using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class SignalInputHoldSound : MonoBehaviour
{
    public enum SignalInputMode
    {
        Auto = 0,
        NormalSignal = 1,
        ColorMemory = 2
    }

    [Header("References")]
    [SerializeField] private CommunicationMissionManager communicationMissionManager;
    [SerializeField] private ColorMemoryMissionManager colorMemoryMissionManager;

    [Header("Mode")]
    [SerializeField] private SignalInputMode inputMode = SignalInputMode.Auto;

    [Header("Audio")]
    [SerializeField] private AudioClip signalButtonClip;
    [SerializeField, Range(0.0f, 1.0f)] private float volume = 1.0f;
    [SerializeField] private bool stopWhenPaused = true;

    [Header("Initial Release Guard")]
    [SerializeField, Tooltip(
        "信号入力モードに入った時点で対象ボタンが既に押されている場合、" +
        "一度すべて離されるまで入力音を鳴らさない")]
    private bool requireReleaseBeforeFirstSound = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private AudioSource audioSource;
    private bool previousSignalInputState = false;
    private bool waitingForInitialRelease = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource();
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        StopLoopSound();
        previousSignalInputState = false;
        waitingForInitialRelease = false;
    }

    private void Update()
    {
        if (
            stopWhenPaused &&
            Time.timeScale <= Mathf.Epsilon
        )
        {
            StopLoopSound();
            return;
        }

        bool isSignalInputState =
            GetIsSignalInputState();

        if (
            isSignalInputState &&
            !previousSignalInputState
        )
        {
            waitingForInitialRelease =
                requireReleaseBeforeFirstSound &&
                GetIsSignalButtonHeld();

            if (
                debugLog &&
                waitingForInitialRelease
            )
            {
                Debug.Log(
                    "信号入力開始時にボタンが押されたままなので、" +
                    "一度離されるまで入力音を抑制します。"
                );
            }
        }

        if (!isSignalInputState)
        {
            previousSignalInputState = false;
            waitingForInitialRelease = false;
            StopLoopSound();
            return;
        }

        previousSignalInputState = true;

        if (waitingForInitialRelease)
        {
            if (GetIsSignalButtonHeld())
            {
                StopLoopSound();
                return;
            }

            waitingForInitialRelease = false;

            if (debugLog)
            {
                Debug.Log(
                    "信号入力開始前のボタン解放を確認しました。" +
                    "次の押下から入力音を鳴らします。"
                );
            }

            StopLoopSound();
            return;
        }

        if (GetIsSignalButtonHeld())
        {
            StartLoopSound();
        }
        else
        {
            StopLoopSound();
        }
    }

    private void OnDisable()
    {
        previousSignalInputState = false;
        waitingForInitialRelease = false;
        StopLoopSound();
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.clip = signalButtonClip;
    }

    private void ResolveReferences()
    {
        if (communicationMissionManager == null)
        {
            communicationMissionManager =
                FindFirstObjectByType<CommunicationMissionManager>();
        }

        if (colorMemoryMissionManager == null)
        {
            colorMemoryMissionManager =
                FindFirstObjectByType<ColorMemoryMissionManager>();
        }
    }

    private bool GetIsSignalInputState()
    {
        SignalInputMode resolvedMode =
            ResolveInputMode();

        switch (resolvedMode)
        {
            case SignalInputMode.NormalSignal:
                return
                    communicationMissionManager != null &&
                    communicationMissionManager.GetCurrentState() ==
                    CommunicationMissionManager.MissionState.Inputting;

            case SignalInputMode.ColorMemory:
                return
                    colorMemoryMissionManager != null &&
                    colorMemoryMissionManager.GetCurrentState() ==
                    ColorMemoryMissionManager.MissionState.Inputting;

            default:
                return false;
        }
    }

    private bool GetIsSignalButtonHeld()
    {
        SignalInputMode resolvedMode =
            ResolveInputMode();

        switch (resolvedMode)
        {
            case SignalInputMode.NormalSignal:
                return
                    DataManager.GetSensorButton4() == 1;

            case SignalInputMode.ColorMemory:
                return
                    DataManager.GetSensorButton2() == 1 ||
                    DataManager.GetSensorButton3() == 1 ||
                    DataManager.GetSensorButton4() == 1;

            default:
                return false;
        }
    }

    private SignalInputMode ResolveInputMode()
    {
        if (inputMode != SignalInputMode.Auto)
        {
            return inputMode;
        }

        if (
            colorMemoryMissionManager != null &&
            colorMemoryMissionManager.isActiveAndEnabled
        )
        {
            return SignalInputMode.ColorMemory;
        }

        if (
            communicationMissionManager != null &&
            communicationMissionManager.isActiveAndEnabled
        )
        {
            return SignalInputMode.NormalSignal;
        }

        return SignalInputMode.Auto;
    }

    private void StartLoopSound()
    {
        if (
            audioSource == null ||
            signalButtonClip == null
        )
        {
            return;
        }

        audioSource.volume = volume;
        audioSource.loop = true;

        if (audioSource.clip != signalButtonClip)
        {
            audioSource.clip = signalButtonClip;
        }

        if (audioSource.isPlaying)
        {
            return;
        }

        audioSource.Play();

        if (debugLog)
        {
            Debug.Log("信号入力音 再生開始");
        }
    }

    private void StopLoopSound()
    {
        if (
            audioSource == null ||
            !audioSource.isPlaying
        )
        {
            return;
        }

        audioSource.Stop();

        if (debugLog)
        {
            Debug.Log("信号入力音 再生停止");
        }
    }

    private void OnValidate()
    {
        volume = Mathf.Clamp01(volume);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.volume = volume;

            if (signalButtonClip != null)
            {
                audioSource.clip = signalButtonClip;
            }
        }
    }
}
