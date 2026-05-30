using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TutorialSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame) ChangeToMainlScene();
            
            return;
        }

        if (gamepad.buttonEast.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame) ChangeToMainlScene();
    }

    private void ChangeToMainlScene()
    {
        SceneManager.LoadScene("MainScene");
    }
}
