using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

class Program
{
    private const int MaxConcurrentThreads = 3;

    private const int ExpectedSetCount = 15;
    private const int ExpectedNumbersPerSet = 100;

    private static readonly Semaphore semaphore = new Semaphore(MaxConcurrentThreads, MaxConcurrentThreads);

    private static readonly object resultsLock = new object();

    private static readonly Mutex totalMutex = new Mutex();

    private static readonly object consoleLock = new object();

    private static readonly List<SetResult> results = new List<SetResult>();

    private static int grandTotal = 0;

    static void Main()
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "datasets.txt");

        if (!File.Exists(filePath))
        {
            Console.WriteLine("Ошибка: файл datasets.txt не найден.");
            Console.WriteLine("Помести datasets.txt рядом с исполняемым файлом проекта.");
            return;
        }

        List<int[]> dataSets;

        try
        {
            dataSets = LoadDataSets(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при чтении наборов данных:");
            Console.WriteLine(ex.Message);
            return;
        }

        Thread[] threads = new Thread[dataSets.Count];
        Stopwatch stopwatch = new Stopwatch();

        stopwatch.Start();

        for (int i = 0; i < dataSets.Count; i++)
        {
            int setNumber = i + 1;
            int[] numbers = dataSets[i];

            threads[i] = new Thread(() => ProcessSet(setNumber, numbers));
            threads[i].Start();
        }

        for (int i = 0; i < threads.Length; i++)
        {
            threads[i].Join();
        }

        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine("===== Результаты обработки наборов =====");

        var orderedResults = results.OrderBy(r => r.SetNumber).ToList();

        foreach (var result in orderedResults)
        {
            Console.WriteLine(
                $"Набор {result.SetNumber}: сумма = {result.Sum}, поток = {result.ThreadName}");
        }

        Console.WriteLine();
        Console.WriteLine($"Общий итог по всем наборам: {grandTotal}");
        Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds} мс");
    }

    static void ProcessSet(int setNumber, int[] numbers)
    {
        semaphore.WaitOne();

        try
        {
            string threadName = Thread.CurrentThread.ManagedThreadId.ToString();

            lock (consoleLock)
            {
                Console.WriteLine($"Поток {threadName} начал обработку набора {setNumber}");
            }

            int sum = 0;
            for (int i = 0; i < numbers.Length; i++)
            {
                sum += numbers[i];
            }

            SetResult result = new SetResult
            {
                SetNumber = setNumber,
                Sum = sum,
                ThreadName = threadName
            };

            lock (resultsLock)
            {
                results.Add(result);
            }

            totalMutex.WaitOne();
            try
            {
                grandTotal += sum;
            }
            finally
            {
                totalMutex.ReleaseMutex();
            }

            lock (consoleLock)
            {
                Console.WriteLine($"Поток {threadName} закончил набор {setNumber}, сумма = {sum}");
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    static List<int[]> LoadDataSets(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);

        if (lines.Length != ExpectedSetCount)
        {
            throw new Exception(
                $"Ожидалось {ExpectedSetCount} наборов, а найдено {lines.Length}.");
        }

        List<int[]> dataSets = new List<int[]>();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                throw new Exception($"Строка {i + 1} пустая.");
            }

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != ExpectedNumbersPerSet)
            {
                throw new Exception(
                    $"В строке {i + 1} должно быть {ExpectedNumbersPerSet} чисел, а найдено {parts.Length}.");
            }

            int[] numbers = new int[parts.Length];

            for (int j = 0; j < parts.Length; j++)
            {
                if (!int.TryParse(parts[j], out numbers[j]))
                {
                    throw new Exception(
                        $"Ошибка в строке {i + 1}: '{parts[j]}' не является целым числом.");
                }

                if (numbers[j] < 1 || numbers[j] > 100)
                {
                    throw new Exception(
                        $"Ошибка в строке {i + 1}: число {numbers[j]} вне диапазона 1..100.");
                }
            }

            dataSets.Add(numbers);
        }

        return dataSets;
    }
}

class SetResult
{
    public int SetNumber { get; set; }
    public int Sum { get; set; }
    public string ThreadName { get; set; } = "";
}