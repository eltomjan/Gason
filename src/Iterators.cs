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
        public int Level { get; private set; }
        public BreadthFirst(BrowseNode wn)
        {
            Root = wn;
            current = wn;
            Level = 0;
        }
        public BrowseNode Next()
        {
            if(Current.Node_Viewer != null) {
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
                current = current.Parent_Viewer;
                return true;
            }
            return false;
        }
    }
}
