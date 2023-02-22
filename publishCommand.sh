dotnet publish -c Release -r ubuntu.16.10-x64 --self-contained true -p:PublishSingleFile=True
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=True