REM Release.bat�̃f�B���N�g���Ɉړ�
pushd %~dp0

REM �O�f�[�^�̍폜
del /F /Q /s static-pages-downloader\exe
del /F /Q /s static-pages-downloader\Script
rmdir /s /Q static-pages-downloader\exe
rmdir /s /Q static-pages-downloader\Script

REM �f�B���N�g���̍쐬
mkdir static-pages-downloader
mkdir static-pages-downloader\Exe
mkdir static-pages-downloader\Script

REM ���s�ɕK�v�ȃt�@�C���̃R�s�[
copy /Y StaticPagesDownloader\settings.yml static-pages-downloader\settings.yml
copy /Y .\LICENSE static-pages-downloader\LICENSE
copy /Y .\README.md static-pages-downloader\README.md
xcopy /Y /e /R StaticPagesDownloader\bin\Release\netcoreapp3.1\publish static-pages-downloader\exe\
xcopy /Y /e /R StaticPagesDownloader\bin\Release\netcoreapp3.1\publish\Script static-pages-downloader\Script

REM �s�K�v�ȃt�@�C�����폜����
del /F /Q static-pages-downloader\exe\*.dev.json
del /F /Q static-pages-downloader\exe\*.exe
del /F /Q /s static-pages-downloader\exe\Script
rmdir /Q /s static-pages-downloader\exe\Script

REM �t�@�C���̓����̍X�V
pushd static-pages-downloader
copy *+

pushd exe
copy *+

REM ���̃f�B���N�g���ɖ߂�
popd
popd
popd
pause