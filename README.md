# NLog.LiteDb

NLog target for the LiteDB database

[![nlog-litedb MyGet Build Status](https://www.myget.org/BuildSource/Badge/nlog-litedb?identifier=e058fc9f-f85c-4d05-b515-f146a61417a2)](https://www.myget.org/)

##Configuration Syntax

```xml
<extensions>
  <add assembly="NLog.LiteDB"/>
</extensions>
<targets>
    <target name="liteDB" xsi:type="liteDBTarget"
            connectionString="filename=NLog.db"
            collectionName="DefaultLog">        
      <property name="ThreadID" layout="${threadid}" bsonType="Int32" />
      <property name="ThreadName" layout="${threadname}" />
      <property name="ProcessID" layout="${processid}" bsonType="Int32"  />
      <property name="ProcessName" layout="${processname:fullName=true}" />
      <property name="UserName" layout="${windows-identity}" />
    </target>
</targets>
<rules>
  <logger name="*" minlevel="Trace" writeTo="liteDB" />
</rules>
```

##Parameters

###General Options

_name_ - Name of the target.

###Connection Options

_connectionName_ - The name of the connection string to get from the config file.

_connectionString_ - Connection string. When provided, it overrides the values specified in connectionName.

###Collection Options
_collectionName_ - The name of the LiteDB collection to write logs to.  


###Document Options

_includeDefaults_ - Specifies if the default document is created when writing to the collection.  Defaults to true.

_field_ - Specifies a root level document field. There can be multiple fields specified.

_property_ - Specifies a dictionary property on the Properties field. There can be multiple properties specified.

##Examples

###Default Configuration with Extra Properties

####NLog.config target

```xml
    <target name="liteDB" xsi:type="liteDBTarget"
            connectionString="filename=NLog.db"
            collectionName="DefaultLog">        
      <property name="ThreadID" layout="${threadid}" bsonType="Int32" />
      <property name="ThreadName" layout="${threadname}" />
      <property name="ProcessID" layout="${processid}" bsonType="Int32"  />
      <property name="ProcessName" layout="${processname:fullName=true}" />
      <property name="UserName" layout="${windows-identity}" />
    </target>
```

####Default Output JSON

```JSON
{
    "_id":{"$oid":"58aa0e644a8392ac98bb4812"},
    "Date":{"$date":"2017-02-19T21:30:12.4760000Z"},
    "Level":"Error",
    "Logger":"NLog.LiteDB.Specs.IntegrationTests.IntegrationTests",
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
