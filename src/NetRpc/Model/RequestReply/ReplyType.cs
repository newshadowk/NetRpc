namespace NetRpc;

internal enum ReplyType
{
    /// <summary>
    /// body:long?, stream length.
    /// </summary>
    ResultStream,

    /// <summary>
    /// body:CustomResult.
    /// </summary>
    CustomResult,

    /// <summary>
    /// body:Callback object.
    /// </summary>
    Callback,

    /// <summary>
    /// body:Exception.
    /// </summary>
    Fault,

    /// <summary>
    /// body:byte[].
    /// </summary>
    Buffer,

    /// <summary>
    /// body:null.
    /// </summary>
    BufferCancel,

    /// <summary>
    /// body:null.
    /// </summary>
    BufferFault,

    /// <summary>
    /// body:null.
    /// </summary>
    BufferEnd
}