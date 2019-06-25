using System;
using System.Collections.Generic;

namespace Gason
{
    public class BrowseNode
    {
        public JsonNode NodeRawData;
        public readonly Byte[] src;
        public int Level_Viewer { get; protected set; }
#if DoubleLinked
        public BrowseNode Parent_Viewer {
            get {
                if (NodeRawData.Parent == null) return null;
                return new BrowseNode(ref NodeRawData.Parent, src) { Level_Viewer = this.Level_Viewer - 1 };
            }
        }
        public BrowseNode Pred_Viewer {
            get {
                if (NodeRawData.Pred == null) return null;
                return new BrowseNode(ref NodeRawData.Pred, src) { Level_Viewer = this.Level_Viewer };
            }
        }
#endif
        public JsonTag Tag_Viewer { get { return NodeRawData.Tag; } }
        public Boolean HasKey { get { if (null != NodeRawData) return NodeRawData.HasKey; else return false; } }
        public String Key_Viewer { get { return NodeRawData.KeyView(src); } }
        public String KeyPrint { get { return NodeRawData.KeyView(src, true); } }
        private static readonly String[] specialNames = "true,false,null".Split(',');
        public String Value_Viewer {
            get {
                if (JsonTag.JSON_OBJECT < NodeRawData.Tag) {
                    return specialNames[NodeRawData.Tag - JsonTag.JSON_TRUE];
                }
                return NodeRawData.ToString(src);
            }
        }
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
                        Level_Viewer = this.Level_Viewer
                    };
#if DoubleLinked
                    retVal.NodeRawData.Parent = NodeRawData.NextTo.Parent ?? NodeRawData.Parent;
#endif
                    return retVal;
                }
                return null;
            }
        }
        public BrowseNode Node_Viewer
        {
            get {
                if (NodeRawData?.NodeBelow != null) {
                    BrowseNode retVal = new BrowseNode(ref NodeRawData.NodeBelow, src) {
                        Level_Viewer = this.Level_Viewer + 1,
                    };
                    return retVal;
                } else return null;
            }
        }
#if DoubleLinked
        public String Path_Viewer { get { return Path(); } }

        public int Count {
            get {
                int retVal = 1;
                JsonNode n = NodeRawData;
                while(n.NextTo != null)
                {
                    n = n.NextTo;
                    retVal++;
                }
                return retVal;
            }
        }

        public String Path(Boolean sortable = false)
        {
            BrowseNode end = this;
            if (end == null) return "";
            Stack<String> elements = new Stack<string>();
            while(end != null) {
                if(end.HasKey) elements.Push($"{end.Key_Viewer}");
                else {
                    if (end.Pred_Viewer == null && end.Next_Viewer == null || sortable) {
                        if (end.Tag_Viewer == JsonTag.JSON_ARRAY) elements.Push("[]");
                        else if(end.Tag_Viewer == JsonTag.JSON_OBJECT) elements.Push("{}");
                    } else if(end.Tag_Viewer == JsonTag.JSON_ARRAY
                            || end.Tag_Viewer == JsonTag.JSON_OBJECT) {
                        int pos = 0;
                        JsonNode start = end.Parent_Viewer.NodeRawData.NodeBelow;
                        while(start != null && start != end.NodeRawData) { pos++; start = start?.NextTo; }
                        if (end.Tag_Viewer == JsonTag.JSON_ARRAY) elements.Push($"[{pos}]");
                        else elements.Push($"{{{pos}}}");
                    }
                }
                end = end.Parent_Viewer;
            }
            return String.Join(".", elements);
        }
        public String DebugView1()
        {
            String retVal = "";
            if (Level_Viewer > 0) retVal = new String(' ', Level_Viewer * 2);
            if(HasKey) {
                if (NodeRawData.Tag == JsonTag.JSON_ARRAY) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\": [\t\t<- {Path()}";
                else {
                    if (NodeRawData.Tag == JsonTag.JSON_STRING) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\": \"{Value_Viewer}\"\t\t<- {Path()}";
                    else if (NodeRawData.Tag == JsonTag.JSON_NUMBER
                          || NodeRawData.Tag == JsonTag.JSON_NUMBER_STR) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\": {Value_Viewer}\t\t<- {Path()}";
                    else if (NodeRawData.Tag > JsonTag.JSON_OBJECT) {
                        if (NodeRawData.Tag == JsonTag.JSON_FALSE) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\": false\t\t<- {Path()}";
                        else if (NodeRawData.Tag == JsonTag.JSON_TRUE) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\": true\t\t<- {Path()}";
                        else if (NodeRawData.Tag == JsonTag.JSON_NULL) retVal = $"{Level_Viewer} {retVal}\"{Key_Viewer}\": null\t\t<- {Path()}";
                        else retVal = "Not implemented !";
                    } else retVal = $"{Level_Viewer} {retVal}{Tag_Viewer}: \"{Key_Viewer}\": {Value_Viewer}\t\t<- {Path()}";
                }
            } else {
                if (NodeRawData.Tag == JsonTag.JSON_ARRAY) retVal = $"{Level_Viewer} {retVal}[\t\t<- {Path()}";
                else if(NodeRawData.Tag == JsonTag.JSON_OBJECT) retVal = $"{Level_Viewer} {retVal}{{\t\t<- {Path()}";
                else {
                    if (NodeRawData.Tag == JsonTag.JSON_STRING) retVal = $"{Level_Viewer} {retVal}\"{Value_Viewer}\"\t\t<- {Path()}";
                    else if (NodeRawData.Tag == JsonTag.JSON_NUMBER
                          || NodeRawData.Tag == JsonTag.JSON_NUMBER_STR) retVal = $"{Level_Viewer} {retVal}{Value_Viewer}\t\t<- {Path()}";
                    else if (NodeRawData.Tag > JsonTag.JSON_OBJECT) {
                        if (NodeRawData.Tag == JsonTag.JSON_FALSE) retVal = $"{Level_Viewer} {retVal}false\t\t<- {Path()}";
                        else if (NodeRawData.Tag == JsonTag.JSON_TRUE) retVal = $"{Level_Viewer} {retVal}true\t\t<- {Path()}";
                        else if (NodeRawData.Tag == JsonTag.JSON_NULL) retVal = $"{Level_Viewer} {retVal}null\t\t<- {Path()}";
                        else retVal = "Not implemented !";
                    } else retVal = $"{Level_Viewer} {retVal}{Tag_Viewer}: {Value_Viewer}\t\t<- {Path()}";
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
#endif
    }
}
