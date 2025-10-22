//  <@$&< copyright begin >&$@> 24FE144C2255E2F7CCB65514965434A807AE8998C9C4D01902A628F980431C98:20241017.A:2025:8:7:7:53
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
