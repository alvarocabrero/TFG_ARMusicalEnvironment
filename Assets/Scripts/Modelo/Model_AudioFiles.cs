using Newtonsoft.Json;
using SimpleFileBrowser;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class Model_AudioFiles
{
    AudioClip[] audioClips = new AudioClip[3];
    private String rutaSERVER = "http://cloud.flexcontrol.net:3005/tracks";

    internal IList<Loop> ObtenerSesiones()
    {


        IList<Loop> session = new List<Loop>();
        using (UnityWebRequest www = UnityWebRequest.Get(rutaSERVER + "/collections"))
        {
            UnityWebRequestAsyncOperation r = www.SendWebRequest();

            Debug.Log("GET COMPLETE");
            while (!r.isDone)
            {
                //Debug.Log("LOADING . . .");

            }
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError(www.error);
            }
            string response = www.downloadHandler.text;
            //Loop json = JsonUtility.FromJson<Loop>(response);
            session = JsonConvert.DeserializeObject<IList<Loop>>(response);



        }
        if (session == null)
            session = new List<Loop>();
        return session;
    }

    public void Guardar(int bpmSesion, string nombreSesion, string[] nombreMarker, string[] nombreFigura, string[] rutasAudio)
    {
        List<Loop> loops = new List<Loop>();

        if (bpmSesion != 0 & nombreSesion != "")
        {

            int i = 0;
            foreach (string ruta in rutasAudio)
            {
                if (ruta != null)
                    if (ruta.Length > 2)
                    {
                        string nombreSonido = Path.GetFileName(ruta);
                        string nombreSonidoLoop = Path.GetFileNameWithoutExtension(ruta).Replace(" ", "");
                        byte[] bytesAudio = FileBrowserHelpers.ReadBytesFromFile(rutasAudio[i]);
                        string id = Upload(nombreSonido, nombreMarker[i], nombreFigura[i].ToLower(), nombreSesion, bytesAudio, bpmSesion).Replace("\"", "");
                        if (id.Equals(""))
                        { throw new Exception("ERROR SUBIENDO ARCHIVOS"); }

                    }
                i++;
            }

            Debug.Log("Sonidos guardados");


        }
        else
        {
            throw new Exception("Campos sin rellenar");
        }
    }

    internal AudioClip CargarAudio(string id)
    {

        AudioClip a = null;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(rutaSERVER + "/obtain/" + id, AudioType.WAV))
        {
            UnityWebRequestAsyncOperation r = www.SendWebRequest();



            Debug.Log("GET COMPLETE");
            while (!r.isDone)
            {
                //   Debug.Log("LOADING . . .");
            }
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            a = DownloadHandlerAudioClip.GetContent(www);


        }
        return a;
    }


    internal void BorrarAudio(string nombreSession)
    {

        UnityWebRequest www = UnityWebRequest.Get(rutaSERVER + "/remove/" + nombreSession);
        www.SendWebRequest();


        if (www.isNetworkError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            // Show results as text
            Debug.Log(www.downloadHandler.text);
        }
    }



    string Upload(string nombreAudio, string nombreImagen, string figura, string nombreSesion, byte[] audio, float bpmSesion)
    {
        WWWForm form = new WWWForm();
        form.AddField("name", nombreAudio);
        form.AddField("nombreImagen", nombreImagen);
        form.AddField("figura", figura);
        form.AddField("nombreSesion", nombreSesion);
        form.AddField("bpmSesion", bpmSesion.ToString());
        form.AddBinaryData("track", audio);
        using (UnityWebRequest www = UnityWebRequest.Post(rutaSERVER, form))
        {
            UnityWebRequestAsyncOperation r = www.SendWebRequest();
            Debug.Log("Form upload complete!");
            while (!r.isDone && !www.downloadHandler.isDone)
            {

            }
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError(www.error);
            }
            string id = www.downloadHandler.text;
            Debug.Log(id);
            return id;

        }
    }
}
