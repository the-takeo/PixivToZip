REM ##=============================================================##
REM ## �v���W�F�N�g�̃r���h�R�}���h��
REM ## �ȉ���ݒ肷��
REM ## cd $(ProjectDir)
REM ## cd ..
REM ## if not exist ForInstall (goto :eof)
REM ## cd ForInstall
REM ## SetProducts.bat $(ConfigurationName) $(TargetDir)
REM ## :eof
REM ##=============================================================##

SET c_name=%1
SET p_dir=%2

if "%c_name%" neq "Release" (
echo "AfterBuildEvent.bat will run only in `Release` mode. Current mode is `%c_name%`"
goto :eof
)

if not exist Products (goto :eof)
set copyto="Products"

copy /Y %p_dir%* %copyto%

REM ##=============================================================##
REM ## �I��
REM ##=============================================================##
:eof