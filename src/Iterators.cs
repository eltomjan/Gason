using System;
using System.Collections.Generic;

namespace Gason
{
    public class BreadthFirst
    {
        public readonly Byte[] src;
        public JsonNode root;
        public JsonNode Root { get { return root; } }
        private JsonNode current;
        public JsonNode Current {
            get { return current; }
            set {
                current = value;
                Level = -1;
                if (value == null) return;
                JsonNode cnt = value;
                while (cnt != null) {
                    Level++;
                    cnt = cnt?.Parent;
                }
            }
        }
        private static readonly String[] specialNames = "true,false,null".Split(',');
        public override string ToString()
        {
            if (JsonTag.JSON_OBJECT < current.Tag) {
                return specialNames[current.Tag - JsonTag.JSON_TRUE];
            }
            return new ByteString(src, current.doubleOrString).ToString();
        }
        public Boolean Orphan { get {
                return (current.NodeBelow == null);
                    ;//&& (current?.NodeRawData?.NextTo == null);
            }
        }
        public int Level { get; private set; }
        public BreadthFirst(JsonNode wn, Byte[] _src)
        {
            src = _src;
            root = wn;
            current = wn;
            Level = 0;
        }
        public BreadthFirst(BrowseNode wn)
        {
            src = wn.src;
            root = wn.NodeRawData;
            current = wn.NodeRawData;
            Level = 0;
        }
        public JsonNode Next()
        {
            if (current == null) return null;
            if(current.NodeBelow != null) {
                Level++;
                current = current.NodeBelow;
            } else if(current.NextTo != null) {
                current = current.NextTo;
            } else {
                while(current != null) {
                    if(current.NextTo != null) {
                        current = current.NextTo;
                        break;
                    }
                    Level--;
                    current = current.Parent;
                }
            }
            return current;
        }
        public Boolean Pred()
        {
            if (current.Pred != null) {
                current = current.Pred;
                return true;
            }
            return false;
        }
        public Boolean Parent()
        {
            if (current.Parent != null)
            {
                Level--;
                current = current.Parent;
                return true;
            }
            return false;
        }
        public bool NextAs(Byte[] src1, BreadthFirst second, Byte[] src2)
        {
            if (second.current.Parent != null) second.current = second.current.Parent.NodeBelow;
            else { // move left
                while (second.current.Pred != null) second.current = second.current.Pred;
            }
            while (Level < second.Level && second.Parent()); // 2nd up if too deep
            for(;;) {
                if (Level == second.Level) {
                    JsonNode ndNode = second.current;
                    while (ndNode != null) {
                        if (current == null || current.Equals(src1, ndNode, src2)) break; // is there Equal node in row collection ?
                        ndNode = ndNode.NextTo;
                    }
                    if (ndNode == null) return false; // not found same on current level
                    second.Current = ndNode; // switch 2 same node
                    return true;
                }
                while (Level > second.Level) {
                    if (second.Level < 0)
                    {
                        second.Current = second.Root;
                        if (second.Current.NodeBelow == null) return false;
                    }
                    else second.Next(); // adjust levels
                }
                if (Level != second.Level) return false;
            }
        }
        public JsonNode RemoveCurrent()
        {
            Current = current.RemoveCurrent(); // request remove and get next one
            return current;
        }
        public Boolean FindNode(String name)
        {
            JsonNode posBackup = current;
            while (current != null)
            {
                if (current.KeysEqual(src, name)) return true;
                Next();
            }
            current = posBackup;
            return false;
        }
        public Boolean NextNth(int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (current == null) return false;
                current = current.NextTo;
            }
            return (current != null);
        }
        public Boolean PrependChild(JsonNode child)
        {
            if (current?.NodeBelow == null) return false;
            child.NextTo = current.NodeBelow;
            current.NodeBelow = child;
            return true;
        }
    }
}
