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
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.Internet | AccessRights.FileSystem)]
    public class TFBot1 : Robot
    {
        public string connectionString { get; set; }

        protected override void OnStart()
        {
            connectionString = "http://127.0.0.1:5000/";

            var request = (HttpWebRequest)WebRequest.Create(connectionString + "model/conv_model.h5");
            request.Method = "POST";
            var stream = request.GetRequestStream();
            var model = File.ReadAllBytes("C:\\Users\\james\\OneDrive\\Documents\\cAlgo\\Sources\\Robots\\TF Bot 1\\conv_model.h5");
            stream.Write(model, 0, model.Length);
            stream.Close();

            HttpWebResponse response;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            } catch (WebException webException)
            {
                response = (HttpWebResponse)webException.Response;
            }

            response.Close();
        }

        protected override void OnBar()
        {
            SendData("C:\\Users\\james\\OneDrive\\Documents\\cAlgo\\Sources\\Robots\\TF Bot 1\\data.csv");
            GetPrediction();
        }

        private void SendData(string filePath)
        {
            var request = (HttpWebRequest)WebRequest.Create(connectionString + "data/data.csv");
            request.Method = "POST";
            var stream = request.GetRequestStream();
            var data = File.ReadAllBytes(filePath);
            stream.Write(data, 0, data.Length);
            stream.Close();

            HttpWebResponse response;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            } catch (WebException webException)
            {
                response = (HttpWebResponse)webException.Response;
            }

            response.Close();
        }

        private void GetPrediction()
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
