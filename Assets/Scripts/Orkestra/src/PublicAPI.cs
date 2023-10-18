using System.Collections;
using System.Collections.Generic;
using System;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace OrkestraLib
{
    public class PublicAPI {
            
             public string agentID="";
             public Func<string,Action<string>,string> on ;
             public Func<string,Action<string>,string> off ;
             public Func<string,string,string> setItem ;
             public Func<string,string> getItem ;
             public Func<JObject> capabilities ;
             public Func<JArray> keys ;
            
    }    
}