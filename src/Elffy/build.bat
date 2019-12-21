: <<EOF
@Echo Off
Goto WINDOWS
EOF
exec cmd //c ${0//\//\\\\} $*
:WINDOWS


set msbuild_path="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
%msbuild_path% -t:restore
%msbuild_path% /p:Configuration=Release Elffy.csproj


