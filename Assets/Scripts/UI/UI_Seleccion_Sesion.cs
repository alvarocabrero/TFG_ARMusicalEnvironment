using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_Seleccion_Sesion : MonoBehaviour
{
    public Dropdown dropDown;
    public Slider slider_Modo;
    public Slider slider_MU;
    public Component panel_cargando;
    Controller_Gestor_Sesion gestor_Sesion;

    public int MODO_SU = 0;
    public int MODO_MU = 1;


    // Start is called before the first frame update
    void Start()
    {
        gestor_Sesion = new Controller_Gestor_Sesion();
        Show_Beat_Options(gestor_Sesion.Get_Available_Sessions());
    }

    private void Show_Beat_Options(List<Loop> beats)
    {
        Canvas c = gameObject.GetComponentInChildren<Canvas>();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        List<string> optionsStrings = new List<string>();
        foreach (Loop s in beats)
        {
            if (!optionsStrings.Contains(s.metadata.nombreSesion.ToString()))
            {
                options.Add(new Dropdown.OptionData(s.metadata.nombreSesion.ToString()));
                optionsStrings.Add(s.metadata.nombreSesion.ToString());
            }
        }

        dropDown.options = options;
    }

    public void Start_Session()
    {
        if (dropDown.options.Count != 0)
        {
            panel_cargando.gameObject.SetActive(true);
            string wanted_Session_Name = dropDown.options[dropDown.value].text;
            gestor_Sesion.Establecer_Nombre_Sesion(wanted_Session_Name);
            gestor_Sesion.Establecer_Modo((int)slider_Modo.value);

            if ((int)slider_MU.value == MODO_SU)
                SceneManager.LoadScene("Main_Game");
            else if ((int)slider_MU.value == MODO_MU)
                SceneManager.LoadScene("MultiUser");
        }
        else
        {
            Debug.LogError("No hay sesiones");
        }
    }

    public void Go_Back()
    {
        SceneManager.LoadScene("Menu_First");
    }
}
