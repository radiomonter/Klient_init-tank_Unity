@echo off
:: Указываем путь к Unity
set "UNITY_PATH=C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"
:: Указываем путь к проекту (текущая папка скрипта)
set "PROJECT_PATH=%~dp0"
:: Убираем замыкающий слэш, так как Unity его иногда не любит
if "%PROJECT_PATH:~-1%"=="\" set "PROJECT_PATH=%PROJECT_PATH:~0,-1%"

echo Launching Unity 2022.3.62f3...
echo Project: %PROJECT_PATH%

:: Запускаем Unity с явным указанием пути к проекту
start "" "%UNITY_PATH%" -projectPath "%PROJECT_PATH%"
