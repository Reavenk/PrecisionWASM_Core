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
    /// <summary>
    /// An implementation of a host function that uses delegates for the
    /// function representation. While it's a bit more clunky to use than
    /// ImportFunction_Refl, it allows assigning lambda functions.
    /// </summary>
    public class ImportFunction_Lam : ImportFunction
    {
        /// <summary>
        /// Delegate type for implementing a host function.
        /// </summary>
        /// <param name="fnParams">The parameters.</param>
        /// <returns>The return values, these must match the correct types
        /// and order of the expected function type being implemented.</returns>
        /// <remarks>The objects for the parameters and return values will
        /// be boxed intrinsic types used for WASM: int, uint, long, ulong,
        /// float, double.</remarks>
        public delegate List<object> fn(List<object> fnParams);

        /// <summary>
        /// Reference to the delegate to invoke.
        /// </summary>
        public fn function = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="function">The delegate to assign.</param>
        public ImportFunction_Lam(fn function)
        { 
            this.function = function;
        }

        public override byte[] InvokeImpl(ImportFunctionUtil utils)
        { 
            if(this.function == null)
                throw new System.Exception("Invoked host function not defined.");

            List<object> fnParams = utils.GetParamsAsObjects();
            List<object> retObjs = function.Invoke(fnParams);
            return utils.ConvertObjectsToResult(retObjs);

        }
    }
}
