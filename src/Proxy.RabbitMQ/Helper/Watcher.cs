using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Proxy.RabbitMQ;

public class SubWatcher
{
    private readonly ExclusiveChecker _checker;
    private readonly BusyTimer _t = new(5000);
    private readonly SyncList<string> _list = new();
    private readonly object _lockCheck = new();

    public event EventHandler<EventArgsT<string>>? Disconnected;

    public SubWatcher(ExclusiveChecker checker)
    {
        _checker = checker;
        _t.ElapsedAsync += ElapsedAsync;
        _t.Start();
    }

    private Task ElapsedAsync(object sender, ElapsedEventArgs e)
    {
        List<string> list;
        lock (_list.SyncRoot)
            list = _list.ToList();

        lock (_lockCheck)
        {
            foreach (var s in list)
            {
                if (!_checker.Check(s))
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

    private void OnDisconnected(EventArgsT<string> e)
    {
        Disconnected?.Invoke(this, e);
    }
}

public class MainWatcher
{
    private readonly IModel _mainChannel;
    private readonly string _queue;
    private readonly BusyTimer _t = new(5000);

    public event EventHandler? Disconnected;

    public MainWatcher(IModel mainChannel, string queue)
    {
        _mainChannel = mainChannel;
        _queue = queue;
        _t.ElapsedAsync += ElapsedAsync;
        _t.Start();
    }

    private Task ElapsedAsync(object sender, ElapsedEventArgs e)
    {
        if (!Check(_mainChannel, _queue))
            OnDisconnected();
        return Task.CompletedTask;
    }

    private static bool Check(IModel channel, string queue)
    {
        try
        {
            channel.QueueDeclarePassive(queue);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnDisconnected()
    {
        Disconnected?.Invoke(this, EventArgs.Empty);
    }
}

public class ExclusiveChecker
{
    private readonly IConnection _subConnection;
    private readonly ILogger _logger;

    public ExclusiveChecker(IConnection subConnection, ILogger logger)
    {
        _subConnection = subConnection;
        _logger = logger;
    }

    public bool Check(string queue)
    {
        IModel ch;
        try
        {
            ch = _subConnection.CreateModel();
        }
        catch
        {
            _logger.LogWarning("ExclusiveChecker, create model err");
            return false;
        }

        try
        {
            using (ch)
                ch.QueueDeclarePassive(queue);
        }
        catch (OperationInterruptedException e)
        {
            if (e.ShutdownReason.ReplyCode == 405)
                return true;
        }

        return false;
    }
}