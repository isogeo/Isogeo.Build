function Unzip-File {
    param (
        [string]$sourcePath,
        [string]$destinationPath
    )
    $_csp=$script:MyInvocation.MyCommand.Path
    $sevenzipPath=Join-Path (Split-Path (Split-Path $_csp)) '7za.exe'
    if (!(Test-Path $sevenzipPath)) {
        Write-Host -ForegroundColor DarkGray "7za.exe was not found at $sevenzipPath"
        $sevenzipPath=Join-Path (Split-Path (Split-Path $_csp) -Parent) 'build\7za.exe'
    }
    if (!(Test-Path $sevenzipPath)) {
        Write-Host -ForegroundColor Yellow "7za.exe was not found at $sevenzipPath"
        $sevenzipPath=Join-Path (Get-ItemProperty -Path "HKLM:\SOFTWARE\7-Zip").Path '7za.exe'
    }
    if (!(Test-Path $sevenzipPath)) {
        throw "7za.exe was not found at $sevenzipPath"
    }

    $sevenzipcmd=$sevenzipPath+" x `"$sourcePath`" -o$destinationPath -aoa -y"
    Write-Host "$sevenzipcmd"
    Invoke-Expression -Command "$sevenzipcmd" | Write-Host -ForegroundColor DarkGray
    FailOnError
}
Set-Alias uz Unzip-File

function FailOnError {
  if ($LastExitCode -ne 0) {
    throw "Command failed with code $LastExitCode"
  }
}
Set-Alias foe FailOnError

# cf. http://blogs.msdn.com/b/powershell/archive/2007/06/18/using-a-dsl-to-generate-xml-in-powershell.aspx
function New-ManifestDocument {
    param (
        [scriptblock]$sb
    )
    $doc = [xml] '<?xml version="1.0" encoding="utf-8"?><isogeoInstallManifest/>'

    #
    # Execute the scriptblock to get the list of element hashtables
    $elements = & $sb

    # Now iterate over the list construction elements then adding
    # the specified attributes. 
    foreach ($e in $elements)
    {
        $elem = $doc.CreateElement($e.element)
        foreach ($attr in $e.Attributes.GetEnumerator())
        {
            $elem.SetAttribute($attr.name, $attr.value)
        }
        # The next step would be to recursivly construct the
        # of this node but that's not implemented yet...

        # Finally add this element
        [void] $doc.get_ChildNodes().Item(1).AppendChild($elem)
    }
    
    $doc
}
Set-Alias nmfd New-ManifestDocument

# cf. http://blogs.msdn.com/b/powershell/archive/2007/06/18/using-a-dsl-to-generate-xml-in-powershell.aspx
function New-ParameterDocument {
    param (
        [scriptblock]$sb
    )
    $doc = [xml] '<?xml version="1.0" encoding="utf-8"?><parameters/>'

    #
    # Execute the scriptblock to get the list of element hashtables
    $elements = & $sb

    # Now iterate over the list construction elements then adding
    # the specified attributes. 
    foreach ($e in $elements)
    {
        $elem = $doc.CreateElement($e.element)
        foreach ($attr in $e.Attributes.GetEnumerator())
        {
            $elem.SetAttribute($attr.name, $attr.value)
        }
        # The next step would be to recursivly construct the
        # of this node but that's not implemented yet...

        # Finally add this element
        [void] $doc.get_ChildNodes().Item(1).AppendChild($elem)
    }
    
    $doc
}
Set-Alias npad New-ParameterDocument

Export-ModuleMember -Function Unzip-File,FailOnError,New-ManifestDocument,New-ParameterDocument -Alias uz,foe,nmfd,npad
