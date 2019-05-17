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
        public Boolean Equals(BrowseNode j2)
        {
            if (m_JsonNode.Tag != j2.m_JsonNode.Tag) return false;
            int pos = m_JsonNode.KeyIndexesData.pos, pos2 = j2.m_JsonNode.KeyIndexesData.pos,
                len = m_JsonNode.KeyIndexesData.length;
            if (len != j2.m_JsonNode.KeyIndexesData.length) return false;

            if (pos == 0) { // 0, x
                if (pos2 > 0) return false;
            } else if (pos2 == 0) return false; // x, 0

            for (var i = 0; i < len; i++) {
                if (src[i + pos] != j2.src[i + pos2]) return false;
            }
            switch (m_JsonNode.Tag) {
                case JsonTag.JSON_STRING:
                case JsonTag.JSON_NUMBER_STR:
                    len = m_JsonNode.doubleOrString.length;
                    if (len != j2.m_JsonNode.doubleOrString.length) return false;
                    pos = m_JsonNode.doubleOrString.pos; pos2 = j2.m_JsonNode.doubleOrString.pos;
                    if (pos == 0) { // 0, x
                        if (pos2 > 0) return false;
                    }
                    else if (pos2 == 0) return false; // x, 0
                    for (var i = 0; i < len; i++) {
                        if (src[i + pos] != j2.src[i + pos2]) return false;
                    }
                    break;
                case JsonTag.JSON_ARRAY:
                case JsonTag.JSON_OBJECT:
                    return true;
                case JsonTag.JSON_NUMBER:
                    return m_JsonNode.doubleOrString.data == j2.m_JsonNode.doubleOrString.data;
                default: // JSON_TRUE, JSON_FALSE, JSON_NULL (same tag)
                    return true;
            }

            return true;
        }
        public BrowseNode RemoveCurrent()
        {
            int arround = 0;
            if (Parent_Viewer?.m_JsonNode != null) arround |= 1;
            if (  Pred_Viewer?.m_JsonNode != null) arround |= 2;
            if (               m_JsonNode != null) arround |= 4;
            if (         m_JsonNode?.next != null) arround |= 8;

            //VisualNode3 parent = null, pred = null, me = null;
            //if (Parent_Viewer?.m_JsonNode != null) parent = new VisualNode3(ref Parent_Viewer.m_JsonNode, src, 10000);
            //if (Pred_Viewer?.m_JsonNode != null) pred = new VisualNode3(ref Pred_Viewer.m_JsonNode, src, 10000);
            //if (m_JsonNode != null) me = new VisualNode3(ref m_JsonNode, src, 10000);
            //Console.WriteLine($"Arround {arround}");
            //if (parent == pred && me != null && arround > 0) ;

            BrowseNode retVal = null;
            switch (arround)
            {
                case 3: // a -> me
                    Pred_Viewer.m_JsonNode.next.node = null; // clear skipped node
                    Pred_Viewer.m_JsonNode.next = Pred_Viewer.m_JsonNode.next?.next;
                    return Parent_Viewer;
                case 5: // only parent & me
                    retVal = Parent_Viewer;
                    if (retVal.m_JsonNode.node == m_JsonNode) retVal.m_JsonNode.node = retVal.m_JsonNode.next;
                    m_JsonNode.next = null; // clear me -> a
                    retVal.m_JsonNode = null; // clear me
                    return retVal.RemoveCurrent();
                case 7: // <-> a -> me => <-> a -> null
                    Pred_Viewer.m_JsonNode.next = m_JsonNode.next;
                    if (m_JsonNode.next == null)
                    {
                        m_JsonNode = null;
                        return Pred_Viewer;
                    }
                    break; // unreachable
                case 13: // me -> a
                    Parent_Viewer.m_JsonNode.node = m_JsonNode.next;
                    //m_JsonNode.next = null; // clear me -> a
                    //m_JsonNode = null; // clear me
                    return Next_Viewer;
                case 15: // a -> me -> b => a -> b
                    Pred_Viewer.m_JsonNode.next = m_JsonNode.next; // skip me
                    retVal = Pred_Viewer.Next_Viewer;
                    m_JsonNode.next = null; // clear me -> b
                    m_JsonNode = null; // clear me
                    return retVal;
                default:
                    Console.WriteLine("Buggy node ?!");
                    break;
            }
            return null; // unreachable
        }
    }
}
