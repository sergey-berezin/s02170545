using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Web;

namespace Test {
    class Program {
        static async System.Threading.Tasks.Task Main(string[] args) {
            HttpClient client = new HttpClient();
            byte[] data = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            string path = "http://localhost:5000/api/matchresult/" + HttpUtility.UrlEncode(Convert.ToBase64String(data));
            Console.WriteLine("http://localhost:5000/api/matchresult/" + Convert.ToBase64String(data));

            HttpResponseMessage response = await client.GetAsync(path);

            Console.WriteLine(response.StatusCode);
            Console.WriteLine(response.Content);
            string resp = await response.Content.ReadAsStringAsync();
            Console.WriteLine(resp);
            dynamic obj = JsonConvert.DeserializeObject(resp);
            obj.classId;
            obj.statistics;
        }
    }
}
