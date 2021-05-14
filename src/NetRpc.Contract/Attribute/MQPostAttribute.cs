using System;

namespace NetRpc.Contract
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class MQPostAttribute : Attribute
    {
        public MQPostAttribute(byte priority = 0)
        {
            Priority = priority;
        }
        /// <summary>
        /// 优先级 0-9 消费者默认是0，数字越大优先级越高
        /// </summary>
        public byte Priority { get; set; }
    }
}