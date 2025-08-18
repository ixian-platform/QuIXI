# QuIXI

**QuIXI** is the quick integration gateway for the [Ixian Platform](https://www.ixian.io).

Its name is a play on words:

* **Quick + IXI** - fast integration into Ixian
* **Qu (Quantum)** - future-ready with post-quantum cryptography
* **qu (Queue)** - message queue support (MQTT / RabbitMQ)

QuIXI lets you connect **anything** - devices, applications, or services - into Ixian's **secure, decentralized, and
presence-based network**.

It provides:

* 📨 **Message Queue Bridges** - Subscribe to Ixian P2P streams via **MQTT** or **RabbitMQ**
* 🌐 **REST API** - Send messages, app data, or service commands into Ixian
* 🔒 **Encryption by Default** - All communication is protected by Ixian Core's cryptography
* 📦 **Lightweight & Portable** - Runs anywhere .NET 8 is supported, from servers to Raspberry Pi

---

## 🚀 Quick Start

### 1. Install .NET 8

QuIXI requires [.NET 8 SDK & Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

### 2. Clone & Build

```bash
git clone https://github.com/ixian-platform/QuIXI.git
cd QuIXI
dotnet build
```

### 3. Configure

Create a file 'ixian.cfg' in the working directory. Example:

```ini
apiPort = 8001
mqDriver = mqtt
mqHost = localhost
mqPort = 1883
```

### 4. Run

```bash
dotnet run --project QuIXI
```

### 5. Try the REST API

Add a contact:

```bash
curl --get \
  --data-urlencode "address=RECIPIENT_IXI_ADDRESS" \
  "http://localhost:8001/addContact"
```

Send a chat message:

```bash
curl --get \
  --data-urlencode "channel=0" \
  --data-urlencode "message=Hello Ixian!" \
  --data-urlencode "address=RECIPIENT_IXI_ADDRESS" \
  "http://localhost:8001/sendChatMessage"
```

---

## ⚙️ Configuration

QuIXI uses a simple `parameterName = parameterValue` format.
Each option is on its own line in `ixian.cfg` file.

### General

* `apiPort` - HTTP/API port (default 8001)
* `apiAllowIp` - Allow API connections from specific IPs (can repeat)
* `apiBind` - Bind to specific address (can repeat)
* `testnetApiPort` - API port in testnet mode
* `addApiUser` - Add 'user:password' credentials (can repeat)
* `externalIp` - External IP address
* `addPeer` - Specify Ixian seed node (can repeat)
* `addTestnetPeer` - Seed node in testnet mode (can repeat)

### Logging

* `maxLogSize` - Max log file size (MB)
* `maxLogCount` - Max number of rotated logs
* `logVerbosity` - Logging level

### Messaging

* `mqDriver` - `mqtt` or `rabbitmq`
* `mqHost` - Queue host
* `mqPort` - Queue port
* `streamCapabilities` - Supported stream types (Incoming, Outgoing, IPN, Apps, AppProtocols)

### Notifications

* `walletNotify` - Execute command when wallet changes

---

## 🌐 REST API Reference

All APIs are `GET` endpoints.

### Contacts

* `/contacts` - List current contacts
* `/addContact?address=` - Request contact with given Ixian address
* `/acceptContact?address=` - Accept contact request
* `/removeContact?address=` - Remove a contact

### Messaging

* `/sendChatMessage?address=&message=&channel=`

  * `address` - Ixian cryptographic address of recipient
  * `message` - Text to send
  * `channel` - Chat channel (usually `0`)

* `/sendSpixiMessage?address=&type=&data=&channel=`

  * `address` - Recipient address
  * `type` - Message type (Spixi-specific)
  * `data` - Payload
  * `channel` - Channel ID

* `/sendAppData?address=&appId=&data=`

  * Send app-specific data identified by `appId`

* `/sendAppData?address=&protocolId=&data=`

  * Send protocol-specific data identified by `protocolId`

* `/getLastMessages?address=&count=&channel=`

  * Fetch recent messages with a given contact
  * `address` - Contact address
  * `count` - Number of messages to retrieve
  * `channel` - Channel ID

---

## 📡 Message Queue Topics

QuIXI publishes Ixian events and messages into **MQTT** or **RabbitMQ topics**.
Applications can subscribe to these topics to react to events in real time (IoT triggers, service automation, analytics, etc.).

### Core Chat & Messaging

* `Chat` - Standard chat messages
* `MsgTyping` - Typing indicator from a contact
* `MsgReceived` - Confirmation a message was received
* `MsgRead` - Confirmation a message was read
* `MsgDelete` - Message was deleted
* `MsgReaction` - Reaction to a message (emoji, like, etc.)

### Contacts & Presence

* `RequestAdd2` - Incoming contact request
* `AcceptAdd2` - Contact request accepted
* `AcceptAddBot` - Bot contact accepted
* `BotAction` - Bot-initiated action
* `LeaveConfirmed` - Bot confirmed contact has left
* `Nick` - Nickname update

### File Transfers

* `FileHeader` - Metadata for an incoming file
* `AcceptFile` - File transfer accepted
* `RequestFileData` - Request for file chunks
* `FileData` - A piece of file content
* `FileFullyReceived` - Entire file transfer complete
* `Avatar` - Profile picture / avatar update

### Funds & Transactions

* `RequestFunds` - Contact is requesting funds
* `RequestFundsResponse` - Response to a funds request
* `SentFunds` - Outgoing funds sent

### Applications & Protocols

* `AppData` - Spixi Mini App Data
* `AppProtocolData` - Spixi Mini App Protocol Data
* `AppRequest` - App session request
* `AppRequestAccept` - App session accepted
* `AppRequestReject` - App session rejected
* `AppEndSession` - App session terminated
* `AppRequestError` - App session error
* `GetAppProtocols` - Request for supported app protocols
* `AppProtocols` - List of supported app protocols

---

## 💡 Example: Decentralized LED on Raspberry Pi 

The repo includes `/Examples/RasPi/LED`, which contains two bash scripts that turn a Raspberry Pi into a
**decentralized LED device**.

It:

1. Automatically accepts any user that adds it.
2. Subscribes to MQTT messages from Ixian.
3. Accepts commands ('on', 'off', 'temp', 'help').
4. Controls GPIO pins or replies via Ixian chat messages.

Example command flow:

* Send **"on"** → LED turns on.
* Send **"temp"** → Device replies with its CPU temperature.

This demonstrates how **any IoT device** can securely integrate into Ixian via QuIXI.

---

## 🌱 Development Branches

* **master** - Stable, production-ready releases
* **development** - Active development, may contain unfinished features

For reproducible builds, always use the latest **release tag** on `master`.

---

## 🤝 Contributing

We welcome contributions and new integration examples.

1. Fork this repo
2. Create a feature branch ('feature/my-change')
3. Commit with clear messages
4. Open a Pull Request

---

## 🌍 Community & Links

* **Website**: [www.ixian.io](https://www.ixian.io)
* **Docs**: [docs.ixian.io](https://docs.ixian.io)
* **Discord**: [discord.gg/pdJNVhv](https://discord.gg/pdJNVhv)
* **Telegram**: [t.me/ixian\_official\_ENG](https://t.me/ixian_official_ENG)
* **Bitcointalk**: [Forum Thread](https://bitcointalk.org/index.php?topic=4631942.0)
* **GitHub**: [ixian-platform](https://www.github.com/ixian-platform)

---

## 📜 License

Licensed under the [MIT License](LICENSE).
