# check argument
if [ "$#" -lt 1 ]; then
    echo "Require destination path"
fi
ssh -i key -v -o "StrictHostKeyChecking=no" $ESS_USER_TPS@$ESS_HOST_TPS<<EOF
mkdir "E:\www\Auto\ess_temp"
# rd "D:\Temp\Source\build\" /S/Q
EOF
scp -o "StrictHostKeyChecking=no" -v -i key -r build $ESS_USER_TPS@$ESS_HOST_TPS:"E:\www\Auto"
# # check argument
# if [ "$#" -lt 1 ]; then
    # echo "Require destination path"
# fi

# $BasePath='D:\Temp'
# $SourcePath='D:\Temp\Source'
# $Destination='ess'
# # bash generate random 32 character alphanumeric string (upper and lowercase) and 
# NEW_UUID=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1)
# # Create temp dir
# mkdir -p "$BasePath\ess_$NEW_UUID"
# cp -R "$SourcePath\build\*" "$BasePath\ess_$NEW_UUID"
# $Message='ESS Application is currently updating ...'
# echo "$Message" > App_Offline.htm
# cp App_Offline.htm "$BasePath\$Destination"
# rm -r "$BasePath\$Destination"
# cp -R "$BasePath\ess_$NEW_UUID\$Destination" "$BasePath\$Destination"