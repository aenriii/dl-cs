using System.Collections.Generic;
using System.Xml;

namespace dl_cs.Util
{
    public static class XmlUtil
    {
        public static List<string> GetAttrsOfTypeNode(this XmlReader reader, string nodeType, string attrName)
        {
            var list = new List<string>();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == nodeType)
                {
                    list.Add(reader.GetAttribute(attrName));
                }
            }
            return list;
        }
        public static List<string> GetValAllNodeType(this XmlReader reader, string nodeType)
        {
            var list = new List<string>();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == nodeType)
                {
                    list.Add(reader.ReadElementContentAsString());
                }
            }
            return list;
        }
        public static Stream Duplicate(this Stream stream)
        {
            long pos = stream.Position;
            MemoryStream stream2 = new();
            stream.CopyTo(stream2);
            if (stream.CanSeek) stream.Seek(pos, SeekOrigin.Begin);
            stream2.Seek(0, SeekOrigin.Begin);
            return stream2;
        }
    }
}