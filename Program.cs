ulong i = 0;
while (true) {
    if (i % 1_000_000_000 == 0) {
        Console.Out.WriteLine(i);
    }
    i++;
}