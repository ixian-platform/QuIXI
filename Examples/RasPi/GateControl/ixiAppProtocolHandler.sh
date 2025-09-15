#!/usr/bin/env bash

GPIO_PIN=11

mosquitto_sub -t "AppProtocolData/#" | while read -r message; do
    read -r sender cmd <<< "$(
        jq -rc '[.sender.base58Address, (.data.data.data | @base64d | fromjson | .action // empty)] | @tsv' <<< "$message"
    )"

    # Normalize to lowercase
    cmd=$(tr '[:upper:]' '[:lower:]' <<< "$cmd")

    case "$cmd" in
        toggle)
            pinctrl "$GPIO_PIN" op dh
            sleep 2
            pinctrl "$GPIO_PIN" op dl
            ;;
        ping)
            ./sendImages.sh add "$sender" &
            ;;
    esac
done
