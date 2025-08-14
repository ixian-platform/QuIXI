#!/bin/bash

GPIO_PIN=4
pinctrl set $GPIO_PIN op pu dl

# Subscribe to the topic
mosquitto_sub -t "Chat/#" | while read -r message
do
    echo "Received: $message"
    command=$(echo "$message" | jq -r '.data.data // empty' | tr '[:upper:]' '[:lower:]')
    sender=$(echo "$message" | jq -r '.sender.base58Address // empty')
    if [ "$command" = "on" ]; then
        echo "Turning GPIO $GPIO_PIN ON"
        pinctrl set $GPIO_PIN op pu dh
    elif [ "$command" = "off" ]; then
        echo "Turning GPIO $GPIO_PIN OFF"
	pinctrl set $GPIO_PIN op pu dl
    elif [ "$command" = "temp" ]; then
        temp=$(vcgencmd measure_temp)
        curl --get --data-urlencode "channel=0" --data-urlencode "message=$temp" --data-urlencode "address=$sender" "localhost:8001/sendChatMessage"
    elif [ "$command" = "help" ]; then
        curl --get --data-urlencode "channel=0" --data-urlencode "message=Use on, off or temp." --data-urlencode "address=$sender" "localhost:8001/sendChatMessage"
    else
        curl --get --data-urlencode "channel=0" --data-urlencode "message=Unknown command, use help for more info." --data-urlencode "address=$sender" "localhost:8001/sendChatMessage"
    fi
done
