
# PIEventsNovo
A console application utilty that provides basic data access and sign up facilities to the PI Data Archive

### The project consists of the following classes 
```
*Program*       : Consists of Main and execution of other classes are through this
*ParseArgs*     : Parse the arguments provided to the application 
*ExecuteCommand*: Takes in the arguments and executes the user specified command
*GlobalConfig*  : Holds the configuration parameters, requires changes to be applied during compile time 
```

### Usage of the arguments 
`pieventsnovo.exe <command> <tagmask1[,tagmask2[...]> <paramteters> [-server Name(def=Default Server)]
```
COMMAND 	 USAGE <> = required [] = optional
**-snap <tagmasks>** #current value
**-sign,<[sa] or [t]> <tagmasks>** s=snapshot, a=archive sa=both, t=timeseries #signups
	Output: SignupType, PIPoint, TimeStamp,Value, {PipeAction,Arrival time}
-arclist <tagmasks> <starttime,endtime>[,MaxCount(def=ArcMaxCollect)] #archive values
-interp <tagmasks> <starttime,endtime>[,TimeSpam(def(10m), hh:mm:ss) or c=Count] #interpolated values
-plot <tagmasks> <starttime,endtime>[,Intervals(def=640)] #plot data 
-summaries <tagmasks> <starttime,endtime>,[e=evt weighted(def) or t=time wt] #point summary
-update <tagmasks> [[Mode],[Buffer options]] #append,update,remove
	Mode: r(replace,def) i(insert) nr(no replace) ro(repalce only) inc(insert no comp) rm(remove)
	Buffer Option: bip(def, buffer if possible) buf(buffer) dnb(do not buffer)
-annotate <tagmasks> [[Mode],[Buffer options]] #add/edit annotation
-delete <tagmasks> <starttime,endtime> #remove archive dat
```

### References
[AF SDK  Library](https://techsupport.osisoft.com/Documentation/PI-AF-SDK/html/1a02af4c-1bec-4804-a9ef-3c7300f5e2fc.htm) .NET assembly that provides structured access to OSIsoft data
