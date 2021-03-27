#!/bin/sh

today=$(date "+%s")
base_dir=$(dirname $0)

out_dir="${base_dir}/nupkgs"

# create packages
dotnet clean -c Release "${base_dir}/src/Elffy.sln"
dotnet build -c Release "${base_dir}/src/Elffy.sln"
# dotnet pack "${base_dir}/src/Elffy/Elffy.csproj" -c Release --version-suffix ".${today}" -o ${out_dir}

for proj in "${base_dir}/src/Elffy/Elffy.csproj" \
            "${base_dir}/src/Elffy.Games/Elffy.Games.csproj" \
            "${base_dir}/src/Elffy.Memory/Elffy.Memory.csproj" \
            "${base_dir}/src/Elffy.Imaging/Elffy.Imaging.csproj" \
            "${base_dir}/src/Elffy.SimpleKit/Elffy.SimpleKit.csproj" \
            "${base_dir}/src/ElffyGenerator/ElffyGenerator.csproj"

do
  dotnet pack $proj -c Release --version-suffix ".${today}" -o ${out_dir} -p:PackageVersion=0.0.0.${today}
done

echo ""
echo "--------------------------"
echo "Complete creating packages ! >> ${out_dir}/"
echo ""

ls ${out_dir} | grep $today
