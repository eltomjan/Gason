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
        BrowseNode v1, v2;
#if DoubleLinked
        BreadthFirst bf1, bf2;
#endif
        Parser jsonParser = new Parser(true); // FloatAsDecimal
        Printer prn = new Printer();

#if DoubleLinked
        raw = Encoding.UTF8.GetBytes(Strings.JSONnetPart1);
        jsonParser.Parse(raw, ref endPos, out JsonNode jsn1
#if KEY_SPLIT
            , new ByteString[] { }, 0, 0, 0
#endif
        );
        bf1 = new BreadthFirst(jsn1, raw);
        v1 = new BrowseNode(ref jsn1, raw);

        raw = Encoding.UTF8.GetBytes(Strings.JSONnetPart2);
        jsonParser.Parse(raw, ref endPos, out JsonNode jsn2
#if KEY_SPLIT
            , new ByteString[] { }, 0, 0, 0
#endif
        );
        jsonParser.SortPaths(jsn2, raw, "id");
        v2 = new BrowseNode(ref jsn2, raw);

        bf2 = new BreadthFirst(jsn2, raw);
        jsonParser.RemoveTwins(ref bf1, ref bf2);

        Console.WriteLine("RemoveTwins result 1/2:");
        Console.WriteLine(prn.Print(ref v1, 0).ToString());
        Console.WriteLine("RemoveTwins result 2/2:");
        Console.WriteLine(prn.Print(ref v2, 0).ToString());

        raw = Encoding.UTF8.GetBytes(Strings.JSONnetComplete);
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
#if !KEY_SPLIT
        jsonParser.SortPaths(jsn, raw, null);
        v1 = new BrowseNode(ref jsn, raw);
        String check = prn.Print(ref v1, 0).ToString();
        if (Strings.Sort1 != check) Console.WriteLine($"SortPaths 2.1 error:\n{Strings.Sort1}!=\n{check}<-");
        else Console.WriteLine("SortPaths 2.1 OK");
        jsonParser.SortPaths(jsn, raw, "id");
        check = prn.Print(ref v1, 0).ToString();
        if (Strings.Sort2 != check) Console.WriteLine($"SortPaths 2.2 error:\n{Strings.Sort2}");
        else Console.WriteLine("SortPaths 2.2 OK");
        endPos = -1;
#else
        Console.WriteLine("1st 2 rows of betters only:");
        jsonParser.Parse(raw, ref endPos, out jsn
            , keys, 2, endPos, 2
            ); // and now following 2
        ValueWriter wr = new ValueWriter();
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
        bf1 = new BreadthFirst(jsn01, raw);
        bf2 = new BreadthFirst(jsn02, raw);
        Tests.ModifyTwitter(ref bf1, ref bf2, raw);
        jsonParser.RemoveTwins(ref bf1, ref bf2);
        if (v1.NodeRawData == null) Console.WriteLine("Bug - 1st modified Twitter JSON empty");
        else if (Strings.Twitter1 == prn.Print(ref v1, 0).ToString()) Console.WriteLine($"Twitter check 1/2 OK - 1st variant has expected content:");
        else Console.WriteLine($"Twiter RemoveTwins result 1/2 differs:\n{prn.Print(ref v1, 0).ToString()}");
        if (v2.NodeRawData == null) Console.WriteLine("Bug - 2nd modified Twitter JSON empty");
        else if (Strings.Twitter2 == prn.Print(ref v2, 0).ToString()) Console.WriteLine($"Twitter check 2/2 OK - 2nd variant has expected content:");
        else Console.WriteLine($"Twiter RemoveTwins result 2/2 differs:\n{prn.Print(ref v2, 0).ToString()}");
#endif

        raw = File.ReadAllBytes(@"citylots.json");
        Benchmark b = new Benchmark(raw);
        b.Run(); // < 30s
        return;
    }
}