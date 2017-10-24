
# PIEventsNovo
A console application utilty that provides basic data access and datapipe signup features.

### Usage of the arguments 
pieventsnovo.exe \<command\> \<tagmask1\[,tagmask2\[...\]\> \<paramteters\> \[-server Name\(def=Default Server\)\]

| Command | Parameters  \<\> = required \[\] = optional | Description|
| :---: | --- | --- |
| -snap | \<tagmasks\> | current value|
| -sign,<[sa] or [t]> | \<tagmasks\> | signup type s=snapshot, a=archive sa=both, t=timeseries |
| | Output format | SignupType, PIPoint, TimeStamp,Value, {PipeAction,Arrival time} |
| -arclist | \<tagmasks\> \<starttime,endtime\>\[,MaxCount\(def=ArcMaxCollect\)\] | archive values|
| -interp  | \<tagmasks\> \<starttime,endtime\>\[,TimeSpam\(def\(10m\), hh:mm:ss\) or c=Count\] | interpolated values|
| -plot  | \<tagmasks\> \<starttime,endtime\>\[,Intervals\(def=640\)\] | plot values |
| -summaries  | \<tagmasks\> \<starttime,endtime\>,\[e=evt weighted\(def\) or t=time wt\] | point summary data |
| -update   | \<tagmasks\> \[\[Mode\],\[Buffer options\]\] | update an event \(append,update,remove\) |
||Mode |r\(replace,def\) i\(insert\) nr\(no replace\) ro\(repalce only\) inc\(insert no comp\) rm\(remove\)|
||Buffer Option |bip\(def, buffer if possible\) buf\(buffer\) dnb\(do not buffer\)|
|-annotate| \<tagmasks\> \[\[Mode\],\[Buffer options\]\]| add/edit annotation|
|-delete| \<tagmasks\> \<starttime,endtime\>|delete archive data|

### The project consists of the following classes 
```
Program       : Consists of Main and calls to methods of other utility classes 
ParseArgs     : Parse the arguments provided to the application 
ExecuteCommand: Takes in the arguments and executes the user specified command
GlobalConfig  : Holds the configuration parameters, requires changes to be applied during compile time 
```

### Software and Assembly Versions
```
Developed: Microsoft Visual Studio Community 2017 15.4.1
Target Framework: .NET Framework 4.5.2
MSCorLib: 4.0.0.0
OSIsoft.AFSDK: 4.0.0.0 Version 2.8.5.7759
```
### References
[AF SDK  Library](https://techsupport.osisoft.com/Documentation/PI-AF-SDK/html/1a02af4c-1bec-4804-a9ef-3c7300f5e2fc.htm) .NET assembly that provides structured access to OSIsoft data

### Points to Note/Possible improvements
```
The target framework (4.5.2) is purposefully chosen to help the applicaiton run with least requirements, 
but you should consider targeting higher versions of the framework (successfully tested on 4.6.1)

Certain code sections are commented out to illustrate concepts and provide leads to the developers
who wish to implement the ideas in their build.

Connecting to a particular data archive requires an entry to be present in the Known Servers Table (KST). 
For collectives the connection is based on the priority set in KST. 

For the bulk data access methods (PIPointList) used in snap, arclist, plot and interp;
if the server version is greater than or equal to 3.4.390 (PI Server 2012), then the SDK is aware 
that it supports the bulk list data access calls. If the version is less than 3.4.390, then the SDK
will internally call the singular data access equivalent in parallel on each PIPoint as an alternative
to produce the same results. This is verified using Supports(PIServerFeature.BulkDataAccess)

PI Data Archive version greater than 3.4.395 (PI Server 2015) supports TimeSeries data pipe and Future data.

FindPIPoints methods used Program.cs, is probably more efficient in finding PI points and helps 
avoid multiple calls to pibasess. However the other method can be used to provide info on missing/duplicates points.

RepalceValues method (Delete) requires PI Data Archive 2016 or later that supports DeleteRange feature. 
This is indicated by Supports(PIServerFeature) check returning true for the case of DeleteRange.
Note this leads to a different DataPipeAction (Refresh) compared to DataPipeAction (Delete)

The ExecuteCommand class is not made static for now with the idea of splitting the switch cases 
into separate methods later on. The parameters passed to it can be tightened in this approach. 

The summaries call currently does not feature support for a specific summary type and this can be 
implemented with minor changes to argument parsing and the parameter to the method. 

Bulk data access methods for the remaining commands (delete,summaries) can be implemented with the help 
of commented sections.  
```

