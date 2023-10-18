using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[Serializable]
public class Loop
{





    public string filename { get; set; }


    public string _id { get; set; }


    public MetaData metadata;
    public Loop(string id, string name, string nombreSonido, string nombreImagen, string figura, float bpm, string nombreSesion)
    {
        this._id = id;
        this.filename = nombreSonido;
    }


}
