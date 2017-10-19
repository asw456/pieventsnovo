using System;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;

namespace pieventsnovo
{
    internal class DataPipeObserver : IObserver<AFDataPipeEvent>
    {
        private string Evt { get; set; }
        public DataPipeObserver(string evt)
        {
            if (GlobalValues.Debug) Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId); 
            Evt = evt;
        }
        public void OnCompleted()
        {
            if (GlobalValues.Debug) Console.WriteLine("Signups Completed"); 
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
