@echo off
REM socket.multiple.bat 10 <-- this will start up 10 servers

cls
FOR /L %%A IN (1,1,%1) DO (
    start cmd.exe /k dotnet run --project Wbskt.Socket.Service.csproj --urls "http://0.0.0.0:505%%A"
)
