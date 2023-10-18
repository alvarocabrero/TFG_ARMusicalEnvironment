using UnityEngine;
using System.Collections.Generic;
using System;
using SimpleFileBrowser;

public class Controller_Gestor_Sesion
{
    private List<Loop> beats;
    public static int MODO_DEMO = 1;
    public static int MODO_FREESTYLE = 0;

    public string nombre_sesion_seleccionado;
    public int modo_Seleccionado;
    public Model_AudioFiles modelAudio;
    public Controller_Gestor_Sesion()
    {
        nombre_sesion_seleccionado = Obtener_Nombre_Sesion_Elegida();
        modo_Seleccionado = Obtener_Modo_Elegido();
        FileBrowser.RequestPermission();
        modelAudio = new Model_AudioFiles();
        beats = (List<Loop>)modelAudio.ObtenerSesiones();

    }

    private int Obtener_Modo_Elegido()
    {
        return PlayerPrefs.GetInt("modo");
    }

    public List<Loop> Get_Available_Sessions()
    {
        return beats;
    }
    public void Establecer_Nombre_Sesion(string nombre)
    {
        PlayerPrefs.SetString("NombreSesion", nombre);

    }
    public string Obtener_Nombre_Sesion_Elegida()
    {
        return PlayerPrefs.GetString("NombreSesion");
    }

    internal float Search_BPM_From_Session(string nombre_sesion)
    {
        foreach (Loop b in beats)
        {
            if (b.metadata.nombreSesion.Equals(nombre_sesion))
            {
                return b.metadata.bpmSession;
            }
        }
        return 0;
    }


    internal void Establecer_Modo(int value)
    {
        if (value.Equals(MODO_DEMO))
        {
            PlayerPrefs.SetInt("modo", MODO_DEMO);
        }
        else if (value.Equals(MODO_FREESTYLE))
        {
            PlayerPrefs.SetInt("modo", MODO_FREESTYLE);
        }
    }

    internal void Delete_Loop(String nombreSession)
    {
        modelAudio.BorrarAudio(nombreSession);
    }
}