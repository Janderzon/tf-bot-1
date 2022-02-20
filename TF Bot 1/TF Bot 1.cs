using System;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Net;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using System.Collections.Generic;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.Internet)]
    public class TFBot1 : Robot
    {
        public string connectionString { get; set; }

        protected override void OnStart()
        {
            connectionString = "http://127.0.0.1:5000/";
        }

        protected override void OnBar()
        {
            var request = (HttpWebRequest)WebRequest.Create(connectionString + "model");
            request.Method = "GET";

            HttpWebResponse response;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            } catch (WebException webException)
            {
                response = (HttpWebResponse)webException.Response;
            }

            var stream = response.GetResponseStream();

            using (var sr = new StreamReader(stream))
            {
                string content = sr.ReadToEnd();
                var deserializedContent = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                if (response.StatusCode == HttpStatusCode.OK)
                    Print(deserializedContent["prediction"]);
                else
                    Print(deserializedContent["error"]);
            }

            response.Close();
        }
    }
}
