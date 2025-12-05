//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HugeFileReader;

public interface IConfig
{
    public string Font { get; set; }
    public int TabSize { get; set; }
}
