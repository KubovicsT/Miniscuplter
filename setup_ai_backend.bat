@echo off
setlocal
cd /d "%~dp0ai_backend"
where python >nul 2>nul
if errorlevel 1 (
  echo Python 3 was not found on PATH.
  echo Install Python 3.11 or newer, then run this file again.
  pause
  exit /b 1
)
if not exist .venv (
  python -m venv .venv
  if errorlevel 1 exit /b 1
)
call .venv\Scripts\activate.bat
python -m pip install --upgrade pip
pip install -r requirements.txt
if errorlevel 1 exit /b 1
echo.
echo AI backend environment is ready.
echo Configure MINISCULPTER_SD_URL and MINISCULPTER_3D_COMMAND if you want generation providers.
pause
