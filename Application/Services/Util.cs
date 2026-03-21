using System.IO.Compression;
using System.Xml;
using System.Xml.Serialization;

namespace Application.Services;

internal static class Util
{
    public static XmlDocument BytesToXml(byte[] bytes)
    {
        using MemoryStream ms = new(bytes);
        XmlDocument xml = new();
        xml.PreserveWhitespace = true;
        xml.Load(ms);
        return xml;
    }

    public static XmlDocument Serialize<T>(T type)
    {
        XmlSerializer serializer = new(typeof(T));
        using MemoryStream stream = new();
        XmlWriterSettings settings = new();
        settings.Indent = true;
        using XmlWriter xmlWriter = XmlWriter.Create(stream, settings);
        serializer.Serialize(xmlWriter, type);
        stream.Position = 0L;
        XmlDocument xml = new();
        xml.PreserveWhitespace = true;
        xml.Load(stream);
        return xml;
    }

    public static T Deserialize<T>(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        using XmlReader reader = XmlReader.Create(stream);
        XmlSerializer xs = new(typeof(T));
        T type = (T?)xs.Deserialize(reader) ?? throw new Exception("İçerik deserialize edilemedi.");
        return type;
    }

    public static byte[] Zip(byte[] content, string entryName)
    {
        using MemoryStream ms = new();
        using (ZipArchive archive = new(ms, ZipArchiveMode.Create, true))
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName);
            using Stream entryStream = entry.Open();
            entryStream.Write(content, 0, content.Length);
        }
        return ms.ToArray();
    }

    public static void ValidateSchema(byte[] content, string nameSpace, string schemaPath)
    {
        XmlReaderSettings settings = new();
        settings.Schemas.XmlResolver = new XmlUrlResolver();
        settings.Schemas.Add(nameSpace, schemaPath);
        settings.ValidationType = ValidationType.Schema;
        settings.ValidationEventHandler += (sender, e) => throw e.Exception;
        using MemoryStream stream = new(content);
        using XmlReader xmlReader = XmlReader.Create(stream, settings);
        while (xmlReader.Read()) { }
    }
}