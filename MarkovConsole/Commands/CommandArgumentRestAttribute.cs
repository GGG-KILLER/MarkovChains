using System;
using System.Collections.Generic;
using System.Text;

namespace MarkovConsole.Commands
{
    [AttributeUsage ( AttributeTargets.Parameter, AllowMultiple = false, Inherited = false )]
    internal sealed class CommandArgumentRestAttribute : Attribute
    {
    }
}
