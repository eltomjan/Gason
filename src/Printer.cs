using System;
using System.Text;

namespace Gason
{
    class Printer
    {
        StringBuilder printing;
        private int indent;
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
            Root = new BrowseNode(ref start, src);
            current = Root;
            Level = 0;
            printing = new StringBuilder();
            this.indent = indent;

            PrintOpen();
            do
            {
                if (current.Node_Viewer != null) {
                    Level++;
                    current = current.Node_Viewer;
                    PrintOpen();
                }
                else if (current.Next_Viewer != null) {
                    current = current.Next_Viewer;
                    PrintOpen();
                } else {
                    while (current != null) {
                        PrintClose();
                        if (current.Next_Viewer != null) {
                            current = current.Next_Viewer;
                            PrintOpen();
                            break;
                        }
                        Level--;
                        current = current.Parent_Viewer;
                    }
                }
            } while (current != null);
            return printing;
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
            if (current.HasKey)
            {
                if (current.m_JsonNode.Tag == JsonTag.JSON_ARRAY) printing.Append($"\"{current.Key_Viewer}\"").Append(':').Append(indent > 0 ? " [\n" : " [");
                else
                {
                    if (current.m_JsonNode.Tag == JsonTag.JSON_STRING) printing.Append($"\"{current.Key_Viewer}\"").Append(':').Append($" \"{current.Value_Viewer}\"");
                    else if (current.m_JsonNode.Tag == JsonTag.JSON_NUMBER
                          || current.m_JsonNode.Tag == JsonTag.JSON_NUMBER_STR
                          || current.m_JsonNode.Tag > JsonTag.JSON_OBJECT) printing.Append($"\"{current.Key_Viewer}\": ").Append(current.Value_Viewer);
                    if (current.m_JsonNode.next != null)
                        if (indent > 0) printing.Append(",\n"); else printing.Append(',');
                }
            }
            else
            {
                if (current.m_JsonNode.Tag == JsonTag.JSON_ARRAY) printing.Append(indent > 0 ? "[\n" : "[");
                else
                {
                    if (current.m_JsonNode.Tag == JsonTag.JSON_STRING) printing.Append($"\"{current.Value_Viewer}\"");
                    else if (current.m_JsonNode.Tag == JsonTag.JSON_NUMBER
                          || current.m_JsonNode.Tag == JsonTag.JSON_NUMBER_STR
                          || current.m_JsonNode.Tag > JsonTag.JSON_OBJECT) printing.Append(current.Value_Viewer);
                    if (current.m_JsonNode.next != null)
                        if (indent > 0) printing.Append(",\n"); else printing.Append(',');
                }
            }
        }
        protected void PrintClose()
        {
            if (current.m_JsonNode.Tag == JsonTag.JSON_ARRAY) printing.Append(new String(' ', current.Level_Viewer * indent)).Append(']');
            else if (current.m_JsonNode.Tag == JsonTag.JSON_OBJECT) printing.Append(new String(' ', current.Level_Viewer * indent)).Append('}');
            if (current.m_JsonNode.next != null) if (indent > 0) printing.Append(",\n"); else printing.Append(',');
            else if (indent > 0) printing.Append('\n');
        }
    }
}
