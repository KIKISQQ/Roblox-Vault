# Building RobloxVault from Source

## Requirements
- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Windows 10 or later

## Getting the source code

**Option A — Git clone** (requires Git installed)
```
git clone https://github.com/YOUR_USERNAME/RobloxVault.git
```

**Option B — Download ZIP** (no account or tools needed)
1. Go to the repository page on GitHub
2. Click the green **Code** button
3. Click **Download ZIP**
4. Extract the folder somewhere on your PC

## Building

Navigate to the project folder, then run:
```
dotnet run
```

## Producing a standalone exe
```
dotnet publish -c Release -r win-x64 --self-contained false
```

The output will be in `bin/Release/net8.0-windows/win-x64/publish/`. Grab `RobloxVault.exe` from there.

## Notes
- Make sure you have the **.NET 8.0 SDK** installed, not just the runtime. The runtime alone is enough to run the app but not to build it.
- The publish command above produces a framework-dependent build, meaning the target machine still needs the .NET 8.0 Desktop Runtime installed to run it.
- If you want a fully self-contained build that bundles the runtime, use `--self-contained true` instead, the output will be larger but runs on any Windows machine without any prerequisites.