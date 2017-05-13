#!/bin/bash
mkdir -p log
rm log/*
for i in $(seq 1 $1)
do
	echo "Creating server $i"
	dotnet run -p Server --server.urls "http://localhost:500$i" --myAddress "localhost:600$i" &> log/server$i.log &
    sleep 1
done
