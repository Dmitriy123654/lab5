using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Channels;

class Program
{
    private static TcpListener listener;
    private static bool productListSent = false;
    private static string PathOfProductFile = "D:\\SSP\\lab5\\lab_5_server\\products2.txt";

    static void Main()
    {
        IPAddress ipAddress = IPAddress.Parse("192.168.56.1");
        int port = 8080;

        listener = new TcpListener(ipAddress, port);
        listener.Start();
        Console.WriteLine("Сервер запущен...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Task.Run(() => HandleClient(client));

        }

    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();

        string clientInfo = client.Client.RemoteEndPoint.ToString();
        Console.WriteLine("Подключен клиент " + clientInfo);
        productListSent = false;
        // Отправляем список товаров клиенту, только если он еще не был отправлен
        /*if (!productListSent)
        {
            string[] products = GetProductNames();
            string productList = string.Join(",", products);
            byte[] productListBytes = Encoding.UTF8.GetBytes(productList);
            stream.Write(productListBytes, 0, productListBytes.Length);
            productListSent = true;
        }*/
        if (!productListSent)
        {
            string[] products = GetProductNames();

            if (products != null)
            {
                string productList = string.Join(",", products);
                byte[] productListBytes = Encoding.UTF8.GetBytes(productList);
                stream.Write(productListBytes, 0, productListBytes.Length);
                productListSent = true;
            }
            else
            {
                // Отправляем сообщение об ошибке клиенту
                string errorMessage = "Ошибка получения списка товаров.";
                byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                stream.Write(errorBytes, 0, errorBytes.Length);
                productListSent = true;
            }
        }

        byte[] buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Получен запрос от клиента: " + clientInfo + "\nТовар: " + request);

            if (request.Equals("closing"))
            {
                productListSent = false;
                break;
            }
            if (request.Length == 0)
            {
                continue;
            }

            string response = GetProductPrice(request);
            byte[] data = Encoding.UTF8.GetBytes(response);
            stream.Write(data, 0, data.Length);
            Console.WriteLine("Отправлен ответ клиенту: " + response);

            LogRequest(request, response);
        }

        // Закрываем соединение
        client.Close();
        Console.WriteLine("Соединение с клиентом " + clientInfo + " закрыто.");
    }

    static string[] GetProductNames()
    {
        try
        {
            if (!File.Exists(PathOfProductFile))
            {
                throw new FileNotFoundException("Файл с товарами не найден.");
            }

            string[] lines = File.ReadAllLines(PathOfProductFile);
            string[] productNames = new string[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(':');
                productNames[i] = parts[0].Trim();
            }

            return productNames;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}");
        }

        return null;
    }

    static string GetProductPrice(string productName)
    {
        string[] lines = File.ReadAllLines(PathOfProductFile);

        foreach (string line in lines)
        {
            string[] parts = line.Split(':');

            if (parts.Length > 1 && parts[0].Trim() == productName)
            {
                return parts[1].Trim();
            }
        }

        return "Цена для " + productName + " не найдена";
    }

    static void LogRequest(string request, string response)
    {
        string filePath = "requests.log";

        try
        {
            if (!File.Exists(filePath) || !Directory.Exists(filePath))
            {
                using (StreamWriter writer = File.CreateText(filePath))
                {
                    writer.WriteLine("Создан новый журнал запросов:");
                }
            }

            using (StreamWriter writer = File.AppendText(filePath))
            {
                writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - Запрос: " + request + "\nОтвет: " + response);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine("Ошибка доступа к файлу: " + ex.Message);
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.WriteLine("Папка не найдена: " + ex.Message);
        }
        catch (IOException ex)
        {
            Console.WriteLine("Ошибка ввода-вывода: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
        }
    }
}