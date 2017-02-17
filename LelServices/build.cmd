@echo off
for /D %%d in (src\*) do (
    cd %%d  
    cd  
    dotnet publish -c Release -o out
    cd ..\..
)
exit /b