$siteName = 'ChocolateyServer'
$appPoolName = 'ChocolateyServerAppPool'
$sitePath = 'c:\tools\chocolatey.server'

function Add-Acl {
    [CmdletBinding()]
    Param (
        [string]$Path,
        [System.Security.AccessControl.FileSystemAccessRule]$AceObject
    )

    Write-Verbose "Retrieving existing ACL from $Path"
    $objACL = Get-ACL -Path $Path
    $objACL.AddAccessRule($AceObject)
    Write-Verbose "Setting ACL on $Path"
    Set-ACL -Path $Path -AclObject $objACL
}

function New-AclObject {
    [CmdletBinding()]
    Param (
        [string]$SamAccountName,
        [System.Security.AccessControl.FileSystemRights]$Permission,
        [System.Security.AccessControl.AccessControlType]$AccessControl = 'Allow',
        [System.Security.AccessControl.InheritanceFlags]$Inheritance = 'None',
        [System.Security.AccessControl.PropagationFlags]$Propagation = 'None'
    )

    New-Object -TypeName System.Security.AccessControl.FileSystemAccessRule($SamAccountName, $Permission, $Inheritance, $Propagation, $AccessControl)
}

if ($null -eq (Get-Command -Name 'choco.exe' -ErrorAction SilentlyContinue)) {
    Write-Warning "Chocolatey not installed. Cannot install standard packages."
    Exit 1
}

# Install Chocolatey.Server
choco upgrade chocolatey.server -y

# Step by step instructions here https://chocolatey.org/docs/how-to-set-up-chocolatey-server#setup-normally
# Import the right modules
Import-Module WebAdministration
# Disable or remove the Default website
Get-Website -Name 'Default Web Site' | Stop-Website
Set-ItemProperty "IIS:\Sites\Default Web Site" serverAutoStart False    # disables website

# Set up an app pool for Chocolatey.Server. Ensure 32-bit is enabled and the managed runtime version is v4.0 (or some version of 4). Ensure it is "Integrated" and not "Classic".
New-WebAppPool -Name $appPoolName -Force
Set-ItemProperty IIS:\AppPools\$appPoolName enable32BitAppOnWin64 True       # Ensure 32-bit is enabled
Set-ItemProperty IIS:\AppPools\$appPoolName managedRuntimeVersion v4.0       # managed runtime version is v4.0
Set-ItemProperty IIS:\AppPools\$appPoolName managedPipelineMode Integrated   # Ensure it is "Integrated" and not "Classic"
Restart-WebAppPool -Name $appPoolName   # likely not needed ... but just in case

# Set up an IIS website pointed to the install location and set it to use the app pool.
New-Website -Name $siteName -ApplicationPool $appPoolName -PhysicalPath $sitePath

# Add permissions to c:\tools\chocolatey.server:
'IIS_IUSRS', 'IUSR', "IIS APPPOOL\$appPoolName" | ForEach-Object {
    $obj = New-AclObject -SamAccountName $_ -Permission 'ReadAndExecute' -Inheritance 'ContainerInherit','ObjectInherit'
    Add-Acl -Path $sitePath -AceObject $obj
}

# Add the permissions to the App_Data subfolder:
$appdataPath = Join-Path -Path $sitePath -ChildPath 'App_Data'
'IIS_IUSRS', "IIS APPPOOL\$appPoolName" | ForEach-Object {
    $obj = New-AclObject -SamAccountName $_ -Permission 'Modify' -Inheritance 'ContainerInherit', 'ObjectInherit'
    Add-Acl -Path $appdataPath -AceObject $obj
}
