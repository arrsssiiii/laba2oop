using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();

    static async Task Main()
    {
        Console.WriteLine("Выберите режим работы:");
        Console.WriteLine("1 - Синхронная версия (без async/await)");
        Console.WriteLine("2 - Асинхронная версия (с async/await)");
        Console.Write("Введите номер режима: ");

        string? input = Console.ReadLine();

        if (input == "1")
        {
            RunSynchronousVersion();
        }
        else if (input == "2")
        {
            await RunAsynchronousVersion();
        }
        else
        {
            Console.WriteLine("Ошибка: нужно ввести 1 или 2.");
        }
    }

    // без async/await 
    static void RunSynchronousVersion()
    {
        string[] urls =
        {
            "https://jsonplaceholder.typicode.com/posts/1",
            "https://jsonplaceholder.typicode.com/users/1",
            "https://jsonplaceholder.typicode.com/todos/1"
        };

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Console.WriteLine();
        Console.WriteLine("===== Синхронная версия =====");

        foreach (string url in urls)
        {
            try
            {
                HttpResponseMessage response = httpClient.GetAsync(url).Result;

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Ошибка запроса к {url}");
                    Console.WriteLine($"Код ответа сервера: {(int)response.StatusCode} {response.StatusCode}");
                    continue;
                }

                string json = response.Content.ReadAsStringAsync().Result;

                Console.WriteLine();
                Console.WriteLine($"Ответ от сервера: {url}");
                Console.WriteLine(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обращении к {url}: {ex.Message}");
            }
        }

        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine($"Общее время работы: {stopwatch.ElapsedMilliseconds} мс");
    }

    // с async/await
    static async Task RunAsynchronousVersion()
    {
        string[] urls =
        {
            "https://jsonplaceholder.typicode.com/posts/1",
            "https://jsonplaceholder.typicode.com/users/1",
            "https://jsonplaceholder.typicode.com/todos/1"
        };

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Console.WriteLine();
        Console.WriteLine("===== Асинхронная версия =====");

        Task[] tasks = new Task[urls.Length];

        for (int i = 0; i < urls.Length; i++)
        {
            string currentUrl = urls[i];
            tasks[i] = FetchAndPrintAsync(currentUrl);
        }

        await Task.WhenAll(tasks);

        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine($"Общее время работы: {stopwatch.ElapsedMilliseconds} мс");
    }

    static async Task FetchAndPrintAsync(string url)
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка запроса к {url}");
                Console.WriteLine($"Код ответа сервера: {(int)response.StatusCode} {response.StatusCode}");
                return;
            }

            string json = await response.Content.ReadAsStringAsync();

            Console.WriteLine();
            Console.WriteLine($"Ответ от сервера: {url}");
            Console.WriteLine(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обращении к {url}: {ex.Message}");
        }
    }
}