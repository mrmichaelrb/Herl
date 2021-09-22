del /s /q *.blend1
del /s /q /a:h *.blend1.meta
del /q *.csproj
del /q *.exe
del UnityPlayer.dll
del WinPixEventRuntime.dll
del /q *.sln
rmdir /q /s .vs
rmdir /q /s Debug
rmdir /q /s Herl_Data
rmdir /q /s Logs
rmdir /q /s obj
move Library\LastSceneManagerSetup.txt .
rmdir /q /s Library
mkdir Library
move LastSceneManagerSetup.txt Library
rmdir /q /s Temp
