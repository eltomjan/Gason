using System;
using System.Collections.Generic;

namespace Gason
{
    public class BrowseNode
    {
        public JsonNode m_JsonNode;
        readonly Byte[] src;
        public int Level_Viewer { get; protected set; }
        public BrowseNode Parent_Viewer { get; private set; }
        public BrowseNode Pred_Viewer { get; private set; }
        public JsonTag Tag_Viewer { get { return m_JsonNode.Tag; } }
        public Boolean HasKey { get { return m_JsonNode.HasKey; } }
        public String Key_Viewer { get { return m_JsonNode.KeyView(src); } }
        public String Value_Viewer { get { return new ByteString(src, m_JsonNode.doubleOrString).ToString(); } }
        public BrowseNode(ref JsonNode my, Byte[] src)
        {
            Level_Viewer = 0;
            m_JsonNode = my;
            this.src = src;
        }
        public BrowseNode Next_Viewer
        {
            get {
                if (m_JsonNode != null && m_JsonNode.next != null)
                {
                    BrowseNode retVal = new BrowseNode(ref m_JsonNode.next, src) {
                        Pred_Viewer = this,
                        Parent_Viewer = this.Parent_Viewer,
                        Level_Viewer = this.Level_Viewer
                    };
                    return retVal;
                }
                return null;
            }
        }
        public BrowseNode Node_Viewer
        {
            get {
                if (m_JsonNode != null && m_JsonNode.ToNode() != null) {
                    BrowseNode retVal = new BrowseNode(ref m_JsonNode.node, src) {
                        Level_Viewer = Level_Viewer + 1,
                        Parent_Viewer = this
                    };
                    return retVal;
                } else return null;
            }
        }
        public String Path
        {
            get {
                BrowseNode end = this;
                if (end == null) return "";
                Stack<String> elements = new Stack<string>();
                while(end != null) {
                    if(end.HasKey) elements.Push($"{end.Key_Viewer}");
                    else {
                        if (end.Pred_Viewer == null && end.Next_Viewer == null) {
                            if (end.Tag_Viewer == JsonTag.JSON_ARRAY) elements.Push("[]");
                            else if(end.Tag_Viewer == JsonTag.JSON_OBJECT) elements.Push("{}");
                        } else if(end.Tag_Viewer == JsonTag.JSON_ARRAY
                             || end.Tag_Viewer == JsonTag.JSON_OBJECT) {
                            int pos = 0;
                            while(end.Pred_Viewer != null) { pos++; end = end.Pred_Viewer; }
                            elements.Push($"[{pos}]");
                        }
                    }
                    end = end.Parent_Viewer;
                }
                return String.Join(".", elements);
            }
        }
        public String DebugView1()
        {
            String retVal = "";
            if (Level_Viewer > 0) retVal = new String(' ', Level_Viewer * 2);
            if(HasKey) {
                if (m_JsonNode.Tag == JsonTag.JSON_ARRAY) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\":[\t\t<- {Path}";
                else {
                    if(m_JsonNode.Tag == JsonTag.JSON_STRING) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\":\"{Value_Viewer}\"\t\t<- {Path}";
                    else if (m_JsonNode.Tag == JsonTag.JSON_NUMBER
                          || m_JsonNode.Tag == JsonTag.JSON_NUMBER_STR
                          || m_JsonNode.Tag > JsonTag.JSON_OBJECT) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\":{Value_Viewer}\t\t<- {Path}";
                    else retVal = $"{Level_Viewer} {retVal}{Tag_Viewer}:\"{Key_Viewer}\":{Value_Viewer}\t\t<- {Path}";
                }
            } else {
                if (m_JsonNode.Tag == JsonTag.JSON_ARRAY) retVal = $"{Level_Viewer} {retVal}[\t\t<- {Path}";
                else if(m_JsonNode.Tag == JsonTag.JSON_OBJECT) retVal = $"{Level_Viewer} {retVal}{{\t\t<- {Path}";
            }
            return retVal;
        }

    }
}
