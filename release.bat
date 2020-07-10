REM Release.batのディレクトリに移動
pushd %~dp0

REM 前データの削除
del /F /Q /s static-pages-downloader\exe
del /F /Q /s static-pages-downloader\Script
rmdir /s /Q static-pages-downloader\exe
rmdir /s /Q static-pages-downloader\Script

REM ディレクトリの作成
mkdir static-pages-downloader
mkdir static-pages-downloader\Exe
mkdir static-pages-downloader\Script

REM 実行に必要なファイルのコピー
copy /Y StaticPagesDownloader\settings.yml static-pages-downloader\settings.yml
copy /Y .\LICENSE static-pages-downloader\LICENSE
copy /Y .\README.md static-pages-downloader\README.md
xcopy /Y /e /R StaticPagesDownloader\bin\Release\netcoreapp3.1\publish static-pages-downloader\exe\
xcopy /Y /e /R StaticPagesDownloader\bin\Release\netcoreapp3.1\publish\Script static-pages-downloader\Script

REM 不必要なファイルを削除する
del /F /Q static-pages-downloader\exe\*.dev.json
del /F /Q static-pages-downloader\exe\*.exe
del /F /Q /s static-pages-downloader\exe\Script
rmdir /Q /s static-pages-downloader\exe\Script

REM ファイルの日時の更新
pushd static-pages-downloader
copy *+

pushd exe
copy *+

REM 元のディレクトリに戻る
popd
popd
popd
pause