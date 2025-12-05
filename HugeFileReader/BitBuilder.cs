//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System;
using System.Collections.Generic;

namespace HugeFileViewer
{
    public class BitBuilder
    {
        private int _count;
        private readonly List<byte[]> _data = new List<byte[]> ();

        public int Count { get { return _count; } }

        public void Add (byte[] bytes)
        {
            if (bytes != null)
            {
                _count += bytes.Length;
                _data.Add (bytes);
            }
        }

        public void Add (int val)
        {
            byte[] valbytes = new byte[1];
            valbytes[0] = (byte)val;

            _count += 1;
            _data.Add (valbytes);
        }

        public byte[] ToArray ()
        {
            int offset = 0;
            byte[] buffer = new byte[_count];
            for (int ii = 0; ii < _data.Count; ii++)
            {
                int blen = _data[ii].Length;
                Array.Copy (_data[ii], 0, buffer, offset, blen);
                offset += blen;
            }

            return buffer;
        }
    }
}
