using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

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
		public void DownloadData()
		{
			var db = GetDatabase();
			if (ServerOption.StoredProcedure)
			{
				DownloadStoredProcedures(db.Name, db.StoredProcedures);
				//DownloadData<StoredProcedure>(db.Name, db.StoredProcedures);
			}
			if (ServerOption.View)
			{
				DownloadViews(db.Name, db.Views);

				//DownloadData<View>(db.Name, db.Views);
			}
			if (ServerOption.UserDefinedFunction)
			{
				DownloadUserDefinedFunctions(db.Name, db.UserDefinedFunctions);

				//DownloadData<UserDefinedFunction>(db.Name, db.UserDefinedFunctions);
			}

		}

		private Database GetDatabase()
		{
			Server srv = new Server(new ServerConnection()
			{
				ConnectionString = ServerOption.ToString()
			});
			return srv.Databases[ServerOption.DbName];
		}

		//Пришлось писать отдельные методы, т.к. для всех элементов БД IsSystemObject конкретно типизирован и не относится к интерфейсам.

		private void DownloadStoredProcedures(String dbName, StoredProcedureCollection storedProcedures)
		{
			var scriptOptions = new ScriptingOptions()
			{
				IncludeDatabaseContext = true,
			};
			foreach (StoredProcedure sp in storedProcedures)
			{
				if (!sp.IsSystemObject)
				{
					StringCollection scriptLines = null;
					try
					{
						scriptLines = sp.Script(scriptOptions);
						scriptLines.RemoveAt(0);
						if (ServerOption.ReplaceFirstCreate)
							scriptLines = ReplaceFirstCreate(scriptLines);

					}
					catch (Exception e)
					{
						var current = Console.ForegroundColor;
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine($"{sp.Name}, {sp.Urn?.Type} : ошибка выгрузки скрипта.");
						Console.ForegroundColor = current;
					}
					if (scriptLines != null)
					{
						var outputString = OutputSchemaObject(dbName, scriptLines);
						WriteFile(sp, outputString);
					}

				}
			}
		}

		private void DownloadUserDefinedFunctions(String dbName, UserDefinedFunctionCollection userDefinedFunctions)
		{
			var scriptOptions = new ScriptingOptions()
			{
				IncludeDatabaseContext = true,
			};
			foreach (UserDefinedFunction udf in userDefinedFunctions)
			{
				if (!udf.IsSystemObject)
				{
					StringCollection scriptLines = null;
					try
					{
						scriptLines = udf.Script(scriptOptions);
						scriptLines.RemoveAt(0);
						if (ServerOption.ReplaceFirstCreate)
							scriptLines = ReplaceFirstCreate(scriptLines);

					}
					catch (Exception e)
					{
						var current = Console.ForegroundColor;
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine($"{udf.Name}, {udf.Urn?.Type} : ошибка выгрузки скрипта.");
						Console.ForegroundColor = current;
					}
					if (scriptLines != null)
					{
						var outputString = OutputSchemaObject(dbName, scriptLines);
						WriteFile(udf, outputString);
					}

				}
			}
		}

		private void DownloadViews(String dbName, ViewCollection viewCollection)
		{
			var scriptOptions = new ScriptingOptions()
			{
				IncludeDatabaseContext = true,
			};
			foreach (View v in viewCollection)
			{
				if (!v.IsSystemObject)
				{
					StringCollection scriptLines = null;
					try
					{
						scriptLines = v.Script(scriptOptions);
						scriptLines.RemoveAt(0);
						if (ServerOption.ReplaceFirstCreate)
							scriptLines = ReplaceFirstCreate(scriptLines);

					}
					catch (Exception e)
					{
						var current = Console.ForegroundColor;
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine($"{v.Name}, {v.Urn?.Type} : ошибка выгрузки скрипта.");
						Console.ForegroundColor = current;
					}
					if (scriptLines != null)
					{
						var outputString = OutputSchemaObject(dbName, scriptLines);
						WriteFile(v, outputString);
					}

				}
			}
		}


		//private void DownloadData<T>(String dbName, SchemaCollectionBase schemaObjects) where T : ScriptSchemaObjectBase, IScriptable
		//{
		//	var scriptOptions = new ScriptingOptions()
		//	{
		//		IncludeDatabaseContext = true,

		//	};
		//	foreach (var item in schemaObjects)
		//	{
		//		StringCollection scriptLines = null;
		//		try
		//		{
		//			scriptLines = (item as IScriptable).Script(scriptOptions);
		//			scriptLines.RemoveAt(0);
		//			if (ServerOption.ReplaceFirstCreate)
		//				scriptLines = ReplaceFirstCreate(scriptLines);
		//		}
		//		catch (Exception e)
		//		{
		//			var obj = item as NamedSmoObject;
		//			var name = obj.Name;
		//			var type = obj.Urn.Type;
		//			var current = Console.ForegroundColor;
		//			Console.ForegroundColor = ConsoleColor.Red;
		//			Console.WriteLine($"{name}, {type} : ошибка выгрузки скрипта.");
		//			Console.ForegroundColor = current;

		//		}
		//		if (scriptLines != null)
		//		{
		//			var outputString = OutputSchemaObject(dbName, scriptLines);
		//			WriteFile(item as NamedSmoObject, outputString);
		//		}

		//	}
		//}
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
