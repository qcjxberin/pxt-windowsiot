// This is the main script that runs on the micro controller unit
// The data is received via radio and sent to gateway via serial
radio.setTransmitSerialNumber(true);
radio.setGroup(42);
let i = 0;
radio.onDataReceived(() => {
    led.toggle(i % 5, i / 5);
    i = (++i % 25);
    radio.writeValueToSerial();
});