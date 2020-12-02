# NLog.LiteDb

NLog target for the LiteDB database

[![Build status](https://doughnutspublishing.visualstudio.com/NLog.Targets.LiteDB/_apis/build/status/NLog.Targets.LiteDB-CI)](https://doughnutspublishing.visualstudio.com/NLog.Targets.LiteDB/_build/latest?definitionId=1)

## Configuration Syntax

Examples below for the 4 connection string types: 

```xml
  <targets>
    <!-- Legacy Target still supported-->
      <target name="legacy" xsi:type="liteDBTarget"
              connectionString="filename=NLog.db"
              collectionName="DefaultLog" FlushTimeout="100" BufferSize="100" SlidingTimeout="true" OverflowAction="Flush">
        <property name="ThreadID" layout="${threadid}" bsonType="Int32" />
        <property name="ThreadName" layout="${threadname}" />
        <property name="ProcessID" layout="${processid}" bsonType="Int32"  />
        <property name="ProcessName" layout="${processname:fullName=true}" />
        <property name="UserName" layout="${windows-identity}" />
     </target>
    <target name="special" xsi:type="liteDBTarget"
            connectionString="special={MyDocuments}\testApp\NLog.db"
            collectionName="DefaultLog" IsJournaling="false" FlushTimeout="100" BufferSize="100" SlidingTimeout="true" OverflowAction="Flush">
      <property name="ThreadID" layout="${threadid}" bsonType="Int32" />
      <property name="ThreadName" layout="${threadname}" />
      <property name="ProcessID" layout="${processid}" bsonType="Int32"  />
      <property name="ProcessName" layout="${processname:fullName=true}" />
      <property name="UserName" layout="${windows-identity}" />
    </target>
    <target name="path" xsi:type="liteDBTarget"
        connectionString="path=c:\temp\testApp\NLog.db"
        collectionName="DefaultLog" IsJournaling="false" FlushTimeout="100" BufferSize="100" SlidingTimeout="true" OverflowAction="Flush">
      <property name="ThreadID" layout="${threadid}" bsonType="Int32" />
      <property name="ThreadName" layout="${threadname}" />
      <property name="ProcessID" layout="${processid}" bsonType="Int32"  />
      <property name="ProcessName" layout="${processname:fullName=true}" />
      <property name="UserName" layout="${windows-identity}" />
    </target>
    <target name="file" xsi:type="liteDBTarget"
    connectionString="file=NLog.db"
    collectionName="DefaultLog" IsJournaling="false" FlushTimeout="100" BufferSize="100" SlidingTimeout="true" OverflowAction="Flush">
      <property name="ThreadID" layout="${threadid}" bsonType="Int32" />
      <property name="ThreadName" layout="${threadname}" />
      <property name="ProcessID" layout="${processid}" bsonType="Int32"  />
      <property name="ProcessName" layout="${processname:fullName=true}" />
      <property name="UserName" layout="${windows-identity}" />
    </target>
  </targets>

  <rules>
    <logger name="*" minlevel="Trace" maxlevel="Debug" writeTo="special" />
    <logger name="*" minlevel="Info" maxlevel="Warn" writeTo="path" />
    <logger name="*" minlevel="Error" maxlevel="Fatal" writeTo="file" />
  </rules>
```

## Parameters

### General Options

_name_ - Name of the target.

### buffer Options

* **flushtimeout** - timeout (in milliseconds) after which the contents of the buffer will be flushed if there's no write in the specified period of time.  Use -1 to disable timed flushes
* **buffersize** - the number of log events to be buffered ( min 1, max 100).  Default is 100.
* **slidingtimeout** - If sliding timeout is enabled, the inactivity timer is reset after each write, if it is disabled - inactivity timer will count from the first event written to the buffer.
* **overflowaction** -the action to take if the buffer overflows. (default "Flush")
* **Discard** - replace the oldest event with new events without sending events down to the wrapped target.
* **Flush** - flush the entire buffer to the wrapped target.

### Connection Options

_connectionName_ - The name of the connection string to get from the config file.

_connectionString_ - 3 connection string types are permitted.  

* **Special** - this allows for the connection string to utilize special folders - Refer to Microsoft documentation on Environment.SpecialFolder Enum for a full list.  They are case sensitive.
* **Path** - as this suggests - a file path.
* **File** - just a base filename.

### Collection Options
_collectionName_ - The name of the LiteDB collection to write logs to.  
_IsJournaling_ - Journaling is enabled by default.  Specify _IsJournaling_="false" to disable LiteDB journaling.


### Document Options

_includeDefaults_ - Specifies if the default document is created when writing to the collection.  Defaults to true.

_field_ - Specifies a root level document field. There can be multiple fields specified.

_property_ - Specifies a dictionary property on the Properties field. There can be multiple properties specified.

## Examples

### Default Configuration with Extra Properties

#### NLog.config target

```xml
    <target name="liteDB" xsi:type="liteDBTarget"
            connectionString="file=NLog.db"
            collectionName="DefaultLog" FlushTimeout="100" BufferSize="100" SlidingTimeout="true" OverflowAction="Flush">        
      <property name="ThreadID" layout="${threadid}" bsonType="Int32" />
      <property name="ThreadName" layout="${threadname}" />
      <property name="ProcessID" layout="${processid}" bsonType="Int32"  />
      <property name="ProcessName" layout="${processname:fullName=true}" />
      <property name="UserName" layout="${windows-identity}" />
    </target>
```
### NLog.config target (LiteDB journal turned off)

```xml
    <target name="liteDB" xsi:type="liteDBTarget"
            connectionString="file=NLog.db"
            collectionName="DefaultLog" IsJournaling="false" FlushTimeout="100" BufferSize="100" SlidingTimeout="true" OverflowAction="Flush">        
      <property name="ThreadID" layout="${threadid}" bsonType="Int32" />
      <property name="ThreadName" layout="${threadname}" />
      <property name="ProcessID" layout="${processid}" bsonType="Int32"  />
      <property name="ProcessName" layout="${processname:fullName=true}" />
      <property name="UserName" layout="${windows-identity}" />
    </target>
```
or
```xml

    <target name="legacy" xsi:type="liteDBTarget"
            connectionString="filename=NLog.db;journal=false"
            collectionName="DefaultLog" FlushTimeout="100" BufferSize="100" SlidingTimeout="true" OverflowAction="Flush">        
      <property name="ThreadID" layout="${threadid}" bsonType="Int32" />
      <property name="ThreadName" layout="${threadname}" />
      <property name="ProcessID" layout="${processid}" bsonType="Int32"  />
      <property name="ProcessName" layout="${processname:fullName=true}" />
      <property name="UserName" layout="${windows-identity}" />
    </target>
```

#### Default Output JSON

```JSON
{
    "_id":{"$oid":"58aa0e644a8392ac98bb4812"},
    "Date":{"$date":"2017-02-19T21:30:12.4760000Z"},
    "Level":"Error",
    "Logger":"NLog.Targets.LiteDB.Specs.IntegrationTests.IntegrationTests",
    "Message":"Test Log Message",
    "Exception":
    {
      "Message":"Test Exception",
      "BaseMessage":"Inner Exception",
      "Text":"System.Exception: Test Exception ---> System.Exception: Inner Exception\r\n   --- End of inner exception stack trace ---",
      "Type":"System.Exception",
      "Source":null
    },
      "Properties":
      {
        "ThreadID":"10",
        "ProcessID":"44184",
        "ProcessName":"-Information about process here ",
        "UserName":"MachineName\\user"
      }
    }
}
```
