# Source from Bash before using the SDK in the Linux cloud environment.
export DOTNET_ROOT=/workspace/toolchains/dotnet
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_HOME=/workspace/toolchains/cli
export NUGET_PACKAGES=/workspace/toolchains/nuget
export XDG_RUNTIME_DIR=/workspace/toolchains/runtime
export XDG_DATA_HOME=/workspace/toolchains/xdg
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
mkdir -p "$XDG_RUNTIME_DIR" "$XDG_DATA_HOME" "$DOTNET_CLI_HOME"
chmod 700 "$XDG_RUNTIME_DIR"
