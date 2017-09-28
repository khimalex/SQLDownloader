using System;
using System.Linq;
using System.Xml.Serialization;

namespace SQLDownloader
{
	public class Servers
	{
		public const String CREATE = "CREATE";
		public const String ALTER = "ALTER";
		[XmlElement]
		public ServerOption[] Server { get; set; }
	}

	public class ServerOption
	{

		[XmlAttribute]
		[RequeredProperty]
		public String Address { get; set; }
		[XmlAttribute]
		[RequeredProperty]
		public String ServerName { get; set; }
		[XmlAttribute]
		[RequeredProperty]
		public String DbName { get; set; }
		[XmlAttribute]
		[RequeredProperty]
		public String Login { get; set; }
		[XmlAttribute]
		[RequeredProperty]
		public String Password { get; set; }
		[XmlAttribute]
		public Boolean StoredProcedure { get; set; }
		[XmlAttribute]
		public Boolean UserDefinedFunction { get; set; }
		[XmlAttribute]
		public Boolean View { get; set; }
		[XmlAttribute]
		public Boolean ReplaceFirstCreate { get; set; }

		public Boolean IsValid()
		{
			var requiredProperties = typeof(ServerOption).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Where(pi => pi.IsDefined(typeof(RequeredPropertyAttribute), true));
			return requiredProperties.All(pi => (!String.IsNullOrEmpty(pi.GetValue(this)?.ToString())));
		}
		public override String ToString()
		{
			return $"Data Source={Address};Initial Catalog={DbName};Persist Security Info=True;User ID={Login};Password={Password}";
		}
	}
	public class RequeredPropertyAttribute : Attribute { }
}
