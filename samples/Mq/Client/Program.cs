using System;
using System.IO;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetRpc;
using NetRpc.RabbitMQ;
using RabbitMQ.Client;
using Helper = TestHelper.Helper;

namespace Client;

internal class Program
{
    private static IServiceAsync _proxyAsync;

    private static async Task Main(string[] args)
    {
        await T1();
        Console.WriteLine("\r\n--------------- End ---------------");
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
        //var r = await _proxyAsync.Call2("123");
        //try
        //{
        //    await Test_ComplexCallAsync();
        //}
        //catch (Exception e)
        //{
        //    Console.WriteLine(e);
        //}

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
        var q = ch.QueueDeclare("rpc_test2", false, false, false, null);
        //var q = ch.QueueDeclare();

        int i = 0;
        while (true)
        {
            //Console.ReadLine();

            i++;
            Console.WriteLine($"{q.QueueName} {i}");
            try
            {
                ch.BasicPublish("", q.QueueName, null, Encoding.UTF8.GetBytes(i.ToString()));
            }
            catch
            {
                Console.WriteLine($"send err {i}");
            }

            await Task.Delay(2000);
        }
    }

    private static void Ch_ModelShutdown(object sender, ShutdownEventArgs e)
    {
        Console.WriteLine("Ch_ModelShutdown");
    }

    private static void Ch_FlowControl(object sender, RabbitMQ.Client.Events.FlowControlEventArgs e)
    {
        Console.WriteLine("Ch_FlowControl");
    }

    private static void Ch_BasicReturn(object sender, RabbitMQ.Client.Events.BasicReturnEventArgs e)
    {
        Console.WriteLine("Ch_BasicReturn");
    }

    private static void Ch_BasicNacks(object sender, RabbitMQ.Client.Events.BasicNackEventArgs e)
    {
        Console.WriteLine("Ch_BasicNacks");
    }

    private static void Ch_BasicAcks(object sender, RabbitMQ.Client.Events.BasicAckEventArgs e)
    {
        Console.WriteLine("Ch_BasicAcks");
    }

    private static void Ch_BasicRecoverOk(object sender, EventArgs e)
    {
        Console.WriteLine("Ch_BasicRecoverOk");
    }

    private static void Ch_CallbackException(object sender, RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
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

    private static void C_ConnectionBlocked(object sender, RabbitMQ.Client.Events.ConnectionBlockedEventArgs e)
    {
        Console.WriteLine("C_ConnectionBlocked");
    }

    private static void C_CallbackException(object sender, RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
    {
        Console.WriteLine("C_CallbackException");
    }

    private static async Task DoT()
    {
        int i = 0;
        while (true)
        {
            await Task.Delay(1000);

            try
            {
                await Test_ComplexCallAsync();
                //await Test_Call2(i++);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
          
            GC.Collect();
        }
    }

    private static async Task Test_Call2(int i)
    {
        Console.Write($"send {i}");
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