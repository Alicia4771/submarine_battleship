using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class TutorialSonarControllerV2 : MonoBehaviour
{
    // ============================================================
    // UI
    // ============================================================

    [Header("Sonar UI")]

    [SerializeField, Tooltip(
        "Canvas内のSonarPanel")]
    private GameObject sonarPanel;


    // ============================================================
    // Keyboard
    // ============================================================

    [Header("Keyboard Test")]

    [SerializeField, Tooltip(
        "Unity上でSpaceキーによるソナー操作を許可する")]
    private bool allowSpaceKey =
        true;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    // ============================================================
    // 状態
    // ============================================================

    private bool inputEnabled =
        false;


    private bool isSonarPanelOpen =
        false;


    private bool waitingForRelease =
        false;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        SetSonarPanel(
            false
        );
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (!inputEnabled)
        {
            SetSonarPanel(
                false
            );

            return;
        }


        // ========================================================
        // 潜望鏡が完全格納されている必要がある
        // ========================================================

        if (
            !DataManager
                .GetIsPeriscopeFullyLowered()
        )
        {
            SetSonarPanel(
                false
            );

            return;
        }


        bool buttonPressed =
            DataManager
                .GetSensorButton1()
            ==
            1;


        bool spacePressed =
            allowSpaceKey &&
            Keyboard.current != null &&
            Keyboard.current
                .spaceKey
                .isPressed;


        // ========================================================
        // 有効化された瞬間にButton1が押しっぱなしだった場合
        // ========================================================

        if (waitingForRelease)
        {
            if (
                !buttonPressed &&
                !spacePressed
            )
            {
                waitingForRelease =
                    false;
            }


            SetSonarPanel(
                false
            );


            return;
        }


        SetSonarPanel(
            buttonPressed ||
            spacePressed
        );
    }


    // ============================================================
    // 入力有効 / 無効
    // ============================================================

    public void SetInputEnabled(
        bool enabled
    )
    {
        inputEnabled =
            enabled;


        if (!inputEnabled)
        {
            waitingForRelease =
                false;


            SetSonarPanel(
                false
            );


            return;
        }


        bool buttonPressed =
            DataManager
                .GetSensorButton1()
            ==
            1;


        bool spacePressed =
            allowSpaceKey &&
            Keyboard.current != null &&
            Keyboard.current
                .spaceKey
                .isPressed;


        waitingForRelease =
            buttonPressed ||
            spacePressed;


        if (debugLog)
        {
            Debug.Log(
                "Tutorial Sonar Input = " +
                inputEnabled
            );
        }
    }


    // ============================================================
    // Panel
    // ============================================================

    private void SetSonarPanel(
        bool open
    )
    {
        isSonarPanelOpen =
            open;


        if (
            sonarPanel != null &&
            sonarPanel.activeSelf !=
            open
        )
        {
            sonarPanel.SetActive(
                open
            );
        }
    }


    public void CloseSonarPanel()
    {
        SetSonarPanel(
            false
        );
    }


    // ============================================================
    // Getter
    // ============================================================

    public bool GetIsSonarPanelOpen()
    {
        return
            isSonarPanelOpen;
    }


    public bool GetInputEnabled()
    {
        return
            inputEnabled;
    }
}