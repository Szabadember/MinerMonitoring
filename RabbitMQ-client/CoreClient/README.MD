.NET Core miner monitoring
- Replace data in curly brackets inside settings.json
- compile for windows using: "dotnet publish -c release -r win7-x64"
- compile for centos linux using: "dotnet publish -c release -r centos.7-x64"
- see other platforma here: https://github.com/dotnet/docs/blob/master/docs/core/rid-catalog.md
- copy publish folder to miner machine
- schedule run of CoreClient using task scheduler (windows) or systemd/initd/cron (linux)