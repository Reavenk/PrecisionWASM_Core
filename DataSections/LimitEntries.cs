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

namespace PxPre.WASM
{
    /// <summary>
    /// The limits structure for tables.
    /// </summary>
    public struct LimitEntries
    {
        /// <summary>
        /// The size, in bytes, for the data type of the table.
        /// </summary>
        public uint dataTypeSize;

        /// <summary>
        /// The minimum number of table entries.
        /// </summary>
        public uint minEntries;

        /// <summary>
        /// The maximum number of table entries.
        /// </summary>
        public uint maxEntries;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dataTypeSize">The size, in bytes, for the data type of the table.</param>
        /// <param name="minEntries">The minimum number of table entries.</param>
        /// <param name="maxEntries">The maximum number of table entries.</param>
        public LimitEntries(uint dataTypeSize, uint minEntries, uint maxEntries)
        { 
            this.dataTypeSize   = dataTypeSize;
            this.minEntries     = minEntries;
            this.maxEntries     = maxEntries;
        }
    }
}
