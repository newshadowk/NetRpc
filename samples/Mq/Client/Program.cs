using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Proxy.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Helper = TestHelper.Helper;

namespace Client;

internal class Program
{
    //private static IServiceAsync _proxyAsync;

    private static async Task Main(string[] args)
    {
        await T1();
        Console.Read();
    }

    private static async Task T1()
    {
        var services = new ServiceCollection();
        services.AddNClientContract<IServiceAsync>("a1");
        services.AddLogging(configure => configure.AddConsole());

        services.AddNGrpcClient();
        services.AddNRabbitMQClient();
        services.Configure<MQClientOptions>("a1", o => o.CopyFrom(Helper.GetMQOptions()));
        //services.Configure<MQClientOptions>(o => o.CopyFrom(Helper.GetMQOptions()));

        var sp = services.BuildServiceProvider();
        var s = sp.GetService<IServiceAsync>();

        var fs = File.OpenRead(@"D:\TestFile\10MB.db");


        try
        {
            Console.WriteLine("send start");
            var r = await s.ComplexCallAsync(new CustomObj(), fs, null, CancellationToken.None);
            Console.WriteLine("send end");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        //await DoT(sp);
    }

    private static async Task T0()
    {
        var f = Helper.GetMQOptions().CreateMainConnectionFactory();

        var c = (IAutorecoveringConnection)f.CreateConnection();
        c.CallbackException += C_CallbackException;
        c.ConnectionBlocked += C_ConnectionBlocked;
        c.ConnectionShutdown += C_ConnectionShutdown;
        c.ConnectionUnblocked += C_ConnectionUnblocked;

        var ch = c.CreateModel();
        ch.CallbackException += Ch_CallbackException;
        ch.BasicRecoverOk += Ch_BasicRecoverOk;
        ch.BasicAcks += Ch_BasicAcks;
        ch.BasicNacks += Ch_BasicNacks;
        ch.BasicReturn += Ch_BasicReturn;
        ch.FlowControl += Ch_FlowControl;
        ch.ModelShutdown += Ch_ModelShutdown;

        //var qName = "rpc_test2";
        //ch.QueueDeclare(qName, exclusive:false, autoDelete:false);
        //var consumer = new AsyncEventingBasicConsumer(ch);
        //consumer.Received += async (_, e) =>
        //{
        //    Console.WriteLine($"r0, {e.DeliveryTag}");
        //    ch.BasicNack(e.DeliveryTag, false, false);
        //};

        //ch.BasicConsume(qName, false, consumer);
    }

    private static async Task T2()
    {
        var f = Helper.GetMQOptions().CreateMainConnectionFactory();
        var c = f.CreateConnection();
        var ch = c.CreateModel();

        var qn = ch.QueueDeclare("rpc_test", false, false).QueueName;

        const long Size = 81920;
        var rawData = new byte[Size];
        Random.Shared.NextBytes(rawData);

        Console.ReadLine();
        Console.WriteLine("send start");

        for (var i = 0; i < 10 * 1000; i++)
        {
            ch.BasicPublish("", qn, null, rawData);
        }

        Console.WriteLine("pub end");
        Console.ReadLine();

        Console.ReadLine();

        GC.Collect();
        Console.WriteLine("GC.Collect();");
        Console.ReadLine();
        try
        {
            Console.WriteLine("close start");
            ch.Close();
            GC.Collect();
            Console.WriteLine("ch.Close();");
            Console.ReadLine();
            c.Close();
            GC.Collect();
            Console.WriteLine("c.Close();");
            Console.ReadLine();
            GC.Collect();
            Console.WriteLine("GC.Collect();");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }


        Console.WriteLine("send end");
    }

    //private static async Task T4()
    //{
    //    var f = Helper.GetMQOptions().CreateMainConnectionFactory();
    //    var c = f.CreateConnection();
    //    var ch = c.CreateModel();

    //    ch.BasicNacks += (s, e) =>
    //    {
    //        Console.WriteLine($"nack, {e.DeliveryTag}");
    //    };
    //    ch.BasicAcks += (s, e) =>
    //    {
    //        Console.WriteLine($"ack, {e.DeliveryTag}");
    //    };
    //    ch.BasicReturn += (s, e) =>
    //    {
    //        Console.WriteLine($"BasicReturn");
    //    };
    //    ch.FlowControl += (s, e) =>
    //    {
    //        Console.WriteLine($"BasicReturn");
    //    };

    //    ch.ExchangeDeclare("e1", );
    //    ch.QueueDeclare(QueueName, QueueDurable, QueueExclusive, QueueDelete, null);
    //    ch.QueueBind(QueueName, ExchangeName, RoutingKey);
    //}

    private static async Task T3()
    {
        var qName = "rpc_test2";
        var f = Helper.GetMQOptions().CreateMainConnectionFactory();
        var c = f.CreateConnection();
        var ch = c.CreateModel();

        ch.BasicNacks += (s, e) => { Console.WriteLine($"nack, {e.DeliveryTag}"); };
        ch.BasicAcks += (s, e) => { Console.WriteLine($"ack, {e.DeliveryTag}"); };
        ch.BasicReturn += (s, e) => { Console.WriteLine("BasicReturn"); };
        ch.FlowControl += (s, e) => { Console.WriteLine("BasicReturn"); };

        ch.BasicQos(0, 0, true);
        var consumer = new AsyncEventingBasicConsumer(ch);
        consumer.Received += async (_, e) =>
        {
            Console.WriteLine($"r0, {e.DeliveryTag}");
            ch.BasicNack(e.DeliveryTag, false, false);
        };
        ch.BasicConsume(qName, false, consumer);

        const long Size = 10;
        var rawData = new byte[Size];
        Random.Shared.NextBytes(rawData);

        var i = 1;
        while (true)
        {
            Console.WriteLine($"send {i}");
            ch.BasicPublish("", qName, false, null, rawData);
            await Task.Delay(2000);
            i++;
        }
    }


    private static void Ch_ModelShutdown(object sender, ShutdownEventArgs e)
    {
        Console.WriteLine("Ch_ModelShutdown");
    }

    private static void Ch_FlowControl(object sender, FlowControlEventArgs e)
    {
        Console.WriteLine("Ch_FlowControl");
    }

    private static void Ch_BasicReturn(object sender, BasicReturnEventArgs e)
    {
        Console.WriteLine("Ch_BasicReturn");
    }

    private static void Ch_BasicNacks(object sender, BasicNackEventArgs e)
    {
        Console.WriteLine("Ch_BasicNacks");
    }

    private static void Ch_BasicAcks(object sender, BasicAckEventArgs e)
    {
        Console.WriteLine("Ch_BasicAcks");
    }

    private static void Ch_BasicRecoverOk(object sender, EventArgs e)
    {
        Console.WriteLine("Ch_BasicRecoverOk");
    }

    private static void Ch_CallbackException(object sender, CallbackExceptionEventArgs e)
    {
        Console.WriteLine("Ch_CallbackException");
    }

    private static void C_ConnectionUnblocked(object sender, EventArgs e)
    {
        Console.WriteLine("C_ConnectionUnblocked");
    }

    private static void C_ConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        Console.WriteLine("C_ConnectionShutdown");
    }

    private static void C_ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        Console.WriteLine("C_ConnectionBlocked");
    }

    private static void C_CallbackException(object sender, CallbackExceptionEventArgs e)
    {
        Console.WriteLine("C_CallbackException");
    }

    private static async Task DoT(IServiceProvider sp)
    {
        var i = 0;
        var cts = new CancellationTokenSource();
        while (true)
        {
            using var serviceScope = sp.CreateScope();
            var s = serviceScope.ServiceProvider.GetService<IServiceAsync>();

            await Task.Delay(3000);
            try
            {
                //await Test_ComplexCallAsync(s, i++, cts.Token);
                await Test_Call2(s, i++);
            }
            catch (Exception e)
            {
                Console.WriteLine($"error, {e.Message}");
            }

            //GC.Collect();
        }
    }

    private static async Task Test_Call2(IServiceAsync s, int i)
    {
        Console.WriteLine($"send {i}");
        await s.Call2(i.ToString());
        Console.WriteLine("end");
    }

    private static Stream _stream;
    private static byte[] _rawData;

    private static Stream GetSteam()
    {
        if (_rawData != null)
        {
            _stream = new MemoryStream(_rawData);
            return _stream;
        }

        const long Size = 200 * 1024 * 1024;
        _rawData = new byte[Size];
        Random.Shared.NextBytes(_rawData);

        _stream = new MemoryStream(_rawData);
        return _stream;
    }

    private static async Task Test_ComplexCallAsync(IServiceAsync service, int i, CancellationToken t)
    {
        using (var stream = GetSteam())
            //using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            Console.Write("[ComplexCallAsync]...Send TestFile.txt...");
            var complexStream = await service.ComplexCallAsync(
                new CustomObj { Date = DateTime.Now, Name = "ComplexCall" + i },
                stream,
                async i => Console.Write(", " + i.Progress),
                t);

            using (complexStream.Stream)
            {
                Console.Write($", receive length:{stream.Length}");
                MemoryStream ms = new();
                try
                {
                    await complexStream.Stream.CopyToAsync(ms);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
        }
    }
}