Import-Module PowershellForXti -Force

$script:gitConfig = [PSCustomObject]@{
    RepoOwner = "JasonBenfield"
    RepoName = "XTI_Git"
    AppName = "XTI_Git"
    AppType = "Package"
}

function Git-NewVersion {
    param(
        [Parameter(Position=0)]
        [ValidateSet("major", "minor", "patch")]
        $VersionType
    )
    $script:gitConfig | New-XtiVersion @PsBoundParameters
}

function Git-NewIssue {
    param(
        [Parameter(Mandatory, Position=0)]
        [string] $IssueTitle,
        [switch] $Start
    )
    $script:gitConfig | New-XtiIssue @PsBoundParameters
}

function Git-StartIssue {
    param(
        [Parameter(Position=0)]
        [long]$IssueNumber = 0
    )
    $script:gitConfig | Xti-StartIssue @PsBoundParameters
}

function Git-CompleteIssue {
    param(
        [ValidateSet("Development", "Production", "Staging", "Test")]
        $EnvName = "Production"
    )
    $script:gitConfig | Xti-CompleteIssue @PsBoundParameters
}

function Git-Publish {
    param(
        [ValidateSet("Development", "Production", "Staging", "Test")]
        $EnvName
    )
    $script:gitConfig | Xti-Publish @PsBoundParameters
}
