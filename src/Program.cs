using System;
using System.IO;
using System.Text;
using Gason;

public class Program
{
    public static void Main()
    {
        Tests.TestAll();

        int endPos = -1;
        JsonNode jsn;
        Byte[] raw;
        Parser jsonParser = new Parser(true); // FloatAsDecimal

        String[] jsons =
@"{
  'id': '000',
  'id': '00',
  'id': '0',
  'id': '0001',
  'type': 'donut',
  'name': 'Cake',
  'ppu': 0.55,
  'batters': [
    {
      'id': '1003',
      'type': 'Blueberry'
    },
    {
      'id': '1002',
      'type': 'Chocolate'
    },
    {
      'id': '1004',
      'type': 'Bad Food'
    },
    {
      'id': '1001',
      'type': 'Regular'
    }
  ]
}|{
  'batters': [
    {
      'id': '1002',
      'type': 'Chocolate'
    },
    {
      'id': '1003',
      'type': 'Blueberry'
    },
    {
      'id': '1004',
      'type': 'Bad Food'
    }
  ],
  'id': '0001',
  'type': 'donut',
  'name': 'Cake',
  'ppu': 0.55
}|{
  'id': '0001',
  'type': 'donut',
  'name': 'Cake',
  'ppu': 0.55
  'batters': [
    {
      'id': '1001',
      'type': 'Regular'
    },
    {
      'id': '1003',
      'type': 'Blueberry'
    },
    {
      'id': '1004',
      'type': 'Bad Food'
    }
  ]
}".Replace("'", "\"").Split('|');
        raw = Encoding.UTF8.GetBytes(jsons[1]);
        jsonParser.Parse(raw, ref endPos, out JsonNode jsn1
#if KEY_SPLIT
            , new ByteString[] { }, 0, 0, 0
#endif
        );
        BrowseNode v1 = new BrowseNode(ref jsn1, raw);

        raw = Encoding.UTF8.GetBytes(jsons[2]);
        jsonParser.Parse(raw, ref endPos, out JsonNode jsn2
#if KEY_SPLIT
            , new ByteString[] { }, 0, 0, 0
#endif
        );
        BrowseNode v2 = new BrowseNode(ref jsn2, raw);

        jsonParser.RemoveTwins(ref v1, ref v2);

        Printer prn = new Printer();
        Console.WriteLine(prn.Print(ref v1, 0).ToString());
        Console.WriteLine(prn.Print(ref v2, 0).ToString());

        raw = Encoding.UTF8.GetBytes(jsons[0]);
        ByteString[] keys = new ByteString[]
        {
            new ByteString("batters"),
            null
        };
        jsonParser.Parse(raw, ref endPos, out jsn
#if KEY_SPLIT
            , keys, 2, 0, 2
#endif
            ); // batters / null path, read only 1st 2
        ValueWriter wr = new ValueWriter();
#if !KEY_SPLIT
        jsonParser.SortPaths(jsn, raw, null);
        Console.WriteLine("Sort result:");
        using (StreamWriter sw = new StreamWriter(Console.OpenStandardOutput()))
        {
            sw.AutoFlush = true;
            wr.DumpValueIterative(sw, jsn, raw);
        }
        endPos = -1;
#else
        jsonParser.Parse(raw, ref endPos, out jsn
            , keys, 2, endPos, 2
            ); // and now following 2
        using (StreamWriter sw = new StreamWriter(Console.OpenStandardOutput()))
        {
            sw.AutoFlush = true;
            wr.DumpValueIterative(sw, jsn, raw);
        }
#endif

        // Twiter complex json -> TC
        raw = Encoding.UTF8.GetBytes(Tests.ReadFile("pass6.json"));
        jsonParser.Parse(raw, ref endPos, out JsonNode jsn01
#if KEY_SPLIT
            , new ByteString[] { }, 0, 0, 0
#endif
        );
        jsonParser.Parse(raw, ref endPos, out JsonNode jsn02
#if KEY_SPLIT
            , new ByteString[] { }, 0, 0, 0
#endif
        );
        v1 = new BrowseNode(ref jsn01, raw);
        v2 = new BrowseNode(ref jsn02, raw);
        BreadthFirst bf1 = new BreadthFirst(v1);
        BreadthFirst bf2 = new BreadthFirst(v2);
        JsonNode nNo2 = null, nNo3 = null, nId1 = null, nId2 = null;
        if (bf2.FindNode("created_at")) // Small TC-like demo
        {
            P_ByteLnk index = bf2.Current.NodeRawData.doubleOrString, tmp;
            ByteString chars = bf2.Current.NodeRawData.GetFatData(raw);
            tmp = index;
            tmp.length = 1;
            tmp.pos += chars.Find('2'); // find "2" for value
            nNo2 = new JsonNode
            {
                Tag = JsonTag.JSON_STRING,
                doubleOrString = tmp
            };
            tmp.pos = index.pos + chars.Find('3'); // find "3" for value
            nNo3 = new JsonNode
            {
                Tag = JsonTag.JSON_STRING,
                doubleOrString = tmp
            };
            tmp.pos = index.pos;
            tmp.length = 3; // Sun
            nId1 = new JsonNode
            {
                Tag = JsonTag.JSON_STRING,
                doubleOrString = tmp
            };
            tmp.pos += 4; // Aug
            nId2 = new JsonNode
            {
                Tag = JsonTag.JSON_STRING,
                doubleOrString = tmp
            };
        }
        if (bf1.FindNode("metadata")
        && bf1.Parent()
        && bf1.NextNth(99)
        && bf1.FindNode("user")
        && bf1.FindNode("hashtags"))
        {
            bf1.Next();
            bf1.PrependChild(nId1);
        }
        bf1.Current = bf1.Root;
        if (bf2.FindNode("metadata")
        && bf2.Parent()
        && bf2.NextNth(37)
        && bf2.FindNode("retweet_count")
        && bf2.FindNode("screen_name")
        && bf2.FindNode("indices"))
        {
            bf2.PrependChild(nNo3);
            bf2.PrependChild(nNo2);
        }
        if (bf2.FindNode("metadata")
        && bf2.Parent()
        && bf2.NextNth(60)
        && bf2.FindNode("user")
        && bf2.FindNode("hashtags"))
        {
            bf2.Next();
            bf2.PrependChild(nId2);
        }
        jsonParser.RemoveTwins(ref v1, ref v2);
        String[] results =
@"{
  'statuses': [
    {
      'created_at': 'Sun Aug 31 00:28:56 +0000 2014',
      'entities': {
        'hashtags': [
          {
            'Sun'
          }
        ]
      }
    }
  ]
}
|{
  'statuses': [
    {
      'entities': {
        'user_mentions': [
          {
            'indices': [
              '2',
              '3'
            ]
          }
        ]
      }
    },
    {
      'created_at': 'Sun Aug 31 00:28:56 +0000 2014',
      'entities': {
        'hashtags': [
          {
            'Aug'
          }
        ]
      }
    }
  ]
}
".Replace("'","\"").Replace("\r\n","\n").Split('|');
        if (v1.NodeRawData == null) Console.WriteLine("Bug - 1st JSON empty");
        else if (results[0] == prn.Print(ref v1, 0).ToString()) Console.WriteLine($"Twitter check 1/2 OK - 2nd file has expected content:\n{results[0]}");
        if (v2.NodeRawData == null) Console.WriteLine("Bug - 2nd JSON empty");
        else if (results[1] == prn.Print(ref v2, 0).ToString()) Console.WriteLine($"Twitter check 2/2 OK - 2nd file has expected content:\n{results[1]}");
        else Console.WriteLine("Bug demo print result differs.");

        raw = File.ReadAllBytes(@"citylots.json");
        Benchmark b = new Benchmark(raw);
        b.Run(); // < 30s
        return;
    }
}