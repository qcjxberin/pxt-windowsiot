// This is the main script that runs on the micro controller unit
// The data is received via radio and sent to gateway via serial

// The MCU also sends commands to and reads text from the gateway

radio.setTransmitSerialNumber(true);
input.onButtonPressed(Button.A, () => {
    led.plot(0, 0)
    radio.sendValue("light", input.lightLevel());
    basic.pause(50)
    led.unplot(0, 0)
});

radio.onDataReceived(() => {
    led.plot(2, 2)
    radio.writeValueToSerial();
    serial.writeString("")
    basic.pause(20)
    led.unplot(2, 2)
});
