using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PrivacyIDEAClient
{
    public class PIResponse
    {
        public string TransactionID { get; set; } = "";
        public string Message { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public string Type { get; set; } = "";
        public string Serial { get; set; } = "";
        public int ErrorCode { get; set; } = 0;
        public bool Status { get; set; } = false;
        public bool Value { get; set; } = false;
        public string Certificate { get; set; } = "";
        public string RolloutState { get; set; } = "";
        public string Raw { get; set; } = "";
        public List<PIChallenge> Challenges { get; set; } = new List<PIChallenge>();
        public PIResponse() { }

        public List<string> TriggeredTokenTypes()
        {
            return Challenges.Select(challenge => challenge.Type).Distinct().ToList();
        }

        public string PushMessage()
        {
            return Challenges.First(challenge => challenge.Type == "push").Message;
        }

        public string WebAuthnSignRequest()
        {
            // Currently get only the first one that was triggered
            string ret = "";
            foreach (PIChallenge challenge in Challenges)
            {
                if (challenge.Type == "webauthn")
                {
                    ret = (challenge as PIWebAuthnSignRequest).WebAuthnSignRequest;
                    break;
                }
            }

            return ret;
        }

        public static PIResponse FromJSON(string json, PrivacyIDEA privacyIDEA)
        {
            if (string.IsNullOrEmpty(json))
            {
                if (privacyIDEA != null)
                {
                    privacyIDEA.Error("Json to parse is empty!");
                }
                return null;
            }

            PIResponse ret = new()
            {
                Raw = json
            };
            try
            {
                JObject jobj = JObject.Parse(json);
                JToken result = jobj["result"];
                if (result != null)
                {
                    ret.Status = (bool)result["status"];

                    JToken jVal = result["value"];
                    if (jVal != null)
                    {
                        ret.Value = (bool)jVal;
                    }

                    JToken error = result["error"];
                    if (error != null)
                    {
                        ret.ErrorCode = (int)error["code"];
                        ret.ErrorMessage = (string)error["message"];
                    }
                }

                JToken detail = jobj["detail"];
                if (detail != null && detail.Type != JTokenType.Null)
                {
                    ret.TransactionID = (string)detail["transaction_id"];
                    ret.Message = (string)detail["message"];
                    ret.Type = (string)detail["type"];
                    ret.Serial = (string)detail["serial"];
                    ret.RolloutState = (string)detail["rollout_state"];
                    ret.Certificate = (string)detail["certificate"];

                    if (detail["multi_challenge"] is JArray multiChallenge)
                    {
                        foreach (JToken element in multiChallenge.Children())
                        {
                            string message = (string)element["message"];
                            string transactionid = (string)element["transaction_id"];
                            string type = (string)element["type"];
                            string serial = (string)element["serial"];
                            if (type == "webauthn")
                            {
                                PIWebAuthnSignRequest tmp = new()
                                {
                                    Message = message,
                                    Serial = serial,
                                    TransactionID = transactionid,
                                    Type = type
                                };
                                JToken attr = element["attributes"];
                                tmp.WebAuthnSignRequest = attr["webAuthnSignRequest"].ToString(Formatting.None);
                                tmp.WebAuthnSignRequest.Replace("\n", "");
                                
                                ret.Challenges.Add(tmp);
                            }
                            else
                            {
                                PIChallenge tmp = new()
                                {
                                    Message = message,
                                    Serial = serial,
                                    TransactionID = transactionid,
                                    Type = type
                                };
                                ret.Challenges.Add(tmp);
                            }
                        }
                    }
                }
            }
            catch (JsonException je)
            {
                if (privacyIDEA != null)
                {
                    privacyIDEA.Error(je);
                }
                return null;
            }

            return ret;
        }

    }
}
