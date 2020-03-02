using Socean.Rpc.Core;
using Socean.Rpc.Core.Client;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test.Client
{
    class Program
    {
        static int _threadCount = 1;
        static int _loopCount = 999999999;
        static IPAddress _ip = IPAddress.Parse("127.0.0.1");
        static int _port = 7777;
        static int _messageLength = 3;
        static int _totalCount = 0;
        static byte[][] _titleBytesArray = new byte[10][];
        static byte[][] _messageBytesArray = new byte[10][];

        static void Main(string[] args)
        {
            NetworkSettings.ReceiveTimeout = 10000;
            NetworkSettings.SendTimeout = 10000;
            NetworkSettings.HighResponse = true;
            NetworkSettings.ClientCacheSize = 100;

            LogAgent.LogAction = (level, message, ex) =>
            {
                if (level != LogLevel.Error)
                    return;

                WriteMessage(message);

                if (ex != null)
                    WriteMessage(ex.Message);
            };

            Console.WriteLine("please input test mode(0 or 1):");
            Console.WriteLine("0----[load test]");
            Console.WriteLine("1----[function test]");
            var cmd = Console.ReadLine();
            switch (cmd)
            {
                case "0":
                    {
                        StartLoadTest();
                        break;
                    }
                case "1":
                    {
                        StartFunctionTest();
                        break;
                    }
                default:
                    {
                        return;
                    }
            }

            while (true)
            {
                if (Console.ReadLine() == "exit")
                    break;
            }
        }

        static int[] titleTestLengthArray = new int[] { 10, 100, 600, 4000,15000 };
        static int[] extentionTestLengthArray = new int[] { 0,10, 100, 600 ,4000,15000};

        private static void StartFunctionTest()
        {
            Console.WriteLine("");
            WriteMessage(string.Format("start function test"));
            WriteMessage(string.Format("ip:{2},port:{3}", _threadCount, _messageLength, _ip, _port));

            for (var i=0; i< titleTestLengthArray.Length; i++)
            {
                for (var j = 0; j < extentionTestLengthArray.Length; j++)
                {
                    RunStartFunctionTest(titleTestLengthArray[i], extentionTestLengthArray[j]).Wait();
                }
            }

            WriteMessage("function test complete!");
        }

        private static async Task RunStartFunctionTest(int titleLength,int extentionLength)
        {
            WriteMessage(string.Format("run new function test"));
            WriteMessage(string.Format("test title length:{0},test extention length:{1}", titleLength, extentionLength));

            int testCount = 0;
            int percentage = 0;
            int maxTestLength = 20000;

            for (var i = 0; i < maxTestLength; i++)
            {
                using (var rpcClient = CreateClient(_ip, _port))
                {
                    var fillingChar = i.ToString().Last();

                    var title = "".PadLeft(titleLength, fillingChar);
                    var extention = "".PadLeft(extentionLength, fillingChar);
                    var sendMessage = "".PadLeft(i, fillingChar);

                    var titleBytes = Encoding.UTF8.GetBytes(title);
                    var extentionBytes = Encoding.UTF8.GetBytes(extention);
                    var messageBytes = Encoding.UTF8.GetBytes(sendMessage);
                   
                    var syncReceive = rpcClient.Query(titleBytes, messageBytes, extentionBytes);

                    if (Encoding.UTF8.GetString(syncReceive.ContentBytes) != sendMessage)
                        throw new Exception();

                    if (Encoding.UTF8.GetString(syncReceive.HeaderExtentionBytes) != extention)
                        throw new Exception();

                    var asyncReceive = await rpcClient.QueryAsync(titleBytes, messageBytes, extentionBytes);

                    if (Encoding.UTF8.GetString(asyncReceive.ContentBytes) != sendMessage)
                        throw new Exception();

                    if (Encoding.UTF8.GetString(asyncReceive.HeaderExtentionBytes) != extention)
                        throw new Exception();
                }

                testCount++;

                if (testCount * 10 / maxTestLength != percentage)
                {
                    percentage = testCount * 10 / maxTestLength;
                    WriteMessage(percentage + "0%");
                }
            }
        }

        private static void StartLoadTest()
        {
            Console.WriteLine("");
            WriteMessage(string.Format("start load test"));
            WriteMessage(string.Format("thread count:{0},", _threadCount));
            WriteMessage(string.Format("message length:{0}",  _messageLength));
            WriteMessage(string.Format("ip:{0},port:{1}", _ip, _port));

            StartMonite();

            GenerateMessage();

            for (var i = 0; i < _threadCount; i++)
            {
                Task.Factory.StartNew(async () =>
                {
                    for (var j = 0; j < _loopCount; j++)
                    {
                        try
                        {
                            await RunLoadTest(_ip, _port, 50);
                        }
                        catch (Exception ex)
                        {
                            WriteMessage(ex.Message);
                            Thread.Sleep(1000);
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }

        private static int GetRandomMessageIndex(int seed)
        {
            return seed % 10;
        }

        private static IClient CreateClient(IPAddress ip, int port)
        {
            return new FastRpcClient(ip, port);
        }

        private static void GenerateMessage()
        {
            for (var i=0; i<10;i++)
            {
                var _sendMessage = "".PadLeft(_messageLength, i.ToString()[0]);
                _messageBytesArray[i] = Encoding.UTF8.GetBytes(_sendMessage);
                _titleBytesArray[i] = Encoding.UTF8.GetBytes("111");
            }
        }

        private static void StartMonite()
        {
            Task.Factory.StartNew(() => {
                var intervalSecond = 2;

                while (true)
                {
                    var processCount = Interlocked.Exchange(ref RpcDebugger.ProcessCount, 0);
                    var processTime = Interlocked.Exchange(ref RpcDebugger.ProcessTime, 0);

                    _totalCount += processCount;

                    processCount = processCount / intervalSecond;
                    processTime = processTime / intervalSecond;
                    if (processCount == 0)
                        processCount = 1;

                    var pt = processTime * 1.0d / processCount;

                    WriteMessage(string.Format("[thread_count:{0}][rps:{1}][process time:{2}ms]", _threadCount, processCount, pt.ToString("f3")));

                    Thread.Sleep(intervalSecond * 1000);
                }
            }, TaskCreationOptions.LongRunning);
        }
       
        private static async Task RunLoadTest(IPAddress ip, int port, int loopCount = 20)
        {
            var startTime = DateTime.Now;

            using (var rpcClient = CreateClient(ip, port))
            {
                for (var i = 0; i < loopCount; i++)
                {
                    var index = GetRandomMessageIndex(startTime.Second);

                    var receive = rpcClient.Query(_titleBytesArray[index], _messageBytesArray[index]);
                    //var receive = await rpcClient.QueryAsync(_titleBytesArray[index], _messageBytesArray[index]);
                }
            }

            var time = (int)(DateTime.Now - startTime).TotalMilliseconds;
            Interlocked.Add(ref RpcDebugger.ProcessTime, time);
            Interlocked.Add(ref RpcDebugger.ProcessCount, loopCount);
        }

        private static void WriteMessage(string message)
        {
            Console.WriteLine("[{0}]{1}",DateTime.Now.ToString("HH:mm:ss.fff"), message);
        }
    }

    public class RpcDebugger
    {
        public static volatile int ProcessCount;

        public static long ProcessTime;

        public static void Clear()
        {
            ProcessCount = 0;
            ProcessTime = 0;
        }
    }
}
