using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Diconnect.Common
{
	public class Serializer
	{
		public static byte[] SerializeToBytes<T>(T obj, Boolean omitXmlDeclaration = false)
		{
			if (obj == null)
				return default(byte[]);
			//logger.Info("Serialization of " + typeof(T).Name + " was started");
			XmlSerializer serializer = new XmlSerializer(typeof(T));
			using (var ms = new MemoryStream())
			{
				using (var xw = XmlWriter.Create(ms, new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true, WriteEndDocumentOnClose = true, OmitXmlDeclaration = omitXmlDeclaration }))
				{
					var namespaces = new XmlSerializerNamespaces();
					namespaces.Add(string.Empty, string.Empty);
					serializer.Serialize(xw, obj, namespaces);
					//logger.Info("Serialization of " + typeof(T).Name + " finished");
					return ms.ToArray();
				}
			}
		}
		public static byte[] SerializeObjectToBytes(object obj, Boolean omitXmlDeclaration = false)
		{
			if (obj == null)
				return default(byte[]);
			Type objType = obj.GetType();
			//logger.Info("Serialization of " + objType.Name + " was started");
			XmlSerializer serializer = new XmlSerializer(objType);
			using (var ms = new MemoryStream())
			{
				using (var xw = XmlWriter.Create(ms, new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true, WriteEndDocumentOnClose = true, OmitXmlDeclaration = omitXmlDeclaration }))
				{
					var namespaces = new XmlSerializerNamespaces();
					namespaces.Add(string.Empty, string.Empty);
					serializer.Serialize(xw, obj, namespaces);
					//logger.Info("Serialization of " + objType.Name + " finished");
					return ms.ToArray();
				}
			}

		}

		public static void SerializeToFile<T>(string filePath, T obj, Boolean omitXmlDeclaration = false)
		{
			if (obj == null || String.IsNullOrEmpty(filePath))
				return;
			//logger.Info("Serialization to " + filePath + " of " + typeof(T).Name + " was started");
			XmlSerializer serializer = new XmlSerializer(typeof(T));
			using (var ms = File.Create(filePath))
			{
				using (var xw = XmlWriter.Create(ms, new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true, WriteEndDocumentOnClose = true, OmitXmlDeclaration = omitXmlDeclaration }))
				{
					var namespaces = new XmlSerializerNamespaces();
					namespaces.Add(string.Empty, string.Empty);
					serializer.Serialize(xw, obj, namespaces);
					//logger.Info("Serialization to " + filePath + " finished");
				}
			}
		}
		public static void SerializeObjectToFile(string filePath, object obj, Boolean omitXmlDeclaration = false)
		{
			if (obj == null || String.IsNullOrEmpty(filePath))
				return;
			Type objType = obj.GetType();
			//logger.Info("Serialization to " + filePath + " of " + objType.Name + " was started");
			XmlSerializer serializer = new XmlSerializer(objType);
			using (var ms = File.Create(filePath))
			{
				using (var xw = XmlWriter.Create(ms, new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true, WriteEndDocumentOnClose = true, OmitXmlDeclaration = omitXmlDeclaration }))
				{
					var namespaces = new XmlSerializerNamespaces();
					namespaces.Add(string.Empty, string.Empty);
					serializer.Serialize(xw, obj, namespaces);
					//logger.Info("Serialization to " + filePath + " finished");
				}
			}
		}

		public static string SerializeToString<T>(T obj, Boolean omitXmlDeclaration = false)
		{

			return Encoding.UTF8.GetString(SerializeToBytes(obj, omitXmlDeclaration));
		}
		public static string SerializeObjectToString(object obj, Boolean omitXmlDeclaration = false)
		{
			return Encoding.UTF8.GetString(SerializeObjectToBytes(obj, omitXmlDeclaration));
		}

		public static T DeserializeFromFile<T>(String filePath) where T : class
		{
			if (String.IsNullOrEmpty(filePath))
				return default(T);
			//logger.Info("Deserializing " + filePath);
			XmlSerializer serializer = new XmlSerializer(typeof(T));
			using (var fs = File.OpenRead(filePath))
			{
				using (XmlReader xr = XmlReader.Create(fs))
				{
					T result = default(T);
					result = serializer.Deserialize(xr) as T;
					return result;
				}
			}
		}

		public static T DeserializeBytes<T>(byte[] fileBytes) where T : class
		{
			if (fileBytes.Length == 0)
				return default(T);
			//logger.Info("Deserialization of byte[] to " + typeof(T).Name);
			XmlSerializer serializer = new XmlSerializer(typeof(T));
			using (var ms = new MemoryStream(fileBytes))
			{
				using (XmlReader xr = XmlReader.Create(ms))
				{
					var result = serializer.Deserialize(xr) as T;
					//logger.Info("Deserialization finished");
					return result;
				}
			}
		}

		public static T DeserialzeString<T>(string rawString) where T : class
		{
			if (String.IsNullOrEmpty(rawString))
				return default(T);
			//logger.Info($"Deserialization string to {nameof(T)}");
			XmlSerializer serializer = new XmlSerializer(typeof(T));
			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(rawString)))
			{
				using (XmlReader xr = XmlReader.Create(ms))
				{
					var result = serializer.Deserialize(xr) as T;
					//logger.Info("Deserialization finished");
					return result;
				}
			}

		}
	}
}
