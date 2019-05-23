using System;
using System.Collections.Generic;
using System.Text;

namespace Gason
{
    public class Parser
    {
        readonly bool FloatAsDecimal = false;
        readonly int JSON_STACK_SIZE;
#if DEBUGGING
        public VisualNode3[] tails;
        public LinkedByteString[] keys;
#else
        public JsonNode[] tails;
        public P_ByteLnk[] keys;
#endif
        public JsonTag[] tags;
        JsonNode o;
#if DEBUGGING
        public VisualNode3 root;
#endif
        public int pos = -1;
        public bool separator = true;
        public Byte prevType = 255;
        public int len;
        public int strPos;
        Byte type;
#if KEY_SPLIT
        Boolean insideLimitBlock, bubbleOut;
#endif
        public void Init(ref Byte[] s, bool nextInit)
        {
            o = new JsonNode();
#if DEBUGGING
            tails = new VisualNode3[JSON_STACK_SIZE];
            LinkedByteString.storage = s;
            keys = new LinkedByteString[JSON_STACK_SIZE];
            root = new VisualNode3(ref o, s, 3000); // Predefined JSON preview size limit (interactive then, also indent 0/-1 or m_Shift_Width)
#else
        tails = new JsonNode[JSON_STACK_SIZE];
        keys = new P_ByteLnk[JSON_STACK_SIZE];
#endif
            tags = new JsonTag[JSON_STACK_SIZE];
            pos = -1;
#if !SKIP_VALIDATION
            separator = true;
            prevType = 255;
#endif
            type = 255;
            len = s.Length;
            strPos = 0;
#if KEY_SPLIT
            insideLimitBlock = false;
            bubbleOut = false;
#endif
        }
        public Parser(bool FloatAsDecimal, int JSON_STACK_SIZE = 32)
        {
            this.FloatAsDecimal = FloatAsDecimal;
            this.JSON_STACK_SIZE = JSON_STACK_SIZE;
        }
        public JsonErrno Parse(Byte[] s, ref int endPos, out JsonNode value
#if KEY_SPLIT
            , ByteString[] keysLog, int level, int startPos, int length
#endif
            )
        {
            int endPosMem = endPos;
            Init(ref s, (endPos > 0));
            value = null;
            while (strPos < len)
            {
#if !SKIP_VALIDATION
                if(type > 1) prevType = type;
#endif
                type = SearchTables.valTypes[s[strPos]];
                if (type <= 1)
                {
                    strPos++;
                    continue; // white space
                }
                endPos = strPos++;
                switch (type) // switch (**endptr) {
                {
                    case 2: // case '-':
                        if(FloatAsDecimal) {
                            endPos = o.String2decimal(ref strPos, s, true);
                        } else {
                            endPos = o.String2double(ref strPos, s, true);
                        }
#if !SKIP_VALIDATION
                        if (0 == (SearchTables.specialTypes[s[endPos]] & 3)) // isdelim
                        {
                            endPos = strPos;
                            return JsonErrno.BAD_NUMBER;
                        }
#endif
                        break;
                    case 4: // 0-9
                        strPos--;
                        if(FloatAsDecimal) {
                            endPos = o.String2decimal(ref strPos, s);
                        } else {
                            endPos = o.String2double(ref strPos, s);
                        }
#if !SKIP_VALIDATION
                        if (0 == (SearchTables.specialTypes[s[endPos]] & 3)) // isdelim
                        {
                            endPos = strPos;
                            return JsonErrno.BAD_NUMBER;
                        }
#endif
                        break;
                    case 3: // case '"':
                        JsonErrno e = o.GetString(ref strPos, s); // new ByteString(s, o.doubleOrString).ToString()
                        if (e != JsonErrno.OK) return e;
#if !SKIP_VALIDATION
                        if (0 == (SearchTables.specialTypes[s[strPos]] & 3)) // !isdelim
                        {
                            endPos = strPos;
                            return JsonErrno.BAD_STRING;
                        }
#endif
                        break;
                    case 7: // 't'
                        if ((SearchTables.specialTypes[s[strPos + 3]] & 3) != 0 // isdelim
                        && (s[strPos + 0] == 'r')
                        && (s[strPos + 1] == 'u')
                        && (s[strPos + 2] == 'e'))
                        {
                            o.Tag = JsonTag.JSON_TRUE;
                            strPos += 3;
#if !SKIP_VALIDATION
                        }
                        else {
                            return JsonErrno.BAD_IDENTIFIER;
#endif
                        }
                        break;
                    case 6: // 'f'
                        if ((SearchTables.specialTypes[s[strPos + 4]] & 3) != 0 // isdelim
                        && (s[strPos + 0] == 'a')
                        && (s[strPos + 1] == 'l')
                        && (s[strPos + 2] == 's')
                        && (s[strPos + 3] == 'e'))
                        {
                            o.Tag = JsonTag.JSON_FALSE;
                            strPos += 4;
#if !SKIP_VALIDATION
                        }
                        else {
                            return JsonErrno.BAD_IDENTIFIER;
#endif
                        }
                        break;
                    case 8: // 'n'
#if !SKIP_VALIDATION
                        if (prevType == 3 && !separator) { // {"Missing colon" null} + fail19.json
                            return JsonErrno.UNEXPECTED_CHARACTER;
                        }
#endif
                        if ((SearchTables.specialTypes[s[strPos + 3]] & 3) != 0 // isdelim
                        && (s[strPos + 0] == 'u')
                        && (s[strPos + 1] == 'l')
                        && (s[strPos + 2] == 'l'))
                        {
                            o.Tag = JsonTag.JSON_NULL;
                            strPos += 3;
#if !SKIP_VALIDATION
                        }
                        else {
                            return JsonErrno.BAD_IDENTIFIER;
#endif
                        }
                        break;
                    case 12: // ']'
#if !SKIP_VALIDATION
                        if (pos == -1)
                            return JsonErrno.STACK_UNDERFLOW;
                        if (tags[pos] != JsonTag.JSON_ARRAY)
                            return JsonErrno.MISMATCH_BRACKET;
                        if (separator && prevType != 11) // '['
                            return JsonErrno.UNEXPECTED_CHARACTER; // fail4
#endif
#if DEBUGGING
                        o.ListToValue(JsonTag.JSON_ARRAY, tails[pos]?.NodeRawData);
                        pos--;
#else
                        o.ListToValue(JsonTag.JSON_ARRAY, tails[pos--]);
#endif
#if !SKIP_VALIDATION
                        if (type > 1) prevType = type;
#endif
                        break;
                    case 13: // '}'
#if !SKIP_VALIDATION
                        if (pos == -1)
                            return JsonErrno.STACK_UNDERFLOW;
                        if (tags[pos] != JsonTag.JSON_OBJECT)
                            return JsonErrno.MISMATCH_BRACKET;
                        if (keys[pos].length != -1)
                            return JsonErrno.UNEXPECTED_CHARACTER;
                        if (separator && prevType != 10) // '{'
                            return JsonErrno.UNEXPECTED_CHARACTER;
#endif
#if DEBUGGING
                        o.ListToValue(JsonTag.JSON_OBJECT, tails[pos]?.NodeRawData);
                        pos--;
#else
                        o.ListToValue(JsonTag.JSON_OBJECT, tails[pos--]);
#endif
#if KEY_SPLIT
                        if (insideLimitBlock && (level == pos + 1))
                        {
                            if (length == 1) {
                                bubbleOut = true;
                            } else if (length > 1){
                                length--;
                            }
                        }
#endif
                        break;
                    case 11: // '['
#if !SKIP_VALIDATION
                        if (++pos == JSON_STACK_SIZE)
                            return JsonErrno.STACK_OVERFLOW;
#else
                        pos++;
#endif
                        tails[pos] = null;
                        tags[pos] = JsonTag.JSON_ARRAY;
                        keys[pos].length = -1;
#if !SKIP_VALIDATION
                        separator = true;
#endif
                        continue;
                    case 10: // '{'
#if !SKIP_VALIDATION
                        if (++pos == JSON_STACK_SIZE)
                            return JsonErrno.STACK_OVERFLOW;
#else
                        pos++;
#endif
#if KEY_SPLIT
                        if (pos == level && !insideLimitBlock)
                        {
                            int i = level - 1;
                            while (i >= 0)
                            {
                                if (keys[i].length != -1)
                                {
#if DEBUGGING
                                    if (keysLog[i].Equals(s, keys[i].idxes)) i--;
#else
                                    if (keysLog[i].Equals(s, keys[i])) i--;
#endif
                                    else break;
                                } else if (keysLog[i] == null) i--; else break;
                            }
                            if(i == -1)
                            { // Keys & level match
                                insideLimitBlock = true;
                                if (startPos > 0) strPos = endPosMem;
                            }
                        }
#endif
                        tails[pos] = null;
                        tags[pos] = JsonTag.JSON_OBJECT;
                        keys[pos].length = -1;
#if !SKIP_VALIDATION
                        separator = true;
#endif
                        continue;
                    case 14: // ':'
#if !SKIP_VALIDATION
                        if (separator || keys[pos].length == -1)
                            return JsonErrno.UNEXPECTED_CHARACTER;
                        separator = true;
#endif
                        continue;
                    case 15: // ','
#if !SKIP_VALIDATION
                        if (separator || keys[pos].length != -1)
                            return JsonErrno.UNEXPECTED_CHARACTER;
                        separator = true;
#endif
                        continue;
#if !SKIP_VALIDATION
                    default:
                        return JsonErrno.UNEXPECTED_CHARACTER;
#endif
                }
#if !SKIP_VALIDATION
                separator = false;
#endif

                if (pos == -1)
                {
                    value = o;
#if !SKIP_VALIDATION
                    while (strPos < len - 1)
                    {
                        if (type == 13) { // '}' / {"Extra value after close": true} "misplaced quoted value"
                            type = 0;
                            while (strPos < len - 1) {
                                type = SearchTables.valTypes[s[strPos]];
                                if (type <= 1) {
                                    strPos++;
                                    continue;
                                }
                                if (type == 3)
                                {
                                    strPos++;
                                    JsonNode trash = new JsonNode();
                                    JsonErrno e = trash.GetString(ref strPos, s);
                                    if (e != JsonErrno.OK) return e;
                                    endPos = strPos;
                                    if (strPos != len -1 || 0 == (SearchTables.specialTypes[s[strPos]] & 3)) // !isdelim
                                    {
                                        return JsonErrno.BAD_STRING;
                                    } else return JsonErrno.OK;
                                }
                                else return JsonErrno.UNEXPECTED_CHARACTER;
                            }
                        }
                        if ((strPos < len -1) && SearchTables.valTypes[s[strPos]] <= 1) strPos++;
                        else
                        {
                            if (s[strPos] == 0) break;
                            if (prevType != 12 || s[strPos] != ',') // ']'
                                return JsonErrno.BREAKING_BAD;
                            else strPos++;
                        }
                    }
#endif
                    return JsonErrno.OK;
                }
#if KEY_SPLIT
                do
                {
#endif
                if (tags[pos] == JsonTag.JSON_OBJECT)
                    {
                        if (keys[pos].length == -1)
                        {
#if !SKIP_VALIDATION
                            if (o.Tag != JsonTag.JSON_STRING)
                                return JsonErrno.UNQUOTED_KEY;
#endif
#if DEBUGGING
                            keys[pos] = new LinkedByteString(o.doubleOrString);
#else
                            keys[pos].data = o.doubleOrString.data;
#endif
#if KEY_SPLIT
                            if (bubbleOut) continue;
                            else break;
#else
                            continue;
#endif
                    }
#if DEBUGGING
                    o.InsertAfter(tails[pos]?.NodeRawData, ref keys[pos].idxes);
#else
                    o.InsertAfter(tails[pos] != null ? tails[pos] : null, ref keys[pos]);
#endif
                    }
                    else
                    {
#if DEBUGGING
                        o.InsertAfter(tails[pos]?.NodeRawData);
#else
                        o.InsertAfter(tails[pos]);
#endif
                    }
                    tails[pos] =
#if DEBUGGING
                        new VisualNode3(ref o, s, 3000);
#else
                        o;
#endif
                    o = new JsonNode();
#if DEBUGGING
                    root.ChangeNode(o);
#endif

#if KEY_SPLIT
                    if (bubbleOut)
                    {
                        if (tags[pos] == JsonTag.JSON_ARRAY
                        || tags[pos] == JsonTag.JSON_OBJECT)
                        { // lists close brackets
#if DEBUGGING
                            o.ListToValue(tags[pos], tails[pos]?.NodeRawData);
#else
                            o.ListToValue(tags[pos], tails[pos]);
#endif
                        }
                        if (pos-- == 0)
                        {
                            while ((strPos < len) && s[strPos++] != ',') ; // find array separator
                            while ((strPos < len) && ((SearchTables.specialTypes[s[strPos]] & 3) != 0)) strPos++; // skip delims
                            while ((strPos < len) && (s[strPos] != '{')) strPos++; // array start
                            if (strPos < len) strPos++;
                            endPos = strPos;
                            value = o;
                            return JsonErrno.OK;
                        }
                    }
                    else break;
                } while (true); // exit by breaks
#endif
            }
            return JsonErrno.BREAKING_BAD;
        }
        public void RemoveTwins(ref BrowseNode v1, ref BrowseNode v2)
        {
            BreadthFirst bf1 = new BreadthFirst(v1);
            BreadthFirst bf2 = new BreadthFirst(v2);
            BrowseNode traversal = bf1.Root;
            bf2.NextAs(bf1);
            Boolean removed = true;
            //int removeNo = 0;
            do
            {
                if (!removed)
                {
                    if (bf2.Current != null) while (bf1.Level == bf2.Level && bf2.Next() != null) ;
                    if (bf2.Current != null) while (bf1.Level != bf2.Level && bf2.Next() != null) ;
                    else bf2.Current = bf2.Root;
                }
                else traversal = bf1.Next();
                if (bf2.Level < 0) bf2.Current = bf2.Root;
                do
                {
                    removed = bf1.NextAs(bf2);
                    if (removed && bf1.Orphan && bf2.Orphan)
                    {
                        //removeNo++;
                        traversal = bf1.RemoveCurrent();
                        bf2.RemoveCurrent();
                        if (traversal == null || traversal.NodeRawData == null
                        || bf1.Root.NodeRawData == null
                        || bf2.Root.NodeRawData == null)
                        {
                            traversal = null;
                            break;
                        }
                    }
                    else removed = false;
                } while (removed);
                if (!removed) traversal = bf1.Next();
            } while (traversal != null);
        }
        public class NodeComparer : IComparable<NodeComparer>
        {
            public BrowseNode element;
            public String Key { get { return element.KeyPrint; } }
            public String Value { get { return element.Value_Viewer; } }
            Byte[] src;
            public NodeComparer(BrowseNode el, Byte[] s) {
                element = el;
                src = s;
            }
            public int CompareTo(NodeComparer other)
            {
                if ((element.NodeRawData.KeyIndexesData.data == other.element.NodeRawData.KeyIndexesData.data)
                 && (element.NodeRawData.doubleOrString.data == other.element.NodeRawData.doubleOrString.data))
                    return 0;
                if ((String.Compare(Key, other.Key) < 0)
                 || (element.NodeRawData.KeyIndexesData.data == other.element.NodeRawData.KeyIndexesData.data
                      && (String.Compare(Value, other.Value) < 0)))
                {
                    return -1;
                }

                return 1;
            }
        }
        public class NodeComparer2 : IComparable<NodeComparer2>
        {
            public String Key { get; set; }
            public List<NodeComparer> elements;
            public int Idx { get; private set; }
            public int Level { get; set; }
            public static int row = 0;
            public NodeComparer2() {
                Idx = row++;
            }
            public BrowseNode GetLast { get { return elements[elements.Count - 1].element; } }
            public override string ToString()
            {
                return Key;
            }
            public int CompareTo(NodeComparer2 other)
            {
                if (Key == other.Key) return 0;
                if (String.Compare(Key, other.Key) < 0)
                {
                    return -1;
                }

                return 1;
            }
        }
        internal void SortPaths(JsonNode root, Byte[] JSONdata, String id)
        {
            Printer p = new Printer();

            Stack<BrowseNode> s = new Stack<BrowseNode>();
            s.Push(new BrowseNode(ref root, JSONdata));
            List<NodeComparer2> rows = new List<NodeComparer2>();
            HashSet<JsonNode> processed = new HashSet<JsonNode>();
            BrowseNode element;
            while (s.Count > 0)
            {
                element = s.Pop();
                if (null != element.NodeRawData.NodeBelow)
                {
                    if (null != element.NodeRawData.NextTo) s.Push(element.Next_Viewer);
                    s.Push(element.Node_Viewer);
                }
                else if (null != element.NodeRawData.NextTo)
                {
                    if (processed.Contains(element.Parent_Viewer.NodeRawData)) continue;
                    else processed.Add(element.Parent_Viewer.NodeRawData);
                    BrowseNode keyNode = element;
                    ParentNode pn = new ParentNode(element);
                    List<NodeComparer> lastChildrens = new List<NodeComparer>();
                    JsonNode following = null;
                    String key = "";
                    while (null != keyNode)
                    {
                        if (JsonTag.JSON_ARRAY == keyNode.Tag_Viewer || JsonTag.JSON_OBJECT == keyNode.Tag_Viewer) s.Push(keyNode);
                        if (id != null && id == keyNode.KeyPrint) {
                            key = $"\\{keyNode.Value_Viewer}";
                        } else {
                            lastChildrens.Add(new NodeComparer(keyNode, JSONdata));
                        }
                        following = keyNode.NodeRawData;
                        keyNode = keyNode.Next_Viewer;
                    }
                    lastChildrens.Sort();
                    pn.SetChild(lastChildrens[0].element);
                    int count = lastChildrens.Count;
                    lastChildrens[count - 1].element.NodeRawData.SetNextTo(null);
                    int level = lastChildrens[count - 1].element.Level_Viewer;
                    for (int i = 1; i < count; i++)
                    {
                        lastChildrens[i - 1].element.NodeRawData.NextTo = lastChildrens[i].element.NodeRawData;
                    }
                    for (int i = 0; i < lastChildrens.Count; i++)
                    {
                        if (lastChildrens[i].element.Tag_Viewer == JsonTag.JSON_ARRAY
                        || lastChildrens[i].element.Tag_Viewer == JsonTag.JSON_OBJECT) lastChildrens.Remove(lastChildrens[i--]);
                    }
                    key = (element.Parent_Viewer ?? element).Path(true) + key;
                    foreach (var item in lastChildrens)
                    {
                        key += $"\t{item.element.KeyPrint}\t'{item.element.Value_Viewer}";
                    }
                    rows.Add(new NodeComparer2() { Key = key, Level = level, elements = lastChildrens });
                }
            }
            rows.Sort();
            int rowsCount = rows.Count;
            BrowseNode pred = new BrowseNode(ref root, JSONdata), current = null;
            VisualNode3 check = new VisualNode3(ref root, JSONdata, 10000);
            for (int i = 0; i < rowsCount; i++)
            {
                current = rows[i].elements[0].element;
                if (current.Level_Viewer > pred.Level_Viewer) {
                    FixArraysOrder(current);
                }
                if (current.Level_Viewer >= pred.Level_Viewer) {
                    current = GetConnectionPath(pred, current);
                    while (pred.Level_Viewer > current.Level_Viewer) pred = pred.Parent_Viewer;
                    if (pred.Level_Viewer == 0) {
                        pred.NodeRawData = current.NodeRawData;
                    } else if (pred.NodeRawData.NodeBelow == current.NodeRawData) {
                        pred.NodeRawData.NodeBelow = current.NodeRawData;
                    } else {
                        pred.NodeRawData.NextTo = current.NodeRawData;
                        current.NodeRawData.NextTo = null;
                    }
                }
                pred = rows[i].GetLast;
            }
            current.NodeRawData.NextTo = null;
            String pp = p.Print(ref root, JSONdata, 0).ToString();
        }
        public static BrowseNode GetCollection(BrowseNode from, int level = 1) {
            while (from.Level_Viewer > level
                || (from.NodeRawData.Tag != JsonTag.JSON_ARRAY
                 && from.NodeRawData.Tag != JsonTag.JSON_OBJECT)) from = from.Parent_Viewer;
            return from;
        }
        public static BrowseNode GetConnectionPath(BrowseNode pred, BrowseNode current)
        {
            int relation = pred.Level_Viewer - current.Level_Viewer;
            if (relation < 0) {
            } else ;
            int topLevel = pred.Level_Viewer;
            if(current.Level_Viewer == topLevel) {
                while(pred.Parent_Viewer != current.Parent_Viewer) {
                    pred = pred.Parent_Viewer;
                    current = current.Parent_Viewer;
                }
                return current;
            } else while(current.Level_Viewer > topLevel) {
                if (pred.NodeRawData.NodeBelow == current.Parent_Viewer.NodeRawData) return current.Parent_Viewer;
                current = current.Parent_Viewer;
            }
            return current;
        }
        public static void FixArraysOrder(BrowseNode me)
        {
            BrowseNode underMe = null;
            while (me != null) {
                if (me.NodeRawData.Tag == JsonTag.JSON_OBJECT || me.NodeRawData.Tag == JsonTag.JSON_ARRAY)
                {
                    if (underMe != null && me.NodeRawData.NodeBelow != underMe.NodeRawData) {
                        me.NodeRawData.NodeBelow = underMe.NodeRawData;
                        return;
                    }
                }
                underMe = me;
                me = me.Parent_Viewer;
            }
        }
        public class ParentNode
        {
            readonly BrowseNode parentNode;
            readonly Boolean first;
            public BrowseNode Parent { get { return parentNode; } }
            public ParentNode(BrowseNode me)
            {
                first = me.Parent_Viewer.NodeRawData.NodeBelow == me.NodeRawData;
                if (first) parentNode = me.Parent_Viewer;
                else parentNode = me.Pred_Viewer;
            }
            public void SetChild(BrowseNode me)
            {
                if (first) parentNode.NodeRawData.NodeBelow = me.NodeRawData;
                else parentNode.NodeRawData.NextTo = me.NodeRawData;
            }
        }
    }
}
