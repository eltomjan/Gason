using System;
using System.Collections.Generic;

namespace Gason
{
    public class BreadthFirst
    {
        public BrowseNode Root { get; private set; }
        private BrowseNode current;
        public BrowseNode Current {
            get { return current; }
            set {
                current = value;
                BrowseNode cnt = value;
                Level = -1;
                while (cnt != null) {
                    Level++;
                    cnt = cnt.Parent_Viewer;
                }
            }
        }
        public Boolean Orphan { get {
                return current.Node_Viewer == null;
            }
        }
        public int Level { get; private set; }
        public BreadthFirst(BrowseNode wn)
        {
            Root = wn;
            current = wn;
            Level = 0;
        }
        public BrowseNode Next()
        {
            if (current == null) return current;
            if(current.Node_Viewer != null) {
                Level++;
                current = current.Node_Viewer;
            } else if(current.Next_Viewer != null) {
                current = current.Next_Viewer;
            } else {
                while(current != null) {
                    if(current.Next_Viewer != null) {
                        current = current.Next_Viewer;
                        break;
                    }
                    Level--;
                    current = current.Parent_Viewer;
                }
            }
            return current;
        }
        public Boolean Pred()
        {
            if (current.Pred_Viewer != null) {
                current = current.Pred_Viewer;
                return true;
            }
            return false;
        }
        public Boolean Parent()
        {
            if (current.Parent_Viewer != null)
            {
                Level--;
                current = current.Parent_Viewer;
                return true;
            }
            return false;
        }
        public bool NextAs(BreadthFirst second)
        {
            if (second.current.Parent_Viewer != null) second.Current = second.current.Parent_Viewer.Node_Viewer;
            else { // move left
                while (second.current.Pred_Viewer != null) second.current = second.current.Pred_Viewer;
            }
            while (Level < second.Level && second.Parent()); // 2nd up if too deep
            for(;;) {
                if (Level == second.Level) {
                    BrowseNode ndNode = second.Current;
                    while (ndNode != null) {
                        if (current.Equals(ndNode)) break; // is there Equal node in row collection ?
                        ndNode = ndNode.Next_Viewer;
                    }
                    if (ndNode == null) return false; // not found same on current level
                    second.Current = ndNode; // switch 2 same node
                    return true;
                }
                while (Level > second.Level) {
                    if (second.Level < 0)
                    {
                        second.Current = second.Root;
                        if (second.Current.NodeRawData.NodeBelow == null) return false;
                    }
                    else second.Next(); // adjust levels
                }
                if (Level != second.Level) return false;
            }
        }
        public BrowseNode RemoveCurrent()
        {
            Current = current.RemoveCurrent(); // request remove and get next one
            return current;
        }
    }
}
