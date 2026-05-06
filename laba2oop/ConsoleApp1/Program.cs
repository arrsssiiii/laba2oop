using System;
using System.Diagnostics;
using System.Threading;

class Program
{
    private const int RangeStart = 1;
    private const int RangeEnd = 10000;
    private const int ThreadCount = 4;

    private static int primeCount = 0;

    private static readonly object monitorLock = new object();
    private static readonly Mutex counterMutex = new Mutex();
    private static readonly Semaphore counterSemaphore = new Semaphore(1, 1);

    private static readonly object consoleLock = new object();

    private static int selectedMode = 0;

    static void Main()
    {
        Console.WriteLine("Выберите режим работы:");
        Console.WriteLine("1 - Monitor (lock)");
        Console.WriteLine("2 - Mutex");
        Console.WriteLine("3 - Semaphore");
        Console.Write("Введите номер режима: ");

        string? input = Console.ReadLine();

        if (!int.TryParse(input, out selectedMode) || selectedMode < 1 || selectedMode > 3)
        {
            Console.WriteLine("Ошибка: нужно ввести 1, 2 или 3.");
            return;
        }

        primeCount = 0;

        Thread[] threads = new Thread[ThreadCount];
        Stopwatch stopwatch = new Stopwatch();

        int totalNumbers = RangeEnd - RangeStart + 1;
        int chunkSize = totalNumbers / ThreadCount;

        stopwatch.Start();

        for (int i = 0; i < ThreadCount; i++)
        {
            int threadNumber = i + 1;
            int start = RangeStart + i * chunkSize;
            int end = (i == ThreadCount - 1) ? RangeEnd : start + chunkSize - 1;

            threads[i] = new Thread(() => ProcessRange(threadNumber, start, end));
            threads[i].Start();
        }

        for (int i = 0; i < ThreadCount; i++)
        {
            threads[i].Join();
        }

        stopwatch.Stop();

        Console.WriteLine();

        switch (selectedMode)
        {
            case 1:
                Console.WriteLine("===== Итог (Monitor / lock) =====");
                break;
            case 2:
                Console.WriteLine("===== Итог (Mutex) =====");
                break;
            case 3:
                Console.WriteLine("===== Итог (Semaphore) =====");
                break;
        }

        Console.WriteLine($"Общее количество простых чисел: {primeCount}");
        Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds} мс");
    }

    static void ProcessRange(int threadNumber, int start, int end)
    {
        for (int number = start; number <= end; number++)
        {
            lock (consoleLock)
            {
                Console.WriteLine($"Поток {threadNumber}: проверяет число {number}");
            }

            if (IsPrime(number))
            {
                IncrementPrimeCount();

                lock (consoleLock)
                {
                    Console.WriteLine($"Поток {threadNumber}: найдено простое число {number}");
                }
            }
        }
    }

    static void IncrementPrimeCount()
    {
        switch (selectedMode)
        {
            case 1:
                lock (monitorLock)
                {
                    primeCount++;
                }
                break;

            case 2:
                counterMutex.WaitOne();
                try
                {
                    primeCount++;
                }
                finally
                {
                    counterMutex.ReleaseMutex();
                }
                break;

            case 3:
                counterSemaphore.WaitOne();
                try
                {
                    primeCount++;
                }
                finally
                {
                    counterSemaphore.Release();
                }
                break;
        }
    }

    static bool IsPrime(int number)
    {
        if (number < 2)
            return false;

        if (number == 2)
            return true;

        if (number % 2 == 0)
            return false;

        int limit = (int)Math.Sqrt(number);

        for (int i = 3; i <= limit; i += 2)
        {
            if (number % i == 0)
                return false;
        }

        return true;
    }
}