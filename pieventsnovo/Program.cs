using System;
using System.Collections.Generic;
using OSIsoft.AF;
using OSIsoft.AF.Time;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using System.Linq;
using OSIsoft.AF.PI;

/// <summary>
/// Application to mimic the some of the functionalities of pievents.exe 
/// Uses AF SDK to handle TimeSeries data pipe
/// </summary>
namespace pieventsnovo
{
    class Program
    {
        static bool DEBUG = false;
        static void Main(string[] args)
        {
            Console.WriteLine(new string('~', 45));
            if (DEBUG) Console.WriteLine($"Main thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            if (DEBUG) Console.WriteLine($"Args length: {args.Length}");

            var commandsList = new List<string>()
            {
                "snap",
                "sign,a",
                "sign,s",
                "sign,sa",
                "sign,as",
                "arclist",
                "interp",
                "summaries",
                "plot",
                "update",
                "annotate",
                "delete"
            };
            var pointsList = new PIPointList();
            var command = String.Empty;
            var startTime = String.Empty;
            var endTime = String.Empty;
            var serverName = String.Empty;
            var addlparam1 = string.Empty;
            var addlparam2 = string.Empty;
            var tagMasks = new string[] { };
            var times = new string[] { };
            var summaryDuration = new AFTimeSpan(TimeSpan.FromMinutes(10)); 
            var st = new AFTime();
            var et = new AFTime();
            PIServer myServer;
            bool cancelSignups = false;

            var AppplicationArgs = new ParseArgs(args);
            try
            {
                if (!AppplicationArgs.CheckHelpVersionOrEmpty())
                    return;
                if (!AppplicationArgs.CheckCommandExists(commandsList, out command))
                    return;
                if (!AppplicationArgs.GetTagNames(out tagMasks))
                    return;
                if (!AppplicationArgs.GetAddlParams(command, ref times, ref startTime, ref endTime, ref addlparam1, ref serverName))
                    return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
           
            #region Connect Server, Verify Times and Points
            if (!String.IsNullOrEmpty(startTime) && !String.IsNullOrEmpty(endTime))
            {
                if (!AFTime.TryParse(startTime, out st))
                {
                    AppplicationArgs.PrintHelp($"Invalid start time {startTime}");
                    return;
                }
                if (!AFTime.TryParse(endTime, out et))
                {
                    AppplicationArgs.PrintHelp($"Invalid end time {endTime}");
                    return;
                }
                if (st == et)
                {
                    AppplicationArgs.PrintHelp("Incorrect(same) time interval specified");
                    return;
                }
            }

            try
            {
                PIServers myServers = new PIServers();
                if (string.IsNullOrEmpty(serverName))
                {
                    if (DEBUG) Console.WriteLine("Attempting connection to default server ...");
                    myServer = myServers.DefaultPIServer;
                }
                else if (myServers.Contains(serverName))
                {
                    if (DEBUG) Console.WriteLine($"Attempting connection to {serverName} server ...");
                    myServer = myServers[serverName];
                }
                else
                {
                    AppplicationArgs.PrintHelp($"Server {serverName} not found in KST");
                    return;
                }
                if (myServer != null)
                {
                    myServer.ConnectionInfo.Preference = AFConnectionPreference.Any;
                    myServer.Connect();
                    Console.WriteLine($"Connected to {myServer.Name} as {myServer.CurrentUserIdentityString}");
                }
            }
            catch (Exception ex)
            {
                AppplicationArgs.PrintHelp("Server Connection error: " + ex.Message);
                return;
            }

            try
            {
                foreach (var n in tagMasks)
                {
                    if (PIPoint.TryFindPIPoint(myServer, n, out PIPoint p))
                    {
                        if (!pointsList.Contains(p)) pointsList.Add(p);
                        else Console.WriteLine($"Duplicate point {p.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"Point {n} not found");
                    }
                }
                if (pointsList.Count == 0)
                {
                    AppplicationArgs.PrintHelp("No valid PI Points, " + $"disconnecting server {myServer.Name}");
                    myServer.Disconnect();
                    System.Threading.Thread.Sleep(200);
                    return;
                }
            }
            catch (Exception ex)
            {
                AppplicationArgs.PrintHelp("Tagmask error " + ex.Message);
                return;
            }
            #endregion

            //Handle KeyPress event from the user
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                if (DEBUG) Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine();
                Console.WriteLine("Program termination received from user ...");
                if (command == "sign,s" || command == "sign,as" || command == "sign,sa" || command == "sign,a")
                {
                    cancelSignups = true;
                    System.Threading.Thread.Sleep(1200);
                }
                else
                {
                    if (myServer != null) myServer.Disconnect();
                    Console.WriteLine(new string('~', 45));
                }
            };

            #region Execute Command
            try
            {
                if (DEBUG) Console.WriteLine($"Commad executing {command}");
                Console.WriteLine();
                switch (command)
                {
                    case "snap":
                        {
                            foreach (var pt in pointsList)
                            {
                                Console.WriteLine($"Point: {pt.Name} Id: {pt.ID} Current Value");
                                Console.WriteLine(new string('-', 45));
                                AFValue v = pt.EndOfStream();
                                Console.WriteLine($"{v.Timestamp}, {v.Value}");
                                Console.WriteLine();
                            }
                            break;
                        }
                    case "arclist":
                        {
                            AFTimeRange timeRange = new AFTimeRange(st, et);
                            if (!Int32.TryParse(addlparam1, out int maxcount))
                                maxcount = 0;

                            foreach (var pt in pointsList)
                            {
                                AFValues vals = pt.RecordedValues(timeRange: timeRange,
                                                                  boundaryType: AFBoundaryType.Inside,
                                                                  filterExpression: null,
                                                                  includeFilteredValues: false,
                                                                  maxCount: maxcount
                                                                  );
                                Console.WriteLine($"Point: {pt.Name} Archive Values Count: {vals.Count}");
                                Console.WriteLine(new string('-', 45));
                                vals.ForEach(v => Console.WriteLine($"{v.Timestamp}, {v.Value}"));
                                Console.WriteLine();
                            }
                            break;
                        }
                    case "delete":
                        {
                            AFTimeRange timeRange = new AFTimeRange(st, et);
                            foreach (var pt in pointsList)
                            {
                                AFValues vals = pt.RecordedValues(timeRange: timeRange,
                                                                  boundaryType: AFBoundaryType.Inside,
                                                                  filterExpression: null,
                                                                  includeFilteredValues: false,
                                                                  maxCount: 0
                                                                  );
                                int delcount = vals.Count;
                                if (vals.Count > 0)
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
                            break;
                        }
                    case "plot":
                        {
                            AFTimeRange timeRange = new AFTimeRange(st, et);
                            if (!Int32.TryParse(addlparam1, out int intervals))
                                intervals = 640; //horizontal pixels in the trend

                            foreach (var pt in pointsList)
                            {
                                AFValues vals = pt.PlotValues(timeRange, intervals);
                                Console.WriteLine($"Point: {pt.Name} Plot Values Interval: {intervals} Count: {vals.Count}");
                                Console.WriteLine(new string('-', 45));
                                vals.ForEach(v => Console.WriteLine($"{v.Timestamp}, {v.Value}"));
                                Console.WriteLine();
                            }
                            break;
                        }
                    case "interp":
                        {
                            AFTimeRange timeRange = new AFTimeRange(st, et);
                            if (addlparam1.StartsWith("c="))
                            {
                                if (!Int32.TryParse(addlparam1.Substring(2), out int count))
                                    count = 10; //default count

                                foreach (var pt in pointsList)
                                {
                                    AFValues vals = pt.InterpolatedValuesByCount(timeRange: timeRange,
                                                                                 numberOfValues: count,
                                                                                 filterExpression: null,
                                                                                 includeFilteredValues: false
                                                                                );
                                    Console.WriteLine($"Point: {pt.Name} Interpolated Values Count: {count}");
                                    Console.WriteLine(new string('-', 45));
                                    vals.ForEach(v => Console.WriteLine($"{v.Timestamp}, {v.Value}"));
                                    Console.WriteLine();
                                }
                            }
                            else
                            {
                                if (!AFTimeSpan.TryParse(addlparam1, out AFTimeSpan interval) || interval == new AFTimeSpan(0))
                                    interval = summaryDuration;

                                foreach (var pt in pointsList)
                                {
                                    AFValues vals = pt.InterpolatedValues(timeRange: timeRange,
                                                                          interval: interval,
                                                                          filterExpression: null,
                                                                          includeFilteredValues: false
                                                                         );
                                    Console.WriteLine($"Point: {pt.Name} Interpolated Values Interval: {interval.ToString()} Count: {vals.Count}");
                                    Console.WriteLine(new string('-', 45));
                                    vals.ForEach(v => Console.WriteLine($"{v.Timestamp}, {v.Value}"));
                                    Console.WriteLine();
                                }
                            }
                            break;
                        }
                    case "summaries":
                        {
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
                            break;
                        }
                    case "update":
                    case "annotate":
                        {
                            AFUpdateOption updateOption = AFUpdateOption.Replace;
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
                                Console.WriteLine($"Point: {pt.Name} Update Value ({updateOption} {bufOption})");
                                Console.WriteLine(new string('-', 45));
                                Console.Write("Enter timestamp: ");
                                var time = Console.ReadLine();
                                Console.Write("Enter new data: ");
                                var data = Console.ReadLine();

                                if (AFTime.TryParse(time, out AFTime ts) && Double.TryParse(data, out var value))
                                {
                                    // to check if value exists a timestamp
                                    //if (pt.RecordedValuesAtTimes(new List<AFTime>() {ts},AFRetrievalMode.Exact).GetType() != typeof(PIException))
                                    try
                                    {
                                        AFValue val = new AFValue(value, ts);
                                        if (command == "annotate")
                                        {
                                            Console.Write("Enter annotation: ");
                                            var ann = Console.ReadLine();
                                            pt.SetAnnotation(val, ann);
                                        }
                                        pt.UpdateValue(value: val, option: updateOption, bufferOption: bufOption);
                                        Console.WriteLine("Successfully updated");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                                Console.WriteLine();
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
                                // PI Data Archive ver. >= 3.4.395 supports TimeSeries and future data
                                //if (Int32.TryParse(myServer.ServerVersion.Substring(4, 3), out int srvbuild) && srvbuild >= 395);
                                if (myServer.Supports(PIServerFeature.TimeSeriesDataPipe))
                                    snapDatapipe = new PIDataPipe(AFDataPipeType.TimeSeries);
                                else
                                    snapDatapipe = new PIDataPipe(AFDataPipeType.Snapshot);

                                Console.WriteLine("Signing up for Snapshot events");
                                var errs = snapDatapipe.AddSignups(pointsList);
                                snapDatapipe.Subscribe(new DataPipeObserver("Snapshot"));
                                if (errs != null)
                                {
                                    foreach (var e in errs.Errors)
                                    {
                                        Console.WriteLine($"Failed snapshot signup {e.Key}, {e.Value}");
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
                                        Console.WriteLine($"Failed archive signup {e.Key}, {e.Value}");
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
                                AppplicationArgs.PrintHelp("No valid PI Points, " + $"disconnecting server {myServer.Name}");
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
                            foreach (var p in pointsList)
                            {
                                Console.WriteLine($"{p.Name,-12}, {p.EndOfStream().Timestamp}, {p.EndOfStream()}");
                            }
                            Console.WriteLine(new string('-', 45));

                            //Fetch events from the data pipes
                            const int maxEventCount = 20;
                            while (!cancelSignups)
                            {
                                if (snapSubscribe)
                                    snapDatapipe.GetObserverEvents(maxEventCount, out bool hasMoreEvents1);
                                if (archSubscribe)
                                    archDatapipe.GetObserverEvents(maxEventCount, out bool hasMoreEvents2);
                                System.Threading.Thread.Sleep(1000); //every second
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
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                AppplicationArgs.PrintHelp(ex.Message);
            }
            #endregion

            if (myServer != null)
            {
                myServer.Disconnect();
                if (DEBUG) Console.WriteLine($"Disconnecting from {myServer.Name}");
            }
            Console.WriteLine(new string('~', 45));
        }

       

    }
}
