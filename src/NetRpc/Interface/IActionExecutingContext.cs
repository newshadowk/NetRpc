using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IActionExecutingContext
    {
        DateTimeOffset StartTime { get; }

        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Result of invoked action.
        /// </summary>
        object? Result { get; set; }

        Dictionary<string, object?> Header { get; set; }

        /// <summary>
        /// A central location for sharing state between components during the invoking process.
        /// </summary>
        Dictionary<object, object?> Properties { get; set; }

        InstanceMethod InstanceMethod { get; }

        ContractMethod ContractMethod { get; }

        ContractInfo Contract { get; }

        Func<object?, Task>? Callback { get; }

        CancellationToken CancellationToken { get; }

        ReadStream? Stream { get; }

        /// <summary>
        /// Args of invoked action without stream and action.
        /// </summary>
        object?[] PureArgs { get; }
    }
}