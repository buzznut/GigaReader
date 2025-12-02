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
