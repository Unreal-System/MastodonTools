using Mastodot;
using System.Net;
using System.Text;

namespace MastodonTools;

static class Program
{
    private static string ExportDir = AppDomain.CurrentDomain.BaseDirectory + "export/";
    public static MastodonClient Client;

    static void Main(string[] args)
    {
        #region Init App Client
        bool success = false;
        do
        {
            Console.WriteLine("Please Input Your Site Address (Like: test.mastodon.net (!DO NOT LIKE! https://test.mastodon.net/ !))");
            Console.Write(">");
            var siteUrl = Console.ReadLine();
            Console.WriteLine("Please Input Your App AccessToken");
            Console.Write(">");
            var appToken = Console.ReadLine();

            Client = new MastodonClient(siteUrl, appToken);
            try
            {
                var me = Client.GetCurrentAccount().Result;
                success = true;
            }
            catch (Exception ex)
            {
                Console.Clear();
                Console.WriteLine(ex.Message);
            }
        } while (success != true);
        success = false;
        Console.Clear();
        #endregion

        #region Search User & Print Statuses
        var uid = 1;
        do
        {
            Console.WriteLine("Please Input You Wanna Export Cacae Account Name");
            Console.Write(">");
            var name = Console.ReadLine();
            
            try
            {
                var result = Client.SearchAccount(name).Result;

                if (result.RawJson == "[]") { Console.WriteLine("No User Data!"); continue; }

                foreach (var i in result)
                {
                    Console.WriteLine($"UserId: {i.Id} DisplayName: {i.DisplayName} FullUserName: {i.FullUserName}");
                }

                Console.WriteLine("Please Input UserId");
                Console.Write(">");
                var input = Console.ReadLine();
                if (int.TryParse(input, out uid) != true) { Console.WriteLine("Input Can't Parse!"); continue; }

                var account = Client.GetAccount(uid).Result;
                var statuses = Client.GetStatuses(uid, true, false, limit: 10).Result;

                foreach (var i in statuses)
                {
                    Console.WriteLine($"MsgId: {i.Id} Date: {i.CreatedAt} MediaCount: {i.MediaAttachments.Count()} Context: {i.Content.Split("</p>")[0].Replace("<p>", "")}");
                }

                if (statuses.Links.Next != null)
                {
                    while (success != true && statuses.Links.Next != null)
                    {
                        Console.Write("Continue Print?Press Key(y/n) > ");
                        var yn = Console.ReadKey();
                        Console.WriteLine();

                        switch (yn.Key)
                        {
                            case ConsoleKey.Y:
                                statuses = Client.GetStatuses(uid, true, false, maxId: statuses.Links.Next, limit: 10).Result;

                                foreach (var i in statuses)
                                {
                                    Console.WriteLine($"MsgId: {i.Id} Date: {i.CreatedAt} MediaCount: {i.MediaAttachments.Count()} Context: {i.Content.Split("</p>")[0].Replace("<p>", "")}");
                                }
                                break;
                            case ConsoleKey.N: success = true; break;
                            default: Console.WriteLine("Input Error!"); break;
                        }
                    }
                    success = true;
                }
                else
                {
                    Console.WriteLine("No More Statuses!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        } while (success != true);
        success = false;
        #endregion

        #region Check File Is Not Found(404) (default not use)
        //do
        //{
        //    Console.WriteLine("Please Input MsgId To Check Is 404");
        //    Console.Write(">");
        //    var input = Console.ReadLine();
        //    if (long.TryParse(input, out long msgId) != true)
        //    {
        //        Console.WriteLine("Input Can't Parse!");
        //        continue;
        //    }
        //    else
        //    {
        //        try
        //        {
        //            var result = Client.GetStatus(msgId).Result;
        //            if (result.MediaAttachments.Count() > 0)
        //            {
        //                foreach (var i in result.MediaAttachments)
        //                {
        //                    Console.WriteLine($"Is 404: {CheckIs404(i.RemoteUrl)}");
        //                    break;
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.Message); continue;
        //        }
        //    }

        //    Console.Write("Continue Check?Press Key(y/n) > ");
        //    var yn = Console.ReadKey();
        //    Console.WriteLine();
        //    switch (yn.Key)
        //    {
        //        case ConsoleKey.Y: continue;
        //        case ConsoleKey.N: success = true; break;
        //        default: Console.WriteLine("Input Error!"); break;
        //    }
        //} while (success != true);
        //success = false;
        #endregion

        #region Set Range Of Export Data Time
        var startTime = DateTime.UnixEpoch;
        var endTime = DateTime.Today;
        do
        {
            Console.WriteLine("Please Input Start Date(yyyy/MM/dd HH:mm:ss)");
            Console.Write(">");
            var input = Console.ReadLine();
            if (DateTime.TryParse(input, out DateTime starTime) != true)
            {
                Console.WriteLine("Input Can't Parse!");
                continue;
            }
            else
            {
                startTime = starTime;
            }
            Console.WriteLine("Please Input Start Date(yyyy/MM/dd HH:mm:ss)");
            Console.Write(">");
            input = Console.ReadLine();
            if (DateTime.TryParse(input, out DateTime stopTime) != true)
            {
                Console.WriteLine("Input Can't Parse!");
                continue;
            }
            else
            {
                endTime = stopTime;
            }
            success = true;
        } while (success != true);
        success = false;
        #endregion

        #region Export Data
        do
        {
            Console.WriteLine();
            Console.WriteLine("Start Export Cache Dataing...");
            Console.WriteLine();
            try
            {
                if (Directory.Exists(ExportDir) != true) Directory.CreateDirectory(ExportDir);

                var statuses = Client.GetStatuses(uid, true, false, limit: 10).Result;
                foreach (var i in statuses)
                {
                    if (i.CreatedAt.Ticks <= startTime.Ticks && i.CreatedAt.Ticks >= endTime.Ticks)
                    {
                        if (i.MediaAttachments.Count() > 0)
                        {
                            foreach (var media in i.MediaAttachments)
                            {
                                var head = media.RemoteUrl.IndexOf("files/");
                                Console.WriteLine();
                                var body = media.RemoteUrl.LastIndexOf("/") + 1;
                                var paths = ExportDir + media.RemoteUrl.Substring(head, body - head);
                                var file = ExportDir + media.RemoteUrl.Substring(head);

                                if (Directory.Exists(paths) != true) Directory.CreateDirectory(paths);
                                if (File.Exists(file) != true) DownloadMedia(i.CreatedAt, media.Url, file);
                            }
                        }
                    }
                }

                while (statuses.Links.Next != null)
                {
                    statuses = Client.GetStatuses(uid, true, false, maxId: statuses.Links.Next, limit: 10).Result;
                    foreach (var i in statuses)
                    {
                        if (i.CreatedAt.Ticks < endTime.Ticks)
                        {
                            success = true; statuses.Links.Next = null; break;
                        }
                        if (i.CreatedAt.Ticks <= startTime.Ticks && i.CreatedAt.Ticks >= endTime.Ticks)
                        {
                            if (i.MediaAttachments.Count() > 0)
                            {
                                foreach (var media in i.MediaAttachments)
                                {
                                    var head = media.RemoteUrl.IndexOf("files/");
                                    Console.WriteLine();
                                    var body = media.RemoteUrl.LastIndexOf("/") + 1;
                                    var paths = ExportDir + media.RemoteUrl.Substring(head, body - head);
                                    var file = ExportDir + media.RemoteUrl.Substring(head);

                                    if (Directory.Exists(paths) != true) Directory.CreateDirectory(paths);
                                    if (File.Exists(file) != true) DownloadMedia(i.CreatedAt, media.Url, file);
                                }
                            }
                        }
                    }
                }
                success = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine("Retrying...");
                Console.WriteLine();
            }
        } while (success != true);
        #endregion

        Console.WriteLine("Finish!");
        Console.ReadLine();
    }

    //private static bool CheckIs404(string url)
    //{
    //    Console.WriteLine($"Checking: {url}");
    //    try
    //    {
    //        HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(url);
    //        webReq.Method = "GET";
    //        webReq.Headers.Add("Accept-Encoding", "gzip, deflate");
    //        webReq.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
    //        webReq.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";
    //        webReq.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

    //        using (var Response = (HttpWebResponse)webReq.GetResponse()) { using (var sr = new StreamReader(Response.GetResponseStream(), Encoding.UTF8)) { return false; } }
    //    } catch (Exception ex) { Console.WriteLine(ex.Message); return true; }
    //}

    private static void DownloadMedia(DateTime time, string sourceUrl, string downloadPath)
    {
		Thread.Sleep(500);
        if (File.Exists(downloadPath) == true)
        {
            Console.WriteLine($"SourceTime: {time} Downloading: {sourceUrl} Has Exist!");
        }
        else
        {
            Console.WriteLine($"SourceTime: {time} Downloading: {sourceUrl}");
            try
            {
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(sourceUrl);
                webReq.Method = "GET";
                webReq.Headers.Add("Accept-Encoding", "gzip, deflate");
                webReq.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
                webReq.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";
                webReq.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (var Response = (HttpWebResponse)webReq.GetResponse())
                {
                    using (var fs = File.OpenWrite(downloadPath))
                    {
                        using (var sr = new StreamReader(Response.GetResponseStream(), Encoding.UTF8))
                        {
                            sr.BaseStream.CopyTo(fs);
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
    }
}
