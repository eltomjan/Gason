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
        public Printer() {}
        public StringBuilder Print(ref JsonNode start, Byte[] src, int indent, Boolean debugInfo = false)
        {
            BrowseNode root = new BrowseNode(ref start, src);
            return Print(ref root, indent, debugInfo);
        }
        public StringBuilder Print(ref BrowseNode current, int _indent, Boolean debugInfo = false)
        {
            BrowseNode currentNode = current;
            printing = new StringBuilder();
            indent = _indent;
            Stack<BrowseNode> levelStack = new Stack<BrowseNode>();
            shift_Width = 2;

            BrowseNode startNode = current;
            String space, newLine;
            if (indent > -1) { space = " "; newLine = "\r\n"; }
            else { space = ""; newLine = ""; }
            JsonTag startTag;
            do
            {
                if (indent > -1) printing.Append(new String(' ', indent)); // Start with indent 
                startTag = currentNode.NodeRawData.Tag;
                if(debugInfo) {
                    JsonNode around = currentNode.NodeRawData;
#if DoubleLinked
                    if (around.Pred != null) printing.Append('<');
                    if (around.Parent != null) printing.Append('^');
#endif
                    if (around.NodeBelow != null) printing.Append(@"\/");
                    if (around.NextTo != null) printing.Append('>');
#if DEBUGGING
                    printing.Append($" {around.uniqueNo}/");
#endif
                }
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
/*/
#if DoubleLinked
        public String PrintStructureInfo(JsonNode start, Byte[] src, int indent)
        {
            String retVal;
            Queue<String> nodeNames = new Queue<String>();
            Queue<JsonNode> whereNext = new Queue<JsonNode>();
            HashSet<JsonNode> visited = new HashSet<JsonNode>();
            int i = 0;
            retVal = JsonNode.PrintStructureInfo(i++, ref start, src, indent);
            visited.Add(start);
            do {
                nodeNames = start.PushNodes(ref visited,ref whereNext);
                if (whereNext.Count == 0) break;
                start = whereNext.Dequeue();
                if (!visited.Contains(start)) {
                    visited.Add(start);
                    retVal += //nodeNames.Dequeue() +
                         JsonNode.PrintStructureInfo(i++, ref start, src, indent);
                }
            } while (true);
            return retVal;
        }
#endif
//*/
    }
}
