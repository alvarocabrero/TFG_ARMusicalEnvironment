using System.Collections;
using System.Collections.Generic;

namespace OrkestraLib
{
    public class User {
        public string name="";
        public string agentid="";
        public string profile="";
        public bool master = true;
        public Dictionary<string,string> capacity =new Dictionary<string,string>();
        public string context ="";
    
  
    public User(string agentid,string name,string profile, Dictionary<string,string> caps ){
        this.agentid = agentid;
        this.name = name;
        this.profile = profile;
        this.capacity = caps;
    }
    }

}