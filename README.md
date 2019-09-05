# Socean.Rpc
 
一个高效的rpc框架，框架特点是稳定和高效，在双核i5笔记本电脑上测试，长连接模式下每秒处理请求数11w左右，短连接模式下每秒处理请求数5000左右

本框架性能可满足部分unity3d服务器端的需求，普通电脑上测试，可支持10000长连接，每秒10个请求（每100毫秒1个请求 ），在性能好的服务器上，20000长连接且每秒20个请求应该是没问题的
  
  
  -------------------------------------------------------------------
  server sample :

  1.定义实体
  
    public class Book
    {
        public string Name { get; set; }
    }
 
 
 
  定义MessageProcessor
 
     public class DefaultMessageProcessor : IMessageProcessor
     {

          public async Task<ResponseBase> Process(Socean.Rpc.Core.Message.FrameData frameData)
          {
              var title = Encoding.UTF8.GetString(frameData.TitleBytes);
              if (title == "/books/namechange")
              {
                  var content = Encoding.UTF8.GetString(frameData.ContentBytes);

                  //here we use newtonsoft.Json serializer 
                  //you need add refer "newtonsoft.Json.dll"
                  var book = JsonConvert.DeserializeObject<Book>(content);
                  book.Name = "new name";

                  var responseContent = JsonConvert.SerializeObject(book);
                  return new BytesResponse(Encoding.UTF8.GetBytes(responseContent));
              }

              if (title == "test return empty")
              {
                  return new EmptyResponse();
              }

              return new ErrorResponse(ResponseCode.SERVICE_NOT_FOUND);
          }
      }


  2.启动服务
  
    var server = new KeepAliveRpcServer();
    server.Bind(IPAddress.Any, 11111);
    server.AutoReconnect = true;
    server.MessageProcessor = new DefaultMessageProcessor();

    server.Start();  
  
  -------------------------------------------------------------------

  client sample:
  
  1.定义实体
  
    public class Book
    {
        public string Name { get; set; }
    }
 
 
  2.执行调用
  
    public Book ChangeBookName(Book book)
    {
        using (var rpcClient = new FastRpcClient(IPAddress.Parse("127.0.0.1"), 11111))
        {
            var requestContent = JsonConvert.SerializeObject(book);
            var response = rpcClient.Query(Encoding.UTF8.GetBytes("/books/namechange"), Encoding.UTF8.GetBytes(requestContent));
            var content = Encoding.UTF8.GetString(response.ContentBytes);
            return JsonConvert.DeserializeObject<Book>(content);
        }
    }
    
  -------------------------------------------------------------------
  
  其他：
  
  NetworkSettings类可修改连接超时时间等参数
  
  性能测试建议(load test)：

  1.压力测试时客户端需将NetworkSettings.ClientDetectReceiveInterval设置成1，可提高客户端的收发效率
  
  2.提升线程优先级至ThreadPriority.Highest
  
  3.客户端并发线程数最好是100至200之间
  
  4.压力测试的客户端最好用win7系统，因为win7的时间片精度有时能达到1ms
  
  5.最好是多机测试 
 
  
  
  
  
  
  
