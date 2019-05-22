using System;
using System.Collections.Generic;
using System.Text;

namespace Gason
{
    class Printer
    {
        StringBuilder printing;
        private int indent;
        int shift_Width;
        public BrowseNode Root { get; private set; }
        private BrowseNode current;
        public BrowseNode Current
        {
            get { return current; }
            set
            {
                current = value;
                BrowseNode cnt = value;
                Level = -1;
                while (cnt != null)
                {
                    Level++;
                    cnt = cnt.Parent_Viewer;
                }
            }
        }
        public int Level { get; private set; }
        public Printer() {}
        public StringBuilder Print(ref JsonNode start, Byte[] src, int indent)
        {
            BrowseNode root = new BrowseNode(ref start, src);
            return Print(ref root, indent);
        }
        public StringBuilder Print(ref BrowseNode current, int _indent)
        {
            BrowseNode currentNode = current;
            printing = new StringBuilder();
            indent = _indent;
            Stack<BrowseNode> levelStack = new Stack<BrowseNode>();
            shift_Width = 2;

            BrowseNode startNode = current;
            String space, newLine;
            if (indent > -1) { space = " "; newLine = "\n"; }
            else { space = ""; newLine = ""; }
            JsonTag startTag;
            do
            {
                if (indent > -1) printing.Append(new String(' ', indent)); // Start with indent 
                startTag = currentNode.NodeRawData.Tag;
                if (startTag == JsonTag.JSON_OBJECT || startTag == JsonTag.JSON_ARRAY) {
                    String open = "";
                    if (startTag == JsonTag.JSON_ARRAY) open = "[]"; else open = "{}";
                    if (currentNode.NodeRawData.NodeBelow == null) {
                        if (currentNode.HasKey) printing.Append($"\"{currentNode.KeyPrint}\":{space}"); // [] or key: []
                        if (currentNode.NodeRawData.NextTo == null) printing.Append($"{open}{newLine}");
                        else printing.Append($"{open},{newLine}");
                        if (currentNode.NodeRawData.NextTo == null) currentNode = currentNode.Node_Viewer;
                    } else {
                        open = open.Substring(0, 1);
                        if (currentNode.HasKey) printing.Append($"\"{currentNode.KeyPrint}\":{space}{open}");
                        else printing.Append($"{open}");
                        if(currentNode.NodeRawData.NodeBelow != null) printing.Append(newLine);
                        if (currentNode.NodeRawData.NodeBelow == null && currentNode.NodeRawData.NextTo != null) BlockEnd(currentNode, ref printing, newLine);
                        if (indent > -1) indent += shift_Width;
                    }
                } else if (startTag == JsonTag.JSON_STRING || startTag == JsonTag.JSON_NUMBER || startTag == JsonTag.JSON_NUMBER_STR) {
                    String quote = (startTag == JsonTag.JSON_STRING) ? "\"" : "";
                    if (currentNode.HasKey) {
                        printing.Append($"\"{currentNode.KeyPrint}\":{space}{quote}{currentNode.Value_Viewer}{quote}{(currentNode.NodeRawData.NextTo != null ? "," : "")}{newLine}"); // "key": "value"(,)
                    } else printing.Append($"{quote}{currentNode.Value_Viewer}{quote}{(currentNode.NodeRawData.NextTo != null ? "," : "")}{newLine}"); // "value"(,)
                } else if (startTag == JsonTag.JSON_TRUE || startTag == JsonTag.JSON_FALSE || startTag == JsonTag.JSON_NULL) {
                    String word;
                    if (startTag == JsonTag.JSON_TRUE) word = "true";
                    else if (startTag == JsonTag.JSON_FALSE) word = "false";
                    else word = "null";
                    if (currentNode.HasKey) {
                        printing.Append($"\"{currentNode.KeyPrint}\":{space}{word}{(currentNode.NodeRawData.NextTo != null ? "," : "")}{newLine}"); // "key": "value"(,)
                    } else printing.Append($"{word}{(currentNode.NodeRawData.NextTo != null ? "," : "")}{newLine}"); // "value"(,)
                }
                if(currentNode != null) {
                if (currentNode.NodeRawData.NodeBelow != null && (startTag == JsonTag.JSON_ARRAY || startTag == JsonTag.JSON_OBJECT))
                { // move down 2 node of structured object
                    levelStack.Push(currentNode);
                    currentNode = currentNode.Node_Viewer;
                    } else { // move right to values
                    if (currentNode.NodeRawData.NextTo != null) currentNode = currentNode.Next_Viewer;
                        else currentNode = currentNode.Node_Viewer; // always null (4 null || non-structured)
                    }
                }
                while (currentNode == null && levelStack.Count > 0)
                { // return back after iterations
                    do {
                        currentNode = levelStack.Pop();
                        if (currentNode.NodeRawData.Tag == JsonTag.JSON_ARRAY || currentNode.NodeRawData.Tag == JsonTag.JSON_OBJECT)
                        { // Array / Object end markers
                            BlockEnd(currentNode, ref printing, newLine);
                        } else {
                            BlockEnd(currentNode, ref printing, newLine); // Array / Object end markers
                        }
                    } while ((levelStack.Count > 1) && ((currentNode == null || (currentNode.NodeRawData.NextTo == null && (currentNode.NodeRawData.NodeBelow == null || currentNode.NodeRawData.NodeBelow.NextTo == null)))));
                    currentNode = currentNode.Next_Viewer; // move right
                }
                if (currentNode == startNode)
                {
                    if (indent > -1) indent = 0;
                    return printing.Append("\n... cycle here");
                }
            } while (currentNode != null || (levelStack.Count > 0)) ;
            return printing;
        }
        protected void BlockEnd(BrowseNode o, ref StringBuilder retVal, String newLine)
        {
            if (o.NodeRawData.Tag == JsonTag.JSON_OBJECT)
            {
                if (indent > -1) {
                    indent -= shift_Width;
                    retVal.Append(new String(' ', indent));
                }
                retVal.Append("}" + ((o.NodeRawData.NextTo != null ? "," : "") + newLine));
            }
            else if (o.NodeRawData.Tag == JsonTag.JSON_ARRAY)
            {
                if (indent > -1)
                {
                    indent -= shift_Width;
                    retVal.Append(new String(' ', indent));
                }
                retVal.Append("]" + ((o.NodeRawData.NextTo != null ? "," : "") + newLine));
            }
        }
        public Boolean Pred()
        {
            if (current.Pred_Viewer != null)
            {
                current = current.Pred_Viewer;
                return true;
            }
            return false;
        }
        public Boolean Parent()
        {
            if (current.Parent_Viewer != null)
            {
                current = current.Parent_Viewer;
                return true;
            }
            return false;
        }
        protected void PrintOpen()
        {
            if (current.Level_Viewer > 0 && indent > 0) printing.Append(new String(' ', current.Level_Viewer * indent));
            String key = indent > 0 ? " " : "";
            if (current.HasKey) key = $"\"{current.KeyPrint}\":{key}"; else key = "";
            if (current.NodeRawData.Tag == JsonTag.JSON_ARRAY) printing.Append(key).Append(indent > 0 ? "[\n" : "[");
            else if (current.NodeRawData.Tag == JsonTag.JSON_OBJECT) printing.Append(key).Append(indent > 0 ? "{\n" : "{");
            else {
                if (current.NodeRawData.Tag == JsonTag.JSON_STRING) printing.Append(key).Append($"\"{current.Value_Viewer}\"");
                else if (current.NodeRawData.Tag == JsonTag.JSON_NUMBER
                        || current.NodeRawData.Tag == JsonTag.JSON_NUMBER_STR
                        || current.NodeRawData.Tag > JsonTag.JSON_OBJECT) printing.Append(key).Append(current.Value_Viewer);
                if (current.NodeRawData.NextTo != null)
                    if (indent > 0) printing.Append(",\n"); else printing.Append(',');
            }
        }
        protected void PrintClose()
        {
            if (current.NodeRawData.Tag == JsonTag.JSON_ARRAY) printing.Append(new String(' ', current.Level_Viewer * indent)).Append(']');
            else if (current.NodeRawData.Tag == JsonTag.JSON_OBJECT) printing.Append(new String(' ', current.Level_Viewer * indent)).Append('}');
            if (current.NodeRawData.NextTo != null) if (indent > 0) printing.Append(",\n"); else printing.Append(',');
            else if (indent > 0) printing.Append('\n');
        }
    }
}
