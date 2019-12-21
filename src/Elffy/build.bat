: <<EOF
@Echo Off
Goto WINDOWS
EOF
exec cmd //c ${0//\//\\\\} $*
:WINDOWS


"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" /p:Configuration=Release Elffy.csproj


