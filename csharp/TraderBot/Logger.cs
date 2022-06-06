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
            if (fileName != "")
            {
                FileName = fileName;
                if (!File.Exists(fileName))
                {
                    var a = File.Create(fileName);
                    a.Close();
                }
            }
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
            else
            {
                LogProvider.LogInformation(strToLog);
            }
        }
        public void LogInformation(string strToLog, PositionsSecurities positions)
        {
            if (FileName != null)
            {
                LogProvider.LogInformation(strToLog,positions);
                string text = File.ReadAllText(FileName);
                using (StreamWriter sw = new StreamWriter(FileName))
                {
                    sw.WriteLine(text + "\n" + strToLog);
                }
            }
            else
            {
                LogProvider.LogInformation(strToLog,positions);
            }
        }
        public void LogError(Exception exception,string strToLog)
        {
            if (FileName != null)
            {
                LogProvider.LogError(exception,strToLog);
                string text = File.ReadAllText(FileName);
                using (StreamWriter sw = new StreamWriter(FileName))
                {
                    sw.WriteLine(text + "\n" + exception.Message +  strToLog);
                }
            }
            else
            {
                LogProvider.LogInformation(strToLog);
            }
        }
        public void LogError(string strToLog)
        {
            if (FileName != null)
            {
                LogProvider.LogError(strToLog);
                string text = File.ReadAllText(FileName);
                using (StreamWriter sw = new StreamWriter(FileName))
                {
                    sw.WriteLine(text + "\n" + strToLog);
                }
            }
            else
            {
                LogProvider.LogError(strToLog);
            }
        }
    }
}
