using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class DelayedVideoLoop : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("動画終了後、何秒待ってから再生し直すか")]
    [SerializeField] private float loopDelay = 30f;

    [Header("ゲーム開始時に自動再生するか")]
    [SerializeField] private bool playOnStart = true;

    [SerializeField, Tooltip("スタート文字のパネル")]
    private GameObject startTextPanel;

    private Coroutine loopCoroutine;

    private void Reset()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        // Unity標準の即時ループは使わない
        videoPlayer.isLooping = false;
        videoPlayer.playOnAwake = false;

        // 動画が最後まで再生された時に呼ばれる
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void Start()
    {
        // 最初はスタート文字を非表示にする
        if (startTextPanel != null)
        {
            startTextPanel.SetActive(false);
        }

        if (playOnStart)
        {
            videoPlayer.Play();
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        // 動画が終了したらスタート文字を表示
        if (startTextPanel != null)
        {
            startTextPanel.SetActive(true);
        }

        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
        }

        loopCoroutine = StartCoroutine(PlayAgainAfterDelay(vp));
    }

    private IEnumerator PlayAgainAfterDelay(VideoPlayer vp)
    {
        // 動画を停止
        vp.Stop();

        // Time.timeScaleの影響を受けない待機
        yield return new WaitForSecondsRealtime(loopDelay);

        // 動画を再生する直前にスタート文字を非表示
        if (startTextPanel != null)
        {
            startTextPanel.SetActive(false);
        }

        // 最初から再生
        vp.Play();

        loopCoroutine = null;
    }
}