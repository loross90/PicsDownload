using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Drawing;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GetImages
{
    public abstract class Request
    {
        public string RequestAddress { get; }

        private string Prefix;

        protected string Response { get; private set; }

        protected dynamic jsonString { get; private set; }

        protected Request(string provider)
        {
            switch (provider)
            {
                case "Nasa":
                    RequestAddress = "https://api.nasa.gov/planetary/apod?api_key=DEMO_KEY";
                    Prefix = provider;
                    break;
                case "Bing":
                    RequestAddress = "http://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=EN-IN";
                    Prefix = provider;
                    break;
                default:
                    throw new Exception();
            }


        }

        public void GetInformationObject()
        {
            var RequestObject = WebRequest.Create(RequestAddress);
            RequestObject.Proxy = WebRequest.DefaultWebProxy;
            RequestObject.Credentials = CredentialCache.DefaultCredentials;
            RequestObject.Proxy.Credentials = CredentialCache.DefaultCredentials;

            var resp = RequestObject.GetResponse();
            Stream receiveStream = resp.GetResponseStream();

            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
            Response = "";
            while (readStream.Peek() >= 0)
            {
                char[] b = new char[1];
                readStream.Read(b, 0, 1);
                Response = Response + b[0];
            }

            jsonString = JsonConvert.DeserializeObject(Response);
            resp.Close();
            readStream.Close();
        }

        public abstract byte[] GetImageBytes();

        public void SavePic(byte[] data)
        {
            using (MemoryStream mem = new MemoryStream(data))
            {
                using (Image image = Image.FromStream(mem))
                {
                    image.Save(AppContext.BaseDirectory + Prefix + DateTime.Now.ToString("yyyy-MM-dd-hhmm-ss-fff") + ".jfif");
                }
            }
        }
    }

    public class BingRequest : Request
    {
        public BingRequest() : base("Bing")
        {

        }

        public override byte[] GetImageBytes()
        {
            byte[] data = null;

            using (WebClient client = new WebClient())
            {
                data = client.DownloadData("http://www.bing.com" + jsonString.images[0].url);
            }

            return data;
        }
    }

    public class NasaRequest : Request
    {
        public NasaRequest() : base("Nasa")
        {

        }

        public override byte[] GetImageBytes()
        {
            byte[] data = null;

            using (WebClient client = new WebClient())
            {
                string url = jsonString.hdurl.ToString();
                data = client.DownloadData(url.Replace("https", "http"));
            }
            return data;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Task[] DownloasTasks = new Task[2]
            {
                new Task(() =>
                {
                    BingRequest Bing = new BingRequest();
                    Bing.GetInformationObject();
                    Bing.SavePic(Bing.GetImageBytes());
                }),
                new Task(() =>
                {
                    NasaRequest Nasa = new NasaRequest();
                    Nasa.GetInformationObject();
                    Nasa.SavePic(Nasa.GetImageBytes());
                }),
            };

            foreach (var t in DownloasTasks)
                t.Start();
            Task.WaitAll(DownloasTasks);

            Console.WriteLine("Загрузки завершены.");
        }
    }
}
