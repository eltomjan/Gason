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
		public static object Decode(byte[] buffer, out String stringify, IList<string> initialDictionary = null, PsonOptions options = PsonOptions.None, int allocationLimit = -1)
		{
			var input = new MemoryStream(buffer);
			using (var decoder = new PsonDecoder(input, initialDictionary, options, allocationLimit))
            {
                Object retVal = decoder.Read();
                stringify = decoder.jsonTxt.ToString();
                return retVal;
            }
        }

		private Stream input;
        private StringBuilder jsonTxt;

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

		public object Read()
		{
			checkDisposed();
			return decodeValue();
		}

		private object decodeValue()
		{ 
			var token = (byte)input.ReadByte();
            Object retVal;
            String value;
            if (token <= Token.MAX)
				return token;
			switch (token)
			{
				case Token.NULL:
                    jsonTxt.Append("null");
					return null;

				case Token.TRUE:
                    jsonTxt.Append("true");
                    return true;

				case Token.FALSE:
                    jsonTxt.Append("false");
                    return false;

				case Token.EOBJECT:
					return new Dictionary<string, object>();

				case Token.EARRAY:
					return new List<object>();

				case Token.ESTRING:
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
                    return retVal;

                case Token.LONG:
                    retVal = input.ReadVarint64().ZigZagDecode();
                    jsonTxt.Append(retVal);
                    return retVal;

                case Token.FLOAT:
					if (input.Read(convertArray, 0, 4) != 4)
						throw new PsonException("stream ended prematurely");
					if (!BitConverter.IsLittleEndian)
						Array.Reverse(convertArray, 0, 4);
                    float retF = BitConverter.ToSingle(convertArray, 0);
                    jsonTxt.Append(retF.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return retF;

				case Token.DOUBLE:
					if (input.Read(convertArray, 0, 8) != 8)
						throw new PsonException("stream ended prematurely");
					if (!BitConverter.IsLittleEndian)
						Array.Reverse(convertArray, 0, 8);
                    double retD = BitConverter.ToDouble(convertArray, 0);
                    jsonTxt.Append(retD.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return retD;

                case Token.STRING_ADD:
				case Token.STRING:
                    value = decodeString(token, false);
                    jsonTxt.Append('"').Append(value).Append('"');
                    return value;

				case Token.STRING_GET:
                    value = getString(input.ReadVarint32());
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
			var count = input.ReadVarint32();
			if (allocationLimit > -1 && count > allocationLimit)
				throw new PsonException("allocation limit exceeded:" + count);
			var list = new List<object>(checked((int)count));
			while (count-- > 0) {
				list.Add(decodeValue());
                if (count > 0) jsonTxt.Append(',');
            }
			return list;
		}

		private Dictionary<string,object> decodeObject()
		{
			var count = input.ReadVarint32();
			if (allocationLimit > -1 && count > allocationLimit)
				throw new PsonException("allocation limit exceeded: " + count);
			var obj = new Dictionary<string, object>(checked((int)count));
			while (count-- > 0)
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
                        jsonTxt.Append('"').Append(key).Append("\":");
                        obj[key] = decodeValue();
						break;

					default:
						throw new PsonException("string token expected");
				}
                if (count > 0) jsonTxt.Append(',');
			}
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
