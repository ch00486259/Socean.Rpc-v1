using Socean.Rpc.Core;
using Socean.Rpc.Core.Client;
using Socean.Rpc.Core.Server;
using System;
using System.Collections.Generic;
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
        static int _loopCount = 99999999;
        static IPAddress _ip = IPAddress.Parse("127.0.0.1");
        static int _port = 7777;
        static int _messageLength = 3;
        static byte[][] _titleBytesArray = new byte[10][];
        static byte[][] _messageBytesArray = new byte[10][];
        static int _totalCount = 0;

        static void Main(string[] args)
        {
            //GC.AddMemoryPressure(55 * 1024 * 1024);

            //var threadPoolSize = 20;

            //ThreadPool.SetMaxThreads(threadPoolSize, threadPoolSize);
            //ThreadPool.SetMinThreads(threadPoolSize, threadPoolSize);

            NetworkSettings.ReceiveTimeout = 10000;
            NetworkSettings.SendTimeout = 10000;
            //NetworkSettings.LoadTest = true;
            NetworkSettings.ClientDetectReceiveInterval = 1;
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

        private static void StartFunctionTest()
        {
            WriteMessage(string.Format("start function test"));
            WriteMessage(string.Format("ip:{2},port:{3}", _threadCount, _messageLength, _ip, _port));
        }

        private static void StartLoadTest()
        {
            WriteMessage(string.Format("start load test"));
            WriteMessage(string.Format("thread count:{0},message_length:{1},ip:{2},port:{3}", _threadCount, _messageLength, _ip, _port));

            StartMonite();

            GenerateMessage();

            for (var i = 0; i < _threadCount; i++)
            {
                var thread = new Thread(() => {
                    for (var j = 0; j < _loopCount; j++)
                    {
                        try
                        {
                            Run(_ip, _port, 20);
                        }
                        catch (Exception ex)
                        {
                            WriteMessage(ex.Message);
                            Thread.Sleep(1000);
                        }
                    }
                });
                thread.IsBackground = true;
                thread.Priority = ThreadPriority.Highest;
                thread.Start();
            }
        }

        private static int GetRandomMessageIndex()
        {
            return DateTime.Now.Second % 10;
        }

        private static IClient CreateClient(IPAddress ip, int port)
        {
            //if(TestConfig.ConnectionType == 0)
            return new FastRpcClient(ip, port);
            //else
            //    return new SimpleRpcClient(ip, port);
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
                    var processCount = Interlocked.Exchange(ref RpcServerDebuger.ProcessCount, 0);
                    var processTime = Interlocked.Exchange(ref RpcServerDebuger.ProcessTime, 0);

                    _totalCount += processCount;

                    processCount = processCount / intervalSecond;
                    processTime = processTime / intervalSecond;
                    if (processCount == 0)
                        processCount = 1;

                    WriteMessage(string.Format("[total:{0}][rps:{1}][process time:{2}ms]",_totalCount, processCount, (processTime*1.0d/ processCount).ToString("f3")));

                    Thread.Sleep(intervalSecond * 1000);
                }
            }, TaskCreationOptions.LongRunning);
        }
       

        private static void Run(IPAddress ip, int port, int loopCount = 20)
        {
            var startTime = DateTime.Now;

            using (var rpcClient = CreateClient(ip, port))
            {
                for (var i = 0; i < loopCount; i++)
                {
                    var index = 0;//GetRandomMessageIndex();

                    var receive = rpcClient.Query(_titleBytesArray[index], _messageBytesArray[index]);

                    //var task = rpcClient.QueryAsync(_lastTitleBytes, _lastMessageBytes);
                    //task.Wait();

                    //var receiveMessage = Encoding.UTF8.GetString(receive.ContentBytes);
                    //if (receiveMessage != _sendMessage)
                    //    throw new Exception("消息错误");
                }
            }

            var time = (int)(DateTime.Now - startTime).TotalMilliseconds;
            Interlocked.Add(ref RpcServerDebuger.ProcessTime, time);
            Interlocked.Add(ref RpcServerDebuger.ProcessCount, loopCount);
        }

        private static void WriteMessage(string message)
        {
            Console.WriteLine("[{0}]{1}",DateTime.Now.ToString("HH:mm:ss.fff"), message);
        }
    }

    public class RpcServerDebuger
    {
        //public static volatile int connectCount;
        //public static RpcServer RpcServer { get; set; }

        public static volatile int ProcessCount;

        public static long ProcessTime;

        public static void Clear()
        {
            //connectCount = 0;
            ProcessCount = 0;
            ProcessTime = 0;
        }
    }
}
