
namespace NetworkDiagnostics
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("-----------------" + DateTimeOffset.Now.ToString());

            NetworkChangeDetector.LogNetworkInfoWithoutDebounce();

            await HttpReq.Run("https://ds.testipv6.cn");

            await HttpReq.Run("https://www.qq.com");

            await HttpReq.Run("https://api.live.bilibili.com");

            await HttpReq.Run("https://live.bilibili.com");
        }
    }
}