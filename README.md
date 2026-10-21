# robocad-сs (.net) [![NuGet](https://img.shields.io/nuget/v/RobocadCs.svg)](https://www.nuget.org/packages/RobocadCs/)

C# port of the **robocadV** (Soft-V) library.
Supports the **Algaritm**, **Studica** (VMX/Titan), the
**Shufflecad** dashboard and the **YDLidar X2**, **n10**.




## Studica HCDIO on Raspberry Pi 4 (pigpio)

Real hardware uses the OS-managed `pigpiod` daemon and its text FIFOs
`/dev/pigpio` and `/dev/pigout`. The library does not start a daemon or a `pigs`
process. Remove the old pi-blaster startup service when migrating. Enable the
pigpiod FIFO interface (do not use `-f`), use the default 5 us sample rate
(`-s 5`), and grant the application's user read/write access to the FIFOs.
Do not run two controlling applications concurrently: FIFO replies are shared.

The default PWM frequency is 100 Hz and range is 10000. HCDIO ports 1..10 map
to BCM GPIO 4, 18, 17, 27, 23, 22, 24, 25, 7, 5. Angle conversion remains
`duty = 0.000666 * angle + 0.05`; the public PWM argument remains a fraction
from 0 to 1. Changing frequency also changes the pulse width for a given duty.
For example, duty 0.15 at 100 Hz is a 1500 us pulse.

Configuration exposes the command FIFO path, response FIFO path, frequency,
and response timeout in seconds (defaults: `/dev/pigpio`, `/dev/pigout`, 100, 1.0).
The obsolete pi-blaster configuration flag has been removed. Java uses JNA;
C# uses libc directly. Neither requires a pigpio language-specific binding.
Java/C# FIFO access requires Linux with `statx` (glibc 2.28+, kernel 4.11+), as
provided by current Raspberry Pi OS. Simulated robots do not access GPIO FIFOs.

Stopping drives **all ten** HCDIO signals LOW, including outputs left active by
an earlier program. Failures are reported after attempting every output and
robot cleanup. A stopped instance rejects subsequent nonzero HCDIO commands;
calling stop again retries disabling outputs. A missing or unresponsive daemon
raises an error rather than creating a regular file or waiting indefinitely.

Stopping pulses does **not** switch off servo power. Some digital servos keep
holding their last position without pulses; guaranteed torque removal for
those servos requires separately controlled power.

Configuration property names: `PigpioPipePath`, `PigpioResponsePath`, `HcdioPwmFrequency`, `PigpioTimeout`.

### FIFO regression checks (Linux)

These checks use temporary FIFOs and a fake daemon; no robot or pigpiod is needed.

```bash
dotnet build tests/PigpioProbe/PigpioProbe.csproj
python3 tests/pigpio_fifo_test.py dotnet tests/PigpioProbe/bin/Debug/net8.0/PigpioProbe.dll
```
