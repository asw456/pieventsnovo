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
        static void Main(string[] args)
        {
            Console.WriteLine(new string('~', 45));
            if (GlobalValues.Debug) Console.WriteLine($"Main thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            if (GlobalValues.Debug) Console.WriteLine($"Args length: {args.Length}");

            var commandsList = new List<string>()
            {
                "snap",
                "sign,a",
                "sign,s",
                "sign,sa",
                "sign,as",
                "sign,tms",
                "arclist",
                "interp",
                "plot",
                "summaries",
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
            var tagMasks = new string[] { };
            var times = new string[] { };
            var summaryDuration = new AFTimeSpan(TimeSpan.FromMinutes(10));
            var st = new AFTime();
            var et = new AFTime();
            PIServer myServer;

            try
            {
                var AppplicationArgs = new ParseArgs(args);
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
                ParseArgs.PrintHelp(ex.Message);
                return;
            }

            #region Connect Server, Verify Times and Points
            if (!String.IsNullOrEmpty(startTime) && !String.IsNullOrEmpty(endTime))
            {
                if (!AFTime.TryParse(startTime, out st))
                {
                    ParseArgs.PrintHelp($"Invalid start time {startTime}");
                    return;
                }
                if (!AFTime.TryParse(endTime, out et))
                {
                    ParseArgs.PrintHelp($"Invalid end time {endTime}");
                    return;
                }
                if (st == et)
                {
                    ParseArgs.PrintHelp("Incorrect(same) time interval specified");
                    return;
                }
            }

            try
            {
                PIServers myServers = new PIServers();
                if (string.IsNullOrEmpty(serverName))
                {
                    if (GlobalValues.Debug) Console.WriteLine("Attempting connection to default server ...");
                    myServer = myServers.DefaultPIServer;
                }
                else if (myServers.Contains(serverName))
                {
                    if (GlobalValues.Debug) Console.WriteLine($"Attempting connection to {serverName} server ...");
                    myServer = myServers[serverName];
                }
                else
                {
                    ParseArgs.PrintHelp($"Server {serverName} not found in KST");
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
                ParseArgs.PrintHelp("Server Connection error: " + ex.Message);
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
                    ParseArgs.PrintHelp("No valid PI Points, " + $"disconnecting server {myServer.Name}");
                    myServer.Disconnect();
                    System.Threading.Thread.Sleep(200);
                    return;
                }
            }
            catch (Exception ex)
            {
                ParseArgs.PrintHelp("Tagmask error " + ex.Message);
                return;
            }
            #endregion

            //Handle KeyPress event from the user
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                if (GlobalValues.Debug) Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine();
                Console.WriteLine("Program termination received from user ...");
                if (command == "sign,s" || command == "sign,as" || command == "sign,sa" || command == "sign,a" || command == "sign,tms")
                {
                    GlobalValues.CancelSignups = true;
                    System.Threading.Thread.Sleep(Convert.ToInt32(GlobalValues.PipeCheckFreq*1.2));
                }
                else
                {
                    if (myServer != null) myServer.Disconnect();
                    Console.WriteLine(new string('~', 45));
                }
            };

            var Exec = new ExecuteCommand();
            if (GlobalValues.Debug) Console.WriteLine($"Commad executing {command}");
            Exec.Excecute(command, pointsList, st, et, summaryDuration, times, addlparam1, myServer);

            if (myServer != null)
            {
                myServer.Disconnect();
                if (GlobalValues.Debug) Console.WriteLine($"Disconnecting from {myServer.Name}");
            }
            Console.WriteLine(new string('~', 45));
        }
    }
}
