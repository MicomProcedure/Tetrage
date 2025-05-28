if "%2" equ "" (
  set param1=HEAD
  set param2=%1
) else (
  set param1=%1
  set param2=%2
)

setlocal enabledelayedexpansion
set diff=
for /f "usebackq" %%i in (`git diff --name-only --diff-filter=ACMR %param2% %param1%`) do (
  set diff=!diff! "%%i"
)

git archive --format=zip --prefix=diff_archive/ %param1% %diff% --output=_list-archive.zip