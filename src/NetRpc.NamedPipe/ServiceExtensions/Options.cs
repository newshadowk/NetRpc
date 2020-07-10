namespace Microsoft.Extensions.DependencyInjection
{
    public class NamePipeServiceOptions
    {
        public int MaxNumberOfServerInstances { get; set; }

        public string Name { get; set; }
    }

    public class NamePipeClientOptions
    {
        public string Name { get; set; }
    }
}