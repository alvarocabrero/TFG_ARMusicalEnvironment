using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace OrkestraLib
{


    public class Orkestra
    {
        string room;
        string url;
        public string agentid;
        ApplicationContext apc;
        MappingService map;
        Dictionary<string, User> users = new Dictionary<string, User>();
        Dictionary<string, string> appData = new Dictionary<string, string>();
        Dictionary<string, JArray> enabledCaps = new Dictionary<string, JArray>();
        public event EventHandler<JObject> UserEvents;
        public event EventHandler<JObject> AppEvents;

        public Orkestra(string url, string room, string agentid)
        {
            this.url = url;
            this.room = room;

            if (agentid != null) this.agentid = (string)agentid;
            else
            {
                Debug.Log("not null!!!");
                this.agentid = Utils.RandomString(8);
                Debug.Log("not null!!!" + this.agentid);
            }

            map = new MappingService(url);
            map.init();
            Task.Delay(2500).ContinueWith(t2 =>
            {
                map.getGroupMapping(room, (e) =>
                {
                    Debug.Log("application custom url" + e);
                    JObject mapg = JObject.Parse(e);
                    apc = new ApplicationContext(mapg["group"].ToString(), this.agentid);
                    map.close();
                    Task.Delay(200).ContinueWith(t => { apc.on("agentchange", onAgentChange); });
                    Task.Delay(2000).ContinueWith(t =>
                    {
                        List<string> keys = apc.getKeys();
                        Debug.Log("testing" + keys + " " + keys.Count);
                        try
                        {
                            foreach (var s in keys)
                            {
                                Debug.Log("Testing" + s);
                                apc.on(s, onAppAttrChange);
                            }
                            this.subscribe("data");

                            foreach (string key in keys)
                            {
                                appData[key] = apc.getItem(key);

                                JObject appD = new JObject();
                                appD.Add("event", "appEvent");
                                appD.Add("key", key);
                                appD.Add("value", appData[key]);
                                AppEventsCall(appD);
                            }


                        }
                        catch (Exception ex)
                        {
                            Debug.Log("errorea" + ex);
                        }

                    });

                });

            });

        }

        /*
         * Restart the AppData if the users connected are only the admin or the actual user
         */


        protected virtual void UserEventsCall(JObject e)
        {
            UserEvents?.Invoke(this, e);
        }
        protected virtual void AppEventsCall(JObject e)
        {
            AppEvents?.Invoke(this, e);
        }
        void onAppAttrChange(string _chg)
        {
            Debug.Log(_chg);
            try
            {
                JObject chg = JObject.Parse(_chg);
                appData[chg["key"].ToString()] = chg["value"].ToString();
                chg.Add("event", "appEvent");
                AppEventsCall(chg);
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
        void onAgentChange(string _chg)
        {
            JObject chg = JObject.Parse(_chg);
            Debug.Log("agentChange" + _chg);
            if (!chg["agentContext"].ToString().Equals("null"))
            {
                if (!this.existsAgent(chg["agentid"].ToString()))
                {
                    try
                    {

                        this.users[chg["agentid"].ToString()] = new User(chg["agentid"].ToString(), chg["agentid"].ToString(), "", new Dictionary<string, string>());
                        User user = this.users[chg["agentid"].ToString()];
                        user.context = chg["agentContext"].ToString();

                        //  this.userObserver.next({evt:EVENT.AGENT_JOIN,type:"agentChange",data:chg});
                        JObject tnp = new JObject();
                        tnp["event"] = "join";
                        tnp["agentid"] = chg["agentid"].ToString();
                        Debug.Log("Joining");
                        this.UserEventsCall(tnp);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("orkestraApp" + ex);
                    }

                }
                JObject diff = (JObject)chg["diff"];
                JArray keys;
                if (chg["agentid"].ToString().Equals(this.agentid)) keys = this.apc.self_ag.keys();
                else keys = this.apc.getAgent(chg["agentid"].ToString()).publicAPI.keys();
                Debug.Log("hemen" + keys);
                if (keys.Count > 0)
                {

                    Debug.Log("enabling " + keys);
                    if (this.enabledCaps.ContainsKey(chg["agentid"].ToString()))
                    {
                        if (!(this.enabledCaps[chg["agentid"].ToString()].IndexOf("data") == -1))
                            this.enableInstrument(keys, chg["agentContext"].ToString());
                    }
                    else
                    {
                        this.enableInstrument(keys, chg["agentContext"].ToString());
                        this.enabledCaps[chg["agentid"].ToString()] = keys;
                    }


                }


            }
            else
            {
                JObject tnp = new JObject();
                tnp["event"] = "left";
                tnp["agentid"] = chg["agentid"].ToString();
                this.users.Remove(chg["agentid"].ToString());
                this.UserEventsCall(tnp);
                // this.userObserver.next({evt:EVENT.AGENT_LEFT,data:chg})
            }

        }
        bool existsAgent(string agid)
        {
            if (this.users.ContainsKey(agid))
            {
                return true;
            }
            else return false;
        }
        public void setAppAttribute(string key, string value)
        {
            if (apc != null)
                apc.setItem("data", value);
        }

        public string getAppAttribute()
        {
            string item = apc.getItem("data");
            return item;
        }
        public void subscribe(string key)
        {
            Debug.Log("subscribing key");
            apc.on(key, onAppAttrChange);
        }
        void enableInstrument(JArray caps, string agentid)
        {
            Debug.Log("enableIns" + agentid + caps);
            try
            {
                foreach (JToken cap in caps)
                {

                    if (agentid.Equals(this.agentid))
                    {

                        apc.self_ag.on(cap.ToString(), (data) =>
                        {
                            // User user = new User(  );
                            //System.Debug.Log("self agentContext");
                            JObject userData = JObject.Parse(data);
                            //System.Debug.Log("users "+this.users.Count+userData+this.users);
                            //  User user = this.users[agentid];
                            Debug.Log("USERS" + JsonConvert.SerializeObject(this.users));

                            if (this.users.ContainsKey(agentid))
                            {
                                User user = this.users[agentid];
                                user.capacity[userData["key"].ToString()] = userData["value"].ToString();
                            }
                            userData.Add("event", "agent_event");
                            //System.Debug.Log("giltza34:" + data );
                            this.UserEventsCall(userData);
                        });
                    }
                    else
                    {
                        PublicAPI context = this.apc.getAgent(agentid).publicAPI;
                        context.on(cap.ToString(), (data) =>
                        {
                            //   User user = new User(  );
                            JObject userData = JObject.Parse(data);
                            Debug.Log("USERS" + JsonConvert.SerializeObject(this.users));
                            if (this.users.ContainsKey(agentid))
                            {
                                User user = this.users[agentid];
                                user.capacity[userData["key"].ToString()] = userData["value"].ToString();
                            }
                            userData.Add("event", "agent_event");
                            //System.Debug.Log("giltza34:" + data );
                            this.UserEventsCall(userData);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
        public void setGlobalItem(string key, string value)
        {
            apc.setItem("text", "kaixooo");
        }
        public string getGlobalItem(string key, string value)
        {
            return apc.getItem("text");
        }
        public void setUserItem(string agentid, string key, string value)
        {

            if (agentid.Equals(this.agentid))
            {
                if (apc != null)
                    if (apc.self_ag != null)
                        apc.self_ag.setItem(key, value);
                //  apc.getAgent(agentid).publicAPI.setItem(key,value);  
            }
            else
                apc.getAgent(agentid).publicAPI.setItem(key, value);

        }
        public string getUserItem(string agentid, string key)
        {
            return apc.getAgent(agentid).publicAPI.getItem(key);
        }
        public Dictionary<string, User> getUsers()
        {
            return this.users;
        }
        public Dictionary<string, string> getGlobalData()
        {
            return this.appData;
        }
        public void close()
        {
            if (apc != null)
                apc.close();

        }
    }

}
