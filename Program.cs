using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SplitIntIntoBytes {
    [StructLayout(LayoutKind.Explicit)]
    struct FooUnion {
        [FieldOffset(0)]
        public byte byte0;
        [FieldOffset(1)]
        public byte byte1;
        [FieldOffset(2)]
        public byte byte2;
        [FieldOffset(3)]
        public byte byte3;

        [FieldOffset(0)]
        public int integer;
    }

    class Program {

        static void Main(string[] args) {
            new Test().RunTest();
        }

        static void Main2(string[] args) {
            testUnion();
            testShift();
            testBitConverter();

            Stopwatch Timer = new Stopwatch();

            Timer.Start();
            int sumTestUnion = testUnion();
            Timer.Stop();

            Console.WriteLine("time of Union:        " + Timer.Elapsed.TotalMicroseconds + " microseconds,  sum: " + sumTestUnion);

            Timer.Restart();
            int sumTestShift = testShift();
            Timer.Stop();

            Console.WriteLine("time of Shift:        " + Timer.Elapsed.TotalMicroseconds + " microseconds,  sum: " + sumTestShift);

            Timer.Restart();
            int sumBitConverter = testBitConverter();
            Timer.Stop();

            Console.WriteLine("time of BitConverter: " + Timer.Elapsed.TotalMicroseconds + " microseconds,  sum: " + sumBitConverter);
            Console.ReadKey();
        }

        static int testBitConverter() {
            byte[] UnionBytes = new byte[4];
            byte[] SumOfBytes = new byte[4];
            SumOfBytes[0] = SumOfBytes[1] = SumOfBytes[2] = SumOfBytes[3] = 0;

            for (int i = 0; i < 1000000; i++) {
                UnionBytes = BitConverter.GetBytes(i);
                SumOfBytes[0] += UnionBytes[0];
                SumOfBytes[1] += UnionBytes[1];
                SumOfBytes[2] += UnionBytes[2];
                SumOfBytes[3] += UnionBytes[3];
            }
            return SumOfBytes[0] + SumOfBytes[1] + SumOfBytes[2] + SumOfBytes[3];
        }

        static int testUnion() {
            byte[] SumOfBytes = new byte[4];
            SumOfBytes[0] = SumOfBytes[1] = SumOfBytes[2] = SumOfBytes[3] = 0;

            FooUnion union = new FooUnion();

            for (int i = 0; i < 1000000; i++) {
                union.integer = i;
                SumOfBytes[0] += union.byte0;
                SumOfBytes[1] += union.byte1;
                SumOfBytes[2] += union.byte2;
                SumOfBytes[3] += union.byte3;
            }
            return SumOfBytes[0] + SumOfBytes[1] + SumOfBytes[2] + SumOfBytes[3];
        }

        static int testShift() {
            byte[] SumOfBytes = new byte[4];
            SumOfBytes[0] = SumOfBytes[1] = SumOfBytes[2] = SumOfBytes[3] = 0;

            int integer = 0;

            for (int i = 0; i < 1000000; i++) {
                integer = i;
                SumOfBytes[0] += (byte)(integer >> 24);
                SumOfBytes[1] += (byte)((integer >> 16) & 0xFF);
                SumOfBytes[2] += (byte)((integer >> 8) & 0xFF);
                SumOfBytes[3] += (byte)(integer & 0xFF);
            }
            return SumOfBytes[0] + SumOfBytes[1] + SumOfBytes[2] + SumOfBytes[3];
        }
    }
}