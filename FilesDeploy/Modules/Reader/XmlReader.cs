using System;
using System.Reflection;
using System.Xml;
using EC2_Manager.Application;

namespace EC2_Manager.Modules.Reader
{
	public static class XmlReader
	{
		public static void LoadLocalSettings(string xmlPath)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(xmlPath);

			XmlNode node = doc.SelectSingleNode("Configurations");

			Type attribType = typeof(LocalSettings);
			FieldInfo[] fields = attribType.GetFields(BindingFlags.Public | BindingFlags.Static);
			for (int i = 0; i < fields.Length; i++)
			{
				XmlNode newNode = node.SelectSingleNode(fields[i].Name);
				fields[i].SetValue(attribType, newNode.InnerText);
			}
		}
	}
}
