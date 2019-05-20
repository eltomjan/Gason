using System;
using System.Collections.Generic;

namespace Gason
{
    public class VisualNode3
    {
        public JsonNode NodeRawData;
        readonly Byte[] src;
        int m_Shift_Width = 2;
        public int m_Indent { get; set; } = 0;
        public int m_debugModeLimit { get; set; }
        Stack<JsonNode> levelStack = new Stack<JsonNode>();
        public VisualNode3(ref JsonNode my, Byte[] src, int debugModeLimit)
        {
            NodeRawData = my;
            this.src = src;
            m_debugModeLimit = debugModeLimit;
        }
        public VisualNode3 Next_Viewer
        {
            get {
                if (NodeRawData != null && NodeRawData.NextTo != null)
                {
                    return new VisualNode3(ref NodeRawData.NextTo, src, m_debugModeLimit);
                }
                return null;
            }
        }
        public VisualNode3 Node_Viewer
        {
            get
            {
                if (NodeRawData != null && NodeRawData.ToNode() != null)
                {
                    return new VisualNode3(ref NodeRawData.NodeBelow, src, m_debugModeLimit);
                }
                else return null;
            }
        }
        public JsonTag Tag_Viewer { get { return NodeRawData.Tag; } }
        public String Key_Viewer {
            get {
                return NodeRawData.KeyView(src);
            }
        }
        public String Value_Viewer { get { return new ByteString(src, NodeRawData.doubleOrString).ToString(); } }
        public void ChangeNode(JsonNode o)
        {
            NodeRawData = o;
        }
        protected void BlockEnd(JsonNode o, ref String retVal, String newLine)
        {
            if (o.Tag == JsonTag.JSON_OBJECT)
            {
                if (m_Indent > -1) {
                    m_Indent -= m_Shift_Width;
                    retVal += new String(' ', m_Indent);
                }
                retVal += ("}" + ((o.NextTo != null ? "," : "") + newLine));
            }
            else if (o.Tag == JsonTag.JSON_ARRAY)
            {
                if (m_Indent > -1)
                {
                    m_Indent -= m_Shift_Width;
                    retVal += new String(' ', m_Indent);
                }
                retVal += ("]" + ((o.NextTo != null ? "," : "") + newLine));
            }
        }
        public String DumpValueIterative(JsonNode o, Boolean debugModeLimit)
        {
            JsonNode startNode = o;
            String space, newLine, retVal = "";
            if (m_Indent > -1) { space = " "; newLine = "\n"; }
            else { space = ""; newLine = ""; }
            JsonTag startTag;
            do
            {
                if(debugModeLimit && retVal.Length > m_debugModeLimit)
                {
                    if (m_Indent > -1) m_Indent = 0;
                    return retVal + "\n...";
                }
                if (m_Indent > -1) retVal +=(new String(' ', m_Indent)); // Start with m_Indent 
                startTag = o.Tag;
                if (startTag == JsonTag.JSON_OBJECT || startTag == JsonTag.JSON_ARRAY) {
                    String open = "";
                    if (startTag == JsonTag.JSON_ARRAY) open = "[]"; else open = "{}";
                    if (o.ToNode() == null) {
                        if (o.HasKey) retVal += ($"\"{o.Key(src)}\":{space}"); // [] or key: []
                        if (o.NextTo == null) retVal += ($"{open}{newLine}");
                        else retVal += ($"{open},{newLine}");
                        if (o.NextTo == null) o = o.NodeBelow;
                    } else {
                        open = open.Substring(0, 1);
                        if (o.HasKey) retVal += ($"\"{o.Key(src)}\":{space}{open}");
                        else retVal +=($"{open}");
                        if(o.ToNode() != null) retVal += (newLine);
                        if (o.ToNode() == null && o.NextTo != null) BlockEnd(o, ref retVal, newLine);
                        if (m_Indent > -1) m_Indent += m_Shift_Width;
                    }
                } else if (startTag == JsonTag.JSON_STRING || startTag == JsonTag.JSON_NUMBER || startTag == JsonTag.JSON_NUMBER_STR) {
                    String quote = (startTag == JsonTag.JSON_STRING) ? "\"" : "";
                    if (o.HasKey) {
                        retVal +=($"\"{o.Key(src)}\":{space}{quote}{o.ToString(src)}{quote}{(o.NextTo != null ? "," : "")}{newLine}"); // "key": "value"(,)
                    } else retVal +=($"{quote}{o.ToString(src)}{quote}{(o.NextTo != null ? "," : "")}{newLine}"); // "value"(,)
                } else if (startTag == JsonTag.JSON_TRUE || startTag == JsonTag.JSON_FALSE || startTag == JsonTag.JSON_NULL) {
                    String word;
                    if (startTag == JsonTag.JSON_TRUE) word = "true";
                    else if (startTag == JsonTag.JSON_FALSE) word = "false";
                    else word = "null";
                    if (o.HasKey) {
                        retVal +=($"\"{o.Key(src)}\":{space}{word}{(o.NextTo != null ? "," : "")}{newLine}"); // "key": "value"(,)
                    } else retVal +=($"{word}{(o.NextTo != null ? "," : "")}{newLine}"); // "value"(,)
                }
                if(o != null) {
                if (o.NodeBelow != null && (startTag == JsonTag.JSON_ARRAY || startTag == JsonTag.JSON_OBJECT))
                { // move down 2 node of structured object
                    levelStack.Push(o);
                    o = o.NodeBelow;
                    } else { // move right to values
                    if (o.NextTo != null) o = o.NextTo;
                        else o = o.NodeBelow; // always null (4 null || non-structured)
                    }
                }
                while (o == null && levelStack.Count > 0)
                { // return back after iterations
                    do {
                        o = levelStack.Pop();
                        if (o.Tag == JsonTag.JSON_ARRAY || o.Tag == JsonTag.JSON_OBJECT)
                        { // Array / Object end markers
                            BlockEnd(o, ref retVal, newLine);
                        } else {
                            BlockEnd(o, ref retVal, newLine); // Array / Object end markers
                        }
                    } while ((levelStack.Count > 1) && ((o == null || (o.NextTo == null && (o.NodeBelow == null || o.NodeBelow.NextTo == null)))));
                    o = o.NextTo; // move right
                }
                if (o == startNode)
                {
                    if (m_Indent > -1) m_Indent = 0;
                    return retVal + "\n... cycle here";
                }
            } while (o != null || (levelStack.Count > 0)) ;
            return retVal;
        }
        public override string ToString()
        {
            return _JSON;
        }
        public string _JSON
        {
            get {
                if (NodeRawData == null) return "NULL";
                return DumpValueIterative(NodeRawData, m_debugModeLimit > 0);
            }
        }
    }
}
