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
        public string ConnectionString { get; set; }

        protected override void OnStart()
        {
            ConnectionString = "http://127.0.0.1:5000/";

            var request = (HttpWebRequest)WebRequest.Create(ConnectionString + "model/conv_model.h5");
            request.Method = "POST";
            var stream = request.GetRequestStream();
            var model = File.ReadAllBytes("C:\\Users\\james\\OneDrive\\Documents\\cAlgo\\Sources\\Robots\\TF Bot 1\\conv_model_5.h5");
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
            if (double.IsNaN(Indicators.SimpleMovingAverage(Bars.ClosePrices, 24 * 7).Result.Last(6)))
                return;
            int numBars = 5;
            var stream = new MemoryStream();
            using (var sw = new StreamWriter(stream))
            {
                sw.WriteLine("OpenTime,ClosePrice");
                for (int i = numBars; i > 0; i--)
                {
                    double closeNorm = Bars.Last(i).Close - Indicators.SimpleMovingAverage(Bars.ClosePrices, 24 * 7).Result.Last(i + 1);
                    sw.WriteLine(Bars.Last(i).OpenTime + "," + closeNorm);
                }
                sw.Flush();
                SendData(stream);
            }

            double prediciton = GetPrediction() + Indicators.SimpleMovingAverage(Bars.ClosePrices, 24 * 7).Result.Last(1);

            double risk = 0.02;
            double minStopLossPips = 5;

            if (prediciton > Ask)
            {
                double takeProfitPips = (prediciton - Ask) / Symbol.PipSize;
                double stopLossPips = 1.0 * takeProfitPips;
                if (stopLossPips > minStopLossPips)
                {
                    double maxLossBaseCurrency = (Math.Min(Account.Equity, Account.Balance) * risk) * (Symbol.PipSize / Symbol.PipValue);
                    double volume = Symbol.NormalizeVolumeInUnits(maxLossBaseCurrency / (stopLossPips * Symbol.PipSize));
                    ExecuteMarketOrder(TradeType.Buy, Symbol.Name, volume, "", takeProfitPips, stopLossPips);
                }
            }
            else if (prediciton < Bid)
            {
                double takeProfitPips = (Bid - prediciton) / Symbol.PipSize;
                double stopLossPips = 1.0 * takeProfitPips;
                if (stopLossPips > minStopLossPips)
                {
                    double maxLossBaseCurrency = (Math.Min(Account.Equity, Account.Balance) * risk) * (Symbol.PipSize / Symbol.PipValue);
                    double volume = Symbol.NormalizeVolumeInUnits(maxLossBaseCurrency / (stopLossPips * Symbol.PipSize));
                    ExecuteMarketOrder(TradeType.Sell, Symbol.Name, volume, "", takeProfitPips, stopLossPips);
                }
            }
        }

        private void SendData(Stream dataStream)
        {
            var request = (HttpWebRequest)WebRequest.Create(ConnectionString + "data/data.csv");
            request.Method = "POST";
            var stream = request.GetRequestStream();
            dataStream.Position = 0;
            dataStream.CopyTo(stream);
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

        private double GetPrediction()
        {
            var request = (HttpWebRequest)WebRequest.Create(ConnectionString + "model");
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

            double prediction = 0;

            using (var sr = new StreamReader(stream))
            {
                string content = sr.ReadToEnd();
                var deserializedContent = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                if (response.StatusCode == HttpStatusCode.OK)
                    prediction = double.Parse(deserializedContent["prediction"]);
                else
                    Print(deserializedContent["error"]);
            }

            response.Close();

            return prediction;
        }
    }
}
