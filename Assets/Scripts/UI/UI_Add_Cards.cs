
using SimpleFileBrowser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_Add_Cards : MonoBehaviour
{
    public int numeroLoops = 3;
    Controller_AudioFiles controller_AudioFiles;
    private GameObject loops;
    public Component panel_cargando;

    public GameObject markersRepetidos;
    // Start is called before the first frame update
    void Start()
    {
        Aceptar();
        loops = GameObject.Find("Loops");
        controller_AudioFiles = new Controller_AudioFiles(numeroLoops);
    }


    public void Go_Back() => SceneManager.LoadScene("Menu_First");

    public void Save_BPM()
    {

        Component[] texts;

        texts = loops.GetComponentsInChildren(typeof(TMP_InputField));

        foreach (TMP_InputField text in texts)
            if (text.name.Equals("InputBPM_Text"))
            {
                controller_AudioFiles.bpmSesion = int.Parse(text.text);
                Debug.Log(text.text);
            }
    }
    public void Save_Name_Session()
    {
        Component[] texts;

        texts = loops.GetComponentsInChildren(typeof(TMP_InputField));

        foreach (TMP_InputField text in texts)
            if (text.name.Equals("InputNombreSesion_Text"))
            {
                controller_AudioFiles.nombreSesion = text.text;
                Debug.Log(text.text);
            }
    }

    private string[] Save_Models3D()
    {
        Component[] drowpdowns;

        drowpdowns = loops.GetComponentsInChildren(typeof(TMP_Dropdown));
        string[] figuras = new string[numeroLoops];
        int i = 0;
        foreach (TMP_Dropdown d in drowpdowns)
        {

            string nombreDropdown = "Dropdown_Modelo_Loop";

            if (d.name.Contains(nombreDropdown))
            {
                Debug.Log(d.value);
                figuras[i] = d.options[d.value].text.Replace(" ", "");
                i++;
            }

        }
        return figuras;
    }

    private string[] Save_Markers()
    {
        Component[] drowpdowns;
        drowpdowns = loops.GetComponentsInChildren(typeof(TMP_Dropdown));
        string[] markers = new string[numeroLoops];
        int i = 0;
        foreach (TMP_Dropdown d in drowpdowns)
        {

            if (d.name.Contains("Dropdown_Marker_Loop"))
            {

                Debug.Log(d.value);
                markers[i] = d.options[d.value].text.Replace(" ", "");
                i++;
            }

        }
        return markers;
    }

    public void Show_Audio_Path(int index, string[] rutasAudio)
    {
        Component[] texts;

        texts = loops.GetComponentsInChildren(typeof(TMP_Text));

        foreach (TMP_Text text in texts)
            if (text.name.Equals("RutaSonido_Loop" + (index + 1)))
            {

                text.text = Path.GetFileNameWithoutExtension(rutasAudio[index]);
            }

    }




    public void Save_Sound_Path(int index)
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Sound", ".wav"));
        FileBrowser.SetDefaultFilter(".wav");
        StartCoroutine(ShowLoadDialogCoroutine(index));


    }
    IEnumerator ShowLoadDialogCoroutine(int index)
    {
        // Show a load file dialog and wait for a response from user
        // Load file/folder: both, Allow multiple selection: true
        // Initial path: default (Documents), Initial filename: empty
        // Title: "Load File", Submit button text: "Load"
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Files and Folders", "Load");

        // Dialog is closed
        // Print whether the user has selected some files/folders or cancelled the operation (FileBrowser.Success)
        Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            // Print paths of the selected files (FileBrowser.Result) (null, if FileBrowser.Success is false)
            for (int i = 0; i < FileBrowser.Result.Length; i++)
            {
                Debug.Log(FileBrowser.Result[i]);
            }
            // Read the bytes of the first file via FileBrowserHelpers
            // Contrary to File.ReadAllBytes, this function works on Android 10+, as well

            //string destinationPath = Path.Combine(Application.persistentDataPath, FileBrowserHelpers.GetFilename(FileBrowser.Result[0]));

            controller_AudioFiles.rutasAudio[index] = FileBrowser.Result[0];
            Show_Audio_Path(index, controller_AudioFiles.rutasAudio);
        }
    }
    public void Save_Session()
    {
        if (Check_Values())
        {
            String[] markers = Save_Markers();
            String[] models3D = Save_Models3D();

            controller_AudioFiles.Save_AudioFiles(markers, models3D);

            Go_Back();
        }
        else
        {
            panel_cargando.gameObject.SetActive(false);
            markersRepetidos.SetActive(true);
            throw new Exception("Error en la comprobacion, markers repetidos o no se ha introducido ninguna ruta");
        }
    }
    public void Cargando()
    {
        panel_cargando.gameObject.SetActive(true);
    }
    public void Aceptar()
    {
        markersRepetidos.SetActive(false);
    }

    private bool Check_Values()
    {
        string[] markers = Save_Markers();
        return Check_Audio_Paths() && Check_Markers(markers) && Check_Text_Fields();
    }

    private bool Check_Text_Fields()
    {
        foreach (Loop beat in controller_AudioFiles.Get_Sessions())
            if (beat.metadata.nombreSesion.Equals(controller_AudioFiles.nombreSesion))
                return false;
        if (controller_AudioFiles.nombreSesion != "" && controller_AudioFiles.bpmSesion != 0)
            return true;
        return false;
    }

    private bool Check_Markers(string[] markers)
    {

        for (int i = 0; i < markers.Length; i++)
        {
            int cont = 0;
            for (int j = markers.Length - 1; j >= 0; j--)
            {
                if (markers[i].Equals(markers[j]))
                {
                    cont++;
                }
            }
            if (cont > 1)
                return false;
        }
        return true;
    }

    private bool Check_Audio_Paths()
    {
        string[] rutasAudio = controller_AudioFiles.rutasAudio;
        int conta = 0;
        foreach (string ruta in rutasAudio)
        {
            if (ruta == null)
                conta++;
        }
        if (conta == rutasAudio.Length)
            return false;
        return true;
    }
}
