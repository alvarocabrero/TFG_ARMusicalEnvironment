using System.Collections;
using UnityEngine;
using OrkestraLib;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public class OrkestraPrueba : MonoBehaviour
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
        ork = await Task.Run(() => new Orkestra("https://cloud.flexcontrol.net/", "ARMusic", null));
        await Task.Delay(5000);

        ork.UserEvents += UserEventSubscriber;
        ork.AppEvents += AppEventSubscriber;
        uiArGameMU = GetComponent<UI_ARGame_MU>();
        controllerARAudio = uiArGameMU.controller_AR_Audio;
        trackables = GetComponents<PersonalizeTrackableEventHandler>();
        //First Send
        FirstSend();
        started = true;
    }

    private void FirstSend()
    {
        DeleteAppData();
    }
    private void DeleteAppData()
    {

        bool admin = false;
        int nAdmin = 0;
        foreach (var a in ork.getUsers())
        {
            if (a.Value.profile.Equals("admin"))
            {
                admin = true;
                nAdmin += 1;
            }
        }
        if (ork.getUsers().Count == 0 || ork.getUsers().Count == 1 && ork.getUsers().ContainsKey(ork.agentid) || ork.getUsers().Count == 1 + nAdmin && ork.getUsers().ContainsKey(ork.agentid) && admin)
        {
            JObject j = new JObject();
            j.Add("Sesion", "empty");
            ork.setAppAttribute("data", JsonConvert.SerializeObject(j));
            Debug.Log("Deleted");

        }

    }
    public IEnumerator transformCam(object sender, JObject _test)
    {

        List<JObject> usuariosExternos = new List<JObject>();
        if (_test.SelectToken("Sesion").Equals("empty"))
        {
            if (pastStatus == null)
            {
                pastStatus = new JObject();
                pastStatus.Add("Usuarios", GetUserInfo());
            }
        }
        else
        {
            //Si hay usuario externo se actualiza el paststatus
            foreach (var usuario in pastStatus.Values())
                if (!usuario["Usuario"].ToString().Equals(ork.agentid))
                {
                    foreach (var a in _test.Properties())
                    {
                        if (!pastStatus[a.Name.ToString()].Equals(a.Value))
                            pastStatus[a.Name.ToString()] = a.Value;
                    }

                }
        }

        //SEPARAR CADA USUARIO A UN JSON
        foreach (var a in _test)
        {
            Debug.Log(a.ToString());
            //usuariosExternos.Add((JObject)a);
        }

        //BUSCAR USUARIOS QUE NO SEA LOS ACTUALES
        //MANDAR LA INFO 
        foreach (var usuario in usuariosExternos)
            if (!usuario["Usuario"].Equals(ork.agentid))
            {
                Debug.Log("Usuario externo: " + usuario);
                controllerARAudio.multiUserPlay(usuario, trackables);
            }

        Debug.Log("A través de transform: " + _test["Usuarios"] + ":  " + _test);
        yield return null;
    }


    /* Receives user context events */
    void UserEventSubscriber(object sender, JObject test)
    {
        string value = test["value"].ToString();
        //UnityMainThreadDispatcher.Instance().Enqueue(transformCam(sender, JObject.Parse(value)));
    }

    /* Receives application context events*/
    void AppEventSubscriber(object sender, JObject data)
    {
        string value = data["value"].ToString();
        Debug.Log("Entra A Event");
        UnityMainThreadDispatcher.Instance().Enqueue(transformCam(sender, JObject.Parse(value)));
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
        bool valor = false;
        if (ork != null)
        {
            if (pastStatus != null)
            {
                foreach (var usuario in pastStatus.Values())
                    if (usuario["Usuario"].ToString().Equals(ork.agentid))
                    {
                        valor = true;
                        foreach (var a in audioMarkers.Properties())
                        {
                            if (!pastStatus[a.Name.ToString()].Equals(a.Value))
                                pastStatus[a.Name.ToString()] = a.Value;
                        }
                    }
                if (!valor)
                {
                    pastStatus["Usuarios"] += audioMarkers.ToString();
                }
            }
            else
            {
                pastStatus = GetUserInfo();
            }
            Debug.Log("ENVIANDO: " + pastStatus);
            ork.setAppAttribute("data", JsonConvert.SerializeObject(pastStatus));
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
            if (controllerARAudio.audio_Manager.audioSourcesPlaying.Contains(audioSource.Key))
                audioMarkers.Add(audioSource.Key.name, "active");
            else
                audioMarkers.Add(audioSource.Key.name, "inactive");
        JObject result = new JObject();
        result.Add(ork.agentid, audioMarkers);
        return result;
    }

    void Update()
    {
        if (started)
        {
            if (pastStatus != null)
            {
                JObject usuarios = new JObject();
                usuarios.Add("Usuarios", GetUserInfo());
                bool statusChanged = CheckStatus(usuarios);
                if (statusChanged)
                {
                    Debug.Log("Cambio");
                    SendInfo(usuarios);
                }
            }
            else
            {
                pastStatus = new JObject();
                // Debug.Log("Primer print" + ork.getGlobalData()["data"].ToString());
                pastStatus.Add("Usuarios", GetUserInfo());
                SendInfo(pastStatus);
            }
        }
    }

    private bool CheckStatus(JObject audioMarkers)
    {
        JObject usuarioActual = GetActualUserJson(audioMarkers);
        JObject pastUsuarioActual = GetActualUserJson(pastStatus);
        if (!(usuarioActual == null || pastUsuarioActual == null))
            foreach (var a in usuarioActual)
            {
                if (!pastUsuarioActual[a.Key].Equals(a.Value))
                    return true;
            }
        return false;
    }

    private JObject GetActualUserJson(JObject usuarios)
    {

        foreach (var usuario in usuarios.Values())
        {
            if (usuario["Usuario"].ToString().Equals(ork.agentid))
            {
                string s = JsonConvert.SerializeObject(usuario);
                return JObject.Parse(s);
            }
        }

        Debug.LogError("NO EXISTE USUARIO EN EL JSON");
        return null;
    }
}
