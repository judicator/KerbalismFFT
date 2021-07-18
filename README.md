# Kerbalism FarFutureTechnologies

Experimental Kerbalism support for Far Future Technologies.

## What parts and features of Far Future Technologies mod are supported and how?

### Antimatter tanks

Antimatter tanks now have full Kerbalism resource system support:
* Planner in VAB/Hangar: information about EC consumption of antimatter containment.
* EC consumption for active vessel: works exactly as in FFT, but use Kerbalism EC consumption/production system.
* EC consumption for unloaded vessels. If vessel runs out of electric charge, antimatter containment will be shut down and all antimatter will annihilate. Resulting thermal energy will be added to antimatter tank on vessel load, which could result in tank explosion. Do not leave your antimatter tanks without power!

### Fusion reactors (this includes some FFT engines with fusion reactors)
* Planner in VAB/Hangar: information about EC production and De (or De/He3) consumption.
* Support for unloaded vessels. Reactor will automatically adjust it's throttle (respecting min and max throttle values) in order to satisfy vessel electricity consumption.

### Particle detector science experiment

Converted to Kerbalism science experiment. Experiment duration set to 2 Kerbin years.

### Engines reliability

Universal Kerbalism reliability patch makes not really good work for FFT engines. All FFT engines are given more suitable reliability parameters, like unlimited ignitions count.

### Fusion reactors radioactivity

Like reactors from NFElectrical, FFT fusion reactors now emit some radiation.
However, reactors will start emitting radiation only then they have been started, and after they have been shutdown, emission will slowly decay to some minimum value.

### FFT engines radioactivity

All FFT engines are radioactive. Some of them are **extremly** radioactive.
This mod implements new mechanics for FFT engines: they start emitting radiation then started, and emission is tied to engine throttle. After engine has been shutdown, emission will rapidly decay to some minimum value.


## Dependencies

* [Kerbalism (3.14)](https://github.com/Kerbalism/Kerbalism)
* [FarFutureTechnologies (1.1.4)](https://github.com/post-kerbin-mining-corporation/FarFutureTechnologies)
* [KerbalismSystemHeat (0.4)](https://github.com/judicator/KerbalismSystemHeat)
* [Module manager (last version preferred)](https://github.com/sarbian/ModuleManager)


## Installation

Please remove mod folder (zKerbalismFFT) from GameData folder inside your Kerbal Space Program folder before installation.

Then place the GameData folder from downloaded archive inside your Kerbal Space Program folder.


## Licensing

The MIT License (MIT)

Copyright (c) 2021 Alexander Rogov

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
