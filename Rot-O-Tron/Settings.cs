using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


internal class Settings
{
    public string? ProjectPath { get; set; }
    public bool? CheckMethodLength { get; set; }
    public bool? CheckMagicNumbers { get; set; }
    public bool? CountLines { get; set; }
    public bool? CheckNuget { get; set; }
    public int CheckMethodLengthDefault { get; set; } = 40; // Defaultwert
}