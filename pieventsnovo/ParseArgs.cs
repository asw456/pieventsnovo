using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace pieventsnovo
{
    public class ParseArgs
    {
        string[] args;
        public ParseArgs(string[] args)
        {
            this.args = args; 
        }

        public bool ParseHelpEmpty()
        {
            if (args.Length == 0)
            {
                PrintHelp("No arguments provided");
                return false;
            }
            if (args[0] == "-?" || args[0] == "-h" || args[0] == "-help")
            {
                PrintHelp("Help Me!");
                return false;
            }
            if (args[0] == "-v" || args[0] == "-ver" || args[0] == "-version")
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{Assembly.GetExecutingAssembly()}");
                AssemblyName[] names = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
                foreach (var n in names) sb.AppendLine($"{n.Name} {n.Version}");
                Console.WriteLine(sb.ToString());
                return false;
            }
            return true;
        }

        public bool CheckCommandExists(IList<string> commandsList, out string command)
        {
            if (commandsList.Contains(args[0].Substring(1)))
            {
                command = args[0].Substring(1);
                return true;
            }
            else
            {
                PrintHelp($"Unknown command {args[0]}");
                command = null;
                return false;
            }
        }

        public bool GetTagNames(out string[] tagmasks)
        {
            if (args.Length > 1)
            {
                tagmasks = args[1].Split(new char[] { ',' });
                return true;
            }
            else 
            {
                PrintHelp("Tag names not specified");
                tagmasks = null;
                return false;
            }
        }

        public bool GetAddlParams(string command, ref string[] times, ref string startTime, ref string endTime,
                                  ref string addlparam1, ref string serverName)
        {
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
                            return false;
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
            if (args.Length > (serverindex + 1) && args[serverindex] == "-server")
            {
                serverName = args[serverindex + 1];
            }
            return true;
        }

        public void PrintHelp(string msg)
        {
            var sb = new StringBuilder();
            sb.AppendLine(msg);
            sb.AppendLine(new string('-', 45));
            sb.AppendLine("pieventsnovo.exe <command> <tagmask1[,tagmask2[...]> <paramteters> [-server Name(def=Default Server)]");
            sb.AppendLine("COMMAND \t USAGE <> = required [] = optional");
            sb.AppendLine("-snap <tagmasks> #current value");
            sb.AppendLine("-sign,<[sa]> <tagmasks> s=snapshot, a=archive sa=both #signups ");
            sb.AppendLine("\tOutput: SignupType, PIPoint, TimeStamp,Value, {PipeAction,Arrival time}");
            sb.AppendLine("-arclist <tagmasks> <starttime,endtime>[,MaxCount(def=ArcMaxCollect)] #archive values");
            sb.AppendLine("-interp <tagmasks> <starttime,endtime>[,TimeSpam(def(10m), hh:mm:ss) or c=Count] #interpolated values");
            sb.AppendLine("-plot <tagmasks> <starttime,endtime>[,Intervals(def=640)] #plot data ");
            sb.AppendLine("-summaries <tagmasks> <starttime,endtime>,[e=evt weighted(def) or t=time wt] #point summary");
            sb.AppendLine("-update <tagmasks> [[Mode],[Buffer options]] #append,update,remove");
            sb.AppendLine("\tMode: r(replace,def) i(insert) nr(no replace) ro(repalce only) inc(insert no comp) rm(remove)");
            sb.AppendLine("\tBuffer Option: bip(def, buffer if possible) buf(buffer) dnb(do not buffer)");
            sb.AppendLine("-annotate <tagmasks> [[Mode],[Buffer options]] #add/edit annotation");
            sb.AppendLine("-delete <tagmasks> <starttime,endtime> #remove archive data");
            sb.AppendLine("Example: pieventsnovo.exe -sign,as sinusoid,cdt158 -server MyServer");
            sb.AppendLine("Example: pieventsnovo.exe -arclist sinusoid,cdt158 *-10m,*");
            sb.AppendLine(new string('~', 45));
            Console.WriteLine(sb.ToString());
        }
    }
}
