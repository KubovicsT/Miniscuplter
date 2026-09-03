@echo off
setlocal EnableExtensions
cd /d "%~dp0ai_backend"

echo Miniscuplter v0.2 AI setup

echo.
set "PYTHON_CMD="
where py >nul 2>nul
if not errorlevel 1 (
  py -3.10 -c "import sys" >nul 2>nul
  if not errorlevel 1 set "PYTHON_CMD=py -3.10"
)
if not defined PYTHON_CMD (
  where python >nul 2>nul
  if not errorlevel 1 set "PYTHON_CMD=python"
)
if not defined PYTHON_CMD (
  echo Python was not found. Install Python 3.10 x64 and run this file again.
  pause
  exit /b 1
)

if not exist .venv (
  %PYTHON_CMD% -m venv .venv
  if errorlevel 1 exit /b 1
)
call .venv\Scripts\activate.bat
python -m pip install --upgrade pip setuptools wheel
if errorlevel 1 exit /b 1

REM Prefer NVIDIA CUDA wheels when an NVIDIA GPU is present. Hunyuan3D 2.1
REM documents PyTorch 2.5.1 + CUDA 12.4 for its reference environment.
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

echo.
echo Core v0.2 AI environment is ready.
echo Start Miniscuplter and open AI Components to download the 2D and 3D model weights.
echo The models are downloaded from their official Hugging Face repositories and are NOT stored in this Git repository.
echo.
pause
