using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace MyBenchmark;

public class StructHashCodeTest {

    public static IEnumerable<int> KeySource => Enumerable.Range(0, 0xfffff);

    public static readonly Dictionary<int, int> IntKeyedDic =
        KeySource.ToDictionary(x => x, x => x);

    public static readonly Dictionary<SimpleWrapper, int> SimpleWrapperKeyedDic =
        KeySource.ToDictionary(x => new SimpleWrapper(x), x => x);

    public static readonly Dictionary<StandardWrapper, int> StandardWrapperKeyedDic =
        KeySource.ToDictionary(x => new StandardWrapper(x), x => x);


    public static readonly Dictionary<RecordWrapper, int> RecordWrapperKeyedDic =
        KeySource.ToDictionary(x => new RecordWrapper(x), x => x);

    [Benchmark]
    public int IntKeyed() {
        var retval = 0;
        foreach (var key in IntKeyedDic.Keys)
            retval += IntKeyedDic[key];
        return retval;
    }

    [Benchmark]
    public int SimpleIntWrapperKeyed() {
        var retval = 0;
        foreach (var key in SimpleWrapperKeyedDic.Keys)
            retval += SimpleWrapperKeyedDic[key];
        return retval;
    }

    [Benchmark]
    public int StandardIntWrapperKeyed() {
        var retval = 0;
        foreach (var key in StandardWrapperKeyedDic.Keys)
            retval += StandardWrapperKeyedDic[key];
        return retval;
    }

    [Benchmark]
    public int IntRecastedKeyed() {
        var retval = 0;
        foreach (var key in SimpleWrapperKeyedDic.Keys)
            retval += IntKeyedDic[key.Value];
        return retval;
    }

    [Benchmark]
    public int RecordStructIntKeyed() {
        var retval = 0;
        foreach (var key in RecordWrapperKeyedDic.Keys) {
            retval += RecordWrapperKeyedDic[key];
        }
        return retval;
    }
}

public readonly struct SimpleWrapper {
    public readonly int Value;

    public SimpleWrapper(int value) => Value = value;
}

public readonly struct StandardWrapper {
    public readonly int Value;

    public StandardWrapper(int value) => Value = value;

    public override bool Equals(object obj)
        => obj is StandardWrapper other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}

public readonly record struct RecordWrapper(int value) {

    public readonly int value = value;
}

public class Program {

    public static void Main_(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}