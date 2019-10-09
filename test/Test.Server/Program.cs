using Socean.Rpc.Core;
using Socean.Rpc.Core.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test.Server
{
    class Program
    {
        private static volatile RpcServer _rpcServer;

        static void Main(string[] args)
        {
            GC.AddMemoryPressure(55 * 1024 * 1024);

            var threadPoolSize = 20;

            ThreadPool.SetMaxThreads(threadPoolSize, threadPoolSize);
            ThreadPool.SetMinThreads(threadPoolSize, threadPoolSize);

            WriteMessage(string.Format("thread pool size:{0}", threadPoolSize));

            LogAgent.LogAction = (level, message, ex) =>
            {
                if (level != LogLevel.Error)
                    return;

                WriteMessage(message);

                if (ex != null)
                    WriteMessage(ex.Message);
            };

            int port = 7777;

            _rpcServer = new RpcServer();
            _rpcServer.Bind(IPAddress.Any, port);
            _rpcServer.MessageProcessor = new CustomMessageProcessor();

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _rpcServer.Start();
                    WriteMessage(string.Format("server started,port:{0}", port)) ;
                }
                catch (Exception ex)
                {
                    _rpcServer = null;
                    WriteMessage("server start failed");
                }
            },TaskCreationOptions.LongRunning);


            while (true)
            {
                if (Console.ReadLine() == "exit")
                    break;
            }
        }

        private static void WriteMessage(string message)
        {
            Console.WriteLine("[{0}] {1}",DateTime.Now.ToString("HH:mm:ss.fff"), message);
        }
    }


    public class CustomMessageProcessor : IMessageProcessor
    {
        public Task<ResponseBase> Process(Socean.Rpc.Core.Message.FrameData frameData)
        {
            ResponseBase response = (new CustomResponse(frameData.HeaderExtentionBytes, frameData.ContentBytes, (byte)ResponseCode.OK));

            return Task.FromResult(response);
        }
    }
}
