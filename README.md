# HomeBear.Rainbow

<img src="docs/header.png" width="100" /> 

> Windows 10 IoT Core UWP app that works great with the [Pimoroni RainbowHAT](https://shop.pimoroni.com/products/rainbow-hat-for-android-things). The app is currently work in progress.

## Prerequirements
- Windows 10
- Visual Studio 2019
- Raspbbery Pi 3 (B) with [Windows 10 IoT Core](https://developer.microsoft.com/en-us/windows/iot) 17763 or higher
- [Pimoroni RainbowHAT](https://shop.pimoroni.com/products/rainbow-hat-for-android-things)

## How it looks

At the moment, it's just a headless application that is controlled by the input controls of the RainbowHAT.

## Features

- [x] Control the large R, G, B LEDs
- [x] Seven APA102 multicolour LEDs
- [x] Listen to 'A', 'B', 'C' capacitive touch buttons
- [ ] 14-segment alphanumeric displays
- [x] BMP280 temperature sensor
- [ ] BMP280 pressure sensor
- [ ] BMP280 humidity sensor
- [ ] Piezo buzzer

## Contributing

Feel free to improve the quality of the code. It would be great to learn more from experienced C#, UWP and IoT developers.

## Authors

Just me, [Tobi]([https://tscholze.github.io).

## Thanks to

* Pimoroni [Discord](https://discordapp.com/invite/hr93ByC) Community
* Pimoroni Python [source](https://github.com/pimoroni/rainbow-hat/blob/master/library/rainbowhat/bmp280.py)
* [Microsoft IoT Samples](https://github.com/ms-iot/adafruitsample/blob/master/Lesson_203/FullSolution/BMP280.cs)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.
Dependencies or assets maybe licensed differently.