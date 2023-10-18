using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_Customize_Sessions : MonoBehaviour
{
    public Dropdown dropDown;
    Controller_Gestor_Sesion controller_Gestor_Sesion;
    private List<Loop> sesiones;

    // Start is called before the first frame update
    void Start()
    {
        controller_Gestor_Sesion = new Controller_Gestor_Sesion();
        sesiones = controller_Gestor_Sesion.Get_Available_Sessions();
        Show_Sessions(sesiones);
    }

    private void Show_Sessions(List<Loop> beats)
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

    public void Delete_Session()
    {
        if (dropDown.options.Count != 0)
        {
            string bpmOption = dropDown.options[dropDown.value].text;
            dropDown.options.Remove(dropDown.options[dropDown.value]);
            controller_Gestor_Sesion.Delete_Loop(bpmOption);
            SceneManager.LoadScene("Menu_CustomizeSessions");
        }
        else { Debug.LogError("No hay sesiones"); }
    }

    public void Go_Back()
    {
        SceneManager.LoadScene("Menu_First");
    }

}
