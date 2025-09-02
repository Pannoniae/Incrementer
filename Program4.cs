using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

// For Linux you'll need to add this package:
// dotnet add package SixLabors.ImageSharp

namespace SIMDMandelbrot {
    class Program {
        // mandelbrot params
        private const int MAX_ITER = 10000;
        // optimize the bailout check for precision
        private const double BAILOUT_SQUARED = 4.0;

        // image settings
        private const int WIDTH = 1920;
        private const int HEIGHT = 1080;
        private const double X_MIN = -2.5f;
        private const double X_MAX = 1.0f;
        private const double Y_MIN = -1.0f;
        private const double Y_MAX = 1.0f;

        static void Main2(string[] args) {
            Console.WriteLine("🚀 SIMD Mandelbrot calculator");

            Configuration.Default.PreferContiguousImageBuffers = true;

            // Benchmark Vector<T> implementation
            var sw = Stopwatch.StartNew();
            var image = CalcMandelbrotVector();
            sw.Stop();
            Console.WriteLine($"Vector<T> implementation: {sw.ElapsedMilliseconds}ms");
            image.Save("mandelbrot_vector.png");

            // Benchmark AVX implementation if available
            if (Avx2.IsSupported) {
                sw.Restart();
                image = CalcMandelbrotAVX();
                sw.Stop();
                Console.WriteLine($"AVX implementation: {sw.ElapsedMilliseconds}ms");
                image.Save("mandelbrot_avx.png");
            }
            else {
                Console.WriteLine("💩 AVX not supported on this CPU");
            }

            Console.WriteLine("✨ Done! Check out those sweet fractals!");
        }

        private static Image<Rgba32> CalcMandelbrotVector() {
            // create ImageSharp image with black background
            var image = new Image<Rgba32>(WIDTH, HEIGHT, new Rgba32(0, 0, 0, 255));

            // Use Memory<Rgba32> for bulk memory operations
            image.DangerousTryGetSinglePixelMemory(out var pixelMemory);


            double xScale = (X_MAX - X_MIN) / WIDTH;
            double yScale = (Y_MAX - Y_MIN) / HEIGHT;

            // simd width (4 or 8 for most CPUs)
            int vecSize = Vector<float>.Count;

            // align vector sizes to prevent edge artifacts
            int alignedWidth = WIDTH + (vecSize - (WIDTH % vecSize)) % vecSize;

            // prepare constant vectors
            var vBailout = new Vector<double>(BAILOUT_SQUARED);
            var vMaxIter = new Vector<long>(MAX_ITER);
            var vOne = Vector<long>.One;
            var vZero = Vector<long>.Zero;

            // Parallel processing for max speed, using dynamic partitioning for better load balancing
            Parallel.For(0, HEIGHT, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                y => {
                    Span<Rgba32> pixels = pixelMemory.Span;
                    double cImag = Y_MIN + y * yScale;

                    // Process in SIMD-aligned chunks to prevent edge artifacts
                    for (long x = 0; x < alignedWidth; x += vecSize) {
                        // real values for our vector
                        double[] reals = new double[vecSize];
                        for (long i = 0; i < vecSize; i++) {
                            long xi = x + i;
                            reals[i] = xi < WIDTH ? X_MIN + xi * xScale : 0;
                        }

                        var vCReal = new Vector<double>(reals);
                        var vCImag = new Vector<double>(cImag);

                        // Avoid denormal numbers for better numerical stability
                        var vZReal = Vector<double>.Zero;
                        var vZImag = Vector<double>.Zero;

                        // iteration counting & escaping
                        var vIter = Vector<long>.Zero;
                        var vMask = new Vector<long>(~0); // all active

                        // last magnitude for smooth coloring
                        var vLastMagnitudeSq = Vector<double>.Zero;

                        // compute the fractal for all points in our vector
                        for (int iter = 0; iter < MAX_ITER; iter++) {
                            // z^2 components
                            var vZRealSq = vZReal * vZReal;
                            var vZImagSq = vZImag * vZImag;
                            var vMagnitudeSq = vZRealSq + vZImagSq;

                            // track magnitude for smooth coloring
                            vLastMagnitudeSq = vMagnitudeSq;

                            // check which points escaped
                            var vEscaped = Vector.GreaterThan(vMagnitudeSq, vBailout);

                            // if newly escaped, record iteration
                            var vNewlyEscaped = vEscaped & vMask;
                            if (!Vector.EqualsAll(vNewlyEscaped, vZero)) {
                                vIter = Vector.ConditionalSelect(
                                    vNewlyEscaped,
                                    new Vector<long>(iter),
                                    vIter
                                );
                            }

                            // update active mask
                            vMask = vMask & ~vEscaped;

                            // bail if all escaped
                            if (Vector.EqualsAll(vMask, vZero))
                                break;

                            // Fix potential numerical issues
                            var vNewZReal = vZRealSq - vZImagSq + vCReal;
                            var vNewZImag = 2 * vZReal * vZImag + vCImag;

                            // Detect and fix potential overflows/underflows
                            var vOverflowMask = Vector.GreaterThan(Vector.Abs(vNewZReal) + Vector.Abs(vNewZImag), new Vector<double>(1e10f));
                            vNewZReal = Vector.ConditionalSelect(vOverflowMask, Vector<double>.Zero, vNewZReal);
                            vNewZImag = Vector.ConditionalSelect(vOverflowMask, Vector<double>.Zero, vNewZImag);

                            vZReal = vNewZReal;
                            vZImag = vNewZImag;
                        }

                        // extract results
                        long[] iterCounts = new long[vecSize];
                        double[] lastMagnitudes = new double[vecSize];
                        vIter.CopyTo(iterCounts);
                        vLastMagnitudeSq.CopyTo(lastMagnitudes);

                        // convert to pretty colors with smooth coloring
                        for (long i = 0; i < vecSize; i++) {
                            long xi = x + i;
                            if (xi < WIDTH) {
                                long iteration = iterCounts[i];
                                double lastMagnitudeSq = lastMagnitudes[i];

                                if (iteration >= MAX_ITER) {
                                    pixels[(int)(y * WIDTH + xi)] = new Rgba32(0, 0, 0, 255); // inside the set
                                }
                                else {
                                    // smooth coloring formula
                                    double smoothIter = iteration + 1 - Math.Log(Math.Log(Math.Sqrt(lastMagnitudeSq))) / Math.Log(2);
                                    var (r, g, b) = MapSmoothColor(smoothIter);
                                    pixels[(int)(y * WIDTH + xi)] = new Rgba32(r, g, b, 255);
                                }
                            }
                        }
                    }
                });

            return image;
        }

        private static Image<Rgba32> CalcMandelbrotAVX() {
            // create ImageSharp image with black background
            var image = new Image<Rgba32>(WIDTH, HEIGHT, new Rgba32(0, 0, 0, 255));

            // Use Memory<Rgba32> for bulk memory operations
            image.DangerousTryGetSinglePixelMemory(out var pixelMemory);
            Span<Rgba32> pixels = pixelMemory.Span;

            double xScale = (X_MAX - X_MIN) / WIDTH;
            double yScale = (Y_MAX - Y_MIN) / HEIGHT;

            // AVX works with 8 floats at once
            const int vecSize = 8;

            // align vector sizes to prevent edge artifacts
            int alignedWidth = WIDTH + (vecSize - (WIDTH % vecSize)) % vecSize;

            // constants for calculation
            var vBailout = Vector256.Create(BAILOUT_SQUARED);
            var vMaxIter = Vector256.Create(MAX_ITER);
            var vOne = Vector256.Create(1);

            // parallel processing for max speed
            Parallel.For(0, HEIGHT, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                y => {
                    double cImag = Y_MIN + y * yScale;

                    // Process in SIMD-aligned chunks to prevent edge artifacts
                    for (int x = 0; x < alignedWidth; x += vecSize) {
                        // set up real coordinates
                        var reals = new double[vecSize];
                        for (int i = 0; i < vecSize; i++) {
                            int xi = x + i;
                            reals[i] = xi < WIDTH ? X_MIN + xi * xScale : 0;
                        }

                        var vCReal = Vector256.Create(reals);
                        var vCImag = Vector256.Create(cImag);
                        var vZReal = Vector256<double>.Zero;
                        var vZImag = Vector256<double>.Zero;

                        // iteration counting
                        var vIter = Vector256<int>.Zero;
                        var vMask = Vector256.Create(~0); // all true = still active

                        // for smooth coloring
                        var vLastMagnitudeSq = Vector256<double>.Zero;

                        // mandelbrot iteration
                        for (int iter = 0; iter < MAX_ITER; iter++) {
                            // compute z^2 components
                            var vZRealSq = Avx.Multiply(vZReal, vZReal);
                            var vZImagSq = Avx.Multiply(vZImag, vZImag);
                            var vMagnitudeSq = Avx.Add(vZRealSq, vZImagSq);

                            // save for smooth coloring
                            vLastMagnitudeSq = vMagnitudeSq;

                            // check escapes
                            var vEscaped = Avx.Compare(vMagnitudeSq, vBailout, FloatComparisonMode.OrderedGreaterThanSignaling);
                            var vEscapedInt = vEscaped.AsInt32();

                            // if newly escaped, record iteration
                            var vNewEscape = Avx2.And(vEscapedInt, vMask);
                            if (Avx2.MoveMask(vNewEscape.AsByte()) != 0) {
                                var vCurrIter = Vector256.Create(iter);
                                vIter = Avx2.BlendVariable(vIter, vCurrIter, vNewEscape);
                            }

                            // update active mask
                            vMask = Avx2.AndNot(vEscapedInt, vMask);

                            // bail if all escaped
                            if (Avx2.MoveMask(vMask.AsByte()) == 0)
                                break;

                            // z = z^2 + c
                            var vTemp = Avx.Subtract(vZRealSq, vZImagSq);
                            var vNewZReal = Avx.Add(vTemp, vCReal);

                            var vZRealZImag = Avx.Multiply(vZReal, vZImag);
                            var vTwoZRealZImag = Avx.Add(vZRealZImag, vZRealZImag);
                            var vNewZImag = Avx.Add(vTwoZRealZImag, vCImag);

                            vZReal = vNewZReal;
                            vZImag = vNewZImag;
                        }

                        // extract iteration counts and magnitudes for coloring
                        var iterCounts = new int[vecSize];
                        var lastMagnitudes = new double[vecSize];

                        unsafe {
                            fixed (int* pIterCounts = iterCounts)
                            fixed (double* pLastMagnitudes = lastMagnitudes) {
                                Avx2.Store(pIterCounts, vIter);
                                Avx.Store(pLastMagnitudes, vLastMagnitudeSq);
                            }
                        }

                        // convert to colors with smooth coloring
                        for (int i = 0; i < vecSize; i++) {
                            int xi = x + i;
                            if (xi < WIDTH) {
                                int iteration = iterCounts[i];
                                double lastMagnitudeSq = lastMagnitudes[i];

                                (int R, int G, int B) color;
                                if (iteration >= MAX_ITER) {
                                    color = (0, 0, 0); // inside the set
                                }
                                else {
                                    // smooth coloring formula
                                    double smoothIter = iteration + 1 - Math.Log(Math.Log(Math.Sqrt(lastMagnitudeSq))) / Math.Log(2);
                                    color = MapSmoothColor(smoothIter);
                                }

                                // write directly to image
                                var (r, g, b) = color;
                                image[xi, y] = new Rgba32(
                                    (byte)r,
                                    (byte)g,
                                    (byte)b,
                                    255);
                            }
                        }
                    }
                });

            return image;
        }

        // smooth coloring using a variety of pretty gradients
        private static (byte R, byte G, byte B) MapSmoothColor(double smoothIter) {
            // Ultra-smooth coloring with cyclic blues
            smoothIter = Math.Max(0, smoothIter); // Ensure positive value

            // Normalize and apply a logarithmic scaling for more detail near the set
            double t = Math.Log(1 + smoothIter) / 20.0;
            t = t - Math.Floor(t); // Force to range [0,1)

            // Create a primarily blue palette with subtle variation
            double r = 0;
            double g = 0;
            double b = 0;

            // Blue-dominant palette - smooth transition focused on blues
            if (t < 0.5) {
                // Dark blue to medium blue
                b = 0.5 + 0.5 * Math.Sin(Math.PI * t);
                g = 0.2 * t;
                r = 0.1 * t;
            }
            else {
                // Medium blue to lighter blue with hints of other colors
                b = 0.7 + 0.3 * Math.Sin(Math.PI * t);
                g = 0.1 + 0.3 * t;
                r = 0.05 + 0.15 * t;
            }

            // Scale to 0-255 range with careful clamping
            byte rb = (byte)Math.Clamp(r * 255, 0, 255);
            byte gb = (byte)Math.Clamp(g * 255, 0, 255);
            byte bb = (byte)Math.Clamp(b * 255, 0, 255);

            return (rb, gb, bb);
        }
    }
}