using System;

[Serializable]
public class MetaData
{

    public string nombreImagen { get; set; }
    public string figura { get; set; }
    public string nombreSesion { get; set; }
    public float bpmSession { get; set; }

    public MetaData(string nombreImagen, string figura, float bpm, string nombreSesion)
    {
        this.nombreImagen = nombreImagen;
        this.figura = figura;
        this.bpmSession = bpm;
        this.nombreSesion = nombreSesion;
    }
}