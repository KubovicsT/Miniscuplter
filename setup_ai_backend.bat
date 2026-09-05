@echo off
setlocal EnableExtensions EnableDelayedExpansion
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

echo Miniscuplter v1.0.5 AI runtime setup
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

rem Keep large runtime downloads out of the Windows user TEMP folder. The
rem cache survives Repair retries, so interrupted multi-gigabyte downloads can
rem resume instead of starting from scratch.
if defined MINISCULPTER_DATA (
  set "RUNTIME_CACHE=%MINISCULPTER_DATA%\runtime-cache"
) else (
  set "RUNTIME_CACHE=%BACKEND_DIR%\.runtime-cache"
)
set "RUNTIME_DOWNLOADS=%RUNTIME_CACHE%\downloads"
set "RUNTIME_TEMP=%RUNTIME_CACHE%\temp"
set "PIP_CACHE_DIR=%RUNTIME_CACHE%\pip-cache"
if not exist "%RUNTIME_DOWNLOADS%" mkdir "%RUNTIME_DOWNLOADS%"
if not exist "%RUNTIME_TEMP%" mkdir "%RUNTIME_TEMP%"
if not exist "%PIP_CACHE_DIR%" mkdir "%PIP_CACHE_DIR%"
set "TMP=%RUNTIME_TEMP%"
set "TEMP=%RUNTIME_TEMP%"
set "PIP_DEFAULT_TIMEOUT=180"
set "PIP_RETRIES=20"

python -m pip install --upgrade pip setuptools wheel
if errorlevel 1 exit /b 1

where nvidia-smi >nul 2>nul
if errorlevel 1 (
  echo No NVIDIA GPU detected. Installing CPU PyTorch.
  python -m pip install torch==2.5.1 torchvision==0.20.1 --timeout 180 --retries 20
  if errorlevel 1 exit /b 1
) else (
  echo NVIDIA GPU detected. Installing CUDA 12.4 PyTorch runtime.
  set "TORCH_WHEEL=!RUNTIME_DOWNLOADS!\torch-2.5.1+cu124-cp310-cp310-win_amd64.whl"
  set "VISION_WHEEL=!RUNTIME_DOWNLOADS!\torchvision-0.20.1+cu124-cp310-cp310-win_amd64.whl"
  set "TORCH_URL=https://download.pytorch.org/whl/cu124/torch-2.5.1+cu124-cp310-cp310-win_amd64.whl"
  set "VISION_URL=https://download.pytorch.org/whl/cu124/torchvision-0.20.1+cu124-cp310-cp310-win_amd64.whl"

  where curl.exe >nul 2>nul
  if errorlevel 1 (
    echo curl.exe is required for resumable PyTorch downloads on Windows.
    exit /b 1
  )

  echo Downloading PyTorch wheel to persistent cache. Interrupted downloads will resume on the next Repair.
  echo Destination: !TORCH_WHEEL!
  curl.exe -L --fail --retry 20 --retry-delay 5 --retry-all-errors --connect-timeout 30 -C - -o "!TORCH_WHEEL!" "!TORCH_URL!"
  if errorlevel 1 (
    echo PyTorch download did not finish. Run Repair AI Runtime again to resume it.
    exit /b 1
  )
  python -c "import pathlib,sys,zipfile; p=pathlib.Path(sys.argv[1]); ok=p.is_file() and p.stat().st_size>2000000000 and zipfile.is_zipfile(p); print(f'PyTorch wheel: {p.stat().st_size/1024/1024:.1f} MiB' if p.is_file() else 'PyTorch wheel missing'); raise SystemExit(0 if ok else 1)" "!TORCH_WHEEL!"
  if errorlevel 1 (
    echo Downloaded PyTorch file is incomplete or invalid. Delete !TORCH_WHEEL! and run Repair AI Runtime again.
    exit /b 1
  )

  echo Downloading torchvision wheel to persistent cache.
  echo Destination: !VISION_WHEEL!
  curl.exe -L --fail --retry 20 --retry-delay 5 --retry-all-errors --connect-timeout 30 -C - -o "!VISION_WHEEL!" "!VISION_URL!"
  if errorlevel 1 (
    echo torchvision download did not finish. Run Repair AI Runtime again to resume it.
    exit /b 1
  )
  python -c "import pathlib,sys,zipfile; p=pathlib.Path(sys.argv[1]); ok=p.is_file() and p.stat().st_size>5000000 and zipfile.is_zipfile(p); print(f'torchvision wheel: {p.stat().st_size/1024/1024:.1f} MiB' if p.is_file() else 'torchvision wheel missing'); raise SystemExit(0 if ok else 1)" "!VISION_WHEEL!"
  if errorlevel 1 (
    echo Downloaded torchvision file is incomplete or invalid. Delete !VISION_WHEEL! and run Repair AI Runtime again.
    exit /b 1
  )

  python -m pip install "!TORCH_WHEEL!" "!VISION_WHEEL!" --timeout 180 --retries 20
  if errorlevel 1 exit /b 1
)

python -m pip install -r requirements.txt --timeout 180 --retries 20
if errorlevel 1 exit /b 1
python -m pip check
if errorlevel 1 exit /b 1

set "RUNTIME_HASH="
for /f "usebackq delims=" %%H in (`powershell -NoProfile -Command "$a=[IO.File]::ReadAllBytes((Resolve-Path 'requirements.txt')); $b=[IO.File]::ReadAllBytes('%SETUP_SCRIPT%'); $all=New-Object byte[] ($a.Length+$b.Length); [Array]::Copy($a,0,$all,0,$a.Length); [Array]::Copy($b,0,$all,$a.Length,$b.Length); $sha=[Security.Cryptography.SHA256]::Create(); ([BitConverter]::ToString($sha.ComputeHash($all))).Replace('-','').ToLowerInvariant()"`) do set "RUNTIME_HASH=%%H"
if not defined RUNTIME_HASH (
  echo Could not calculate the AI runtime fingerprint.
  exit /b 1
)
> .venv\miniscuplter_runtime.sha256 echo %RUNTIME_HASH%

echo.
echo AI runtime is ready under %CD%\.venv
if defined MINISCULPTER_DATA echo AI models and runtime cache are stored under %MINISCULPTER_DATA%
echo Download model weights from Miniscuplter Launcher; model updates remain manual.
echo.
if "%QUIET%"=="0" pause
exit /b 0
