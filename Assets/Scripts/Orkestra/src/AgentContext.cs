using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace OrkestraLib
{

public class AgentContext {
  System.Object self = new System.Object(){};
  Dictionary<string,ContextItem> _contextItems = new Dictionary<string,ContextItem>(); // Map key to {currentValue:null, callbacks:[], on:func, off:func}
  List<Action<string>> _change_callbacks = new List<Action<string>> ();
  Dictionary<string,string> _capabilities = new Dictionary<string,string>();
  string agentID="";
  List<Action<string>> _agent_update_handlers = new List<Action<string>>();



  public AgentContext(string agentID){
      this.agentID = agentID;
      //System.Console.WriteLine("AgentContext init");
      InstrumentMap inst = new InstrumentMap();
      inst.val="data";
      inst.init = (e)=>{
        //System.Console.WriteLine("myPosition INIT function"+e);
        this.setCapability("data","supported");
        return null;
      };
      inst.on = (e)=>{
        //System.Console.WriteLine("myPosition ON function"+e);
        this.setItem("data","{\"position\":[0,0,0],\"rotation\":[0,0,0]}");
        return null;
      };
            inst.off = (e) =>
            {
                //System.Console.WriteLine(e);
                return null;
            };
      this._addContextElements(inst);
  }

  public void _agent_change_cb(){
    for (int i = 0; i < _agent_update_handlers.Count; i++) {
      try {
        _agent_update_handlers[i].Invoke("{agentid:\""+this.agentID+"\"");
      } catch (Exception ex) {
        System.Console.WriteLine("Error in meta-update" + ex);
      }
    }
    System.Console.WriteLine("AgentContext_agent_change_cb" + _agent_update_handlers);
  }

  public void setCapability(string what, string state){
            try
            {
                Console.WriteLine("setCap " + what + state);
                _capabilities[what] = state;
                _agent_change_cb();
                _doCallbacks(what, "keychange", null);
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
  }
  public void _addContextElements(InstrumentMap instrumentMap) {
	
	  ContextItem c1 = new ContextItem (
	    instrumentMap.val,
	    new List<Action<string>>(),
	    instrumentMap.on,
        instrumentMap.off,
        instrumentMap.init
	  );
	  _contextItems.Add(instrumentMap.val,c1);
                    
       System.Console.WriteLine("AgentContext_init function running");
       instrumentMap.init.Invoke(this);
    
      
      System.Console.WriteLine("AgentContext_addContexts");
      _agent_change_cb();
      //return self;
  }
  
  public void _doCallbacks(string what, string e, Action<string> handler){
    System.Console.WriteLine("AgentContext_do_callback "+what);
  	if (what.Equals("keychange")) {
                for (int i = 0; i < _change_callbacks.Count; i++) {
                    try {
                        _change_callbacks[i].Invoke(e);
                    } catch (Exception ex) {
                        System.Console.WriteLine("Error in keychange handler:" + ex);
                    }
                }
                return;
            }
      if (_contextItems.GetType().GetProperty(what)!=null) 
	    {
	    	//System.Console.WriteLine ("Unsupported event " + what);
            }
            Action<string> h;
            for (int i = 0; i < _contextItems[what].callbacks.Count; i++) {
                h = _contextItems[what].callbacks[i];
                // if (handler == null) {
                //     // all handlers to be invoked, except those with pending immeditate
                //     if (h._immediate_pending) {
                //         continue;
                //     }
                // } else {
                //     // only given handler to be called
                //     if (h == handler) handler._immediate_pending = false;
                //     else {
                //         continue;
                //     }
                // }
                try {
                    h.Invoke(e);
                } catch (Exception ex) {
                    System.Console.WriteLine("Error in " + what + ": " + h + ": " + ex);
                }
            }
  }
   public string off  (string what, Action<string> handler) { 
       return "";
   }
    public string on (string what, Action<string> handler) {
            System.Console.WriteLine("AgentContext_ON " + what);
            if (what.Equals("keychange")) {
                _change_callbacks.Add(handler);
                Console.WriteLine("<<<< key change " + keys());
                handler.Invoke(JsonConvert.SerializeObject(keys()));
                return "";
            }
            if (what.Equals("agentchange")) {
                System.Console.WriteLine("*** Registered agentchange");
                _agent_update_handlers.Add(handler);
                try {
                    handler.Invoke("{agentid:\""+this.agentID+"\""); //self as parameter
                } catch (Exception err) {
                    System.Console.WriteLine("Error in agentchange handler" + err);
                }
                return "";
            }
            //if (!handler || typeof handler !== "function") throw "Illegal handler";
            if (!_contextItems.ContainsKey(what)) System.Console.WriteLine( "Unsupported event " + what);
            int index = -1;
            for (int i = 0; i< _contextItems[what].callbacks.Count;i++){
                if (_contextItems[what].callbacks[i].Equals(handler)) index = i;
            }
            
            if (index == -1) {
                if (_contextItems[what].callbacks.Count == 0) {
                    // Turn on?
                       
                            //System.Console.WriteLine("Turning " + what + " on"+_contextItems[what]);
                            try {
                            _contextItems[what].on.Invoke(this); //self as parameter
                            }
                            catch (Exception ex){
                                System.Console.WriteLine("Exception agentContext on" +ex);
                            }
                    
                }
                // register handler
                _contextItems[what].callbacks.Add(handler);
                // flag handler
              //  handler._immediate_pending = true;
                // do immediate callback
                //setTimeout(function () {
                    _doCallbacks(what, JsonConvert.SerializeObject(new {
                        key= what,
                        value = _contextItems[what].currentValue
                    }), null);
                //}, 15);
            }
          
      return "";
    }
    public  void setItem (string what, string value){
      if (!_contextItems.ContainsKey(what)) {
                //System.Console.WriteLine("Adding"+what);
                Dictionary<string,object> i = new Dictionary<string,object>();
                InstrumentMap imap = new InstrumentMap(value);
                
                _addContextElements(imap);
            }

            _contextItems[what].currentValue = value;

            // Do callbacks on it
            _doCallbacks(what, JsonConvert.SerializeObject(new {
                key = what,
                value = value
            }),null);
      
      
    }
    public string getItem (string what){
        if (!_contextItems.ContainsKey(what)) {
                //System.Console.WriteLine("Unknown item " + what);
            }
        return _contextItems[what].currentValue;
    }
    public Dictionary<string,string> capabilities() {
        return this._capabilities;
    }
    public JArray keys () {
            JArray res = new JArray();
            System.Console.WriteLine("contextItems"+ _contextItems);
            foreach ( var item in _contextItems) {
                res.Add(item.Key);
            }
          return res;
        }

}
}

