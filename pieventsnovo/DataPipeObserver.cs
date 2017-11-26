using System;
using System.Threading;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;

namespace pieventsnovo
{
    internal class DataPipeObserver : IObserver<AFDataPipeEvent>
    {
        private string Evt;
        public DataPipeObserver(string evt)
        {
            if (GlobalConfig.Debug) Console.WriteLine(Thread.CurrentThread.ManagedThreadId); 
            Evt = evt;
        }

        public void OnCompleted()
        {
            if (GlobalConfig.Debug) Console.WriteLine("Signups Completed"); 
        }

        public void OnError(Exception error)
        {
            Console.WriteLine(error.Message);
        }

        public void OnNext(AFDataPipeEvent value)
        {
            AFValue v = value.Value;
            Console.WriteLine($"{Evt}, {v.PIPoint.Name,-12}, {v.Timestamp}, {v.Value}, {{{value.Action}, {DateTime.Now}}}");
            // timeseries subscription carries point archive information
            //Console.WriteLine(value.SpecificUpdatedValue);
            //if (ArchSubscribe && (value.PreviousEventAction == AFDataPipePreviousEventAction.PreviousEventArchived));
        }
    }
}
