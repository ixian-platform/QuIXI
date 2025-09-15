# Decentralized LED (Raspberry Pi)

This example shows how to turn a Raspberry Pi into a **decentralized IoT LED device** connected to the
[Ixian Platform](https://www.ixian.io) via **QuIXI**.

The device securely integrates into Ixian, automatically accepts new users, and reacts to chat commands
or MQTT messages in real time.

---

## 🛠 Requirements

* Raspberry Pi 2+ with **Raspberry Pi OS Lite**
* Internet connection
* LED connected to **GPIO pin 4**
* .NET 8
* Mosquitto MQTT

---

## ⚡ Installation

Run the following commands on your Pi:

```bash
# Update and install dependencies
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
````

---

## 📂 Project Structure

```
/LED
 ├── start.sh              # Launches QuIXI and helper scripts
 ├── ixiAutoAccept.sh      # Automatically accepts incoming contacts
 ├── ixiMessageHandler.sh  # Handles chat commands (on/off/temp/help/wifi/contacts)
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
chmod +x start.sh ixiAutoAccept.sh ixiMessageHandler.sh helpers.sh
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

Before you can send commands, add your Ixian address as a contact to QuIXI. Replace `DEVICE_ADDRESS` with the
address of your Spixi client:

```bash
curl --get \
  --data-urlencode "address=DEVICE_ADDRESS" \
  "http://localhost:8001/addContact"
```

---

## 💡 Supported Commands

Send commands via Ixian chat or through MQTT messages (`Chat/#` topic).

| Command    | Action                                         |
| ---------- | ---------------------------------------------- |
| `on`       | Turns the LED **on**                           |
| `off`      | Turns the LED **off**                          |
| `temp`     | Replies with Raspberry Pi CPU temperature      |
| `wifi`     | Manage Wi-Fi connections (add/remove/list)     |
| `contacts` | Manage Ixian contacts (accept/add/remove/list) |
| `help`     | Show available commands                        |

Example message flow:

* Send **"on"** → LED turns on
* Send **"temp"** → Device replies with CPU temperature

---

## 🔌 How It Works

1. `start.sh` launches QuIXI and the message handler.
2. `ixiAutoAccept.sh` listens on `RequestAdd2/#` and ensures the device accepts any new contact requests.
3. `ixiMessageHandler.sh` listens on `Chat/#`, parses incoming commands, and:

   * Controls GPIO pin 4 (LED on/off)
   * Executes helper functions (`temp`, `wifi`, `contacts`)
   * Replies via Ixian chat

This demonstrates how **any IoT device** can securely integrate into Ixian's decentralized, encrypted network.

---

## 📜 License

This example is part of [QuIXI](https://github.com/ixian-platform/QuIXI) and licensed under the [MIT License](../../../LICENSE).
