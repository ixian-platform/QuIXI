#!/usr/bin/env bash

. helpers.sh

LMSTUDIO_URL="http://localhost:1234/v1/chat/completions"
LMSTUDIO_MODEL="lmstudio -community/Meta-Llama-3-8B-Instruct-GGUF"

lmstudio_request() {
    local message="$1"

    local json="{\"model\":\"$LMSTUDIO_MODEL\",\"messages\":[{\"role\":\"system\",\"content\":\"Be nice and polite.\"},{\"role\":\"user\",\"content\":\"$message\"}],\"temperature\":0.7,\"max_tokens\":-1,\"stream\":false}"

    # Capture full response
    local response
    response=$(printf '%s\n' "$json" |
        curl -sS -X POST "$LMSTUDIO_URL" \
            -H "Content-Type: application/json" \
            --data-binary @-)

    # Check for transport errors
    if [ $? -ne 0 ]; then
        echo "$response"
        return 1
    fi

    echo "$response"
    return 0
}

# Listen for MQTT messages
mosquitto_sub -t "Chat/#" | while read -r message; do
    # Parse JSON message
    data=$(echo "$message" | jq -rc '[.sender.base58Address,.data.data] | @tsv')
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
            if response=$(lmstudio_request "$args"); then
                content=$(jq -rc '.choices[0].message.content // empty' <<<"$response")
                send_message "$sender" "$content"
            else
                send_message "$sender" "Couldn't process request."
            fi
            ;;
    esac
done
