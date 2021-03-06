## This file is a general .xdc for the CmodA7 rev. B
## To use it in a project:
## - uncomment the lines corresponding to used pins
## - rename the used ports (in each line, after get_ports) according to the top level signal names in the project
set_property BITSTREAM.CONFIG.UNUSEDPIN PULLDOWN [current_design]
set_property BITSTREAM.GENERAL.COMPRESS FALSE [current_design]

# Clock signal 12 MHz
set_property -dict {PACKAGE_PIN L17 IOSTANDARD LVCMOS33} [get_ports CLK12]

# UART
set_property -dict {PACKAGE_PIN J18 IOSTANDARD LVCMOS33} [get_ports uart_rxd_out]
set_false_path -to [get_ports uart_rxd_out]
set_property -dict {PACKAGE_PIN J17 IOSTANDARD LVCMOS33} [get_ports uart_txd_in]
set_false_path -from [get_ports uart_txd_in]

# LEDs
set_property -dict {PACKAGE_PIN A17 IOSTANDARD LVCMOS33} [get_ports {LED[0]}]
set_property -dict {PACKAGE_PIN C16 IOSTANDARD LVCMOS33} [get_ports {LED[1]}]
set_false_path -to [get_ports {LED[*]}]

set_property CFGBVS VCCO [current_design]
set_property CONFIG_VOLTAGE 3.3 [current_design]

set_property -dict {PACKAGE_PIN C17 IOSTANDARD LVCMOS33} [get_ports {RGBLED[0]}]
set_property -dict {PACKAGE_PIN B16 IOSTANDARD LVCMOS33} [get_ports {RGBLED[1]}]
set_property -dict {PACKAGE_PIN B17 IOSTANDARD LVCMOS33} [get_ports {RGBLED[2]}]
set_false_path -to [get_ports {RGBLED[*]}]

# Buttons
set_property -dict {PACKAGE_PIN A18 IOSTANDARD LVCMOS33} [get_ports {BTN[0]}]
set_property -dict {PACKAGE_PIN B18 IOSTANDARD LVCMOS33} [get_ports {BTN[1]}]
set_false_path -from [get_ports {BTN[*]}]

## Pmod Header PMOD
set_property PACKAGE_PIN G17 [get_ports {PMOD[0]}]
set_property PACKAGE_PIN G19 [get_ports {PMOD[1]}]
set_property PACKAGE_PIN N18 [get_ports {PMOD[2]}]
set_property PACKAGE_PIN L18 [get_ports {PMOD[3]}]
set_property PACKAGE_PIN H17 [get_ports {PMOD[4]}]
set_property PACKAGE_PIN H19 [get_ports {PMOD[5]}]
set_property PACKAGE_PIN J19 [get_ports {PMOD[6]}]
set_property PACKAGE_PIN K18 [get_ports {PMOD[7]}]
set_property DRIVE 4 [get_ports {PMOD[*]}]
set_property SLEW SLOW [get_ports {PMOD[*]}]
set_property IOSTANDARD LVTTL [get_ports {PMOD[*]}]

## Analog XADC Pins
#set_property -dict {PACKAGE_PIN G2 IOSTANDARD LVCMOS33} [get_ports {xa_n[0]}]
#set_property -dict {PACKAGE_PIN G3 IOSTANDARD LVCMOS33} [get_ports {xa_p[0]}]
#set_property -dict {PACKAGE_PIN J2 IOSTANDARD LVCMOS33} [get_ports {xa_n[1]}]
#set_property -dict {PACKAGE_PIN H2 IOSTANDARD LVCMOS33} [get_ports {xa_p[1]}]

## GPIO Pins
## Pins 15 and 16 should remain commented if using them as analog inputs
set_property PACKAGE_PIN M3 [get_ports {pioA[1]}]
set_property IOSTANDARD LVCMOS33 [get_ports {pioA[1]}]
set_property DRIVE 4 [get_ports {pioA[1]}]
set_property PULLDOWN true [get_ports {pioA[1]}]
set_property SLEW SLOW [get_ports {pioA[1]}]

set_property PACKAGE_PIN L3 [get_ports {pioA[2]}]
set_property IOSTANDARD LVCMOS33 [get_ports {pioA[2]}]
set_property PULLDOWN true [get_ports {pioA[2]}]
set_property DRIVE 4 [get_ports {pioA[2]}]
set_property SLEW SLOW [get_ports {pioA[2]}]

set_property PACKAGE_PIN A16 [get_ports {pioA[3]}]
set_property IOSTANDARD LVCMOS33 [get_ports {pioA[3]}]
set_property PULLDOWN true [get_ports {pioA[3]}]
set_property DRIVE 4 [get_ports {pioA[3]}]
set_property SLEW SLOW [get_ports {pioA[3]}]

set_property PACKAGE_PIN K3 [get_ports {pioA[4]}]
set_property IOSTANDARD LVCMOS33 [get_ports {pioA[4]}]
set_property PULLDOWN true [get_ports {pioA[4]}]
set_property DRIVE 4 [get_ports {pioA[4]}]
set_property SLEW SLOW [get_ports {pioA[4]}]

set_property PACKAGE_PIN C15 [get_ports {pioA[5]}]
set_property IOSTANDARD LVCMOS33 [get_ports {pioA[5]}]
set_property PULLDOWN true [get_ports {pioA[5]}]
set_property DRIVE 4 [get_ports {pioA[5]}]
set_property SLEW SLOW [get_ports {pioA[5]}]

#set_property PACKAGE_PIN H1 [get_ports {pioA[6]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioA[6]}]
#set_property PULLDOWN true [get_ports {pioA[6]}]
#set_property PACKAGE_PIN A15 [get_ports {pioA[7]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioA[7]}]
#set_property PULLDOWN true [get_ports {pioA[7]}]
#set_property PACKAGE_PIN B15 [get_ports {pioA[8]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioA[8]}]
#set_property PULLDOWN true [get_ports {pioA[8]}]
#set_property PACKAGE_PIN A14 [get_ports {pioA[9]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioA[9]}]
#set_property PULLDOWN true [get_ports {pioA[9]}]
#set_property PACKAGE_PIN J3 [get_ports {pioA[10]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioA[10]}]
#set_property PULLDOWN true [get_ports {pioA[10]}]
#set_property PACKAGE_PIN J1 [get_ports {pioA[11]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioA[11]}]
#set_property PULLDOWN true [get_ports {pioA[11]}]
#set_property PACKAGE_PIN K2 [get_ports {pioA[12]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioA[12]}]
#set_property PULLDOWN true [get_ports {pioA[12]}]
#set_property PACKAGE_PIN L1 [get_ports {pioA[13]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioA[13]}]
#set_property PULLDOWN true [get_ports {pioA[13]}]
#set_property PACKAGE_PIN L2 [get_ports {pioA[14]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioA[14]}]
#set_property PULLDOWN true [get_ports {pioA[14]}]
#set_property PACKAGE_PIN M1 [get_ports {pioB[17]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioB[17]}]
#set_property PULLDOWN true [get_ports {pioB[17]}]
#set_property PACKAGE_PIN N3 [get_ports {pioB[18]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioB[18]}]
#set_property PULLDOWN true [get_ports {pioB[18]}]
#set_property PACKAGE_PIN P3 [get_ports {pioB[19]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioB[19]}]
#set_property PULLDOWN true [get_ports {pioB[19]}]
#set_property PACKAGE_PIN M2 [get_ports {pioB[20]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioB[20]}]
#set_property PULLDOWN true [get_ports {pioB[20]}]
#set_property PACKAGE_PIN N1 [get_ports {pioB[21]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioB[21]}]
#set_property PULLDOWN true [get_ports {pioB[21]}]
#set_property PACKAGE_PIN N2 [get_ports {pioB[22]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioB[22]}]
#set_property PULLDOWN true [get_ports {pioB[22]}]
#set_property PACKAGE_PIN P1 [get_ports {pioB[23]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioB[23]}]
#set_property PULLDOWN true [get_ports {pioB[23]}]
#set_property PACKAGE_PIN R3 [get_ports {pioC[26]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[26]}]
#set_property PULLDOWN true [get_ports {pioC[26]}]
#set_property PACKAGE_PIN T3 [get_ports {pioC[27]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[27]}]
#set_property PULLDOWN true [get_ports {pioC[27]}]
#set_property PACKAGE_PIN R2 [get_ports {pioC[28]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[28]}]
#set_property PULLDOWN true [get_ports {pioC[28]}]
#set_property PACKAGE_PIN T1 [get_ports {pioC[29]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[29]}]
#set_property PULLDOWN true [get_ports {pioC[29]}]
#set_property PACKAGE_PIN T2 [get_ports {pioC[30]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[30]}]
#set_property PULLDOWN true [get_ports {pioC[30]}]
#set_property PACKAGE_PIN U1 [get_ports {pioC[31]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[31]}]
#set_property PULLDOWN true [get_ports {pioC[31]}]
#set_property PACKAGE_PIN W2 [get_ports {pioC[32]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[32]}]
#set_property PULLDOWN true [get_ports {pioC[32]}]
#set_property PACKAGE_PIN V2 [get_ports {pioC[33]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[33]}]
#set_property PULLDOWN true [get_ports {pioC[33]}]
#set_property PACKAGE_PIN W3 [get_ports {pioC[34]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[34]}]
#set_property PULLDOWN true [get_ports {pioC[34]}]
#set_property PACKAGE_PIN V3 [get_ports {pioC[35]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[35]}]
#set_property PULLDOWN true [get_ports {pioC[35]}]
#set_property PACKAGE_PIN W5 [get_ports {pioC[36]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[36]}]
#set_property PULLDOWN true [get_ports {pioC[36]}]
#set_property PACKAGE_PIN V4 [get_ports {pioC[37]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[37]}]
#set_property PULLDOWN true [get_ports {pioC[37]}]
#set_property PACKAGE_PIN U4 [get_ports {pioC[38]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[38]}]
#set_property PULLDOWN true [get_ports {pioC[38]}]
#set_property PACKAGE_PIN V5 [get_ports {pioC[39]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[39]}]
#set_property PULLDOWN true [get_ports {pioC[39]}]
#set_property PACKAGE_PIN W4 [get_ports {pioC[40]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[40]}]
#set_property PULLDOWN true [get_ports {pioC[40]}]
#set_property PACKAGE_PIN U5 [get_ports {pioC[41]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[41]}]
#set_property PULLDOWN true [get_ports {pioC[41]}]
#set_property PACKAGE_PIN U2 [get_ports {pioC[42]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[42]}]
#set_property PULLDOWN true [get_ports {pioC[42]}]
#set_property PACKAGE_PIN W6 [get_ports {pioC[43]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[43]}]
#set_property PULLDOWN true [get_ports {pioC[43]}]
#set_property PACKAGE_PIN U3 [get_ports {pioC[44]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[44]}]
#set_property PULLDOWN true [get_ports {pioC[44]}]
#set_property PACKAGE_PIN U7 [get_ports {pioC[45]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[45]}]
#set_property PULLDOWN true [get_ports {pioC[45]}]
#set_property PACKAGE_PIN W7 [get_ports {pioC[46]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[46]}]
#set_property PULLDOWN true [get_ports {pioC[46]}]
#set_property PACKAGE_PIN U8 [get_ports {pioC[47]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[47]}]
#set_property PULLDOWN true [get_ports {pioC[47]}]
#set_property PACKAGE_PIN V8 [get_ports {pioC[48]}]
#set_property IOSTANDARD LVCMOS33 [get_ports {pioC[48]}]
#set_property PULLDOWN true [get_ports {pioC[48]}]


# drive the DAC outputs with maximum strength (those are protected with resistors)
#set_property DRIVE 24 [get_ports {ja[*]}]
#set_property SLEW FAST [get_ports {ja[*]}]
#set_property IOSTANDARD LVTTL [get_ports {ja[*]}]
#set_property PULLDOWN true [get_ports {ja[0]}]
#set_property PULLDOWN true [get_ports {ja[1]}]
#set_property PULLDOWN true [get_ports {ja[2]}]
#set_property PULLDOWN true [get_ports {ja[3]}]
#set_property PULLDOWN true [get_ports {ja[4]}]
#set_property PULLDOWN true [get_ports {ja[5]}]
#set_property PULLDOWN true [get_ports {ja[6]}]
#set_property PULLDOWN true [get_ports {ja[7]}]

# set asynchronous outputs as don't-care
#set_false_path -to [get_ports LED]
#set_false_path -to [get_ports RGB0_Red]
#set_false_path -to [get_ports RGB0_Green]
#set_false_path -to [get_ports RGB0_Blue]
#set_false_path -to [get_ports {ja[*]}]
set_false_path -to [get_ports {pioA[*]}]
set_false_path -to [get_ports {PMOD[*]}]

#create_clock -period 8.000 -name VIRTUAL_clk_out1_clkMul -waveform {0.000 4.000}
#set_input_delay -clock [get_clocks VIRTUAL_clk_out1_clkMul] -min -add_delay 0.200 [get_ports {BTN[*]}]
#set_input_delay -clock [get_clocks VIRTUAL_clk_out1_clkMul] -max -add_delay 1.200 [get_ports {BTN[*]}]
#set_input_delay -clock [get_clocks VIRTUAL_clk_out1_clkMul] -min -add_delay 0.200 [get_ports {ja[*]}]
#set_input_delay -clock [get_clocks VIRTUAL_clk_out1_clkMul] -max -add_delay 1.200 [get_ports {ja[*]}]
#set_input_delay -clock [get_clocks VIRTUAL_clk_out1_clkMul] -min -add_delay 0.200 [get_ports {pioA[*]}]
#set_input_delay -clock [get_clocks VIRTUAL_clk_out1_clkMul] -max -add_delay 1.200 [get_ports {pioA[*]}]
#set_input_delay -clock [get_clocks VIRTUAL_clk_out1_clkMul] -min -add_delay 0.200 [get_ports {pioB[*]}]
#set_input_delay -clock [get_clocks VIRTUAL_clk_out1_clkMul] -max -add_delay 1.200 [get_ports {pioB[*]}]
#set_input_delay -clock [get_clocks VIRTUAL_clk_out1_clkMul] -min -add_delay 0.200 [get_ports {pioC[*]}]
#set_input_delay -clock [get_clocks VIRTUAL_clk_out1_clkMul] -max -add_delay 1.200 [get_ports {pioC[*]}]

# image generation and VGA are fully independent clocks
set_false_path -from [get_clocks -of_objects [get_nets clk200]] -to [get_clocks -of_objects [get_nets vgaClk]]
set_false_path -from [get_clocks -of_objects [get_nets vgaClk]] -to [get_clocks -of_objects [get_nets clk200]]

# CPU access will have many cycles margin
set_false_path -from [get_clocks -of_objects [get_nets clk100]] -to [get_clocks -of_objects [get_nets clk200]]
set_false_path -from [get_clocks -of_objects [get_nets clk200]] -to [get_clocks -of_objects [get_nets clk100]]

set_property BITSTREAM.GENERAL.CRC DISABLE [current_design]
set_property CONFIG_MODE SPIx4 [current_design]


set_false_path -from [get_clocks -of_objects [get_pins iClk1/inst/mmcm_adv_inst/CLKOUT0]] -to [get_clocks -of_objects [get_pins iClk2/inst/plle2_adv_inst/CLKOUT0]]
set_false_path -from [get_clocks -of_objects [get_pins iClk2/inst/plle2_adv_inst/CLKOUT0]] -to [get_clocks -of_objects [get_pins iClk1/inst/mmcm_adv_inst/CLKOUT0]]
