using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    internal static class IntExtensions
    {
        public static bool IsPrimeNumber(this int number)
        {
            if (number <= 1)
            {
                return false;
            }
            var isPrime = true;
            for (int i = 2; i < number; i++)
            {
                if (number % i == 0)
                {
                    isPrime = false;
                    break;
                }
            }
            return isPrime;
        }
    }
}

namespace PrimeNumbers
{
    internal class Program
    {
        private const int MaxPrimeNumber = 500_000;
        private const int ChunkSize = 50_000;

        private static readonly object Obj = new object();

        [DllImport("Kernel32.dll"), SuppressUnmanagedCodeSecurity]
        public static extern int GetCurrentProcessorNumber();

        private static async Task Main(string[] args)
        {
            Console.WriteLine($"{nameof(Main)}, ThreadId {Thread.CurrentThread.ManagedThreadId}");

            var stopwatch = Stopwatch.StartNew();

            #region Synchronous
            var count = SynchronousMethod();
            Console.WriteLine(count);

            //41538
            //0:00:18,4202546
            #endregion

            #region Asynchronous
            //var count = await AsynchronousMethod();
            //Console.WriteLine(count);

            //from 150001 to 200000, 10, 5
            //from 100001 to 150000, 6, 0
            //from 250001 to 300000, 11, 3
            //from 200001 to 250000, 7, 3
            //from 1 to 50000, 4, 0
            //from 50001 to 100000, 5, 2
            //from 350001 to 400000, 8, 4
            //from 300001 to 350000, 9, 7
            //from 400001 to 450000, 4, 0
            //from 450001 to 500000, 5, 5
            //41538
            //0:00:05,5200586
            #endregion

            #region ParallelFor
            //var count = ParallelForMethod();
            //Console.WriteLine(count);

            //41538
            //0:00:04,3893398
            #endregion

            #region ParallelInvoke
            //ParallelInvokeMethod();
            //Console.WriteLine(_count);

            //from 350001 to 400000, 10, 1
            //from 1 to 50000, 1, 6
            //from 200001 to 250000, 5, 2
            //from 50001 to 100000, 7, 2
            //from 250001 to 300000, 8, 4
            //from 400001 to 450000, 11, 3
            //from 150001 to 200000, 6, 0
            //from 300001 to 350000, 9, 6
            //from 100001 to 150000, 4, 3
            //from 450001 to 500000, 7, 0
            //41538
            //0:00:05,5681937
            #endregion

            Console.WriteLine($"{stopwatch.Elapsed:g}");
        }

        #region Synchronous
        private static int SynchronousMethod()
        {
            var count = 0;
            for (var i = 0; i <= MaxPrimeNumber; i++)
            {
                if (i.IsPrimeNumber())
                {
                    count++;
                }
            }

            return count;
        }
        #endregion

        #region ParallelFor
        private static int ParallelForMethod()
        {
            var count = 0;

            Parallel.For(0, MaxPrimeNumber + 1, i =>
            {
                if (i.IsPrimeNumber())
                {
                    // count++ is not thread safe

                    Interlocked.Increment(ref count);

                    //lock (Obj)
                    //{
                    //    count++;
                    //}
                }
            });

            return count;
        }
        #endregion

        #region ParallelInvoke
        private static int _count;

        private static void ParallelInvokeMethod()
        {
            var actions = new List<Action>();
            var current = 0;
            while (current < MaxPrimeNumber)
            {
                var innerCurrent = current;
                actions.Add(() => { InvokeMethodFromTo(innerCurrent + 1, innerCurrent + ChunkSize); });
                current += ChunkSize;
            }

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1 // By default, -1, With 1 will be so slow than synchronous method
            };

            Parallel.Invoke(options, actions.ToArray());
        }

        private static void InvokeMethodFromTo(int from, int to)
        {
            Console.WriteLine($"from {from} to {to}, ThreadId {Thread.CurrentThread.ManagedThreadId}, CPU # {GetCurrentProcessorNumber()}");

            var count = 0;

            for (var i = from; i <= to; i++)
            {
                if (i.IsPrimeNumber())
                {
                    count++;
                }
            }

            lock (Obj)
            {
                _count += count;
            }
        }
        #endregion

        #region Asynchronous
        private static async Task<int> AsynchronousMethod()
        {
            var tasks = new List<Task<int>>();
            var current = 0;
            while (current < MaxPrimeNumber)
            {
                tasks.Add(AsynchronousMethodFromTo(current + 1, current + ChunkSize));
                current += ChunkSize;
            }
            await Task.WhenAll(tasks);
            return tasks.Sum(t => t.Result);
        }

        private static async Task<int> AsynchronousMethodFromTo(int from, int to)
        {
            return await Task.Run(() =>
            {
                Console.WriteLine($"from {from} to {to}, ThreadId {Thread.CurrentThread.ManagedThreadId}, CPU # {GetCurrentProcessorNumber()}");

                var count = 0;

                for (var i = from; i <= to; i++)
                {
                    if (i.IsPrimeNumber())
                    {
                        count++;
                    }
                }

                return count;
            });
        }
        #endregion
    }
}
