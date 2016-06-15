# Seq Forwarder

[Seq Forwarder](http://blog.getseq.net/help-us-test-seq-forwarder/) was recently announced. **We're in the process 
of moving its source code to this repository, which we expect to complete by the end of June 2016.** Until that
work is complete, things may churn a bit here.

### Building

Visual Studio 2015 is required. While migrating from our internal CI no scripted build is set up yet - just build
the solution in Release mode and grab the resulting binaries.

### Debugging

`F5` will work, but you will need to either run the `install` command (see below) to create an HTTP namespace
reservation, or run as Administrator (on Windows).

### Deployment

The outputs from _Seq.Forwarder_ and _Seq.Forwarder.Administration_ (if required) projects can be XCOPY-deployed.

### Setup

Run `Seq.Forwarder.Administration.exe` to install the forwarder, or check out the command-line for scripted setup.

### Command-line usage

**List available commands**

```
seq-forwarder help
```

**Get command help**

```
seq-forwarder help <command>
```

**Install as a Windows service**

```
seq-forwarder install
```

**Set destination Seq server details**

```
seq-forwarder config -k output.serverUrl --value="http://my-seq/"
seq-forwarder config -k output.apiKey --value="1234567890"
```

**Start the Windows service**

```
seq-forwarder start
```

**Run interactively**

```
seq-forwarder run
```

**Change the buffer size cap (defaults to 64 MB)**

```
seq-forwarder config -k storage.bufferSizeBytes -v 1073741824  
seq-forwarder restart  
```

### Linux, Mac

.NET Core support is intended sometime after its RTM.
