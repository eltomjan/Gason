using Gason;
using PSON.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PSON
{
	/// <summary>
	/// A high-level PSON decoder that maintains a dictionary.
	/// </summary>
	public class PsonDecoder : IDisposable
	{
		public static object Decode(byte[] buffer, out JsonNode root, out String stringify, IList<string> initialDictionary = null, PsonOptions options = PsonOptions.None, int allocationLimit = -1)
		{
			var input = new MemoryStream(buffer);
			using (var decoder = new PsonDecoder(input, initialDictionary, options, allocationLimit))
            {
                Object retVal = decoder.Read(out root);
                stringify = decoder.jsonTxt.ToString();
                return retVal;
            }
        }

		private Stream input;
        private StringBuilder jsonTxt;
        private JsonNode o;

        private List<string> dictionary;

		private PsonOptions options;

		private int allocationLimit;

		private readonly byte[] convertArray = new byte[8];

		public PsonDecoder(Stream input, IList<string> initialDictionary = null, PsonOptions options = PsonOptions.None, int allocationLimit = -1)
		{
            if (ReferenceEquals(input, null))
				throw new ArgumentNullException("input");
            jsonTxt = new StringBuilder();
            this.input = input;
			this.options = options;
			this.allocationLimit = allocationLimit;
			if (initialDictionary == null)
				dictionary = null;
			else
                dictionary = new List<string>(initialDictionary);
		}

		public object Read(out JsonNode root)
		{
            root = null;
            checkDisposed();
            o = new JsonNode { Tag = JsonTag.JSON_OBJECT };
            Object retVal = decodeValue();
            root = o;
            return retVal;
		}

		private object decodeValue()
		{
			var token = (byte)input.ReadByte();
            Object retVal;
            String value;
            if (token <= Token.MAX)
				return token;
            JsonNode root = o;
#if DEBUGGING
            while (root?.Parent?.Parent != null) root = root.Parent;
            VisualNode3 oV = new VisualNode3(ref root, Encoding.UTF8.GetBytes(jsonTxt.ToString()), 10000);
#endif
            switch (token)
			{
				case Token.NULL:
                    jsonTxt.Append("null");
                    o.Tag = JsonTag.JSON_NULL;
					return null;

				case Token.TRUE:
                    jsonTxt.Append("true");
                    o.Tag = JsonTag.JSON_TRUE;
                    return true;

				case Token.FALSE:
                    jsonTxt.Append("false");
                    o.Tag = JsonTag.JSON_FALSE;
                    return false;

				case Token.EOBJECT:
                    o.Tag = JsonTag.JSON_OBJECT;
					return new Dictionary<string, object>();

				case Token.EARRAY:
                    o.Tag = JsonTag.JSON_ARRAY;
					return new List<object>();

				case Token.ESTRING:
                    o.Tag = JsonTag.JSON_STRING;
					return string.Empty;

				case Token.OBJECT:
                    jsonTxt.Append('{');
                    retVal = decodeObject();
                    jsonTxt.Append('}');
                    return retVal;

				case Token.ARRAY:
                    jsonTxt.Append('[');
                    retVal = decodeArray();
                    jsonTxt.Append(']');
                    return retVal;

                case Token.INTEGER:
                    retVal = input.ReadVarint32().ZigZagDecode();
                    jsonTxt.Append(retVal);
                    o.Tag = JsonTag.JSON_NUMBER;
                    o.doubleOrString.number = (double)retVal;
                    return retVal;

                case Token.LONG:
                    retVal = input.ReadVarint64().ZigZagDecode();
                    jsonTxt.Append(retVal);
                    o.Tag = JsonTag.JSON_NUMBER;
                    o.doubleOrString.number = (double)retVal;
                    return retVal;

                case Token.FLOAT:
					if (input.Read(convertArray, 0, 4) != 4)
						throw new PsonException("stream ended prematurely");
					if (!BitConverter.IsLittleEndian)
						Array.Reverse(convertArray, 0, 4);
                    float retF = BitConverter.ToSingle(convertArray, 0);
                    jsonTxt.Append(retF.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    o.Tag = JsonTag.JSON_NUMBER;
                    o.doubleOrString.number = (double)retF;
                    return retF;

				case Token.DOUBLE:
					if (input.Read(convertArray, 0, 8) != 8)
						throw new PsonException("stream ended prematurely");
					if (!BitConverter.IsLittleEndian)
						Array.Reverse(convertArray, 0, 8);
                    double retD = BitConverter.ToDouble(convertArray, 0);
                    o.Tag = JsonTag.JSON_NUMBER;
                    o.doubleOrString.number = retD;
                    jsonTxt.Append(retD.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return retD;

                case Token.STRING_ADD:
				case Token.STRING:
                    value = decodeString(token, false);
                    o.Tag = JsonTag.JSON_STRING;
                    o.doubleOrString.pos = jsonTxt.Length + 1;
                    o.doubleOrString.length = value.Length;
                    jsonTxt.Append('"').Append(value).Append('"');
                    return value;

				case Token.STRING_GET:
                    value = getString(input.ReadVarint32());
                    o.Tag = JsonTag.JSON_STRING;
                    o.doubleOrString.pos = jsonTxt.Length + 1;
                    o.doubleOrString.length = value.Length;
                    jsonTxt.Append('"').Append(value).Append('"');
                    return value;

                case Token.BINARY:
					return decodeBinary();

				default:
					throw new PsonException("illegal token: 0x" + token.ToString("x2")); // should never happen
			}
		}

		private IList<object> decodeArray()
		{
            o.Tag = JsonTag.JSON_ARRAY;
			var count = input.ReadVarint32();
			if (allocationLimit > -1 && count > allocationLimit)
				throw new PsonException("allocation limit exceeded:" + count);
			var list = new List<object>(checked((int)count));
            JsonNode aPos = o, aRet = o;
            bool first1 = true;
			do {
				list.Add(decodeValue());
                if (first1) {
                    first1 = false;
                    o = aPos.NodeBelow;
                    while (o.NextTo != null) o = o.NextTo;
                }
                if (count-- > 1) {
                    jsonTxt.Append(',');
                    o = o.CreateNext();
                    aPos = o;
                } else break;
            } while (true);
            o = aRet;
            return list;
        }

		private Dictionary<string,object> decodeObject()
		{
			var count = input.ReadVarint32();
			if (allocationLimit > -1 && count > allocationLimit)
				throw new PsonException("allocation limit exceeded: " + count);
            if(o.Tag != JsonTag.JSON_NULL) o = o.CreateNode();
            o.Tag = JsonTag.JSON_OBJECT;
            JsonNode thisObj = o;
            if (count > 0) o = o.CreateNode();
			var obj = new Dictionary<string, object>(checked((int)count));
            do
            {
                var strToken = (byte)input.ReadByte();
                String key;
                switch (strToken)
                {
                    case Token.STRING_ADD:
                    case Token.STRING:
                        key = decodeString(strToken, true);
                        jsonTxt.Append('"').Append(key).Append("\":");
                        obj[key] = decodeValue();
                        break;

                    case Token.STRING_GET:
                        key = getString(input.ReadVarint32());
                        o.SetKey(jsonTxt.Length + 1, key.Length);
                        jsonTxt.Append('"').Append(key).Append("\":");
                        obj[key] = decodeValue();
                        break;

                    default:
                        throw new PsonException("string token expected");
                }
                if (count-- > 1)
                {
                    jsonTxt.Append(',');
                    o = o.CreateNext();
                }
                else break;
            } while (true);
            o = thisObj;
            return obj;
		}

		private string decodeString(byte token, bool isKey)
		{
            var count = checked((int)input.ReadVarint32());
			if (allocationLimit > -1 && count > allocationLimit)
				throw new PsonException("allocation limit exceeded: " + count);
			var buffer = new byte[count];
			if (input.Read(buffer, 0, count) != count)
				throw new PsonException("stream ended prematurely");
			var value = Encoding.UTF8.GetString(buffer);
			if (token == Token.STRING_ADD)
			{
				if (isKey)
				{
					if ((options & PsonOptions.ProgressiveKeys) == 0)
						throw new PsonException("illegal progressive key");
				}
				else
				{
					if ((options & PsonOptions.ProgressiveValues) == 0)
						throw new PsonException("illegal progressive value");
				}
				dictionary.Add(value);
			}
			return value;
		}

		private string getString(uint index)
		{
            if (index >= dictionary.Count)
				throw new PsonException("dictionary index out of bounds: " + index);
            return dictionary[checked((int)index)];
		}

		private byte[] decodeBinary()
		{
			var count = (int)input.ReadVarint32();
			if (allocationLimit > -1 && count > allocationLimit)
				throw new PsonException("allocation limit exceeded: " + count);
			var bytes = new byte[count];
			if (input.Read(bytes, 0, count) != count)
				throw new PsonException("stream ended prematurely");
			return bytes;
		}

		#region IDisposable Support

		private bool disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					input.Dispose();
					input = null;
					dictionary = null;
				}
				disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}

		private void checkDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException(GetType().Name);
		}

		#endregion
	}
}
