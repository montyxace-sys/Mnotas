# Compila todos los artefactos de Mnotas que se pueden generar en Windows.
# Ejecutar desde la raíz del repo: powershell -ExecutionPolicy Bypass -File build-all.ps1
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $MyInvocation.MyCommand.Path)

function Step([string]$title, [scriptblock]$action) {
    Write-Host "`n===== $title =====" -ForegroundColor Cyan
    & $action
}

Step 'Pruebas del núcleo' {
    dotnet run --project tests\Mnotas.CoreTests\Mnotas.CoreTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas del núcleo.' }
}

Step 'WPF Windows x64 (portable + MSI)' {
    # Los archivos AssemblyInfo generados sin RID y con RID se duplican si se dejan los obj anteriores.
    if (Test-Path obj) { Remove-Item obj -Recurse -Force }
    New-Item -ItemType Directory -Path publish -Force | Out-Null
    dotnet publish Mnotas.csproj -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist\portable
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo publicar Mnotas.exe' }
    Copy-Item dist\portable\Mnotas.exe publish\Mnotas.exe -Force
    wix build installer.wxs -arch x64 -o dist\Mnotas-1.1.0-x64.msi
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el MSI' }
}

$desktops = @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')
foreach ($rid in $desktops) {
    Step "Avalonia escritorio $rid" {
        dotnet publish Mnotas.Desktop\Mnotas.Desktop.csproj -c Release -r $rid `
            --self-contained true -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true -o "dist\desktop\$rid"
        if ($LASTEXITCODE -ne 0) { throw "No se pudo publicar Avalonia $rid" }
    }
}

foreach ($rid in $desktops) {
    Step "CLI $rid" {
        dotnet publish Mnotas.Cli\Mnotas.Cli.csproj -c Release -r $rid `
            --self-contained true -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true -o "dist\cli\$rid"
        if ($LASTEXITCODE -ne 0) { throw "No se pudo publicar la CLI $rid" }
    }
}

Step 'Android (APK)' {
    dotnet publish Mnotas.Android\Mnotas.Android.csproj -c Release -f net11.0-android `
        -p:AndroidPackageFormat=apk -o dist\android-apk
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo publicar el APK' }
}

Step 'Android (AAB)' {
    dotnet publish Mnotas.Android\Mnotas.Android.csproj -c Release -f net11.0-android `
        -p:AndroidPackageFormat=aab -o dist\android-aab
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo publicar el AAB' }
}

Step 'iOS (binario de dispositivo; el IPA requiere firma Apple)' {
    dotnet build Mnotas.iOS\Mnotas.iOS.csproj -c Release -r ios-arm64
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el binario iOS' }
}

Write-Host "`nArtefactos en publish\ y dist\" -ForegroundColor Green
Get-ChildItem publish,dist -Recurse -File |
    Where-Object { $_.Extension -in '.exe', '.msi', '.apk', '.aab', '.ipa', '.zip', '.gz' -or $_.Name -in 'Mnotas.Portable', 'mnotas' } |
    Select-Object FullName, Length
