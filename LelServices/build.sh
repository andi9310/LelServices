cd src
for D in *; do
    if [ -d "${D}" ]; then
		cd "${D}"
		dotnet publish -c Release -o out
		cd ..
    fi
done