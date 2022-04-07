using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetRpc;
using NetRpc.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Helper = TestHelper.Helper;

namespace Client;

internal class Program
{
    private static IServiceAsync _proxyAsync;

    private static async Task Main(string[] args)
    {
        await T1();
        Console.Read();
    }

    private static async Task T1()
    {
        var services = new ServiceCollection();
        services.AddNClientContract<IServiceAsync>();
        services.AddLogging(configure => configure.AddConsole());
        services.AddNRabbitMQClient(o => o.CopyFrom(Helper.GetMQOptions()));
        var sp = services.BuildServiceProvider();
        _proxyAsync = sp.GetService<IClientProxy<IServiceAsync>>()!.Proxy;

        try
        {
            var s  = await _proxyAsync.Call2("123");
            Console.WriteLine($"ret:{s}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        //try
        //{
        //    await _proxyAsync.Call2("456");
        //}
        //catch (Exception e)
        //{
        //    Console.WriteLine(e);
        //    throw;
        //}

        //await Task.Delay(1000);

        //Console.WriteLine("ReadLine");
        //Console.ReadLine();

        DoT();
    }

    private static async Task T0()
    {
        var f = Helper.GetMQOptions().CreateConnectionFactory();

        var c = f.CreateConnection();
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
        var qName = "rpc_test2";
        //var q = ch.QueueDeclare(qName, false, false, false, null);
        var q = ch.QueueDeclare();
        
        //int ii = 0;
        //while (true)
        //{
        //    ii++;
        //    Console.WriteLine(ii);
        //    ch.QueueDeclarePassive(qName);
        //}

        //var i = 0;
        //while (true)
        //{
        //    //Console.ReadLine();
        //    i++;
        //    Console.WriteLine($"{qName} {i}");
        //    try
        //    {
        //        ch.BasicPublish("", qName, true, null, Encoding.UTF8.GetBytes(i.ToString()));
        //    }
        //    catch
        //    {
        //        Console.WriteLine($"send err {i}");
        //    }

        //    await Task.Delay(2000);
        //}
    }

    private static void T21(IConnection c)
    {
        try
        {
            var ch = c.CreateModel();
            var qn = ch.QueueDeclare().QueueName;
            Console.WriteLine(qn);
            ch.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static async Task T2()
    {
        var f2 = Helper.GetMQOptions().CreateConnectionFactory();
        var c2 = f2.CreateConnection();

        Task.Run(() =>
        {
            while (true)
            {
                T21(c2);
            }
        });

        Task.Run(() =>
        {
            while (true)
            {
                T21(c2);
            }
        });

        //var ch2 = c2.CreateModel();



        //ch.BasicReturn += (sender, args) =>
        //{
        //    var dd = sender;
        //    Console.WriteLine();
        //};

        //ch.BasicPublish("", "qn", true, null, Encoding.UTF8.GetBytes("123"));


        //var qn = ch.QueueDeclare("rpc_test", false, false, false).QueueName;


        //var consumer = new AsyncEventingBasicConsumer(ch);
        //consumer.Received += ConsumerReceivedAsync;
        //var _consumerTag = ch.BasicConsume(qn, true, consumer);

        //ch.BasicCancel(_consumerTag);
        //ch.Close();
        //c.Close();

        //var f2 = Helper.GetMQOptions().CreateConnectionFactory();
        //var c2 = f2.CreateConnection();
        //var ch2 = c2.CreateModel();

        //while (true)
        //{
        //    try
        //    {
        //        ch2.QueueDeclarePassive(qn);
        //    }
        //    catch (OperationInterruptedException e)
        //    {
        //        Console.WriteLine(e.ShutdownReason.ReplyCode);
        //    }
        //}


        //ch2.QueueDeclare("test2", false, false, true);
        //ch2.BasicQos(0, (ushort)1, false);
        //var consumer2 = new AsyncEventingBasicConsumer(ch2);
        //_consumerTag = ch2.BasicConsume("test2", false, consumer2);




        //try
        //{
        //    var qdp = ch2.QueueDeclarePassive("123");
        //}
        //catch (Exception e)
        //{
        //    Console.WriteLine(e);
        //}


        //ch.BasicCancel(_consumerTag);

        //ch2.BasicPublish("", qn, null, Encoding.UTF8.GetBytes("123"));
    }

    private static async Task ConsumerReceivedAsync(object sender, BasicDeliverEventArgs e)
    {
        Console.WriteLine(Encoding.UTF8.GetString(e.Body.Span));
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

    private static async Task DoT()
    {
        var i = 0;
        while (true)
        {
            //await Task.Delay(1000);
            try
            {
                await Test_ComplexCallAsync();
                //await Test_Call2(i++);
            }
            catch (Exception e)
            {
                Console.WriteLine("error ");
            }

            GC.Collect();
        }
    }

    private static async Task Test_Call2(int i)
    {
        Console.WriteLine($"send {i}");
        await _proxyAsync.Call2(i.ToString());
        Console.WriteLine("end");
    }

    private static async Task Test_ComplexCallAsync()
    {
        using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            Console.Write("[ComplexCallAsync]...Send TestFile.txt...");
            var complexStream = await _proxyAsync.ComplexCallAsync(
                new CustomObj { Date = DateTime.Now, Name = "ComplexCall" },
                stream,
                async i => Console.Write(", " + i.Progress),
                default);

            using (var stream2 = complexStream.Stream)
                Console.Write($", receive length:{stream.Length}");
            Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
        }
    }
}