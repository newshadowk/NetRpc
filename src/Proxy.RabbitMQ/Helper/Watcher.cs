﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Proxy.RabbitMQ;

public sealed class SubWatcher : IDisposable
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

    public void Dispose()
    {
        _t.Dispose();
    }
}

public class ExclusiveChecker
{
    private readonly IConnection _subConnection;

    public ExclusiveChecker(IConnection subConnection)
    {
        _subConnection = subConnection;
    }

    public bool Check(string? queue)
    {
        if (queue == null)
            return true;

        IModel ch;
        try
        {
            ch = _subConnection.CreateModel();
        }
        catch
        {
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