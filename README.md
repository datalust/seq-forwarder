# Seq Forwarder [![Build status](https://ci.appveyor.com/api/projects/status/qdvdn50xqwi43jkm?svg=true)](https://ci.appveyor.com/project/datalust/seq-forwarder) [![Join the chat at https://gitter.im/datalust/seq](https://img.shields.io/gitter/room/datalust/seq.svg)](https://gitter.im/datalust/seq) [![Download](https://img.shields.io/github/release/datalust/seq-forwarder.svg)](https://github.com/datalust/seq-forwarder/releases)

Seq Forwarder is a client-side tool for sending log data to Seq.

### HTTP forwarding

Seq Forwarder can run as a Windows service on client machines. It receives events over its local HTTP
API and persists these to its own internal storage until the remote Seq server can be reached.

![Seq Forwarder](https://raw.githubusercontent.com/nblumhardt/images/master/seq-forwarder-schematic.png)

Seq Forwarder listens on port `15341` by default. The HTTP ingestion API is identical to
the Seq one, so standard client libraries like _Serilog.Sinks.Seq_ can write to
it directly.

```csharp
Log.Logger = new LoggerConfiguration()  
    .WriteTo.Seq("http://localhost:15341")
    .CreateLogger();

Log.Information("Hello, Seq Forwarder!");  
```

### Building

Seq Forwarder is a .NET Core application that can be built using the .NET Core SDK on Windows, macOS, and Linux.

### Debugging

`F5` will work, but on Windows you will need to either run the `install` command (see below) to create an HTTP namespace
reservation, or run as Administrator.

### Deployment

Unzip the artifact bundle for your platform.

### Setup

To set up Seq Forwarder as a Windows service:

```
seqfwd install
seqfwd config -k output.serverUrl --value="http://seq.example.com/"
seqfwd config -k output.apiKey --value="1a2b3c4d5e6f"
seqfwd config -k storage.bufferSizeBytes -v 1073741824
seqfwd start
```

The default buffer size cap is 64 MB.

### Troubleshooting

By default the "forwarder" logs will be stored under `%PROGRAMDATA%\Seq\Logs`.  If the destination Seq server is not available, an exception will be stored in these log files.

If you need to inspect the current configuration, it can be found at: `%PROGRAMDATA%\Seq\Forwarder\SeqForwarder.json`

### Command-line reference

TODO

