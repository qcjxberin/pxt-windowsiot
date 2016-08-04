# pxt-windowsiot

A Windows IoT Core gateway for PXT data streams.

To use this app,

* Make sure you've installed the [Serial drivers](https://codethemicrobit.com/device/serial).
* connect your micro:bit to your computer and upload [this program](https://codethemicrobit.com/xuvcgmjfbq)

```
radio.setTransmitSerialNumber(true);
radio.setGroup(42);
let i = 0;
radio.onDataReceived(() => {
    led.toggle(i % 5, i / 5);
    i = (++i % 25);
    radio.writeValueToSerial();
});
```

* leave your micro:bit connected!

Students should send data using `radio.sendData` using group 42.

```
radio.setTransmitSerialNumber(true);
radio.setGroup(42);
basic.forever(() => {
    radio.sendValue("data", input.lightLevel())
})
```

## Building

* Open `CloudClient/CloudClient.sln` in Visual Studio 2015.
* Select `Debug / x86` and build!

