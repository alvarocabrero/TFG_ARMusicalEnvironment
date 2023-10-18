using UnityEngine;
using Vuforia;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class Controller_AR_Audio
{

    //AUDIO MANAGER

    internal Audio_Manager audio_Manager;
    //ARCHIVOS DE AUDIO
    internal Model_AudioFiles model_AudioFiles;
    //Gestor de sesion
    internal Controller_Gestor_Sesion controller_Gestor_Sesion;
    //Modelos3D
    public GameObject[] models3D;
    public List<TrackableBehaviour> figurasBorrado;

    public Controller_AR_Audio(GameObject[] models3D)
    {
        this.models3D = models3D;
        figurasBorrado = new List<TrackableBehaviour>();
        //Crear acceso al controlador de sesiones
        controller_Gestor_Sesion = new Controller_Gestor_Sesion();

        //Crear acceso a Archivos de audio
        model_AudioFiles = new Model_AudioFiles();

        this.audio_Manager = new Audio_Manager(controller_Gestor_Sesion.Search_BPM_From_Session(controller_Gestor_Sesion.Obtener_Nombre_Sesion_Elegida()));

        //  VuforiaARController.Instance.RegisterVuforiaStartedCallback(CreateImageTargetFromSideloadedTexture);
        CreateImageTargetFromSideloadedTexture();

    }
    internal string getBPM() => controller_Gestor_Sesion.Obtener_Nombre_Sesion_Elegida() + " " + controller_Gestor_Sesion.Search_BPM_From_Session(controller_Gestor_Sesion.Obtener_Nombre_Sesion_Elegida()) + " BPM";

    internal void Reset()
    {

        controller_Gestor_Sesion.Establecer_Nombre_Sesion("");

    }

    internal void multiUserPlay(JObject j, PersonalizeTrackableEventHandler[] trackables)
    {


        //UNIFICAR JSONS CON MARKERS ACTIVOS, SI UN MARKER DE USER 2 ESTA ACTIVO 

        string sesion = j["Sesion"].ToString();
        string modo = j["Modo"].ToString();
        int modoActual = controller_Gestor_Sesion.modo_Seleccionado;
        string sesionActual = controller_Gestor_Sesion.Obtener_Nombre_Sesion_Elegida();

        if (modoActual.ToString().Equals(modo.ToString()) && sesion.ToString().Equals(sesionActual.ToString()))
            foreach (PersonalizeTrackableEventHandler eventHandler in trackables)
            {
                string value = j[eventHandler.name].ToString();
                Debug.Log(eventHandler.name + ": " + value);
                Renderer[] renderer = eventHandler.GetRendererComponents();
                if (value.Equals("inactive"))
                {
                    if (!renderer[0].enabled)
                    {
                        if (modoActual.Equals(Controller_Gestor_Sesion.MODO_DEMO))
                        {

                            eventHandler.GetAudioComponent().mute = true;
                        }
                        else
                        {
                            // Disable audioSource:
                            eventHandler.GetAudioComponent().Stop();
                            eventHandler.GetAudioComponent().enabled = false;
                        }
                    }
                }
                else if (value.Equals("active"))
                {
                    if (!renderer[0].enabled)
                    {
                        if (modoActual.Equals(Controller_Gestor_Sesion.MODO_DEMO))
                        {
                            eventHandler.GetAudioComponent().mute = false;
                            eventHandler.GetAudioComponent().time = float.Parse(j["Time" + eventHandler.name].ToString());
                        }
                        else
                        {
                            // Disable audioSource:
                            eventHandler.GetAudioComponent().enabled = true;
                            eventHandler.GetAudioComponent().time = float.Parse(j["Time" + eventHandler.name].ToString());
                        }
                    }
                }
            }
    }

    void CreateImageTargetFromSideloadedTexture()
    {
        foreach (Loop b in controller_Gestor_Sesion.Get_Available_Sessions())
            if (b.metadata.nombreSesion.Equals(controller_Gestor_Sesion.Obtener_Nombre_Sesion_Elegida()))
            {
                GameObject figura = Figura(b.metadata.figura);
                if (figura == null)
                    throw new Exception("FIGURA CON MAL NOMBRE");
                AudioClip a = model_AudioFiles.CargarAudio(b._id);
                if (a == null)
                    throw new Exception("Sonido no encontrado");
                TrackAudioAndImage(b.metadata.nombreImagen, a, figura);
            }
    }

    private GameObject Figura(string figura)
    {
        GameObject figuraO = Search_Modelo(figura);
        if (figura == null)
            throw new Exception("Error en la figura");

        return GameObject.Instantiate<GameObject>(figuraO);

    }

    private GameObject Search_Modelo(string nombre)
    {
        foreach (GameObject objeto in models3D)
            if (objeto.name.ToLower().Equals(nombre.ToLower()))
                return objeto;
        return null;
    }
    private void TrackAudioAndImage(string rutaImagen, AudioClip clipAudio, GameObject model3D)
    {

        var trackableBehaviour = Track("Vuforia/" + rutaImagen + ".JPG", rutaImagen);
        figurasBorrado.Add(trackableBehaviour);
        clipAudio.name = rutaImagen;
        LoadAudio(trackableBehaviour, clipAudio);
        //Coordenadas de la tarjeta
        //trackableBehaviour.gameObject.AddComponent<TargetScreenCoords>();
        MakeParent(trackableBehaviour, model3D);

    }



    private void LoadAudio(TrackableBehaviour trackableBehaviour, AudioClip clipAudio)
    {

        AudioSource audioSource = trackableBehaviour.gameObject.AddComponent<AudioSource>();
        audioSource.name = clipAudio.name;
        audioSource.clip = clipAudio;
        audioSource.playOnAwake = false;
        if (controller_Gestor_Sesion.modo_Seleccionado.Equals(Controller_Gestor_Sesion.MODO_DEMO))
        {
            audioSource.mute = true;
        }
        audioSource.enabled = false;
        audioSource.loop = true;
        audioSource.transform.SetParent(trackableBehaviour.gameObject.transform);

        audio_Manager.audioSourcesLoaded.Add(audioSource, controller_Gestor_Sesion.nombre_sesion_seleccionado);
    }

    public void Update()
    {

        if (controller_Gestor_Sesion.Obtener_Nombre_Sesion_Elegida() != "")
        {
            if (controller_Gestor_Sesion.modo_Seleccionado == Controller_Gestor_Sesion.MODO_FREESTYLE)
                audio_Manager.Sync();
            else if (controller_Gestor_Sesion.modo_Seleccionado == Controller_Gestor_Sesion.MODO_DEMO)
                audio_Manager.SyncDemo();
        }
    }


    //Hace del GameObject padre al trackablebehaviour para que pueda activarse desde el eventHandler
    private void MakeParent(TrackableBehaviour trackableBehaviour, GameObject gO)
    {
        gO.transform.SetParent(trackableBehaviour.gameObject.transform);
    }

    //Creates the trackable behaviour
    TrackableBehaviour Track(string path, string name)
    {
        var objectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();

        var runtimeImageSource = objectTracker.RuntimeImageSource;

        runtimeImageSource.SetFile(VuforiaUnity.StorageType.STORAGE_APPRESOURCE, path, 0.7f, name);

        // create a new dataset and use the source to create a new trackable
        var dataset = objectTracker.CreateDataSet();


        var trackableBehaviour = dataset.CreateTrackable(runtimeImageSource, name);

        // add the PersonalizeTrackableEventHandler to the newly created game object

        trackableBehaviour.gameObject.AddComponent<PersonalizeTrackableEventHandler>();

        // activate the dataset
        objectTracker.ActivateDataSet(dataset);

        return trackableBehaviour;
    }

}