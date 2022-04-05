Import-Module PowershellForXti -Force

$script:xtiConfig = [PSCustomObject]@{
    RepoOwner = "JasonBenfield"
    RepoName = "XTI_Git"
}

function Xti-NewVersion {
    param(
        [Parameter(Position=0)]
        [ValidateSet("major", "minor", "patch")]
        $VersionType
    )
    $script:xtiConfig | New-BaseXtiVersion @PsBoundParameters
}

function Xti-NewIssue {
    param(
        [Parameter(Mandatory, Position=0)]
        [string] $IssueTitle,
        [switch] $Start
    )
    $script:xtiConfig | New-BaseXtiIssue @PsBoundParameters
}

function Xti-StartIssue {
    param(
        [Parameter(Position=0)]
        [long]$IssueNumber = 0
    )
    $script:xtiConfig | BaseXti-StartIssue @PsBoundParameters
}

function Xti-CompleteIssue {
    $script:xtiConfig | BaseXti-CompleteIssue @PsBoundParameters
}

function Xti-Publish {
    param(
        [ValidateSet("Development", "Production")]
        $EnvName,
        [ValidateSet("Default", "DB")]
        $HubAdministrationType = "Default"
    )
    $script:xtiConfig | BaseXti-Publish @PsBoundParameters
}
