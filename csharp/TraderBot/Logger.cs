using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi.V1;

namespace TraderBot
{
    public class Logger
    {
        Logger<TradingService> LogProvider { get; set; }

        string FileName { get; set; }

        public Logger(ILogger<TradingService> logger, string fileName)
        {
            LogProvider = (Logger<TradingService>?)logger;
            FileName = fileName;
            if (!File.Exists(fileName))
            {
               var a = File.Create(fileName);
                a.Close(); 
            }
        }
        public Logger(Logger<TradingService> logger)
        {
            LogProvider = logger;
        }

        public void LogInformation(string strToLog)
        {
            if(FileName != null)
            {
                LogProvider.LogInformation(strToLog);
                string text = File.ReadAllText(FileName);
                using(StreamWriter sw = new StreamWriter(FileName))
                {
                    sw.WriteLine(text + "\n" + strToLog);
                }
            }
        }
        public void LogInformation(string strToLog, PositionsSecurities positions)
        {
            if (FileName != null)
            {
                LogProvider.LogInformation(strToLog);
                string text = File.ReadAllText(FileName);
                using (StreamWriter sw = new StreamWriter(FileName))
                {
                    sw.WriteLine(text + "\n" + strToLog);
                }
            }
        }
        public void LogError(Exception exception,string str)
        {

        }
        public void LogError(string str)
        {

        }
    }
}
