using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MorrisonServer.Models.NotiFactoryMD
{
    public class MD_App_userDevice
    {
        public object appSysId { get; set; }
        public object sysUserId { get; set; }
        public object RegId { get; set; }
        public object TokenId { get; set; }
        public object platform { get; set; }
        public object model { get; set; }
        public object UUID { get; set; }
        public object ticket { get; set; }
    }

    public class Push
    {
        #region PushAndroidV2 正常推播功能 2018-01-21
        public string pushAndroid(string title, string msg, string soundId, string param, string count, string deviceIds)
        {
            string errStr = "";
            try
            {
                string serverApiKey = System.Configuration.ConfigurationManager.AppSettings["AndroidPushKey"];    //pxmartit


                string senderId = System.Configuration.ConfigurationManager.AppSettings["AndroidSenderId"];   //pxmartit
                var value = System.Web.HttpUtility.UrlEncode(msg, System.Text.UTF8Encoding.GetEncoding("UTF-8"));
                WebRequest tRequest = WebRequest.Create("https://android.googleapis.com/gcm/send");

                var pushM = "\"" + msg + "\"";

                tRequest.Method = "post";
                tRequest.ContentType = "application/json";
                tRequest.Headers.Add(string.Format("Authorization: key={0}", serverApiKey));
                tRequest.Headers.Add(string.Format("Sender: id={0}", senderId));

                string postData = "{\"collapse_key\":\"score_update\",\"time_to_live\":108,\"delay_while_idle\":true,\"data\": { \"title\": \"" + title + "\", \"message\" : " + pushM + ",\"time\": " + "\"" + System.DateTime.Now.ToString() + "\" ";
                postData += param;
                postData += ", \"count\": \"" + (count == "" ? "0" : count) + "\" ";
                postData += ", \"soundname\": \"" + (soundId == "" ? "default" : soundId) + "\" ";
                postData += ", \"content-available\": \"0\" ";
                postData += "},\"registration_ids\":[" + deviceIds.ToString() + "]}";

                Encoding encoding = new UTF8Encoding();
                Byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                tRequest.ContentLength = byteArray.Length;

                using (Stream dataStream = tRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);

                    using (WebResponse tResponse = tRequest.GetResponse())
                    {
                        using (Stream dataStreamResponse = tResponse.GetResponseStream())
                        {
                            using (StreamReader tReader = new StreamReader(dataStreamResponse))
                            {
                                String sResponseFromServer = tReader.ReadToEnd();
                                GCMResponse GCMResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<GCMResponse>(sResponseFromServer);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                errStr = ex.Message;
            }

            return errStr;
        }

        #region Android private method
        private class GCMResponse
        {
            public string multicast_id { get; set; }
            public int success { get; set; }
            public int failure { get; set; }
            public int canonical_ids { get; set; }
            public List<GCMReslut> results { get; set; }
        }

        private class GCMReslut
        {
            public string error { get; set; }
        }
        #endregion
        #endregion

        public string pushiOS(string title, string msg, string msgHtml, string soundId, string backcolor, string param, string deviceIds)
        {
            string errStr = "";

            return errStr;
        }
    }
}