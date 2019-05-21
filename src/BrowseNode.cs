using System;
using System.Collections.Generic;

namespace Gason
{
    public class BrowseNode
    {
        public JsonNode NodeRawData;
        readonly Byte[] src;
        public int Level_Viewer { get; protected set; }
        public BrowseNode Parent_Viewer { get; private set; }
        public BrowseNode Pred_Viewer { get; private set; }
        public JsonTag Tag_Viewer { get { return NodeRawData.Tag; } }
        public Boolean HasKey { get { if (null != NodeRawData) return NodeRawData.HasKey; else return false; } }
        public String Key_Viewer { get { return NodeRawData.KeyView(src); } }
        public String Value_Viewer { get { return new ByteString(src, NodeRawData.doubleOrString).ToString(); } }
        public BrowseNode(ref JsonNode my, Byte[] src)
        {
            Level_Viewer = 0;
            NodeRawData = my;
            this.src = src;
        }
        public BrowseNode Next_Viewer
        {
            get {
                if (NodeRawData != null && NodeRawData.NextTo != null)
                {
                    BrowseNode retVal = new BrowseNode(ref NodeRawData.NextTo, src) {
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
                if (NodeRawData != null && NodeRawData.ToNode() != null) {
                    BrowseNode retVal = new BrowseNode(ref NodeRawData.NodeBelow, src) {
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
                if (NodeRawData.Tag == JsonTag.JSON_ARRAY) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\": [\t\t<- {Path}";
                else {
                    if (NodeRawData.Tag == JsonTag.JSON_STRING) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\": \"{Value_Viewer}\"\t\t<- {Path}";
                    else if (NodeRawData.Tag == JsonTag.JSON_NUMBER
                          || NodeRawData.Tag == JsonTag.JSON_NUMBER_STR) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\": {Value_Viewer}\t\t<- {Path}";
                    else if (NodeRawData.Tag > JsonTag.JSON_OBJECT) {
                        if (NodeRawData.Tag == JsonTag.JSON_FALSE) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\": false\t\t<- {Path}";
                        else if (NodeRawData.Tag == JsonTag.JSON_TRUE) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\": true\t\t<- {Path}";
                        else if (NodeRawData.Tag == JsonTag.JSON_NULL) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\": null\t\t<- {Path}";
                        else retVal = "Not implemented !";
                    } else retVal = $"{Level_Viewer} {retVal}{Tag_Viewer}: \"{Key_Viewer}\": {Value_Viewer}\t\t<- {Path}";
                }
            } else {
                if (NodeRawData.Tag == JsonTag.JSON_ARRAY) retVal = $"{Level_Viewer} {retVal}[\t\t<- {Path}";
                else if(NodeRawData.Tag == JsonTag.JSON_OBJECT) retVal = $"{Level_Viewer} {retVal}{{\t\t<- {Path}";
                else {
                    if (NodeRawData.Tag == JsonTag.JSON_STRING) retVal = $"{Level_Viewer} {retVal}\"{Value_Viewer}\"\t\t<- {Path}";
                    else if (NodeRawData.Tag == JsonTag.JSON_NUMBER
                          || NodeRawData.Tag == JsonTag.JSON_NUMBER_STR) retVal = $"{Level_Viewer} {retVal}{Value_Viewer}\t\t<- {Path}";
                    else if (NodeRawData.Tag > JsonTag.JSON_OBJECT) {
                        if (NodeRawData.Tag == JsonTag.JSON_FALSE) retVal = $"{Level_Viewer} {retVal}false\t\t<- {Path}";
                        else if (NodeRawData.Tag == JsonTag.JSON_TRUE) retVal = $"{Level_Viewer} {retVal}true\t\t<- {Path}";
                        else if (NodeRawData.Tag == JsonTag.JSON_NULL) retVal = $"{Level_Viewer} {retVal}null\t\t<- {Path}";
                        else retVal = "Not implemented !";
                    } else retVal = $"{Level_Viewer} {retVal}{Tag_Viewer}: {Value_Viewer}\t\t<- {Path}";
                }
            }
            return retVal;
        }
        public Boolean KeyEquals(ByteString key)
        {
            return key.Equals(src, NodeRawData.KeyIndexesData);
        }
        public Boolean ValueEquals(ByteString val)
        {
            return val.Equals(src, NodeRawData.doubleOrString);
        }
        public Boolean Equals(BrowseNode j2)
        {
            if (NodeRawData.Tag != j2.NodeRawData.Tag) return false;
            int pos = NodeRawData.KeyIndexesData.pos, pos2 = j2.NodeRawData.KeyIndexesData.pos,
                len = NodeRawData.KeyIndexesData.length;
            if (len != j2.NodeRawData.KeyIndexesData.length) return false;

            if (pos == 0) { // 0, x
                if (pos2 > 0) return false;
            } else if (pos2 == 0) return false; // x, 0

            for (var i = 0; i < len; i++) {
                if (src[i + pos] != j2.src[i + pos2]) return false;
            }
            switch (NodeRawData.Tag) {
                case JsonTag.JSON_STRING:
                case JsonTag.JSON_NUMBER_STR:
                    len = NodeRawData.doubleOrString.length;
                    if (len != j2.NodeRawData.doubleOrString.length) return false;
                    pos = NodeRawData.doubleOrString.pos; pos2 = j2.NodeRawData.doubleOrString.pos;
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
                    return NodeRawData.doubleOrString.data == j2.NodeRawData.doubleOrString.data;
                default: // JSON_TRUE, JSON_FALSE, JSON_NULL (same tag)
                    return true;
            }

            return true;
        }
        public Boolean ReplaceNext(JsonNode newNext)
        {
            if (NodeRawData?.NextTo == null) return false;
            NodeRawData.NextTo = newNext;
            return true;
        }
        public void ReplaceNode(JsonNode newNode)
        {
            JsonNode old = NodeRawData;
            NodeRawData = newNode;
            if (old.NextTo != null) NodeRawData.NextTo = old.NextTo;
            if (old.NodeBelow != null && old.NodeBelow == null) NodeRawData.NodeBelow = old.NodeBelow;
        }
        public void SkipNext()
        {
            NodeRawData.NextTo = NodeRawData.NextTo?.NextTo;
        }
        public BrowseNode RemoveCurrent()
        {
            int arround = 0;
            if (Parent_Viewer?.NodeRawData?.NodeBelow != null) arround = 1;
            if (  Pred_Viewer?.NodeRawData            != null) arround |= 2;
            if (               NodeRawData            != null) arround |= 4;
            if (               NodeRawData?.NextTo    != null) arround |= 8;

            //VisualNode3 parent = null, pred = null, me = null;
            //if (Parent_Viewer?.NodeRawData != null) parent = new VisualNode3(ref Parent_Viewer.NodeRawData, src, 10000);
            //if (Pred_Viewer?.NodeRawData != null) pred = new VisualNode3(ref Pred_Viewer.NodeRawData, src, 10000);
            //if (NodeRawData != null) me = new VisualNode3(ref NodeRawData, src, 10000);
            //Console.WriteLine($"Arround {arround}");
            //if (parent == pred && me != null && arround > 0) ;

            BrowseNode retVal = null, retVal2 = null;
            switch (arround)
            { // 0, 1, 4 non-sense (complete Orphan), 2, 6 bug (Parent should be coppied), 8-11 impossible, 12 bug - cut from parents, 14 - bug lost parent copy
                case 3: // a -> me                      | Parent / Pred, -, -
                    retVal = Pred_Viewer;
                    retVal.SkipNext(); // Link Pred -> Next(=null)
                    return retVal; // next @pred (last node in a row)
                case 5: // Orphan, nested obj or array  | Parent / -, Node, -
                    retVal = Parent_Viewer;
                    if(retVal?.NodeRawData.NodeBelow == NodeRawData)
                    if (retVal.Tag_Viewer == JsonTag.JSON_ARRAY
                    || retVal.Tag_Viewer == JsonTag.JSON_OBJECT) {
                        retVal2 = retVal.RemoveEmpties(NodeRawData); // remove parent 2
                    }
                    if(retVal?.Parent_Viewer.NodeRawData != null) retVal = retVal?.Parent_Viewer;
                    if (retVal2 == null || retVal2.NodeRawData == null) return retVal;
                    return retVal2;
                case 7: // <-> a -> me => <-> a -> null | Parent / Pred, Node, -
                    if (!Pred_Viewer.ReplaceNext(null)) return null; // Link Pred -> Next(=null)
                    NodeRawData = null;
                    return Pred_Viewer; 
                case 13: // me -> a                     | Parent / - , Node, Next
                    retVal = Next_Viewer;
                    Parent_Viewer.NodeRawData.NodeBelow = NodeRawData.NextTo;
                    NodeRawData.NextTo = null; // clear me -> a
                    NodeRawData = null; // clear me
                    return retVal; // next @next
                case 15: // a -> me -> b => a -> b      | Parent, Pred, Node, Next
                    Pred_Viewer.SkipNext();
                    retVal = Pred_Viewer.Next_Viewer; // new Next (2x)
                    NodeRawData.NextTo = null; // clear me -> b
                    NodeRawData = null; // clear me
                    return retVal; // next @next
                default:
                    Console.WriteLine("Buggy node ?!");
                    break;
            }
            return null; // unreachable
        }
        public BrowseNode RemoveEmpties(JsonNode removed)
        {
            if (Tag_Viewer != JsonTag.JSON_ARRAY
             && Tag_Viewer != JsonTag.JSON_OBJECT) return this; // only 2 types could be useless
            int arround = 0;
            if (Parent_Viewer?.NodeRawData?.NodeBelow != null) arround =  1;
            if (  Pred_Viewer?.NodeRawData            != null) arround |= 2;
            if (               NodeRawData            != null){arround |= 4;
                if (NodeRawData.doubleOrString.data != 0) return this; // if has no value
            }
            if (               NodeRawData?.NextTo    != null) arround |= 8;

            //VisualNode3 parent = null, pred = null, me = null;
            //if (Parent_Viewer?.NodeRawData != null) parent = new VisualNode3(ref Parent_Viewer.NodeRawData, src, 10000);
            //if (Pred_Viewer?.NodeRawData != null) pred = new VisualNode3(ref Pred_Viewer.NodeRawData, src, 10000);
            //if (NodeRawData != null) me = new VisualNode3(ref NodeRawData, src, 10000);
            //Console.WriteLine($"Arround {arround}");
            //if (parent == pred && me != null && arround > 0) ;

            BrowseNode retVal = null, retVal2 = null, retVal3 = null;
            switch (arround)
            { // 0, 1, 4 non-sense (complete Orphan), 2, 6 bug (Parent should be coppied), 8-11 impossible, 12 bug - cut from parents, 14 - bug lost parent copy
                case 3: // a -> me                      | Parent / Pred, -, -
                    retVal = Pred_Viewer;
                    retVal.SkipNext(); // Link Pred -> Next(=null)
                    return retVal.RemoveEmpties(NodeRawData); // next @pred (last node in a row)
                case 4:
                    NodeRawData = null;
                    return null;
                case 5: // Orphan, nested obj or array  | Parent / -, Node, -
                    if (NodeRawData.NodeBelow != removed) return this;
                    retVal = Parent_Viewer;
                    retVal2 = retVal.RemoveEmpties(NodeRawData); // remove parent 2
                    NodeRawData = null; // clear me
                    if (retVal2 != null) return retVal2;
                    if (retVal?.Parent_Viewer != null) return retVal?.Parent_Viewer;
                    return retVal;
                case 7: // <-> a -> me => <-> a -> null | Parent / Pred, Node, -
                    if (NodeRawData.NodeBelow != removed) return this;
                    if (!Pred_Viewer.ReplaceNext(null)) return null; // Link Pred -> Next(=null)
                    NodeRawData = null;
                    return Pred_Viewer;
                case 13: // me -> a                     | Parent / - , Node, Next
                    if (NodeRawData.NodeBelow != removed) return this;
                    retVal = Next_Viewer;
                    retVal2 = Parent_Viewer;
                    Parent_Viewer.NodeRawData.NodeBelow = NodeRawData.NextTo;
                    NodeRawData.NextTo = null; // clear me -> a
                    NodeRawData = null; // clear me
                    if (retVal2 == retVal3) return retVal2; // next @next
                    return (retVal3 == null) ? retVal2 : retVal3;
                case 15: // a -> me -> b => a -> b      | Parent, Pred, Node, Next
                    Pred_Viewer.SkipNext();
                    retVal = Pred_Viewer.Next_Viewer; // new Next (2x)
                    NodeRawData.NextTo = null; // clear me -> b
                    NodeRawData = null; // clear me
                    return retVal; // next @next
                default:
                    Console.WriteLine("Buggy node ?!");
                    break;
            }
            return null;
        }
    }
}
