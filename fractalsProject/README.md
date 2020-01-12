# FPGA fractal demo
Markus Nentwig, 20?? - 2020

Fractal generation is a popular FPGA design challenge. 
For example, Stanford university 
https://web.stanford.edu/class/ee183/handouts/lect7.pdf
used it as lab exercise in 2002. I also remember the demo from some trade fair around that time and had the itch since then try my hands on it.

Well, I got around to it eventually. 

It took a long time.

## Motivation
An FPGA "fun" project adds one very unique design challenge: It needs to be sufficiently fun all along the way. 

Sometimes, you are your own worst enemy. After a long, tedious climb, you finally see a straight, obvious, logical route all the way downhill to the finish. 
And decide not to take it because it's too dull. Same as with hiking, it's not about getting from A to B in the most efficient manner. 
So you throw in some new ideas to make it more interesting. Repeat too many times.

Chasing the "fun factor" eventually evolved into unwritten requirements somewhere along those lines:

* Real time calculation: The Stanford lab exercise demanded it already in 2002.
* Full HD resolution (1920x1080) at 60 Hz. That's the monitor on my desk. Period.
* Use the FPGA in a sensible manner. The resulting implementation can achieve multiplier utilization close to 100 %, that's 18 billion multiplications per second (on 35-size Artix and an USB bus power budget of ~2 Watts).	
* Perform dynamic resource allocation. The fractals algorithm is somewhat unusual as the required number of iterations varies between points. Compared to a fixed number of iterations, complexity increases substantially (a random number of results may appear in one clock cycle, results are unordered) but so does performance.
* Limit to 18-bit multiplications because it is the native size for DSP48 blocks. It is straightforward to increase the internal bitwidth for higher resolution, but resource usage skyrockets.
* Be (reasonably) vendor-independent. Being halfway through a microblaze MCS implementation for the controls, the fun factor dropped below threshold so I abandoned it.
* Realistic CPU size: For a CPU-centric industrial (not "fun") design I see no alternative to the vendor's proprietary CPU offerings e.g. Microblaze or Zynq ARM. However, for a minimal controller I expect that it is small even if it makes no real difference on the large Xilinx FPGA - I may want to use the same technology on lower-tier FPGAs e.g. Lattice, and then CPU size and efficiency becomes more critical. James Bowman's J1B fits the bill for me, and its 32 bit extension is used heavily in this project.
* Reaching the conclusion (personal opinion!) that J1B's native gforth toolchain would never be accepted for a state-of-the-art industrial project, I ended up writing my own simple compiler / assembler "forthytwo.exe". This may seem over the top at first glance but it paves the way for e.g. floating-point math in hand-crafted assembler with manageable effort. I've seen ad-hoc compilers for large-volume ASIC custom microcontrollers based on Excel spreadsheets - maybe spinning your own compiler is not _that_ absurd, after all. I like it very much.
* Floating point math for the controls: Fixed point is great for raw throughput but gets tedious for performance-uncritical code.
* CPU Bootloader on plain UART (meaning no proprietary Xilinx JTAG). The included bootloader implements robust synchronization and efficient binary upload.
* No esoteric tools, not relying on Linux. On a clean Windows PC, the build system can be set up by installing MinGW (developer settings), Vivado and Verilator. See my install notes for the latter. Use e.g. Teraterm with the bootloader.
* Batteries-included project so you can pull it out of the hat and reuse it by deleting what is not needed (this more on the microcontroller side, as the fractals part is quite problem-specific)

## Ready/valid design pattern notes
The calculation engine relies heavily on the valid/ready handshaking paradigm, which is used consistently throughout the chain.

### Ready / valid combinational path problem
In a typical processing block, data moves in lock-step through a sequence of registers. 
When cascading any number of such blocks, via ready-/valid interfaces, the "ready" signal forms a combinational path from the end to the beginning of the chain. This can cause problems with timing closure.
The problem is clear to see when considering what happens when the end of the processing chain signals "not ready" (to accept data). 
The data over the whole length of the pipeline has nowhere to go, therefore the whole chain must be stopped within a single clock cycle.

The solution is to break the chain into multiple segments using FIFOs (2 slots is enough).
There's a catch: I could design an "optimized" FIFO that will accept data even if full, when an element is taken from the output in the same clock cycle.
This "optimization" would introduce exactly the combinational path the FIFO is supposed to break, thus it would be useless for decoupling the combinational chain.
In other words, the input of the FIFO may not use the output-side "ready" signal combinationally.

### Ready / valid flow control
The data flow in a ready-/valid chain can be interrupted at any point simply by inserting a block that forces both valid and ready signal to zero.
This block may be combinational and may depend on an observed data value. 
This pattern is used to stop the calculation engine from running too far ahead of the monitor's electron beam.

(to be continued)