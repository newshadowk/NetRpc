using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace NetRpc;

public class ActionInvoker
{
    private readonly ActionExecutingContext _context;
    private ActionExecutedContext? _actionExecutedContext;
    private readonly FilterCursor _cursor;

    public ActionInvoker(ActionExecutingContext context)
    {
        _context = context;
        _cursor = new FilterCursor(context.InstanceMethod.ActionFilters.Cast<IAsyncActionFilter>().ToList());
    }

    public async Task InvokeAsync()
    {
        var next = State.ActionNext;
        var scope = Scope.Invoker;
        object state = null!;
        var isCompleted = false;
        while (!isCompleted)
            await Next(ref next, ref scope, ref state, ref isCompleted);
    }

    private Task Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
    {
        switch (next)
        {
            case State.ActionBegin:
            {
                _cursor.Reset();
                goto case State.ActionNext;
            }

            case State.ActionNext:
            {
                var current = _cursor.GetNextFilter();
                if (current != null)
                {
                    state = current;
                    goto case State.ActionAsyncBegin;
                }

                goto case State.ActionInside;
            }

            case State.ActionAsyncBegin:
            {
                var filter = (ActionFilterAttribute) state;
                var task = filter.OnActionExecutionAsync(_context, InvokeNextActionFilterAwaitedAsync);
                if (task.Status != TaskStatus.RanToCompletion)
                {
                    next = State.ActionAsyncEnd;
                    return task;
                }

                goto case State.ActionAsyncEnd;
            }

            case State.ActionAsyncEnd:
            {
                if (_actionExecutedContext == null)
                {
                    // If we get here then the filter didn't call 'next' indicating a short circuit.
                    _actionExecutedContext = new ActionExecutedContext(_context)
                    {
                        Canceled = true
                    };
                }

                goto case State.ActionEnd;
            }

            case State.ActionInside:
            {
                var task = InvokeActionInsideAsync();
                if (task.Status != TaskStatus.RanToCompletion)
                {
                    next = State.ActionEnd;
                    return task;
                }

                goto case State.ActionEnd;
            }

            case State.ActionEnd:
            {
                if (scope == Scope.Action)
                {
                    if (_actionExecutedContext == null)
                        _actionExecutedContext = new ActionExecutedContext(_context);

                    isCompleted = true;
                    return Task.CompletedTask;
                }

                var actionExecutedContext = _actionExecutedContext;
                Rethrow(actionExecutedContext);

                isCompleted = true;
                return Task.CompletedTask;
            }

            default:
                throw new InvalidOperationException();
        }
    }

    private static void Rethrow(ActionExecutedContext? context)
    {
        if (context == null)
            return;

        if (context.ExceptionHandled)
            return;

        context.ExceptionDispatchInfo?.Throw();

        if (context.Exception != null)
            throw context.Exception;
    }

    private async Task<ActionExecutedContext> InvokeNextActionFilterAwaitedAsync()
    {
        if (_context.Result != null)
        {
            // If we get here, it means that an async filter set a result AND called next(). This is forbidden.
            throw new InvalidOperationException("an async filter set a result AND called next()");
        }

        await InvokeNextActionFilterAsync();

        return _actionExecutedContext!;
    }

    private async Task InvokeNextActionFilterAsync()
    {
        try
        {
            var next = State.ActionNext;
            var scope = Scope.Action;
            var state = (object) null!;
            var isCompleted = false;
            while (!isCompleted)
            {
                await Next(ref next, ref scope, ref state, ref isCompleted);
            }
        }
        catch (Exception exception)
        {
            _actionExecutedContext = new ActionExecutedContext(_context)
            {
                ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception)
            };
        }
    }

    private async Task InvokeActionInsideAsync()
    {
        _context.Result = await _context.InstanceMethod.MethodInfo.InvokeAsync(_context.Instance.Target, _context.Args);
    }

    private enum Scope
    {
        Invoker,
        Action
    }

    private enum State
    {
        ActionBegin,
        ActionNext,
        ActionAsyncBegin,
        ActionAsyncEnd,
        ActionInside,
        ActionEnd
    }
}