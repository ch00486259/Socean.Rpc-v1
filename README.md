# Socean.Rpc
An efficient rpc framework,stable and efficient,the rps is about 110k on two core i5 notebook computer and it would be higher in 24 core device.

一个高效的rpc框架，框架特点是 稳定和 高效，在双核i5笔记本电脑上,每秒处理请求数（rps）在11w左右,理论上在24核服务器上应该会更高，不过没实际测试过

  
  
  -------------------------------------------------------------------
  server sample :

  1.定义实体
  
    public class Book
    {
        public string Name { get; set; }
    }
 
 --------------------------
 
 
  定义MessageProcessor
 
     public class DefaultMessageProcessor : IMessageProcessor
     {

          public ResponseBase Process(string title, byte[] contentBytes)
          {

              if (title == "book/name/change")
              {
                  var content = Encoding.UTF8.GetString(contentBytes);

                  //here we use newtonsoft.Json serializer 
                  //you need add refer "newtonsoft.Json.dll"
                  var book = JsonConvert.DeserializeObject<Book>(content);
                  book.Name = "new name";

                  var responseContent = JsonConvert.SerializeObject(book);
                  return new BytesResponse(Encoding.UTF8.GetBytes(responseContent));
              }

              if (title == "empty")
              {
                  return new EmptyResponse();
              }

              throw new Exception();
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
        using (var rpcClient = ShortConnectionRpcClientFactory.Create(IPAddress.Parse("127.0.0.1"), 11111))
        {
            var requestContent = JsonConvert.SerializeObject(book);
            var response = rpcClient.Query("book/name/change", Encoding.UTF8.GetBytes(requestContent));
            var content = Encoding.UTF8.GetString(response.ContentBytes);
            return JsonConvert.DeserializeObject<Book>(content);
        }
    }
    
  -------------------------------------------------------------------
  
  其他：
  
  NetworkSettings类可修改连接超时时间等参数
  
  若果要进行性能测试，最好是在客户端把NetworkSettings.ClientDetectReceiveInterval设置成1，并提升线程优先级至ThreadPriority.Highest
  
  
  Socean.Rpc.Core未来的变动会比较小，接下来会推出Socean.Rpc.Contract，以支持各种扩展，如MVC中的route等功能
  
  
  
  
  
  
