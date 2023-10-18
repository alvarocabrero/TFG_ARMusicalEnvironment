using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vuforia;

public class UI_ARGame : MonoBehaviour
{
    Controller_AR_Audio controller_AR_Audio;
    // TEXTO UI BPM
    public Text bpmText;
    public GameObject[] models3D;

    // Start is called before the first frame update
    void Start()
    {

        //VuforiaRuntime.Instance.InitVuforia();
        controller_AR_Audio = new Controller_AR_Audio(models3D);

        bpmText.text = controller_AR_Audio.getBPM().ToString();
    }

    void FixedUpdate()
    {
        controller_AR_Audio.Update();
    }

    public void Go_Back()
    {
        controller_AR_Audio.Reset();

        //VuforiaRuntime.Instance.Deinit();
        SceneManager.LoadScene("Menu_SelectSessions");

    }
}
