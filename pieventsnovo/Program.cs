using OSIsoft.AF.PI;
using System;
using System.Collections.Generic;
using OSIsoft.AF;
using OSIsoft.AF.Time;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using System.Reflection;

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
            if (DEBUG)
                Console.WriteLine($"Main thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            if (DEBUG)
                Console.WriteLine($"Arg length {args.Length}");

            bool cancelSignups = false;
            if (args.Length == 0)
            {
                PrintHelp("No arguments provided");
                return;
            }
            if (args[0] == "-?" || args[0] == "-h" || args[0] == "-help")
            {
                PrintHelp("Help Me!");
                return;
            }
            if (args[0] == "-v" || args[0] == "-ver" || args[0] == "-version")
            {
                Console.WriteLine($"{Assembly.GetExecutingAssembly()}");
                AssemblyName[] names = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
                foreach (var n in names) Console.WriteLine($"{n.Name} {n.Version}");
                return;
            }

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
            var pointsList = new List<PIPoint>();
            var command = String.Empty;
            var startTime = String.Empty;
            var endTime = String.Empty;
            var serverName = String.Empty;
            var addlparam1 = string.Empty;
            var addlparam2 = string.Empty;
            var tagMasks = new string[] { };
            var times = new string[] { };
            var summaryDuration = new AFTimeSpan(0, 0, 0, 0, 10, 0, 0); // 10mins
            var st = new AFTime();
            var et = new AFTime();
            PIServer myServer;
          

            if (commandsList.Contains(args[0].Substring(1)))
            {
                command = args[0].Substring(1);
            }
            else
            {
                PrintHelp($"Unknown command {args[0]}");
                return;
            }

            if (args.Length > 1)
            {
                tagMasks = args[1].Split(new char[] { ',' });
            }
            else
            {
                PrintHelp("Tag names not specified");
                return;
            }

            int serverindex = 2;
            switch (command)
            {
                case "snap":
                case "sign,a":
                case "sign,s":
                case "sign,sa":
                case "sign,as":
                    break;
                case "arclist":
                case "count":
                case "interp":
                case "plot":
                case "delete":
                case "summaries":
                    {

                        if (args.Length > 2 && !(args[2] == "-server"))
                        {
                            serverindex++;
                            times = args[2].Split(new char[] { ',' });
                            if (times.Length > 1)
                            {
                                startTime = times[0];
                                endTime = times[1];
                            }
                            if (times.Length > 2) addlparam1 = times[2];
                        }
                        else
                        {
                            PrintHelp("Missing Start and(or) End Time");
                            return;
                        }
                        break;
                    }
                case "update":
                case "annotate":
                    {

                        if (args.Length > 2 && !(args[2] == "-server"))
                        {
                            times = args[2].Split(new char[] { ',' });
                            serverindex++;
                        }
                    }
                    break;
            }

            if (DEBUG) Console.WriteLine($"server index: {serverindex}");
            if (args.Length > (serverindex + 1) && args[serverindex] == "-server")
            {
                serverName = args[serverindex + 1];
            }

            if (!String.IsNullOrEmpty(startTime) && !String.IsNullOrEmpty(endTime))
            {
                if (!AFTime.TryParse(startTime, out st))
                {
                    PrintHelp($"Invalid start time {startTime}");
                    return;
                }
                if (!AFTime.TryParse(endTime, out et))
                {
                    PrintHelp($"Invalid end time {endTime}");
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
                    PrintHelp($"Server {serverName} not found in KST");
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
                PrintHelp("Server Connection error: " + ex.Message);
                return;
            }

            //Handle KeyPress event from the user
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                if (DEBUG)
                    Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
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
                    PrintHelp("No valid PI Points, " + $"disconnecting server {myServer.Name}");
                    myServer.Disconnect();
                    System.Threading.Thread.Sleep(200);
                    return;
                }
            }
            catch (Exception ex)
            {
                PrintHelp("Tagmask error " + ex.Message);
                return;
            }

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
                                    //{
                                    //    Console.Write("Enter new data: ");
                                    //    var data = Console.ReadLine();
                                    //}
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
                            bool snapOnly = false;
                            bool archOnly = false;
                            if (command.Substring(5) == "a")
                            {
                                archOnly = true;
                                Console.WriteLine("Signing up for Archive events");
                            }
                            else if (command.Substring(5) == "s")
                            {
                                snapOnly = true;
                                Console.WriteLine("Signing up for Snapshot events");
                            }
                            else
                            {
                                Console.WriteLine("Signing up for Snapshot & Archive events");
                            }

                            PIDataPipe snapDatapipe;
                            if (Int32.TryParse(myServer.ServerVersion.Substring(4, 3), out int srvbuild) && srvbuild >= 395)
                            { snapDatapipe = new PIDataPipe(AFDataPipeType.TimeSeries); }
                            else
                            { snapDatapipe = new PIDataPipe(AFDataPipeType.Snapshot); }

                            var archDatapipe = new PIDataPipe(AFDataPipeType.Archive);

                            if (snapOnly)
                            {
                                archDatapipe.Dispose();
                                var errs = snapDatapipe.AddSignups(pointsList);
                                snapDatapipe.Subscribe(new DataPipeObserver("Snapshot"));
                                if (errs != null)
                                {
                                    foreach (var e in errs.Errors)
                                    {
                                        Console.WriteLine($"Failed snapshot signup {e.Key}, {e.Value}");
                                        pointsList.Remove(e.Key);
                                    }
                                }
                            }
                            else if (archOnly)
                            {
                                snapDatapipe.Dispose();
                                var errs = archDatapipe.AddSignups(pointsList);
                                if (errs != null)
                                {
                                    foreach (var e in errs.Errors)
                                    {
                                        Console.WriteLine($"Failed archive signup {e.Key}, {e.Value}");
                                        pointsList.Remove(e.Key);
                                    }
                                }
                                archDatapipe.Subscribe(new DataPipeObserver("Archive"));
                            }
                            else
                            {
                                var errs1 = snapDatapipe.AddSignups(pointsList);
                                var errs2 = archDatapipe.AddSignups(pointsList);
                                var errPoints = new List<string>();
                                if (errs1 != null)
                                {
                                    foreach (var e in errs1.Errors)
                                    {
                                        Console.WriteLine($"Failed snapshot signup {e.Key}, {e.Value}");
                                        errPoints.Add(e.Key.Name);
                                    }
                                }
                                if (errs2 != null)
                                {
                                    foreach (var e in errs2.Errors)
                                    {
                                        Console.WriteLine($"Failed archive signup {e.Key}, {e.Value}");
                                        if (errPoints.Contains(e.Key.Name)) pointsList.Remove(e.Key);
                                    }
                                }
                                snapDatapipe.Subscribe(new DataPipeObserver("Snapshot"));
                                archDatapipe.Subscribe(new DataPipeObserver("Archive "));
                            }
                            if (pointsList.Count == 0)
                            {
                                PrintHelp("No valid PI Points, " + $"disconnecting server {myServer.Name}");
                                myServer.Disconnect();
                                System.Threading.Thread.Sleep(200);
                                return;
                            }
                            Console.WriteLine();
                            Console.WriteLine("Subscribed Points (current value): ");
                            foreach (var p in pointsList)
                            {
                                Console.WriteLine($"{p.Name,-12}, {p.EndOfStream().Timestamp}, {p.EndOfStream()}");
                            }
                            Console.WriteLine(new string('-', 45));

                            //Fetch events from the data pipes
                            while (!cancelSignups)
                            {
                                if (snapOnly) { snapDatapipe.GetObserverEvents(20, out bool hasMoreEvents1); }
                                else if (archOnly) { archDatapipe.GetObserverEvents(20, out bool hasMoreEvents2); }
                                else
                                {
                                    snapDatapipe.GetObserverEvents(20, out bool hasMoreEvents1);
                                    archDatapipe.GetObserverEvents(20, out bool hasMoreEvents2);
                                }
                                System.Threading.Thread.Sleep(1000); //every second
                            }

                            Console.WriteLine("Cancelling signups ...");
                            if (snapOnly)
                            {
                                snapDatapipe.Close();
                                snapDatapipe.Dispose();
                            }
                            else if (archOnly)
                            {
                                archDatapipe.Close();
                                archDatapipe.Dispose();
                            }
                            else
                            {
                                snapDatapipe.Close();
                                snapDatapipe.Dispose();

                                archDatapipe.Close();
                                archDatapipe.Dispose();
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (myServer != null)
            {
                myServer.Disconnect();
                if (DEBUG) Console.WriteLine($"Disconnecting from {myServer.Name}");
            }
            Console.WriteLine(new string('~', 45));
        }

        private static void PrintHelp(string msg)
        {
            Console.WriteLine(msg);
            Console.WriteLine(new string('-', 45));
            Console.WriteLine("pieventsnovo.exe <command> <tagmask1[,tagmask2[...]> <paramteters> [-server Name(def=Default Server)]");
            Console.WriteLine("COMMAND \t USAGE <> = required [] = optional");
            Console.WriteLine("-snap <tagmasks> #current value");
            Console.WriteLine("-sign,<[sa]> <tagmasks> s=snapshot, a=archive sa=both #signups ");
            Console.WriteLine("\tOutput: SignupType, PIPoint, TimeStamp,Value, {PipeAction,Arrival time}");
            Console.WriteLine("-arclist <tagmasks> <starttime,endtime>[,MaxCount(def=ArcMaxCollect)] #archive values");
            Console.WriteLine("-interp <tagmasks> <starttime,endtime>[,TimeSpam(def(10m), hh:mm:ss) or c=Count] #interpolated values");
            Console.WriteLine("-plot <tagmasks> <starttime,endtime>[,Intervals(def=640)] #plot data ");
            Console.WriteLine("-summaries <tagmasks> <starttime,endtime>,[e=evt weighted(def) or t=time wt] #point summary");
            Console.WriteLine("-update <tagmasks> [[Mode],[Buffer options]] #append,update,remove");
            Console.WriteLine("\tMode: r(replace,def) i(insert) nr(no replace) ro(repalce only) inc(insert no comp) rm(remove)");
            Console.WriteLine("\tBuffer Option: bip(def, buffer if possible) buf(buffer) dnb(do not buffer)");
            Console.WriteLine("-annotate <tagmasks> [[Mode],[Buffer options]] #add/edit annotation");
            Console.WriteLine("-delete <tagmasks> <starttime,endtime> #remove archive data");
            Console.WriteLine("Example: pieventsnovo.exe -sign,as sinusoid,cdt158 -server MyServer");
            Console.WriteLine("Example: pieventsnovo.exe -arclist sinusoid,cdt158 *-10m,*");
            Console.WriteLine(new string('~', 45));
        }

    }
}
