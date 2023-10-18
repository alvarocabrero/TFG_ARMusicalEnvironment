using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using SocketIOClient;

namespace OrkestraLib
{
    public class MappingService
    {
        string url = "";
        SocketIO _connection;
        bool connected = false;
        string _readystate = "connecting";

        Dictionary<string, List<Action<string>>> _callbacks = new Dictionary<string, List<Action<string>>>();
        Stack<Action<string>> waitingUserPromises = new Stack<Action<string>>();
        Stack<Action<string>> waitingGroupPromises = new Stack<Action<string>>();
        Dictionary<string, string> STATE = new Dictionary<string, string>();

        public MappingService(string url)
        {
            this.url = url;
            STATE.Add("CONNECTING", "connecting");
            STATE.Add("OPEN", "open");
            STATE.Add("CLOSED", "closed");
            _callbacks.Add("readystatechange", new List<Action<string>>());
            System.Console.WriteLine("mapping service" + this.url);
            try
            {
                this._connection = new SocketIO(this.url, new SocketIOOptions
                {
                    EIO = 3
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            this._connection.On("connected", (e) => {   
                Console.WriteLine("error" + e);
            });
            this._connection.On("error", (e) => {
                Console.WriteLine("error" + e);
            });
           
        }
        public async void init()
        {

            //  this._connection.On("connect", onConnect);
            await this._connection.ConnectAsync();
            this._connection.On("mapping", onMapping);
            this.readystate("open");
        }

        void onMapping(SocketIOResponse response)
        {
            System.Console.WriteLine("onmapping" + response.GetValue());
            try
            {
                System.Console.WriteLine("before parse");
                JObject _response = (JObject)JToken.FromObject(response.GetValue());
                System.Console.WriteLine("after parse");
                var host = this.url;

                // if (typeof url === 'object' || !url) {
                //     var host = window.location.protocol + '//' + window.location.host + '/';
                // }
                System.Console.WriteLine("error parsein");
                JObject _result = new JObject();
                if (_response["group"] == null)
                {

                    if (_response["user"] != null)
                    {
                        _result["user"] = host + _response["user"];
                    }
                    if (_response["app"] != null)
                    {
                        _result["app"] = host + _response["app"];
                    }
                    if (_response["userApp"] != null)
                    {
                        _result["userApp"] = host + _response["userApp"];
                    }


                    if (waitingUserPromises.Count > 0)
                    {
                        System.Console.WriteLine("waiting promise");
                        Action<string> promise = waitingUserPromises.Pop();
                        promise.Invoke(JsonConvert.SerializeObject(_result));
                    }
                }
                else
                {
                    //System.Console.WriteLine("GROUP");
                    var result = new
                    {
                        group = host + _response["group"]
                    };
                    if (waitingGroupPromises.Count > 0)
                    {
                        //System.Console.WriteLine("GROUP INVOK");
                        Action<string> promise = waitingGroupPromises.Pop();
                        promise.Invoke(JsonConvert.SerializeObject(result));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }
        public void getGroupMapping(string groupId, Action<string> cb)
        {
            System.Console.WriteLine("get group mapping {0}", groupId);
            if (!groupId.Equals(null))
            {
                string request = "{\"groupId\":\"" + groupId + "\"}";
                System.Console.WriteLine("Emiting groupID " + request);
                this._connection.EmitAsync("getMapping", JsonConvert.DeserializeObject(request));
            }
            else
            {
                System.Console.WriteLine("groupId undefined");
            }
            // Action<string> promise = new Action<string>();
            waitingGroupPromises.Push(cb);

            //  console.log(options.maxTimeout);
            // setTimeout(function () {
            //     reject({
            //         error: 'timeout-mappinservice2'
            //     })
            // }, options.maxTimeout);
            // });
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
        void _do_callbacks(string what, string e, object handler)
        {

            if (!_callbacks.ContainsKey(what)) System.Console.WriteLine("Unsupported event " + what);
            Action<string> h;
            for (int i = 0; i < _callbacks[what].Count; i++)
            {
                h = _callbacks[what][i];

                try
                {

                    //System.Console.WriteLine ("Invoking " + what);
                    h.Invoke(e);

                }
                catch (Exception ex)
                {
                    //System.Console.WriteLine("Error in " + what + ": " + h + ": " + ex);
                }
            }

        }
        public async void close()
        {
            await this._connection.DisconnectAsync();

        }

    }
}