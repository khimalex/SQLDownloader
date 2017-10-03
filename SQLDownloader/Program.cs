using System;
using System.Threading.Tasks;
using Diconnect.Common;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;

namespace SQLDownloader
{
	class Program
	{
		static void Main(string[] args)
		{

			var sw = new Stopwatch();
			sw.Start();
			CLOptions options = new CLOptions();
			CommandLine.Parser.Default.ParseArgumentsStrict(args, options);
			var logger = GetLogger(options);

			if (!File.Exists(options.ServerListFilePath))
			{
				logger.Log($"Файл '{options.ServerListFilePath}' не существует.");
				Environment.Exit(-1);
			}
			if (!Directory.Exists(options.WriteToFolderPath))
			{
				logger.Log($"Директория '{options.WriteToFolderPath}' не существует.");
				Directory.CreateDirectory(options.WriteToFolderPath);
				logger.Log($"Директорию '{options.WriteToFolderPath}' создали.");
			}

			var serverList = Serializer.DeserializeFromFile<Servers>(options.ServerListFilePath);

			logger.Log($"Начали загрузки для серверов: \n{String.Join(Environment.NewLine, serverList.Server.Select(s => s.ToString()))}");

			Parallel.ForEach(serverList.Server, s =>
			{
				if (!s.IsValid())
				{
					var current = Console.ForegroundColor;
					Console.ForegroundColor = ConsoleColor.Red;
					logger.Log($"Неправильные настройки для cервера: '{s}' или не задано имя сервера: '{s.ServerName}'");
					Console.ForegroundColor = current;
					return;
				}

				var serverDataDir = Path.Combine(options.WriteToFolderPath, s.ServerName, DateTime.Now.ToString("yyyyMMdd"));
				if (!Directory.Exists(serverDataDir))
				{
					logger.Log($"Директория '{serverDataDir}' не существует.");
					Directory.CreateDirectory(serverDataDir);
					logger.Log($"Директорию '{serverDataDir}' создали.");

				}
				var downloader = new Downloader(s, options.WriteToFolderPath, logger);
				downloader.DownloadData().GetAwaiter().GetResult();
			});
			logger.Log($"Закончили загрузки для серверов: \n{String.Join(Environment.NewLine, serverList.Server.Select(s => s.ToString()))}");
			sw.Stop();
			logger.Log($"Прошло времени: {sw.Elapsed}");
		}

		static ILog GetLogger(CLOptions options)
		{
			if (options.OutputToCmd && !String.IsNullOrEmpty(options.LogFile))
			{
				return new ToConsoleAndFile(options.LogFile);
			}
			else if (!String.IsNullOrEmpty(options.LogFile))
			{
				return new ToFile(options.LogFile);
			}
			else
			{
				return new ToConsole(options.OutputToCmd);
			}
		}
	}
}
