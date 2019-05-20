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

        raw = File.ReadAllBytes(@"citylots.json");
        Benchmark b = new Benchmark(raw);
        b.Run(); // < 30s
        return;
    }
}