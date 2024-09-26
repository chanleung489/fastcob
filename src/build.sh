echo building
stat -c '%y' ~/Documents/rw/steamdir/RainWorld_Data/StreamingAssets/mods/znery.fastcob/plugins/Fastcob.dll
dotnet build -c Release
cp bin/Release/net48/Fastcob.dll ~/Documents/rw/steamdir/RainWorld_Data/StreamingAssets/mods/znery.fastcob/plugins/
stat -c '%y' ~/Documents/rw/steamdir/RainWorld_Data/StreamingAssets/mods/znery.fastcob/plugins/Fastcob.dll
