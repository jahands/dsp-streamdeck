@echo off

goto :cleanup
goto :copyTree
goto :buildExe
REM goto :copyExe
goto :craftPlugin
goto :end

:cleanup
del /s /q "com.github.jahands.dsp_production.streamDeckPlugin"
del /s /q "com.github.jahands.dsp_production.sdPlugin"

:copyTree
xcopy ..\tree .\com.github.jahands.dsp_production.sdPlugin\ /S /Y

:buildExe
echo Building executables ...
cd "../VBANDeck"
call build_exe.bat
cd "../build"

:craftPlugin
DistributionTool.exe com.github.jahands.dsp_production.sdPlugin .

:end
del /s /q "com.github.jahands.dsp_production.sdPlugin"
echo Done.
