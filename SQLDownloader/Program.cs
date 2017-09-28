using System;
using System.Threading.Tasks;
using Diconnect.Common;
using System.IO;
using System.Linq;
using System.Diagnostics;

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

			if (!File.Exists(options.ServerListFilePath))
			{
				Console.WriteLine($"Файл '{options.ServerListFilePath}' не существует.");
				Environment.Exit(-1);
			}
			if (!Directory.Exists(options.WriteToFolderPath))
			{
				Console.WriteLine($"Директория '{options.WriteToFolderPath}' не существует.");
				Directory.CreateDirectory(options.WriteToFolderPath);
				Console.WriteLine($"Директорию '{options.WriteToFolderPath}' создали.");
			}

			var serverList = Serializer.DeserializeFromFile<Servers>(options.ServerListFilePath);

			Console.WriteLine($"Начали загрузки для серверов: \n{String.Join(Environment.NewLine, serverList.Server.Select(s => s.ToString()))}");
			Parallel.ForEach(serverList.Server, s =>
			{
				if (!s.IsValid())
				{
					var current = Console.ForegroundColor;
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"Неправильные настройки для cервера: '{s}' или не задано имя сервера: '{s.ServerName}'");
					Console.ForegroundColor = current;
					return;
				}

				var serverDataDir = Path.Combine(options.WriteToFolderPath, s.ServerName, DateTime.Now.ToString("yyyyMMdd"));
				if (!Directory.Exists(serverDataDir))
				{
					Console.WriteLine($"Директория '{serverDataDir}' не существует.");
					Directory.CreateDirectory(serverDataDir);
					Console.WriteLine($"Директорию '{serverDataDir}' создали.");

				}
				var downloader = new Downloader(s, options.WriteToFolderPath);
				downloader.DownloadData();
			});
			Console.WriteLine($"Закончили загрузки для серверов: \n{String.Join(Environment.NewLine, serverList.Server.Select(s => s.ToString()))}");
			sw.Stop();
			Console.WriteLine($"Прошло времени: {sw.Elapsed}");

		}
	}
}
