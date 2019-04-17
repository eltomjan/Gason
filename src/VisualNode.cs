using System;
using System.Text;

namespace Gason
{
    public class VisualNode
    {
        public JsonNode myNode;
        Byte[] src;
        public VisualNode(JsonNode my, Byte[] src)
        {
            myNode = my;
            this.src = src;
        }
        public VisualNode Next
        {
            get {
                if (myNode != null && myNode.next != null)
                {
                    return new VisualNode(myNode.next, src);
                }
                return null;
            }
        }
        public VisualNode Node
        {
            get
            {
                if (myNode != null && myNode.ToNode() != null)
                {
                    return new VisualNode(myNode.ToNode(), src);
                }
                else return null;
            }
        }
        public void ChangeNode(JsonNode o)
        {
            myNode = o;
        }
        public override string ToString()
        {
            String key = myNode.Key2str(src);
            if (myNode.Tag == JsonTag.JSON_NUMBER) {
                return key + myNode.ToNumber().ToString(System.Globalization.CultureInfo.InvariantCulture);
            } else if(myNode.Tag == JsonTag.JSON_NUMBER_STR) {
                return key + myNode.ToString(src);
            } else if (myNode.Tag == JsonTag.JSON_ARRAY) {
                return key + "Arr [";
            } else if (myNode.Tag == JsonTag.JSON_OBJECT) {
                return key + "Obj {";
            } else if (myNode.Tag == JsonTag.JSON_STRING) {
                return key + Encoding.UTF8.GetString(src, myNode.doubleOrString.pos, myNode.doubleOrString.length);
            } else if (myNode.Tag == JsonTag.JSON_TRUE) {
                return key + "true";
            } else if (myNode.Tag == JsonTag.JSON_FALSE) {
                return key + "false";
            } else if (myNode.Tag == JsonTag.JSON_NULL) {
                return key + "null";
            } else return key + "N/A";
        }
    }
}
