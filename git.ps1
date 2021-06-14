Import-Module PowershellForXti -Force

$script:coreConfig = [PSCustomObject]@{
    RepoOwner = "JasonBenfield"
    RepoName = "XTI_Git"
    AppName = "XTI_Git"
    AppType = "Package"
    ProjectDir = ""
}

function Git-New-XtiIssue {
    param(
        [Parameter(Mandatory, Position=0)]
        [string] $IssueTitle,
        $Labels = @(),
        [string] $Body = "",
        [switch] $Start
    )
    $script:coreConfig | New-XtiIssue @PsBoundParameters
}

function Git-Xti-StartIssue {
    param(
        [Parameter(Position=0)]
        [long]$IssueNumber = 0,
        $IssueBranchTitle = "",
        $AssignTo = ""
    )
    $script:coreConfig | Xti-StartIssue @PsBoundParameters
}

function Git-New-XtiVersion {
    param(
        [Parameter(Position=0)]
        [ValidateSet("major", "minor", "patch")]
        $VersionType,
        [ValidateSet("Development", "Production", "Staging", "Test")]
        $EnvName = "Production"
    )
    $script:coreConfig | New-XtiVersion @PsBoundParameters
}

function Git-Xti-Merge {
    param(
        [Parameter(Position=0)]
        [string] $CommitMessage
    )
    $script:coreConfig | Xti-Merge @PsBoundParameters
}

function Git-New-XtiPullRequest {
    param(
        [Parameter(Position=0)]
        [string] $CommitMessage
    )
    $script:coreConfig | New-XtiPullRequest @PsBoundParameters
}

function Git-Xti-PostMerge {
    param(
    )
    $script:coreConfig | Xti-PostMerge @PsBoundParameters
}

function Git-Publish {
    param(
        [switch] $Prod
    )
    $script:coreConfig | Xti-PublishPackage @PsBoundParameters
    if($Prod) {
        $script:coreConfig | Xti-Merge
    }
}
