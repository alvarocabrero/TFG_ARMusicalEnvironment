using System.Collections;
using System.Collections.Generic;
using System;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace OrkestraLib
{
    public class RemoteAgent:Agent{
        public PublicAPI publicAPI = new PublicAPI();
        public Func<string,string,string,string> update_value;
        public Func<string,string> update_meta;
        public  Dictionary<string,List<Action<string>>> handlers;
        public RemoteAgent(){}

        
    }
}   