using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces;
public interface IUdpMessageTypeFieldDto
{
    string Field { get;}
    int Index { get; }
    string IncomingUnitName { get; }
    string OutgoingUnitName { get; }
}
