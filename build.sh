#!/bin/bash
dotnet publish -c Release -r linux-x64 -o . --self-contained false /p:PublishSingleFile=true /p:DebugType=None /p:UseSharedCompilation=false /nodeReuse:false
