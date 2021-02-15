using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneTransition : MonoBehaviour
{
    public static SceneTransition instance;

    public Animator transition;

    // Start is called before the first frame update
    void Start()
    {
        if (instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        GameObject.DontDestroyOnLoad(this.gameObject);
    }

    public void StartTransition()
    {
        transition.SetTrigger("start");
    }

    public void EndTransition()
    {
        transition.SetTrigger("end");
    }
}
