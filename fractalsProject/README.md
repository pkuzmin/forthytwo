# FPGA fractal demo
Markus Nentwig, 20?? - 2020

### Youtube video:

_Note: Note: blurring and artifacts are phone camera artifacts not present in the generated electrical VGA signal_
<a href="http://www.youtube.com/watch?feature=player_embedded&v=XnHhH9rjF9c
" target="_blank"><img src="http://img.youtube.com/vi/XnHhH9rjF9c/0.jpg" 
alt="FPGA demo" width="240" height="180" border="10" /></a>

Fractal generation is a popular FPGA design exercise. 
For example, Stanford university used it as [lab assignment](https://web.stanford.edu/class/ee183/handouts/lect7.pdf) already in 2002. 
It must have been around that time that I saw a similar demo on some trade fair, and had the itch to try my hands on it ever since.

Well, I got around to it eventually. 

It took a long time.

## Motivation
An FPGA free-time project adds one unique design challenge: It needs to be fun all along the way. 

Sometimes, the way to the finish line gets straight, obvious and _boring_. Like hiking versus commuting, it's not just about getting from A to B in the most efficient manner... So you throw in a new idea to make it interesting again. Rinse and repeat.

## Overview
The "fun factor driven requirements management" eventually evolved along those lines:

* Real time calculation: The Stanford lab exercise demanded it already in 2002.
* Full HD resolution (1920x1080) at 60 Hz
* Use the FPGA sensibly. The resulting implementation comes close to one operation per multiplier per clock cycle. That's 18 billion multiplications per second on the 35-size Artix with a USB bus power budget of ~2 Watts.	
* Perform dynamic resource allocation. The fractals algorithm is somewhat unusual as the required number of iterations varies between points. Compared to simply setting a fixed number of iterations, complexity increases substantially (a random number of results may appear in one clock cycle, results are unordered) but so does performance.
* Limit to 18-bit multiplications because it is the native width for Xilinx 6/7 series DSP48 blocks. It is straightforward to increase the internal bitwidth for higher resolution, but resource usage skyrockets.
* Be (reasonably) vendor-independent. I decided to go for the open-source "J1B" soft-core CPU instead of e.g. Microblaze MCS, which would have been very straightforward.
* Instead of the original "gforth" toolchain for J1B, I decided to use my own simple compiler / assembler "forthytwo.exe" which got cleaned up along the way.
* Floating point math - fixed point is tedious for performance-uncritical code _"For when you gaze long into the rabbit hole, the rabbit hole gazes back into you"_ My own "minimal" float implementation doesn't try to be as refined or safe as IEEE 754, but is _small_ and does a great job so far.
* A CPU Bootloader on plain UART (meaning no proprietary Xilinx JTAG). The included bootloader implements robust synchronization and efficient binary upload.
* No esoteric tools, be able to run on Windows (again, Linux would be easier).
On a clean Windows PC, the build system can be set up by installing MinGW (developer settings), Vivado and Verilator. See my install notes for the latter. Use e.g. Teraterm with the bootloader.
* Batteries-included project, intended for reuse by deleting what is not needed (maybe this is more on the microcontroller side, as the fractals part is quite problem-specific)

## Ready/valid design pattern notes
The calculation engine relies heavily on the [valid/ready handshaking paradigm](https://inst.eecs.berkeley.edu/~cs150/Documents/Interfaces.pdf), which is used consistently throughout the chain.

Here, it is quite essential, for a simple reason: 
The 200 MHz clock rate of the fractal generator is less than two times the VGA pixel rate. Any "sub-optimal" handshaking scheme that required an idle clock cycle to recover would break the architecture.

### Ready / valid combinational path problem
In a typical processing block, data moves in lock-step through a sequence of registers. 
When cascading any number of such blocks via ready-/valid interfaces, the "ready" signals form a combinational path from the end to the beginning of the chain. This can make timing closure difficult or impossible.
The problem is clear to see when considering what happens when the end of the processing chain signals "not ready" (to accept data):
The data over the whole length of the pipeline has nowhere to go, therefore the whole chain must be stopped within a single clock cycle.

The solution is to divide the chain into multiple segments using FIFOs (2 slots is enough).
There's a catch: I could design an "optimized" FIFO that will accept data even if full, as long as an element is taken from the output in the same clock cycle.
This "optimization" would introduce exactly the combinational path the FIFO is supposed to break, thus it would be useless for decoupling the combinational chain.
In other words, the input of the FIFO may not use the output-side "ready" signal combinationally.

### Ready / valid flow control
Maybe this is obvious, but the data flow in a ready-/valid chain can be stalled at any point simply by inserting a block that forces both valid and ready signal to zero (not asserted).
This block may be combinational and may depend on an observed data value. 
This pattern is used to stop the calculation engine from running too far ahead of the monitor's electron beam.

## RTL implementation

### Clock domain
There are three clock domains: 
* The calculation engine (most of the design) at 200 MHz. This could be pushed higher but would make the design more difficult to work with. Right now, there is no reason for doing so, and the chip already runs quite hot.
* The VGA monitor signals at a pixel frequency of 148.5 MHz
* The J1B CPU at 100 MHz, because its critical path is too slow for 200 MHz operation.

### Data flow
_Note: Names in the picture correspond to the Verilog implementation_

![Top level diagram](https://github.com/mnentwig/forthytwo/blob/master/fractalsProject/wwwSrc/systemDiagram.png "Top level diagram")

Block "vga" **100** creates the VGA monitor timing. One of its outputs is the number of the pixel currently under the electron beam.

The pixel position passes via Gray coding through a clock domain crossing **110** into the "trigger" block **120**.
Here, the start of a new frame is detected when the pixel position returns to zero. 
This happens immediately after the last visible pixel has been sent to the display so the front porch / VSYNC / back porch time intervals can be used to pre-compute image data, up to the capacity of the buffer RAM **220**.

Detection of a new frame start triggers the following "pixScanner" **130**. This block has already received fractal coordinates from CPU **140** and scans them row by row, using two pairs of increments: a first delta X/Y pair for the electron beam moving right (colums) and a second pair for moving down (rows). Using appropriate deltas, the picture can be rotated by any angle.

The block keeps a frame counter, which is polled by CPU **140** to start computing the next frame coordinates as soon as the previous ones have been stored.

Generated pixel coordinates move forward into FIFO **150**. This is solely to decouple the combinational accept/ready paths. 
It does not improve throughput since the pixel scanner is already capable of generating one output per clock cycle.

"Pixel coordinates" are formed by the X and Y location in fractal space and the linear pixel position, equivalent to its counterpart from vga block **100**. 
The latter is necessary because results will need to be re-ordered.

The pixel coordinates now move into a cyclic distribution queue **170**. Its purpose is to serve pixel coordinates to the parallel "julia" (fractal) calculation engines **180**.
If one calculation engine is ready to accept a new job, the value will drop out of the queue, otherwise it will move right through slots **170** and eventually cycle back to the head of the queue.

Queue **160** will only accept new input from FIFO_K **150** when no data is looping around. Use of the ready/valid protocol makes the implementation of this feature relatively straightforward.

Calculation engines **180** will iterate the Mandelbrot set algorithm ("escape time" algorithm, see Wikipedia: https://en.wikipedia.org/wiki/Mandelbrot_set). 

With default settings (easily changed), the implementation uses 30 "julia" engines **180**. Each of them is formed by 12 pipeline levels. In other words, each engine juggles up to 12 independent calculations at a time.
Each "julia" engine performs three parallel multiplications (xx, yy, xy), using 90 multipliers in total, with one operation per cycle each under full load.

Since buffer space in RAM **220** downstream is fairly limited - much less than a full frame - the calculation engines **180** must be prevented from running too far ahead of the electron beam position. 
Therefore, a flow control mechanism **190** is built into the calculation engines. It checks each entry's pixel number against the electron beam and prevents it from leaving calculation engine **180** if it would cause cyclic overflow in RAM **220**.
If denied exit, the value will continue dummy iterations through the calculation engine. The clock domain crossing **110** delays the pixel position by a few clock cycles relative to the actual image generation, therefore flow control will always (conservatively) lag behind a few pixels.

Similar to the circular distribution queue **160**, results are collected into circular collection queue **190**. If a slot **200** is empty, it will accept a result from calculation engine **180**, otherwise the calculation engine will continue dummy iterations on the result.

Exiting data items from collection queue **190** move into FIFO_E **210** and then into dual port memory **220**. This FIFO is not strictly necessary anymore: Since dual port ram **220** will always be ready to accept data, it could be replaced with a cheaper register.

Dual port memory **220** is indexed on its second port by the electron beam position from "vga" block **100**. The dp-memory implements the crossing for data into the VGA pixel clock domain. 

RAM output values **230** represent a pixel's iteration count and are converted to RGB color signal **250** by color map RAM **240**. 

Its contents are configurable by CPU **140**. For color cycle animation, color map RAM **240** must be updated during the HSYNC period to not cause artifacts in a running frame (since there are no shadow registers).

The RGB color signal **250** is finally forwarded, together with HSYNC and VSYNC signals from vga generator **100** to the monitor output..

While not shown in the picture, buttons and LEDs are accessible via a register attached to the CPU's IO port.

### The "julia" calculation engine
To be continued

# Running the demo
* Get CMOD A7-35 board (or modify the project). The design is too large for the 15-size variant.
* upload the prebuilt "final" bitstream
* With the CPU running, the two buttons will switch the red and green LED, respectively.
* Wire up a monitor with jumper cables to the DIL48 socket

# Default pinout
*pin 1: RED (red wire in photos)
*pin 2: GREEN (green wire in photos)
*pin 3: BLUE (blue wire in photos)
*pin 4: HSYNC (yellow wire in photos)
*pin 5: VSYNC (orange wire in photos)
*pin 25 or PMOD header: common GND (black wire in photos)

Please note: 3.3 V is out-of-spec for VGA analog signals.
I've never run into issues with direct wiring on quite a few monitors / beamers but please do use common sense.

![Jumper wires to off-the-shelf VGA monitor cable](https://github.com/mnentwig/forthytwo/blob/master/fractalsProject/wwwSrc/pinout1.jpg "Jumper wires to off-the-shelf VGA monitor cable")
![Jumper wires (different view)](https://github.com/mnentwig/forthytwo/blob/master/fractalsProject/wwwSrc/pinout2.jpg "Jumper wires  (different view)")
![Jumper wire cabling, FPGA end](https://github.com/mnentwig/forthytwo/blob/master/fractalsProject/wwwSrc/pinout2.jpg "Jumper wire cabling, FPGA end")

If in doubt, please use a [standard VGA connector chart](http://www.hardwarebook.info/VGA_(15)) for reference ("at the monitor cable", not "at the video card").

To rebuild the bitstream, run from the top level directory 
- make forthytwo: builds the compiler
### For a "bootloader" version (to edit the microcontroller code)
* make fractals (still from the top level directory). This creates the main.v file with J1B rom contents
* open the Vivado project in fractalsProject/CMODA7_fractalDemo
* generate bitstream
* upload bitstream
* connect teraterm to the serial port. Test that a key press echos back an "x" => bootloader is functional
* send "fractalsProject/out/main.bootBin" via Teraterm's "send file" function in binary mode (!!). Note, it does not matter whether "main.bootBin" was compiled with bootloader enabled or disabled.
* correct upload prints one letter "1" and the buttons will light the LEDs. Only now the VGA signal is present

### For the "final" version
* edit bootloader/bootloader.txt according to the instructions in the first line (comment out first BRA: and uncomment second BRA:)
* from toplevel: make fractals
* Build bitstream from Vivado
* program bitstream (or write to flash). Once the yellow "prog ready" LED lights up, red/green LEDs should respond to button presses and the VGA signal is present.

