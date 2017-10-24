using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using System;
using System.Collections.Generic;
using System.Linq;

namespace pieventsnovo
{
    public class ExecuteCommand
    {
        internal void Execute(string command, PIPointList pointsList, AFTime st, AFTime et,
                               AFTimeSpan summaryDuration, string[] times, string addlparam1, PIServer myServer)
        {
            try
            {
                Console.WriteLine();
                switch (command)
                {
                    case "snap":
                        {
                            Console.WriteLine($"Point Name(Point Id), Timestamp, Current Value");
                            Console.WriteLine(new string('-', 45));
                            AFListResults<PIPoint, AFValue> results = pointsList.EndOfStream();
                            if (results.HasErrors)
                            {
                                foreach (var e in results.Errors) Console.WriteLine($"{e.Key}: {e.Value}");
                            }
                            foreach (var v in results.Results)
                            {
                                if (!results.Errors.ContainsKey(v.PIPoint))
                                {
                                    Console.WriteLine($"{string.Concat($"{v.PIPoint.Name} ({v.PIPoint.ID})"),-15}, {v.Timestamp}, {v.Value}");
                                }
                            }
                            Console.WriteLine();
                            break;
                        }
                    case "arclist":
                        {
                            AFTimeRange timeRange = new AFTimeRange(st, et);
                            if (!Int32.TryParse(addlparam1, out int maxcount))
                                maxcount = 0;

                            // Holds the results keyed on the associated point
                            var resultsMap = new Dictionary<PIPoint, AFValues>();
                            var pagingConfig = new PIPagingConfiguration(PIPageType.TagCount, GlobalConfig.PageSize);
                            IEnumerable<AFValues> listResults = pointsList.RecordedValues(timeRange: timeRange,
                                                                  boundaryType: AFBoundaryType.Inside,
                                                                  filterExpression: null,
                                                                  includeFilteredValues: false,
                                                                  pagingConfig: pagingConfig,
                                                                  maxCount: maxcount
                                                                  );
                            foreach (var pointResults in listResults) resultsMap[pointResults.PIPoint] = pointResults;
                            foreach (var pointValues in resultsMap)
                            {
                                Console.WriteLine($"Point: {pointValues.Key} Archive Values Count: {pointValues.Value.Count}");
                                Console.WriteLine(new string('-', 45));
                                pointValues.Value.ForEach(v => Console.WriteLine($"{v.Timestamp}, {v.Value}"));
                                Console.WriteLine();
                            }
                            break;
                        }
                    case "plot":
                        {
                            AFTimeRange timeRange = new AFTimeRange(st, et);
                            if (!Int32.TryParse(addlparam1, out int intervals))
                                intervals = 640; //horizontal pixels in the trend

                            var resultsMap = new Dictionary<PIPoint, AFValues>();
                            var pagingConfig = new PIPagingConfiguration(PIPageType.TagCount, GlobalConfig.PageSize);
                            IEnumerable<AFValues> listResults = pointsList.PlotValues(timeRange: timeRange,
                                                                  intervals: intervals,
                                                                  pagingConfig: pagingConfig
                                                                  );
                            foreach (var pointResults in listResults) resultsMap[pointResults.PIPoint] = pointResults;
                            foreach (var pointValues in resultsMap)
                            {
                                Console.WriteLine($"Point: {pointValues.Key} Plot Values Interval: {intervals} Count: {pointValues.Value.Count}");
                                Console.WriteLine(new string('-', 45));
                                pointValues.Value.ForEach(v => Console.WriteLine($"{v.Timestamp}, {v.Value}"));
                                Console.WriteLine();
                            }
                            break;
                        }
                    case "interp":
                        {
                            AFTimeRange timeRange = new AFTimeRange(st, et);
                            var resultsMap = new Dictionary<PIPoint, AFValues>();
                            var pagingConfig = new PIPagingConfiguration(PIPageType.TagCount, GlobalConfig.PageSize);

                            if (addlparam1.StartsWith("c="))
                            {
                                if (!Int32.TryParse(addlparam1.Substring(2), out int count))
                                    count = 10; //default count

                                IEnumerable<AFValues> listResults = pointsList.InterpolatedValuesByCount(timeRange: timeRange,
                                                                                numberOfValues: count,
                                                                                filterExpression: null,
                                                                                includeFilteredValues: false,
                                                                                pagingConfig: pagingConfig
                                                                               );
                                foreach (var pointResults in listResults) resultsMap[pointResults.PIPoint] = pointResults;
                                foreach (var pointValues in resultsMap)
                                {
                                    Console.WriteLine($"Point: {pointValues.Key} Interpolated Values Count: {pointValues.Value.Count}");
                                    Console.WriteLine(new string('-', 45));
                                    pointValues.Value.ForEach(v => Console.WriteLine($"{v.Timestamp}, {v.Value}"));
                                    Console.WriteLine();
                                }
                            }
                            else
                            {
                                if (!AFTimeSpan.TryParse(addlparam1, out AFTimeSpan interval) || interval == new AFTimeSpan(0))
                                    interval = summaryDuration;

                                IEnumerable<AFValues> listResults = pointsList.InterpolatedValues(timeRange: timeRange,
                                                                               interval: interval,
                                                                               filterExpression: null,
                                                                               includeFilteredValues: false,
                                                                               pagingConfig: pagingConfig
                                                                              );
                                foreach (var pointResults in listResults) resultsMap[pointResults.PIPoint] = pointResults;
                                foreach (var pointValues in resultsMap)
                                {
                                    Console.WriteLine($"Point: {pointValues.Key} Interpolated Values Interval: {interval.ToString()}");
                                    Console.WriteLine(new string('-', 45));
                                    pointValues.Value.ForEach(v => Console.WriteLine($"{v.Timestamp}, {v.Value}"));
                                    Console.WriteLine();
                                }
                            }
                            break;
                        }
                    case "summaries":
                        {
                            var resultsMap = new Dictionary<PIPoint, AFValues>();
                            var pagingConfig = new PIPagingConfiguration(PIPageType.TagCount, GlobalConfig.PageSize);
                            if (st > et) //summaries cannot handle reversed times
                            {
                                var temp = st;
                                st = et;
                                et = temp;
                            }
                            AFTimeRange timeRange = new AFTimeRange(st, et);
                            var intervalDefinitions = new AFTimeIntervalDefinition(timeRange, 1);
                            AFCalculationBasis calculationBasis = AFCalculationBasis.EventWeighted;
                            if (addlparam1 == "t")
                                calculationBasis = AFCalculationBasis.TimeWeighted;

                            foreach (var pt in pointsList)
                            {
                                var summaryType = AFSummaryTypes.All;
                                if (pt.PointType == PIPointType.Digital
                                                    || pt.PointType == PIPointType.Timestamp
                                                    || pt.PointType == PIPointType.Blob
                                                    || pt.PointType == PIPointType.String
                                                    || pt.PointType == PIPointType.Null)
                                {
                                    summaryType = AFSummaryTypes.AllForNonNumeric;
                                }
                                IDictionary<AFSummaryTypes, AFValues> summaries = pt.Summaries(new List<AFTimeIntervalDefinition>() {
                                                                                               intervalDefinitions },
                                                                                               reverseTime: false,
                                                                                               summaryType: summaryType,
                                                                                               calcBasis: calculationBasis,
                                                                                               timeType: AFTimestampCalculation.Auto
                                                                                               );
                                Console.WriteLine($"Point: {pt.Name} {calculationBasis} Summary");
                                Console.WriteLine(new string('-', 45));
                                foreach (var s in summaries)
                                {
                                    AFValues vals = s.Value;
                                    foreach (var v in vals)
                                    {
                                        if (v.Value.GetType() != typeof(PIException))
                                            Console.WriteLine($"{s.Key,-16}: {v.Value}");
                                        else
                                            Console.WriteLine($"{s.Key,-16}: {v}");
                                    }
                                }
                                Console.WriteLine();
                            }
                            /*
                            Non numeric tags in pointsList requires splitting of queries so the above is preferred. 
                            The below implementation works when there are no non-numeric types or one particular summary needs to be run
                            */
                            //var listResults = pointsList.Summaries(new List<AFTimeIntervalDefinition>() {
                            //                                                               intervalDefinitions },
                            //                                                               reverseTime: false,
                            //                                                               summaryTypes: AFSummaryTypes.All,
                            //                                                               calculationBasis: calculationBasis,
                            //                                                               timeType: AFTimestampCalculation.Auto,
                            //                                                               pagingConfig: pagingConfig
                            //                                                               );
                            //foreach (IDictionary<AFSummaryTypes, AFValues> summaries in listResults)
                            //{
                            //     foreach (IDictionary<AFSummaryTypes, AFValues> pointResults in listResults)
                            //    {
                            //            AFValues pointValues = pointResults[AFSummaryTypes.Average];
                            //            PIPoint point = pointValues.PIPoint;
                            //           //Map the results back to the point
                            //           resultsMap[point] = pointValues;
                            //     }
                            //}
                            break;
                        }
                    case "update":
                    case "annotate":
                        {
                            string addlparam2 = string.Empty;
                            AFUpdateOption updateOption = AFUpdateOption.Replace;
                            AFBufferOption bufOption = AFBufferOption.BufferIfPossible;
                            AFValue val;
                            if (times.Length > 0)
                            {
                                addlparam1 = times[0];
                                if (times.Length > 1)
                                {
                                    addlparam2 = times[1];
                                }
                            }
                            switch (addlparam1)
                            {
                                case "i":
                                    updateOption = AFUpdateOption.Insert;
                                    break;
                                case "nr":
                                    updateOption = AFUpdateOption.NoReplace;
                                    break;
                                case "ro":
                                    updateOption = AFUpdateOption.ReplaceOnly;
                                    break;
                                case "inc":
                                    updateOption = AFUpdateOption.InsertNoCompression;
                                    break;
                                case "rm":
                                    updateOption = AFUpdateOption.Remove;
                                    break;
                            }
                            switch (addlparam2)
                            {
                                case "dnb":
                                    bufOption = AFBufferOption.DoNotBuffer;
                                    break;
                                case "buf":
                                    bufOption = AFBufferOption.Buffer;
                                    break;
                            }
                            foreach (var pt in pointsList)
                            {
                                Console.WriteLine($"Point: {pt.Name} {command} ({updateOption} {bufOption})");
                                Console.WriteLine(new string('-', 45));
                                Console.Write("Enter timestamp: ");
                                var time = Console.ReadLine();

                                if (!AFTime.TryParse(time, out AFTime ts))
                                {
                                    ParseArgs.PrintHelp("Invalid Timestamp");
                                    break;
                                }
                                if (command == "update" || 
                                    !(pt.RecordedValuesAtTimes(new List<AFTime>() { ts }, AFRetrievalMode.Exact)[0].IsGood))
                                {
                                    Console.Write("Enter new value: ");
                                    var data = Console.ReadLine();
                                    if (!Double.TryParse(data, out var value))
                                    {
                                        ParseArgs.PrintHelp("Invalid data");
                                        break;
                                    }
                                    val = new AFValue(value, ts);
                                }
                                else
                                {
                                     val = pt.RecordedValuesAtTimes(new List<AFTime>() { ts }, AFRetrievalMode.Exact)[0];
                                }
                                if (command == "annotate")
                                {
                                    Console.Write("Enter annotation: ");
                                    var ann = Console.ReadLine();
                                    pt.SetAnnotation(val, ann);
                                }
                                pt.UpdateValue(value: val, option: updateOption, bufferOption: bufOption);
                                Console.WriteLine($"Successfully {command}d");
                            }
                            Console.WriteLine();
                            break;
                        }
                    case "delete":
                        {
                            AFTimeRange timeRange = new AFTimeRange(st, et);
                            if (myServer.Supports(PIServerFeature.DeleteRange))
                            {
                                foreach (var pt in pointsList)
                                {
                                    int delcount = 0;
                                    var intervalDefinitions = new AFTimeIntervalDefinition(timeRange, 1);
                                    //getting the count of events 
                                    IDictionary<AFSummaryTypes, AFValues> summaries = pt.Summaries(new List<AFTimeIntervalDefinition>() {
                                                                                               intervalDefinitions },
                                                                                               reverseTime: false,
                                                                                               summaryType: AFSummaryTypes.Count,
                                                                                               calcBasis: AFCalculationBasis.EventWeighted,
                                                                                               timeType: AFTimestampCalculation.Auto
                                                                                               );
                                    foreach (var s in summaries)
                                    {
                                        AFValues vals = s.Value;
                                        vals = s.Value;
                                        foreach (var v in vals)
                                        {
                                            if (v.Value.GetType() != typeof(PIException))
                                                delcount = v.ValueAsInt32();
                                        }
                                    }
                                    if (delcount > 0)
                                    {
                                        var errs = pt.ReplaceValues(timeRange, new List<AFValue>() { });
                                        if (errs != null)
                                        {
                                            foreach (var e in errs.Errors)
                                            {
                                                Console.WriteLine(e);
                                                delcount--;
                                            }
                                        }
                                    }
                                    Console.WriteLine($"Point: {pt.Name} Deleted {delcount} events");
                                    Console.WriteLine(new string('-', 45));
                                    Console.WriteLine();
                                }
                            }
                            else
                            {
                                foreach (var pt in pointsList)
                                {
                                    AFValues vals = pt.RecordedValues(timeRange: timeRange,
                                                                      boundaryType: AFBoundaryType.Inside,
                                                                      filterExpression: null,
                                                                      includeFilteredValues: false,
                                                                      maxCount: 0
                                                                      );
                                    int delcount = vals.Count;
                                    if (delcount > 0)
                                    {
                                        var errs = pt.UpdateValues(values: vals,
                                                               updateOption: AFUpdateOption.Remove,
                                                               bufferOption: AFBufferOption.BufferIfPossible);
                                        if (errs != null)
                                        {
                                            foreach (var e in errs.Errors)
                                            {
                                                Console.WriteLine(e);
                                                delcount--;
                                            }
                                        }
                                    }
                                    Console.WriteLine($"Point: {pt.Name} Deleted {delcount} events");
                                    Console.WriteLine(new string('-', 45));
                                    Console.WriteLine();
                                }
                            }
                            break;
                        }
                    case "sign,t":
                        {
                            Dictionary<PIPoint, int> errPoints = pointsList.ToDictionary(key => key, value => 0);
                            //if (Int32.TryParse(myServer.ServerVersion.Substring(4, 3), out int srvbuild) && srvbuild >= 395);
                            if (myServer.Supports(PIServerFeature.TimeSeriesDataPipe))
                            {
                                PIDataPipe timeSeriesDatapipe = new PIDataPipe(AFDataPipeType.TimeSeries);
                                Console.WriteLine("Signing up for TimeSeries events");
                                var errs = timeSeriesDatapipe.AddSignups(pointsList);
                                if (errs != null)
                                {
                                    foreach (var e in errs.Errors)
                                    {
                                        Console.WriteLine($"Failed timeseries signup: {e.Key}, {e.Value.Message}");
                                        errPoints[e.Key]++;
                                    }
                                    foreach (var ep in errPoints)
                                    {
                                        if (ep.Value >= 1) pointsList.Remove(ep.Key);
                                    }
                                    if (pointsList.Count == 0)
                                    {
                                        ParseArgs.PrintHelp("No valid PI Points, " + $"disconnecting server {myServer.Name}");
                                        if (timeSeriesDatapipe != null)
                                        {
                                            timeSeriesDatapipe.Close();
                                            timeSeriesDatapipe.Dispose();
                                        }
                                        myServer.Disconnect();
                                        System.Threading.Thread.Sleep(200);
                                        return;
                                    }
                                }
                                timeSeriesDatapipe.Subscribe(new DataPipeObserver("TimeSeries"));
                                Console.WriteLine("Subscribed Points (current value): ");
                                AFListResults<PIPoint, AFValue> results = pointsList.EndOfStream();
                                if (results.HasErrors)
                                {
                                    foreach (var e in results.Errors) Console.WriteLine($"{e.Key}: {e.Value}");
                                }
                                foreach (var v in results.Results)
                                {
                                    if (!results.Errors.ContainsKey(v.PIPoint))
                                    {
                                        Console.WriteLine($"{v.PIPoint.Name,-12}, {v.Timestamp}, {v.Value}");
                                    }
                                }
                                Console.WriteLine(new string('-', 45));

                                //Fetch timeseries events till user termination
                                while (!GlobalConfig.CancelSignups)
                                {
                                    timeSeriesDatapipe.GetObserverEvents(GlobalConfig.PipeMaxEvtCount, out bool hasMoreEvents);
                                    System.Threading.Thread.Sleep(GlobalConfig.PipeCheckFreq);
                                }
                                Console.WriteLine("Cancelling signups ...");
                                if (timeSeriesDatapipe != null)
                                {
                                    timeSeriesDatapipe.Close();
                                    timeSeriesDatapipe.Dispose();
                                }
                            }
                            else
                            {
                                ParseArgs.PrintHelp($"Time series not supported in Archive version {myServer.ServerVersion}");
                            }
                            break;
                        }
                    case "sign,as":
                    case "sign,sa":
                    case "sign,a":
                    case "sign,s":
                        {
                            bool snapSubscribe = false;
                            bool archSubscribe = false;
                            PIDataPipe snapDatapipe = null;
                            PIDataPipe archDatapipe = null;
                            Dictionary<PIPoint, int> errPoints = pointsList.ToDictionary(key => key, value => 0);
                            if (command.Substring(5).Contains("s"))
                            {
                                snapSubscribe = true;
                                snapDatapipe = new PIDataPipe(AFDataPipeType.Snapshot);

                                Console.WriteLine("Signing up for Snapshot events");
                                var errs = snapDatapipe.AddSignups(pointsList);
                                snapDatapipe.Subscribe(new DataPipeObserver("Snapshot"));
                                if (errs != null)
                                {
                                    foreach (var e in errs.Errors)
                                    {
                                        Console.WriteLine($"Failed snapshot signup: {e.Key}, {e.Value.Message}");
                                        errPoints[e.Key]++;
                                    }
                                }
                            }
                            if (command.Substring(5).Contains("a"))
                            {
                                archSubscribe = true;
                                archDatapipe = new PIDataPipe(AFDataPipeType.Archive);
                                Console.WriteLine("Signing up for Archive events");
                                var errs = archDatapipe.AddSignups(pointsList);
                                if (errs != null)
                                {
                                    foreach (var e in errs.Errors)
                                    {
                                        Console.WriteLine($"Failed archive signup: {e.Key}, {e.Value.Message}");
                                        errPoints[e.Key]++;
                                    }
                                }
                                archDatapipe.Subscribe(new DataPipeObserver("Archive "));
                            }

                            //remove unsubscribable points
                            int errorLimit = snapSubscribe ? 1 : 0;
                            if (archSubscribe) errorLimit++;
                            foreach (var ep in errPoints)
                            {
                                if (ep.Value >= errorLimit) pointsList.Remove(ep.Key);
                            }
                            if (pointsList.Count == 0)
                            {
                                ParseArgs.PrintHelp("No valid PI Points, " + $"disconnecting server {myServer.Name}");
                                if (snapDatapipe != null)
                                {
                                    snapDatapipe.Close();
                                    snapDatapipe.Dispose();
                                }
                                if (archDatapipe != null)
                                {
                                    archDatapipe.Close();
                                    archDatapipe.Dispose();
                                }
                                myServer.Disconnect();
                                System.Threading.Thread.Sleep(200);
                                return;
                            }
                            Console.WriteLine("Subscribed Points (current value): ");
                            //foreach (var p in pointsList)
                            //{
                            //    Console.WriteLine($"{p.Name,-12}, {p.EndOfStream().Timestamp}, {p.EndOfStream()}");
                            //}
                            AFListResults<PIPoint, AFValue> results = pointsList.EndOfStream();
                            if (results.HasErrors)
                            {
                                foreach (var e in results.Errors) Console.WriteLine($"{e.Key}: {e.Value}");
                            }
                            foreach (var v in results.Results)
                            {
                                if (!results.Errors.ContainsKey(v.PIPoint))
                                {
                                    Console.WriteLine($"{v.PIPoint.Name,-12}, {v.Timestamp}, {v.Value}");
                                }
                            }
                            Console.WriteLine(new string('-', 45));

                            //Fetch events from the data pipes
                            while (!GlobalConfig.CancelSignups)
                            {
                                if (snapSubscribe)
                                    snapDatapipe.GetObserverEvents(GlobalConfig.PipeMaxEvtCount, out bool hasMoreEvents1);
                                if (archSubscribe)
                                    archDatapipe.GetObserverEvents(GlobalConfig.PipeMaxEvtCount, out bool hasMoreEvents2);
                                System.Threading.Thread.Sleep(GlobalConfig.PipeCheckFreq); 
                            }
                            Console.WriteLine("Cancelling signups ...");
                            if (snapDatapipe != null)
                            {
                                snapDatapipe.Close();
                                snapDatapipe.Dispose();
                            }
                            if (archDatapipe != null)
                            {
                                archDatapipe.Close();
                                archDatapipe.Dispose();
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ParseArgs.PrintHelp(ex.Message);
                if (myServer != null)
                {
                    myServer.Disconnect();
                    if (GlobalConfig.Debug) Console.WriteLine($"Disconnecting from {myServer.Name}");
                }
                Console.WriteLine(new string('~', 45));
            }
        }
    }
}
