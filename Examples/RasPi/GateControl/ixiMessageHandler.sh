#!/usr/bin/env bash

. helpers.sh

# Listen for MQTT messages
mosquitto_sub -t "Chat/#" | while read -r message; do
    # Parse JSON message
    data=$(echo "$message" | jq -rc '[.sender,.data.data] | @tsv')
    sender=$(echo "$data" | awk '{print $1}')
    cmd=$(echo "$data" | awk '{print tolower($2)}')
    args="$(echo "$data" | cut -f2-)"

    case "$cmd" in
        temp)
            temp "$sender"
            ;;
        wifi)
            wifi "$sender" "${args[@]}"
            ;;
        contacts)
            contacts "$sender" "${args[@]}"
            ;;
        help)
            send_message "$sender" "Commands: temp, wifi [add/remove/list], contacts [accept/add/remove/list], help"
            ;;
        *)
            send_message "$sender" "Unknown command, use help for more info."
            ;;
    esac
done
