echo building
stat -c '%y' ~/Documents/rw/steamdir/RainWorld_Data/StreamingAssets/mods/znery.fastcob/plugins/Fastcob.dll
dotnet build -c Debug
cp bin/Debug/net48/* ~/Documents/rw/steamdir/RainWorld_Data/StreamingAssets/mods/znery.fastcob/plugins/
stat -c '%y' ~/Documents/rw/steamdir/RainWorld_Data/StreamingAssets/mods/znery.fastcob/plugins/Fastcob.dll
