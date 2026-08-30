using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class SignalInputUIController : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const float HiddenAlpha =
        0.0f;

    private const float VisibleAlpha =
        1.0f;


    private const string DefaultTitle =
        "送信信号";

    private const string DefaultShortSymbolText =
        "・";

    private const string DefaultLongSymbolText =
        "―";

    private const string DefaultEmptySlotText =
        "_";

    private const string DefaultSeparatorText =
        " ";


    // ============================================================
    // Signal Input
    // ============================================================

    [Header("Signal Input")]

    [SerializeField, Tooltip(
        "Button4による信号入力を管理するSignalInputController。" +
        "未設定の場合はシーン内から自動検索する")]
    private SignalInputController
        signalInputController;


    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]

    [SerializeField, Tooltip(
        "信号入力UI全体のCanvasGroup。" +
        "このGameObjectを非Activeにはせず、CanvasGroupで表示・非表示を切り替える")]
    private CanvasGroup
        inputCanvasGroup;


    [SerializeField, Tooltip(
        "「送信信号」などのタイトルを表示するTextMeshPro。" +
        "不要な場合は未設定でもよい")]
    private TMP_Text
        titleText;


    [SerializeField, Tooltip(
        "「・ ― _ _」のように現在の入力内容を表示するTextMeshPro")]
    private TMP_Text
        inputText;


    [SerializeField, Tooltip(
        "「2 / 4」のように入力数を表示するTextMeshPro。" +
        "不要な場合は未設定でもよい")]
    private TMP_Text
        countText;


    // ============================================================
    // Display
    // ============================================================

    [Header("Display")]

    [SerializeField, Tooltip(
        "タイトルとして表示する文字")]
    private string title =
        DefaultTitle;


    [SerializeField, Tooltip(
        "短信号を表す文字")]
    private string shortSymbolText =
        DefaultShortSymbolText;


    [SerializeField, Tooltip(
        "長信号を表す文字")]
    private string longSymbolText =
        DefaultLongSymbolText;


    [SerializeField, Tooltip(
        "まだ入力していない場所を表す文字")]
    private string emptySlotText =
        DefaultEmptySlotText;


    [SerializeField, Tooltip(
        "各記号の間に入れる文字。全角空白を推奨")]
    private string separatorText =
        DefaultSeparatorText;


    [SerializeField, Tooltip(
        "未入力の場所を「_」などで表示する")]
    private bool showEmptySlots =
        true;


    [SerializeField, Tooltip(
        "入力数を「2 / 4」の形式で表示する")]
    private bool showInputCount =
        true;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ResolveReferences();

        SetupStaticText();

        SetVisible(
            false
        );
    }


    // ============================================================
    // OnEnable
    // ============================================================

    private void OnEnable()
    {
        ResolveReferences();

        SubscribeEvents();
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ResolveReferences();

        SubscribeEvents();

        RefreshFromController();
    }


    // ============================================================
    // OnDisable
    // ============================================================

    private void OnDisable()
    {
        UnsubscribeEvents();
    }


    // ============================================================
    // 参照取得
    // ============================================================

    private void ResolveReferences()
    {
        if (signalInputController == null)
        {
            signalInputController =
                FindFirstObjectByType<
                    SignalInputController
                >();
        }


        if (
            signalInputController == null &&
            debugLog
        )
        {
            Debug.LogWarning(
                "SignalInputUIController: " +
                "SignalInputControllerが見つかりません。"
            );
        }
    }


    // ============================================================
    // Event登録
    // ============================================================

    private void SubscribeEvents()
    {
        if (signalInputController == null)
        {
            return;
        }


        // 二重登録防止
        signalInputController
            .InputModeChanged -=
                HandleInputModeChanged;


        signalInputController
            .InputModeChanged +=
                HandleInputModeChanged;


        signalInputController
            .EnteredSignalsChanged -=
                HandleEnteredSignalsChanged;


        signalInputController
            .EnteredSignalsChanged +=
                HandleEnteredSignalsChanged;
    }


    // ============================================================
    // Event解除
    // ============================================================

    private void UnsubscribeEvents()
    {
        if (signalInputController == null)
        {
            return;
        }


        signalInputController
            .InputModeChanged -=
                HandleInputModeChanged;


        signalInputController
            .EnteredSignalsChanged -=
                HandleEnteredSignalsChanged;
    }


    // ============================================================
    // 入力モード変更
    // ============================================================

    private void HandleInputModeChanged(
        bool isInputting
    )
    {
        if (isInputting)
        {
            RefreshFromController();

            SetVisible(
                true
            );


            if (debugLog)
            {
                Debug.Log(
                    "SignalInputUI: 表示開始"
                );
            }


            return;
        }


        SetVisible(
            false
        );


        if (debugLog)
        {
            Debug.Log(
                "SignalInputUI: 表示終了"
            );
        }
    }


    // ============================================================
    // 入力内容変更
    // ============================================================

    private void HandleEnteredSignalsChanged(
        IReadOnlyList<SignalSymbol> signals,
        int expectedCount
    )
    {
        UpdateDisplay(
            signals,
            expectedCount
        );
    }


    // ============================================================
    // 現在値から再描画
    // ============================================================

    private void RefreshFromController()
    {
        if (signalInputController == null)
        {
            SetVisible(
                false
            );

            return;
        }


        UpdateDisplay(
            signalInputController
                .GetEnteredSignals(),
            signalInputController
                .GetExpectedSignalCountForDisplay()
        );


        SetVisible(
            signalInputController
                .GetIsInputEnabled()
        );
    }


    // ============================================================
    // 表示更新
    // ============================================================

    private void UpdateDisplay(
        IReadOnlyList<SignalSymbol> signals,
        int expectedCount
    )
    {
        int enteredCount =
            signals != null
                ? signals.Count
                : 0;


        if (inputText != null)
        {
            inputText.text =
                BuildSignalText(
                    signals,
                    expectedCount
                );
        }


        if (countText != null)
        {
            if (showInputCount)
            {
                countText.gameObject.SetActive(
                    true
                );


                countText.text =
                    enteredCount +
                    " / " +
                    Mathf.Max(
                        0,
                        expectedCount
                    );
            }
            else
            {
                countText.gameObject.SetActive(
                    false
                );
            }
        }
    }


    // ============================================================
    // 信号文字列作成
    // ============================================================

    private string BuildSignalText(
        IReadOnlyList<SignalSymbol> signals,
        int expectedCount
    )
    {
        int enteredCount =
            signals != null
                ? signals.Count
                : 0;


        int slotCount;


        if (showEmptySlots)
        {
            slotCount =
                Mathf.Max(
                    enteredCount,
                    expectedCount
                );
        }
        else
        {
            slotCount =
                enteredCount;
        }


        if (slotCount <= 0)
        {
            return string.Empty;
        }


        StringBuilder builder =
            new StringBuilder();


        for (
            int signalIndex = 0;
            signalIndex < slotCount;
            signalIndex++
        )
        {
            if (signalIndex > 0)
            {
                builder.Append(
                    separatorText
                );
            }


            if (
                signals != null &&
                signalIndex < signals.Count
            )
            {
                builder.Append(
                    ConvertSignalSymbolToText(
                        signals[signalIndex]
                    )
                );
            }
            else
            {
                builder.Append(
                    emptySlotText
                );
            }
        }


        return
            builder.ToString();
    }


    // ============================================================
    // 1記号を表示文字へ変換
    // ============================================================

    private string ConvertSignalSymbolToText(
        SignalSymbol signalSymbol
    )
    {
        switch (signalSymbol)
        {
            case SignalSymbol.Short:

                return
                    shortSymbolText;


            case SignalSymbol.Long:

                return
                    longSymbolText;


            default:

                return
                    "?";
        }
    }


    // ============================================================
    // 固定表示
    // ============================================================

    private void SetupStaticText()
    {
        if (titleText != null)
        {
            titleText.text =
                title;
        }
    }


    // ============================================================
    // 表示 / 非表示
    // ============================================================

    private void SetVisible(
        bool visible
    )
    {
        if (inputCanvasGroup == null)
        {
            return;
        }


        inputCanvasGroup.alpha =
            visible
                ? VisibleAlpha
                : HiddenAlpha;


        // このUIは表示専用なので操作入力を受け取らない
        inputCanvasGroup.interactable =
            false;


        inputCanvasGroup.blocksRaycasts =
            false;
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(title))
        {
            title =
                DefaultTitle;
        }


        if (string.IsNullOrEmpty(shortSymbolText))
        {
            shortSymbolText =
                DefaultShortSymbolText;
        }


        if (string.IsNullOrEmpty(longSymbolText))
        {
            longSymbolText =
                DefaultLongSymbolText;
        }


        if (string.IsNullOrEmpty(emptySlotText))
        {
            emptySlotText =
                DefaultEmptySlotText;
        }


        if (separatorText == null)
        {
            separatorText =
                DefaultSeparatorText;
        }
    }
}
