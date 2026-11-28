param(
    [string]$BaseUrl = 'http://127.0.0.1:5000',
    [pscredential]$OtherTeacher
)

$ErrorActionPreference = 'Stop'
$suffix = [DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff')
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
        if ($null -eq $_.Exception.Response) { throw }
        $status = [int]$_.Exception.Response.StatusCode
        $payload = $null
    }
    if ($status -ne $ExpectedStatus) { throw "$Method $Path expected HTTP $ExpectedStatus but got $status" }
    return $payload
}

function Get-Token([string]$Username, [string]$Password, [string]$Role) {
    $login = Invoke-Json POST '/api/auth/login' $null @{ username = $Username; password = $Password }
    if ($login.role -ne $Role) { throw "$Username expected role $Role but got $($login.role)" }
    return $login.token
}

function New-Student([string]$Username) {
    Invoke-Json POST '/api/auth/register' $null @{ username = $Username; password = $password; role = 'Teacher' } | Out-Null
    return Get-Token $Username $password 'Student'
}

$teacherToken = Get-Token 'teacher' 'teacher123' 'Teacher'
$studentToken = New-Student $student1
$otherStudentToken = New-Student $student2

$question = Invoke-Json POST '/api/teacher/questions' $teacherToken @{ stem = 'Boundary test question'; optionA = 'A'; optionB = 'B'; optionC = 'C'; optionD = 'D'; correctOption = 1; isPublished = $true; visibility = 'TeacherPrivate' }
$class1 = Invoke-Json POST '/api/teacher/classes' $teacherToken @{ name = "Boundary class A $suffix" }
$class2 = Invoke-Json POST '/api/teacher/classes' $teacherToken @{ name = "Boundary class B $suffix" }
Invoke-Json POST "/api/teacher/classes/$($class1.id)/members" $teacherToken @{ usernames = @($student1) } | Out-Null
Invoke-Json POST "/api/teacher/classes/$($class2.id)/members" $teacherToken @{ usernames = @($student2) } | Out-Null

$now = [DateTimeOffset]::UtcNow
$past = Invoke-Json POST '/api/teacher/exams' $teacherToken @{ title = "Expired exam $suffix"; questionIds = @([long]$question.id); studentIds = @(); classIds = @([long]$class1.id); startAt = $now.AddMinutes(-20).ToString('O'); endAt = $now.AddMinutes(-10).ToString('O') }
$future = Invoke-Json POST '/api/teacher/exams' $teacherToken @{ title = "Future exam $suffix"; questionIds = @([long]$question.id); studentIds = @(); classIds = @([long]$class1.id); startAt = $now.AddMinutes(10).ToString('O'); endAt = $now.AddMinutes(20).ToString('O') }
$current = Invoke-Json POST '/api/teacher/exams' $teacherToken @{ title = "Class boundary exam $suffix"; questionIds = @([long]$question.id); studentIds = @(); classIds = @([long]$class1.id); startAt = $now.AddMinutes(-1).ToString('O'); endAt = $now.AddMinutes(10).ToString('O') }

Invoke-Json POST "/api/exams/$($past.id)/start" $studentToken $null 404 | Out-Null
Invoke-Json POST "/api/exams/$($future.id)/start" $studentToken $null 404 | Out-Null
Invoke-Json POST "/api/exams/$($current.id)/start" $studentToken $null 200 | Out-Null
Invoke-Json POST "/api/exams/$($current.id)/start" $otherStudentToken $null 404 | Out-Null
Invoke-Json POST '/api/teacher/classes' $studentToken @{ name = "Forbidden class $suffix" } 403 | Out-Null

# Publish an untimed exam, then edit the bank before two students open it.
$snapshotExam = Invoke-Json POST '/api/teacher/exams' $teacherToken @{
    title = "Snapshot exam $suffix"
    questionIds = @([long]$question.id)
    classIds = @([long]$class1.id, [long]$class2.id)
}
Invoke-Json PUT "/api/teacher/questions/$($question.id)" $teacherToken @{
    stem = 'Edited bank question'; optionA = 'Edited A'; optionB = 'B'; optionC = 'C'; optionD = 'D'
    correctOption = 2; points = 7; isPublished = $true; visibility = 'TeacherPrivate'
} | Out-Null

$attempt = Invoke-Json POST "/api/exams/$($snapshotExam.id)/start" $studentToken $null
$otherAttempt = Invoke-Json POST "/api/exams/$($snapshotExam.id)/start" $otherStudentToken $null
$original = Invoke-Json GET "/api/exam-attempts/$($attempt.id)/questions" $studentToken $null
$otherView = Invoke-Json GET "/api/exam-attempts/$($otherAttempt.id)/questions" $otherStudentToken $null
if ($original.stem -ne 'Boundary test question' -or $original.optionA -ne 'A' -or $otherView.stem -ne $original.stem -or $otherView.optionA -ne $original.optionA) {
    throw 'Question bank changes altered the published exam'
}
if ($original.PSObject.Properties['correctOption']) { throw 'Active exam exposed its answer key' }

Invoke-Json POST "/api/exam-attempts/$($attempt.id)/answers" $studentToken @{
    questionId = [long]$question.id; selectedOption = 1; revision = 1
} | Out-Null
Invoke-Json POST "/api/exam-attempts/$($attempt.id)/submit" $studentToken $null | Out-Null
Invoke-Json POST "/api/teacher/exams/$($snapshotExam.id)/close" $teacherToken $null | Out-Null
Invoke-Json POST "/api/teacher/exams/$($snapshotExam.id)/publish-grades" $teacherToken $null | Out-Null
Invoke-Json PUT "/api/teacher/questions/$($question.id)" $teacherToken @{
    stem = 'Bank edited after grades'; optionA = 'Edited again'; optionB = 'B'; optionC = 'C'; optionD = 'D'
    correctOption = 3; points = 9; isPublished = $true; visibility = 'TeacherPrivate'
} | Out-Null
$review = Invoke-Json GET "/api/grades/$($attempt.id)/answers" $studentToken $null
if ($review.stem -ne $original.stem -or $review.optionA -ne $original.optionA -or $review.selectedOption -ne 1 -or $review.correctOption -ne 1 -or $review.points -ne 1 -or $review.maxPoints -ne 1) {
    throw 'Published review does not match the original exam and recorded score'
}
Write-Output 'PASS exam snapshot, grading and historical review'

if ($OtherTeacher) {
    if ($OtherTeacher.UserName -eq 'teacher') { throw 'Supply a different, pre-provisioned teacher.' }
    $otherTeacherToken = Get-Token $OtherTeacher.UserName $OtherTeacher.GetNetworkCredential().Password 'Teacher'
    Invoke-Json POST "/api/teacher/classes/$($class1.id)/members" $otherTeacherToken @{ usernames = @($student2) } 400 | Out-Null
    $otherClasses = Invoke-Json GET '/api/teacher/classes' $otherTeacherToken $null
    if (@($otherClasses | Where-Object { $_.id -eq $class1.id }).Count -ne 0) { throw 'other teacher could read a class owned by the first teacher' }
    Write-Output 'PASS cross-teacher class isolation'
} else {
    Write-Output 'SKIP cross-teacher class isolation (supply -OtherTeacher)'
}

Write-Output "PASS business boundary checks: expired=$($past.id), future=$($future.id), cross-class=$($current.id)"
