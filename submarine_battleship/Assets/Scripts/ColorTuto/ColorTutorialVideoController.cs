using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class ColorTutorialVideoController : MonoBehaviour
{
    // ============================================================
    // Video Type
    // ============================================================

    public enum TutorialVideoType
    {
        None = 0,
        Sonar = 1,
        RaisePeriscope = 2,
        LowerPeriscope = 3,
        RotateLeft = 4,
        InputSignal = 5,
        ResetSignal = 6
    }


    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "TutorialManagerに付けたVideoPlayer")]
    private VideoPlayer videoPlayer;


    [SerializeField, Tooltip(
        "動画表示全体をまとめたPanel。非表示時にOFFにする")]
    private GameObject tutorialVideoPanel;


    // ============================================================
    // Video Clips
    // ============================================================

    [Header("Video Clips")]

    [SerializeField, Tooltip(
        "黒いボタンを押してソナーを表示する動画")]
    private VideoClip sonarVideo;


    [SerializeField, Tooltip(
        "赤いボタンを押して潜望鏡を上げる動画")]
    private VideoClip raisePeriscopeVideo;


    [SerializeField, Tooltip(
        "青いボタンを押して潜望鏡を下げる動画")]
    private VideoClip lowerPeriscopeVideo;


    [SerializeField, Tooltip(
        "潜望鏡を左へ回す動画")]
    private VideoClip rotateLeftVideo;


    [SerializeField, Tooltip(
        "赤→青→赤→赤の順に信号入力する動画")]
    private VideoClip inputSignalVideo;


    [SerializeField, Tooltip(
        "白いボタンを押して入力信号をリセットする動画")]
    private VideoClip resetSignalVideo;


    // ============================================================
    // Playback Settings
    // ============================================================

    [Header("Playback Settings")]

    [SerializeField, Tooltip(
        "チュートリアル動画をループ再生する")]
    private bool loopVideo =
        true;


    [SerializeField, Tooltip(
        "動画表示時に毎回先頭から再生する")]
    private bool restartFromBeginning =
        true;


    [SerializeField, Tooltip(
        "シーン開始時は動画Panelを非表示にする")]
    private bool hidePanelOnStart =
        true;


    // ============================================================
    // Test
    // ============================================================

    [Header("Test")]

    [SerializeField, Tooltip(
        "動画切替確認用。ONの場合、Start時にTest Videoを再生する")]
    private bool playTestVideoOnStart =
        false;


    [SerializeField, Tooltip(
        "テスト再生する動画")]
    private TutorialVideoType testVideo =
        TutorialVideoType.Sonar;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    // ============================================================
    // Internal
    // ============================================================

    private TutorialVideoType currentVideoType =
        TutorialVideoType.None;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ResolveReferences();

        ConfigureVideoPlayer();
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        if (hidePanelOnStart)
        {
            HideVideo();
        }


        if (playTestVideoOnStart)
        {
            PlayVideo(
                testVideo
            );
        }
    }


    // ============================================================
    // References
    // ============================================================

    private void ResolveReferences()
    {
        if (videoPlayer == null)
        {
            videoPlayer =
                GetComponent<VideoPlayer>();
        }
    }


    // ============================================================
    // Configure
    // ============================================================

    private void ConfigureVideoPlayer()
    {
        if (videoPlayer == null)
        {
            Debug.LogError(
                "ColorTutorialVideoController: " +
                "VideoPlayerが設定されていません。"
            );

            return;
        }


        videoPlayer.playOnAwake =
            false;


        videoPlayer.isLooping =
            loopVideo;


        // 動画ファイル側に音声トラックが残っていても
        // チュートリアル動画から音を出さない
        videoPlayer.audioOutputMode =
            VideoAudioOutputMode.None;
    }


    // ============================================================
    // Public Play
    // ============================================================

    public void PlayVideo(
        TutorialVideoType videoType
    )
    {
        VideoClip clip =
            GetVideoClip(
                videoType
            );


        if (clip == null)
        {
            Debug.LogWarning(
                "ColorTutorialVideoController: " +
                "指定された動画が設定されていません。VideoType = " +
                videoType
            );

            HideVideo();

            return;
        }


        if (videoPlayer == null)
        {
            ResolveReferences();

            ConfigureVideoPlayer();
        }


        if (videoPlayer == null)
        {
            return;
        }


        if (tutorialVideoPanel != null)
        {
            tutorialVideoPanel.SetActive(
                true
            );
        }


        bool clipChanged =
            videoPlayer.clip !=
            clip;


        if (clipChanged)
        {
            videoPlayer.Stop();

            videoPlayer.clip =
                clip;
        }


        videoPlayer.isLooping =
            loopVideo;


        videoPlayer.audioOutputMode =
            VideoAudioOutputMode.None;


        if (
            restartFromBeginning ||
            clipChanged
        )
        {
            videoPlayer.Stop();

            videoPlayer.clip =
                clip;

            videoPlayer.time =
                0.0;
        }


        currentVideoType =
            videoType;


        videoPlayer.Play();


        if (debugLog)
        {
            Debug.Log(
                "ColorTutorialVideoController: " +
                "動画再生 = " +
                videoType +
                " / " +
                clip.name
            );
        }
    }


    // ============================================================
    // Convenience Methods
    // ============================================================

    public void PlaySonarVideo()
    {
        PlayVideo(
            TutorialVideoType.Sonar
        );
    }


    public void PlayRaisePeriscopeVideo()
    {
        PlayVideo(
            TutorialVideoType.RaisePeriscope
        );
    }


    public void PlayLowerPeriscopeVideo()
    {
        PlayVideo(
            TutorialVideoType.LowerPeriscope
        );
    }


    public void PlayRotateLeftVideo()
    {
        PlayVideo(
            TutorialVideoType.RotateLeft
        );
    }


    public void PlayInputSignalVideo()
    {
        PlayVideo(
            TutorialVideoType.InputSignal
        );
    }


    public void PlayResetSignalVideo()
    {
        PlayVideo(
            TutorialVideoType.ResetSignal
        );
    }


    // ============================================================
    // Hide
    // ============================================================

    public void HideVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }


        if (tutorialVideoPanel != null)
        {
            tutorialVideoPanel.SetActive(
                false
            );
        }


        currentVideoType =
            TutorialVideoType.None;


        if (debugLog)
        {
            Debug.Log(
                "ColorTutorialVideoController: 動画を非表示"
            );
        }
    }


    // ============================================================
    // Getter
    // ============================================================

    public TutorialVideoType GetCurrentVideoType()
    {
        return
            currentVideoType;
    }


    public bool GetIsVideoVisible()
    {
        return
            tutorialVideoPanel != null &&
            tutorialVideoPanel.activeSelf;
    }


    // ============================================================
    // Clip
    // ============================================================

    private VideoClip GetVideoClip(
        TutorialVideoType videoType
    )
    {
        switch (videoType)
        {
            case TutorialVideoType.Sonar:

                return
                    sonarVideo;


            case TutorialVideoType.RaisePeriscope:

                return
                    raisePeriscopeVideo;


            case TutorialVideoType.LowerPeriscope:

                return
                    lowerPeriscopeVideo;


            case TutorialVideoType.RotateLeft:

                return
                    rotateLeftVideo;


            case TutorialVideoType.InputSignal:

                return
                    inputSignalVideo;


            case TutorialVideoType.ResetSignal:

                return
                    resetSignalVideo;


            default:

                return null;
        }
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        if (videoPlayer == null)
        {
            videoPlayer =
                GetComponent<VideoPlayer>();
        }


        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake =
                false;


            videoPlayer.isLooping =
                loopVideo;


            videoPlayer.audioOutputMode =
                VideoAudioOutputMode.None;
        }
    }
}
