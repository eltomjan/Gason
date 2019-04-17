using Gason;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class Tests
{
    static int parsed;
    static int failed;
    static int m_start1;
    static String ReadFile(String filename)
    {
        String[] paths = {
            "jsonchecker",
            "../jsonchecker",
            "../../jsonchecker"
        };
        String retVal;
        foreach (var name in paths)
        {
            if (Directory.Exists(name))
            {
                if (File.Exists(Path.Combine(name, filename)))
                    retVal = File.ReadAllText(Path.Combine(name, filename));
                else return null;
                return retVal;
            }
        }
        return null;
    }
    static void Parse(String csource, int no, bool ok) {
        Byte[] utf, source, dest;
        int endptr = -1;
        ValueWriter wr = new ValueWriter();
        Parser json = new Parser(true);
        Boolean shrink = false;
        Regex r = new Regex(@"[ \t\r\n]");
        String print;
        do {
            if(shrink)
            {
                csource = r.Replace(csource, "");
            }
            utf = Encoding.UTF8.GetBytes(csource);
            source = new byte[utf.Length + 1];
            dest = new byte[utf.Length*2 + 1];
            utf.CopyTo(source, 0);
            endptr = -1;
            JsonErrno result = json.Parse(source, ref endptr, out JsonNode value
#if KEY_SPLIT
                , new ByteString[] { }, 0, 0, -1
#endif
                );
            if (shrink || no > 100) {
                if(json != null && result == JsonErrno.OK)
                {
                    using (MemoryStream memory = new MemoryStream(dest))
                    using (StreamWriter sw = new StreamWriter(memory))
                    //using (StreamWriter sw = new StreamWriter(Console.OpenStandardOutput()))
                    {
                        sw.NewLine = "\n"; sw.AutoFlush = true;
                        wr.DumpValueIterative(sw, value, source, no > 100 ? 0 : -1); // print formatted JSON
                        sw.Flush();
                        int size = (int)memory.Position;
                        memory.Position = 0;
                        memory.Read(dest, 0, size);
                        print = Encoding.UTF8.GetString(dest, 0, size);
                        if (csource.TrimEnd('\n') != print.TrimEnd('\n'))
                        {
                            Console.WriteLine($"{no%100}:Dump bug:\n{csource}\nvs:\n{print}\n");
                        }
                    }
                }
            } else {
                if (ok && result != JsonErrno.OK)
                {
                    Console.WriteLine($"{no}:FAILED { parsed }: {result}\\{csource}\n{(int)(endptr + 1)} - \\{Encoding.UTF8.GetString(source,0,endptr)}\n");
                    ++failed;
                }
                if (!ok && result == JsonErrno.OK)
                {
                    Console.WriteLine($"{no}:PASSED {parsed}:\n{csource}\n");
                    ++failed;
                }
            }
            if(no <= 100) shrink = !shrink;
        } while (shrink);
        ++parsed;
    }

    static void Pass(String csource, int no = -1) {
        if (m_start1 > 0)  m_start1--;
        if (m_start1 == 0 || no > 0) Parse(csource, no, true);
    }
    static void Fail(String csource, int no = -1) {
        if (m_start1 > 0) m_start1--;
        if (m_start1 == 0 || no > 0) Parse(csource, no, false);
    }
    public static int TestAll(int start1 = 0, int failStart = 0, int passStart = 0) {
        m_start1 = start1;
        Parser jsonParser = new Parser(true); // FloatAsDecimal
        // jsonchecker/failXX.json
        for (int i = 1; i <= 33; i++) {
            if (i == 1) // fail1.json is valid in rapidjson, which has no limitation on type of root element (RFC 7159).
                continue;
            if (i == 18)    // fail18.json is valid in rapidjson, which has no limitation on depth of nesting.
                continue;
            if (i == 7) continue; // switched 2 pass4
            if (i == 10) continue; // switched 2 pass5

            String json = ReadFile($"fail{i}.json");
            if (json == null) {
                Console.WriteLine($"jsonchecker file not found fail{i}.json");
                continue;
            }

            if (failStart == 0 || failStart == i)
                Fail(json, i);
        }
        // passX.json
        for (int i = 1; i <= 6; i++)
        {
            String json = ReadFile($"pass{i}.json");
            if (json == null)
            {
                Console.WriteLine($"jsonchecker file %s not found pass{i}.json");
                continue;
            }

            if (passStart == 0 || passStart == i)
                Pass(json, i);
        }
        foreach (var i in new Byte[4] { 1, 3, 6, 7 })
        {
            String json = ReadFile($"pass{i}vsFrmt.json");
            if (json == null)
            {
                Console.WriteLine($"jsonchecker file %s not found pass{i}.json");
                continue;
            }

            Pass(json, i + 100);
        }

      Pass("1234567890"); // 1.
      Pass("1e-21474836311");
      Pass("1e-42147483631");
      Pass("\"A JSON payload should be an object or array, not a string.\"");
      Fail("[ 1 [   , \"<-- missing inner value 1\"]]"); // 5.
      Fail("{ \"1\" [   , \"<-- missing inner value 2\"]}");
      Fail("[ \"1\" {   , \"<-- missing inner value 3\":\"x\"}]");
      Fail("[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[\"Too deep\"]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]");
      Fail("{\"Unfinished object\"}");
      Fail("{\"Unfinished object 2\" null \"x\"}"); // 10.
      Pass("[1, 2, \"хУй\", [[0.5], 7.11, 13.19e+1], \"ba\\u0020r\", [ [ ] ], -0, -.666, [true, null], {\"WAT?!\": false}]");
      Pass("[\n" + // 12.
        "    \"JSON Test Pattern pass1\",\n" +
        "    {\"object with 1 member\":[\"array with 1 element\"]},\n" +
        "    {},\n" +
        "    [],\n" +
        "    -42,\n" +
        "    true,\n" +
        "    false,\n" +
        "    null,\n" +
        "    {\n" +
        "        \"integer\": 1234567890,\n" +
        "        \"real\": -9876.543210,\n" +
        "        \"e\": 0.123456789e-12,\n" +
        "        \"E\": 1.234567890E+34,\n" +
        "        \"\":  23456789012E66,\n" +
        "        \"zero\": 0,\n" +
        "        \"one\": 1,\n" +
        "        \"space\": \" \",\n" +
        "        \"quote\": \"\\\"\",\n" +
        "        \"backslash\": \"\\\\\",\n" +
        "        \"controls\": \"\\b\\f\\n\\r\\t\",\n" +
        "        \"slash\": \"/ & \\/\",\n" +
        "        \"alpha\": \"abcdefghijklmnopqrstuvwyz\",\n" +
        "        \"ALPHA\": \"ABCDEFGHIJKLMNOPQRSTUVWYZ\",\n" +
        "        \"digit\": \"0123456789\",\n" +
        "        \"0123456789\": \"digit\",\n" +
        "        \"special\": \"`1~!@#$%^&json()_+-={':[,]}|;.</>?\",\n" +
        "        \"hex\": \"\\u0123\\u4567\\u89AB\\uCDEF\\uabcd\\uef4A\",\n" +
        "        \"true\": true,\n" +
        "        \"false\": false,\n" +
        "        \"null\": null,\n" +
        "        \"array\":[  ],\n" +
        "        \"object\":{  },\n" +
        "        \"address\": \"50 St. James Street\",\n" +
        "        \"url\": \"http://www.JSON.org/\",\n" +
        "        \"comment\": \"// /json <!-- --\",\n" +
        "        \"# -- --> json/\": \" \",\n" +
        "        \" s p a c e d \" :[1,2 , 3\n" +
        "\n" +
        ",\n" +
        "\n" +
        "4 , 5        ,          6           ,7        ],\"compact\":[1,2,3,4,5,6,7],\n" +
        "        \"jsontext\": \"{\\\"object with 1 member\\\":[\\\"array with 1 element\\\"]}\",\n" +
        "        \"quotes\": \"&#34; \\u0022 %22 0x22 034 &#x22;\",\n" +
        "        \"\\/\\\\\\\"\\uCAFE\\uBABE\\uAB98\\uFCDE\\ubcda\\uef4A\\b\\f\\n\\r\\t`1~!@#$%^&json()_+-=[]{}|;:',./<>?\"\n" +
        ": \"A key can be any string\"\n" +
        "    },\n" +
        "    0.5 ,98.6\n" +
        ",\n" +
        "99.44\n" +
        ",\n" +
        "\n" +
        "1066,\n" +
        "1e1,\n" +
        "0.1e1,\n" +
        "1e-1,\n" +
        "1e00,2e+00,2e-00\n" +
        ",\"rosebud\"]");
      Pass(new Regex(@"[']").Replace("{'key1': {'key2l2': 'vl2'},'key3': [{'key4l2': ''}],'key5': [{'key6l2': 1}]}", "\"")); // 13.
      Pass(new Regex(@"[']").Replace("{ 'a':'Alpha','b':true,'c':12345,'d':[true,[false,[-123456789,null],3.9676,['Something else.',false],null]],'e'" // 14.
            + ":{'zero':null,'one':1,'two':2,'three':[3],'four':[0,1,2,3,4]},'f':null,'h':{'a':{'b':{'c':{'d':{'e':{'f':{'g':null}}}}}}},'i':[[[[[[[null]]]]]]]}", "\""));

    if (failed > 0)
        Console.WriteLine($"{failed}/{parsed} TESTS FAILED\n");
    else
        Console.WriteLine("ALL TESTS PASSED");

    return 0;
}
}
