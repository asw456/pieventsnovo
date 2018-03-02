
namespace pieventsnovo
{
    public static class GlobalConfig
    {
        public static bool Debug = false;
        public static bool CancelSignups = false;
        public const int PipeCheckFreq = 3000; //milliseconds
        public const int PipeMaxEvtCount = 20;
        public const int PageSize = 1000;
        public const int PointChangeFreq = PipeCheckFreq;
        public const int MaxPointChange = 1000;
    }
}
