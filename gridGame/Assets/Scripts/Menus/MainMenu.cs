using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayButton()
    {
        StartCoroutine(PlayCoroutine());
    }

    IEnumerator PlayCoroutine()
    {
        SceneTransition.instance.StartTransition();

        yield return new WaitForSeconds(0);

        SceneManager.LoadScene("testScene");

        SceneTransition.instance.EndTransition();

    }
}
