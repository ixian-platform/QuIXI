#!/bin/bash

export PATH=$PATH:/root/.dotnet

cd /root
/usr/bin/bash ixiAutoAccept.sh &
/usr/bin/bash ixiMessageHandler.sh &

cd /root/QuIXI/QuIXI/bin/Release/net8.0/
/root/.dotnet/dotnet QuIXI.dll --walletPassword YOUR_WALLET_PASSWORD
