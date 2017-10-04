using System;
using System.Collections.Specialized;
using System.Linq;

namespace SQLDownloader
{

	public class ReplacerFactories
	{
		public IReplacer GetReplacer(String smoObjectType)
		{
			switch (smoObjectType)
			{
				case "View":
					return new ReplaceV();
				case "StoredProcedure":
					return new ReplaceSP();
				case "UserDefinedFunction":
					return new ReplaceUDF();
				default:
					return null;
			}
		}
	}

	public interface IReplacer
	{
		String ReplaceText { get; }
		String ReplacedText { get; }
		StringCollection Replace(StringCollection src);
	}
	public class ReplaceSP : IReplacer
	{
		public String ReplaceText => "CREATE PROCEDURE";
		public String ReplacedText => "ALTER PROCEDURE";

		public StringCollection Replace(StringCollection src)
		{
			var preReturn = src.Cast<String>().ToList();
			var spBody = preReturn.Last();// Where(s => s.Split('\n').Count() > 0).FirstOrDefault(s => s.StartsWith(create, true, CultureInfo.InvariantCulture));

			var createStart = spBody.IndexOf(ReplaceText, StringComparison.InvariantCultureIgnoreCase);
			var removedCreate = spBody.Remove(createStart, ReplaceText.Length);
			var addedAlter = removedCreate.Insert(createStart, ReplacedText);
			src.Remove(spBody);
			src.Add(addedAlter);
			return src;
		}
	}
	public class ReplaceUDF : IReplacer
	{
		public String ReplaceText => "CREATE FUNCTION";
		public String ReplacedText => "ALTER FUNCTION";


		public StringCollection Replace(StringCollection src)
		{
			var preReturn = src.Cast<String>().ToList();
			var spBody = preReturn.Last();// Where(s => s.Split('\n').Count() > 0).FirstOrDefault(s => s.StartsWith(create, true, CultureInfo.InvariantCulture));

			var createStart = spBody.IndexOf(ReplaceText, StringComparison.InvariantCultureIgnoreCase);
			var removedCreate = spBody.Remove(createStart, ReplaceText.Length);
			var addedAlter = removedCreate.Insert(createStart, ReplacedText);
			src.Remove(spBody);
			src.Add(addedAlter);
			return src;
		}
	}

	public class ReplaceV : IReplacer
	{
		public String ReplaceText => "CREATE VIEW";
		public String ReplacedText => "ALTER VIEW";


		public StringCollection Replace(StringCollection src)
		{
			var preReturn = src.Cast<String>().ToList();
			var spBody = preReturn.Last();// Where(s => s.Split('\n').Count() > 0).FirstOrDefault(s => s.StartsWith(create, true, CultureInfo.InvariantCulture));

			var createStart = spBody.IndexOf(ReplaceText, StringComparison.InvariantCultureIgnoreCase);
			var removedCreate = spBody.Remove(createStart, ReplaceText.Length);
			var addedAlter = removedCreate.Insert(createStart, ReplacedText);
			src.Remove(spBody);
			src.Add(addedAlter);
			return src;
		}
	}
}
