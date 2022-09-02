using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

#nullable enable

namespace PrivacyIDEAClient
{
    public class PIToken
    {
        public Dictionary<string, object> data = new();
        public Dictionary<string, object> info = new();
        public static PIToken? FromJSON(string json, Action<Exception> log)
        {
            try
            {
                PIToken token = new();
                var tmp = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (tmp is not null)
                {
                    token.data = tmp!;
                    if (token.data.TryGetValue("info", out object? info) && info is JObject jobj)
                    {
                        tmp = jobj.ToObject<Dictionary<string, object>>();
                        if (tmp is not null)
                        {
                            token.info = tmp;
                        }
                    }
                    return token;
                }
            }
            catch (Exception e)
            {
                log.Invoke(e);
            }

            return null;
        }
    }
}
