using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SQLDownloader
{
	public interface ILog
	{
		void Log(string log);
		void Log(Exception e);

	}
	public class ToConsole : ILog
	{
		public ToConsole(Boolean verbose)
		{
			Verbose = verbose;
		}
		public Boolean Verbose { get; private set; }

		public void Log(String log)
		{
			if (Verbose)
			{
				Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffffff")}] : {log}");
			}
		}

		public void Log(Exception e)
		{
			if (Verbose)
			{
				var exceptions = FromException(e);
				Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffffff")}] : '{String.Join("',", exceptions.Select(ee => ee.Message))}'");
			}
		}


		private IEnumerable<Exception> FromException(Exception e)
		{
			if (e is null)
				yield return new Exception("");
			Exception inner = e;
			while (inner != null)
			{
				yield return inner;
				inner = inner.InnerException;
			}
		}

	}
	public class ToFile : ILog
	{
		private static object locker = new object();
		public ToFile(String logFilePath)
		{

			LogDirectoryName = Path.GetDirectoryName(logFilePath);
			LogFileName = Path.GetFileName(logFilePath);

			LogFilePath = Path.Combine(LogDirectoryName, LogFileName);
		}
		public String LogDirectoryName { get; private set; }
		public String LogFileName { get; private set; }
		public String LogFilePath { get; private set; }
		public void Log(String log)
		{
			CheckDirectory();
			lock (locker)
			{
				File.AppendAllLines(LogFilePath, new String[] { $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffffff")}] : {log}" });
			}
		}

		public void Log(Exception e)
		{
			CheckDirectory();
			var exceptions = FromException(e);
			lock (locker)
			{
				File.AppendAllLines(LogFilePath, new String[] { $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffffff")}] : '{String.Join("',", exceptions.Select(ee => ee.Message))}'" });
			}
		}
		private IEnumerable<Exception> FromException(Exception e)
		{
			if (e is null)
				yield return new Exception("");
			Exception inner = e;
			while (inner != null)
			{
				yield return inner;
				inner = inner.InnerException;
			}
		}

		private void CheckDirectory()
		{
			lock (locker)
			{
				if (!Directory.Exists(LogDirectoryName))
				{
					Directory.CreateDirectory(LogDirectoryName);
				}
			}
		}

	}

	public class ToConsoleAndFile : ILog
	{
		public ToConsoleAndFile(String logFilePath)
		{
			ToConsole = new ToConsole(true);
			ToFile = new ToFile(logFilePath);
		}
		public ILog ToConsole { get; private set; }
		public ILog ToFile { get; private set; }
		public void Log(String log)
		{
			ToConsole.Log(log);
			ToFile.Log(log);
		}

		public void Log(Exception e)
		{
			ToConsole.Log(e);
			ToFile.Log(e);
		}
	}
}
