# LM Studio Integration (Raspberry Pi)

This example shows how to create a **decentralized AI chatbot** using QuIXI and **LM Studio** connected to the
[Ixian Platform](https://www.ixian.io) via **QuIXI**.

The device receives Ixian chat messages, forwards them to a local LLM, and replies with AI-generated responses while still
supporting basic system commands.

---

## 🛠 Requirements

* Raspberry Pi 2+ or Linux host
* Internet connection
* LM Studio running locally (default: `http://localhost:1234/v1/chat/completions`)
* .NET 8
* Mosquitto MQTT

---

## ⚡ Installation

```bash
sudo apt update
sudo apt install -y curl git jq mosquitto mosquitto-clients

# Install .NET 8
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --version latest --verbose
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >> ~/.bashrc
source ~/.bashrc

# Clone and build QuIXI + Ixian Core
git clone https://github.com/ixian-platform/Ixian-Core.git
git clone https://github.com/ixian-platform/QuIXI.git
dotnet build -c Release

```

---

## 📂 Project Structure

```
/LMStudio
 ├── start.sh              # Launches QuIXI and message handler
 ├── ixiMessageHandler.sh  # Handles chat commands & forwards messages to LM Studio
 ├── helpers.sh            # Helper functions for messaging & system control
 ├── ixian.cfg             # Example QuIXI configuration file
 ├── quixi.service         # systemd service file for auto-start
```

> **Note:** `quixi.service` assumes `start.sh` and all helper scripts reside in `/root/`. Adjust paths if you move them elsewhere.

---

## ▶️ Running the Example

### Copy example ixian.cfg file to Release directory and edit if required

```bash
cp ixian.cfg QuIXI/QuIXI/bin/Release/net8.0/
```

### Manual start

```bash
chmod +x start.sh ixiMessageHandler.sh helpers.sh
./start.sh
```

### Auto-start on boot (systemd)

Copy the provided service file:

```bash
sudo cp quixi.service /etc/systemd/system/
sudo systemctl enable quixi
sudo systemctl start quixi
```

Check status:

```bash
systemctl status quixi
```

---

## 🔑 Add Your Device as a Contact

Before you can send commands and messages to AI, add your Ixian address as a contact to QuIXI. Replace `DEVICE_ADDRESS` with the
address of your Spixi client:

```bash
curl --get \
  --data-urlencode "address=DEVICE_ADDRESS" \
  "http://localhost:8001/addContact"
```

---

## 💡 Supported Commands

Send commands via Ixian chat (`Chat/#` topic):

| Command    | Action                                         |
| ---------- | ---------------------------------------------- |
| `temp`     | Replies with Raspberry Pi CPU temperature      |
| `wifi`     | Manage Wi-Fi connections (add/remove/list)     |
| `contacts` | Manage Ixian contacts (accept/add/remove/list) |
| `help`     | Show available commands                        |
| Any other  | Forwarded to LM Studio for AI-generated reply  |

---

## 🔌 How It Works

1. `start.sh` launches QuIXI and the message handler.
2. `ixiMessageHandler.sh` listens to `Chat/#` and parses incoming messages:

   * Built-in commands (`temp`, `wifi`, `contacts`, `help`) handled locally.
   * All other messages forwarded to LM Studio via HTTP API.
3. AI responses are sent back to the sender via Ixian chat.

This demonstrates how **Ixian + QuIXI** can be used to create a **decentralized AI assistant**.

---

## 📜 License

This example is part of [QuIXI](https://github.com/ixian-platform/QuIXI) and licensed under the [MIT License](../../../LICENSE).
