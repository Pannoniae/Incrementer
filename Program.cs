public struct IntHolder {
    public int i;
}


public class Program {
    public static void Main(string[] args) {

        IntHolder y = new IntHolder();
        y.i = 5;
        Foo(ref y);
        Console.WriteLine(y.i);
        Foo2(ref y);
        Console.WriteLine(y.i);
    }

    public static void Foo(ref IntHolder x) {
        x.i = 10;
    }

    public static void Foo2(ref IntHolder x) {
        x = new IntHolder();
    }
}