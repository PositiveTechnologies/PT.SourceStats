using Fclp;
using Newtonsoft.Json;
using PT.PM.Common;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using static System.String;

namespace PT.SourceStats.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            var parser = new FluentCommandLineParser();

            string fileName = Empty;
            string projectId = Empty;
            string serverUri = Empty;
            string outDir = Empty;
            bool multithreading = false;
            bool sendStatistics = false;
            LogLevel logLevel = LogLevel.All;
            int startInd = 0;
            int length = 0;
            bool showVersion = false;
            var logger = new Logger();

            parser.Setup<string>('f', "file").Callback(f => fileName = f.NormDirSeparator());
            parser.Setup<bool>("mt").Callback(mt => multithreading = mt);
            parser.Setup<bool>("send-statistics").Callback(ss => sendStatistics = ss);
            parser.Setup<LogLevel>("log-level").Callback(ll => logLevel = ll);
            parser.Setup<int>("start").Callback(ind => startInd = ind);
            parser.Setup<int>("length").Callback(l => length = l);
            parser.Setup<string>("project-id").Callback(pid => projectId = pid);
            parser.Setup<string>("server-uri").Callback(s => serverUri = s);
            parser.Setup<string>("out-dir").Callback(dir => outDir = dir.NormDirSeparator());
            parser.Setup<bool>('v', "version").Callback(v => showVersion = v);
            parser.SetupHelp("?", "help").Callback(helpText => logger?.LogInfo(new ErrorMessage(helpText)));

            var argsWithUsualSlashes = args.Select(arg => arg.Replace('/', '\\')).ToArray(); // TODO: bug in FluentCommandLineParser.
            var parsingResult = parser.Parse(argsWithUsualSlashes);
            if (!parsingResult.HasErrors)
            {
                if (showVersion)
                {
                    Assembly coreAssembly = Assembly.GetAssembly(typeof(PM.Workflow));
                    Assembly wrapperAssembly = Assembly.GetExecutingAssembly();
                    string coreVersion = FileVersionInfo.GetVersionInfo(coreAssembly.Location).FileVersion;
                    string wrapperVersion = FileVersionInfo.GetVersionInfo(wrapperAssembly.Location).FileVersion;
                    Console.WriteLine($"PT.PM version: {coreVersion}");
                    Console.WriteLine($"PT.SourceStats version: {wrapperVersion}");
                }

                logger.LogLevel = logLevel;
                try
                {
                    var statisticsCollector = new DirectoryStatisticsCollector
                    {
                        Multithreading = multithreading,
                        Logger = logger
                    };
                    StatisticsMessage statisticsMessage = statisticsCollector.CollectStatistics(fileName, startInd, length);
                    statisticsMessage.Id = projectId;

                    try
                    {
                        var statSender = new StatSender();
                        var text = JsonConvert.SerializeObject(statisticsMessage);
                        try
                        {
                            if (!IsNullOrWhiteSpace(outDir))
                            {
                                File.WriteAllText(Path.Combine(outDir, "PT.SourceStats.json"), text);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogInfo(new ErrorMessage(ex.ToString()));
                        }

                        if (sendStatistics)
                        {
                            statSender.SendStat(text, serverUri).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogInfo(new ErrorMessage(ex.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogInfo(new ErrorMessage(ex.ToString()));
                }
            }
            else
            {
                parser.HelpOption.ShowHelp(parser.Options);
                logger.LogInfo(new ErrorMessage(parsingResult.ErrorText));
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press Enter to exit");
                Console.ReadLine();
            }
        }
    }
}
