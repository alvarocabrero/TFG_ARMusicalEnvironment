using System.Collections;
using UnityEngine;
using OrkestraLib;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public class Orkestration : MonoBehaviour
{

    Orkestra ork;
    UI_ARGame_MU uiArGameMU;
    Controller_AR_Audio controllerARAudio;
    private bool started;
    PersonalizeTrackableEventHandler[] trackables;
    private JObject pastStatus;

    private void Start()
    {
        StartAsync();
    }

    async Task StartAsync()
    {
        trackables = new PersonalizeTrackableEventHandler[3];
        ork = await Task.Run(() => new Orkestra("https://cloud.flexcontrol.net/", "ARMusic", null));
        await Task.Delay(5000);

        ork.UserEvents += UserEventSubscriber;
        ork.AppEvents += AppEventSubscriber;
        uiArGameMU = GetComponent<UI_ARGame_MU>();
        controllerARAudio = uiArGameMU.controller_AR_Audio;
        GameObject marker1 = GameObject.Find("Marker1");
        GameObject marker2 = GameObject.Find("Marker2");
        GameObject marker3 = GameObject.Find("Marker3");
        trackables[0] = marker1.GetComponent<PersonalizeTrackableEventHandler>();
        trackables[1] = marker2.GetComponent<PersonalizeTrackableEventHandler>();
        trackables[2] = marker3.GetComponent<PersonalizeTrackableEventHandler>();
        SendInfo(GetUserInfo());


    }


    public IEnumerator ReceiveData(object sender, JObject _test)
    {
        Debug.Log(_test["Usuario"] + " " + ork.agentid);
        if (!_test["Usuario"].ToString().Equals(ork.agentid.ToString()))
        {
            Debug.Log("Usuario externo: " + _test);

            controllerARAudio.multiUserPlay(_test, trackables);
        }
        yield return null;
    }


    /* Receives user context events */
    void UserEventSubscriber(object sender, JObject test)
    {
        string value = test["value"].ToString();
        UnityMainThreadDispatcher.Instance().Enqueue(ReceiveData(sender, JObject.Parse(value)));
    }

    /* Receives application context events*/
    void AppEventSubscriber(object sender, JObject data)
    {
        string value = data["value"].ToString();
        Debug.Log("Entra A Event");
        // UnityMainThreadDispatcher.Instance().Enqueue(ReceiveData(sender, JObject.Parse(value)));
    }

    void OnDestroy()
    {
        Debug.Log("Closing ");
        try
        {
            if (ork != null)
                ork.close();
        }
        catch (Exception ex)
        {
            Debug.Log("Closing " + ex);
        }
    }
    void OnDisable()
    {
        Debug.Log("Closing ");
        if (ork != null)
            ork.close();
    }

    void SendInfo(JObject audioMarkers)
    {

        if (ork != null)
        {
            pastStatus = audioMarkers;
            ork.setUserItem(ork.agentid, "data", JsonConvert.SerializeObject(audioMarkers));
            started = true;
        }

    }

    JObject GetUserInfo()
    {
        string sesion = controllerARAudio.controller_Gestor_Sesion.nombre_sesion_seleccionado;
        string modo = controllerARAudio.controller_Gestor_Sesion.modo_Seleccionado.ToString();

        JObject audioMarkers = new JObject();

        audioMarkers.Add("Usuario", ork.agentid);
        audioMarkers.Add("Sesion", sesion);
        audioMarkers.Add("Modo", modo);

        foreach (var audioSource in controllerARAudio.audio_Manager.audioSourcesLoaded)
        {
            if (controllerARAudio.audio_Manager.audioSourcesPlaying.Contains(audioSource.Key))
                audioMarkers.Add(audioSource.Key.name, "active");
            else
                audioMarkers.Add(audioSource.Key.name, "inactive");
            audioMarkers.Add("Time" + audioSource.Key.name, audioSource.Key.time);
        }


        return audioMarkers;
    }

    void Update()
    {
        if (started)
        {
            bool statusChanged = CheckStatus(GetUserInfo());
            if (statusChanged)
            {
                Debug.Log("Cambio");
                SendInfo(GetUserInfo());
            }

        }

    }

    private bool CheckStatus(JObject audioMarkers)
    {
        JObject usuarioActual = audioMarkers;
        JObject pastUsuarioActual = pastStatus;
        if (!(usuarioActual == null || pastUsuarioActual == null))
            foreach (var a in usuarioActual)
            {
                if (!pastUsuarioActual[a.Key].Equals(a.Value) && !a.Key.Contains("Time"))
                    return true;
            }
        return false;
    }


}
