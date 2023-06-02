using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BrojacReciWebServer
{
    class Program
    {
        static readonly object cacheLock = new object();
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();

        static async Task Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5050/");
            listener.Start();
            Console.WriteLine("Listening...");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                Task.Run(() => ObradaZahteva(context));
            }
        }

        static async Task ObradaZahteva(HttpListenerContext context)
        {
            string zahtevUrl = context.Request.Url.LocalPath;
            Console.WriteLine("Zahtev primljen: " + zahtevUrl);

            string odgovor = "";

            lock (cacheLock)
            {
                if (cache.ContainsKey(zahtevUrl))
                {
                    odgovor = cache[zahtevUrl];
                    Console.WriteLine("Odgovor iz cache-a");
                }
            }

            if (odgovor == "")
            {
                string absolutePath = Path.GetFullPath(".");
                string directoryPath = Path.GetDirectoryName(absolutePath);
                string parentDirectoryPath = Directory.GetParent(directoryPath).ToString();
                string rootFolder = Directory.GetParent(parentDirectoryPath).ToString();

                string filename = context.Request.Url.Segments.Last();
                string putanjaF1 = Directory.GetFiles(rootFolder, filename, SearchOption.AllDirectories).FirstOrDefault();
                string putanjaF = Path.GetDirectoryName(putanjaF1) + zahtevUrl.Replace('/', '\\');

                if (!File.Exists(putanjaF))
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                    Console.WriteLine("Fajl nije pronadjen: " + putanjaF);
                    return;
                }

                string sadrzajFajla = await File.ReadAllTextAsync(putanjaF);
                string[] words = sadrzajFajla.Split(new char[] { ' ', '\t', '\n', '\r', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                int i = 0;
                foreach (string word in words)
                {
                    if (word.Length > 5 && char.IsUpper(word[0]))
                    {
                        Console.WriteLine(word);
                        i++;
                    }
                }
                odgovor = "Broj reci sa velikim pocetnim slovom koje su duze od 5 karaktera je: " + i;
                lock (cacheLock)
                {
                    cache[zahtevUrl] = odgovor;
                    Console.WriteLine("Odgovor kesiran");
                }
            }

            byte[] buffer = Encoding.UTF8.GetBytes(odgovor);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();
            Console.WriteLine("Zahtev obradjen");
        }
    }
}
