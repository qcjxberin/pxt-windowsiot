setlocal

set TARGET_PATH=C:\projects\git_repos\arturl-iot-utilities\DeviceCenter

copy /y AppPackages\CloudClient_1.0.0.0_ARM_Test\CloudClient_1.0.0.0_ARM.appx %TARGET_PATH%\DeviceCenter\Assets\Apps\CloudGateway\ARM
copy /y AppPackages\CloudClient_1.0.0.0_ARM_Test\CloudClient_1.0.0.0_ARM.cer %TARGET_PATH%\DeviceCenter\Assets\Apps\CloudGateway\ARM

copy /y AppPackages\CloudClient_1.0.0.0_x86_Test\CloudClient_1.0.0.0_x86.appx %TARGET_PATH%\DeviceCenter\Assets\Apps\CloudGateway\x86
copy /y AppPackages\CloudClient_1.0.0.0_x86_Test\CloudClient_1.0.0.0_x86.cer %TARGET_PATH%\DeviceCenter\Assets\Apps\CloudGateway\x86

