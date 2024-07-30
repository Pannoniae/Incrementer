using System;
using System.Diagnostics;

/// <summary>
/// Struct with 4 readonly-fields
/// </summary>
public struct Int256
{
    private readonly long bits0;
    private readonly long bits1;
    private readonly long bits2;
    private readonly long bits3;

    public Int256(long bits0, long bits1, long bits2, long bits3)
    {
        this.bits0 = bits0; this.bits1 = bits1; this.bits2 = bits2; this.bits3 = bits3;
    }

    public long Bits0 { get { return bits0; } }
    public long Bits1 { get { return bits1; } }
    public long Bits2 { get { return bits2; } }
    public long Bits3 { get { return bits3; } }
}

/// <summary>
/// Int256 variant with non-readonly fields
/// </summary>
public struct Int256_NRO
{
    private long bits0;
    private long bits1;
    private long bits2;
    private long bits3;

    public Int256_NRO(long bits0, long bits1, long bits2, long bits3)
    {
        this.bits0 = bits0; this.bits1 = bits1; this.bits2 = bits2; this.bits3 = bits3;
    }

    public long Bits0 { get { return bits0; } }
    public long Bits1 { get { return bits1; } }
    public long Bits2 { get { return bits2; } }
    public long Bits3 { get { return bits3; } }

    public long TotalValueOnStruct { get { return Bits0 + Bits1 + Bits2 + Bits3; } }
}

class Test
{
    private readonly Int256     value;
    private readonly Int256_NRO value2;

    /* non-readonly variants */

    private Int256     value3;
    private Int256_NRO value4;


    public Test()
    {
        value  = new Int256    (1L, 5L, 10L, 100L);
        value2 = new Int256_NRO(1L, 5L, 10L, 100L);

        /* non-readonly variants */

        value3 = new Int256    (1L, 5L, 10L, 100L);
        value4 = new Int256_NRO(1L, 5L, 10L, 100L);
    }

    public long TotalValue {
        get { return value.Bits0 + value.Bits1 + value.Bits2 + value.Bits3; }
    }

    public long TotalValue2 {
        get { return value2.Bits0 + value2.Bits1 + value2.Bits2 + value2.Bits3; }
    }

    public long TotalValue3 {
        get { return value3.Bits0 + value3.Bits1 + value3.Bits2 + value3.Bits3; }
    }

    public long TotalValue4 {
        get { return value4.Bits0 + value4.Bits1 + value4.Bits2 + value4.Bits3; }
    }

    public void RunTest()
    {
        const int numIterations = 100000000;

        // Just make sure it's JITted…
        long sample = TotalValue; sample = TotalValue2; sample = TotalValue3; sample = TotalValue4;

        long total;

        Stopwatch sw = Stopwatch.StartNew(); total = 0;
        for (int i = 0; i < numIterations; i++) total += TotalValue;
        sw.Stop();
        Console.WriteLine("{1,-60} {0,5} ms", sw.ElapsedMilliseconds, "Readonly value, readonly fields:");

        sw.Reset(); sw.Start(); total = 0;
        for (int i = 0; i < numIterations; i++) total += TotalValue2;
        sw.Stop();
        Console.WriteLine("{1,-60} {0,5} ms", sw.ElapsedMilliseconds, "Readonly value, non-readonly fields:");


        sw.Reset(); sw.Start(); total = 0;
        for (int i = 0; i < numIterations; i++) total += value2.TotalValueOnStruct;
        sw.Stop();
        Console.WriteLine("{1,-60} {0,5} ms", sw.ElapsedMilliseconds, "Readonly value, non-readonly fields, Total on structure:");


        /* now the non-readonly 'value' versions value3 and value4*/

        sw.Reset(); sw.Start(); total = 0;
        for (int i = 0; i < numIterations; i++) total += TotalValue3;
        sw.Stop();
        Console.WriteLine("{1,-60} {0,5} ms", sw.ElapsedMilliseconds, "Non-Readonly value, readonly fields:");


        sw.Reset(); sw.Start(); total = 0;
        for (int i = 0; i < numIterations; i++) total += TotalValue4;
        sw.Stop();
        Console.WriteLine("{1,-60} {0,5} ms", sw.ElapsedMilliseconds, "Non-Readonly value, non-readonly fields:");

        sw.Reset(); sw.Start(); total = 0;
        for (int i = 0; i < numIterations; i++) total += value4.TotalValueOnStruct;
        sw.Stop();
        Console.WriteLine("{1,-60} {0,5} ms", sw.ElapsedMilliseconds, "Non-Readonly value, non-readonly fields, Total on structure:");
    }
}