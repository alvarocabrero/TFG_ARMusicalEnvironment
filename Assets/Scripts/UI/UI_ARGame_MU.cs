using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vuforia;

public class UI_ARGame_MU : MonoBehaviour
{
    internal Controller_AR_Audio controller_AR_Audio;
    // TEXTO UI BPM
    public Text bpmText;
    public GameObject[] models3D;
    private Orkestration orkestraPrueba;
    // Start is called before the first frame update
    void Start()
    {
        controller_AR_Audio = new Controller_AR_Audio(models3D);

        orkestraPrueba = gameObject.AddComponent<Orkestration>();

        bpmText.text = controller_AR_Audio.getBPM().ToString();

    }

    void FixedUpdate()
    {
        controller_AR_Audio.Update();
    }

    public void Go_Back()
    {
        controller_AR_Audio.Reset();

        SceneManager.LoadScene("Menu_SelectSessions");

    }
}
