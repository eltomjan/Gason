using System;

namespace Gason
{
    public class VisualNode2
    {
        public JsonNode m_JsonNode;
        Byte[] src;
        int m_Indent = 0, m_Shift_Width = 2;
        public VisualNode2(JsonNode my, Byte[] src)
        {
            m_JsonNode = my;
            this.src = src;
        }
        public VisualNode2 Next_Viewer
        {
            get {
                if (m_JsonNode != null && m_JsonNode.next != null)
                {
                    return new VisualNode2(m_JsonNode.next, src);
                }
                return null;
            }
        }
        public VisualNode2 Node_Viewer
        {
            get
            {
                if (m_JsonNode != null && m_JsonNode.ToNode() != null)
                {
                    return new VisualNode2(m_JsonNode.ToNode(), src);
                }
                else return null;
            }
        }
        public JsonTag Tag_Viewer { get { return m_JsonNode.Tag; } }
        public String Key_Viewer {
            get {
                return m_JsonNode.KeyView(src);
            }
        }
        public String Value_Viewer { get { return new ByteString(src, m_JsonNode.doubleOrString).ToString(); } }
        public void ChangeNode(JsonNode o)
        {
            m_JsonNode = o;
        }
        public String DumpValue(JsonNode o, ref String retVal, int indent = 0)
        {
            JsonNode i;
            if (o.Tag == JsonTag.JSON_NUMBER)
            {
                retVal += o.ToNumber().ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (o.Tag == JsonTag.JSON_NUMBER_STR)
            {
                retVal += (o.ToString(src));
            }
            else if (o.Tag == JsonTag.JSON_STRING)
            {
                retVal += ($"\"{ o.ToString(src) }\"");
            }
            else if (o.Tag == JsonTag.JSON_ARRAY)
            {
                // It is not necessary to use o.toNode() to check if an array or object
                // is empty before iterating over its members, we do it here to allow
                // nicer pretty printing.
                if (null == o.ToNode())
                {
                    retVal += "[]";
                }
                if (indent > -1)
                {
                    retVal += "[\n";
                }
                else
                {
                    retVal += "[";
                }
                i = o.node;
                while (null != i)
                {
                    if (indent > -1)
                        retVal += (new String(' ', indent + m_Shift_Width));
                    DumpValue(i, ref retVal, indent > -1 ? indent + m_Shift_Width : indent);
                    if (indent > -1)
                    {
                        retVal += i.next != null ? ",\n" : "\n";
                    }
                    else if (i.next != null)
                    {
                        retVal += ",";
                    }
                    i = i.next;
                }
                retVal += (indent > -1) ? (new String(' ', indent) + ']') : "]";
            }
            else if (o.Tag == JsonTag.JSON_OBJECT)
            {
                if (null == o.ToNode())
                {
                    retVal += "{}";
                }
                if (indent > -1)
                {
                    retVal += "{\n";
                }
                else
                {
                    retVal += "{";
                }
                i = o.node;
                while (null != i)
                {
                    if (indent > -1)
                        retVal += new String(' ', indent + m_Shift_Width);
                    retVal += $"\"{ i.Key(src) }\"";
                    if (indent > -1)
                    {
                        retVal += ": ";
                    }
                    else
                    {
                        retVal += ":";
                    }
                    DumpValue(i, ref retVal, indent > -1 ? indent + m_Shift_Width : indent);
                    if (indent > -1)
                    {
                        retVal += i.next != null ? ",\n" : "\n";
                    }
                    else if (i.next != null)
                    {
                        retVal += ",";
                    }
                    i = i.next;
                }
                retVal += ((indent > -1) ? new String(' ', indent) : "") + "}";
            }
            else if (o.Tag == JsonTag.JSON_TRUE)
            {
                retVal += "true";
            }
            else if (o.Tag == JsonTag.JSON_FALSE)
            {
                retVal += "false";
            }
            else if (o.Tag == JsonTag.JSON_NULL)
            {
                retVal += "null";
            }
            return retVal;
        }
        public override string ToString()
        {
            return _JSON;
        }
        public string _JSON
        {
            get {
                String retVal = m_JsonNode.Key2str(src);
                DumpValue(m_JsonNode, ref retVal, m_Indent);
                return retVal;
            }
        }
    }
}
