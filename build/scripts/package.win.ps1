Remove-Item -Path build\SourceGitPlus\*.pdb -Force
Compress-Archive -Path build\SourceGitPlus -DestinationPath "build\sourcegitplus_${env:VERSION}.${env:RUNTIME}.zip" -Force