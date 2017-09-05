# Seq Forwarder [![Build status](https://ci.appveyor.com/api/projects/status/qdvdn50xqwi43jkm?svg=true)](https://ci.appveyor.com/project/datalust/seq-forwarder) [![Join the chat at https://gitter.im/datalust/seq](https://img.shields.io/gitter/room/datalust/seq.svg)](https://gitter.im/datalust/seq) [![Download](https://img.shields.io/github/release/datalust/seq-forwarder.svg)](https://github.com/datalust/seq-forwarder/releases)

[Seq Forwarder](http://blog.getseq.net/help-us-test-seq-forwarder/) is a client-side tool for sending log data to Seq.

### HTTP forwarding

Seq Forwarder can run as a Windows service on client machines. It receives events over a local HTTP
API and persists these to its own internal storage until the remote Seq server can be reached.

![Seq Forwarder](https://raw.githubusercontent.com/nblumhardt/images/master/seq-forwarder-schematic.png)

Seq Forwarder listens on port `15341`. The HTTP ingestion API is identical to
the Seq one, so standard client libraries like _Serilog.Sinks.Seq_ can write to
it directly.

```csharp
Log.Logger = new LoggerConfiguration()  
    .WriteTo.Seq("http://localhost:15341")
    .CreateLogger();

Log.Information("Hello, Seq Forwarder!");  
```

### Importing JSON log files

The `seq-forwarder import` command can be used to import JSON log files directly into Seq. The log file needs to 
be in Serilog's native JSON format (e.g. produced by the [Seq sink](https://github.com/serilog/serilog-sinks-seq) or
Serilog's `JsonFormatter`) with one JSON-encoded event per line.

```
seq-forwarder import -f myapp.json -u https://my-seq -p User=appuser1 -p Email=appuser@example.com
```

The command will print a GUID `ImportId` that will be attached to the imported events in Seq. Additional properties
can be specified on the command-line, like `User=` and `Email=` above, to tag the events.

### Building

Visual Studio 2017 is required. While migrating from our internal CI no scripted build is set up yet - just build
the solution in Release mode and grab the resulting binaries.

The solution is currently a Windows-only .NET 4.5.2 application. .NET Core support is intended sometime after its RTM.

You will need [Wix 3.10](http://wixtoolset.org) to build the setup/MSI.

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

