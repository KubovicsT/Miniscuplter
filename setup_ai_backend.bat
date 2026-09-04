@echo off
setlocal EnableExtensions
set "QUIET=0"
if /I "%~1"=="/quiet" set "QUIET=1"
set "SETUP_SCRIPT=%~f0"
set "BACKEND_DIR=%~dp0ai_backend"
if not exist "%BACKEND_DIR%\requirements.txt" set "BACKEND_DIR=%~dp0App\ai_backend"
if not exist "%BACKEND_DIR%\requirements.txt" (
  echo AI backend files were not found under the Miniscuplter installation.
  if "%QUIET%"=="0" pause
  exit /b 3
)
cd /d "%BACKEND_DIR%"

echo Miniscuplter v1.0 AI runtime setup

echo.
set "PYTHON_CMD="
where py >nul 2>nul
if not errorlevel 1 (
  py -3.10 -c "import sys,struct; raise SystemExit(0 if sys.version_info[:2]==(3,10) and struct.calcsize('P')*8==64 else 1)" >nul 2>nul
  if not errorlevel 1 set "PYTHON_CMD=py -3.10"
)
if not defined PYTHON_CMD (
  where python >nul 2>nul
  if not errorlevel 1 (
    python -c "import sys,struct; raise SystemExit(0 if sys.version_info[:2]==(3,10) and struct.calcsize('P')*8==64 else 1)" >nul 2>nul
    if not errorlevel 1 set "PYTHON_CMD=python"
  )
)
if not defined PYTHON_CMD (
  echo Python 3.10 x64 was not found. Install Python 3.10 x64, then use Repair AI Runtime in Miniscuplter Launcher.
  if "%QUIET%"=="0" pause
  exit /b 2
)

if not exist .venv (
  %PYTHON_CMD% -m venv .venv
  if errorlevel 1 exit /b 1
)
call .venv\Scripts\activate.bat
python -m pip install --upgrade pip setuptools wheel
if errorlevel 1 exit /b 1

where nvidia-smi >nul 2>nul
if errorlevel 1 (
  echo No NVIDIA GPU detected. Installing CPU PyTorch.
  pip install torch==2.5.1 torchvision==0.20.1
) else (
  echo NVIDIA GPU detected. Installing CUDA 12.4 PyTorch runtime.
  pip install torch==2.5.1 torchvision==0.20.1 --index-url https://download.pytorch.org/whl/cu124
)
if errorlevel 1 exit /b 1

pip install -r requirements.txt
if errorlevel 1 exit /b 1
pip check
if errorlevel 1 exit /b 1

set "RUNTIME_HASH="
for /f "usebackq delims=" %%H in (`powershell -NoProfile -Command "$a=[IO.File]::ReadAllBytes((Resolve-Path 'requirements.txt')); $b=[IO.File]::ReadAllBytes('%SETUP_SCRIPT%'); $all=New-Object byte[] ($a.Length+$b.Length); [Array]::Copy($a,0,$all,0,$a.Length); [Array]::Copy($b,0,$all,$a.Length,$b.Length); ([BitConverter]::ToString([Security.Cryptography.SHA256]::HashData($all))).Replace('-','').ToLowerInvariant()"`) do set "RUNTIME_HASH=%%H"
if not defined RUNTIME_HASH (
  echo Could not calculate the AI runtime fingerprint.
  exit /b 1
)
> .venv\miniscuplter_runtime.sha256 echo %RUNTIME_HASH%

echo.
echo AI runtime is ready under %CD%\.venv
if defined MINISCULPTER_DATA echo AI models will be stored under %MINISCULPTER_DATA%
echo Download model weights from Miniscuplter Launcher; model updates remain manual.
echo.
if "%QUIET%"=="0" pause
exit /b 0
