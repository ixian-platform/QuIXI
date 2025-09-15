# QuIXI Examples

This directory contains **hands-on examples** demonstrating how to integrate various devices and applications into the [Ixian Platform](https://www.ixian.io) via **QuIXI**.

Each example includes:

* Installation and setup instructions
* Scripts for command handling
* Usage instructions with supported commands
* Optionally, integration with IoT devices, App Protocols, or AI services

---

## 📚 Available Examples (Raspberry Pi)

### 1. Decentralized LED

Turn a Raspberry Pi into a **decentralized IoT LED device**:

* Automatically accepts new Ixian contacts
* Responds to chat commands and MQTT messages (`on`, `off`, `temp`, `wifi`, `contacts`, `help`)
* Controls an LED connected to GPIO pin 4

[View README](./RasPi/LED/README.md)

---

### 2. Camera & Gate Control

Integrate a Raspberry Pi with a **security camera and gate system** using **App Protocols**:

* Streams camera images to registered online Spixi Mini App addresses
* Responds to gate toggle commands (`toggle`)
* Handles chat commands (`temp`, `wifi`, `contacts`, `help`)

[View README](./RasPi/GateControl/README.md)

---

### 3. LM Studio Integration

Create a **decentralized AI chatbot** using **LM Studio**:

* Forwards chat messages to a local LLM for AI responses
* Supports basic system commands (`temp`, `wifi`, `contacts`, `help`)
* Replies directly to Ixian chat messages

[View README](./RasPi/LMStudio/README.md)

---

## 🔑 Add Your Ixian Address as a Contact

For all examples, you must **add your Ixian address as a contact** before sending commands or receiving data.
Replace `DEVICE_ADDRESS` with the address of your Spixi client:

```bash
curl --get \
  --data-urlencode "address=DEVICE_ADDRESS" \
  "http://localhost:8001/addContact"
```

---

## 🛠 Getting Started

1. Install [Raspberry Pi OS Lite](https://www.raspberrypi.org/software/operating-systems/) on a Raspberry Pi 2+ (if using
hardware examples).
2. Follow the **Installation** and **Running the Example** sections in the individual example README for your chosen project.
3. Test commands via Ixian chat or App Protocol messages as described in each README.

---

## 📜 License

All examples are part of [QuIXI](https://github.com/ixian-platform/QuIXI) and licensed under the [MIT License](../LICENSE).
