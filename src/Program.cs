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
  'id': '0001',
  'type': 'donut',
  'name': 'Cake',
  'ppu': 0.55,
  'batters': [
    {
      'id': '1001',
      'type': 'Regular'
    },
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
        Console.WriteLine(prn.Print(ref v1, 2).ToString());
        Console.WriteLine(prn.Print(ref v2, 2).ToString());

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
        using (StreamWriter sw = new StreamWriter(Console.OpenStandardOutput()))
        {
            sw.AutoFlush = true;
            wr.DumpValueIterative(sw, jsn, raw);
        }
#if !KEY_SPLIT
        endPos = -1;
#endif
        jsonParser.Parse(raw, ref endPos, out jsn
#if KEY_SPLIT
            , keys, 2, endPos, 2
#endif
            ); // and now following 2
        using (StreamWriter sw = new StreamWriter(Console.OpenStandardOutput()))
        {
            sw.AutoFlush = true;
            wr.DumpValueIterative(sw, jsn, raw);
        }

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
        JsonNode nNo2 = null, nNo3 = null;
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
        }
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
        jsonParser.RemoveTwins(ref v1, ref v2);
        String result2 =
@"{
  'statuses': [
    {
      {
        'user_mentions': [
          {
            'indices': [
              '2',
,
              '3'
            ]
          }
        ]
      }
    }
  ]
}
".Replace("'", "\"").Replace("\r\n", "\n");
        if (v1.NodeRawData != null) Console.WriteLine("Bug - 1st JSON not empty");
        else Console.WriteLine("Twitter check 1/2 OK - 1st file empty");
        if (v2.NodeRawData == null) Console.WriteLine("Bug - 2nd JSON empty");
        else if (result2 == prn.Print(ref v2, 2).ToString()) Console.WriteLine($"Twitter check 2/2 OK - 2nd file has expected content:\n{result2}");
        else Console.WriteLine("Bug demo print result differs.");


        raw = File.ReadAllBytes(@"citylots.json");
        Benchmark b = new Benchmark(raw);
        b.Run(); // < 30s
        return;
    }
}