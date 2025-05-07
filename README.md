# 🔌 LiteMq

[![.NET](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/)
[![LiteDB](https://img.shields.io/badge/LiteDB-embedded-green)](https://www.litedb.org/)
[![License: MIT](https://img.shields.io/badge/license-MIT-lightgrey.svg)](https://opensource.org/licenses/MIT)

> A lightweight, persistent TCP message queue with pub/sub, clustering, and deduplication — powered by LiteDB.

---

## ✨ Features

- 🧵 Persistent queueing with [LiteDB](https://www.litedb.org/)
- 🔌 TCP-based publish/subscribe
- 🌐 Clustered brokers with message forwarding
- 🛡 Deduplication via message hashing
- 🧪 Integration-tested with xUnit
- 🧰 Minimal dependencies

---

## 📦 Requirements

- .NET 9.0+
- NuGet: `LiteDB`
- (For tests) xUnit

---

## 🚀 Getting Started

### 🔧 Build

```bash
dotnet restore
dotnet build
```

### ▶️ Run a Broker

```bash
var server = BrokerServerBuilder
    .Create()
    .Build();
    
server.Start();
```

(Default: ip `127.0.0.1` port `6000`, maxRetryForPeersCommunication `0`, maxDelayForPeersCommunicationInSeconds `100` DB file `LiteMq_127.0.0.1_6000.db`)

---

## 📡 TCP Protocol

Each message ends in `\n`. Supported commands:

| Command      | Format                        |
|--------------|-------------------------------|
| Subscribe    | `sub|topic`                   |
| Publish      | `pub|topic|payload`           |

Example:  
```text
pub|news|hello world
sub|news
```

---

## 🌐 Clustering

Define peer brokers as `IPEndPoint`s. The queue auto-forwards messages to peers while avoiding loops (via deduplication hash):

```csharp    
var peers = new List<IPEndPoint>
{
    new("127.0.0.1", 5001),
    new("127.0.0.1", 5002)
};

var server = BrokerServerBuilder
    .Create()
    .WithIp("127.0.0.1")
    .WithPort(5000)
    .WithPeers(peers)
    .WithMaxRetryForPeersCommunication(3)
    .WithMaxDelayForPeersCommunication(200)
    .Build();

server.Start();
```

---

## 🧪 Tests

Integration tests simulate full broker networks using real TCP clients.

### Run tests

```bash
dotnet test
```

### Example test structure

- **SingleBrokerPublishTest**: Validates local pub/sub
- **MultiBrokerClusterTest**: Validates cluster forwarding
- **BrokerTestHelper**: Manages test brokers dynamically

---

## 📁 Project Structure

```
LiteMq/
├── LiteMq
├───────── Builders
├────────────────── BrokerServerBuilder.cs # Logic to build an instance
├───────── Entities
├────────────────── Message.cs
├────────────────── Subscription.cs
├───────── Managers
├────────────────── SubscriptionManager.cs # Logic to manage subscriptions
├────────────────── SubscriptionManager.cs # Logic to manage peers
├───────── BrokerServer.cs  # Core logic (broker + queue)
├───────── MessageQueue.cs
├── LiteMq.Tests
├───────── BrokerTestHelper.cs  # Utilities for spinning up test brokers
├───────── BrokerTests.cs  # Basic pub/sub tests
├───────── MultiBrokerClusterTest.cs  # Multi-broker integration test
├── README.md
```

---

## 🧰 Developer Scripts (optional)

You can add these scripts to your repo:

**build.sh**
```bash
#!/bin/bash
dotnet restore
dotnet build
```

**run.sh**
```bash
#!/bin/bash
dotnet run --project LiteMq
```

**test.sh**
```bash
#!/bin/bash
dotnet test
```

Run with:
```bash
chmod +x build.sh run.sh test.sh
./build.sh && ./test.sh
```

---

## 📜 License

This project is licensed under the MIT License. See `LICENSE` for details.
