using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class startGame : MonoBehaviour
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }
    public bool now = false;
    Cursor c;
    
    public void play()
    {

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        now = true;
    }
    public bool getStart()
    {
        return now;
    }
}
