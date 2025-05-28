if "%2" equ "" (
  set param1=HEAD
  set param2=%1
) else (
  set param1=%1
  set param2=%2
)

git diff --name-status %param2% %param1% > _list-file.txt