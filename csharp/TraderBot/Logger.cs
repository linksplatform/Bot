using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi.V1;
using System.IO;
using Platform.IO;

namespace TraderBot
{
    public class Logger
    {
        Logger<TradingService> LogProvider { get; set; }

        public readonly string FileName;

        public Logger(ILogger<TradingService> logger, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentOutOfRangeException(nameof(fileName), "File name is empty.");
            }
            LogProvider = (Logger<TradingService>?)logger;
            FileName = fileName;
        }

        public void LogInformation(string @string)
        {
            LogProvider.LogInformation(@string);
            using var fileStream = Platform.IO.FileHelpers.Append(FileName);
            using StreamWriter writer = new(fileStream);
            writer.WriteLine(@string);
        }
        public void LogInformation(string @string, PositionsSecurities positions)
        {
            LogProvider.LogInformation(@string, positions);
            using var fileStream = Platform.IO.FileHelpers.Append(FileName);
            using StreamWriter writer = new(fileStream);
            writer.WriteLine(@string);
        }
        public void LogError(Exception @exception,string @string)
        {
            LogProvider.LogError(@string, @exception);
            using var fileStream = Platform.IO.FileHelpers.Append(FileName);
            using StreamWriter writer = new(fileStream);
            writer.WriteLine(@string);
        }
        public void LogError(string @string)
        {
            LogProvider.LogError(@string);
            using var fileStream = Platform.IO.FileHelpers.Append(FileName);
            using StreamWriter writer = new(fileStream);
            writer.WriteLine(@string);
        }
    }
}
