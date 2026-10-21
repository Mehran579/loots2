using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class gamestarted : MonoBehaviour
{

    public void Onclikc()
    {
        Debug.Log("clicked");
        SceneManager.LoadScene(1);
    }
}

