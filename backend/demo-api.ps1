param([string]$BaseUrl = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'

function Send-Command([string]$Path, [hashtable]$Body) {
    Invoke-RestMethod -Method Post -Uri "$BaseUrl$Path" `
        -ContentType 'application/json' -Body ($Body | ConvertTo-Json -Depth 10)
}

$state = Send-Command '/api/warehouses/' @{
    name = 'Yamen delivery demo'
    rows = 1
    columns = 5
    cells = @(
        @{ position = @{ row = 0; column = 1 }; type = 'Pickup' }
        @{ position = @{ row = 0; column = 4 }; type = 'Dropoff' }
    )
}
$warehousePath = "/api/warehouses/$($state.id)"
$state = Send-Command "$warehousePath/robots" @{
    expectedRevision = $state.revision
    position = @{ row = 0; column = 0 }
    battery = 100
    capacity = 5
}
$state = Send-Command "$warehousePath/tasks" @{
    expectedRevision = $state.revision
    pickup = @{ row = 0; column = 1 }
    dropoff = @{ row = 0; column = 4 }
    load = 2
    priority = 1
}
$taskId = $state.tasks[0].id
$state = Send-Command "$warehousePath/tasks/$taskId/assign" @{ expectedRevision = $state.revision }
for ($step = 0; $step -lt 6; $step++) {
    $state = Send-Command "$warehousePath/ticks" @{ expectedRevision = $state.revision }
    Write-Output "Tick $($state.tick): robot=$($state.robots[0].state), task=$($state.tasks[0].status), battery=$($state.robots[0].battery)"
}
if ($state.tasks[0].status -ne 'Completed') { throw 'Demo delivery did not complete.' }
Write-Output "Completed. Inspect $BaseUrl$warehousePath"
