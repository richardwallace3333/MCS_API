## MCS API README

Welcome to the MCS API for controlling and acquiring status from external IP cameras. This API is built on top of ZeroMQ and allows seamless camera operations through a set of commands that are easy to integrate into your C# applications.

### Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Usage](#usage)
4. [Available Commands](#available-commands)
5. [Example](#example)
6. [License](#license)

### Overview

The MCS API uses the ZeroMQ library to communicate with the central MCS application via a socket connection. It provides functionality to:

- Connect and disconnect from the MCS application
- Start and stop monitoring
- Retrieve images and status information
- Manage camera settings and operations

### Installation

To use the MCS API, ensure that you have the following prerequisites:

- [.NET Framework](https://dotnet.microsoft.com/) (Version should be compatible with your project).
- [ZeroMQ](https://github.com/zeromq/netmq) (NetMQ library for .NET).
- [Newtonsoft.Json](https://www.newtonsoft.com/json) for JSON serialization/deserialization.

You can install the required libraries via NuGet Package Manager:

```shell
Install-Package NetMQ
Install-Package Newtonsoft.Json
```

### Usage

To integrate the MCS API into your C# project, follow these steps:

1. **Initialize the MCSAPI:**

    ```csharp
    using MCSControl;

    MCSAPI mcsApi = new MCSAPI();
    ```

2. **Connect to the MCS Application:**

    ```csharp
    var (success, status) = mcsApi.Connect();
    if (success)
    {
        Console.WriteLine($"Connected successfully: {status}");
    }
    ```

3. **Execute Commands:**

   You can now use the various methods provided by `MCSAPI` to control the cameras and retrieve data.

### Available Commands

- **Connect/Disconnect:**
  - `Connect()`
  - `Disconnect()`

- **Camera Controls:**
  - `Start()`
  - `Stop()`
  - `GetLatestCaptureId()`
  - `GetCaptureImages()`
  - `AddCamera()`
  - `RemoveCamera()`
  
- **Information Retrieval:**
  - `GetStatus()`
  - `GetCameraCount()`
  - `GetParamSettings()`
  - `GetStatusSettings()`
  
- **Settings Management:**
  - `UpdateSettings(Dictionary<string, Dictionary<string, string>> Categories)`
  - `GetRecordingRootDIR()`
  - `setRecordingRootDIR(string recordingDIR)`

### Example

Below is an example of using `MCSAPI` to start capturing images from connected cameras:

```csharp
using System;
using MCSControl;

class Program
{
    static void Main(string[] args)
    {
        MCSAPI mcsApi = new MCSAPI();

        // Attempt to connect
        var (success, status) = mcsApi.Connect();
        if (success)
        {
            Console.WriteLine($"Connected: {status}");

            // Start capturing
            mcsApi.Start();
            Console.WriteLine("Capture started.");

            // Get status
            string status = mcsApi.GetStatus();
            Console.WriteLine($"Status: {status}");

            // Stop capturing
            mcsApi.Stop();
            Console.WriteLine("Capture stopped.");

            // Disconnect
            mcsApi.Disconnect();
            Console.WriteLine("Disconnected.");
        }
        else
        {
            Console.WriteLine("Failed to connect.");
        }
    }
}
```

### License

This project is licensed under the MIT License.

---

Feel free to explore and extend the API based on your requirements. For any issues or enhancements, contributions are welcome!
