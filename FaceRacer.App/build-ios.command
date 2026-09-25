#!/bin/bash

dotnet build ./FaceRacerLive/FaceRacerLive.csproj -f net10.0-ios -p:RuntimeIdentifier=ios-arm64
