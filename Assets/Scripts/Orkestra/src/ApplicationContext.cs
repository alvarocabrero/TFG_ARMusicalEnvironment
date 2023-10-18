using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Dynamic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OrkestraLib
{
public class ApplicationContext 
{
    string url="";
    Dictionary<string, RemoteAgent> agents;
    SharedState _sharedstate;
    public AgentContext self_ag;
    bool __ready = false;
      // Handle updates
    Dictionary<string, List<string>> _sub_param2agent = new Dictionary<string, List<string>>(); // Remote subscriptions, parameter -> [agentid, ...]
    Dictionary<string, List<string>> _sub_agent2param = new Dictionary<string, List<string>>(); // Remote agent -> [parameter, ...]
    Dictionary<string, Action<string>> _sub_param2handler = new Dictionary<string, Action<string>>();
    Dictionary<string, JObject> _currentAgentState = new Dictionary<string, JObject>(); // For diffs
    Dictionary<string, string> _last_capabilities = new Dictionary<string, string>(); // For annouce diffs
    List<Action<string>> _cbs = new List<Action<string>> ();
    Dictionary<string, List<Action<string>>>  _globalCbs = new Dictionary<string, List<Action<string>>> ();
    public ApplicationContext(string url,string agentid){
         this.url = url;
         System.Console.WriteLine("ApplicationContext init" + this.url);
         this._sharedstate = new SharedState(url, agentid);
        
         this._sharedstate.init();
         this._sharedstate.on("readystatechange",readystatechange,null);
         this.agents = new Dictionary<string, RemoteAgent>();

    }
    void readystatechange(string data){
          System.Console.WriteLine("apptxt "+data);
          if (data.Equals(_sharedstate.STATE["OPEN"])) {
                    __ready = true;
                this.self_ag = new AgentContext(this._sharedstate.agentID);
                string meta =  "{\"keys\":"+JsonConvert.SerializeObject(this.self_ag.keys())+", \"capabilities\":"+JsonConvert.SerializeObject(this.self_ag.capabilities())+"}";
                 
                
                    _sharedstate.setItem("__meta__" + _sharedstate.agentID,meta);
                    _sharedstate.setItem("__metasub__" + _sharedstate.agentID, JsonConvert.SerializeObject(new JArray()));
                    _sharedstate.setPresence("online");
                  
                    // Register
                    


                    // Also check for new parameters being added
                    //  moved here to only test if _sharedstate == open
                    this.self_ag.on("keychange",  (e) => {
                        // Update my meta description too
                     
                        var meta2 = new {
                            keys = this.self_ag.keys(),
                            capabilities = this.self_ag.capabilities()
                        };
                        _sharedstate.setItem("__meta__" + _sharedstate.agentID, JsonConvert.SerializeObject(meta2));
                    });

                    _sharedstate.on("presence", (e) => {
                        System.Console.WriteLine("appli: "+e);
                        JObject d = JObject.Parse(e);
                        string _agentid = d["key"].ToString();
                        object _state = d["value"];
                        if (d["value"].ToString().Equals("offline")) {
                            if (agents.ContainsKey(_agentid)) {
                                // Clean up this node

                                agents[_agentid] = null;
                                agents.Remove(_agentid);
                                _doCallbacks("agentchange", JsonConvert.SerializeObject(new {
                                    agentid = _agentid,
                                    agentContext = "null"
                                }));

                            }
                            // TODO: Also clean up other state that should be removed?
                            /*
                            if (options.autoremove == true) {
                              this.removeItem("__meta__" + _agentid)
                              this.removeItem("__metasub__" + _agentid)
                              this.delAgent(_agentid);
                            }
                            */

                        } 
                        else {
                          
                            if (d["value"].ToString().Equals("online")) {
                                //System.Console.WriteLine("app: inside online");
                                if (!agents.ContainsKey(_agentid)) {
                                     //System.Console.WriteLine("IF BEF DOCALLBACK"+e);
                                    agents[_agentid] = remoteAgentContext(_agentid);
                                }
                              
                                var diff = new  {
                                    added = new JArray(),
                                    altered = new JArray(),
                                    removed = new JArray()
                                };


                                /*  if (_last_capabilities[_agentid] != undefined) {
                                      for (var capability in _last_capabilities[_agentid]) {
                                           console.log(capability);
                                         if (new_capabilities[capability] === undefined) {
                                              diff.removed.push(capability);
                                          }
                                     }
                                  } else {
                                      _last_capabilities[_agentid] = [];
                                  }

                                  for (var capability in new_capabilities) {
                                      if (_last_capabilities[_agentid][capability] === undefined) {
                                          diff.added[capability] = new_capabilities[capability];
                                      } else if (_last_capabilities[_agentid][capability] != new_capabilities[capability]) {
                                         diff.altered[capability] = new_capabilities[capability];
                                      }
                                  }
                                  */
                                // _last_capabilities[_agentid] = new_capabilities;
                                Task.Delay(0).ContinueWith(t2 =>
                                {
                                    var new_capabilities = agents[_agentid].publicAPI.keys();
                                    List<string> caps = new_capabilities.ToObject<List<string>>();
                                    _update_subscriptions(_agentid, caps);
                                    _doCallbacks("agentchange", new
                                    {
                                        agentid = _agentid,
                                        agentContext = _agentid,
                                        diff = diff
                                    });
                                });
                            }

                        }
                        //agents[_agentid].
                    },null);
                _sharedstate.on("change",  (ev) => {
                    System.Console.WriteLine(ev);
                    try  {
                    JObject e = JObject.Parse(ev);
                if (e["key"].ToString().IndexOf("__meta__") > -1) {
                    string _agentid = e["key"].ToString().Substring(8);
                    //System.Console.WriteLine("agentid: "+_agentid);
                    RemoteAgent a = null;
                    if (agents.ContainsKey(_agentid)) a = agents[_agentid];
                            Task.Delay(150).ContinueWith(t2 =>
                            {
                                if (a != null)
                                {
                                    a.update_meta(e["value"].ToString());
                                    _doCallbacks("agentchange", JsonConvert.SerializeObject(new
                                    {
                                        agentid = _agentid,
                                        agentContext = _agentid
                                    }));


                                }
                            });
                } else if (e["key"].ToString().IndexOf("__metasub__") > -1) {
                    var _agentid = e["key"].ToString().Substring(11);
                        JArray tnp = JArray.Parse(e["value"].ToString());
                        List<string> list = tnp.ToObject<List<string>>();
                       _update_subscriptions(_agentid, list);

                } else if (e["key"].ToString().IndexOf("__val__") > -1) {
                    string _agentid = e["key"].ToString().Substring(7).Split('_')[0];
                    string _key = e["key"].ToString().Substring(7).Split('_')[1];
                    _update_value(_agentid, _key, e["value"].ToString());
                } else if (e["key"].ToString().IndexOf("__global__") > -1) {
                    System.Console.WriteLine("globalVar" + e["key"] + e["value"]);
                    string _what = e["key"].ToString().Substring(10);
                    if (_globalCbs.ContainsKey(_what)) {
                        _doCallbacks(_what, JsonConvert.SerializeObject(new {
                            key = _what,
                            value = e["value"].ToString()
                        }));
                    }
                } else {
                   System.Console.WriteLine("Unknown Change:" + JsonConvert.SerializeObject(e));
                }
                }
                catch (Exception ex){
                    System.Console.WriteLine("Exception" + ex);
                }
            },null);
                }
    }    
    public RemoteAgent getAgent  (string aid) {
            if (agents.ContainsKey(aid)) return agents[aid];
            else return null;
    }
     public RemoteAgent getAgentContext  (string aid) {
            if (agents.ContainsKey(aid)) return remoteAgentContext(aid);
            else return null;
    }
    public String getMe  () {
            return this._sharedstate.agentID;
    }
    public Dictionary<string,RemoteAgent> getAgents  () {
        if (!this.agents.ContainsKey(_sharedstate.agentID)) {
            this.agents[_sharedstate.agentID] = remoteAgentContext(_sharedstate.agentID);
        }
        Dictionary<string,RemoteAgent> agents = new Dictionary<string,RemoteAgent>();
        agents.Add("self",agents[_sharedstate.agentID]);
         
       foreach(KeyValuePair<string, RemoteAgent> entry in agents)
        {
           if (entry.Value.agentID.Equals(_sharedstate.agentID)) continue;
            if (agents.ContainsKey(_sharedstate.agentID)) {
                agents[_sharedstate.agentID] = agents[_sharedstate.agentID];
            }
       }
       
        return agents;
    }
        bool _ready (){

          return  __ready;
        }
     public  void addAgent( RemoteAgent agent) {
          // TODO: Check argument!
          if (agents[agent.agentID] != null) {
            //System.Console.WriteLine("Already have agent '" + agent.agentID + "'");
          }
          JObject _diff = new JObject ();
          _diff["added"] =  agent.publicAPI.capabilities();
          agents[agent.agentID] = agent;
          _doCallbacks("agentchange", JsonConvert.SerializeObject(new  {
            agentid = agent.agentID,
            agentContext = agent,
            diff = _diff
          }));
        }

    public void removeAgent (RemoteAgent agent) {
          if (agents[agent.agentID] != agent) {
            //System.Console.WriteLine("Failed to remove agent '" + agent.agentID + "'");
          }
          agents.Remove(agent.agentID);
          _doCallbacks("agentchange", JsonConvert.SerializeObject(new {
              agentid = agent.agentID,
              agentContext = "null"
          }));
        }


       public void setItem  (string key, string value) {
            if (key!=null & value!=null) {
                _sharedstate.setItem("__global__" + key, value);
            }
        }

       public List<string> getKeys  () {
            List<string> allKeys = _sharedstate.keys();
            //System.Console.WriteLine("allkeys "+allKeys.Count);
            List<string> globalVars = new List<string>();
            for (int i = 0, len = allKeys.Count; i < len; i++) {
                if (allKeys[i].IndexOf("__global__") > -1) {
                    globalVars.Add(allKeys[i].Substring(10));
                }
            }
            return globalVars;
        }

       public string getItem (string key) {
            return _sharedstate.getItem("__global__" + key);
        }      
    void _update_subscriptions(string _aid2, List<string> _subs) {
        try {
                Console.WriteLine("subs " + _subs);
            if (_subs==null){
                string tnp = _sharedstate.getItem("__metasub__" + _aid2);
                JArray list = JArray.Parse(tnp);
                _subs = list.ToObject<List<string>>();
                 
            }
            if (_subs==null) {
                System.Console.WriteLine("Not subs");
            }
            else {
                //List<Action<string>> handlers = getAgent(_aid2).handlers;
                List<string> thesubs;
                if (! _sub_agent2param.ContainsKey(_aid2) ){
                   thesubs = new List<string>();
                }
               else  thesubs = _sub_agent2param[_aid2];
                for (int i = 0; i < _subs.Count; i++) {
                    var target_agent = _subs[i].Substring(0, _subs[i].IndexOf("_"));
                    var target_param = _subs[i].Substring(_subs[i].IndexOf("_") + 1);
                    if (target_agent.Equals(_sharedstate.agentID)) {
                        // See if this agent already subscribes to my parameter, otherwise add it
                        if (thesubs.IndexOf(target_param) > -1) {
                            // Aready subscribed to this one
                            continue;
                        }
                        if (!_sub_param2agent.ContainsKey(target_param) ) {
                            _sub_param2agent[target_param] = new List<string>();
                        }
                        _sub_param2agent[target_param].Add(_aid2);
                        if (!_sub_param2handler.ContainsKey(target_param)) {
                            _sub_param2handler[target_param] =  (e) => {
                                 JObject d= JObject.Parse(e);
                                _sharedstate.setItem("__val__" + _sharedstate.agentID + "_" + d["key"].ToString(), d["value"].ToString());
                            };
                        }
                        this.self_ag.on(target_param, _sub_param2handler[target_param]);
                    }
                }
                // see if there was an unsubscribe
                for (int i = 0; i < thesubs.Count; i++) {
                     List<string> keys = new List<string>(_sub_param2agent.Keys);

                    if (keys.IndexOf(thesubs[i]) == -1) {
                        var target_agent = thesubs[i].Substring(0, thesubs[i].IndexOf("_"));
                        var target_param = thesubs[i].Substring(thesubs[i].IndexOf("_") + 1);
                        if (!target_agent.Equals(_sharedstate.agentID)) {
                            continue;
                        }
                        if (_sub_param2agent.ContainsKey(target_param)) {
                         //   _sub_param2agent[target_param]= Utils.Splice(_sub_param2agent[target_param],_sub_param2agent[target_param].IndexOf(thesubs[i]), 1);
                          //  if (_sub_param2agent[target_param].Count == 0) {
                                // Only unsub if no more agents listen to this
                                // TO CHECK IT: OFF  WITHOUT REASON
                                //_the_self.off(target_param, _sub_param2handler[target_param]);
                          //  }
                        } else {
                            
                            //System.Console.WriteLine(" *** Warning, unsub but don't have handler");
                        }
                        // TODO: Clean up if nobody uses it any more
                        // if _sub_param2agent[target_param].length == 0
                    }
                }
                _sub_agent2param[_aid2] = _subs;
            }
            }
            catch(Exception ex){
                System.Console.WriteLine("Updated subscription"+ex);   
            }
        }
       void _update_value  (string _aid,string _key,string _val) {
            RemoteAgent ra = getAgent(_aid);
            if (ra!=null) {
                ra.update_value(_aid, _key, _val);
            }
        }

      public void on (string what, Action<string> handler) {
            if (what.Equals("agentchange")) {
                _cbs.Add(handler);
            } else {
                if (!_globalCbs.ContainsKey(what)) {
                    _globalCbs[what] = new List<Action<string>>();
                }
                System.Console.WriteLine(what,_globalCbs);
                _globalCbs[what].Add(handler);
            }

        }

      public void off (string what, Action<string> handler) {
            if (what.Equals("agentchange")) {
                if (_cbs.IndexOf(handler) == -1) {
                    //System.Console.WriteLine("Error handler not registered");
                }
             //   _cbs.splice(_cbs.indexOf(handler), 1);
            } else {
                if (_globalCbs[what].IndexOf(handler) == -1) {
                     //System.Console.WriteLine("Error handler not registered");
                }
              //  _globalCbs[what].splice(_globalCbs[what].indexOf(handler), 1);
            }

        }
    public void close(){
        this._sharedstate.disconnect();
    }      
    void _doCallbacks  (string what, object d) {
            //System.Console.WriteLine("DOCALLBACK1"+d);
            JObject caps = new JObject();
            string json = JsonConvert.SerializeObject(d);
             json = json.Replace("\"[", "[");
             json = json.Replace("\"{", "{");
             json = json.Replace("\\n", "");
             json = json.Replace("\\", "");
             json = json.Replace("}\"}", "}}");
             json = json.Replace("]\"}", "]}");
             if (json[0]=='"')json = json.Substring(1,json.Length-2);
             else if (json[json.Length-1]=='"') json = json.Substring(0,json.Length-1);
             //System.Console.WriteLine("DOCALLBACK1 JSON"+json);    
            JObject e = JObject.Parse(json);
            
             //System.Console.WriteLine("DOCALLBACK1 JSON"+what);    
            if (what.Equals("agentchange")) {
                string agentid = e["agentid"].ToString();
                
                if (e["agentContext"].ToString().Equals("null")) {
                    _currentAgentState.Remove(agentid);
                     //System.Console.WriteLine(_currentAgentState);
                } else if (!_currentAgentState.ContainsKey(agentid)) {
                    _currentAgentState[agentid] = new JObject();
                    _currentAgentState[agentid]["capabilities"] = new JObject();
                    _currentAgentState[agentid]["keys"] = new JArray();
                        
                }
                //System.Console.WriteLine("DOCALLBACK3"+e);
                // Create diff
                JObject tnp = new JObject();
                tnp["capabilities"] = new JObject();
                tnp["keys"] = new JArray();
                e["diff"] = tnp;
                //e["diff"]["capabilities"]=new JArray();
                //e["diff"]["keys"]= new JArray();
                try{
               
               if (!e["agentContext"].ToString().Equals("null")) {
                  //System.Console.WriteLine(e["agentid"].ToString());
                  PublicAPI api = getAgent((e["agentid"].ToString())).publicAPI;
                  JObject cs = api.capabilities();
                    foreach (var c in cs) {
                        //System.Console.WriteLine("keya "+c.Key+ cs[c.Key.ToString()]);
                        string c1 = c.Key.ToString();
                        if (cs[c1]!=null) {
                              if (!_currentAgentState.ContainsKey(api.agentID)){
                                 _currentAgentState.Add(api.agentID,new JObject());
                              }
                            if ( _currentAgentState[api.agentID].Property("capabilities")==null) {
                               
                                _currentAgentState[api.agentID]["capabilities"] = new JObject();
                                caps = new JObject();
                            }else {
                                caps = (JObject) _currentAgentState[api.agentID]["capabilities"]; 
                            }
                             if (caps.Property(c1)==null) {
                                 e["diff"]["capabilities"][c1] = cs[c1];
                             }
                             else {
                                 if (caps[c1].ToString().Equals(cs[c1])){
                                      e["diff"]["capabilities"][c1] = cs[c1];
                                 }
                             }
                        }
                    }
                    System.Console.WriteLine("DOCALLBACK for" + api.keys());
                   
                   JArray keys = api.keys();
                    for (int i = 0; i < keys.Count; i++) {
                        if (!_currentAgentState.ContainsKey(api.agentID)){
                            _currentAgentState.Add(api.agentID,new JObject());
                        }
                        if ( _currentAgentState[api.agentID].Property("keys")==null) {
                               
                                _currentAgentState[api.agentID]["keys"] = new JArray();
                               
                            }
                        if ((_currentAgentState[api.agentID]["keys"]as JArray).IndexOf(keys[i]) == -1) {
                            (e["diff"]["keys"] as JArray).Add(keys[i]);
                        }
                    }
                   _currentAgentState[api.agentID] = new JObject();
                   _currentAgentState[api.agentID]["capabilities"] = JObject.Parse(JsonConvert.SerializeObject(cs));
                   _currentAgentState[api.agentID]["keys"] =  JArray.Parse(JsonConvert.SerializeObject(keys));;
                   
                
               }
                 System.Console.WriteLine("DOCALLBACK berfore call"+e);
                for (int i = 0; i < _cbs.Count; i++) {
                    try {
                        _cbs[i].Invoke(JsonConvert.SerializeObject(e));
                    } catch (Exception err) {
                       System.Console.WriteLine("Error in agentchange callback: "+ err);
                       //System.Console.WriteLine("cb:"+ _cbs[i]);
                    }
                }  } catch(Exception ex){
                System.Console.WriteLine("expection app: "+ex );
            }
               
            } else {
                for (int i = 0, len = _globalCbs[what].Count; i < len; i++) {
                    try {
                        System.Console.WriteLine(what, i);
                        _globalCbs[what][i].Invoke(JsonConvert.SerializeObject(e));
                    } catch (Exception err) {
                       System.Console.WriteLine("Error in agentchange callback: "+ err);
                       //System.Console.WriteLine("cb:"+  _globalCbs[what][i]);
                    }
                }
            }
          
            
            

        }

     RemoteAgent remoteAgentContext (string __agentid) {
            RemoteAgent self = new RemoteAgent();
            self.handlers = new  Dictionary<string,List<Action<string>>>();
            self.handlers.Add("agentchange", new List<Action<string>>());
            
            
            self.agentID = __agentid;
            _sharedstate.on("change", (e) => {
               // System.Console.WriteLine("aldekaera"+e);
                JObject d = JObject.Parse(e);
                if (d["key"].ToString().IndexOf("__val__" + self.agentID) == 0) {
                    //System.Console.WriteLine("Handlers "+e+new String(self.handlers.SelectMany(a => $"{a.Key}: {a.Value} {Environment.NewLine}").ToArray()));
                    // Updated values for my stuff
                    if (self.handlers.ContainsKey(d["key"].ToString()) & self.handlers[d["key"].ToString()].Count > 0) {
                        for (int i = 0; i < self.handlers[d["key"].ToString()].Count; i++) {
                            try {
                                
                                self.handlers[d["key"].ToString()][i].Invoke(e);
                            } catch (Exception ee) {
                                System.Console.WriteLine("Error in callback: remote change"+ee );
                            }
                        }
                    }
                }
            },null);
             JArray keys  () {
                string meta = _sharedstate.getItem("__meta__" + self.agentID);
                if (meta!=null){
                    meta = meta.Replace("\\n", "");
                    meta = meta.Replace("\\", "");
                    meta = meta.Substring(1,meta.Length-2);
                    meta = "[" + meta +"]";
                    System.Console.WriteLine("meta keys agtx: "+meta);
                    JArray _meta = JArray.Parse(meta);
                    if (_meta==null) return new JArray();
                    return  JArray.FromObject(_meta[0]["keys"]);
                }
                else {
                       return new JArray();
                }
            };

            JObject capabilities  () {
                string meta  = _sharedstate.getItem("__meta__" + self.agentID);
                if (meta!=null){
                    meta = meta.Replace("\\n", "");
                    meta = meta.Replace("\\", "");
                    meta = meta.Substring(1,meta.Length-2);
                    meta = "[" + meta +"]";
                    //System.Console.WriteLine("meta " +meta);
                    JArray _meta = JArray.Parse(meta);
                    if (_meta==null)  return new JObject();
                    return (JObject)_meta[0]["capabilities"];
                }
                else {
                       return new JObject();
                }

            }

            string on (string what,Action<string> handler) {
                if (what.Equals("agentchange")) {
                    self.handlers["agentchange"].Add(handler);
                    return "";
                }
                System.Console.WriteLine("Giltzak;"+keys()+" "+_sharedstate.agentID);
                // if (keys().TakeWhile(x => x.ToString() == what).Count() == 0) {
                //     //System.Console.WriteLine("unknow parameter"+ what);
                // }
                
                string subscriptions = _sharedstate.getItem("__metasub__" + _sharedstate.agentID);
                //System.Console.WriteLine("subs:" + subscriptions);
                JArray _subscriptions;
                if (subscriptions == null)  _subscriptions = new JArray();
                else  {
                     try{   
                     _subscriptions = JArray.Parse(subscriptions);
                     }
                     catch (Exception ex){
                          _subscriptions = new JArray();
                     }
                }
                string item = self.agentID + "_" + what;

                if (_subscriptions.IndexOf(item) == -1) {
                    _subscriptions.Add(item);
                    Console.WriteLine("<<<subscription " + _subscriptions);
                    _sharedstate.setItem("__metasub__" + _sharedstate.agentID, JsonConvert.SerializeObject(_subscriptions));
                }
                // Remember the handler
                 //System.Console.WriteLine("Handlers "+self.handlers);
                if (!self.handlers.ContainsKey(what) ) {
                    self.handlers[what] = new List<Action<string>>();
                }
                 //System.Console.WriteLine("Handlers2 "+self.handlers);
                 try {
                     self.handlers[what].Add(handler);
                 }
                 catch (Exception exx){
                     //System.Console.WriteLine("Adding handler"+exx);
                 }
                //System.Console.WriteLine("Handlers3 "+self.handlers);

                // check if we have a value already
                string value = _sharedstate.getItem("__val__" + item);
                if (value!=null) {
                    self.handlers[what][0].Invoke(JsonConvert.SerializeObject(value));
                   // handler.Invoke(what, value);
                }
                return "";
            }

            string off (string what, Action<string> handler) {
                // if (what.Equals("agentchange")) {
                //     self.handlers[what].splice(self.handlers[what].indexOf(handler), 1);
                //     return;
                // }
                // if (keys().indexOf(what) == -1) {
                //     throw "Unknown parameter " + what;
                // }
                // var subscriptions = clone(_sharedstate.getItem("__metasub__" + _sharedstate.agentid));
                // var item = self.agentid + "_" + what;

                // if (subscriptions.indexOf(item) > -1) {
                //     subscriptions.splice(subscriptions.indexOf(item), 1);
                //     _sharedstate.setItem("__metasub__" + _sharedstate.agentid, subscriptions);
                // } else {
                //     throw "Not subscribed to " + what;
                // }
                // // TODO: Clean up self.handlers[what]
                // if (!self.handlers[what]) {
                //     console.log("*** Warning: Missing handler for " + what);
                //     return;

                // }
                // self.handlers[what].splice(self.handlers[what].indexOf(item), 1);
                return "";
            }

            string update_meta (string meta) {
                //System.Console.WriteLine("update_meta "+meta);
                JObject _meta = JObject.Parse(meta);
                JObject caps = (JObject)_meta["capabilities"]; 
                Dictionary<string,JArray> diff = new Dictionary<string,JArray>();
                diff.Add("added",new JArray());
                diff.Add("altered",new JArray());
                diff.Add("removed",new JArray());
                foreach(KeyValuePair<string, string> entry in _last_capabilities){
                    if (caps[entry.Key]==null){
                            diff["removed"].Add(entry.Key);
                    }
                }

                
                // for (var capability in meta.capabilities) {
                //     if (_last_capabilities[capability] === undefined) {
                //         diff.added[capability] = meta.capabilities[capability];
                //     } else if (_last_capabilities[capability] != meta.capabilities[capability]) {
                //         diff.altered[capability] = meta.capabilities[capability];
                //     }
                // }
                // _last_capabilities = meta.capabilities;

                // for (var i = 0; i < self.handlers["agentchange"].length; i++) {
                //     try {
                //         self.handlers["agentchange"][i].call(self.publicAPI, {
                //             diff: diff
                //         });
                //     } catch (err) {
                //         console.log("Error in meta-update", err);
                //     }
                // }
                return "";
            }

            string update_value  (string _aid,string _key, string _value) {
                if (!self.handlers.ContainsKey(_key)) {
                    return "";
                }
                for (int i = 0; i < self.handlers[_key].Count; i++) {
                    try {
                         var obj = new {
                             key=_key,
                             value = _value,
                             agentid = _aid
                         };
                        self.handlers[_key][i].Invoke(JsonConvert.SerializeObject(obj));
                    } catch (Exception err) {
                        //System.Console.WriteLine("Error in update: "+ err);
                    }
                }
                return "";
            }

            string setItem (string key, string value) {
                _sharedstate.setItem("__val__" + self.agentID + "_" + key, value);
                return "";
            }
            string getItem (string key) {
                return _sharedstate.getItem("__val__" + self.agentID + "_" + key);
            }
            self.update_value = update_value;
            self.update_meta = update_meta;

            
             self.publicAPI.agentID = self.agentID;
             self.publicAPI.keys = keys;
             self.publicAPI.on = on;
             self.publicAPI.off = off;
             self.publicAPI.setItem = setItem;
             self.publicAPI.getItem = getItem;
             self.publicAPI.capabilities = capabilities;
            
        return self;
    }


}
}