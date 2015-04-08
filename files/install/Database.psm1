Import-Module "sqlps" -DisableNameChecking
[Reflection.Assembly]::LoadWithPartialName('System.Data')



function Update-Database {
    param (
        [string]$connectionString,
        [string]$upgradeBasePath
    )
    $cs=New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
    $instance=$cs.DataSource
    $database=$cs.InitialCatalog
    Write-Host "Upgrading" $database "on" $instance

    $v=Get-DatabaseVersion $connectionString
    while (Test-Path -Path (Join-Path $upgradeBasePath $v)) {
        Write-Host "Upgrading version: " $v

        $upgradePath=Join-Path $upgradeBasePath $v
        $upgradeScript=Join-Path $upgradePath Upgrade.sql
        if (!(Test-Path -Path $upgradeScript)) {
            throw $upgradeScript + "not found!"
        }
        ExecuteScript-Database $connectionString $upgradeScript

        $v2=Get-DatabaseVersion $connectionString
        if ($v -eq $v2) {
            throw "Version number was not updated!"
        }
        $v=$v2
    }
    Write-Host "Upgraded" $database "on" $instance
    Write-Host "Current version: " $v
    Write-Host
}
Set-Alias updb Upgrade-Database



function Reset-Database {
    param (
        [string]$connectionString,
        [string]$defaultDatabasePath,
        [string]$databaseSourceFilename='Isogeo.Central.bacpac',
        [string]$databaseSourceDir='\\HQ.isogeo.fr\GlobalShare\Developments\Isogeo.Central\current\',
        [bool]$localBackup=$TRUE
    )
    $databaseSourcePath=Join-Path $databaseSourceDir $databaseSourceFilename
    $ext=[System.IO.Path]::GetExtension($databaseSourceFilename)

    $cs=New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
    $instance=$cs.DataSource
    $database=$cs.InitialCatalog

    Set-Location "SQLSERVER:\SQL\$instance\"
    $server=Get-Item .
    $serverName=$server.NetName

#    if ($serverName -ne [System.Net.Dns]::GetHostName()) {
#        $remoteSession=New-PSSession -ComputerName $serverName
#    }
    try {
        if ($server.Databases[$database] -ne $NULL) {
            $db=$server.Databases[$database]

            $fgc=$db.FileGroups
            if ($defaultDatabasePath -eq $NULL) {
                $defaultDatabasePath=Split-Path $fgc[$db.DefaultFileGroup].Files[0].FileName
            }

            if ($db.Status -bor [Microsoft.SqlServer.Management.Smo.DatabaseStatus]::Normal) {
                Write-Host "Deleting" $database "on" $instance

                $server.KillAllProcesses($database)
#                $db.SetOffline()
                try {
                    $server.KillDatabase($database)
                } catch {
                }
            }
        }

#        if ($remoteSession -ne $NULL) {
#            Import-PSSession -Session $remoteSession
#        }
        if ($ext -eq ".mdf") {
            if ($localBackup)  {
                $dbfile=Join-Path -Path $defaultDatabasePath -ChildPath (Split-Path $databaseSourcePath -Leaf)
                Write-Host "Copying" $databaseSourcePath "to" $dbfile "on" $serverName -ForegroundColor DarkGray
                [System.IO.File]::Copy($databaseSourcePath, $dbfile, $TRUE)
                #Copy-Item -Path $databaseSourcePath -Destination $defaultDatabasePath
            } else {
                $dbfile=$databaseSourcePath
            }
        }
    } finally {
#        if ($remoteSession -ne $NULL) {
#            Remove-PSSession -Session $remoteSession
#        }
    }

    if ($ext -eq ".mdf") {
        Write-Host "Attaching" $database "on" $instance
        $strColl=New-Object System.Collections.Specialized.StringCollection
        $strColl.Add($dbfile) | Out-Null
        $server.AttachDatabase($database, $strColl, [Microsoft.SqlServer.Management.Smo.AttachOptions]::RebuildLog)
    } elseif ($ext -eq ".bacpac") {
        Write-Host "Importing" $database "on" $instance
        Add-Type -Path "C:\Program Files (x86)\Microsoft SQL Server\120\DAC\bin\Microsoft.SqlServer.Dac.dll" 
        $ds=New-Object Microsoft.SqlServer.Dac.DacServices($cs);
        $bp=[Microsoft.SqlServer.Dac.BacPackage]::Load($databaseSourcePath)
        $ds.ImportBacpac($bp, $database)
    }
    Write-Host
}
Set-Alias rdb Reset-Database



function Get-DatabaseVersion {
    param (
        [string]$connectionString
    )
    
    $query='SELECT dbo.GetVersion()'
    $v="0.0.0.0"

    $cs=New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
    $instance=$cs.DataSource
    $database=$cs.InitialCatalog
    $username=$cs.UserID

    if (!([string]::IsNullOrEmpty($username))) {
        $password=$cs.Password
        if (!([string]::IsNullOrEmpty($password))) {
            $version=Invoke-Sqlcmd -Query $query -ServerInstance $cs.DataSource -Database $cs.InitialCatalog -Username $username -Password $password -AbortOnError -OutputSqlErrors $TRUE
            $v=$version[0]
        } else {
            $version=Invoke-Sqlcmd -Query $query -ServerInstance $cs.DataSource -Database $cs.InitialCatalog -Username $username -AbortOnError -OutputSqlErrors $TRUE
            $v=$version[0]
        }
    } else {
        $version=Invoke-Sqlcmd -Query $query -ServerInstance $cs.DataSource -Database $cs.InitialCatalog -AbortOnError -OutputSqlErrors $TRUE
        $v=$version[0]
    }

    return [version]$v
}
Set-Alias gdbv Get-DatabaseVersion



function ExecuteScript-Database {
    param (
        [string]$connectionString,
        [string]$scriptPath
    )

    $cs=New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
    $instance=$cs.DataSource
    $database=$cs.InitialCatalog
    $username=$cs.UserID
    Set-Location (Split-Path $scriptPath)
    $scriptName=Split-Path $scriptPath -leaf

    $sqlcmd="sqlcmd.exe"
    $csp=Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\120\Tools\ClientSetup"
    if ($csp -ne $NULL) {
        $sqlcmd=Join-Path $csp.ODBCToolsPath sqlcmd.exe
    }

    $sqlcmdargs=@("-t"; "600"; "-b"; "-X"; "-S"; "$instance"; "-d"; "$database")
    if (!([string]::IsNullOrEmpty($username))) {
        $sqlcmdargs+="-U"
        $sqlcmdargs+="$username"

        $password=$cs.Password
        if (!([string]::IsNullOrEmpty($password))) {
            $sqlcmdargs+="-P"
            $sqlcmdargs+="""$password"""
        }
    } else {
        $sqlcmdargs+="-E"
    }
    $sqlcmdargs+="-i"
    $sqlcmdargs+="$scriptName"

    #Invoke-Sqlcmd -InputFile $scriptName -ServerInstance $cs.DataSource -Database $cs.InitialCatalog -AbortOnError -OutputSqlErrors
    & "$sqlcmd" $sqlcmdargs | Write-Host -ForegroundColor DarkGray
    if ($LastExitCode -ne 0) {
        throw "SQL command failed with error code $LASTEXITCODE"
    }
}



Export-ModuleMember -Function Get-DatabaseVersion,Reset-Database,Update-Database -Alias gdbv,rdb,updb