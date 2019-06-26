using System;
using System.Collections.Generic;
using System.IO;
using Gason;

namespace PSON
{
	/// <summary>
	/// A high-level PSON encoder that maintains a dictionary.
	/// </summary>
	public class PsonEncoder : PsonWriter
    {
		#region Public static methods

		public static byte[] Encode(BrowseNode structure, IList<string> initialDictionary = null, PsonOptions options = PsonOptions.None)
		{
			var output = new MemoryStream();
			using (var encoder = new PsonEncoder(output, initialDictionary, options))
			{
				encoder.Write(structure);
				return output.ToArray();
			}
		}

		#endregion

		#region Non-public properties

		private PsonOptions options;

		private Dictionary<string, uint> dictionary;

		#endregion

		#region Public constructors

		public PsonEncoder(Stream output, IList<string> initialDictionary = null, PsonOptions options = PsonOptions.None) : base(output)
		{
			this.options = options;
			if (initialDictionary == null)
				dictionary = null;
			else
			{
				dictionary = new Dictionary<string, uint>(initialDictionary.Count);
				uint index = 0;
				foreach (var key in initialDictionary)
					dictionary[key] = index++;
			}
		}

		#endregion

		#region Public methods

		public void Write(BrowseNode obj)
		{
            if (obj.Tag_Viewer == JsonTag.JSON_NULL)
                WriteNull();

            else if (obj.Tag_Viewer == JsonTag.JSON_STRING)
                writeString(obj.Value_Viewer, false);

            else if (obj.Tag_Viewer == JsonTag.JSON_NUMBER_STR)
            {
                WriteDouble(Double.Parse(obj.Value_Viewer.Replace('.', ',')));
            } else if (obj.Tag_Viewer == JsonTag.JSON_NUMBER)
                WriteDouble(obj.NodeRawData.ToNumber());

            else if (obj.Tag_Viewer >= JsonTag.JSON_TRUE) // true, false, null
                WriteBool(obj.Tag_Viewer == JsonTag.JSON_TRUE);

            else if (obj.Tag_Viewer == JsonTag.JSON_ARRAY)
                WriteArray(obj);

            else if (obj.Tag_Viewer == JsonTag.JSON_OBJECT)
                WriteObject(obj);

            else
                throw new ArgumentException("unsupported type: " + obj.Tag_Viewer, "obj");
		}

		public override void WriteString(string str) => writeString(str, false);

        public void WriteStringKey(string str) => writeString(str, true);

		public void WriteArray(BrowseNode list)
		{
            if (list.NodeRawData == null)
            {
				WriteNull();
				return;
			}
            BrowseNode below = list.Node_Viewer;
            var count = below?.Count??0;
			WriteStartArray(count);
            if (below!= null) do
            {
                Write(below);
                below = below.Next_Viewer;
            } while (below != null);
        }

        public void WriteObject(BrowseNode obj)
		{
			if (obj.NodeRawData == null)
			{
				WriteNull();
				return;
			}
            BrowseNode below = obj.Node_Viewer;
			WriteStartObject(below.Count);
            do
            {
                writeString((string)below.Key_Viewer, true);
                Write(below);
                below = below.Next_Viewer;
            } while (below != null);
		}
		#endregion

		#region Non-public methods

		private void writeString(string str, bool isKey = false)
		{
			if (ReferenceEquals(str, null))
			{
				WriteNull();
				return;
			}
			if (str.Length == 0)
			{
				WriteEmptyString();
				return;
			}
			uint index;
			if (dictionary != null && dictionary.TryGetValue(str, out index))
			{
				WriteStringGet(index);
				return;
			}
			if (isKey)
			{
				if ((options & PsonOptions.ProgressiveKeys) > 0)
				{
					dictionary.Add(str, (uint)dictionary.Count);
					WriteStringAdd(str);
				}
				else
					base.WriteString(str);
			}
			else
			{
				if ((options & PsonOptions.ProgressiveValues) > 0)
				{
					dictionary.Add(str, (uint)dictionary.Count);
					WriteStringAdd(str);
				}
				else
					base.WriteString(str);
			}
		}

		#endregion
	}
}
