using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using SocketIOClient;
namespace OrkestraLib
{
    public class SharedState
    {
        string url = "";
        SocketIO _connection;
        System.Object self = new System.Object() { };
        string _readystate = "connecting";
        string presence = "";
        // READY STATE for Shared State


        // Event Handlers
        Dictionary<string, object> _presence = new Dictionary<string, object>();
        public Dictionary<string, object> _sharedStates = new Dictionary<string, object>();
        Dictionary<string, object> _stateChanges = new Dictionary<string, object>();
        Dictionary<string, List<Action<string>>> _callbacks = new Dictionary<string, List<Action<string>>>();
        public Dictionary<string, string> STATE = new Dictionary<string, string>();

        bool _request = false;
        bool connected = false;
        public string agentID = "";


        public SharedState(string url, string agentID)
        {
            this.url = url;
            this.agentID = agentID;
            //System.Console.WriteLine("SharedState init");
            STATE.Add("CONNECTING", "connecting");
            STATE.Add("OPEN", "open");
            STATE.Add("CLOSED", "closed");
            _callbacks.Add("change", new List<Action<string>>());
            _callbacks.Add("remove", new List<Action<string>>());
            _callbacks.Add("readystatechange", new List<Action<string>>());
            _callbacks.Add("presence", new List<Action<string>>());
            //_connection["connected"] = false;
        }


        public async void init()
        {
            System.Console.WriteLine("init sharedState"+this.url);
            this._connection = new SocketIO(this.url, new SocketIOOptions
            {
                EIO = 3
            });

            await this._connection.ConnectAsync();

            this._connection.On("connected", onConnect);
            this._connection.On("disconnect", onDisconnect);
            this._connection.On("joined", onJoined);
            this._connection.On("status", onStatus);
            this._connection.On("changeState", onChangeState);
            this._connection.On("initState", onInitState);
            this._connection.On("ssError", onError);
            if (this.connected == true)
            {
                onConnect(null);
            }

            this.readystate("connecting");
            System.Console.WriteLine("change sharedState" + this.url);
        }




        /* sockets events */
        void onConnect(SocketIOResponse obj)
        {
            System.Console.WriteLine("SharedStae connected");
            this.readystate("connecting");
            var datagram = new
            {
                agentID = this.agentID
            };
            _sendDatagram("join", JsonConvert.SerializeObject(datagram));
            this.connected = true;

        }

        void onDisconnect(SocketIOResponse obj)
        {
            this.readystate("disconnecting");
            Console.WriteLine("DISCONNECTING");
        }
        void onJoined(SocketIOResponse data)
        {
            try
            {
                System.Console.WriteLine("SharedStae joined");
                string json = JsonConvert.SerializeObject(data.GetValue());
                System.Console.WriteLine("SharedStae joined" + json);
                JObject datagram = JObject.Parse(json);
                //System.Object dataGram = JsonConvert.DeserializeObject(data.ToString()) as System.Object;
                System.Console.WriteLine("joined"+datagram["agentID"]+ " " + this.agentID);
                if (datagram["agentID"].ToString().Equals(this.agentID))
                {
                    //System.Console.WriteLine("joined"+datagram["agentID"]);
                    int[] array = {

            };

                    _connection.EmitAsync("getInitState", array);
                }
                else
                {
                    readystate("open");
                    setPresence("online");
                }
                cleanLoop();
            }
            catch (Exception ex) {
                System.Console.WriteLine(ex);
            }

        }
        void onStatus(SocketIOResponse data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data.GetValue());
                System.Console.WriteLine("SharedStae status change" + json);
                JObject datagram = JObject.Parse(json);
                JArray __presence = datagram["presence"] as JArray;
                //System.Object dataGram = JsonConvert.DeserializeObj
                //System.Console.WriteLine("SharedStae status count" +__presence.Count);
                for (int i = 0; i < __presence.Count; i++)
                {
                    if ((__presence[i] != null & __presence[i]["key"] != null))
                    {
                        if (__presence[i]["value"].ToString().Equals("connected")) __presence[i]["value"] = "online";
                        var presence = "{\"key\" :\"" + __presence[i]["key"].ToString() + "\",\"value\":\"" + __presence[i]["value"].ToString() + "\"}";
                        //System.Console.WriteLine("Inside ");
                        _presence[__presence[i]["key"].ToString()] = __presence[i]["value"].ToString();
                        this._do_callbacks("presence", presence, null);
                    }
                    else
                    {
                        System.Console.WriteLine("SHAREDSTATE - reveived 'presence' already saved or something wrong" + __presence[i]);
                    }

                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }
        void onChangeState(SocketIOResponse data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data.GetValue());
                JArray datagram = JArray.Parse(json) as JArray;
                for (int i = 0; i < datagram.Count; i++)
                {
                    System.Console.WriteLine("set -- change state" + json + datagram[i]["key"]);
                    if (datagram[i]["type"].ToString() == "set")
                    {
                        System.Console.WriteLine("set -- change state" + _sharedStates);
                        JObject state;
                        if (!_sharedStates.ContainsKey(datagram[i]["key"].ToString()))
                        {
                            state = new JObject();

                            state.Add("key", datagram[i]["key"].ToString());
                            state.Add("value", datagram[i]["value"].ToString());
                            state.Add("type", "add");

                        }
                        else {
                            state = new JObject();
                            state.Add("key", datagram[i]["key"].ToString());
                            state.Add("value", datagram[i]["value"].ToString());
                            state.Add("type", "add");
                            state["type"] = "update";

                        }
                            //System.Console.WriteLine("Set inside -- change state");

                           /* if (_sharedStates.ContainsKey(datagram[i]["key"].ToString()))
                            {
                                state["type"] = "update";
                            }
                            */

                            //System.Console.WriteLine("Set inside after -- change state");
                            _sharedStates[datagram[i]["key"].ToString()] = datagram[i]["value"].ToString();
                            //System.Console.WriteLine("Before callback -- change state");
                            this._do_callbacks("change", JsonConvert.SerializeObject(state), null);


                    }
                    else if (datagram[i]["type"].ToString() == "remove")
                    {
                        if (datagram[i]["key"].ToString() != "" & _sharedStates.ContainsKey(datagram[i]["key"].ToString()))
                        {
                            dynamic o = JObject.Parse(_sharedStates[datagram[i]["key"].ToString()].ToString());
                            dynamic state = new
                            {
                                key = datagram[i]["key"].ToString(),
                                value = o.value,
                                type = "delete"
                            };
                            _sharedStates.Remove(datagram[i]["key"].ToString());
                            this._do_callbacks("remove", JsonConvert.SerializeObject(state), null);
                        }

                    }

                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
            //System.Console.WriteLine("fin -- change state");
        }
        void onInitState(SocketIOResponse data)
        {
            System.Console.WriteLine("initState");
            try
            {
                string json = JsonConvert.SerializeObject(data.GetValue());
                JArray states = JArray.Parse(json) as JArray;
                //System.Console.WriteLine(states.Count);
                foreach (dynamic state in states)
                {
                    if (state.type == "set")
                    {
                        ////System.Console.WriteLine("j2oined"+_sharedStates.ContainsKey(state["key"].ToString()));
                        if (_sharedStates.ContainsKey(state["key"].ToString()))
                        {
                            //System.Console.WriteLine("j2oined "+state["key"]);
                            if (_sharedStates[state["key"].ToString() != state["value"].ToString()])
                            {
                                //System.Console.WriteLine("Testing");
                                _sharedStates[state["key"].ToString()] = state["value"].ToString();
                                var _state = new
                                {
                                    key = state["key"].ToString(),
                                    value = state["value"].ToString(),
                                    type = "add"
                                };
                                _sharedStates[state["key"].ToString()] = state["value"].ToString();

                                this._do_callbacks("change", JsonConvert.SerializeObject(_state), null);
                            }
                        }
                        else
                        {
                            _sharedStates.Add(state["key"].ToString(), state["value"].ToString());

                        }

                    }

                }
                readystate("open");
                this.presence = "online";
                setPresence("online");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            //System.Console.WriteLine("fin -- initState");
        }
        void onError(SocketIOResponse data)
        {
            System.Console.WriteLine("Error->>" + data.GetValue());
        }

        /* Public functions to interactive whit SS */

        public void setItem(string key, string value)
        {
            if (_request)
            {
                var state = new
                {
                    type = "set",
                    key = key,
                    value = value
                };

                _stateChanges[key] = state;
            }
            else
            {
                if (readystate(null) == "open")
                {
                    if (key != null)
                    {
                        var datagram = "";
                        //System.Console.WriteLine("SHAREDSTATE "+value);
                        try
                        {
                            if (!(value.Equals("[]") | value.Equals("{}"))) JsonConvert.DeserializeObject(value);

                            datagram = "[{\"type\": \"set\",\"key\": \"" + key + "\",\"value\":" + value + "}]";
                            _sendDatagram("changeState", datagram);
                        }
                        catch (Exception ex)
                        {
                            datagram = "[{\"type\": \"set\",\"key\": \"" + key + "\",\"value\":\"" + value + "\"}]";
                            _sendDatagram("changeState", datagram);
                            System.Console.WriteLine("exception setItem "+ex+value);   
                        }


                    }
                    else
                    {
                        System.Console.WriteLine("SHAREDSTATE - params with error - key:" + key + "value:" + value);
                    }
                }
                else
                {
                    System.Console.WriteLine("SHAREDSTATE - setItem not possible - connection status:" + readystate(null));
                }
            }


        }
        public string getItem(string key)
        {

            if (key == null)
            {
                int[] datagram = { };
                _sendDatagram("getState", JsonConvert.SerializeObject(datagram));

            }
            else
            {

                if (_sharedStates.ContainsKey(key))
                {
                    return JsonConvert.SerializeObject(_sharedStates[key]);
                }
                else return null;
            }
            return null;
        }

        public void removeItem(string key)
        {
            if (_request)
            {
                var state = new
                {
                    type = "remove",
                    key = key
                };
                _stateChanges[key] = state;
            }
            else
            {

                if (_sharedStates.ContainsKey(key.ToString()))
                {
                    string datagram = "[{\"type\": \"remove\",\"key\": \"" + key.ToString() + "\"}]";
                    this._connection.EmitAsync("changeState", JsonConvert.DeserializeObject(datagram));
                }
                else
                {
                    ////System.Console.WriteLine("SHAREDSTATE - key with error - key:" + key);
                }
            }

        }
        public List<string> keys()
        {
            //System.Console.WriteLine("keys "+this._sharedStates.Keys);
            return new List<string>(this._sharedStates.Keys);

        }
        public void request()
        {
            _request = true;

        }
        public void send()
        {
            if (readystate(null) == "open")
            {
                _request = false;
                List<string> keys = new List<string>(this._stateChanges.Keys);

                if (keys.Count > 0)
                {
                    JArray datagram = new JArray();
                    for (int i = 0; i < keys.Count; i++)
                    {
                        datagram.Add(_stateChanges[keys[i]]);
                    }
                    _sendDatagram("changeState", JsonConvert.SerializeObject(datagram));
                    this._stateChanges = new Dictionary<string, object>();
                }
            }
            else
            {
                System.Console.WriteLine ("SHAREDSTATE - send not possible - connection status:" + readystate(null));
            }

        }
        string readystate(string new_state)
        {

            if (new_state != null)
            {
                bool found = false;
                List<string> keys = new List<string>(this.STATE.Keys);

                foreach (string key in keys)
                {
                    if (!STATE.ContainsKey(key)) continue;
                    if (STATE[key].Equals(new_state)) found = true;
                }
                if (!found) //System.Console.WriteLine("Illegal state value " + new_state);
                    // check state transition
                    if (_readystate == "closed") return null; // never leave final state
                                                              // perform state transition
                if (!new_state.Equals(_readystate))
                {
                    _readystate = new_state;
                    // trigger events
                    _do_callbacks("readystatechange", new_state, null);
                }
            }
            else return _readystate;
            return null;
        }
        /*internal functions */
        public async void _sendDatagram(string type, string datagram)
        {
            //System.Console.WriteLine("datagram: "+type+" "+" "+datagram);
            try
            {
                await this._connection.EmitAsync(type, JsonConvert.DeserializeObject(datagram));
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Sending datagram"+ex);
            }
        }
        /** call to local listeners **/
        void _do_callbacks(string what, string e, object handler)
        {
           // System.Console.WriteLine ("callbacks " + what+ " "+e);
            if (!_callbacks.ContainsKey(what)) System.Console.WriteLine("Unsupported event " + what);
            Action<string> h;
            for (int i = 0; i < _callbacks[what].Count; i++)
            {
                h = _callbacks[what][i];
                // if (handler == undefined) {
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
                try
                {
                    // if (h._ctx) {
                    //     h.Invoke(h._ctx, e);
                    // } else {
                    //System.Console.WriteLine ("Invoking " + what);
                    h.Invoke(e);
                    //  }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("Error in " + what + ": " + h + ": " + ex);
                }
            }
        }
        void _autoClean(string agentid)
        {

            ////System.Console.WriteLine("*** Cleaning agent "+ agentid);
            // Go through the dataset and remove anything left from this node
            foreach (var key in _sharedStates)
            {
                if (_sharedStates.ContainsKey(key.Key))
                {
                    if (key.Key.IndexOf("__") == 0 && key.Key.IndexOf("__" + agentid) > -1)
                    {
                        //  removeItem(key.Key);
                    }
                }
            }
        }

        public void setPresence(string state)
        {

            if (state.Equals("online"))
            {

                string datagram = "{\"agentID\": \"" + this.agentID + "\",\"presence\": \"online\"}";
                try
                {
                    this._connection.EmitAsync("changePresence", JsonConvert.DeserializeObject(datagram));

                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("SetPresence" +ex);
                }

            }


        }

        void cleanLoop()
        {

            foreach (var key in _sharedStates)
            {
                if (_sharedStates.ContainsKey(key.Key))
                {
                    if (key.Key.IndexOf("__meta__") == 0)
                    {
                        string agentid = key.Key.Substring(8);
                        if (_presence.ContainsKey(agentid))
                        {
                            _autoClean(agentid);
                        }

                    }
                }
            }

        }
        /** listen for event **/
        public void on(string what, Action<string> handler, string ctx)
        {
            try
            {
                if (!_callbacks.ContainsKey(what)) Console.WriteLine("Unsupported event " + what);
                // if (ctx) {
                //     handler._ctx = ctx;
                // }
                var index = _callbacks[what].IndexOf(handler);
                if (index == -1)
                {
                    // register handler
                    _callbacks[what].Add(handler);
                    // flag handler
                    // handler._immediate_pending = true;
                    // do immediate callback

                    switch (what)
                    {
                        case "change":

                            List<string> keys = new List<string>(this._sharedStates.Keys);
                            //  if (keys.Count === 0) {
                            //      handler._immediate_pending = false;
                            // } else {
                            for (int i = 0, len = keys.Count; i < len; i++)
                            {
                                var state = new
                                {
                                    key = keys[i],
                                    value = _sharedStates[keys[i]],
                                    type = "update"
                                };
                                this._do_callbacks("change", JsonConvert.SerializeObject(state), handler);
                            }
                            // }
                            break;
                        case "presence":
                            List<string> keys2 = new List<string>(this._presence.Keys);
                            // if (keys.length === 0) {
                            //     handler._immediate_pending = false;
                            // } else {
                            for (int i = 0, len = keys2.Count; i < len; i++)
                            {
                                var msg = "{\"key\" :\"" + keys2[i] + "\",\"value\":\"" + this._presence[keys2[i]] + "\"}";

                                //System.Console.WriteLine(JsonConvert.SerializeObject(this._presence));
                                this._do_callbacks("presence", msg, handler);
                            }
                            //}
                            break;
                        case "remove":
                            // handler._immediate_pending = false;
                            break;
                        case "readystatechange":
                            this._do_callbacks("readystatechange", this.readystate(null), handler);
                            break;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
        public void off(string what, Action<string> handler)
        {
            if (_callbacks[what] != null)
            {
                var index = _callbacks[what].IndexOf(handler);
                if (index > -1)
                {
                    _callbacks[what] = Utils.Splice(_callbacks[what], index, 1);
                }
            }

        }
        public async void disconnect()
        {
            await this._connection.DisconnectAsync();
        }



    }



}