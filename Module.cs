// MIT License
// 
// Copyright (c) 2021 Pixel Precision, LLC
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections;
using System.Collections.Generic;

namespace PxPre.WASM
{
    // TODO: Look into the name. This may not be the best classname to describe its
    // purpose and to differentiate itself from imported modules.
    public class Module
    {
        // TODO: Remove elsewhere - possibly into Bin?
        public enum ImportType
        {
            // https://webassembly.github.io/spec/core/binary/modules.html#import-section
            TypeIndex   = 0x00, // A function type index
            TableType   = 0x01, // A table type
            MemType     = 0x02, // A mem type
            GlobalType  = 0x03  // A global type 
        }

        public const uint UnloadedStartIndex = unchecked((uint)~0);

        public List<FunctionType> types = new List<FunctionType>();

        public List<Export> exports = new List<Export>();

        public List<Function> functions = new List<Function>();
        
        public StoreDeclarations storeDecl;


        public uint startFnIndex = UnloadedStartIndex;

        protected Module()
        { 
            this.storeDecl = new StoreDeclarations(this);
        }

        unsafe public static Module LoadBinary(byte [] rb)
        {
            fixed (byte * pb = rb)
            { 
                uint idx = 0;
                return LoadBinary(pb, ref idx, rb.Length);
            }
        }

        unsafe public static Module LoadBinary(byte * pb, ref uint idx, int endIdx)
        {
            // https://www.reddit.com/r/WebAssembly/comments/9vq019/is_anyone_learning_webassembly_in_binary_im_stuck/
            // https://webassembly.github.io/wabt/demo/wat2wasm/

            if (*((int*)(&pb[idx])) != BinParse.WASM_BINARY_MAGIC)
                return null;

            idx += 4;

            if(*(int*)&pb[idx] != BinParse.WASM_BINARY_VERSION)
                return null;

            idx += 4;

            Module ret = new Module();

            while(idx < endIdx)
            {
                Bin.Section sectionCode = (Bin.Section)pb[idx];
                ++idx;

                uint sectionSize = BinParse.LoadUnsignedLEB32(pb, ref idx);

                if(sectionCode == Bin.Section.CustomSec)
                { 
                    uint end = idx + sectionSize;

                    while(true)
                    { 
                        return ret;

                        // TODO: Considered end and unprocessed for now.
                        //
                        // uint nameLen = LoadUnsignedLEB32(pb, ref idx);
                        // string customSecName = LoadString(pb, nameLen, ref idx);
                        // 
                        // uint subType = LoadUnsignedLEB32(pb, ref idx);
                        // uint subSize = LoadUnsignedLEB32(pb, ref idx);
                    }
                }
                else if(sectionCode == Bin.Section.TypeSec)
                { 
                    uint numTypes = BinParse.LoadUnsignedLEB32(pb, ref idx);
                    for(uint i = 0; i < numTypes; ++i)
                    {
                        Bin.TypeID type = (Bin.TypeID)BinParse.LoadUnsignedLEB32(pb, ref idx);
                        if(type == Bin.TypeID.Function)
                        {
                            FunctionType fty = new FunctionType();
                            fty.typeid = (uint)type;
                            ret.types.Add(fty);

                            uint numParams = BinParse.LoadUnsignedLEB32(pb, ref idx);
                            for(uint j = 0; j < numParams; ++j)
                            {
                                FunctionType.DataOrgInfo paramInfo = new FunctionType.DataOrgInfo();
                                paramInfo.type = (Bin.TypeID)BinParse.LoadUnsignedLEB32(pb, ref idx);
                                fty.paramTypes.Add(paramInfo);
                            }

                            uint numResults = BinParse.LoadUnsignedLEB32(pb, ref idx);
                            for(uint j = 0; j < numResults; ++j)
                            {
                                FunctionType.DataOrgInfo resultInfo = new FunctionType.DataOrgInfo();
                                resultInfo.type = (Bin.TypeID)BinParse.LoadUnsignedLEB32(pb, ref idx);
                                fty.resultTypes.Add(resultInfo);
                            }

                            fty.InitializeOrganization();
                        }
                        else
                        { 
                        }
                    }
                }
                else if(sectionCode == Bin.Section.ImportSec)
                { 
                    uint numImports = BinParse.LoadUnsignedLEB32(pb, ref idx);

                    for(uint i = 0; i < numImports; ++i)
                    { 
                        uint modnameLen = BinParse.LoadUnsignedLEB32(pb, ref idx);
                        string modName = LoadString(pb, modnameLen, ref idx);

                        uint fieldnameLen = BinParse.LoadUnsignedLEB32(pb, ref idx);
                        string fieldName = LoadString(pb, fieldnameLen, ref idx);

                        ImportType importTy = (ImportType)BinParse.LoadUnsignedLEB32(pb, ref idx);
                        switch(importTy)
                        {
                            case ImportType.TypeIndex: 
                                {
                                    uint fnTyIdx = BinParse.LoadUnsignedLEB32(pb, ref idx);
                                    FunctionType fnTy = ret.types[(int)fnTyIdx];
                                    ret.storeDecl.AddFunctionImp(modName, fieldName, fnTy);
                                }
                                break;

                            case ImportType.TableType:
                                {
                                    const int FLAG_HASMAX = 0x01;
                                    const int FLAG_OTHERS = ~(FLAG_HASMAX);

                                    // We may be able top unify parts of this code with non-imported tables
                                    uint type       = BinParse.LoadUnsignedLEB32(pb, ref idx);
                                    uint flags      = BinParse.LoadUnsignedLEB32(pb, ref idx);
                                    uint initial    = BinParse.LoadUnsignedLEB32(pb, ref idx);
                                    uint ? max      = null;

                                    if((flags & FLAG_HASMAX) != 0)
                                        max = BinParse.LoadUnsignedLEB32(pb, ref idx);

                                    if((flags & FLAG_OTHERS) != 0)
                                        throw new System.Exception("Encountered unknown flags for imported table.");

                                    ret.storeDecl.AddTableImp(modName, fieldName, (Bin.TypeID)type, initial, max);
                                }
                                break;

                            case ImportType.MemType:
                                {
                                    const int FLAG_HASMAX = 0x01;
                                    const int FLAG_OTHERS = ~(FLAG_HASMAX);

                                    // We may be able to unify parts of this code with non-imported memory
                                    uint flags      = BinParse.LoadUnsignedLEB32(pb, ref idx);
                                    uint initial    = BinParse.LoadUnsignedLEB32(pb, ref idx);
                                    uint ? max      = null;

                                    if((flags & FLAG_HASMAX) != 0)
                                        max = BinParse.LoadUnsignedLEB32(pb, ref idx);

                                    if ((flags & FLAG_OTHERS) != 0)
                                        throw new System.Exception("Encountered unknown flags for imported memory.");

                                    ret.storeDecl.AddMemoryImp(modName, fieldName, initial, initial, max);
                                }
                                break;

                            case ImportType.GlobalType:
                                {
                                    uint type = BinParse.LoadUnsignedLEB32(pb, ref idx);
                                    uint mutability = BinParse.LoadUnsignedLEB32(pb, ref idx);

                                    ret.storeDecl.AddGlobalImp(modName, fieldName, (Bin.TypeID)type, mutability != 0);
                                }
                                break;
                        }

                        
                    }
                }
                else if(sectionCode == Bin.Section.FunctionSec)
                {
                    uint numFunctions = BinParse.LoadUnsignedLEB32(pb, ref idx);
                    for (uint i = 0; i < numFunctions; ++i)
                    {
                        Function function = new Function(ret);
                        uint fnType = BinParse.LoadUnsignedLEB32(pb, ref idx);
                        function.typeidx = fnType;
                        function.fnType = ret.types[(int)fnType];

                        ret.storeDecl.AddFunctionLoc(function.fnType);

                        ret.functions.Add(function);

                    }
                }
                else if(sectionCode == Bin.Section.TableSec)
                { 
                    uint numTables = BinParse.LoadUnsignedLEB32(pb, ref idx);

                    for(uint i = 0; i < numTables; ++i)
                    { 
                        const int FLAG_HASMAX = 0x01;
                        const int FLAG_OTHERS = ~(FLAG_HASMAX);

                        Bin.TypeID ty = (Bin.TypeID)BinParse.LoadUnsignedLEB32(pb, ref idx);

                        uint flags = BinParse.LoadUnsignedLEB32(pb, ref idx);
                        uint initial = BinParse.LoadUnsignedLEB32(pb, ref idx); 
                        uint ? max = null;
                        
                        if((flags & FLAG_HASMAX) != 0)
                            max = BinParse.LoadUnsignedLEB32(pb, ref idx);

                        if((flags & FLAG_OTHERS) != 0)
                            throw new System.Exception("Table section contains unsupported flags.");

                        ret.storeDecl.AddTableLoc(ty, initial, max);
                    }
                }
                else if(sectionCode == Bin.Section.MemorySec)
                {
                    // Prepare the declaration of memory regions.
                    //
                    // Note that this is only prepping for the data payloads, actual
                    // parsing of that data happens in the Data section.
                    uint numMems = BinParse.LoadUnsignedLEB32(pb, ref idx); // Right now this is assumed to be 1
                    
                    for (uint i = 0; i < numMems; ++i)
                    {
                        const int FLAG_HASMAX = 0x01;
                        const int FLAG_OTHERS = ~(FLAG_HASMAX);

                        uint memFlags           = BinParse.LoadUnsignedLEB32(pb, ref idx);
                        uint memInitialPageCt   = BinParse.LoadUnsignedLEB32(pb, ref idx);
                        uint ? memMaxPageCt     = null;

                        if((memFlags & FLAG_HASMAX) != 0)
                            memMaxPageCt = BinParse.LoadUnsignedLEB32(pb, ref idx);

                        if((memFlags & FLAG_OTHERS) != 0)
                            throw new System.Exception("Memory section contains unsupported flags.");

                        ret.storeDecl.AddMemoryLoc( memInitialPageCt,  memInitialPageCt, memMaxPageCt);
                    }
                }
                else if(sectionCode == Bin.Section.GlobalSec)
                {
                    uint numGlobals = BinParse.LoadUnsignedLEB32(pb, ref idx);
                    for(uint i = 0; i < numGlobals; ++i)
                    {
                        const int FLAG_MUTABLE = 0x01;
                        const int FLAG_OTHERS = ~(FLAG_MUTABLE);

                        uint globType = BinParse.LoadUnsignedLEB32(pb, ref idx);
                        uint globFlags = BinParse.LoadUnsignedLEB32(pb, ref idx);
                        bool mutable = (globFlags & FLAG_MUTABLE) != 0;

                        if((globFlags & FLAG_OTHERS) != 0)
                            throw new System.Exception("Global section contains unsupported flags.");

                        // For now we're just going to assume they do a type.const, then the value,
                        // and then an end.
                        //
                        // I actually haven't read the specs to see what's allowed here.
                        if(globType == (int)Bin.TypeID.Int32)
                        {
                            AssertConsumeByte(pb, ref idx, (byte)Instruction.i32_const);
                            int idef = BinParse.LoadSignedLEB32(pb, ref idx);
                            AssertConsumeByte(pb, ref idx, (byte)Instruction.end);

                            ret.storeDecl.AddGlobalLoc(idef, mutable);
                        }
                        else if(globType == (int)Bin.TypeID.Float32)
                        {
                            AssertConsumeByte(pb, ref idx, (byte)Instruction.f32_const);
                            float fdef = *(float*)&pb[idx];
                            idx += 4;
                            AssertConsumeByte(pb, ref idx, (byte)Instruction.end);

                            ret.storeDecl.AddGlobalLoc(fdef, mutable);
                        }
                        else if(globType == (int)Bin.TypeID.Int64)
                        {
                            AssertConsumeByte(pb, ref idx, (byte)Instruction.i64_const);
                            long ldef = BinParse.LoadSignedLEB64(pb, ref idx);
                            AssertConsumeByte(pb, ref idx, (byte)Instruction.end);

                            ret.storeDecl.AddGlobalLoc(ldef, mutable);
                        }
                        else if (globType == (int)Bin.TypeID.Float64)
                        {
                            AssertConsumeByte(pb, ref idx, (byte)Instruction.f64_const);
                            double ddef = *(float*)&pb[idx];
                            idx += 8;
                            AssertConsumeByte(pb, ref idx, (byte)Instruction.end);

                            ret.storeDecl.AddGlobalLoc(ddef, mutable);
                        }
                        else
                            throw new System.Exception("Unexpected global type.");
                            
                    }
                }
                else if(sectionCode == Bin.Section.ExportSec)
                { 
                    uint numExports = BinParse.LoadUnsignedLEB32(pb, ref idx);
                    for(uint i = 0; i < numExports; ++i)
                    { 
                        uint strLen = BinParse.LoadUnsignedLEB32(pb, ref idx);
                        string name = LoadString(pb, strLen, ref idx);
                        uint kind = BinParse.LoadUnsignedLEB32(pb, ref idx);
                        uint index = BinParse.LoadUnsignedLEB32(pb, ref idx);

                        Export export   = new Export();
                        export.name     = name;
                        export.kind     = (ImportType)kind;
                        export.index    = index;
                        ret.exports.Add(export);
                    }
                }
                else if(sectionCode == Bin.Section.StartSec)
                {
                    ret.startFnIndex = BinParse.LoadUnsignedLEB32(pb, ref idx);
                    ret.ValidateStartFunction(true);
                }
                else if(sectionCode == Bin.Section.ElementSec) 
                {
                    uint numSegments = BinParse.LoadUnsignedLEB32(pb, ref idx);

                    if(numSegments == 0)
                        continue;

                    if (ret.storeDecl.tables.Count < 1)
                        throw new System.Exception("Element(s) specified when no tables are defined.");
                    
                    DefTable defTable = ret.storeDecl.tables[0];

                    for(uint i = 0; i < numSegments; ++i)
                    {
                        // Table index
                        uint flags = BinParse.LoadUnsignedLEB32(pb, ref idx);

                        DefSegment ds = new DefSegment(pb, ref idx, false);

                        Bin.TypeID tabTy = ret.storeDecl.tables[0].type;
                        uint tySize = DataStore.GetTypeIDSize(tabTy);

                        uint elemCt = BinParse.LoadUnsignedLEB32(pb, ref idx);
                        ds.data = new byte[elemCt * tySize];
                        
                        fixed(byte * ptabdefs = ds.data)
                        {
                            switch (tabTy)
                            { 
                                case Bin.TypeID.FuncRef:
                                case Bin.TypeID.Int32:
                                    for (int j = 0; j < elemCt; ++j)
                                    {
                                        ((int*)ptabdefs)[j] = BinParse.LoadSignedLEB32(pb, ref idx);
                                    }
                                    break;

                                case Bin.TypeID.Float32:
                                    for (int j = 0; j < elemCt; ++j)
                                    {
                                        ((float*)ptabdefs)[j] = *(float*)pb;
                                        idx += 4;
                                    }
                                    break;

                                case Bin.TypeID.Int64:
                                    for (int j = 0; j < elemCt; ++j)
                                    {
                                        ((long*)ptabdefs)[j] = BinParse.LoadSignedLEB64(pb, ref idx);
                                    }
                                    break;

                                case Bin.TypeID.Float64:
                                    for (int j = 0; j < elemCt; ++j)
                                    {
                                        ((double*)ptabdefs)[j] = *(double*)pb;
                                        idx += 8;
                                    }
                                    break;
                            }
                        }
                    }
                }
                else if(sectionCode == Bin.Section.CodeSec)
                { 
                    uint numFunctions = BinParse.LoadUnsignedLEB32(pb, ref idx);
                    for(uint i = 0; i < numFunctions; ++i)
                    {
                        Function function = ret.functions[(int)i];

                        uint bodySize = BinParse.LoadUnsignedLEB32(pb, ref idx);
                        uint end = idx + bodySize;

                        uint localsCount = BinParse.LoadUnsignedLEB32(pb, ref idx); 
                        for(int j = 0; j < localsCount; ++j)
                        { 
                            // The number of consecutive occurences of this type
                            uint localTyCt  = BinParse.LoadUnsignedLEB32(pb, ref idx);
                            // The type to place on the stack. The quantity of how many
                            // is specified in localTyCt.
                            uint type = BinParse.LoadUnsignedLEB32(pb, ref idx);

                            for(int k = 0; k < localTyCt; ++k)
                            {
                                FunctionType.DataOrgInfo doi = new FunctionType.DataOrgInfo();
                                doi.type = (Bin.TypeID)type;
                                function.localTypes.Add(doi);
                            }
                        }
                        function.InitializeOrganization();

                        uint size = end - idx;
                        function.expression = new byte[size];

                        System.Runtime.InteropServices.Marshal.Copy(
                            (System.IntPtr)(int*)(&pb[idx]), 
                            function.expression, 
                            (int)0, 
                            (int)size);

                        idx = end;
                    }

                    for (uint i = 0; i < numFunctions; ++i)
                        ret.functions[(int)i].ExpandExpressionToBeUsable(ret);
                }
                else if(sectionCode == Bin.Section.DataSec)
                {
                    uint numData = BinParse.LoadUnsignedLEB32(pb, ref idx);

                    if(numData == 0)
                        continue;

                    if (ret.storeDecl.memories.Count < 1)
                        throw new System.Exception("Data(s) specified when no mems are defined.");

                    for(uint i = 0; i < numData; ++i)
                    {
                        uint segHeaderFlags = BinParse.LoadUnsignedLEB32(pb, ref idx);

                        DefMem dmem = ret.storeDecl.memories[0];

                        DefSegment ds = new DefSegment(pb, ref idx, false);
                        uint dataSz = BinParse.LoadUnsignedLEB32(pb, ref idx);

                        ds.data = new byte[dataSz];

                        // We're going to do the copy manually, but if there's a C#
                        // low-level copy function that also does this, that would be 
                        // prefered.
                        for (uint j = 0; j < dataSz; ++j)
                            ds.data[j] = pb[idx + j];

                        dmem.AddDefault(ds);

                        //ret.storeDecl.memories[(int)i] = dmem;
                        ret.storeDecl.memories[0] = dmem;

                        idx += dataSz;
                    }
                }
                else
                { 
                    throw new System.Exception("Encountered unknown Module section.");
                }
            }
            ++idx;

            return ret;
        }

        unsafe static string LoadString(byte * pb, uint len, ref uint idx)
        { 
            byte [] rb = new byte[len];
            for(int i = 0; i < len; ++i)
                rb[i] = pb[idx + i];

            idx += len;

            return System.Text.Encoding.UTF8.GetString(rb);
        }

        public int GetExportedFunctionID(string fnName)
        {
            foreach(Export e in this.exports)
            { 
                if(e.kind != ImportType.TypeIndex)
                    continue;

                if(e.name == fnName)
                    return (int)e.index;
            }
            return -1;
        }

        public Function GetExportedFunction(string fnName)
        { 
            int fnid = this.GetExportedFunctionID(fnName);
            if(fnid == -1)
                return null;

            IndexEntry ie = this.storeDecl.IndexingFunction[fnid];
            if(ie.type == IndexEntry.FnIdxType.Import)
                return null;

            return this.functions[ie.index];
        }

        public int GetExportedFunctionID(string fnName, out FunctionType fnty)
        {
            foreach (Export e in this.exports)
            {
                if (e.kind != ImportType.TypeIndex)
                    continue;

                if (e.name == fnName)
                {
                    fnty = this.types[(int)this.functions[(int)e.index].typeidx];
                    return (int)e.index;
                }
            }
            fnty = null;
            return -1;
        }

        unsafe private static void AssertConsumeByte(byte* pb, ref uint idx, byte match)
        { 
            if(pb[idx] != match)
                throw new System.Exception($"Unexpected byte. Expecting {match} but encountered {pb[idx]}.");

            ++idx;
        }

        public bool Validate(bool throwOnError = false)
        { 
            if(this.ValidateStartFunction(throwOnError) == false)
                return false;

            HashSet<string> encounteredNames = new HashSet<string>();
            foreach(Export e in this.exports)
            { 
                if(encounteredNames.Add(e.name) == false)
                { 
                    if(throwOnError == true)
                        throw new System.Exception($"Multiple exports found with the same name {e.name}");

                    return false;
                }
            }
            return true;
        }

        public bool ValidateStartFunction(bool throwOnError = false)
        {
            if(this.startFnIndex == UnloadedStartIndex)
                return true;

            IndexEntry fie = this.storeDecl.IndexingFunction[(int)this.startFnIndex];
            Function startFn = this.functions[fie.index];

            if (startFn.fnType.resultTypes.Count > 0 || startFn.fnType.paramTypes.Count > 0)
            {
                if(throwOnError == true)
                    throw new System.Exception("Start function is invalid function type.");

                return false;
            }
            return true;
        }

        //public static Session LoadString(string str)
        //{ }
    }
}