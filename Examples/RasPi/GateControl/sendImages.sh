#!/usr/bin/env bash

PROTOCOL_ID="com.ixilabs.gatecontrol"
CAMERA_IMAGE="/root/cur.jpg"
ADDRESS_FILE="/root/addresses.txt"
LOCK_FILE="/root/addresses.lock"
INTERVAL=0.1   # 100ms
TTL=10         # seconds before eviction
declare -A addr_time  # address -> timestamp

touch "$ADDRESS_FILE"

# Load addresses and timestamps from file
load_addresses() {
    declare -g -A addr_time
    addr_time=()
    while IFS=" " read -r address ts; do
        # Skip empty lines or invalid data
        [[ -z "$address" || -z "$ts" ]] && continue
        addr_time["$address"]=$ts
    done < "$ADDRESS_FILE"
}

# Save current address list with timestamps
save_addresses() {
    : > "${ADDRESS_FILE}.tmp"
    for address in "${!addr_time[@]}"; do
        [[ -z "$address" ]] && continue
        echo "$address ${addr_time[$address]}" >> "${ADDRESS_FILE}.tmp"
    done
    mv "${ADDRESS_FILE}.tmp" "$ADDRESS_FILE"
}

# Add or update address with current timestamp
add_address() {
    local now=$(date +%s)
    (
        flock -x 200
        load_addresses
        for address in "$@"; do
            [[ -z "$address" ]] && continue
            addr_time["$address"]=$now
        done
        save_addresses
    ) 200>"$LOCK_FILE"
}

send_image() {
    local address=$1
    local img_file=$2

    local img_base64=$(base64 -w 0 "$img_file")

    local data=$(printf '{\\"imageBase64\\":\\"%s\\"}' "$img_base64")

    printf '{"method":"sendAppData","params":{"address":"%s","protocolId":"%s","data":"%s"}}\n' \
        "$address" "$PROTOCOL_ID" "$data" |
        curl -sS -o /dev/null -X POST "http://localhost:8001/sendAppData" \
            -H "Content-Type: application/json" \
            --data-binary @-
}

# Main loop: send images & evict expired addresses
start_loop() {
    echo "Starting main loop..."
    while true; do
        local now=$(date +%s)
        (
            flock -x 200
            load_addresses

            for address in "${!addr_time[@]}"; do
                [[ -z "$address" ]] && continue
                if (( now - addr_time[$address] > TTL )); then
                    unset "addr_time[$address]"
                else
                    send_image "$address" "${CAMERA_IMAGE}" &
                fi
            done

            save_addresses
        ) 200>"$LOCK_FILE"

        sleep "$INTERVAL"
    done
}

case "$1" in
    add)
        shift
        add_address "$@"
        ;;
    start)
        start_loop
        ;;
    *)
        echo "Usage: $0 {add <address1> [address2] ...|start}"
        exit 1
        ;;
esac
