using System;
using System.IO;
using System.Text;
using Gason;

public class Program
{
    public static void Main()
    {
        Tests.TestAll();

        String json;
        int endPos = -1;
        JsonNode jsn;
        Byte[] raw;
        Parser jsonParser = new Parser(true); // FloatAsDecimal

        json = "{\n" +
               "  \"id\": \"0001\",\n" +
               "  \"type\": \"donut\",\n" +
               "  \"name\": \"Cake\",\n" +
               "  \"ppu\": 0.55,\n" +
               "  \"batters\": [\n" +
               "      { \n" +
               "        \"id\": \"1001\",\n" +
               "        \"type\": \"Regular\"\n" +
               "      },\n" +
               "      { \n" +
               "        \"id\": \"1002\",\n" +
               "        \"type\": \"Chocolate\"\n" +
               "      },\n" +
               "      { \n" +
               "        \"id\": \"1003\",\n" +
               "        \"type\": \"Blueberry\"\n" +
               "      },\n" +
               "      { \n" +
               "        \"id\": \"1004\",\n" +
               "        \"type\": \"Devil's Food\"\n" +
               "      }\n" +
               "  ]\n" +
               "}";

        raw = Encoding.UTF8.GetBytes(json);
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
        endPos = -1;
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

        raw = File.ReadAllBytes(@"big.json");
        jsonParser.Parse(raw, ref endPos, out jsn
#if KEY_SPLIT
            , null, 0, 0, -1
#endif
            );

        wr = new ValueWriter();
        using (StreamWriter sw = new StreamWriter(Console.OpenStandardOutput()))
        {
            sw.AutoFlush = true;
            wr.DumpValue(sw, jsn, raw); // print formatted JSON
        }

        return;
    }
}