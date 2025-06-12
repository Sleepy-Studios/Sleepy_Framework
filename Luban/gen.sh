#!/bin/bash

# 设置环境变量
WORKSPACE=.
LUBAN_DLL="$WORKSPACE/Tools/Luban/Luban.dll"
CONF_ROOT="$WORKSPACE/DataTables"

# 执行 Luban 命令
dotnet "$LUBAN_DLL" \
    -t all \
    -c cs-simple-json \
    -d json \
    -x outputCodeDir=../Assets/Scripts/HotUpdate/Luban \
    -x outputDataDir=../Assets/GameRes/Config/Luban \
    --conf "$CONF_ROOT/luban.conf" \
    -x outputDataDir=output