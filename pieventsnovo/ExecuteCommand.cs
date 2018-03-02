using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;
using System.Threading;

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
                            var sb = new StringBuilder();
                            sb.AppendLine($"Point Name(Point Id), Timestamp, Current Value");
                            sb.AppendLine(new string('-', 45));
                            AFListResults<PIPoint, AFValue> results = pointsList.EndOfStream();
                            if (results.HasErrors)
                            {
                                foreach (var e in results.Errors) sb.AppendLine($"{e.Key}: {e.Value}");
                            }
                            foreach (var v in results.Results)
                            {
                                if (!results.Errors.ContainsKey(v.PIPoint))
                                {
                                    sb.AppendLine($"{string.Concat($"{v.PIPoint.Name} ({v.PIPoint.ID})"),-15}," +
                                        $" {v.Timestamp}, {v.Value}");
                                }
                            }
                            sb.AppendLine();
                            Console.Write(sb.ToString());
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
                                var sb = new StringBuilder();
                                sb.AppendLine($"Point: {pointValues.Key} Archive Values " +
                                    $"Count: {pointValues.Value.Count}");
                                sb.AppendLine(new string('-', 45));
                                pointValues.Value.ForEach(v => sb.AppendLine($"{v.Timestamp}, {v.Value}"));
                                sb.AppendLine();
                                Console.Write(sb.ToString());
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
                                var sb = new StringBuilder();
                                sb.AppendLine($"Point: {pointValues.Key} Plot Values Interval: {intervals}" +
                                    $" Count: {pointValues.Value.Count}");
                                sb.AppendLine(new string('-', 45));
                                pointValues.Value.ForEach(v => sb.AppendLine($"{v.Timestamp}, {v.Value}"));
                                sb.AppendLine();
                                Console.Write(sb.ToString());
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
                                    var sb = new StringBuilder();
                                    sb.AppendLine($"Point: {pointValues.Key} Interpolated Values " +
                                        $"Count: {pointValues.Value.Count}");
                                    sb.AppendLine(new string('-', 45));
                                    pointValues.Value.ForEach(v => sb.AppendLine($"{v.Timestamp}, {v.Value}"));
                                    sb.AppendLine();
                                    Console.Write(sb.ToString());
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
                                    var sb = new StringBuilder();
                                    sb.AppendLine($"Point: {pointValues.Key} Interpolated Values " +
                                        $"Interval: {interval.ToString()}");
                                    sb.AppendLine(new string('-', 45));
                                    pointValues.Value.ForEach(v => sb.AppendLine($"{v.Timestamp}, {v.Value}"));
                                    sb.AppendLine();
                                    Console.Write(sb.ToString());
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
                                var sb = new StringBuilder();
                                sb.AppendLine($"Point: {pt.Name} {calculationBasis} Summary");
                                sb.AppendLine(new string('-', 45));
                                foreach (var s in summaries)
                                {
                                    AFValues vals = s.Value;
                                    foreach (var v in vals)
                                    {
                                        if (v.Value.GetType() != typeof(PIException))
                                        {
                                            if (string.Compare(s.Key.ToString(), "Minimum", true) == 0
                                                     || string.Compare(s.Key.ToString(), "Maximum", true) == 0)
                                                sb.AppendLine($"{s.Key,-16}: {v.Value,-20} {v.Timestamp}");
                                            else
                                                sb.AppendLine($"{s.Key,-16}: {v.Value}");
                                        }
                                        else
                                            sb.AppendLine($"{s.Key,-16}: {v}");
                                    }
                                }
                                sb.AppendLine();
                                Console.Write(sb.ToString());
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
                    case "uploadcsv":
                        {
                            string addlparam2 = string.Empty;
                            AFUpdateOption updOption = AFUpdateOption.Replace;
                            AFBufferOption bufOption = AFBufferOption.BufferIfPossible;
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
                                case "r":
                                    updOption = AFUpdateOption.Replace;
                                    break;
                                case "i":
                                    updOption = AFUpdateOption.Insert;
                                    break;
                                case "nr":
                                    updOption = AFUpdateOption.NoReplace;
                                    break;
                                case "ro":
                                    updOption = AFUpdateOption.ReplaceOnly;
                                    break;
                                case "inc":
                                    updOption = AFUpdateOption.InsertNoCompression;
                                    break;
                                case "rm":
                                    updOption = AFUpdateOption.Remove;
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

                            if (command == "uploadcsv")
                            {
                                string v;
                                var values = new List<AFValue>();
                                int linescount = 0;
                                int linesparsed = 0;
                                Console.WriteLine($"Point: {pointsList[0].Name} Uploading values ({updOption} {bufOption})");
                                Console.WriteLine(new string('-', 45));

                                while ((v = Console.ReadLine()) != null)  // read till end of file 
                                {
                                    linescount++;
                                    var timevalue = v.Split(new char[] { ',' });
                                    if (timevalue.Length > 1
                                        && DateTime.TryParse(timevalue[0], out DateTime ts))
                                    {
                                        // use datetime as string in AFTime to be treated as localtime   
                                        values.Add(new AFValue(timevalue[1], new AFTime(ts.ToString())));
                                        if (GlobalConfig.Debug) Console.WriteLine($"{timevalue[0]}, {timevalue[1]}");
                                        linesparsed++;
                                    }
                                }
                                Console.WriteLine($"Lines read from file: {linescount}, Succcesfully parsed lines: {linesparsed}");
                                AFErrors<AFValue> errors = pointsList[0].UpdateValues(values, updOption, bufOption);
                                Console.WriteLine($"Uploaded {linesparsed - (errors != null ? errors.Errors.Count : 0)} values");
                                if (errors != null && errors.HasErrors)
                                {
                                    var sb = new StringBuilder();
                                    sb.AppendLine($"Total Errors: {errors.Errors.Count}");
                                    foreach (var e in errors.Errors)
                                    {
                                        sb.AppendLine($"{e.Key} : {e.Value}");
                                    }
                                    sb.AppendLine();
                                    Console.Write(sb.ToString());
                                }
                            }
                            else
                            {
                                foreach (var pt in pointsList)
                                {
                                    AFValue val;
                                    Console.WriteLine($"Point: {pt.Name} {command} ({updOption} {bufOption})");
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
                                    /*buffering data through PIBufSS has a limitation where error feedback from PI Data Archive
                                    cannot be returned to the caller*/
                                    pt.UpdateValue(value: val, option: updOption, bufferOption: bufOption);
                                    Console.WriteLine($"Successfully {command}d");
                                    if (bufOption != AFBufferOption.DoNotBuffer)
                                        Console.WriteLine($"Caution: Using PIBufSS has no error feedback to the caller");
                                }
                            }
                            Console.WriteLine();
                            break;
                        }
                    case "delete":
                        {
                            AFTimeRange timeRange = new AFTimeRange(st, et);
                            var bufOption = AFBufferOption.BufferIfPossible;
                            switch (addlparam1)
                            {
                                case "dnb":
                                    bufOption = AFBufferOption.DoNotBuffer;
                                    break;
                                case "buf":
                                    bufOption = AFBufferOption.Buffer;
                                    break;
                            }

                            if (myServer.Supports(PIServerFeature.DeleteRange))
                            {
                                foreach (var pt in pointsList)
                                {
                                    int delcount = 0;
                                    var sb = new StringBuilder();
                                    var intervalDefinitions = new AFTimeIntervalDefinition(timeRange, 1);
                                    //getting the count of events - optional
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
                                                delcount = v.ValueAsInt32(); //count 
                                        }
                                    }
                                    if (delcount > 0)
                                    {
                                        var errs = pt.ReplaceValues(timeRange, new List<AFValue>() { }, bufOption);
                                        if (errs != null)
                                        {
                                            foreach (var e in errs.Errors)
                                            {
                                                sb.AppendLine($"{e.Key}: {e.Value}");
                                                delcount--;
                                            }
                                        }
                                    }
                                    sb.AppendLine($"Point: {pt.Name} Deleted {delcount} events");
                                    sb.AppendLine(new string('-', 45));
                                    sb.AppendLine();
                                    Console.Write(sb.ToString());
                                }
                            }
                            else
                            {
                                foreach (var pt in pointsList)
                                {
                                    int delcount = 0;
                                    var sb = new StringBuilder();
                                    AFValues vals = pt.RecordedValues(timeRange: timeRange,
                                                                      boundaryType: AFBoundaryType.Inside,
                                                                      filterExpression: null,
                                                                      includeFilteredValues: false,
                                                                      maxCount: 0
                                                                      );
                                    delcount = vals.Count;
                                    if (delcount > 0)
                                    {
                                        var errs = pt.UpdateValues(values: vals,
                                                               updateOption: AFUpdateOption.Remove,
                                                               bufferOption: bufOption);
                                        if (errs != null)
                                        {
                                            foreach (var e in errs.Errors)
                                            {
                                                sb.AppendLine($"{e.Key}: {e.Value}");
                                                delcount--;
                                            }
                                        }
                                    }
                                    sb.AppendLine($"Point: {pt.Name} Deleted {delcount} events");
                                    sb.AppendLine(new string('-', 45));
                                    sb.AppendLine();
                                    Console.Write(sb.ToString());
                                }
                            }
                            break;
                        }
                    case "pointchanges":
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("Monitoring PI Point Changes");
                            
                            // Used to serialize / deserialize the cookie
                            IFormatter formatter = new BinaryFormatter();
                            // Cookie used to get changes since the last call
                            PIPointChangesCookie cookie = null;
                            // File used to persist the cookie while the process is not running.
                            FileStream cookieFile = null;
                            try
                            {
                                if (File.Exists("cookie.dat"))
                                {
                                    using (cookieFile = new FileStream("cookie.dat", FileMode.Open))
                                    {
                                        // Try to read the cookie from the file
                                        cookie = (PIPointChangesCookie)formatter.Deserialize(cookieFile);
                                        sb.AppendLine("Previously cached cookie file read");
                                        cookieFile.Close();
                                    }
                                }
                            }
                            catch (FileNotFoundException ex) //should not hit
                            {
                                throw new ArgumentException("Cookie has never been persisted " + ex.Message);
                            }
                            catch (SerializationException ex)
                            {
                                throw new ArgumentException("Cookie could not be read " + ex.Message);

                            }
                            catch (SecurityException ex)
                            {
                                throw new ArgumentException("No permission to read cookie file " + ex.Message);
                            }
                            catch (IOException ex)
                            {
                                throw new ArgumentException("I/O Exception " + ex.Message);    
                            }
                            sb.AppendLine("Point Name (Point Id)");
                            sb.AppendLine(new string('-', 45));
                            pointsList.ToList().ForEach(p => sb.AppendLine($"{p.Name} ({p.ID})"));
                            sb.AppendLine();
                            Console.Write(sb.ToString());

                            // If the cookie is null, initialize it to start monitoring changes
                            if (cookie is null)
                                   myServer.FindChangedPIPoints(GlobalConfig.MaxPointChange, pointsList, out cookie);

                            while (!GlobalConfig.CancelSignups)
                            {
                                // Log changes that have occurred since the last call
                                IList<PIPointChangeInfo> changes = myServer.FindChangedPIPoints(GlobalConfig.MaxPointChange, 
                                                                                                            cookie, out cookie);
                                if (!(changes is null))
                                {
                                    foreach (PIPointChangeInfo change in changes)
                                    {
                                       Console.WriteLine($"ID: {change.ID, -5}, Action: {change.Action}, {DateTime.Now}");
                                    }
                                }
                                Thread.Sleep(GlobalConfig.PointChangeFreq);
                            }
                            try
                            {
                                using (cookieFile = new FileStream("cookie.dat", FileMode.Create))
                                {
                                    // Write the cookie to the file
                                    formatter.Serialize(cookieFile, cookie);
                                    cookieFile.Close();
                                    Console.WriteLine("Point changes cache saved to cookie file");
                                }
                            }
                            catch (SerializationException ex)
                            {
                                throw new ArgumentException("Cookie could not be read " + ex.Message);

                            }
                            catch (SecurityException ex)
                            {
                                throw new ArgumentException("No permission to read cookie file " + ex.Message);
                            }
                            catch (IOException ex)
                            {
                                throw new ArgumentException("I/O Exception " + ex.Message);
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
                                        ParseArgs.PrintHelp("No valid PI Points");
                                        if (timeSeriesDatapipe != null)
                                        {
                                            timeSeriesDatapipe.Close();
                                            timeSeriesDatapipe.Dispose();
                                        }
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
                                    Thread.Sleep(GlobalConfig.PipeCheckFreq);
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
                                ParseArgs.PrintHelp("No valid PI Points");
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
                                Thread.Sleep(GlobalConfig.PipeCheckFreq);
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
            catch
            {
                throw;
            }
        }
    }
}
