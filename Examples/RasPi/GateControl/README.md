# Camera & Gate Control (Raspberry Pi)

This example shows how to turn a Raspberry Pi into a **camera + gate control device** connected to the
[Ixian Platform](https://www.ixian.io) via **QuIXI** and **Spixi Mini Apps**.

The device streams camera images, and accepts gate toggle commands in real time.

---

## 🛠 Requirements

* Raspberry Pi 2+ with **Raspberry Pi OS Lite**
* Internet connection
* Camera accessible at `/root/cur.jpg`
* Gate control connected to **GPIO pin 11**
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
````

---

## 📂 Project Structure

```
/GateControl
 ├── start.sh                 # Launches QuIXI and helper scripts
 ├── ixiAppProtocolHandler.sh # Handles App Protocol toggle/ping commands
 ├── ixiMessageHandler.sh     # Handles chat commands (temp/wifi/contacts/help)
 ├── sendImages.sh            # Sends camera images to Spixi Mini Apps
 ├── helpers.sh               # Helper functions for messaging & system control
 ├── ixian.cfg                # Example QuIXI configuration file
 ├── quixi.service            # systemd service file for auto-start
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
chmod +x start.sh ixiAppProtocolHandler.sh ixiMessageHandler.sh sendImages.sh helpers.sh
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

Send commands via Ixian chat (`Chat/#`) or through App Protocol messages (`AppProtocolData/#`):

| Command / Action        | Description                                    |
| ----------------------- | ---------------------------------------------- |
| `toggle` (App Protocol) | Opens/closes the gate connected to GPIO 11     |
| `ping` (App Protocol)   | Registers sender to receive camera images      |
| `temp`                  | Replies with Raspberry Pi CPU temperature      |
| `wifi`                  | Manage Wi-Fi connections (add/remove/list)     |
| `contacts`              | Manage Ixian contacts (accept/add/remove/list) |
| `help`                  | Show available commands                        |

**App Protocol ID:** `com.ixilabs.gatecontrol`

Camera images are sent periodically to registered Spixi Mini App addresses.

---

## 🔌 How It Works

1. `start.sh` launches QuIXI and all helpers.
2. `ixiAppProtocolHandler.sh` listens for `toggle` and `ping` commands via App Protocol.
3. `sendImages.sh` streams camera images to registered addresses using the TTL mechanism.
4. `ixiMessageHandler.sh` listens on `Chat/#`, parses commands, and executes helper functions (`temp`, `wifi`, `contacts`).

This demonstrates how **IoT devices** can integrate into Ixian using **App Protocols and MQTT**, enabling real-time interaction
and streaming with Spixi Mini Apps.

---

## 📲 Spixi Mini App

To use the camera + gate control interface, install the Example Gate Control Mini App in Spixi:

https://resources.ixian.io/gatecontrol.spixi

---

## 📜 License

This example is part of [QuIXI](https://github.com/ixian-platform/QuIXI) and licensed under the [MIT License](../../../LICENSE).
