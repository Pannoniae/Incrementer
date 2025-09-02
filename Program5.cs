using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;


namespace Incrementer;

public sealed class InvariantArray<T> where T : class {
    private readonly Wrapper<T>[] array;

    public InvariantArray(int size) {
        array = new Wrapper<T>[size];
    }

    public T this[int index] {
        get { return array[index].Value; }
        set { array[index] = value; }
    }
}

public struct Wrapper<T> where T : class {
    private readonly T value;

    public T Value {
        get { return value; }
    }

    public Wrapper(T value) {
        this.value = value;
    }

    public static implicit operator Wrapper<T>(T value) {
        return new Wrapper<T>(value);
    }
}

public static class ArrayPerformance {
    delegate void Test(int iterations, int size, Stopwatch watch);

    public static int main(string[] args) {
        return 0;
    }

    static void Moin(string[] args) {
        if (args.Length != 2) {
            Console.WriteLine("Usage: ArrayPerformance <iterations> <size>");
            return;
        }

        int iterations = int.Parse(args[0]);
        int size = int.Parse(args[1]);

        var tests = new Test[] {
            WriteObjectArray,
            ReadObjectArray,
            WriteStringArray,
            ReadStringArray,
            WriteWrapperArray,
            ReadWrapperArray,
            WriteInvariantArray,
            ReadInvariantArray
        };
        foreach (var test in tests) {
            Stopwatch watch = new Stopwatch();
            test(1, 1, watch); // JIT Compilation
            watch.Reset();
            GC.Collect();
            test(iterations, size, watch);
            watch.Stop();
            Console.WriteLine("{0}: {1}ms", test.Method.Name,
                (int)watch.Elapsed.TotalMilliseconds);
            GC.Collect();
        }
    }

    static void WriteObjectArray(int iterations, int size, Stopwatch watch) {
        object value = new object();
        var array = new object[size];

        watch.Start();
        for (int i = 0; i < iterations; i++) {
            for (int j = 0; j < size; j++) {
                array[j] = value;
            }
        }
    }

    static void ReadObjectArray(int iterations, int size, Stopwatch watch) {
        object value = new object();
        object[] array = Enumerable.Repeat(value, size).ToArray();

        watch.Start();
        for (int i = 0; i < iterations; i++) {
            for (int j = 0; j < size; j++) {
                AssertNotNull(array[j]);
            }
        }
    }

    static void WriteStringArray(int iterations, int size, Stopwatch watch) {
        string value = "value";
        string[] array = new string[size];

        watch.Start();
        for (int i = 0; i < iterations; i++) {
            for (int j = 0; j < size; j++) {
                array[j] = value;
            }
        }
    }

    static void ReadStringArray(int iterations, int size, Stopwatch watch) {
        string value = "value";
        string[] array = Enumerable.Repeat(value, size)
            .ToArray();

        watch.Start();
        for (int i = 0; i < iterations; i++) {
            for (int j = 0; j < size; j++) {
                AssertNotNull(array[j]);
            }
        }
    }

    static void WriteWrapperArray(int iterations, int size, Stopwatch watch) {
        object value = new object();
        Wrapper<object>[] array = new Wrapper<object>[size];

        watch.Start();
        for (int i = 0; i < iterations; i++) {
            for (int j = 0; j < size; j++) {
                array[j] = value;
            }
        }
    }

    static void ReadWrapperArray(int iterations, int size, Stopwatch watch) {
        object value = new object();
        Wrapper<object>[] array = Enumerable.Repeat(new Wrapper<object>(value), size)
            .ToArray();

        watch.Start();
        for (int i = 0; i < iterations; i++) {
            for (int j = 0; j < size; j++) {
                AssertNotNull(array[j].Value);
            }
        }
    }

    static void WriteInvariantArray(int iterations, int size, Stopwatch watch) {
        string value = "value";
        InvariantArray<string> array = new InvariantArray<string>(size);

        watch.Start();
        for (int i = 0; i < iterations; i++) {
            for (int j = 0; j < size; j++) {
                array[j] = value;
            }
        }
    }

    static void ReadInvariantArray(int iterations, int size, Stopwatch watch) {
        string value = "value";
        InvariantArray<string> array = new InvariantArray<string>(size);
        // Can't use ToArray, although we could use a ToInvariantArray
        // extension method if we wanted to create one
        for (int i = 0; i < size; i++) {
            array[i] = value;
        }

        watch.Start();
        for (int i = 0; i < iterations; i++) {
            for (int j = 0; j < size; j++) {
                AssertNotNull(array[j]);
            }
        }
    }

    static void AssertNotNull(object item) {
        if (item == null) {
            throw new ArgumentNullException();
        }
    }
}