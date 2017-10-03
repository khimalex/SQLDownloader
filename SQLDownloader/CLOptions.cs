using CommandLine;
using System;

namespace SQLDownloader
{
	public class CLOptions
	{
		[Option('s', "settings", HelpText ="Путь к файлу со списком серверов и их настройками загрузки, авторизации и п.р.", Required = true)]
		public String ServerListFilePath { get; set; }
		[Option('w', "writeto", HelpText = "Путь к директории, куда будут загружаться все файлы", Required = true)]
		public String WriteToFolderPath { get; set; }
		[Option('v', "verbose", HelpText ="Output details in console.")]
		public Boolean OutputToCmd { get; set; }
		[Option('l', "logging", HelpText = "Also output details to log file.")]
		public String LogFile { get; set; }

	}
}
