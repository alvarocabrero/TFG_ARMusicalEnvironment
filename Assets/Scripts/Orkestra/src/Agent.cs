using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace OrkestraLib
{
    public class Agent {
        public List<Action<string>> handlers = new  List<Action<string>>();
        public string agentID ="";
       

    }    
}