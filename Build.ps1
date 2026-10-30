param([string]$Configuration='Release')
$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
  foreach($arch in 'x64','Win32') {
    $folder=if($arch -eq 'Win32'){'x86'}else{'x64'}
    cmake -S . -B "obj/$folder" -A $arch
    if($LASTEXITCODE){throw 'CMake configure failed'}
    cmake --build "obj/$folder" --config $Configuration
    if($LASTEXITCODE){throw 'Native build failed'}
    foreach($ahk in 'AHK v1','AHK v2') {
      New-Item -ItemType Directory -Force "$ahk/Lib/$folder" | Out-Null
      Copy-Item "obj/$folder/$Configuration/InterceptorBridge.dll" "$ahk/Lib/$folder/"
    }
  }
  $vs=& "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -property installationPath
  & "$vs/MSBuild/Current/Bin/MSBuild.exe" Managed/AutoHotInterception/AutoHotInterception.csproj /p:Configuration=$Configuration /v:minimal
  if($LASTEXITCODE){throw 'Managed build failed'}
  foreach($ahk in 'AHK v1','AHK v2'){Copy-Item obj/Managed/AutoHotInterception.dll "$ahk/Lib/"}
  & "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe" /nologo /target:winexe /optimize+ /win32manifest:Launcher/AutoHotInterceptor.manifest /out:AutoHotInterceptor.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll Launcher/Launcher.cs
  if($LASTEXITCODE){throw 'Launcher build failed'}
} finally {Pop-Location}
