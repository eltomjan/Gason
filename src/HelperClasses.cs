using System;

namespace Gason
{
    public partial class JsonNode
    {
        public JsonNode Next2parent(ref DebugVisual dv)
        { // 9 Parent / - , - , Next
            JsonNode retVal = next;
            parent.node = next;
            next.parent = parent; // null not set by parser
            dv?.update(this);
            next.pred = null;
            dv?.update(this);
            next = null;
            dv?.update(this, -8);
            parent = null;
            dv?.update(this, -1);
            return retVal;
        }
        public JsonNode Pred2parent(ref DebugVisual dv)
        { // 3 Parent / Pred, -, -
            JsonNode retVal = pred;
            retVal.next = null; // Link Pred -> Next(=null)
            pred = null;
            dv?.update(this, -2);
            parent = null;
            dv?.update(this, -1);
            return retVal; // next @pred (last node in a row)
        }
        public JsonNode DeleteNode2next(ref DebugVisual dv)
        { // 13 Parent / - , Node, Next
            JsonNode retVal = next, retVal2 = parent;
            retVal.pred = null;
            parent.node = next;
            next.parent = parent;
            parent = null;
            dv?.update(this, -1);
            next = null;
            dv?.update(this, -8);
            node = null;
            dv?.update(this, -4);
            return retVal ?? retVal2;
        }
        public class DebugVisual
        {
            String desc;
            readonly Byte[] src;
            int arround;
            VisualNode3 parent, pred, that, node, next, Parent_View;
            public DebugVisual(JsonNode _me, int _arround, Byte[] _src)
            {
                src = _src;
                arround = _arround;
                update(_me, _arround);
            }
            public void update(JsonNode _me, int? _arround = null) {
                //                   0    1    2     3    4    5     6     7    8     9   10     11    12    13     14     15
                String[] arrows = { "x", "↑", "←", "←↑", "↓", "↕", "←↓", "←↕", "→", "↑→", "↔", "←↑→", "↓→", "↕→", "←↓→", "←↕→" };
                if (_me.parent?.node == null) {
                    Parent_View = parent;
                    parent = null;
                } else parent = new VisualNode3(ref _me.parent.node, src, 10000);
                pred = new VisualNode3(ref _me.pred, src, 10000);
                that = new VisualNode3(ref _me, src, 10000);
                if (_arround < 0) arround += (int)_arround;
                desc = $"{arrows[arround]} {that.Tag_Viewer} {that.Key_Viewer}:{that.Value_Viewer}";
                node = new VisualNode3(ref _me.node, src, 10000);
                next = new VisualNode3(ref _me.next, src, 10000);
            }

            public override string ToString()
            {
                return desc;
            }
        }
    }
}
