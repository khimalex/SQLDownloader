using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLDownloader
{
	public class Downloader
	{
		public Downloader(ServerOption serverOption, String writeToFolderPath)
		{
			ServerOption = serverOption;
			WriteToFolderPath = writeToFolderPath;

		}

		public ServerOption ServerOption { get; private set; }
		public String WriteToFolderPath { get; private set; }
		public async Task DownloadData()
		{
			//var downloadStoredProcedures = new DownloadDelegate(DownloadStoredProcedures);

			List<Task> downloadActions = new List<Task>();

			//Из-за того, что у нас одно подключение используется параллельно несколькими потоками из пула, иногда один поток забирает работу другого потока, при этом
			//Возникает ошибка одновременного использования одного канала подключения разными потоками.
			//Делаем для каждого потока своё персональное подключение, с которыми они будут работать.
			if (ServerOption.StoredProcedure)
			{
				var dbSP = GetDatabase();
				var spTask = Task.Run(() => DownloadData<StoredProcedure>(dbSP.Name, dbSP.StoredProcedures));
				downloadActions.Add(spTask);
			}
			if (ServerOption.View)
			{
				var dbV = GetDatabase();
				var vTask = Task.Run(() => DownloadData<View>(dbV.Name, dbV.Views));
				downloadActions.Add(vTask);
			}
			if (ServerOption.UserDefinedFunction)
			{
				var dbUDF = GetDatabase();
				var udfTask = Task.Run(() => DownloadData<UserDefinedFunction>(dbUDF.Name, dbUDF.UserDefinedFunctions));
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

		private void DownloadData<T>(String dbName, SchemaCollectionBase schemaObjects) where T : ScriptSchemaObjectBase, IScriptable
		{
			var scriptOptions = new ScriptingOptions()
			{
				AllowSystemObjects = false,
				IncludeDatabaseContext = true,
			};
			foreach (var item in schemaObjects)
			{
				StringCollection scriptLines = null;
				try
				{
					scriptLines = (item as IScriptable).Script(scriptOptions);
					scriptLines.RemoveAt(0);
					if (ServerOption.ReplaceFirstCreate)
						scriptLines = ReplaceFirstCreate(scriptLines);
				}
				catch (Exception e)
				{
					//var obj = item as NamedSmoObject;
					//var name = obj.Name;
					//var type = obj.Urn.Type;
					//var current = Console.ForegroundColor;
					//Console.ForegroundColor = ConsoleColor.Red;
					//Console.WriteLine($"{name}, {type} : ошибка выгрузки скрипта.");
					//Console.ForegroundColor = current;

				}
				if (scriptLines != null && scriptLines.Count != 0)
				{
					var outputString = OutputSchemaObject(dbName, scriptLines);
					WriteFile(item as NamedSmoObject, outputString);
				}

			}
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


		private void WriteFile(NamedSmoObject obj, string sqlContent)
		{
			//Считаем, Что директории существуют - подготовили их перед стартом загрузок.
			var name = obj.Name;
			var type = obj.Urn.Type;
			var extension = "sql";
			var fileName = $"{name}.{type}.{extension}";

			var fullFilePath = Path.Combine(WriteToFolderPath, ServerOption.ServerName, DateTime.Now.ToString("yyyyMMdd"), fileName);
			File.Delete(fullFilePath);
			File.WriteAllText(fullFilePath, sqlContent);
			//var fileName = 
		}

	}
}
