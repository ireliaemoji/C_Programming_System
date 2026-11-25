param(
    [string]$BaseUrl = 'http://127.0.0.1:5000'
)

$ErrorActionPreference = 'Stop'
$suffix = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
$teacher1 = "boundary_teacher_$suffix"
$teacher2 = "boundary_teacher_other_$suffix"
$student1 = "boundary_student_$suffix"
$student2 = "boundary_student_other_$suffix"
$password = 'Boundary123!'

function Invoke-Json {
    param([string]$Method, [string]$Path, [string]$Token, [object]$Body, [int]$ExpectedStatus = 200)
    $headers = @{}
    if ($Token) { $headers.Authorization = "Bearer $Token" }
    try {
        $json = if ($null -eq $Body) { $null } else { $Body | ConvertTo-Json -Depth 8 -Compress }
        $request = @{ Uri = $BaseUrl.TrimEnd('/') + $Path; Method = $Method; Headers = $headers; UseBasicParsing = $true }
        if ($null -ne $json) { $request.ContentType = 'application/json'; $request.Body = $json }
        $response = Invoke-WebRequest @request
        $status = [int]$response.StatusCode
        $payload = if ($response.Content) { $response.Content | ConvertFrom-Json } else { $null }
    } catch {
        $status = [int]$_.Exception.Response.StatusCode.value__
        $payload = $null
    }
    if ($status -ne $ExpectedStatus) { throw "$Method $Path expected HTTP $ExpectedStatus but got $status" }
    return $payload
}

function New-User([string]$Username, [string]$Role) {
    Invoke-Json POST '/api/auth/register' $null @{ username = $Username; password = $password } | Out-Null
    $login = Invoke-Json POST '/api/auth/login' $null @{ username = $Username; password = $password }
    if ($login.role -ne $Role) { throw "$Username expected role $Role but got $($login.role)" }
    return $login.token
}

$teacherToken = New-User $teacher1 'Teacher'
$otherTeacherToken = New-User $teacher2 'Teacher'
$studentToken = New-User $student1 'Student'
$otherStudentToken = New-User $student2 'Student'

$question = Invoke-Json POST '/api/teacher/questions' $teacherToken @{ stem = '边界测试题'; optionA = 'A'; optionB = 'B'; optionC = 'C'; optionD = 'D'; correctOption = 1; isPublished = $true; visibility = 'TeacherPrivate' }
$class1 = Invoke-Json POST '/api/teacher/classes' $teacherToken @{ name = "边界班级一_$suffix" }
$class2 = Invoke-Json POST '/api/teacher/classes' $otherTeacherToken @{ name = "边界班级二_$suffix" }
Invoke-Json POST "/api/teacher/classes/$($class1.id)/members" $teacherToken @{ usernames = @($student1) } | Out-Null
Invoke-Json POST "/api/teacher/classes/$($class2.id)/members" $otherTeacherToken @{ usernames = @($student2) } | Out-Null

$now = [DateTimeOffset]::UtcNow
$past = Invoke-Json POST '/api/teacher/exams' $teacherToken @{ title = "已过期边界考试_$suffix"; questionIds = @([long]$question.id); studentIds = @(); classIds = @([long]$class1.id); startAt = $now.AddMinutes(-20).ToString('O'); endAt = $now.AddMinutes(-10).ToString('O') }
$future = Invoke-Json POST '/api/teacher/exams' $teacherToken @{ title = "未开始边界考试_$suffix"; questionIds = @([long]$question.id); studentIds = @(); classIds = @([long]$class1.id); startAt = $now.AddMinutes(10).ToString('O'); endAt = $now.AddMinutes(20).ToString('O') }
$current = Invoke-Json POST '/api/teacher/exams' $teacherToken @{ title = "跨班级边界考试_$suffix"; questionIds = @([long]$question.id); studentIds = @(); classIds = @([long]$class1.id); startAt = $now.AddMinutes(-1).ToString('O'); endAt = $now.AddMinutes(10).ToString('O') }

Invoke-Json POST "/api/exams/$($past.id)/start" $studentToken $null 404 | Out-Null
Invoke-Json POST "/api/exams/$($future.id)/start" $studentToken $null 404 | Out-Null
Invoke-Json POST "/api/exams/$($current.id)/start" $studentToken $null 200 | Out-Null
Invoke-Json POST "/api/exams/$($current.id)/start" $otherStudentToken $null 404 | Out-Null
Invoke-Json POST "/api/teacher/classes/$($class1.id)/members" $otherTeacherToken @{ usernames = @($student2) } 400 | Out-Null
Invoke-Json POST '/api/teacher/classes' $studentToken @{ name = "越权班级_$suffix" } 403 | Out-Null

$otherClasses = Invoke-Json GET '/api/teacher/classes' $otherTeacherToken $null
if (@($otherClasses | Where-Object { $_.id -eq $class1.id }).Count -ne 0) { throw 'other teacher could read a class owned by the first teacher' }

Write-Output "PASS business boundary checks: expired=$($past.id), future=$($future.id), cross-class=$($current.id)"
