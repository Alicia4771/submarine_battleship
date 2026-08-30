using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ColorSequenceInputUIController : MonoBehaviour
{
    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField]
    private ColorSequenceInputController
        colorSequenceInputController;


    [SerializeField]
    private CanvasGroup
        inputCanvasGroup;


    [SerializeField]
    private TMP_Text
        titleText;


    [SerializeField]
    private TMP_Text
        inputText;


    [SerializeField]
    private TMP_Text
        countText;


    // ============================================================
    // Display
    // ============================================================

    [Header("Display")]

    [SerializeField]
    private string title =
        "送信色";


    [SerializeField]
    private string emptyText =
        "_";


    [SerializeField]
    private string separator =
        "　";


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ResolveReferences();


        if (titleText != null)
        {
            titleText.text =
                title;
        }


        SetVisible(
            false
        );
    }


    // ============================================================
    // Enable
    // ============================================================

    private void OnEnable()
    {
        ResolveReferences();

        SubscribeEvents();
    }


    // ============================================================
    // Disable
    // ============================================================

    private void OnDisable()
    {
        UnsubscribeEvents();
    }


    // ============================================================
    // References
    // ============================================================

    private void ResolveReferences()
    {
        if (
            colorSequenceInputController ==
            null
        )
        {
            colorSequenceInputController =
                FindFirstObjectByType<
                    ColorSequenceInputController
                >();
        }
    }


    // ============================================================
    // Events
    // ============================================================

    private void SubscribeEvents()
    {
        if (
            colorSequenceInputController ==
            null
        )
        {
            return;
        }


        colorSequenceInputController
            .InputModeChanged -=
                HandleInputModeChanged;


        colorSequenceInputController
            .InputModeChanged +=
                HandleInputModeChanged;


        colorSequenceInputController
            .EnteredColorsChanged -=
                HandleColorsChanged;


        colorSequenceInputController
            .EnteredColorsChanged +=
                HandleColorsChanged;
    }


    private void UnsubscribeEvents()
    {
        if (
            colorSequenceInputController ==
            null
        )
        {
            return;
        }


        colorSequenceInputController
            .InputModeChanged -=
                HandleInputModeChanged;


        colorSequenceInputController
            .EnteredColorsChanged -=
                HandleColorsChanged;
    }


    // ============================================================
    // Input mode
    // ============================================================

    private void HandleInputModeChanged(
        bool enabled
    )
    {
        SetVisible(
            enabled
        );
    }


    // ============================================================
    // Colors
    // ============================================================

    private void HandleColorsChanged(
        IReadOnlyList<ColorSignalSymbol> colors,
        int expectedCount
    )
    {
        if (inputText != null)
        {
            inputText.text =
                BuildText(
                    colors,
                    expectedCount
                );
        }


        if (countText != null)
        {
            int count =
                colors != null
                    ? colors.Count
                    : 0;


            countText.text =
                count +
                " / " +
                expectedCount;
        }
    }


    // ============================================================
    // Text
    // ============================================================

    private string BuildText(
        IReadOnlyList<ColorSignalSymbol> colors,
        int expectedCount
    )
    {
        StringBuilder builder =
            new StringBuilder();


        for (
            int i = 0;
            i < expectedCount;
            i++
        )
        {
            if (i > 0)
            {
                builder.Append(
                    separator
                );
            }


            if (
                colors != null &&
                i < colors.Count
            )
            {
                builder.Append(
                    GetColorText(
                        colors[i]
                    )
                );
            }
            else
            {
                builder.Append(
                    emptyText
                );
            }
        }


        return builder.ToString();
    }


    private string GetColorText(
        ColorSignalSymbol color
    )
    {
        switch (color)
        {
            case ColorSignalSymbol.Red:
                return "赤";

            case ColorSignalSymbol.Blue:
                return "青";

            case ColorSignalSymbol.Yellow:
                return "黄";

            default:
                return "?";
        }
    }


    // ============================================================
    // Visibility
    // ============================================================

    private void SetVisible(
        bool visible
    )
    {
        if (
            inputCanvasGroup ==
            null
        )
        {
            return;
        }


        inputCanvasGroup.alpha =
            visible
                ? 1.0f
                : 0.0f;


        inputCanvasGroup.interactable =
            false;


        inputCanvasGroup.blocksRaycasts =
            false;
    }
}