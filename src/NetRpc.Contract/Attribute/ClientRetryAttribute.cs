using System;

namespace NetRpc.Contract
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, Inherited = false)]
    public sealed class ClientRetryAttribute : Attribute
    {
        /// <summary>
        /// Milliseconds.
        /// </summary>
        public int[] SleepDurations { get; set; }

        /// <param name="sleepDurations">Milliseconds</param>
        public ClientRetryAttribute(params int[] sleepDurations)
        {
            SleepDurations = sleepDurations;
        }
    }
}