# Get-SonarrBackup

## SYNOPSIS
Gets backup instance information from Sonarr.

## SYNTAX

### Set 1
```
Get-SonarrBackup [[-Type] <BackupType[]>] [<CommonParameters>]
```

## DESCRIPTION
Retrieves information about recent backups performed from within Sonarr.  Results can be filtered by the type of backup operation that occurred.

## EXAMPLES

### EXAMPLE 1

```powershell
C:\PS> Get-SonarrBackup
```

## PARAMETERS

### Type
Indicates to return only backups that are of the selected type(s).

```yaml
Type: BackupType[]
Parameter Sets: Set 1
Aliases: 

Required: false
Position: 0
Default Value: 
Accepted Values: Manual, Scheduled, Update
Pipeline Input: False
```

### \<CommonParameters\>
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None


## OUTPUTS

### MG.Sonarr.Results.Backup


## NOTES

## RELATED LINKS

[Online Version:](https://github.com/Yevrag35/PoshSonarr/wiki/Get-SonarrBackup)
