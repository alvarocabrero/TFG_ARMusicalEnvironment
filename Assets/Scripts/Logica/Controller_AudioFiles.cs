using System;
using System.Collections.Generic;

public class Controller_AudioFiles
{

    public String nombreSesion;
    public int bpmSesion;
    public String[] rutasAudio;
    Model_AudioFiles model_AudioFiles;


    public Controller_AudioFiles(int numeroLoops)
    {
        model_AudioFiles = new Model_AudioFiles();
        rutasAudio = new string[numeroLoops];
        bpmSesion = 0;
        nombreSesion = "";

    }

    internal void Save_AudioFiles(string[] markers, string[] figuras)
    {
        model_AudioFiles.Guardar(bpmSesion, nombreSesion, markers, figuras, rutasAudio);


    }

    internal List<Loop> Get_Sessions()
    {
       return (List<Loop>) model_AudioFiles.ObtenerSesiones();


    }
}