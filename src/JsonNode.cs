using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Gason
{
    public partial class JsonNode
    { // 16B
        public JsonTag Tag = JsonTag.JSON_NULL;
        private JsonNode next; // 4B
        private JsonNode parent;
        private JsonNode pred;
        private JsonNode node;
        protected P_ByteLnk keyIdxes;
#if DebugPrint
        public int startPos, endPos;
#endif
#if DEBUGGING
        public int uniqueNo;
#endif
        public P_ByteLnk KeyIndexesData { get { return keyIdxes; } }

        public P_ByteLnk doubleOrString;
        public ref JsonNode Parent { get { return ref parent; } }
        public ref JsonNode Pred { get { return ref pred; } }
        public ref JsonNode NextTo {
            get {
                if (next != null) next.parent = parent;
                return ref next;
            }
        }
        public void SetNextTo(JsonNode newNode) {
            if(next?.pred != null) next.pred = null;
            next = newNode;
        }
        public ref JsonNode NodeBelow {
            get {
                if(node?.parent != null) node.parent = this;
                return ref node;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonNode() {}
        public String Key(Byte[] src)
        {
            if (keyIdxes.pos + keyIdxes.length < src.Length)
                return Encoding.UTF8.GetString(src, keyIdxes.pos, keyIdxes.length);
            else return "Key index out of string !";
        }
        public String KeyView(Byte[] src, Boolean print = false)
        {
            if (keyIdxes.length == 0) return print ? "" : $"Empty:{keyIdxes.pos},{keyIdxes.length}";
            else return Encoding.UTF8.GetString(src, keyIdxes.pos, keyIdxes.length);
        }
        public String debugView(Byte[] src) { return $"{Tag} {KeyView(src)}:{ToString(src)}"; }
        public Boolean HasKey  { get { return (keyIdxes.pos != 0) || (keyIdxes.length != 0); } }
        public String Key2str(Byte[] src)
        {
            if (keyIdxes.length == 0) return "";
            else return Encoding.UTF8.GetString(src, keyIdxes.pos, keyIdxes.length) + " :";
        }
        public Boolean KeysEqual(Byte[] src, P_ByteLnk nd)
        {
            if (keyIdxes.length != nd.length) return false;
            int length = keyIdxes.length;
            for (int i = 0; i < length; i++)
            {
                if (src[keyIdxes.pos + i] != src[nd.pos + i]) return false;
            }
            return true;
        }
        public Boolean KeysEqual(Byte[] src, String nd)
        {
            if (keyIdxes.length != nd.Length) return false;
            int length = keyIdxes.length;
            for (int i = 0; i < length; i++)
            {
                if (src[keyIdxes.pos + i] != nd[i]) return false;
            }
            return true;
        }
        public Boolean VakuesEqual(Byte[] src, JsonNode nd)
        {
            if (Tag != nd.Tag) return false;
            if (doubleOrString.length != nd.doubleOrString.length) return false;
            if (Tag == JsonTag.JSON_NUMBER) return doubleOrString.number == nd.doubleOrString.number;
            int length = doubleOrString.length;
            for (int i = 0; i < length; i++)
            {
                if (src[doubleOrString.pos + i] != src[nd.doubleOrString.pos + i]) return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InsertAfter(JsonNode orig)
        { // orig = previous node, 1t, ... / this == last Node
            if (orig == null)
            {
                next = this;
                return;
            }
            next = orig.next; // last -> 1st
            orig.next = this; // 1st -> last
            this.pred = orig; // before prev <- last
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InsertAfter(JsonNode orig, ref P_ByteLnk key)
        { // orig = previous node, 1t, ... / this == last Node
            keyIdxes.data = key.data;
            key.length = -1;
            if (orig == null)
            {
                next = this;
                return;
            }
            next = orig.next; // last -> 1st
            orig.next = this; // prev -> last
            this.pred = orig; // before prev <- last
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ListToValue(JsonTag tag, JsonNode tail)
        { // this=parent, tails[pos]=last, 1st, ...
            Tag = tag;
            JsonNode head = tail;
            if (null != head)
            {
                head = tail.next; // 1st one
                tail.next = null; // last -> next => cut end
                head.pred = null; // Remove pred of 1st one
                node = head; // attach to parent
                head.parent = this; // and 
                doubleOrString.data = 0; // clear parent's value
            } else if(tag == JsonTag.JSON_ARRAY) doubleOrString.data = 0; // clear key value
        }
        public ByteString GetFatData(Byte[] src) { return new ByteString(src, doubleOrString); }
        public string ToString(Byte[] src)
        {
            switch (Tag)
            {
                case JsonTag.JSON_NUMBER:
                    return ToNumber().ToString(System.Globalization.CultureInfo.InvariantCulture);
                case JsonTag.JSON_NUMBER_STR:
                    return Encoding.ASCII.GetString(src, doubleOrString.pos, doubleOrString.length);
                case JsonTag.JSON_FALSE:
                    return "false";
                case JsonTag.JSON_TRUE:
                    return "true";
                case JsonTag.JSON_NULL:
                    return "null";
                case JsonTag.JSON_STRING:
                    return Encoding.UTF8.GetString(src, doubleOrString.pos, doubleOrString.length);
                case JsonTag.JSON_ARRAY:
                    return "Array";
                case JsonTag.JSON_OBJECT:
                    return "Object";
            }
            return "#N/A";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int String2double(ref int refPos, Byte[] src, bool minus = false)
        { // -[0-9]*.[0-9]+[eE][0-9]+
            doubleOrString.data = 0; // number = 0
#if !SKIP_VALIDATION
            if(src[refPos] == '0' && SearchTables.valTypes[src[refPos+1]] == 4) return refPos;
#endif
            while (SearchTables.valTypes[src[refPos]] == 4)
                doubleOrString.number = (doubleOrString.number * 10) + (src[refPos++] - '0');

            if (src[refPos] == '.')
            {
                ++refPos;

                double fraction = 1;
                while (SearchTables.valTypes[src[refPos]] == 4)
                {
                    fraction *= 0.1;
                    doubleOrString.number += (src[refPos++] - '0') * fraction;
                }
            }

            if (SearchTables.valTypes[src[refPos]] == 5)
            {
                ++refPos;

                double vbase = 10;
                if (src[refPos] == '+')
                    ++refPos;
                else if (src[refPos] == '-')
                {
                    ++refPos;
                    vbase = 0.1;
                }

                uint exponent = 0;
#if !SKIP_VALIDATION
                if (SearchTables.valTypes[src[refPos]] != 4) return --refPos;
#endif
                while (SearchTables.valTypes[src[refPos]] == 4)
                    exponent = (exponent * 10) + ((uint)src[refPos++] - '0');

                double power = 1;
                for (; exponent != 0; exponent >>= 1, vbase *= vbase)
                    if ((exponent & 1) != 0)
                        power *= vbase;

                doubleOrString.number *= power;
            }

            if (minus) doubleOrString.number = -doubleOrString.number;
            Tag = JsonTag.JSON_NUMBER;
            return refPos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int String2decimal(ref int refPos, Byte[] src, bool minus = false)
        { // String2double copy
            if (minus) doubleOrString.pos = refPos - 1; else doubleOrString.pos = refPos;
#if !SKIP_VALIDATION
            if (src[refPos] == '0' && SearchTables.valTypes[src[refPos + 1]] == 4) return refPos;
#endif
            while (SearchTables.valTypes[src[refPos]] == 4) refPos++;

            if (src[refPos] == '.')
            {
                ++refPos;

                while (SearchTables.valTypes[src[refPos]] == 4) refPos++;
            }

            if (SearchTables.valTypes[src[refPos]] == 5)
            {
                ++refPos;

                if (src[refPos] == '+' || src[refPos] == '-')
                    ++refPos;
#if !SKIP_VALIDATION
                if (SearchTables.valTypes[src[refPos]] != 4) return --refPos;
#endif
                while (SearchTables.valTypes[src[refPos]] == 4) refPos++;
            }

            Tag = JsonTag.JSON_NUMBER_STR;
            doubleOrString.length = refPos - doubleOrString.pos;
            return refPos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonErrno GetString(ref int refPos, Byte[] src)
        {
            doubleOrString.pos = refPos;
            int end = refPos;
            while (src[end] != '"')
            {
                if (src[end] == '\\')
                {
                    end++;
                    if (src[end] == 'u')
                    {
                        end += 4;
#if !SKIP_VALIDATION
                    }
                    else
                    { // or \\,\"./,\b\f\n\r\t
                        if ("\\\"/bfnrt".IndexOf((char)src[end]) < 0)
                            return JsonErrno.BAD_STRING;
#endif
                    }
                }
#if !SKIP_VALIDATION
                else if (src[end] < ' ')
                {
                    return JsonErrno.BAD_STRING;
                }
#endif
                end++;
            }
            doubleOrString.length = end - refPos;
            refPos = end + 1;
            this.Tag = JsonTag.JSON_STRING;
            return JsonErrno.OK;
        }
        public double ToNumber()
        {
            return doubleOrString.number;
        }
        public JsonNode ToNode()
        {
            return NodeBelow;
        }
        internal Queue<String> PushNodes(ref HashSet<JsonNode> visited, ref Queue<JsonNode> whereNext)
        {
            Queue<String> retVal = new Queue<String>();
            if (parent != null) {
                if (!visited.Contains(parent)) {
                    whereNext.Enqueue(parent);
                    retVal.Enqueue("Parent ");
                }
            }
            if (next != null) {
                if (!visited.Contains(next)) {
                    whereNext.Enqueue(next);
                    retVal.Enqueue("Next ");
                }
            }
            if (pred != null) {
                if (!visited.Contains(pred)) {
                    whereNext.Enqueue(pred);
                    retVal.Enqueue("Pred ");
                }
            }
            if (node != null) {
                if (!visited.Contains(node)) {
                    whereNext.Enqueue(node);
                    retVal.Enqueue("Node ");
                }
            }
            return retVal;
        }
/*/
        unsafe public static String PrintStructureInfo(int i, ref JsonNode me, Byte[] src, int indent)
        {
            String retVal;
            TypedReference mr = __makeref(me), pred = __makeref(me.pred), myNode = __makeref(me.node), next = __makeref(me.next), parent = __makeref(me.parent);
            IntPtr mePtr = **(IntPtr**)(&mr),
                predPtr = **(IntPtr**)(&pred),
                nodePtr = **(IntPtr**)(&myNode),
                nPtr = **(IntPtr**)(&next),
                paPtr = **(IntPtr**)(&parent);
            IntPtr addr = (IntPtr)123456, a0 = (IntPtr)0;
            //*/ /*
            if (mePtr != a0) mePtr = addr;
            if (predPtr != a0) predPtr = addr;
            if (nodePtr != a0) nodePtr = addr;
            if (nPtr != a0) nPtr = addr;
            if (paPtr != a0) paPtr = addr;//*/ /*
            retVal = $"{i}: {mePtr} Tag {me.Tag}\n" +
                $"HasKey:{me.HasKey} {me.KeyView(src, false)}\n" +
                $"KeyIndexesData Data:{me.keyIdxes.data} Pos:{me.keyIdxes.pos} Len:{me.keyIdxes.length}\n" +
                $"Pred: {predPtr}\n" +
                $"Node: {nodePtr}\n" +
                $"Next: {nPtr}\n" +
                $"Parent: {paPtr}\n" +
                $"doubleOrString Data:{me.doubleOrString.data} Pos:{me.doubleOrString.pos} Len:{me.doubleOrString.length} Nr:{me.doubleOrString.number}\n" +
                new ByteString(src, me.doubleOrString).ToString() +
                "\n\n";

            return retVal;
        }
//*/
        public Boolean Equals(Byte[] src1, JsonNode j2, Byte[] src2)
        {
            if (Tag != j2.Tag) return false;
            int pos = KeyIndexesData.pos, pos2 = j2.KeyIndexesData.pos,
                len = KeyIndexesData.length;
            if (len != j2.KeyIndexesData.length) return false;

            if (pos == 0)
            { // 0, x
                if (pos2 > 0) return false;
            }
            else if (pos2 == 0) return false; // x, 0

            for (var i = 0; i < len; i++)
            {
                if (src1[i + pos] != src2[i + pos2]) return false;
            }
            switch (Tag)
            {
                case JsonTag.JSON_STRING:
                case JsonTag.JSON_NUMBER_STR:
                    len = doubleOrString.length;
                    if (len != j2.doubleOrString.length) return false;
                    pos = doubleOrString.pos; pos2 = j2.doubleOrString.pos;
                    if (pos == 0)
                    { // 0, x
                        if (pos2 > 0) return false;
                    }
                    else if (pos2 == 0) return false; // x, 0
                    for (var i = 0; i < len; i++)
                    {
                        if (src1[i + pos] != src2[i + pos2]) return false;
                    }
                    break;
                case JsonTag.JSON_ARRAY:
                case JsonTag.JSON_OBJECT:
                    return true;
                case JsonTag.JSON_NUMBER:
                    return doubleOrString.data == j2.doubleOrString.data;
                default: // JSON_TRUE, JSON_FALSE, JSON_NULL (same tag)
                    return true;
            }

            return true;
        }
        public Boolean ReplaceNext(JsonNode newNext)
        {
            if (next == null) return false;
            next = newNext;
            if(newNext != null) {
                newNext.pred = this;
                newNext.parent = parent;
            }
            return true;
        }
        public void SkipNext()
        {
            if (next?.next?.next?.pred != null) next.next.next.pred = next;
            next.pred = null;
            next = next?.next;
        }
        public JsonNode RemoveCurrent(Byte[] src)
        {
            JsonNode retVal = null, retVal2 = null;
            int arround = 0;
            if (parent       != null) arround  = 1; // ↑
            if (pred         != null) arround |= 2; // ←
            if (      node   != null) arround |= 4; // ↓
            if (        next != null) arround |= 8; // →

            VisualNode3 vn;
            DebugVisual dv = null; // new DebugVisual(this, arround, src);

            switch (arround)
            {
                case 1: //                              | Parent / -, -, -
                    retVal = parent;
                    retVal2 = parent?.parent;
                    parent.node = null;
                    parent = null;
                    retVal = retVal.RemoveEmpties(this, src);
                    retVal = retVal ?? retVal2;
                    dv?.update(retVal, -1);
                    return retVal;
                case 2: //                              | - / Pred, -, -
                    retVal = pred;
                    pred.next = null;
                    pred = null;
                    return retVal;
                case 3: //                              | Parent / Pred, -, -
                    return Pred2parent(ref dv);
                case 5: // Orphan, nested obj or array  | Parent / -, Node, -
                    retVal = Parent;
                    if (retVal?.node.node == this)
                        if (retVal.Tag == JsonTag.JSON_ARRAY
                        || retVal.Tag == JsonTag.JSON_OBJECT)
                        {
                            retVal2 = retVal.RemoveEmpties(node, src); // remove parent 2
                        }
                    if (retVal?.Parent.NodeBelow != null) retVal = retVal?.Parent;
                    if (retVal2 == null || retVal2.NodeBelow == null) return retVal;
                    return retVal2;
                case 7: // <-> a -> me => <-> a -> null | Parent / Pred, Node, -
                    retVal = Pred;
                    if (!Pred.ReplaceNext(null)) return null; // Link Pred -> Next(=null)
                    node = null;
                    NodeBelow = null;
                    return retVal;
                case 9: // Parent / - , - , Next
                    return Next2parent(ref dv);
                case 10: // - / Pred , - , Next
                    retVal = next;
                    pred.next = next;
                    next.pred = pred;
                    next.parent = parent;
                    return next;
                case 11: // Parent / Pred , - , Next
                    retVal = next;
                    pred.next = next;
                    next.pred = pred;
                    next.parent = parent;
                    return next;
                case 13: // me -> a                     | Parent / - , Node, Next
                    retVal = next;
                    parent.node = next;
                    next.parent = parent;
                    node = null; // clear me
                    return retVal; // next @next
                case 15: // a -> me -> b => a -> b      | Parent, Pred, Node, Next
                    pred.next = next;
                    next.pred = pred;
                    retVal = next;
                    node = null; // clear me
                    return retVal; // next @next
                default:
                    Console.WriteLine($"RemoveCurrent buggy node ({arround}) ?!");
                    break;
            }
            return null; // unreachable
        }
        public JsonNode RemoveEmpties(JsonNode removed, Byte[] src)
        {
            if (Tag != JsonTag.JSON_ARRAY && Tag != JsonTag.JSON_OBJECT
             /*&& !(keyIdxes.data == 0 && doubleOrString.data == 0 && node == null)*/) return this; // only 2 types could be useless
            int arround = 0;
            if (parent       != null) arround  = 1; // ↑
            if (pred         != null) arround |= 2; // ←
            if (      node   != null)               // ↓
            {
                                      arround |= 4;
                if (doubleOrString.data != 0) return this; // if has no value
            }
            if (        next != null) arround |= 8; // →

            VisualNode3 vn;
            JsonNode retVal = null, retVal2 = null;
            DebugVisual dv = null; // new DebugVisual(this, arround, src);

            switch (arround)
            {
                case 1: //                              | Parent / -, -, -
                    retVal = parent;
                    retVal.node = null;
                    parent = null;
                    retVal2 = retVal.parent;
                    if (retVal2 != null) {
                        retVal = retVal2.RemoveEmpties(retVal, src);
                        vn = new VisualNode3(ref retVal, src, 10000);
                        return retVal;
                    }
                    return retVal;
                case 2: //                              | - / Pred, -, -
                    retVal = pred;
                    retVal.next = null;
                    return retVal;
                case 3: //                              | Parent / Pred, -, -
                    retVal = parent;
                    pred.next = null;
                    parent = null;
                    pred = null;
                    dv.update(retVal, -3);
                    return retVal; // next @pred (last node in a row)
                case 4:
                    NodeBelow = null;
                    return null;
                case 5: // Orphan, nested obj or array  | Parent / -, Node, -
                    if (node != removed) return this;
                    retVal = Parent;
                    retVal2 = retVal.RemoveEmpties(this, src); // remove parent 2
                    parent.node = null;
                    parent = null;
                    node.parent = null;
                    node = null; // clear me
                    if (retVal2 != null) {
                        vn = new VisualNode3(ref retVal2, src, 10000);
                        return retVal2;
                    }
                    return retVal?.parent ?? retVal;
                case 7: // <-> a -> me => <-> a -> null | Parent / Pred, Node, -
                    if (node != removed) return this;
                    retVal = Pred;
                    if (!Pred.ReplaceNext(null)) return null; // Link Pred -> Next(=null)
                    node = null;
                    return retVal;
                case 9: // Parent / - , - , Next
                    retVal = parent;
                    parent.node = next;
                    next.pred = null;
                    next = null;
                    retVal = parent.RemoveEmpties(parent.node, src);
                    return retVal;
                case 11: // Parent / Pred , - , Next
                    retVal = next;
                    pred.next = next;
                    next.pred = pred;
                    dv?.update(this);
                    next = null;
                    pred = null;
                    retVal2 = parent;
                    parent = null;
                    dv?.update(retVal, -11);
                    //retVal2.RemoveEmpties(this, src);
                    return retVal;
                case 13: // me -> a                     | Parent / - , Node, Next
                    if (node != removed) return this;
                    retVal = DeleteNode2next(ref dv);
                    vn = new VisualNode3(ref retVal, src, 10000);
                    return retVal;
                case 15: // a -> me -> b => a -> b      | Parent, Pred, Node, Next
                    pred.next = next;
                    next.pred = pred;
                    retVal = parent;
                    pred = null;
                    node = null; // clear me
                    next = null;
                    retVal2 = retVal.RemoveEmpties(this, src); // next @next
                    parent = null;
                    return retVal2;
                case 0:
                    return null; // ok nothing around
                default:
                    Console.WriteLine($"RemoveEmpties buggy node ({arround}) ?!");
                    break;
            }
            return null;
        }
    }
}
