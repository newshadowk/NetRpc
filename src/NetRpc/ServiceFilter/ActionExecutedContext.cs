using System;
using System.Runtime.ExceptionServices;

namespace NetRpc
{
    public sealed class ActionExecutedContext
    {
        private Exception _exception;
        private ExceptionDispatchInfo _exceptionDispatchInfo;

        public ActionExecutedContext(ActionExecutingContext actionExecutingContext)
        {
            ActionExecutingContext = actionExecutingContext;
        }

        public ActionExecutingContext ActionExecutingContext { get; set; }

        public bool Canceled { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="System.Exception"/> caught while executing the action or action filters, if
        /// any.
        /// </summary>
        public Exception Exception
        {
            get
            {
                if (_exception == null && _exceptionDispatchInfo != null)
                    return _exceptionDispatchInfo.SourceException;
                return _exception;
            }
            set
            {
                _exceptionDispatchInfo = null;
                _exception = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Runtime.ExceptionServices.ExceptionDispatchInfo"/> for the
        /// <see cref="Exception"/>, if an <see cref="System.Exception"/> was caught and this information captured.
        /// </summary>
        public ExceptionDispatchInfo ExceptionDispatchInfo
        {
            get => _exceptionDispatchInfo;
            set
            {
                _exception = null;
                _exceptionDispatchInfo = value;
            }
        }

        public bool ExceptionHandled { get; set; }
    }
}