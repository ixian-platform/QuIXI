#!/usr/bin/env bash

send_message() {
    local addr="$1"
    local msg="$2"
    curl --silent --get \
        --data-urlencode "channel=0" \
        --data-urlencode "message=$msg" \
        --data-urlencode "address=$addr" \
        "http://localhost:8001/sendChatMessage" >/dev/null
}

# escape function for JSON string values
json_escape() {
    local s="$1"
    s=${s//\\/\\\\}   # backslashes
    s=${s//\"/\\\"}   # quotes
    s=${s//$'\n'/\\n} # newlines
    s=${s//$'\r'/\\r} # carriage returns
    s=${s//$'\t'/\\t} # tabs
    printf '%s' "$s"
}

send_request() {
    local cmd="$1"
    shift

    local json_params=""
    local first=1

    for kv in "$@"; do
        key="${kv%%=*}"
        val="${kv#*=}"
        val=$(json_escape "$val")
        if [[ $first -eq 1 ]]; then
            json_params="\"$key\":\"$val\""
            first=0
        else
            json_params="$json_params,\"$key\":\"$val\""
        fi
    done

    local json="{\"method\":\"$cmd\",\"params\":{${json_params}}}"

    # Capture full response
    local response
    response=$(printf '%s\n' "$json" |
        curl -sS -X POST "http://localhost:8001/" \
            -H "Content-Type: application/json" \
            --data-binary @-)

    # Check for transport errors
    if [ $? -ne 0 ]; then
        echo "$response"
        return 1
    fi

    # Check if response has a non-null error field
    if ! echo "$response" | grep -q '"error":null'; then
        echo "$response"
        return 1
    fi

    echo "$response"
    return 0
}


temp() {    
    local addr="$1"
    local t=$(vcgencmd measure_temp | cut -d'=' -f2)
    send_message "$addr" "Temperature: $t"
}

wifi() {
    local addr="$1"
    shift

    local input="$*" 
    eval "set -- $input"    
    shift

    local action="$1"
    shift

    case "$action" in
        add)
            local ssid="$1"
            local pass="$2"
            if [ -z "$ssid" ] || [ -z "$pass" ]; then
                send_message "$addr" "Usage: wifi add SSID PASSWORD"
            else
                sudo nmcli dev wifi connect "$ssid" password "$pass" >/dev/null 2>&1
                if [ $? -eq 0 ]; then
                    send_message "$addr" "Connected to $ssid"
                else
                    send_message "$addr" "Failed to connect to $ssid"
                fi
            fi
            ;;
        remove)
            local ssid="$1"
            if [ -z "$ssid" ]; then
                send_message "$addr" "Usage: wifi remove SSID"
            else
                sudo nmcli connection delete "$ssid" >/dev/null 2>&1
                if [ $? -eq 0 ]; then
                    send_message "$addr" "Removed $ssid"
                else
                    send_message "$addr" "Failed to remove $ssid"
                fi
            fi
            ;;
        list)
            sudo nmcli dev wifi rescan
            # List all visible WiFi networks
            local all_networks
            all_networks=$(nmcli -t -f SSID,ACTIVE dev wifi | awk -F: '{print $1 ($2=="yes"?"*":"")}')
            
            # List registered (saved) networks
            local registered_networks
            registered_networks=$(nmcli -t -f NAME connection show)
            
            send_message "$addr" "Available WiFi networks: $all_networks"
            send_message "$addr" "Registered networks: $registered_networks"
            ;;
        *)
            send_message "$addr" "Usage: wifi [add SSID PASS | remove SSID | list]"
            ;;
    esac
}


contacts() {
    local sender="$1"
    shift

    local input="$*"
    eval "set -- $input"
    shift

    local action="$1"
    shift

    case "$action" in
        accept)
            local contact_address="$1"
            if [ -z "$contact_address" ]; then
                send_message "$sender" "Usage: contacts accept CONTACT_ADDRESS"
            else
                if send_request acceptContact "address=$contact_address" >/dev/null; then
                    send_message "$sender" "Accepted contact $contact_address"
                else
                    send_message "$sender" "Failed to accept contact $contact_address"
                fi
            fi
            ;;
        add)
            local contact_address="$1"
            if [ -z "$contact_address" ]; then
                send_message "$sender" "Usage: contacts add CONTACT_ADDRESS"
            else
                if send_request addContact "address=$contact_address" >/dev/null; then
                    send_message "$sender" "Added contact $contact_address"
                else
                    send_message "$sender" "Failed to add contact $contact_address"
                fi
            fi
            ;;
        remove)
            local contact_address="$1"
            if [ -z "$contact_address" ]; then
                send_message "$sender" "Usage: contacts remove CONTACT_ADDRESS"
            else
                if send_request removeContact "address=$contact_address" >/dev/null; then
                    send_message "$sender" "Removed contact $contact_address"
                else
                    send_message "$sender" "Failed to remove contact $contact_address"
                fi
            fi
            ;;
        list)
            local stored_contacts
            stored_contacts=$(send_request contacts)
            if [ $? -eq 0 ]; then
                send_message "$sender" "Contacts: $stored_contacts"
            else
                send_message "$sender" "Failed to fetch contacts"
            fi
            ;;
        *)
            send_message "$sender" "Usage: contacts [accept ADDRESS | add ADDRESS | remove ADDRESS | list]"
            ;;
    esac
}
