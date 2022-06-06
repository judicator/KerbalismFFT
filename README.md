# Kerbalism FarFutureTechnologies

"Middleman" mod, which implements experimental Kerbalism resource system support for [Nertea's Far Future Technologies](https://forum.kerbalspaceprogram.com/index.php?/topic/199070-*/).


## What parts and features of Far Future Technologies mod are supported and how?

### Antimatter tanks

* Planner in VAB/Hangar: information about EC consumption of antimatter containment.
* EC consumption for active vessel: works exactly as in FFT, but use Kerbalism EC consumption/production system.
* EC consumption for unloaded vessels. If vessel runs out of electric charge, antimatter containment will be shut down and all antimatter will annihilate. Resulting thermal energy will be added to antimatter tank on vessel load, which could result in tank explosion. Do not leave your antimatter tanks without power!

### Fusion reactors (this includes some FFT engines with built-in fusion reactors)

* Planner in VAB/Hangar: information about EC production and De (or De/He3) consumption.
* Support for unloaded vessels. Reactor will automatically adjust it's throttle (respecting minimum throttle value) in order to satisfy vessel electricity consumption.

### Particle detector science experiment

Converted to Kerbalism science experiment. Experiment duration set to 2 Kerbin years.

### Engines reliability

All FFT engines are given more suitable reliability parameters, like unlimited ignitions count, unlimited MTBF and so on.

## Dependencies

* [KerbalismSystemHeat (0.5.0)](https://github.com/judicator/KerbalismSystemHeat) (bundled as part of download)
* [Kerbalism (3.14)](https://github.com/Kerbalism/Kerbalism)
* [FarFutureTechnologies (1.2.0)](https://github.com/post-kerbin-mining-corporation/FarFutureTechnologies)
* [Module manager (last version preferred)](https://github.com/sarbian/ModuleManager)


## Supported KSP versions

KerbalismFFT have been tested in KSP versions from 1.8.1 to 1.12.3.


## Installation

Please remove mod folder (`zKerbalismFFT`) from `GameData` folder inside your Kerbal Space Program folder before installation.

Then place the GameData folder from downloaded archive inside your Kerbal Space Program folder.


## Mod settings

In `zKerbalismFFT/Settings.cfg` file you can change two coefficients:

* Radiation emission of all FFT engines will be multiplied by `FFT_Engines_Radioactivity_Coeff`. Set it to lower value (let's say, `0.1` or even `0.01`), if you feel that FFT engines are too radioactive.
* Radiation emission of all fusion reactors will be multiplied by `FFT_FusionReactors_Radioactivity_Coeff`. Feel free to change this value as you see fit.


## Optional patch

There is an optional patch in `Extras/FFTFusionReactorsLowerMinThrust`. It changes minimum throttle for fusion reactor from default 10% to 5%.

If you want to install it, just copy `SystemHeatFissionReactorsLowerMinThrust` folder to your `GameData`.


## Licensing

The MIT License (MIT)

Copyright (c) 2022 Alexander Rogov

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
