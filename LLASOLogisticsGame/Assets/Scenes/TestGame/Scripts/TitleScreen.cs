using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    void Start()
    {
        // Make sure the mouse can be seen and used on this screen
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // The Play button calls this
    public void PlayGame()
    {
        // Scene number 1 in the build list is your game
        SceneManager.LoadScene(1);
    }
}