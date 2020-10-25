#!/bin/bash
cd ..
dotnet publish -c Release -r linux-x64 -o /opt/twitchtools --self-contained false /p:PublishSingleFile=true /p:DebugType=None /p:UseSharedCompilation=false /nodeReuse:false
