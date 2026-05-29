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
            return;
        }

        if (gamepad.buttonEast.wasPressedThisFrame) ChangeToTutorialScene();
    }

    private void ChangeToTutorialScene()
    {
        SceneManager.LoadScene("TutorialScene");
    }
}
