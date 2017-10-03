using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLDownloader
{
	public class Downloader
	{
		private readonly ILog Logger;
		public Downloader(ServerOption serverOption, String writeToFolderPath, ILog logger)
		{
			Logger = logger;
			ServerOption = serverOption;
			WriteToFolderPath = writeToFolderPath;

		}

		public ServerOption ServerOption { get; private set; }
		public String WriteToFolderPath { get; private set; }
		public async Task DownloadData()
		{
			//var downloadStoredProcedures = new DownloadDelegate(DownloadStoredProcedures);

			List<Task> downloadActions = new List<Task>();

			var db = GetDatabase();

			//Из-за того, что у нас одно подключение используется параллельно несколькими потоками из пула, иногда один поток забирает работу другого потока, при этом
			//Возникает ошибка одновременного использования одного канала подключения разными потоками.
			//Делаем для каждого потока своё персональное подключение, с которыми они будут работать.
			if (ServerOption.StoredProcedure)
			{
				var sps = db.EnumObjects(DatabaseObjectTypes.StoredProcedure);
				var groups = sps.Rows.Cast<DataRow>().Select((dr, index) => new { dr, index }).GroupBy(g => g.index / (sps.Rows.Count / Environment.ProcessorCount), el => el.dr["Urn"].ToString());

				Logger.Log($"{ServerOption.ServerName} TotalGroupsSP: " + String.Join(",", groups.Select(g => g.Count().ToString())));
				Logger.Log($"{ServerOption.ServerName} TotalSP: " + sps.Rows.Count);

				var spTask = Task.Run(() => Download(groups));
				downloadActions.Add(spTask);
			}
			if (ServerOption.View)
			{
				var sps = db.EnumObjects(DatabaseObjectTypes.View);
				var groups = sps.Rows.Cast<DataRow>().Select((dr, index) => new { dr, index }).GroupBy(g => g.index / (sps.Rows.Count / Environment.ProcessorCount), el => el.dr["Urn"].ToString());

				Logger.Log($"{ServerOption.ServerName} TotalGroupsV: " + String.Join(",", groups.Select(g => g.Count().ToString())));
				Logger.Log($"{ServerOption.ServerName} TotalV: " + sps.Rows.Count);

				var vTask = Task.Run(() => Download(groups));
				downloadActions.Add(vTask);
			}
			if (ServerOption.UserDefinedFunction)
			{
				var sps = db.EnumObjects(DatabaseObjectTypes.UserDefinedFunction);
				var groups = sps.Rows.Cast<DataRow>().Select((dr, index) => new { dr, index }).GroupBy(g => g.index / (sps.Rows.Count / Environment.ProcessorCount), el => el.dr["Urn"].ToString());

				Logger.Log($"{ServerOption.ServerName} TotalGroupsUDF: " + String.Join(",", groups.Select(g => g.Count().ToString())));
				Logger.Log($"{ServerOption.ServerName} TotalUDF: " + sps.Rows.Count);

				var udfTask = Task.Run(() => Download(groups));
				downloadActions.Add(udfTask);
			}

			await Task.WhenAll(downloadActions.ToArray());

		}

		private Database GetDatabase()
		{
			Server srv = new Server(new ServerConnection()
			{
				ConnectionString = ServerOption.ToString(),
			});
			return srv.Databases[ServerOption.DbName];
		}



		private void Download(IEnumerable<IGrouping<int, string>> smoObjectsGroup)
		{
			Parallel.ForEach(smoObjectsGroup, g =>
			{
				Server serv = new Server(new ServerConnection()
				{
					ConnectionString = ServerOption.ToString(),
				});

				Scripter scripter = new Scripter
				{
					Server = serv
				};
				scripter.Options.IncludeHeaders = true;

				scripter.Options.SchemaQualify = true;
				scripter.Options.AllowSystemObjects = false;
				try
				{
					foreach (var urn in g)
					{
						var urnn = new Urn(urn);
						var type = urnn.Type;
						var name = urnn.GetNameForType(type);
						if (!urnn.GetAttribute("Schema").ToUpper().Equals("dbo".ToUpper()))
						{
							continue;
						}
						var smoObject = serv.GetSmoObject(urnn);
						var scriptLines = scripter.Script(new SqlSmoObject[] { smoObject });

						Logger.Log($"Forming script: '{name}'");
						if (scriptLines.Count == 0)
						{
							Logger.Log($"Неизвестная ошибка формирования скрипта для: '{name}'");
							continue;
						}
						//throw new IndexOutOfRangeException($"{nameof(scriptLines)}.Count == {scriptLines.Count}. '{name}'");
						scriptLines.RemoveAt(0);
						var outputString = OutputSchemaObject(ServerOption.DbName, scriptLines);

						var extension = "sql";
						var fileName = $"{name}.{type}.{extension}";
						var fullFilePath = Path.Combine(WriteToFolderPath, ServerOption.ServerName, DateTime.Now.ToString("yyyyMMdd"), fileName);

						Logger.Log($"Write File: '{fullFilePath}'");
						WriteFile(fullFilePath, outputString);
					}
				}
				catch (Exception e)
				{
					Logger.Log(e);
				}
			});

		}

		private StringCollection ReplaceFirstCreate(StringCollection sc)
		{
			var preReturn = sc.Cast<String>().ToList();
			var stringWithCreate = preReturn.Last();// Where(s => s.Split('\n').Count() > 0).FirstOrDefault(s => s.StartsWith(create, true, CultureInfo.InvariantCulture));
			var createStart = stringWithCreate.IndexOf(Servers.CREATE, StringComparison.InvariantCultureIgnoreCase);
			var removedCreate = stringWithCreate.Remove(createStart, Servers.CREATE.Length);
			var addedAlter = removedCreate.Insert(createStart, Servers.ALTER.ToUpper() + " ");
			sc.Remove(stringWithCreate);
			sc.Add(addedAlter);
			return sc;
		}
		private string OutputSchemaObject(String dbName, StringCollection sc)
		{
			var lines = sc.Cast<String>().ToList();
			if (lines.Count == 0)
			{
				return String.Empty;
			}

			var resultString = new StringBuilder();
			resultString.AppendLine($"USE [{dbName}]");
			resultString.AppendLine($"GO");
			for (int i = 0; i < lines.Count; i++)
			{
				resultString.AppendLine(lines[i]);
				resultString.AppendLine($"GO");
			}
			return resultString.ToString();
		}


		private void WriteFile(String fullFilePath, string sqlContent)
		{
			//Считаем, Что директории существуют - подготовили их перед стартом загрузок.
			File.Delete(fullFilePath);
			File.WriteAllText(fullFilePath, sqlContent);
		}

	}
}
