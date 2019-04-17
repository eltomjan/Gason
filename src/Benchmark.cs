using System;
using System.Diagnostics;
using Gason;
public class Benchmark
{
    Byte[] myJSON;
    public Benchmark(Byte[] json)
    {
        myJSON = json;
    }
    public void Run()
    {
        Parser gasonCsharp = new Parser(false);
        int endPos;
        JsonNode jsn;
        JsonErrno e;

        Stopwatch sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < 10; i++) {
            endPos = -1;
            e = gasonCsharp.Parse(myJSON, ref endPos, out jsn
#if KEY_SPLIT
            , null, 0, 0, -1
#endif
            );
        }
        sw.Stop();
        Console.WriteLine($"Parse 10x ={sw.Elapsed}");
    }
}