using UnityEngine;
using UnityEngine.SceneManagement;

public class ReloadScene : MonoBehaviour
{
    public void ReloadButton()
    {
       SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
