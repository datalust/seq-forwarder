# Seq Forwarder [![Build status](https://ci.appveyor.com/api/projects/status/qdvdn50xqwi43jkm/branch/master?svg=true)](https://ci.appveyor.com/project/seqlogs/seq-forwarder/branch/master) [![Join the chat at https://gitter.im/datalust/seq](https://img.shields.io/gitter/room/datalust/seq.svg)](https://gitter.im/datalust/seq)

[Seq Forwarder](http://blog.getseq.net/help-us-test-seq-forwarder/) was recently announced. **We're in the process 
of moving its source code to this repository, which we expect to complete by the end of June 2016.** Until that
work is complete, things may churn a bit here.

### Building

Visual Studio 2015 is required. While migrating from our internal CI no scripted build is set up yet - just build
the solution in Release mode and grab the resulting binaries.

The solution is currently a Windows-only .NET 4.5.2 application. .NET Core support is intended sometime after its RTM.

### Debugging

`F5` will work, but you will need to either run the `install` command (see below) to create an HTTP namespace
reservation, or run as Administrator (on Windows).

### Deployment

The outputs from _Seq.Forwarder_ and _Seq.Forwarder.Administration_ (if required) projects can be XCOPY-deployed.

### Setup

Run `Seq.Forwarder.Administration.exe` to install the forwarder, or check out the command-line for scripted setup.

### Command-line usage

**List available commands:**

```
seq-forwarder help
```

**Get command help:**

```
seq-forwarder help <command>
```

**Install as a Windows service:**

```
seq-forwarder install
```

**Set destination Seq server details:**

```
seq-forwarder config -k output.serverUrl --value="http://my-seq/"
seq-forwarder config -k output.apiKey --value="1234567890"
```

**Start the Windows service:**

```
seq-forwarder start
```

**Run interactively:**

```
seq-forwarder run
```

**Change the buffer size cap (defaults to 64 MB):**

```
seq-forwarder config -k storage.bufferSizeBytes -v 1073741824  
seq-forwarder restart  
```

**Directly import a JSON log file:**

```
seq-forwarder import -f myapp.json -u https://my-seq -p User=appuser1 -p Email=appuser@example.com
```

### Logging

Seq Forwarder listens on port `15341`. The HTTP ingestion API is identical to
the Seq one, so standard client libraries like _Serilog.Sinks.Seq_ can write to
it directly.

```csharp
Log.Logger = new LoggerConfiguration()  
    .WriteTo.Seq("http://localhost:15341")
    .CreateLogger();

Log.Information("Hello, Seq Forwarder!");  
```
