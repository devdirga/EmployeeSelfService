 dotnet publish KANO.ESS.sln -c Release
 Remove-Item -Path ".\build" -Recurse -Force
 & New-Item -ItemType Directory -Force -Path ".\build" >$null 2>&1
 
 Foreach($file in dir .){   
    $releaseDir = "$($file.FullName)\bin\Release\netcoreapp2.2\"
    If(((Get-Item $file.PSPath) -is [System.IO.DirectoryInfo]) -and (Test-Path $releaseDir -PathType Any)){            
            echo ">>> $($file.Name)"            
            $newDir = ".\build\$($file.Name)\"
            $newFullDir = ".\build\full\$($file.Name)\"
            
            echo "    - copying build files"
            & New-Item -ItemType Directory -Force -Path $newDir >$null 2>&1
            & New-Item -ItemType Directory -Force -Path $newFullDir >$null 2>&1
            Copy-Item -Path "$($releaseDir)*" -Destination $newDir
            Copy-Item -Path "$($releaseDir)publish\*" -Destination $newFullDir

            if($file.Name -eq "KANO.ESS"){
                echo "    - copying wwwroot"
                Copy-Item -Recurse "$($releaseDir)publish\wwwroot" -Destination "$($newDir)wwwroot" -Container
            }

            echo "    - removing unused files"
            Remove-Item -Path "$($newDir)*" -Include *.json            
            Remove-Item -Path "$($newDir)publish"
            Remove-Item -Path "$($newDir)Properties"
            Remove-Item -Path "$($newDir)Connected Services"            
            #Remove-Item .\build
    }
    
}

$runCommand = @'
<# Get current directory path #>
taskkill /IM "dotnet.exe" /F
$src = (Get-Item -Path ".\" -Verbose).FullName;

<# Iterate all directories present in the current directory path #>
Get-ChildItem $src -directory | where {$_.PsIsContainer} | Select-Object -Property Name | ForEach-Object {
    $cdProjectDir = [string]::Format("cd /d {0}\{1}",$src, $_.Name);
    
    <# Get project's bundle config file path #>    
    $projectDir = [string]::Format("{0}\{1}\{2}.dll",$src, $_.Name,$_.Name); 
    $fileExists = Test-Path $projectDir;
    
    <# Check project having bundle config file #>
    if($fileExists -eq $true){
        <# Start cmd process and execute 'dotnet run' #>
        $params=[string]::Format("/C {0} && dotnet {1}.dll",$cdProjectDir,$_.Name)                
        echo "Running $($_.Name)"
        #Start-Process -Verb runas "cmd.exe" $params;        
        Start-Process "cmd.exe" $params;
    }
}  
'@
echo $runCommand > ".\build\run.ps1"
echo $runCommand > ".\build\full\run.ps1"