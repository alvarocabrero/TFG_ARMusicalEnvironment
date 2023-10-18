using SimpleFileBrowser;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_Menu_Principal : MonoBehaviour
{
    public Component panelFuncional;
    public Component panelAyuda;
    private void Start()
    {
    }
    //Carga el menu de selececcion de bpms
    public void Start_Select_Sessions()
    {
        SceneManager.LoadScene("Menu_SelectSessions");
    }

    //Carga el menu de add Cartas
    public void Start_Add_New_Sessions()
    {
        SceneManager.LoadScene("Menu_AddNewSessions");
    }
    //Cierra la aplicacion
    public void Close()
    {
        Application.Quit();
    }

    public void Show_Help()
    {
        panelAyuda.gameObject.SetActive(true);
        panelFuncional.gameObject.SetActive(false);
    }

    public void Close_Help()
    {
        panelAyuda.gameObject.SetActive(false);
        panelFuncional.gameObject.SetActive(true);

    }
    public void Start_Customize_Sessions()
    {
        SceneManager.LoadScene("Menu_CustomizeSessions");
    }


}
