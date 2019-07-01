using System;
using System.Collections.Generic;

namespace Gason
{
#if DoubleLinked
    public abstract class Iterator
    {
        public readonly Byte[] src;
        public JsonNode root;
        public JsonNode Root { get { return root; } }
        internal JsonNode current;
        public JsonNode Current {
            get { return current; }
            set {
                current = value;
                UpdateLevel();
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
        internal int maxLevel = -1;
        public Iterator(JsonNode wn, Byte[] _src)
        {
            src = _src;
            root = wn;
            current = wn;
            Level = 0;
        }
        public Iterator(BrowseNode wn)
        {
            src = wn.src;
            root = wn.NodeRawData;
            current = wn.NodeRawData;
            Level = 0;
        }
        internal JsonNode NextDF()
        {
            if (current == null) return null;
            if(current.NodeBelow != null) {
                Level++;
                current = current.NodeBelow;
                if (maxLevel < Level) maxLevel = Level;
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
        internal JsonNode NextBF()
        {
            if (current == null) return null;
            if(current.NextTo != null) {
                current = current.NextTo;
            } else if(current.Parent != null) {
                int lev = Level;
                JsonNode point = current;
                while (NextDF() != null && lev != Level) ;
                if (current != null) return current;
                do {
                    current = root;
                    Level = 0;
                    lev++;
                    while (NextDF() != null && lev != Level) ;
                    current = current?.NextTo;
                    if (current != null) return current;
                } while (maxLevel >= Level && Level >= 0);
                return current;
            } else if(current.NodeBelow != null) {
                Level++;
                current = current.NodeBelow;
            } else if(current.Pred != null) {
                while (current.Pred != null) current = current.Pred;
                while (current != null && current.NodeBelow == null) current = current.NextTo;
                if (current != null) return NextDF();
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
        public abstract JsonNode Next();

        public JsonNode RemoveCurrent()
        {
            Current = current.RemoveCurrent(src); // request remove and get next one
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
            child.NextTo = current.NodeBelow; // child -> 1st child of current
            current.NodeBelow.Pred = child; // child <- 1st child of current
            current.NodeBelow = child; // current \/ child
            child.Parent = current.Parent; // child /\ current's Parent
            return true;
        }
        internal void UpdateLevel()
        {
            JsonNode value = current;
            Level = -1;
            if (value == null) return;
            JsonNode cnt = value;
            while (cnt != null)
            {
                Level++;
                cnt = cnt?.Parent;
            }
        }
    }

    public class DepthFirst : Iterator
    {
        public DepthFirst(BrowseNode wn) : base(wn) { }
        public DepthFirst(JsonNode wn, Byte[] _src) : base(wn, _src) { }

        public override JsonNode Next() { return NextDF(); }

        public bool NextAs(Byte[] src1, Iterator second, Byte[] src2)
        {
            if (second.current.Parent != null && second.current.Parent.NodeBelow != null) {
                second.current = second.current.Parent.NodeBelow;
            } else { // move left
                while (second.Pred()) ;
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
                JsonNode startAt = second.current;
                while (Level > second.Level) {
                    if (second.Level < 0)
                    {
                        second.Current = second.Root;
                        if (second.Current.NodeBelow == null) return false;
                    }
                    else {
                        second.Next(); // adjust levels
                        //if (second.Level < 0) second.Current = second.root;
                    }
                    if (second.current == null) return false;
                    if (second.current == startAt) return false;
                }
                if (Level != second.Level) return false;
            }
        }    }
    public class BreadthFirst : Iterator
    {
        public BreadthFirst(BrowseNode wn) : base(wn) { }
        public BreadthFirst(JsonNode wn, Byte[] _src) : base(wn, _src) { }
        public override JsonNode Next() { return NextBF(); }
    }
#endif
}
