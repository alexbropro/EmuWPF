using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmuWPF.ECores
{
    interface IEmulator
    {
        string Name { get; }

        void Launch(string gamePath);

        void Stop();

    }
}
