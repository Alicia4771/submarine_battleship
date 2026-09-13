using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ColorSequenceInputUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ColorSequenceInputController colorSequenceInputController;
    [SerializeField] private CanvasGroup inputCanvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text inputText;
    [SerializeField] private TMP_Text countText;

    [Header("Display")]
    [SerializeField] private string title = "送信色";

    [SerializeField, Tooltip("入力済みの色を表示する丸。通常は●を使用する")]
    private string filledCircle = "●";

    [SerializeField, Tooltip("未入力部分を表示する丸。通常は○を使用する")]
    private string emptyCircle = "○";

    [SerializeField, Tooltip("各丸の間に入れる文字")]
    private string separator = "　";

    [Header("Circle Colors")]
    [SerializeField, Tooltip("赤信号を表示する丸の色")]
    private Color redColor = Color.red;

    [SerializeField, Tooltip("青信号を表示する丸の色")]
    private Color blueColor = Color.blue;

    [SerializeField, Tooltip("黄信号を表示する丸の色")]
    private Color yellowColor = Color.yellow;

    [SerializeField, Tooltip("まだ入力されていない丸の色")]
    private Color emptyCircleColor = new Color(0.75f, 0.75f, 0.75f, 1.0f);

    private void Awake()
    {
        ResolveReferences();

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (inputText != null)
        {
            inputText.richText = true;
        }

        SetVisible(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void ResolveReferences()
    {
        if (colorSequenceInputController == null)
        {
            colorSequenceInputController =
                FindFirstObjectByType<ColorSequenceInputController>();
        }
    }

    private void SubscribeEvents()
    {
        if (colorSequenceInputController == null)
        {
            return;
        }

        colorSequenceInputController.InputModeChanged -= HandleInputModeChanged;
        colorSequenceInputController.InputModeChanged += HandleInputModeChanged;

        colorSequenceInputController.EnteredColorsChanged -= HandleColorsChanged;
        colorSequenceInputController.EnteredColorsChanged += HandleColorsChanged;
    }

    private void UnsubscribeEvents()
    {
        if (colorSequenceInputController == null)
        {
            return;
        }

        colorSequenceInputController.InputModeChanged -= HandleInputModeChanged;
        colorSequenceInputController.EnteredColorsChanged -= HandleColorsChanged;
    }

    private void HandleInputModeChanged(bool enabled)
    {
        SetVisible(enabled);
    }

    private void HandleColorsChanged(
        IReadOnlyList<ColorSignalSymbol> colors,
        int expectedCount
    )
    {
        if (inputText != null)
        {
            inputText.text = BuildCircleText(colors, expectedCount);
        }

        if (countText != null)
        {
            int count = colors != null ? colors.Count : 0;

            countText.text =
                count +
                " / " +
                expectedCount;
        }
    }

    private string BuildCircleText(
        IReadOnlyList<ColorSignalSymbol> colors,
        int expectedCount
    )
    {
        StringBuilder builder = new StringBuilder();

        for (int index = 0; index < expectedCount; index++)
        {
            if (index > 0)
            {
                builder.Append(separator);
            }

            if (
                colors != null &&
                index < colors.Count
            )
            {
                builder.Append(
                    GetColoredCircleText(
                        colors[index]
                    )
                );
            }
            else
            {
                builder.Append(
                    GetRichTextColorTag(
                        emptyCircleColor,
                        emptyCircle
                    )
                );
            }
        }

        return builder.ToString();
    }

    private string GetColoredCircleText(
        ColorSignalSymbol color
    )
    {
        switch (color)
        {
            case ColorSignalSymbol.Red:
                return GetRichTextColorTag(redColor, filledCircle);

            case ColorSignalSymbol.Blue:
                return GetRichTextColorTag(blueColor, filledCircle);

            case ColorSignalSymbol.Yellow:
                return GetRichTextColorTag(yellowColor, filledCircle);

            default:
                return GetRichTextColorTag(emptyCircleColor, "?");
        }
    }

    private string GetRichTextColorTag(
        Color color,
        string text
    )
    {
        string htmlColor =
            ColorUtility.ToHtmlStringRGBA(color);

        return
            "<color=#" +
            htmlColor +
            ">" +
            text +
            "</color>";
    }

    private void SetVisible(bool visible)
    {
        if (inputCanvasGroup == null)
        {
            return;
        }

        inputCanvasGroup.alpha =
            visible
                ? 1.0f
                : 0.0f;

        inputCanvasGroup.interactable = false;
        inputCanvasGroup.blocksRaycasts = false;
    }
}
