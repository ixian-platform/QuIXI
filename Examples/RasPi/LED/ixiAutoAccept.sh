#!/usr/bin/env bash

# Subscribe to the topic
mosquitto_sub -t "RequestAdd2/#" | while read -r message
do
    echo "Received: $message"
    sender=$(echo "$message" | jq -r '.sender.base58Address // empty')
    curl --get --data-urlencode "address=$sender" "localhost:8001/acceptContact"
done
