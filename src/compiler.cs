﻿using System;
using System.Collections.Generic;
using System.Text;

/// <summary>Takes preprocessed token stream. Renders binary data into internal memory structure for output with dumpHex()</summary>
class compiler {
    lstFileWriter_cl lstFileWriter = new lstFileWriter_cl();

    /// <summary>data memory labels (e.g. variables)</summary>
    Dictionary<string, UInt32> dataSymbols = new Dictionary<string, uint>();

    /// <summary>executable code memory labels (e.g. functions, branch targets)</summary>
    Dictionary<string, UInt32> codeSymbols = new Dictionary<string, uint>();

    /// <summary>macro definitions (text substitution)</summary>
    Dictionary<string, List<token>> macros = new Dictionary<string, List<token>>();

    /// <summary>generates J1 16-bit opcode from parameters</summary>
    static class opcode {
        public enum jumptype_e { JUMP = 0, COND = 0x2000, CALL = 0x4000 };
        public enum ALU_e { T = 0, N, T_plus_N, T_and_N, T_or_N, T_xor_N, not_T, N_equals_T, N_lts_T, N_rshift_T, N_lshift_T, R2T, mem2T, io2T, depth, N_ltu_T };
        public enum path_e { none = 0x0000, T2N = 0x0010, T2R = 0x0020, N2Tsto = 0x0030, N2Tio = 0x0040, ioRead = 0x0050 };

        public static UInt16 imm(UInt16 value) {
            if(value >= 0x8000) throw new Exception("imm value out of range");
            return (UInt16)(value | 0x8000);
        }

        public static UInt16 jump(UInt16 targetaddr, jumptype_e jumptype) {
            if((targetaddr > 0x1FFF)) throw new Exception("jump out of range");
            return (UInt16)(targetaddr | (int)jumptype);
        }

        public static UInt16 ALU(bool R2PC = false, ALU_e op = ALU_e.T, path_e path = path_e.none, bool doRet = false, int dRetStack = 0, int dDataStack = 0) {
            if((dRetStack < -2) || (dRetStack > 1) || (dDataStack < -2) || (dRetStack > 1)) throw new Exception("invalid stack depth change");
            return (UInt16)(
                0x6000
                | (R2PC ? 1 << 12 : 0)
                | ((int)op << 8)
                | (int)path
                | (doRet ? 1 << 7 : 0)
                | (int)(dRetStack & 0x3) << 2
                | (int)(dDataStack & 0x3)
                );
        }
    }

    static void generate_IMM(List<token> tokens, UInt32 val, token tRef) {
        string annotation = "push IMM value '"+util.hex8(val)+"'";
        // === TBD: use fetch ===
        // also in support of shift register reduction (32 bit barrel shifter is expensive...)

        if(val <= 0x7FFF) {
            tokens.Add(new token("core.imm"+util.hex4((UInt16)val), tRef, annotation));
        } else if(~val <= 0x7FFFF) {
            tokens.Add(new token("core.imm"+util.hex4((UInt16)~val), tRef, annotation));
            tokens.Add(new token("core.invert", tRef));
        } else {
            UInt32 hi = val >> 16;
            UInt32 lo = val & 0xFFFF;

            if((hi > 0x7FFF) && (lo > 0x7FFF)) {
                tokens.Add(new token("core.imm"+util.hex4((UInt16)~hi), tRef, annotation));
                tokens.Add(new token("core.imm"+util.hex4(16), tRef));
                tokens.Add(new token("core.lshift", tRef));    // 0:[~high,0000]
                tokens.Add(new token("core.imm"+util.hex4((UInt16)~lo), tRef));
                tokens.Add(new token("core.or", tRef));        // 0:[~high,~low]
                tokens.Add(new token("core.invert", tRef));    // 0: [high,low]
            } else if(hi > 0x7FFF) {
                tokens.Add(new token("core.imm"+util.hex4((UInt16)~hi), tRef, annotation));
                tokens.Add(new token("core.invert", tRef));
                tokens.Add(new token("core.imm"+util.hex4(16), tRef));
                tokens.Add(new token("core.lshift", tRef));    // 0: [high, 0000]
                tokens.Add(new token("core.imm"+util.hex4((UInt16)lo), tRef));
                tokens.Add(new token("core.or", tRef));
            } else if(lo > 0x7FFF) {
                // difficulty: cannot invert lo in 16 bits
                tokens.Add(new token("core.imm"+util.hex4((UInt16)hi), tRef, annotation));
                tokens.Add(new token("core.imm"+util.hex4((UInt16)16), tRef));
                tokens.Add(new token("core.lshift", tRef));
                tokens.Add(new token("core.invert", tRef));    // 0:[~high,FFFF]
                tokens.Add(new token("core.imm"+util.hex4((UInt16)~lo), tRef));     // 1:[~high,FFFF] 0: [0000,~low]
                tokens.Add(new token("core.invert", tRef));    // 1:[~high,FFFF] 0: [FFFF,low]
                tokens.Add(new token("core.xor", tRef));       // 0: [high,low]
            } else {
                if(hi != 0) {
                    tokens.Add(new token("core.imm"+util.hex4((UInt16)hi), tRef, annotation));
                    tokens.Add(new token("core.imm"+util.hex4(16), tRef));
                    tokens.Add(new token("core.lshift", tRef));
                }
                if((lo != 0) || (hi == 0)) {
                    tokens.Add(new token("core.imm"+util.hex4((UInt16)lo), tRef, annotation));
                }
                if(hi != 0)
                    tokens.Add(new token("core.xor", tRef));
            }
        }
    }

    /// <summary>compiler pass: replaces immediate values with assembly language creating the desired value</summary>
    /// <returns>modified token list</returns>
    List<token> pass_num2IMM(List<token> tokensIn) {
        List<token> tokensOut = new List<token>();
        foreach(token t in tokensIn) {
            UInt32 val;
            if(util.tryParseNum(t.body, out val))
                generate_IMM(tokensOut, val, t);
            else
                tokensOut.Add(t);
        }
        return tokensOut;
    }

    List<token> pass_extractMacros(List<token> tokensIn) {
        List<token> tokensOut = new List<token>();
        string activeMacroName = null;
        token activeMacroToken = null;
        foreach(token t in tokensIn) {
            if(t.body.StartsWith("::")) {
                if(activeMacroName != null) throw t.buildException("macro recursion is not allowed");
                activeMacroToken = t;
                activeMacroName = t.body.Substring(2); if(activeMacroName.Length == 0) throw t.buildException("missing macro name");
                if(util.tryParseNum(activeMacroName, out UInt32 dummy)) throw t.buildException("invalid macro name (must not be a number)");
                string prevDef = this.nameExists(activeMacroName);
                if(prevDef != null) throw t.buildException(":: name already exists as "+prevDef);
                this.macros[activeMacroName] = new List<token>();
            } else {
                if(activeMacroName != null) {

                    // do not add "return" to macros (execution continues past the macro end)
                    if(t.body != ";")
                        this.macros[activeMacroName].Add(t);
                } else {
                    tokensOut.Add(t);
                }

                if(t.body == ";") {
                    if(activeMacroName != null) {
                        activeMacroName = null;
                        activeMacroToken = null;
                    }
                }
            }
        }
        if(activeMacroName != null)
            throw activeMacroToken.buildException("open macro at end of input: >>>"+activeMacroName+"<<<");
        return tokensOut;
    }

    List<token> pass_substituteMacros(List<token> tokensIn) {
        bool change = true;
        List<token> tokensOut = null;
        while(change) {
            change = false;
            tokensOut = new List<token>();
            foreach(token t in tokensIn)
                if(this.macros.ContainsKey(t.body)) {
                    change = true;
                    string annotation = "'" + t.body + "' macro inclusion by " + t.getAnnotation();
                    string annot2 = "('" + t.body + "' cont'd) " + t.getAnnotation();
                    foreach(token tt in this.macros[t.body]) {
                        tokensOut.Add(new token(tSrc: tt, annotation: annotation));
                        annotation = annot2;
                    }
                } else
                    tokensOut.Add(t);
            tokensIn = tokensOut;
        }
        return tokensOut;
    }

    string nameExists(string n) {
        if(this.macros.ContainsKey(n)) return "macro";
        if(this.codeSymbols.ContainsKey(n)) return "code label";
        if(this.dataSymbols.ContainsKey(n)) return "data label";
        return null;
    }

    Dictionary<string, UInt16> opcodes = new Dictionary<string, ushort>();

    /// <summary>Start address of code in terms of 8-bit bytes</summary>
    UInt32 baseAddrCode_bytes;

    /// <summary>Start address of data in terms of 8-bit bytes</summary>
    UInt32 baseAddrData_bytes;

    /// <summary>Size of memory in terms of 8-bit bytes</summary>
    UInt32 memSize_bytes;

    UInt32 codeMemPtr;
    UInt32 dataMemPtr;
    UInt32[] mem;

    public compiler(List<token> tokens, UInt32 baseAddrCode_bytes, UInt32 baseAddrData_bytes, UInt32 memSize_bytes) {
        this.baseAddrCode_bytes = baseAddrCode_bytes;
        this.baseAddrData_bytes = baseAddrData_bytes;
        this.memSize_bytes = memSize_bytes;

        this.codeMemPtr = baseAddrCode_bytes / 2;   // CPU addresses 16-bit words
        this.dataMemPtr = baseAddrData_bytes;       // data mem is addressed in bytes (even though the J1 has no smaller access mode than 32 bits)
        this.mem = new uint[memSize_bytes/4];       // native BRAM is 32 bit

        // === all possible immediate load instructions ===
        for(UInt16 imm = 0; imm <= 0x7FFF; ++imm)
            this.opcodes["core.imm"+util.hex4(imm)] = opcode.imm(imm);

        // === all possible branch instructions ===
        for(UInt16 dest = 0; dest < 0x2000; dest += 1) {
            this.opcodes["core.call"+util.hex4(dest)] = opcode.jump(targetaddr: dest, jumptype: opcode.jumptype_e.CALL);
            this.opcodes["core.bz"+util.hex4(dest)] = opcode.jump(targetaddr: dest, jumptype: opcode.jumptype_e.COND);
            this.opcodes["core.bra"+util.hex4(dest)] = opcode.jump(targetaddr: dest, jumptype: opcode.jumptype_e.JUMP);
        }

        this.opcodes["core.noop"] = opcode.ALU();
        this.opcodes["core.plus"] = opcode.ALU(op: opcode.ALU_e.T_plus_N, dDataStack: -1);
        this.opcodes["core.xor"] = opcode.ALU(op: opcode.ALU_e.T_xor_N, dDataStack: -1);
        this.opcodes["core.and"] = opcode.ALU(op: opcode.ALU_e.T_and_N, dDataStack: -1);
        this.opcodes["core.or"] = opcode.ALU(op: opcode.ALU_e.T_or_N, dDataStack: -1);
        this.opcodes["core.invert"] = opcode.ALU(op: opcode.ALU_e.not_T);
        this.opcodes["core.equals"] = opcode.ALU(op: opcode.ALU_e.N_equals_T, dDataStack: -1);
        this.opcodes["core.lessThanSigned"] = opcode.ALU(op: opcode.ALU_e.N_lts_T, dDataStack: -1);
        this.opcodes["core.lessThanUnsigned"] = opcode.ALU(op: opcode.ALU_e.N_ltu_T, dDataStack: -1);
        this.opcodes["core.swap"] = opcode.ALU(op: opcode.ALU_e.N, path: opcode.path_e.T2N);
        this.opcodes["core.dup"] = opcode.ALU(op: opcode.ALU_e.T, path: opcode.path_e.T2N, dDataStack: 1);
        this.opcodes["core.drop"] = opcode.ALU(op: opcode.ALU_e.N, dDataStack: -1);
        this.opcodes["core.over"] = opcode.ALU(op: opcode.ALU_e.N, path: opcode.path_e.T2N, dDataStack: 1);
        this.opcodes["core.nip"] = opcode.ALU(op: opcode.ALU_e.T, dDataStack: -1);
        this.opcodes["core.pushR"] = opcode.ALU(op: opcode.ALU_e.N, path: opcode.path_e.T2R, dDataStack: -1, dRetStack: 1);
        this.opcodes["core.fetchR"] = opcode.ALU(op: opcode.ALU_e.R2T, path: opcode.path_e.T2N, dDataStack: 1);
        this.opcodes["core.popR"] = opcode.ALU(op: opcode.ALU_e.R2T, path: opcode.path_e.T2N, dDataStack: 1, dRetStack: -1);
        this.opcodes["core.fetch1"] = opcode.ALU(op: opcode.ALU_e.T);
        this.opcodes["core.fetch2"] = opcode.ALU(op: opcode.ALU_e.mem2T);
        this.opcodes["core.ioFetch1"] = opcode.ALU(op: opcode.ALU_e.T, path: opcode.path_e.ioRead);
        this.opcodes["core.ioFetch2"] = opcode.ALU(op: opcode.ALU_e.io2T);
        this.opcodes["core.sto1"] = opcode.ALU(op: opcode.ALU_e.T, path: opcode.path_e.N2Tsto, dDataStack: -1);
        this.opcodes["core.sto2"] = opcode.ALU(op: opcode.ALU_e.N, dDataStack: -1);
        this.opcodes["core.ioSto1"] = opcode.ALU(op: opcode.ALU_e.T, path: opcode.path_e.N2Tio, dDataStack: -1);
        this.opcodes["core.ioSto2"] = opcode.ALU(op: opcode.ALU_e.N, dDataStack: -1);
        this.opcodes["core.rshift"] = opcode.ALU(op: opcode.ALU_e.N_rshift_T, dDataStack: -1);
        this.opcodes["core.lshift"] = opcode.ALU(op: opcode.ALU_e.N_lshift_T, dDataStack: -1);
        this.opcodes["core.return"] = opcode.ALU(op: opcode.ALU_e.T, doRet: true, dRetStack: -1);

        // replace any literal numbers with instructions that push the value
        tokens = this.pass_num2IMM(tokens);

        // macro substitution before anything else (e.g. a library might wrap vars and funs into a macro)
        tokens = this.pass_extractMacros(tokens);
        tokens = this.pass_substituteMacros(tokens);

        this.renderBinary(tokens);

        // === check overlap ===
        UInt32 codeMemPtr_bytes = this.codeMemPtr * 2;   // CPU addresses 16-bit words
        UInt32 dataMemPtr_bytes = this.dataMemPtr;

        if(codeMemPtr_bytes > this.mem.Length) throw new Exception("code overruns physical memory");
        if(dataMemPtr_bytes > this.mem.Length) throw new Exception("code overruns physical memory");
        if(this.baseAddrCode_bytes < this.baseAddrData_bytes) {
            if(codeMemPtr_bytes > this.baseAddrData_bytes) throw new Exception("code segment overruns into data segment");
        } else
            if(dataMemPtr_bytes > this.baseAddrCode_bytes) throw new Exception("data segment overruns into code segment");
    }

    void codeToMem(UInt32 addr, string mnem, string annotation) {
        UInt16 val = this.opcodes[mnem];

        // code memory is native 32 bit but to the CPU it appears as 16 bit
        // with low-high multiplexed in RTL
        UInt32 wordAddr = addr >> 1;
        bool isLow = (addr & 1) == 0;
        if(isLow) {
            this.mem[wordAddr] &= 0xFFFF0000;
            this.mem[wordAddr] |= val;
        } else {
            this.mem[wordAddr] &= 0x0000FFFF;
            this.mem[wordAddr] |= (((UInt32)val) << 16);
        }

        this.lstFileWriter.annotateCode(addr, val, mnem, annotation);
    }

    void writeCode(string mnemonic, string annotation) {
        this.codeToMem(this.codeMemPtr, mnem: mnemonic, annotation: annotation);
        this.codeMemPtr += 1;
    }

    void writeData(UInt32 data, string token) {
        this.lstFileWriter.annotateData(this.dataMemPtr, data, token);
        this.mem[this.dataMemPtr >> 2] = data;
        this.dataMemPtr += 4;
    }

    class flowcontrol {
        public enum t_e {
            IF, ELSE,
            DO_STARTLABEL, DO_EXITBRANCH
        };
        public t_e t;
        public UInt32 addr;
        public token src;
    }

    class flowcontrol2 {
        public enum t_e { BRA, BZ, CALL };
        public t_e t;
        public UInt32 addr;
        public string label;
        public token src;
    }

    public void renderBinary(List<token> tokens) {
        // backannotation for flow control constructs
        Stack<flowcontrol> fc = new Stack<flowcontrol>();

        // backannotation for direct use of BRA, BZ, CALL
        HashSet<flowcontrol2> fcBranch = new HashSet<flowcontrol2>();
        for(int ix = 0; ix < tokens.Count; ++ix) {
            token tt = tokens[ix];
            string t = tt.body;

            if(t == "#EOF") {
                if(fc.Count > 0) {
                    flowcontrol a = fc.Peek();
                    throw a.src.buildException("dangling '"+a.src.body+"' at end of file");
                }
                continue;
            }

            if(t.StartsWith("BRA:")) {
                t = t.Substring(4);
                fcBranch.Add(new flowcontrol2() { t = flowcontrol2.t_e.BRA, addr = this.codeMemPtr, label = t, src = tt });
                string m = "core.bra"+util.hex4(/*will be updated in the 2nd pass */0);
                this.writeCode(mnemonic: m, annotation: tt.body + " " + tt.getAnnotation() + " ");
                continue;
            }

            if(t.StartsWith("BZ:")) {
                t = t.Substring(3);
                fcBranch.Add(new flowcontrol2() { t = flowcontrol2.t_e.BZ, addr = this.codeMemPtr, label = t, src = tt });
                string m = "core.bz"+util.hex4(/*will be updated in the 2nd pass */0);
                this.writeCode(mnemonic: m, annotation: tt.body + " " + tt.getAnnotation() + " ");
                continue;
            }

            if(t == "IF") {
                fc.Push(new flowcontrol() { t = flowcontrol.t_e.IF, addr = this.codeMemPtr, src = tt });
                string m = "core.bz"+util.hex4(/*will be updated when the destination address is reached*/0);
                this.writeCode(m, "'if-not' branch" + tt.getAnnotation() + " ");
                continue;
            }

            if(t == "ELSE") {
                if(fc.Count < 1) throw tt.buildException("'else' without 'if'");
                if(fc.Peek().t != flowcontrol.t_e.IF) throw new Exception("'else' without matching 'if'");
                fc.Push(new flowcontrol() { t = flowcontrol.t_e.ELSE, addr = this.codeMemPtr, src = tt });
                string m = "core.bra"+util.hex4(/*will be updated when the destination address is reached*/0);
                this.writeCode(m, "'skip-else' branch" + tt.getAnnotation() + " ");
                continue;
            }

            if(t == "ENDIF") {
                if(fc.Count < 1) throw tt.buildException("dangling endif");
                if(fc.Peek().t == flowcontrol.t_e.ELSE) {
                    // === modify the unconditional branch before "else" to jump to current address
                    string m1 = "core.bra"+util.hex4((UInt16)this.codeMemPtr);
                    this.codeToMem(fc.Peek().addr, m1, /*already annotated*/null);
                    this.lstFileWriter.annotateCodeLabel(this.codeMemPtr, "else bypass target");
                    fc.Pop();
                }

                if(fc.Count < 1) throw tt.buildException("dangling endif");
                if(fc.Peek().t != flowcontrol.t_e.IF) throw tt.buildException("'endif' without matching 'if'");
                // === modify the "if-not" conditional branch to jump to current address
                string m2 = "core.bz"+util.hex4((UInt16)this.codeMemPtr);
                this.codeToMem(fc.Peek().addr, m2, /*already annotated */null);
                this.lstFileWriter.annotateCodeLabel(this.codeMemPtr, "if (not) target");
                fc.Pop();
                continue;
            }

            if(t == "DO") {
                this.writeCode("core.pushR", "'__DO__' push limit to R " + tt.getAnnotation() + " ");

                this.lstFileWriter.annotateCodeLabel(this.codeMemPtr, "DO loop comparison");
                fc.Push(new flowcontrol() { t = flowcontrol.t_e.DO_STARTLABEL, addr = this.codeMemPtr, src = tt });
                this.writeCode("core.dup", "'DO' duplicate counter " + tt.getAnnotation() + " ");
                this.writeCode("core.fetchR", "'DO' get copy of limit " + tt.getAnnotation() + " ");
                this.writeCode("core.lessThanSigned", "'DO' comparison " + tt.getAnnotation() + " ");

                fc.Push(new flowcontrol() { t = flowcontrol.t_e.DO_EXITBRANCH, addr = this.codeMemPtr, src = tt });
                this.writeCode("core.bz"+util.hex4(/* to be updated later */0), "'DO' comparison " + tt.getAnnotation() + " ");
                continue;
            }

            if(t == "LOOP") {
                if(fc.Count < 2) throw tt.buildException("dangling LOOP");
                flowcontrol exitBranch = fc.Pop(); if(exitBranch.t != flowcontrol.t_e.DO_EXITBRANCH) throw tt.buildException("LOOP without DO (check for open flow control e.g. IF without ENDIF)");
                flowcontrol startLabel = fc.Pop(); if(startLabel.t != flowcontrol.t_e.DO_STARTLABEL) throw tt.buildException("LOOP internal error");

                // === increase counter ===
                this.writeCode("core.imm"+util.hex4((UInt16)1), "'DO' counter increment value" + tt.getAnnotation() + " ");
                this.writeCode("core.plus", "'DO' counter increment " + tt.getAnnotation() + " ");

                // === jump back to start of loop ===
                this.writeCode("core.bra"+util.hex4((UInt16)startLabel.addr), "'DO' jump backwards to comparison " + tt.getAnnotation() + " ");

                // === update the exit branch target to point here ===
                this.lstFileWriter.annotateCodeLabel(this.codeMemPtr, "DO exit condition target");
                string m = "core.bz"+util.hex4((UInt16)this.codeMemPtr);
                this.codeToMem(exitBranch.addr, m, /*already annotated */null);
                this.writeCode("core.drop", "'DO' clean up loop variable"); // 
                this.writeCode("core.popR", "'DO' clean up saved limit from rstack");
                this.writeCode("core.drop", "'__DO__' last instruction: clean up saved limit from stack");
                continue;
            }

            if(t.StartsWith("VAR:")) {
                string t2 = t.Substring(4);
                string[] t3 = t2.Split('=');
                if(t3.Length != 2) throw tt.buildException("var: invalid syntax, expecting var:name:value");
                string n = t3[0];
                if(!util.tryParseNum(t3[1], out uint val)) throw tt.buildException("var: invalid syntax. Failed to parse value in var:name:value");
                string prevDef = this.nameExists(n);
                if(prevDef != null) throw tt.buildException("var: name already exists as "+prevDef);
                this.dataSymbols[n] = this.dataMemPtr;
                this.writeData(val, t);
                continue;
            }

            if(t.StartsWith(":")) {
                // === code label ===
                this.lstFileWriter.annotateCodeLabel(this.codeMemPtr, t);
                t = t.Substring(1); // drop leading colon from name
                if(this.nameExists(t) != null) throw tt.buildException("label has already been defined as " + this.nameExists(t));
                this.codeSymbols[t] = this.codeMemPtr;
                continue;
            }

            // === direct use of code label => CALL ===
            if(this.codeSymbols.ContainsKey(t)) {
                string mnem = "core.CALL"+util.hex4((UInt16)this.codeSymbols[t]);
                this.writeCode(mnem, "call "+tt.getAnnotation());
                continue;
            }

            // === load address of variable ===
            if(this.dataSymbols.ContainsKey(t)) {
                string mnem = "core.imm"+util.hex4((UInt16)this.dataSymbols[t]);
                this.writeCode(mnem, "data addr: '"+t+"'"+tt.getAnnotation());
                continue;
            }

            // === Mnemonic to numerical opcode ===
            if(this.opcodes.ContainsKey(t)) {
                this.writeCode(t, tt.getAnnotation());
                continue;
            }

            if(t.StartsWith("CALL:"))
                t = t.Substring(5);

            fcBranch.Add(new flowcontrol2() { t = flowcontrol2.t_e.CALL, addr = this.codeMemPtr, label = t, src = tt });
            string m5 = "core.call"+util.hex4(/*will be updated in the 2nd pass */0);
            this.writeCode(mnemonic: m5, annotation: tt.body + " " + tt.getAnnotation() + " ");
        }

        // === update (possibly) backwards branches)
        foreach(flowcontrol2 f in fcBranch) {
            string m = null;
            if(!this.codeSymbols.ContainsKey(f.label))
                throw f.src.buildException("label '"+f.label+"' not found (must be defined using : )");
            string aHex = util.hex4((UInt16)this.codeSymbols[f.label]);
            switch(f.t) {
                case flowcontrol2.t_e.BRA:
                    m = "core.bra"+aHex; break;
                case flowcontrol2.t_e.BZ:
                    m = "core.bz"+aHex; break;
                case flowcontrol2.t_e.CALL:
                    m = "core.call"+aHex; break;
            }
            this.codeToMem(f.addr, m, /*already annotated */null);
        }
    }

    public void dumpHex(string filename) {
        StringBuilder sb = new StringBuilder();
        for(int ix = 0; ix < this.mem.Length; ++ix) {
            sb.AppendLine(util.hex8(this.mem[ix]));
        }
        System.IO.File.WriteAllText(filename, sb.ToString());
    }

    public void dumpLst(string filename) {
        this.lstFileWriter.dumpLst(filename);
    }
}