using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Proxy.RabbitMQ;

public class QueueWatcher
{
    private readonly IConnection _subConnection;
    private readonly ILogger _logger;
    private readonly BusyTimer _t = new(2000);
    private readonly SyncList<string> _list = new();
    private readonly object _lockCheck = new();

    public event EventHandler<EventArgsT<string>>? Disconnected;

    public QueueWatcher(IConnection subConnection, ILogger logger)
    {
        _subConnection = subConnection;
        _logger = logger;
        _t.ElapsedAsync += ElapsedAsync;
        _t.Start();
    }

    private Task ElapsedAsync(object sender, System.Timers.ElapsedEventArgs @event)
    {
        List<string> list;
        lock (_list.SyncRoot) 
            list = _list.ToList();

        
        lock (_lockCheck)
        {
            foreach (var s in list)
            {
                IModel ch;
                try
                {
                    ch = _subConnection.CreateModel();
                }
                catch
                {
                    _logger.LogWarning("QueueWatcher, create model err");
                    return Task.CompletedTask;
                }

                if (!CheckExclusive(ch, s)) 
                    OnDisconnected(new EventArgsT<string>(s));
            }
        }

        return Task.CompletedTask;
    }

    public void Add(string queue)
    {
        _list.Add(queue);
    }

    public void Remove(string queue)
    {
        _list.Remove(queue);
    }

    private static bool CheckExclusive(IModel channel, string queue)
    {
        try
        {
            using (channel) 
                channel.QueueDeclarePassive(queue);
        }
        catch (OperationInterruptedException e)
        {
            if (e.ShutdownReason.ReplyCode == 405)
                return true;
        }

        return false;
    }

    private void OnDisconnected(EventArgsT<string> e)
    {
        Disconnected?.Invoke(this, e);
    }
}