using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    
    
    void Start()
    {
        
    }

    void Update()
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame) ChangeToTutorialScene();
            
            return;
        }

        if (gamepad.buttonEast.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame) ChangeToTutorialScene();
    }

    private void ChangeToTutorialScene()
    {
        SceneManager.LoadScene("TutorialScene");
    }
}
